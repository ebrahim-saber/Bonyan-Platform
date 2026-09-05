using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Infrastructure;
using ContractingPlatform.Infrastructure.Data;
using ContractingPlatform.Infrastructure.Hubs;
using ContractingPlatform.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Application & Infrastructure Services (Clean Architecture)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Global Anti-CSRF Protection & Controllers Configuration
builder.Services.AddControllersWithViews(options =>
{
    // Automatically enforce Anti-Forgery Validation on all POST, PUT, DELETE requests
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// 3. Configure Hardened Authentication Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true; // Prevent XSS extraction of auth cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Secure in HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // CSRF defense
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});

// 4. Rate Limiting Defense (Brute Force, Credential Stuffing & DoS Protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict limiter for authentication endpoints: max 5 requests per minute per IP
    options.AddPolicy("auth-limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = builder.Environment.IsDevelopment() ? 50 : 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // General limiter for public API endpoints: max 60 requests per minute per IP
    options.AddPolicy("general-limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// 5. Auto-Migrate & Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbSeeder.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "حدث خطأ أثناء تطبيق الترحيلات أو تهيئة البيانات الأولية لقاعدة البيانات.");
    }
}

// 6. Security Headers & Pipeline Hardening
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 7. Rate Limiter Middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 8. Map Endpoints & SignalR Hub
app.MapHub<PlatformHub>("/hubs/platform");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // RESTful Web APIs

app.Run();
