using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application.DTOs.Common;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<int>> CreateProjectAsync(CreateProjectDto dto, int clientProfileId)
    {
        var category = await _context.Categories.FindAsync(dto.CategoryId);
        if (category == null)
        {
            return ApiResponse<int>.Fail("تصنيف المشروع غير موجود");
        }

        var project = new ProjectRequest
        {
            ClientProfileId = clientProfileId,
            CategoryId = dto.CategoryId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            City = dto.City.Trim(),
            District = dto.District.Trim(),
            DetailedAddress = dto.DetailedAddress,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ExpectedBudgetMin = dto.ExpectedBudgetMin,
            ExpectedBudgetMax = dto.ExpectedBudgetMax,
            DesiredExecutionDate = dto.DesiredExecutionDate,
            Status = ProjectStatus.OpenForBids
        };

        await _context.ProjectRequests.AddAsync(project);
        await _context.SaveChangesAsync();

        return ApiResponse<int>.Ok(project.Id, "تم إنشاء ونشر طلب المشروع بنجاح وهو الآن متاح لاستقبال عروض المقاولين");
    }

    public async Task<ApiResponse<ProjectDetailsDto>> GetProjectDetailsAsync(int projectId, int? currentContractorProfileId = null)
    {
        var project = await _context.ProjectRequests
            .Include(p => p.Category)
            .Include(p => p.Client).ThenInclude(c => c.User)
            .Include(p => p.Attachments)
            .Include(p => p.Bids).ThenInclude(b => b.Contractor)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        if (project == null)
        {
            return ApiResponse<ProjectDetailsDto>.Fail("طلب المشروع غير موجود");
        }

        // Increment Views
        project.ViewsCount++;
        await _context.SaveChangesAsync();

        var details = new ProjectDetailsDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            CategoryName = project.Category.NameAr,
            CategoryIcon = project.Category.IconCss ?? "bi-hammer",
            City = project.City,
            District = project.District,
            DetailedAddress = project.DetailedAddress,
            ExpectedBudgetMin = project.ExpectedBudgetMin,
            ExpectedBudgetMax = project.ExpectedBudgetMax,
            DesiredExecutionDate = project.DesiredExecutionDate,
            Status = project.Status,
            BidsCount = project.Bids.Count,
            CreatedAt = project.CreatedAt,
            ClientProfileId = project.ClientProfileId,
            ClientName = project.Client.User.FullName,
            AttachmentUrls = project.Attachments.Select(a => a.FilePath).ToList(),
            HasUserBid = currentContractorProfileId.HasValue && project.Bids.Any(b => b.ContractorProfileId == currentContractorProfileId.Value),
            Bids = project.Bids.OrderByDescending(b => b.SubmittedAt).Select(b => new BidListItemDto
            {
                Id = b.Id,
                ContractorProfileId = b.ContractorProfileId,
                ContractorCompanyName = b.Contractor.CompanyName,
                ContractorRating = b.Contractor.Rating,
                ContractorTotalReviews = b.Contractor.TotalReviews,
                ProposedPrice = b.ProposedPrice,
                DurationDays = b.DurationDays,
                Notes = b.Notes,
                MaterialCost = b.MaterialCost,
                LaborCost = b.LaborCost,
                Status = b.Status,
                SubmittedAt = b.SubmittedAt
            }).ToList()
        };

        return ApiResponse<ProjectDetailsDto>.Ok(details);
    }

    public async Task<List<ProjectCardDto>> GetOpenProjectsAsync(int? categoryId = null, string? city = null)
    {
        var query = _context.ProjectRequests
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Client).ThenInclude(c => c.User)
            .Where(p => !p.IsDeleted && p.Status == ProjectStatus.OpenForBids);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City.Contains(city));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectCardDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description.Length > 150 ? p.Description.Substring(0, 150) + "..." : p.Description,
                CategoryName = p.Category.NameAr,
                CategoryIcon = p.Category.IconCss ?? "bi-hammer",
                City = p.City,
                District = p.District,
                ExpectedBudgetMin = p.ExpectedBudgetMin,
                ExpectedBudgetMax = p.ExpectedBudgetMax,
                Status = p.Status,
                BidsCount = p.Bids.Count,
                CreatedAt = p.CreatedAt,
                ClientName = p.Client.User.FullName
            })
            .ToListAsync();
    }

    public async Task<List<ProjectCardDto>> GetClientProjectsAsync(int clientProfileId)
    {
        return await _context.ProjectRequests
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.ClientProfileId == clientProfileId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectCardDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description.Length > 150 ? p.Description.Substring(0, 150) + "..." : p.Description,
                CategoryName = p.Category.NameAr,
                CategoryIcon = p.Category.IconCss ?? "bi-hammer",
                City = p.City,
                District = p.District,
                ExpectedBudgetMin = p.ExpectedBudgetMin,
                ExpectedBudgetMax = p.ExpectedBudgetMax,
                Status = p.Status,
                BidsCount = p.Bids.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .Include(c => c.Services.Where(s => s.IsActive))
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<ServiceItem>> GetCategoryServicesAsync(int categoryId)
    {
        return await _context.ServiceItems
            .AsNoTracking()
            .Where(s => s.CategoryId == categoryId && s.IsActive && !s.IsDeleted)
            .ToListAsync();
    }
}
