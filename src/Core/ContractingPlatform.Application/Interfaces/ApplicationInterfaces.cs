using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.DTOs.Bids;
using ContractingPlatform.Application.DTOs.Contracts;
using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Application.DTOs.Notifications;
using ContractingPlatform.Application.DTOs.Chat;
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
    Task<ApiResponse<ProjectDetailsDto>> GetProjectDetailsAsync(int projectId, string? currentUserId = null, int? currentContractorProfileId = null, bool isAdmin = false);
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
    Task<ApiResponse<ContractDetailsDto>> GetContractDetailsAsync(int contractId, string? requestingUserId = null, bool isAdmin = false);
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

public interface ISecurityAuditService
{
    Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null, bool isSuspicious = false);
}

public class UploadedFileResult
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public interface IFileStorageService
{
    Task<UploadedFileResult> SaveFileAsync(Stream fileStream, string originalFileName, string contentType, string subFolder, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string relativeFilePath);
    bool IsAllowedExtension(string fileName);
    bool IsAllowedFileSize(long sizeInBytes);
}

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, string? actionUrl = null);
    Task<List<NotificationItemDto>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<UnreadNotificationsSummaryDto> GetUnreadSummaryAsync(string userId);
    Task<ApiResponse<bool>> MarkAsReadAsync(int notificationId, string userId);
    Task<ApiResponse<bool>> MarkAllAsReadAsync(string userId);
}

public interface IChatService
{
    Task<ApiResponse<ChatMessageDto>> SendMessageAsync(SendChatMessageDto dto, string senderUserId);
    Task<List<ChatMessageDto>> GetContractChatHistoryAsync(int contractId, string currentUserId);
    Task<List<ChatConversationDto>> GetUserConversationsAsync(string currentUserId);
    Task<ApiResponse<bool>> MarkConversationAsReadAsync(int contractId, string currentUserId);
}


