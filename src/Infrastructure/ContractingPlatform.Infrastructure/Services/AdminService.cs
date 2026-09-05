using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContractorProfile>> GetPendingContractorsAsync()
    {
        return await _context.ContractorProfiles
            .Include(c => c.User)
            .Include(c => c.Services).ThenInclude(s => s.ServiceItem)
            .Where(c => c.VerificationStatus == VerificationStatus.Pending && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiResponse<bool>> UpdateContractorStatusAsync(int contractorProfileId, VerificationStatus status, string? notes)
    {
        var contractor = await _context.ContractorProfiles
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == contractorProfileId);

        if (contractor == null)
        {
            return ApiResponse<bool>.Fail("ملف المقاول غير موجود");
        }

        contractor.VerificationStatus = status;
        contractor.VerificationNotes = notes;
        if (status == VerificationStatus.Approved)
        {
            contractor.VerifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        string msg = status switch
        {
            VerificationStatus.Approved => "تم اعتماد وتوثيق حساب المنشأة بنجاح",
            VerificationStatus.Rejected => "تم رفض اعتماد المنشأة مع حفظ الملاحظات",
            _ => "تم تحديث حالة المنشأة"
        };

        return ApiResponse<bool>.Ok(true, msg);
    }

    public async Task<object> GetPlatformStatisticsAsync()
    {
        var totalProjects = await _context.ProjectRequests.CountAsync(p => !p.IsDeleted);
        var activeProjects = await _context.ProjectRequests.CountAsync(p => p.Status == ProjectStatus.InProgress && !p.IsDeleted);
        var openProjects = await _context.ProjectRequests.CountAsync(p => p.Status == ProjectStatus.OpenForBids && !p.IsDeleted);
        var completedProjects = await _context.ProjectRequests.CountAsync(p => p.Status == ProjectStatus.Completed && !p.IsDeleted);

        var totalContractors = await _context.ContractorProfiles.CountAsync(c => !c.IsDeleted);
        var approvedContractors = await _context.ContractorProfiles.CountAsync(c => c.VerificationStatus == VerificationStatus.Approved && !c.IsDeleted);
        var pendingContractors = await _context.ContractorProfiles.CountAsync(c => c.VerificationStatus == VerificationStatus.Pending && !c.IsDeleted);

        var totalClients = await _context.ClientProfiles.CountAsync(c => !c.IsDeleted);
        var totalBids = await _context.Bids.CountAsync(b => !b.IsDeleted);

        var totalVolume = await _context.ProjectContracts
            .Where(c => !c.IsDeleted)
            .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m;

        var totalPlatformCommission = await _context.ProjectContracts
            .Where(c => !c.IsDeleted)
            .SumAsync(c => (decimal?)c.PlatformCommissionAmount) ?? 0m;

        return new
        {
            TotalProjects = totalProjects,
            ActiveProjects = activeProjects,
            OpenProjects = openProjects,
            CompletedProjects = completedProjects,
            TotalContractors = totalContractors,
            ApprovedContractors = approvedContractors,
            PendingContractors = pendingContractors,
            TotalClients = totalClients,
            TotalBids = totalBids,
            TotalVolume = totalVolume,
            TotalPlatformCommission = totalPlatformCommission
        };
    }
}
