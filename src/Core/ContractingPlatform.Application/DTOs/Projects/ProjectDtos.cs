using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Projects;

public class CreateProjectDto
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? DetailedAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public decimal? ExpectedBudgetMin { get; set; }
    public decimal? ExpectedBudgetMax { get; set; }
    public DateTime? DesiredExecutionDate { get; set; }
    public List<ContractingPlatform.Application.Interfaces.UploadedFileResult> Attachments { get; set; } = new();
}

public class ProjectAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FormattedSize => FileSizeBytes > 1024 * 1024 
        ? $"{(FileSizeBytes / (1024.0 * 1024.0)):F1} MB" 
        : $"{(FileSizeBytes / 1024.0):F0} KB";
    public bool IsImage => ContentType.StartsWith("image/") || 
                           FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                           FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || 
                           FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                           FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    public bool IsPdf => ContentType.Contains("pdf") || FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    public bool IsDwg => FileName.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase);
}

public class ProjectCardDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public decimal? ExpectedBudgetMin { get; set; }
    public decimal? ExpectedBudgetMax { get; set; }
    public ProjectStatus Status { get; set; }
    public int BidsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ClientName { get; set; } = string.Empty;
}

public class ProjectDetailsDto : ProjectCardDto
{
    public int ClientProfileId { get; set; }
    public string? DetailedAddress { get; set; }
    public DateTime? DesiredExecutionDate { get; set; }
    public List<string> AttachmentUrls { get; set; } = new();
    public List<ProjectAttachmentDto> Attachments { get; set; } = new();
    public List<BidListItemDto> Bids { get; set; } = new();
    public bool HasUserBid { get; set; }
}

public class BidListItemDto
{
    public int Id { get; set; }
    public int ContractorProfileId { get; set; }
    public string ContractorCompanyName { get; set; } = string.Empty;
    public decimal ContractorRating { get; set; }
    public int ContractorTotalReviews { get; set; }
    public decimal ProposedPrice { get; set; }
    public int DurationDays { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public BidStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
}
