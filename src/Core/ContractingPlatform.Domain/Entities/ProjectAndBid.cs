using ContractingPlatform.Domain.Common;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Domain.Entities;

public class ProjectRequest : BaseEntity
{
    public int ClientProfileId { get; set; }
    public virtual ClientProfile Client { get; set; } = null!;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DetailedAddress { get; set; }

    public decimal? ExpectedBudgetMin { get; set; }
    public decimal? ExpectedBudgetMax { get; set; }
    public DateTime? DesiredExecutionDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.OpenForBids;
    public int BidsCount { get; set; } = 0;
    public int ViewsCount { get; set; } = 0;

    // Navigation Properties
    public virtual ICollection<ProjectAttachment> Attachments { get; set; } = new List<ProjectAttachment>();
    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public virtual ProjectContract? Contract { get; set; }
}

public class ProjectAttachment : BaseEntity
{
    public int ProjectRequestId { get; set; }
    public virtual ProjectRequest ProjectRequest { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class Bid : BaseEntity
{
    public int ProjectRequestId { get; set; }
    public virtual ProjectRequest ProjectRequest { get; set; } = null!;

    public int ContractorProfileId { get; set; }
    public virtual ContractorProfile Contractor { get; set; } = null!;

    public decimal ProposedPrice { get; set; }
    public int DurationDays { get; set; }
    public string Notes { get; set; } = string.Empty;

    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }

    public BidStatus Status { get; set; } = BidStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public virtual ProjectContract? Contract { get; set; }
}
