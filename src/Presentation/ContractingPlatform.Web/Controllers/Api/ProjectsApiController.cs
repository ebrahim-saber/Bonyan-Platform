using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.Interfaces;

namespace ContractingPlatform.Web.Controllers.Api;

[ApiController]
[EnableRateLimiting("general-limit")]
[Route("api/v1/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsApiController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// استرجاع قائمة المشاريع المفتوحة والمتاحة لاستقبال العروض
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProjectCardDto>>>> GetOpenProjects([FromQuery] int? categoryId, [FromQuery] string? city)
    {
        var projects = await _projectService.GetOpenProjectsAsync(categoryId, city);
        return Ok(ApiResponse<List<ProjectCardDto>>.Ok(projects));
    }

    /// <summary>
    /// استرجاع تفاصيل مشروع معين وكافة العروض المقدمة عليه
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailsDto>>> GetProjectDetails(int id)
    {
        var result = await _projectService.GetProjectDetailsAsync(id, null, null, false);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// استرجاع تصنيفات المقاولات والخدمات المتاحة
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<object>>> GetCategories()
    {
        var categories = await _projectService.GetActiveCategoriesAsync();
        return Ok(ApiResponse<object>.Ok(categories));
    }
}
