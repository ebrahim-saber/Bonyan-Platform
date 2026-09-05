using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Payments;

public class CheckoutInitiationDto
{
    public int ProjectContractId { get; set; }
    public int? MilestoneId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string MilestoneTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ContractorCompanyName { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string EscrowProtectionNote { get; set; } = string.Empty;
}

public class ProcessPaymentDto
{
    public int ProjectContractId { get; set; }
    public int? MilestoneId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Mada;
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string? OtpCode { get; set; }
}

public class PaymentReceiptDto
{
    public string TransactionReference { get; set; } = string.Empty;
    public int ProjectContractId { get; set; }
    public int? MilestoneId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string MilestoneTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ContractorCompanyName { get; set; } = string.Empty;
    public string ContractorCrNumber { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ContractorNetAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EscrowLockedAt { get; set; }
    public DateTime? EscrowReleasedAt { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
}

public class ContractFinancialSummaryDto
{
    public decimal TotalContractValue { get; set; }
    public decimal TotalInEscrow { get; set; }
    public decimal TotalReleasedToContractor { get; set; }
    public decimal TotalRemainingUnfunded { get; set; }
}
