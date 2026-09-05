using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Bids;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class BidService : IBidService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public BidService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<int>> SubmitBidAsync(CreateBidDto dto, int contractorProfileId)
    {
        var project = await _context.ProjectRequests
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectRequestId && !p.IsDeleted);

        if (project == null)
        {
            return ApiResponse<int>.Fail("المشروع المطلوب غير موجود");
        }

        if (project.Status != ProjectStatus.OpenForBids)
        {
            return ApiResponse<int>.Fail("المشروع غير متاح حالياً لتقديم العروض");
        }

        var existingBid = await _context.Bids
            .FirstOrDefaultAsync(b => b.ProjectRequestId == dto.ProjectRequestId && b.ContractorProfileId == contractorProfileId);

        if (existingBid != null)
        {
            return ApiResponse<int>.Fail("لقد قمت بتقديم عرض سعر مسبقاً على هذا المشروع");
        }

        var bid = new Bid
        {
            ProjectRequestId = dto.ProjectRequestId,
            ContractorProfileId = contractorProfileId,
            ProposedPrice = dto.ProposedPrice,
            DurationDays = dto.DurationDays,
            Notes = dto.Notes.Trim(),
            MaterialCost = dto.MaterialCost,
            LaborCost = dto.LaborCost,
            Status = BidStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        project.BidsCount++;
        await _context.Bids.AddAsync(bid);
        await _context.SaveChangesAsync();

        var clientProfile = await _context.ClientProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == project.ClientProfileId);
        var contractorProfile = await _context.ContractorProfiles.FirstOrDefaultAsync(c => c.Id == contractorProfileId);
        if (clientProfile != null && contractorProfile != null)
        {
            await _notificationService.SendNotificationAsync(
                clientProfile.UserId,
                "عرض سعر جديد على مشروعك",
                $"قدمت منشأة {contractorProfile.CompanyName} عرض سعر بقيمة {bid.ProposedPrice:N0} ر.س لمشروع '{project.Title}'.",
                $"/Projects/Details/{project.Id}");
        }

        return ApiResponse<int>.Ok(bid.Id, "تم إرسال عرض السعر للعميل بنجاح");
    }

    public async Task<ApiResponse<bool>> AcceptBidAsync(AcceptBidDto dto, int clientProfileId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var bid = await _context.Bids
                .Include(b => b.ProjectRequest)
                .Include(b => b.Contractor)
                .FirstOrDefaultAsync(b => b.Id == dto.BidId);

            if (bid == null)
            {
                return ApiResponse<bool>.Fail("عرض السعر المحدد غير موجود");
            }

            var project = bid.ProjectRequest;
            if (project.ClientProfileId != clientProfileId)
            {
                return ApiResponse<bool>.Fail("غير مصرح لك بقبول عروض على هذا المشروع");
            }

            if (project.Status != ProjectStatus.OpenForBids)
            {
                return ApiResponse<bool>.Fail("المشروع ليس في حالة استقبال العروض حالياً");
            }

            // 1. Update Bids Status
            bid.Status = BidStatus.Accepted;
            var otherBids = await _context.Bids
                .Where(b => b.ProjectRequestId == project.Id && b.Id != bid.Id)
                .ToListAsync();

            foreach (var ob in otherBids)
            {
                ob.Status = BidStatus.Rejected;
            }

            // 2. Update Project Status
            project.Status = ProjectStatus.InProgress;

            // 3. Create Contract
            decimal commissionRate = 5.0m; // 5%
            decimal commissionAmount = Math.Round(bid.ProposedPrice * (commissionRate / 100m), 2);
            decimal netContractorAmount = bid.ProposedPrice - commissionAmount;

            var contract = new ProjectContract
            {
                ProjectRequestId = project.Id,
                AcceptedBidId = bid.Id,
                ClientProfileId = clientProfileId,
                ContractorProfileId = bid.ContractorProfileId,
                TotalAmount = bid.ProposedPrice,
                PlatformCommissionPercentage = commissionRate,
                PlatformCommissionAmount = commissionAmount,
                ContractorNetAmount = netContractorAmount,
                Status = ProjectStatus.InProgress,
                StartDate = DateTime.UtcNow,
                ExpectedEndDate = DateTime.UtcNow.AddDays(bid.DurationDays),
                TermsAndConditions = dto.TermsAndConditions ?? "العقد يخضع لسياسات المنصة والشروط المعتمدة"
            };

            await _context.ProjectContracts.AddAsync(contract);
            await _context.SaveChangesAsync();

            // 4. Create Milestones
            if (dto.Milestones != null && dto.Milestones.Any())
            {
                int order = 1;
                foreach (var m in dto.Milestones)
                {
                    contract.Milestones.Add(new ProjectMilestone
                    {
                        ProjectContractId = contract.Id,
                        Title = m.Title,
                        Description = m.Description,
                        Amount = m.Amount,
                        OrderIndex = order++,
                        Status = MilestoneStatus.Pending,
                        DueDate = m.DueDate
                    });
                }
            }
            else
            {
                // Default 3 standard milestones: 30% Down payment, 40% Mid progress, 30% Handover
                decimal m1 = Math.Round(bid.ProposedPrice * 0.30m, 2);
                decimal m2 = Math.Round(bid.ProposedPrice * 0.40m, 2);
                decimal m3 = bid.ProposedPrice - (m1 + m2);

                contract.Milestones.Add(new ProjectMilestone
                {
                    ProjectContractId = contract.Id,
                    Title = "الدفعة الأولى: بدء الأعمال وتوريد التجهيزات",
                    Description = "تجهيز الموقع والبدء في المرحلة التأسيسية",
                    Amount = m1,
                    OrderIndex = 1,
                    Status = MilestoneStatus.InProgress
                });

                contract.Milestones.Add(new ProjectMilestone
                {
                    ProjectContractId = contract.Id,
                    Title = "الدفعة الثانية: إنجاز المرحلة الرئيسية",
                    Description = "إنجاز 60% من الأعمال الميدانية المعتمدة",
                    Amount = m2,
                    OrderIndex = 2,
                    Status = MilestoneStatus.Pending
                });

                contract.Milestones.Add(new ProjectMilestone
                {
                    ProjectContractId = contract.Id,
                    Title = "الدفعة الختامية: الفحص والتسليم النهائي",
                    Description = "معاينة العميل واستلام الأعمال كاملة مع شهادات الضمان",
                    Amount = m3,
                    OrderIndex = 3,
                    Status = MilestoneStatus.Pending
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var winningContractor = await _context.ContractorProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == bid.ContractorProfileId);
            var projectClient = await _context.ClientProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == clientProfileId);
            if (winningContractor != null)
            {
                await _notificationService.SendNotificationAsync(
                    winningContractor.UserId,
                    "تهانينا! تم قبول عرض السعر وتوقيع العقد",
                    $"وافق العميل {(projectClient?.User?.FullName ?? "صاحب المشروع")} على عرضك لمشروع '{project.Title}'. تم إنشاء العقد رقم #{contract.Id}.",
                    $"/Contracts/Details/{contract.Id}");
            }

            return ApiResponse<bool>.Ok(true, "تم قبول العرض وإنشاء العقد وجدولة الدفعات بنجاح");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<bool>.Fail($"حدث خطأ أثناء قبول العرض: {ex.Message}");
        }
    }

    public async Task<List<BidListItemDto>> GetContractorBidsAsync(int contractorProfileId)
    {
        return await _context.Bids
            .AsNoTracking()
            .Include(b => b.ProjectRequest)
            .Where(b => b.ContractorProfileId == contractorProfileId)
            .OrderByDescending(b => b.SubmittedAt)
            .Select(b => new BidListItemDto
            {
                Id = b.Id,
                ContractorProfileId = b.ContractorProfileId,
                ContractorCompanyName = b.Contractor.CompanyName,
                ProposedPrice = b.ProposedPrice,
                DurationDays = b.DurationDays,
                Notes = b.Notes,
                MaterialCost = b.MaterialCost,
                LaborCost = b.LaborCost,
                Status = b.Status,
                SubmittedAt = b.SubmittedAt
            })
            .ToListAsync();
    }
}
