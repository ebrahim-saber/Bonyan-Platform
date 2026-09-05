using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Contractors;
using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class ContractorService : IContractorService
{
    private readonly ApplicationDbContext _context;

    public ContractorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContractorPublicProfileDto?> GetPublicProfileAsync(int contractorProfileId)
    {
        var contractor = await _context.ContractorProfiles
            .Include(c => c.User)
            .Include(c => c.Services).ThenInclude(cs => cs.ServiceItem)
            .Include(c => c.ReviewsReceived).ThenInclude(r => r.Client).ThenInclude(cl => cl.User)
            .Include(c => c.Contracts.Where(ct => ct.Status == ProjectStatus.Completed))
                .ThenInclude(ct => ct.ProjectRequest).ThenInclude(p => p.Category)
            .Include(c => c.Contracts.Where(ct => ct.Status == ProjectStatus.Completed))
                .ThenInclude(ct => ct.Review)
            .Include(c => c.Contracts.Where(ct => ct.Status == ProjectStatus.Completed))
                .ThenInclude(ct => ct.Client).ThenInclude(cl => cl.User)
            .FirstOrDefaultAsync(c => c.Id == contractorProfileId && !c.IsDeleted);

        if (contractor == null)
        {
            return null;
        }

        var reviews = contractor.ReviewsReceived.OrderByDescending(r => r.CreatedAt).ToList();
        double qualityPct = 100;
        double punctualityPct = 100;
        double communicationPct = 100;

        if (reviews.Any())
        {
            qualityPct = Math.Round(reviews.Average(r => r.QualityRating) / 5.0 * 100, 1);
            punctualityPct = Math.Round(reviews.Average(r => r.PunctualityRating) / 5.0 * 100, 1);
            communicationPct = Math.Round(reviews.Average(r => r.CommunicationRating) / 5.0 * 100, 1);
        }

        var completedProjects = contractor.Contracts
            .Where(ct => ct.Status == ProjectStatus.Completed)
            .OrderByDescending(ct => ct.ActualEndDate ?? ct.CreatedAt)
            .Select(ct => new ContractorCompletedProjectDto
            {
                ContractId = ct.Id,
                ProjectRequestId = ct.ProjectRequestId,
                Title = ct.ProjectRequest?.Title ?? $"مشروع رقم #{ct.ProjectRequestId}",
                CategoryName = ct.ProjectRequest?.Category?.NameAr ?? "مقاولات عامة",
                City = ct.ProjectRequest?.City ?? contractor.City,
                TotalAmount = ct.TotalAmount,
                CompletedAt = ct.ActualEndDate ?? ct.CreatedAt,
                OverallRating = ct.Review?.OverallRating,
                ClientComment = ct.Review?.Comment,
                ClientName = ct.Client?.User?.FullName ?? "عميل المنصة"
            }).ToList();

        var reviewDtos = reviews.Select(r => new ReviewItemDto
        {
            Id = r.Id,
            ClientName = r.Client?.User?.FullName ?? "عميل موثق",
            OverallRating = r.OverallRating,
            QualityRating = r.QualityRating,
            PunctualityRating = r.PunctualityRating,
            CommunicationRating = r.CommunicationRating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new ContractorPublicProfileDto
        {
            Id = contractor.Id,
            UserId = contractor.UserId,
            CompanyName = contractor.CompanyName,
            CommercialRegistrationNo = contractor.CommercialRegistrationNo,
            TaxNumber = contractor.TaxNumber,
            Bio = contractor.Bio ?? "منشأة مقاولات وتشطيبات هندسية معتمدة عبر منصة بُنيان.",
            YearsOfExperience = contractor.YearsOfExperience,
            LogoUrl = contractor.LogoUrl,
            City = contractor.City,
            District = contractor.District,
            CoverageCities = contractor.CoverageCities ?? contractor.City,
            VerificationStatus = contractor.VerificationStatus,
            Rating = contractor.Rating,
            TotalReviews = contractor.TotalReviews,
            QualityPercentage = qualityPct,
            PunctualityPercentage = punctualityPct,
            CommunicationPercentage = communicationPct,
            Services = contractor.Services.Select(s => s.ServiceItem.NameAr).ToList(),
            CompletedProjects = completedProjects,
            Reviews = reviewDtos
        };
    }

    public async Task<List<ContractorDirectoryItemDto>> GetContractorsDirectoryAsync(string? city = null, int? serviceId = null)
    {
        var query = _context.ContractorProfiles
            .AsNoTracking()
            .Include(c => c.Services).ThenInclude(cs => cs.ServiceItem)
            .Where(c => !c.IsDeleted && c.VerificationStatus == VerificationStatus.Approved);

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City.Contains(city) || (c.CoverageCities != null && c.CoverageCities.Contains(city)));
        }

        if (serviceId.HasValue)
        {
            query = query.Where(c => c.Services.Any(s => s.ServiceItemId == serviceId.Value));
        }

        var contractors = await query
            .OrderByDescending(c => c.Rating)
            .ThenByDescending(c => c.TotalReviews)
            .ToListAsync();

        return contractors.Select(c => new ContractorDirectoryItemDto
        {
            Id = c.Id,
            CompanyName = c.CompanyName,
            City = c.City,
            YearsOfExperience = c.YearsOfExperience,
            Rating = c.Rating,
            TotalReviews = c.TotalReviews,
            IsVerified = c.VerificationStatus == VerificationStatus.Approved,
            Services = c.Services.Select(s => s.ServiceItem.NameAr).ToList()
        }).ToList();
    }
}
