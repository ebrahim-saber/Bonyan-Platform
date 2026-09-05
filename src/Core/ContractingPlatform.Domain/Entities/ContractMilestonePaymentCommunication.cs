using ContractingPlatform.Domain.Common;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Domain.Entities;

public class ProjectContract : BaseEntity
{
    public int ProjectRequestId { get; set; }
    public virtual ProjectRequest ProjectRequest { get; set; } = null!;

    public int AcceptedBidId { get; set; }
    public virtual Bid AcceptedBid { get; set; } = null!;

    public int ClientProfileId { get; set; }
    public virtual ClientProfile Client { get; set; } = null!;

    public int ContractorProfileId { get; set; }
    public virtual ContractorProfile Contractor { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public decimal PlatformCommissionPercentage { get; set; } = 5.0m; // Default 5%
    public decimal PlatformCommissionAmount { get; set; }
    public decimal ContractorNetAmount { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.InProgress;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedEndDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public string? TermsAndConditions { get; set; }

    // Navigation Properties
    public virtual ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    public virtual ProjectReview? Review { get; set; }
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

public class ProjectMilestone : BaseEntity
{
    public int ProjectContractId { get; set; }
    public virtual ProjectContract Contract { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int OrderIndex { get; set; } = 1;

    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ContractorSubmissionNotes { get; set; }
    public string? ContractorProofAttachmentUrl { get; set; }
    public string? ClientApprovalNotes { get; set; }

    // Navigation Property
    public virtual PaymentTransaction? Transaction { get; set; }
}

public class PaymentTransaction : BaseEntity
{
    public int ProjectContractId { get; set; }
    public virtual ProjectContract Contract { get; set; } = null!;

    public int? MilestoneId { get; set; }
    public virtual ProjectMilestone? Milestone { get; set; }

    public int ClientProfileId { get; set; }
    public virtual ClientProfile Client { get; set; } = null!;

    public int ContractorProfileId { get; set; }
    public virtual ContractorProfile Contractor { get; set; } = null!;

    public decimal Amount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetAmount { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Mada;
    public string TransactionReference { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? EscrowLockedAt { get; set; }
    public DateTime? EscrowReleasedAt { get; set; }
}

public class ProjectReview : BaseEntity
{
    public int ProjectContractId { get; set; }
    public virtual ProjectContract Contract { get; set; } = null!;

    public int ClientProfileId { get; set; }
    public virtual ClientProfile Client { get; set; } = null!;

    public int ContractorProfileId { get; set; }
    public virtual ContractorProfile Contractor { get; set; } = null!;

    public int OverallRating { get; set; } // 1 - 5
    public int QualityRating { get; set; } // 1 - 5
    public int PunctualityRating { get; set; } // 1 - 5 (الالتزام بالوقت)
    public int CommunicationRating { get; set; } // 1 - 5 (التواصل)

    public string? Comment { get; set; }
}

public class ChatMessage : BaseEntity
{
    public int? ProjectContractId { get; set; }
    public virtual ProjectContract? Contract { get; set; }

    public string SenderUserId { get; set; } = string.Empty;
    public virtual ApplicationUser Sender { get; set; } = null!;

    public string ReceiverUserId { get; set; } = string.Empty;
    public virtual ApplicationUser Receiver { get; set; } = null!;

    public string MessageText { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
