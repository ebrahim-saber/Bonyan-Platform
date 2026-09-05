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

        if (dto.Attachments != null && dto.Attachments.Any())
        {
            foreach (var att in dto.Attachments)
            {
                project.Attachments.Add(new ProjectAttachment
                {
                    FileName = att.FileName,
                    FilePath = att.FilePath,
                    ContentType = att.ContentType,
                    FileSizeBytes = att.FileSizeBytes
                });
            }
        }

        await _context.ProjectRequests.AddAsync(project);
        await _context.SaveChangesAsync();

        return ApiResponse<int>.Ok(project.Id, "تم إنشاء ونشر طلب المشروع بنجاح وهو الآن متاح لاستقبال عروض المقاولين");
    }

    public async Task<ApiResponse<ProjectDetailsDto>> GetProjectDetailsAsync(
        int projectId,
        string? currentUserId = null,
        int? currentContractorProfileId = null,
        bool isAdmin = false)
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

        // Commercial Privacy & Sealed Bids Protection:
        // 1. Project Owner & Admin: See all bids with full financial breakdowns.
        // 2. Competing Contractors: Only see their OWN bid in full. Other bids are masked to prevent price tampering & bid sniping.
        // 3. Guests/Unauthenticated: Bids are hidden; only total bids count is visible.
        bool isOwner = !string.IsNullOrEmpty(currentUserId) && project.Client?.User?.Id == currentUserId;
        bool canViewAllBids = isOwner || isAdmin;

        List<BidListItemDto> bidsList;

        if (canViewAllBids)
        {
            bidsList = project.Bids.OrderByDescending(b => b.SubmittedAt).Select(b => new BidListItemDto
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
            }).ToList();
        }
        else if (currentContractorProfileId.HasValue)
        {
            bidsList = project.Bids.OrderByDescending(b => b.SubmittedAt).Select(b =>
            {
                bool isMyBid = b.ContractorProfileId == currentContractorProfileId.Value;
                return new BidListItemDto
                {
                    Id = isMyBid ? b.Id : 0,
                    ContractorProfileId = isMyBid ? b.ContractorProfileId : 0,
                    ContractorCompanyName = isMyBid ? b.Contractor.CompanyName : "عرض منافس معتمد (عرض سري)",
                    ContractorRating = isMyBid ? b.Contractor.Rating : 5.0m,
                    ContractorTotalReviews = isMyBid ? b.Contractor.TotalReviews : 0,
                    ProposedPrice = isMyBid ? b.ProposedPrice : 0,
                    DurationDays = isMyBid ? b.DurationDays : 0,
                    Notes = isMyBid ? b.Notes : "تفاصيل الأسعار والعرض المالي سرية ومتاحة لصاحب المشروع فقط لضمان النزاهة التنافسية.",
                    MaterialCost = isMyBid ? b.MaterialCost : null,
                    LaborCost = isMyBid ? b.LaborCost : null,
                    Status = b.Status,
                    SubmittedAt = b.SubmittedAt
                };
            }).ToList();
        }
        else
        {
            // Anonymous visitors only see count of bids, not private pricing
            bidsList = new List<BidListItemDto>();
        }

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
            ClientName = project.Client?.User?.FullName ?? "عميل منصة بُنيان",
            AttachmentUrls = project.Attachments.Select(a => a.FilePath).ToList(),
            Attachments = project.Attachments.Select(a => new ProjectAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FilePath = a.FilePath,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes
            }).ToList(),
            HasUserBid = currentContractorProfileId.HasValue && project.Bids.Any(b => b.ContractorProfileId == currentContractorProfileId.Value),
            Bids = bidsList
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
