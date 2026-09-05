using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Web.Controllers;

[Authorize(Roles = nameof(UserType.Admin))]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ApplicationDbContext _context;

    public AdminController(IAdminService adminService, ApplicationDbContext context)
    {
        _adminService = adminService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stats = await _adminService.GetPlatformStatisticsAsync();
        ViewBag.Stats = stats;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Contractors()
    {
        var contractors = await _context.ContractorProfiles
            .Include(c => c.User)
            .Include(c => c.Services).ThenInclude(s => s.ServiceItem)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(contractors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateContractorStatus(int id, VerificationStatus status, string? notes)
    {
        var result = await _adminService.UpdateContractorStatusAsync(id, status, notes);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Contractors));
    }

    [HttpGet]
    public async Task<IActionResult> Projects()
    {
        var projects = await _context.ProjectRequests
            .Include(p => p.Category)
            .Include(p => p.Client).ThenInclude(c => c.User)
            .Include(p => p.Bids)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(projects);
    }
}
