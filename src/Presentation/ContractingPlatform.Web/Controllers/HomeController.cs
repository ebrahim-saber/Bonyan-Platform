using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Web.Models;

namespace ContractingPlatform.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProjectService _projectService;

    public HomeController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _projectService.GetActiveCategoriesAsync();
        var latestProjects = await _projectService.GetOpenProjectsAsync();
        ViewBag.Categories = categories;
        ViewBag.LatestProjects = latestProjects.Take(6).ToList();
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
