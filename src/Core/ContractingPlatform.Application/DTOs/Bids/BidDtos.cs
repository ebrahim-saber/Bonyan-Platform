using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Bids;

public class CreateBidDto
{
    public int ProjectRequestId { get; set; }
    public decimal ProposedPrice { get; set; }
    public int DurationDays { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
}

public class AcceptBidDto
{
    public int BidId { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<CreateMilestoneInputDto> Milestones { get; set; } = new();
}

public class CreateMilestoneInputDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int OrderIndex { get; set; }
    public DateTime? DueDate { get; set; }
}
