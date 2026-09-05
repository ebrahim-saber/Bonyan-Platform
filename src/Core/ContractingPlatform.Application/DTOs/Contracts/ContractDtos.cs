using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Contracts;

public class ContractDetailsDto
{
    public int Id { get; set; }
    public int ProjectRequestId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;

    public int ContractorProfileId { get; set; }
    public string ContractorCompanyName { get; set; } = string.Empty;
    public string ContractorPhone { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public decimal PlatformCommissionAmount { get; set; }
    public decimal ContractorNetAmount { get; set; }

    public ProjectStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public string? TermsAndConditions { get; set; }

    public List<MilestoneItemDto> Milestones { get; set; } = new();
    public bool HasReview { get; set; }
}

public class MilestoneItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int OrderIndex { get; set; }
    public MilestoneStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ContractorSubmissionNotes { get; set; }
    public string? ContractorProofAttachmentUrl { get; set; }
    public string? ClientApprovalNotes { get; set; }
}

public class SubmitMilestoneProofDto
{
    public int MilestoneId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}
