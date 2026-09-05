using ContractingPlatform.Domain.Common;

namespace ContractingPlatform.Domain.Entities;

public class SecurityLog : BaseEntity
{
    public string EventType { get; set; } = string.Empty; // e.g. "LOGIN_FAILED", "IDOR_ATTEMPT", "ESCROW_RELEASE", "RATE_LIMIT_HIT"
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public bool IsSuspicious { get; set; } = false;
}
