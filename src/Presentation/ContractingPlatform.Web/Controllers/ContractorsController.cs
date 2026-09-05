using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.Interfaces;

namespace ContractingPlatform.Web.Controllers;

public class ContractorsController : Controller
{
    private readonly IContractorService _contractorService;
    private readonly IProjectService _projectService;

    public ContractorsController(IContractorService contractorService, IProjectService projectService)
    {
        _contractorService = contractorService;
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city, int? serviceId)
    {
        var contractors = await _contractorService.GetContractorsDirectoryAsync(city, serviceId);
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        ViewBag.SelectedCity = city;
        ViewBag.SelectedServiceId = serviceId;

        return View(contractors);
    }

    [HttpGet]
    public async Task<IActionResult> Profile(int id)
    {
        var profile = await _contractorService.GetPublicProfileAsync(id);
        if (profile == null)
        {
            TempData["ErrorMessage"] = "ملف المقاول غير موجود أو غير معتمد";
            return RedirectToAction(nameof(Index));
        }

        return View(profile);
    }
}
