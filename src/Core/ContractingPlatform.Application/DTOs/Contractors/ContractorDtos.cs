using ContractingPlatform.Application.DTOs.Reviews;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Contractors;

public class ContractorPublicProfileDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNo { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public string? LogoUrl { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? CoverageCities { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public bool IsVerified => VerificationStatus == VerificationStatus.Approved;
    
    // Ratings Breakdown
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public double QualityPercentage { get; set; }
    public double PunctualityPercentage { get; set; }
    public double CommunicationPercentage { get; set; }

    // Services and Portfolio
    public List<string> Services { get; set; } = new();
    public List<ContractorCompletedProjectDto> CompletedProjects { get; set; } = new();
    public List<ReviewItemDto> Reviews { get; set; } = new();
}

public class ContractorCompletedProjectDto
{
    public int ContractId { get; set; }
    public int ProjectRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CompletedAt { get; set; }
    public int? OverallRating { get; set; }
    public string? ClientComment { get; set; }
    public string ClientName { get; set; } = string.Empty;
}

public class ContractorDirectoryItemDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public bool IsVerified { get; set; }
    public List<string> Services { get; set; } = new();
}
