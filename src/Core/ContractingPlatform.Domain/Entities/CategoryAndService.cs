using ContractingPlatform.Domain.Common;

namespace ContractingPlatform.Domain.Entities;

public class Category : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? IconCss { get; set; } // e.g. "bi-tools", "bi-building"
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation Properties
    public virtual ICollection<ServiceItem> Services { get; set; } = new List<ServiceItem>();
    public virtual ICollection<ProjectRequest> Projects { get; set; } = new List<ProjectRequest>();
}

public class ServiceItem : BaseEntity
{
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<ContractorService> ContractorServices { get; set; } = new List<ContractorService>();
}

public class ContractorService : BaseEntity
{
    public int ContractorProfileId { get; set; }
    public virtual ContractorProfile ContractorProfile { get; set; } = null!;

    public int ServiceItemId { get; set; }
    public virtual ServiceItem ServiceItem { get; set; } = null!;
}
