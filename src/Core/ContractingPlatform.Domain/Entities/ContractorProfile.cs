using ContractingPlatform.Domain.Common;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Domain.Entities;

public class ContractorProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNo { get; set; } = string.Empty; // السجل التجاري
    public string? TaxNumber { get; set; } // الرقم الضريبي
    public string? Bio { get; set; }
    public int YearsOfExperience { get; set; }
    public string? LogoUrl { get; set; }

    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? AddressDetails { get; set; }
    public string? CoverageCities { get; set; } // مدن التغطية مثل: الرياض, جدة, الدمام

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? VerificationNotes { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public decimal Rating { get; set; } = 5.0m;
    public int TotalReviews { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<ContractorService> Services { get; set; } = new List<ContractorService>();
    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public virtual ICollection<ProjectContract> Contracts { get; set; } = new List<ProjectContract>();
    public virtual ICollection<ProjectReview> ReviewsReceived { get; set; } = new List<ProjectReview>();
    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
