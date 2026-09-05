using Microsoft.Extensions.Logging;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ApplicationDbContext context, ILogger<SecurityAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null, bool isSuspicious = false)
    {
        try
        {
            var log = new SecurityLog
            {
                EventType = eventType,
                Description = description,
                UserId = userId,
                IpAddress = ipAddress,
                IsSuspicious = isSuspicious,
                CreatedAt = DateTime.UtcNow
            };

            await _context.SecurityLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            if (isSuspicious)
            {
                _logger.LogWarning("[SECURITY WARNING] Type: {Type} | User: {UserId} | IP: {IP} | Description: {Desc}",
                    eventType, userId ?? "Anonymous", ipAddress ?? "Unknown", description);
            }
            else
            {
                _logger.LogInformation("[SECURITY AUDIT] Type: {Type} | User: {UserId} | Description: {Desc}",
                    eventType, userId ?? "System", description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist security audit log entry.");
        }
    }
}
