using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Admin;

public class AdminFinancialDashboardDto
{
    public decimal TotalContractVolume { get; set; }
    public decimal TotalHeldInEscrow { get; set; }
    public decimal TotalReleasedToContractors { get; set; }
    public decimal TotalPlatformCommission { get; set; }
    public int ActiveEscrowContractsCount { get; set; }
    public int TotalTransactionsCount { get; set; }

    public List<AdminEscrowTransactionDto> RecentTransactions { get; set; } = new();
}

public class AdminEscrowTransactionDto
{
    public int Id { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public int ContractId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string MilestoneTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ContractorCompanyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EscrowLockedAt { get; set; }
    public DateTime? EscrowReleasedAt { get; set; }
}

public class PlatformStatisticsDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int OpenProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int TotalContractors { get; set; }
    public int ApprovedContractors { get; set; }
    public int PendingContractors { get; set; }
    public int TotalClients { get; set; }
    public int TotalBids { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalPlatformCommission { get; set; }
}
