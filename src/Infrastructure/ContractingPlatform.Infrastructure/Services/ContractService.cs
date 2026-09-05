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

    public ContractService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ContractDetailsDto>> GetContractDetailsAsync(int contractId)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.ProjectRequest).ThenInclude(p => p.Category)
            .Include(c => c.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contractor).ThenInclude(co => co.User)
            .Include(c => c.Milestones.OrderBy(m => m.OrderIndex))
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == contractId && !c.IsDeleted);

        if (contract == null)
        {
            return ApiResponse<ContractDetailsDto>.Fail("العقد المطلوب غير موجود");
        }

        var dto = new ContractDetailsDto
        {
            Id = contract.Id,
            ProjectRequestId = contract.ProjectRequestId,
            ProjectTitle = contract.ProjectRequest.Title,
            CategoryName = contract.ProjectRequest.Category.NameAr,
            ClientProfileId = contract.ClientProfileId,
            ClientName = contract.Client.User.FullName,
            ClientPhone = contract.Client.User.PhoneNumber ?? "",
            ContractorProfileId = contract.ContractorProfileId,
            ContractorCompanyName = contract.Contractor.CompanyName,
            ContractorPhone = contract.Contractor.User.PhoneNumber ?? "",
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
                ClientApprovalNotes = m.ClientApprovalNotes
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
            return ApiResponse<bool>.Fail("غير مصرح لك بتقديم إنجاز لهذه المرحلة");
        }

        milestone.Status = MilestoneStatus.SubmittedForReview;
        milestone.ContractorSubmissionNotes = dto.Notes;
        milestone.ContractorProofAttachmentUrl = dto.AttachmentUrl;

        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "تم تسليم إنجاز المرحلة للعميل بنجاح للمراجعة والاعتماد");
    }

    public async Task<ApiResponse<bool>> ApproveMilestoneAndReleasePaymentAsync(int milestoneId, int clientProfileId, string? notes)
    {
        var milestone = await _context.ProjectMilestones
            .Include(m => m.Contract).ThenInclude(c => c.Milestones)
            .Include(m => m.Contract).ThenInclude(c => c.ProjectRequest)
            .FirstOrDefaultAsync(m => m.Id == milestoneId);

        if (milestone == null)
        {
            return ApiResponse<bool>.Fail("المرحلة غير موجودة");
        }

        if (milestone.Contract.ClientProfileId != clientProfileId)
        {
            return ApiResponse<bool>.Fail("غير مصرح لك باعتماد هذه المرحلة");
        }

        milestone.Status = MilestoneStatus.Paid;
        milestone.CompletedAt = DateTime.UtcNow;
        milestone.ClientApprovalNotes = notes;

        // Record Escrow Release Transaction
        var transaction = new PaymentTransaction
        {
            ProjectContractId = milestone.ProjectContractId,
            MilestoneId = milestone.Id,
            ClientProfileId = clientProfileId,
            ContractorProfileId = milestone.Contract.ContractorProfileId,
            Amount = milestone.Amount,
            PlatformFee = Math.Round(milestone.Amount * (milestone.Contract.PlatformCommissionPercentage / 100m), 2),
            NetAmount = milestone.Amount - Math.Round(milestone.Amount * (milestone.Contract.PlatformCommissionPercentage / 100m), 2),
            PaymentStatus = PaymentStatus.ReleasedToContractor,
            PaymentMethod = PaymentMethod.Mada,
            EscrowReleasedAt = DateTime.UtcNow
        };

        await _context.PaymentTransactions.AddAsync(transaction);

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

        return ApiResponse<bool>.Ok(true, allCompleted 
            ? "تم اعتماد آخر مرحلة واكتمال المشروع بنجاح! يمكنك الآن تقييم المقاول" 
            : "تم اعتماد إنجاز المرحلة والإفراج عن الدفعة للمقاول بنجاح");
    }
}
