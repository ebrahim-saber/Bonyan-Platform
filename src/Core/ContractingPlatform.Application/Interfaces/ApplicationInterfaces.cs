using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.DTOs.Bids;
using ContractingPlatform.Application.DTOs.Contracts;
using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    Task<int?> GetClientProfileIdAsync();
    Task<int?> GetContractorProfileIdAsync();
}

public interface IProjectService
{
    Task<ApiResponse<int>> CreateProjectAsync(CreateProjectDto dto, int clientProfileId);
    Task<ApiResponse<ProjectDetailsDto>> GetProjectDetailsAsync(int projectId, int? currentContractorProfileId = null);
    Task<List<ProjectCardDto>> GetOpenProjectsAsync(int? categoryId = null, string? city = null);
    Task<List<ProjectCardDto>> GetClientProjectsAsync(int clientProfileId);
    Task<List<Category>> GetActiveCategoriesAsync();
    Task<List<ServiceItem>> GetCategoryServicesAsync(int categoryId);
}

public interface IBidService
{
    Task<ApiResponse<int>> SubmitBidAsync(CreateBidDto dto, int contractorProfileId);
    Task<ApiResponse<bool>> AcceptBidAsync(AcceptBidDto dto, int clientProfileId);
    Task<List<BidListItemDto>> GetContractorBidsAsync(int contractorProfileId);
}

public interface IContractService
{
    Task<ApiResponse<ContractDetailsDto>> GetContractDetailsAsync(int contractId);
    Task<List<ContractDetailsDto>> GetUserContractsAsync(string userId, UserType userType);
    Task<ApiResponse<bool>> SubmitMilestoneProofAsync(SubmitMilestoneProofDto dto, int contractorProfileId);
    Task<ApiResponse<bool>> ApproveMilestoneAndReleasePaymentAsync(int milestoneId, int clientProfileId, string? notes);
}

public interface IReviewService
{
    Task<ApiResponse<int>> SubmitReviewAsync(CreateReviewDto dto, int clientProfileId);
    Task<List<ReviewItemDto>> GetContractorReviewsAsync(int contractorProfileId);
}

public interface IAdminService
{
    Task<List<ContractorProfile>> GetPendingContractorsAsync();
    Task<ApiResponse<bool>> UpdateContractorStatusAsync(int contractorProfileId, VerificationStatus status, string? notes);
    Task<object> GetPlatformStatisticsAsync();
}
