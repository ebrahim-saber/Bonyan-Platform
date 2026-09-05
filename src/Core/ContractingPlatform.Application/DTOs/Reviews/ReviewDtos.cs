namespace ContractingPlatform.Application.DTOs.Reviews;

public class CreateReviewDto
{
    public int ProjectContractId { get; set; }
    public int OverallRating { get; set; }
    public int QualityRating { get; set; }
    public int PunctualityRating { get; set; }
    public int CommunicationRating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewItemDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int OverallRating { get; set; }
    public int QualityRating { get; set; }
    public int PunctualityRating { get; set; }
    public int CommunicationRating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
