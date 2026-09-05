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
    private readonly IFileStorageService _fileStorageService;

    public ProjectsController(
        IProjectService projectService, 
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _projectService = projectService;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
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

        var result = await _projectService.GetProjectDetailsAsync(id, _currentUserService.UserId, contractorId, User.IsInRole(nameof(UserType.Admin)));
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
    public async Task<IActionResult> Create(CreateProjectDto dto, List<IFormFile>? attachments)
    {
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        if (!ModelState.IsValid) return View(dto);

        var clientProfileId = await _currentUserService.GetClientProfileIdAsync();
        if (!clientProfileId.HasValue)
        {
            TempData["ErrorMessage"] = "تعذر تحديد حساب العميل، يرجى إعادة تسجيل الدخول";
            return RedirectToAction("Login", "Account");
        }

        // Process engineering blueprints & site photos
        if (attachments != null && attachments.Count > 0)
        {
            foreach (var file in attachments)
            {
                if (file.Length == 0) continue;

                if (!_fileStorageService.IsAllowedExtension(file.FileName))
                {
                    ModelState.AddModelError("", $"نوع الملف '{file.FileName}' غير مسموح به. الامتدادات المدعومة: PDF, DWG, DXF, JPG, PNG, WEBP");
                    return View(dto);
                }

                if (!_fileStorageService.IsAllowedFileSize(file.Length))
                {
                    ModelState.AddModelError("", $"حجم الملف '{file.FileName}' كبير جداً. الحد الأقصى هو 20 ميجابايت");
                    return View(dto);
                }

                try
                {
                    using var stream = file.OpenReadStream();
                    var savedResult = await _fileStorageService.SaveFileAsync(
                        stream, 
                        file.FileName, 
                        file.ContentType, 
                        "projects");

                    dto.Attachments.Add(savedResult);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"فشل رفع الملف '{file.FileName}': {ex.Message}");
                    return View(dto);
                }
            }
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
