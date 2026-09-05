using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Infrastructure.Data;
using ContractingPlatform.Infrastructure.Services;

namespace ContractingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=ContractingPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddSignalR();

        // Register Application Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IBidService, BidService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
