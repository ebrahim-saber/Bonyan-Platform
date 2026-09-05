using ContractingPlatform.Domain.Common;

namespace ContractingPlatform.Domain.Entities;

public class ClientProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string? NationalIdOrIqama { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? AddressDetails { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Navigation Properties
    public virtual ICollection<ProjectRequest> ProjectRequests { get; set; } = new List<ProjectRequest>();
    public virtual ICollection<ProjectContract> Contracts { get; set; } = new List<ProjectContract>();
    public virtual ICollection<ProjectReview> ReviewsGiven { get; set; } = new List<ProjectReview>();
    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
