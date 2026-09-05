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
