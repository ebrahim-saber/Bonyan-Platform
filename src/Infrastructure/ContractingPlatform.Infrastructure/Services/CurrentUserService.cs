using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public async Task<int?> GetClientProfileIdAsync()
    {
        if (string.IsNullOrEmpty(UserId)) return null;
        var profile = await _context.ClientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == UserId);
        return profile?.Id;
    }

    public async Task<int?> GetContractorProfileIdAsync()
    {
        if (string.IsNullOrEmpty(UserId)) return null;
        var profile = await _context.ContractorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == UserId);
        return profile?.Id;
    }
}
