using Microsoft.AspNetCore.Identity;
using ContractingPlatform.Domain.Common;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Domain.Entities;

public class ApplicationUser : IdentityUser, IAuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    public virtual ClientProfile? ClientProfile { get; set; }
    public virtual ContractorProfile? ContractorProfile { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
    public virtual ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
}
