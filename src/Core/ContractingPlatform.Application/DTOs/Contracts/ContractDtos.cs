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
    public string? TransactionReference { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
}

public class SubmitMilestoneProofDto
{
    public int MilestoneId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

public class PrintableContractDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int ProjectRequestId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? DetailedAddress { get; set; }

    // First Party (Client / Owner)
    public string ClientFullName { get; set; } = string.Empty;
    public string? ClientNationalIdOrIqama { get; set; }
    public string ClientPhone { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;

    // Second Party (Contractor)
    public string ContractorCompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNo { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string ContractorRepresentativeName { get; set; } = string.Empty;
    public string ContractorPhone { get; set; } = string.Empty;
    public string ContractorEmail { get; set; } = string.Empty;
    public bool IsContractorVerified { get; set; }

    // Financials & Schedule
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; } // 15% VAT
    public decimal TotalWithTax { get; set; }
    public decimal PlatformCommissionAmount { get; set; }
    public decimal ContractorNetAmount { get; set; }
    public DateTime ContractDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public int TotalDurationDays { get; set; }
    public ProjectStatus Status { get; set; }

    // Milestones
    public List<PrintableMilestoneDto> Milestones { get; set; } = new();

    // Legal Terms & Verification
    public string? TermsAndConditions { get; set; }
    public string VerificationSealCode { get; set; } = string.Empty;
    public string EscrowGuaranteeReference { get; set; } = string.Empty;
}

public class PrintableMilestoneDto
{
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
    public MilestoneStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsFundedInEscrow { get; set; }
}

