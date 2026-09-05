using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<int>> SubmitReviewAsync(CreateReviewDto dto, int clientProfileId)
    {
        var contract = await _context.ProjectContracts
            .Include(c => c.Contractor)
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == dto.ProjectContractId);

        if (contract == null)
        {
            return ApiResponse<int>.Fail("العقد غير موجود");
        }

        if (contract.ClientProfileId != clientProfileId)
        {
            return ApiResponse<int>.Fail("غير مصرح لك بتقييم هذا المشروع");
        }

        if (contract.Status != ProjectStatus.Completed)
        {
            return ApiResponse<int>.Fail("لا يمكن تقييم المشروع إلا بعد اكتمال جميع مراحله وتسليمها");
        }

        if (contract.Review != null)
        {
            return ApiResponse<int>.Fail("تم تقديم تقييم لهذا المشروع مسبقاً");
        }

        var review = new ProjectReview
        {
            ProjectContractId = contract.Id,
            ClientProfileId = clientProfileId,
            ContractorProfileId = contract.ContractorProfileId,
            OverallRating = dto.OverallRating,
            QualityRating = dto.QualityRating,
            PunctualityRating = dto.PunctualityRating,
            CommunicationRating = dto.CommunicationRating,
            Comment = dto.Comment?.Trim()
        };

        await _context.ProjectReviews.AddAsync(review);

        // Recalculate Contractor Rating & TotalReviews
        var contractor = contract.Contractor;
        var existingRatings = await _context.ProjectReviews
            .Where(r => r.ContractorProfileId == contractor.Id)
            .Select(r => r.OverallRating)
            .ToListAsync();

        existingRatings.Add(dto.OverallRating);
        contractor.TotalReviews = existingRatings.Count;
        contractor.Rating = Math.Round((decimal)existingRatings.Average(), 2);

        await _context.SaveChangesAsync();

        return ApiResponse<int>.Ok(review.Id, "شكراً لك، تم إضافة تقييمك بنجاح");
    }

    public async Task<List<ReviewItemDto>> GetContractorReviewsAsync(int contractorProfileId)
    {
        return await _context.ProjectReviews
            .AsNoTracking()
            .Include(r => r.Client).ThenInclude(c => c.User)
            .Where(r => r.ContractorProfileId == contractorProfileId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewItemDto
            {
                Id = r.Id,
                ClientName = r.Client.User.FullName,
                OverallRating = r.OverallRating,
                QualityRating = r.QualityRating,
                PunctualityRating = r.PunctualityRating,
                CommunicationRating = r.CommunicationRating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }
}
