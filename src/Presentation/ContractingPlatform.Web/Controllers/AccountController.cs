using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ContractingPlatform.Application.DTOs.Auth;
using ContractingPlatform.Application.Interfaces;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;
using ContractingPlatform.Infrastructure.Data;

namespace ContractingPlatform.Web.Controllers;

[EnableRateLimiting("auth-limit")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IProjectService _projectService;
    private readonly ISecurityAuditService _securityAuditService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IProjectService projectService,
        ISecurityAuditService securityAuditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _projectService = projectService;
        _securityAuditService = securityAuditService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(dto);

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null || !user.IsActive)
        {
            await _securityAuditService.LogSecurityEventAsync("LOGIN_ATTEMPT_UNKNOWN_USER", $"Login attempt for non-existent or inactive user: {dto.Email}", isSuspicious: true);
            ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة، أو تم تعطيل الحساب");
            return View(dto);
        }

        // Enforce Account Lockout Defense against Brute-Force & Credential Stuffing
        var result = await _signInManager.PasswordSignInAsync(user, dto.Password, dto.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _securityAuditService.LogSecurityEventAsync("LOGIN_SUCCESS", $"User {user.Email} logged in successfully", userId: user.Id);
            TempData["SuccessMessage"] = $"مرحباً بك مجدداً، {user.FullName}!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, nameof(UserType.Admin)))
                return RedirectToAction("Index", "Admin");
            if (await _userManager.IsInRoleAsync(user, nameof(UserType.Contractor)))
                return RedirectToAction("Index", "Projects");

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            await _securityAuditService.LogSecurityEventAsync("ACCOUNT_LOCKED_OUT", $"Account {user.Email} was locked out due to multiple failed login attempts", userId: user.Id, isSuspicious: true);
            ModelState.AddModelError("", "تم قفل الحساب مؤقتاً لمدة 15 دقيقة لتكرار المحاولات الخاطئة حرصاً على أمانك.");
            return View(dto);
        }

        await _securityAuditService.LogSecurityEventAsync("LOGIN_FAILED", $"Failed login attempt for {dto.Email}", userId: user.Id, isSuspicious: true);
        ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
        return View(dto);
    }

    [HttpGet]
    public IActionResult RegisterClient()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterClientDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterClient(RegisterClientDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "البريد الإلكتروني مسجل بالفعل");
            return View(dto);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            UserType = UserType.Client,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, nameof(UserType.Client));

            var clientProfile = new ClientProfile
            {
                UserId = user.Id,
                City = dto.City.Trim(),
                District = dto.District.Trim()
            };

            await _context.ClientProfiles.AddAsync(clientProfile);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "تم إنشاء حسابك بنجاح! يمكنك الآن طرح مشاريعك واستقبال العروض";
            return RedirectToAction("Index", "Projects");
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError("", err.Description);
        }

        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> RegisterContractor()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        return View(new RegisterContractorDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterContractor(RegisterContractorDto dto)
    {
        ViewBag.Categories = await _projectService.GetActiveCategoriesAsync();
        if (!ModelState.IsValid) return View(dto);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "البريد الإلكتروني مسجل بالفعل");
            return View(dto);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            UserType = UserType.Contractor,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, nameof(UserType.Contractor));

            var contractorProfile = new ContractorProfile
            {
                UserId = user.Id,
                CompanyName = dto.CompanyName.Trim(),
                CommercialRegistrationNo = dto.CommercialRegistrationNo.Trim(),
                TaxNumber = dto.TaxNumber?.Trim(),
                Bio = dto.Bio.Trim(),
                YearsOfExperience = dto.YearsOfExperience,
                City = dto.City.Trim(),
                District = dto.District.Trim(),
                CoverageCities = dto.CoverageCities?.Trim() ?? dto.City.Trim(),
                VerificationStatus = VerificationStatus.Pending // Under admin review
            };

            await _context.ContractorProfiles.AddAsync(contractorProfile);
            await _context.SaveChangesAsync();

            if (dto.SelectedServiceIds != null && dto.SelectedServiceIds.Any())
            {
                foreach (var serviceId in dto.SelectedServiceIds)
                {
                    await _context.ContractorServices.AddAsync(new ContractorService
                    {
                        ContractorProfileId = contractorProfile.Id,
                        ServiceItemId = serviceId
                    });
                }
                await _context.SaveChangesAsync();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "تم تسجيل منشأتك بنجاح! طلب التوثيق قيد المراجعة، ويمكنك الآن استعراض المشاريع وتقديم العروض";
            return RedirectToAction("Index", "Projects");
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError("", err.Description);
        }

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "تم تسجيل الخروج بنجاح";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
