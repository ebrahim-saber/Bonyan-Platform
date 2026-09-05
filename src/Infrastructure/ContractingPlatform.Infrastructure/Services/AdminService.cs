using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Admin;
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

    public async Task<PlatformStatisticsDto> GetPlatformStatisticsAsync()
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

        return new PlatformStatisticsDto
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

    public async Task<AdminFinancialDashboardDto> GetFinancialDashboardAsync()
    {
        var totalVolume = await _context.ProjectContracts
            .Where(c => !c.IsDeleted)
            .SumAsync(c => (decimal?)c.TotalAmount) ?? 0m;

        var totalCommission = await _context.ProjectContracts
            .Where(c => !c.IsDeleted)
            .SumAsync(c => (decimal?)c.PlatformCommissionAmount) ?? 0m;

        var totalHeldInEscrow = await _context.PaymentTransactions
            .Where(t => t.PaymentStatus == PaymentStatus.HeldInEscrow && !t.IsDeleted)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var totalReleased = await _context.PaymentTransactions
            .Where(t => t.PaymentStatus == PaymentStatus.ReleasedToContractor && !t.IsDeleted)
            .SumAsync(t => (decimal?)t.NetAmount) ?? 0m;

        var activeContractsCount = await _context.ProjectContracts
            .CountAsync(c => c.Status == ProjectStatus.InProgress && !c.IsDeleted);

        var totalTransactionsCount = await _context.PaymentTransactions
            .CountAsync(t => !t.IsDeleted);

        var recentTransactions = await _context.PaymentTransactions
            .Include(t => t.Contract).ThenInclude(c => c.ProjectRequest)
            .Include(t => t.Milestone)
            .Include(t => t.Client).ThenInclude(cl => cl.User)
            .Include(t => t.Contractor)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(30)
            .Select(t => new AdminEscrowTransactionDto
            {
                Id = t.Id,
                TransactionReference = t.TransactionReference,
                ContractId = t.ProjectContractId,
                ProjectTitle = t.Contract.ProjectRequest.Title,
                MilestoneTitle = t.Milestone != null ? t.Milestone.Title : "دفعة تعاقدية عامة",
                ClientName = t.Client.User.FullName,
                ContractorCompanyName = t.Contractor.CompanyName,
                Amount = t.Amount,
                FeeAmount = t.PlatformFee,
                NetAmount = t.NetAmount,
                Status = t.PaymentStatus,
                Method = t.PaymentMethod,
                CreatedAt = t.CreatedAt,
                EscrowLockedAt = t.EscrowLockedAt,
                EscrowReleasedAt = t.EscrowReleasedAt
            })
            .ToListAsync();

        return new AdminFinancialDashboardDto
        {
            TotalContractVolume = totalVolume,
            TotalHeldInEscrow = totalHeldInEscrow,
            TotalReleasedToContractors = totalReleased,
            TotalPlatformCommission = totalCommission,
            ActiveEscrowContractsCount = activeContractsCount,
            TotalTransactionsCount = totalTransactionsCount,
            RecentTransactions = recentTransactions
        };
    }
}
