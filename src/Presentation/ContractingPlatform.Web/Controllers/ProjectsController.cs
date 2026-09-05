using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ICurrentUserService _currentUserService;

    public ProjectsController(IProjectService projectService, ICurrentUserService currentUserService)
    {
        _projectService = projectService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId = null, string? city = null)
    {
        var projects = await _projectService.GetOpenProjectsAsync(categoryId, city);
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedCity = city;
        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        int? contractorId = null;
        if (User.IsInRole(nameof(UserType.Contractor)))
        {
            contractorId = await _currentUserService.GetContractorProfileIdAsync();
        }

        var result = await _projectService.GetProjectDetailsAsync(id, contractorId);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        return View(new CreateProjectDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> Create(CreateProjectDto dto)
    {
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        if (!ModelState.IsValid) return View(dto);

        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب العميل، يرجى إعادة تسجيل الدخول";
            return RedirectToAction("Login", "Account");
        }

        var result = await _projectService.CreateProjectAsync(dto, clientProfileId.Value);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        ModelState.AddModelError("", result.Message);
        return View(dto);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserType.Client))]
    public async Task<IActionResult> MyProjects()
    {
        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب العميل";
            return RedirectToAction("Index", "Home");
        }

        var projects = await _projectService.GetClientProjectsAsync(clientProfileId.Value);
        return View(projects);
    }
}
