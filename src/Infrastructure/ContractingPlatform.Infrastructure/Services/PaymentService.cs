using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Payments;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ISecurityAuditService _securityAuditService;

    public PaymentService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ISecurityAuditService securityAuditService)
    {
        _context = context;
        _notificationService = notificationService;
        _securityAuditService = securityAuditService;
    }

    public async Task<ApiResponse<CheckoutInitiationDto>> PrepareCheckoutAsync(int contractId, int? milestoneId, int clientProfileId)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.ProjectRequest)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.Milestones.OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);

        if (contract == null)
        {
            return ApiResponse<CheckoutInitiationDto>.Fail("العقد المطلوب غير موجود");
        }

        if (contract.ClientProfileId != clientProfileId)
        {
            await _securityAuditService.LogSecurityEventAsync(
                "UNAUTHORIZED_CHECKOUT_ATTEMPT",
                $"ClientProfile #{clientProfileId} attempted to checkout contract #{contractId} owned by ClientProfile #{contract.ClientProfileId}",
                isSuspicious: true);

            return ApiResponse<CheckoutInitiationDto>.Fail("غير مصرح لك بإجراء سداد على هذا العقد");
        }

        ProjectMilestone? targetMilestone = null;
        if (milestoneId.HasValue)
        {
            targetMilestone = contract.Milestones.FirstOrDefault(m => m.Id == milestoneId.Value);
            if (targetMilestone == null)
            {
                return ApiResponse<CheckoutInitiationDto>.Fail("المرحلة المحددة غير موجودة بهذا العقد");
            }
        }
        else
        {
            // Pick the next pending milestone
            targetMilestone = contract.Milestones.FirstOrDefault(m => m.Status == MilestoneStatus.Pending);
            if (targetMilestone == null)
            {
                return ApiResponse<CheckoutInitiationDto>.Fail("جميع مراحل هذا العقد مسددة أو قيد التنفيذ بالفعل");
            }
        }

        if (targetMilestone.Status == MilestoneStatus.Paid)
        {
            return ApiResponse<CheckoutInitiationDto>.Fail("هذه المرحلة مدفوعة ومكتملة بالفعل");
        }

        // Check if already deposited in escrow
        var existingEscrow = await _context.PaymentTransactions
            .FirstOrDefaultAsync(pt => pt.MilestoneId == targetMilestone.Id && pt.PaymentStatus == PaymentStatus.HeldInEscrow);

        if (existingEscrow != null)
        {
            return ApiResponse<CheckoutInitiationDto>.Fail("دفعة هذه المرحلة محجوزة بالفعل في حساب الضمان البنكي");
        }

        decimal baseAmount = targetMilestone.Amount;
        decimal platformFee = Math.Round(baseAmount * (contract.PlatformCommissionPercentage / 100m), 2);
        decimal vatOnFee = Math.Round(platformFee * 0.15m, 2);

        var dto = new CheckoutInitiationDto
        {
            ProjectContractId = contract.Id,
            MilestoneId = targetMilestone.Id,
            ProjectTitle = contract.ProjectRequest?.Title ?? "مشروع مقاولات",
            MilestoneTitle = targetMilestone.Title,
            ClientName = contract.Client?.User?.FullName ?? "العميل",
            ContractorCompanyName = contract.Contractor?.CompanyName ?? "المقاول",
            BaseAmount = baseAmount,
            PlatformFee = platformFee,
            VatAmount = vatOnFee,
            TotalAmount = baseAmount,
            EscrowProtectionNote = "المبلغ يودع في حساب الضمان البنكي المشترك ولا يتم صرفه للمقاول إلا بعد إنجاز المرحلة واعتمادك الميداني لها."
        };

        return ApiResponse<CheckoutInitiationDto>.Ok(dto);
    }

    public async Task<ApiResponse<PaymentReceiptDto>> ProcessPaymentAsync(ProcessPaymentDto dto, int clientProfileId)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.ProjectRequest)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.Milestones)
            .FirstOrDefaultAsync(c => c.Id == dto.ProjectContractId && !c.IsDeleted);

        if (contract == null)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("العقد المطلوب غير موجود");
        }

        if (contract.ClientProfileId != clientProfileId)
        {
            await _securityAuditService.LogSecurityEventAsync(
                "UNAUTHORIZED_PAYMENT_PROCESS_ATTEMPT",
                $"ClientProfile #{clientProfileId} attempted processing payment on contract #{dto.ProjectContractId}",
                isSuspicious: true);

            return ApiResponse<PaymentReceiptDto>.Fail("غير مصرح لك بإجراء سداد على هذا العقد");
        }

        var milestone = contract.Milestones.FirstOrDefault(m => m.Id == dto.MilestoneId);
        if (milestone == null)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("المرحلة المحددة غير صالحة");
        }

        if (milestone.Status == MilestoneStatus.Paid)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("هذه المرحلة مسددة بالكامل مسبقاً");
        }

        // Validate simulated card inputs
        var cleanCard = (dto.CardNumber ?? "").Replace(" ", "").Replace("-", "");
        if (dto.PaymentMethod != PaymentMethod.ApplePay && dto.PaymentMethod != PaymentMethod.BankTransfer)
        {
            if (string.IsNullOrWhiteSpace(cleanCard) || cleanCard.Length < 15 || cleanCard.Length > 19)
            {
                return ApiResponse<PaymentReceiptDto>.Fail("رقم البطاقة غير صحيح. يرجى إدخال 16 رقماً صالحاً.");
            }

            if (string.IsNullOrWhiteSpace(dto.Cvv) || dto.Cvv.Length < 3 || dto.Cvv.Length > 4)
            {
                return ApiResponse<PaymentReceiptDto>.Fail("رمز التحقق (CVV) غير صحيح");
            }
        }

        // Check if already in escrow
        var existingTx = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.MilestoneId == milestone.Id && t.PaymentStatus == PaymentStatus.HeldInEscrow);

        if (existingTx != null)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("تم حجز دفعة هذه المرحلة في الضمان مسبقاً");
        }

        decimal commissionRate = contract.PlatformCommissionPercentage;
        decimal platformFee = Math.Round(milestone.Amount * (commissionRate / 100m), 2);
        decimal netAmount = milestone.Amount - platformFee;
        decimal vatOnFee = Math.Round(platformFee * 0.15m, 2);

        var transactionReference = "ESCROW-DEP-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
        var now = DateTime.UtcNow;

        var transaction = new PaymentTransaction
        {
            ProjectContractId = contract.Id,
            MilestoneId = milestone.Id,
            ClientProfileId = clientProfileId,
            ContractorProfileId = contract.ContractorProfileId,
            Amount = milestone.Amount,
            PlatformFee = platformFee,
            NetAmount = netAmount,
            PaymentStatus = PaymentStatus.HeldInEscrow,
            PaymentMethod = dto.PaymentMethod,
            TransactionReference = transactionReference,
            EscrowLockedAt = now,
            CreatedAt = now
        };

        await _context.PaymentTransactions.AddAsync(transaction);

        // Update milestone status to InProgress now that escrow is funded
        milestone.Status = MilestoneStatus.InProgress;
        if (contract.Status != ProjectStatus.InProgress)
        {
            contract.Status = ProjectStatus.InProgress;
        }

        await _context.SaveChangesAsync();

        await _securityAuditService.LogSecurityEventAsync(
            "ESCROW_PAYMENT_DEPOSITED",
            $"Client #{clientProfileId} successfully deposited {milestone.Amount} SAR into Escrow for Milestone #{milestone.Id}, Contract #{contract.Id}. Ref: {transactionReference}",
            userId: contract.Client.UserId);

        // Notify Contractor via SignalR
        await _notificationService.SendNotificationAsync(
            contract.Contractor.UserId,
            "تم تأمين دفعة في حساب الضمان",
            $"أودع العميل {contract.Client.User.FullName} دفعة مرحلة '{milestone.Title}' بقيمة {milestone.Amount:N0} ر.س في حساب الضمان. يمكنك البدء بالأعمال الميدانية بثقة.",
            $"/Contracts/Details/{contract.Id}");

        // Notify Client
        await _notificationService.SendNotificationAsync(
            contract.Client.UserId,
            "إيداع ناجح في حساب الضمان",
            $"تم تأمين {milestone.Amount:N0} ر.س لمرحلة '{milestone.Title}'. أموالك محفوظة ولن تصرف إلا بعد معاينتك واستلامك للعمل.",
            $"/Payments/Receipt?reference={transactionReference}");

        var receipt = new PaymentReceiptDto
        {
            TransactionReference = transactionReference,
            ProjectContractId = contract.Id,
            MilestoneId = milestone.Id,
            ProjectTitle = contract.ProjectRequest?.Title ?? "مشروع مقاولات",
            MilestoneTitle = milestone.Title,
            ClientName = contract.Client.User.FullName,
            ClientPhone = contract.Client.User.PhoneNumber ?? "",
            ContractorCompanyName = contract.Contractor.CompanyName,
            ContractorCrNumber = contract.Contractor.CommercialRegistrationNo ?? "1010894523",
            BaseAmount = milestone.Amount,
            PlatformFee = platformFee,
            VatAmount = vatOnFee,
            TotalAmount = milestone.Amount,
            ContractorNetAmount = netAmount,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = PaymentStatus.HeldInEscrow,
            CreatedAt = now,
            EscrowLockedAt = now,
            QrCodeData = $"BUNYAN-ESCROW|REF:{transactionReference}|TOTAL:{milestone.Amount}|VAT:{vatOnFee}|DATE:{now:yyyy-MM-dd HH:mm}"
        };

        return ApiResponse<PaymentReceiptDto>.Ok(receipt, "تم إيداع الدفعة بنجاح في حساب الضمان البنكي المعتمد");
    }

    public async Task<ApiResponse<PaymentReceiptDto>> GetReceiptAsync(string transactionReference, string userId, bool isAdmin)
    {
        var tx = await _context.PaymentTransactions
            .Include(t => t.Contract).ThenInclude(c => c.ProjectRequest)
            .Include(t => t.Contract).ThenInclude(c => c.Client).ThenInclude(cl => cl.User)
            .Include(t => t.Contract).ThenInclude(c => c.Contractor).ThenInclude(co => co.User)
            .Include(t => t.Milestone)
            .FirstOrDefaultAsync(t => t.TransactionReference == transactionReference && !t.IsDeleted);

        if (tx == null)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("سند المعاملة غير موجود");
        }

        // BOLA defense: only parties of contract or admin
        bool isClient = tx.Contract.Client.UserId == userId;
        bool isContractor = tx.Contract.Contractor.UserId == userId;

        if (!isAdmin && !isClient && !isContractor)
        {
            return ApiResponse<PaymentReceiptDto>.Fail("غير مصرح لك بعرض هذا السند المالي");
        }

        decimal vatOnFee = Math.Round(tx.PlatformFee * 0.15m, 2);

        var dto = new PaymentReceiptDto
        {
            TransactionReference = tx.TransactionReference,
            ProjectContractId = tx.ProjectContractId,
            MilestoneId = tx.MilestoneId,
            ProjectTitle = tx.Contract.ProjectRequest?.Title ?? "مشروع مقاولات",
            MilestoneTitle = tx.Milestone?.Title ?? "دفعة تعاقدية",
            ClientName = tx.Contract.Client?.User?.FullName ?? "العميل",
            ClientPhone = tx.Contract.Client?.User?.PhoneNumber ?? "",
            ContractorCompanyName = tx.Contract.Contractor?.CompanyName ?? "المقاول",
            ContractorCrNumber = tx.Contract.Contractor?.CommercialRegistrationNo ?? "1010894523",
            BaseAmount = tx.Amount,
            PlatformFee = tx.PlatformFee,
            VatAmount = vatOnFee,
            TotalAmount = tx.Amount,
            ContractorNetAmount = tx.NetAmount,
            PaymentMethod = tx.PaymentMethod,
            PaymentStatus = tx.PaymentStatus,
            CreatedAt = tx.CreatedAt,
            EscrowLockedAt = tx.EscrowLockedAt,
            EscrowReleasedAt = tx.EscrowReleasedAt,
            QrCodeData = $"BUNYAN-ESCROW|REF:{tx.TransactionReference}|TOTAL:{tx.Amount}|STATUS:{tx.PaymentStatus}|DATE:{tx.CreatedAt:yyyy-MM-dd HH:mm}"
        };

        return ApiResponse<PaymentReceiptDto>.Ok(dto);
    }

    public async Task<ContractFinancialSummaryDto> GetContractFinancialSummaryAsync(int contractId)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.Milestones)
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
        {
            return new ContractFinancialSummaryDto();
        }

        var inEscrow = contract.Transactions
            .Where(t => t.PaymentStatus == PaymentStatus.HeldInEscrow)
            .Sum(t => t.Amount);

        var released = contract.Transactions
            .Where(t => t.PaymentStatus == PaymentStatus.ReleasedToContractor)
            .Sum(t => t.Amount);

        var remaining = Math.Max(0, contract.TotalAmount - inEscrow - released);

        return new ContractFinancialSummaryDto
        {
            TotalContractValue = contract.TotalAmount,
            TotalInEscrow = inEscrow,
            TotalReleasedToContractor = released,
            TotalRemainingUnfunded = remaining
        };
    }
}
