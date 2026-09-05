using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Application;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Infrastructure;
using ContractingPlatform.Infrastructure.Data;
using ContractingPlatform.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Application & Infrastructure Services (Clean Architecture)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Configure MVC & Web API Controllers
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 3. Configure Authentication Cookie Paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// 4. Auto-Migrate & Seed Database on Startup
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

// 5. Configure HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 6. Map Endpoints & SignalR Hub
app.MapHub<PlatformHub>("/hubs/platform");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // For RESTful Web API

app.Run();
