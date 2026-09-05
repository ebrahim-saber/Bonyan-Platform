using System.Data;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Contracts;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly ApplicationDbContext _context;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly INotificationService _notificationService;

    public ContractService(
        ApplicationDbContext context, 
        ISecurityAuditService securityAuditService,
        INotificationService notificationService)
    {
        _context = context;
        _securityAuditService = securityAuditService;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<ContractDetailsDto>> GetContractDetailsAsync(int contractId, string? requestingUserId = null, bool isAdmin = false)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.ProjectRequest).ThenInclude(p => p.Category)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.Milestones.OrderBy(m => m.OrderIndex)).ThenInclude(m => m.Transaction)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);

        if (contract == null)
        {
            return ApiResponse<ContractDetailsDto>.Fail("العقد المطلوب غير موجود");
        }

        // BOLA / IDOR Defense Check: Only the Contract's Client, Contractor, or Platform Admin may view
        if (!isAdmin && !string.IsNullOrEmpty(requestingUserId))
        {
            bool isClient = contract.Client?.UserId == requestingUserId;
            bool isContractor = contract.Contractor?.UserId == requestingUserId;

            if (!isClient && !isContractor)
            {
                await _securityAuditService.LogSecurityEventAsync(
                    "UNAUTHORIZED_CONTRACT_ACCESS_BLOCKED",
                    $"User '{requestingUserId}' attempted unauthorized view of Contract #{contractId}",
                    userId: requestingUserId,
                    isSuspicious: true);

                return ApiResponse<ContractDetailsDto>.Fail("غير مصرح لك بالاطلاع على تفاصيل هذا العقد");
            }
        }

        var dto = new ContractDetailsDto
        {
            Id = contract.Id,
            ProjectRequestId = contract.ProjectRequestId,
            ProjectTitle = contract.ProjectRequest.Title,
            CategoryName = contract.ProjectRequest.Category.NameAr,
            ClientProfileId = contract.ClientProfileId,
            ClientName = contract.Client?.User?.FullName ?? "العميل",
            ClientPhone = contract.Client?.User?.PhoneNumber ?? "",
            ContractorProfileId = contract.ContractorProfileId,
            ContractorCompanyName = contract.Contractor?.CompanyName ?? "المقاول",
            ContractorPhone = contract.Contractor?.User?.PhoneNumber ?? "",
            TotalAmount = contract.TotalAmount,
            PlatformCommissionAmount = contract.PlatformCommissionAmount,
            ContractorNetAmount = contract.ContractorNetAmount,
            Status = contract.Status,
            StartDate = contract.StartDate,
            ExpectedEndDate = contract.ExpectedEndDate,
            TermsAndConditions = contract.TermsAndConditions,
            HasReview = contract.Review != null,
            Milestones = contract.Milestones.Select(m => new MilestoneItemDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Amount = m.Amount,
                OrderIndex = m.OrderIndex,
                Status = m.Status,
                DueDate = m.DueDate,
                CompletedAt = m.CompletedAt,
                ContractorSubmissionNotes = m.ContractorSubmissionNotes,
                ContractorProofAttachmentUrl = m.ContractorProofAttachmentUrl,
                ClientApprovalNotes = m.ClientApprovalNotes,
                TransactionReference = m.Transaction?.TransactionReference,
                PaymentStatus = m.Transaction?.PaymentStatus
            }).ToList()
        };

        return ApiResponse<ContractDetailsDto>.Ok(dto);
    }

    public async Task<List<ContractDetailsDto>> GetUserContractsAsync(string userId, UserType userType)
    {
        var query = _context.ProjectContracts
            .AsNoTracking()
            .Include(c => c.ProjectRequest).ThenInclude(p => p.Category)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.Milestones)
            .Where(c => !c.IsDeleted);

        if (userType == UserType.Client)
        {
            query = query.Where(c => c.Client.UserId == userId);
        }
        else if (userType == UserType.Contractor)
        {
            query = query.Where(c => c.Contractor.UserId == userId);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContractDetailsDto
            {
                Id = c.Id,
                ProjectRequestId = c.ProjectRequestId,
                ProjectTitle = c.ProjectRequest.Title,
                CategoryName = c.ProjectRequest.Category.NameAr,
                ClientProfileId = c.ClientProfileId,
                ClientName = c.Client.User.FullName,
                ContractorProfileId = c.ContractorProfileId,
                ContractorCompanyName = c.Contractor.CompanyName,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                StartDate = c.StartDate,
                ExpectedEndDate = c.ExpectedEndDate,
                Milestones = c.Milestones.OrderBy(m => m.OrderIndex).Select(m => new MilestoneItemDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Amount = m.Amount,
                    OrderIndex = m.OrderIndex,
                    Status = m.Status
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<bool>> SubmitMilestoneProofAsync(SubmitMilestoneProofDto dto, int contractorProfileId)
    {
        var milestone = await _context.ProjectMilestones
            .Include(m => m.Contract)
            .FirstOrDefaultAsync(m => m.Id == dto.MilestoneId);

        if (milestone == null)
        {
            return ApiResponse<bool>.Fail("المرحلة غير موجودة");
        }

        if (milestone.Contract.ContractorProfileId != contractorProfileId)
        {
            await _securityAuditService.LogSecurityEventAsync(
                "UNAUTHORIZED_MILESTONE_SUBMIT_ATTEMPT",
                $"ContractorProfile #{contractorProfileId} tried submitting milestone #{dto.MilestoneId} belonging to another contractor",
                isSuspicious: true);

            return ApiResponse<bool>.Fail("غير مصرح لك بتقديم إنجاز لهذه المرحلة");
        }

        if (milestone.Status == MilestoneStatus.Paid)
        {
            return ApiResponse<bool>.Fail("هذه المرحلة مدفوعة ومكتملة بالفعل");
        }

        milestone.Status = MilestoneStatus.SubmittedForReview;
        milestone.ContractorSubmissionNotes = dto.Notes;
        milestone.ContractorProofAttachmentUrl = dto.AttachmentUrl;

        await _context.SaveChangesAsync();

        var clientProfile = await _context.ClientProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == milestone.Contract.ClientProfileId);
        if (clientProfile != null)
        {
            await _notificationService.SendNotificationAsync(
                clientProfile.UserId,
                "إثبات إنجاز مرحلة بانتظار اعتمادك",
                $"أنهى المقاول مرحلة '{milestone.Title}' في العقد #{milestone.ProjectContractId} ورفع تقرير الإنجاز للمعاينة.",
                $"/Contracts/Details/{milestone.ProjectContractId}");
        }

        return ApiResponse<bool>.Ok(true, "تم تسليم إنجاز المرحلة للعميل بنجاح للمراجعة والاعتماد");
    }

    public async Task<ApiResponse<bool>> ApproveMilestoneAndReleasePaymentAsync(int milestoneId, int clientProfileId, string? notes)
    {
        // Pessimistic / Serializable Lock to prevent double-spending race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Contract).ThenInclude(c => c.Milestones)
                .Include(m => m.Contract).ThenInclude(c => c.ProjectRequest)
                .Include(m => m.Contract).ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
            {
                return ApiResponse<bool>.Fail("المرحلة غير موجودة");
            }

            if (milestone.Contract.ClientProfileId != clientProfileId)
            {
                await _securityAuditService.LogSecurityEventAsync(
                    "UNAUTHORIZED_MILESTONE_PAYMENT_RELEASE_ATTEMPT",
                    $"ClientProfile #{clientProfileId} attempted to release payment for milestone #{milestoneId} of another client",
                    isSuspicious: true);

                return ApiResponse<bool>.Fail("غير مصرح لك باعتماد هذه المرحلة");
            }

            // Anti-Double-Spend Defense Check
            if (milestone.Status == MilestoneStatus.Paid)
            {
                return ApiResponse<bool>.Fail("تنبيه أمني: تم سداد وإفراج دفعة هذه المرحلة مسبقاً، لا يمكن تكرار الصرف");
            }

            if (milestone.Status != MilestoneStatus.SubmittedForReview)
            {
                return ApiResponse<bool>.Fail("لا يمكن الإفراج عن الدفعة؛ المرحلة لم تُسلّم للمعاينة والاعتماد بعد من المقاول");
            }

            milestone.Status = MilestoneStatus.Paid;
            milestone.CompletedAt = DateTime.UtcNow;
            milestone.ClientApprovalNotes = notes;

            // Record Escrow Release Transaction
            var commissionPercentage = milestone.Contract.PlatformCommissionPercentage;
            var platformFee = Math.Round(milestone.Amount * (commissionPercentage / 100m), 2);
            var netAmount = milestone.Amount - platformFee;

            // Update existing Escrow transaction or create release transaction
            var existingTx = await _context.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.MilestoneId == milestone.Id);

            if (existingTx != null)
            {
                existingTx.PaymentStatus = PaymentStatus.ReleasedToContractor;
                existingTx.EscrowReleasedAt = DateTime.UtcNow;
                existingTx.PlatformFee = platformFee;
                existingTx.NetAmount = netAmount;
            }
            else
            {
                var paymentRecord = new PaymentTransaction
                {
                    ProjectContractId = milestone.ProjectContractId,
                    MilestoneId = milestone.Id,
                    ClientProfileId = clientProfileId,
                    ContractorProfileId = milestone.Contract.ContractorProfileId,
                    Amount = milestone.Amount,
                    PlatformFee = platformFee,
                    NetAmount = netAmount,
                    PaymentStatus = PaymentStatus.ReleasedToContractor,
                    PaymentMethod = PaymentMethod.Mada,
                    TransactionReference = "ESCROW-REL-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                    EscrowReleasedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.PaymentTransactions.AddAsync(paymentRecord);
            }

            // Check if all milestones are paid -> mark contract & project as Completed!
            bool allCompleted = milestone.Contract.Milestones.All(m => m.Id == milestone.Id || m.Status == MilestoneStatus.Paid);
            if (allCompleted)
            {
                milestone.Contract.Status = ProjectStatus.Completed;
                milestone.Contract.ActualEndDate = DateTime.UtcNow;
                milestone.Contract.ProjectRequest.Status = ProjectStatus.Completed;
            }
            else
            {
                // Activate the next pending milestone
                var nextMilestone = milestone.Contract.Milestones
                    .Where(m => m.OrderIndex > milestone.OrderIndex && m.Status == MilestoneStatus.Pending)
                    .OrderBy(m => m.OrderIndex)
                    .FirstOrDefault();

                if (nextMilestone != null)
                {
                    nextMilestone.Status = MilestoneStatus.InProgress;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var contractorProfile = await _context.ContractorProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == milestone.Contract.ContractorProfileId);
            if (contractorProfile != null)
            {
                await _notificationService.SendNotificationAsync(
                    contractorProfile.UserId,
                    "تم اعتماد المرحلة وإطلاق الدفعة المالية",
                    $"وافق العميل على إنجاز مرحلة '{milestone.Title}' في العقد #{milestone.ProjectContractId} وتم تحرير دفعة مالية بقيمة {milestone.Amount:N0} ر.س إلى رصيدك.",
                    $"/Contracts/Details/{milestone.ProjectContractId}");
            }

            await _securityAuditService.LogSecurityEventAsync(
                "ESCROW_PAYMENT_RELEASED",
                $"Successfully released Escrow payment {milestone.Amount} SAR for Milestone #{milestone.Id}, Contract #{milestone.ProjectContractId}",
                userId: milestone.Contract.Client?.UserId);

            return ApiResponse<bool>.Ok(true, allCompleted 
                ? "تم اعتماد آخر مرحلة واكتمال المشروع بنجاح! يمكنك الآن تقييم المقاول" 
                : "تم اعتماد إنجاز المرحلة والإفراج عن الدفعة للمقاول بنجاح من حساب الضمان");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<bool>.Fail($"حدث خطأ أثناء معالجة الإفراج المالي: {ex.Message}");
        }
    }
}
