using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Application.DTOs.Auth;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public class RegisterClientDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
}

public class RegisterContractorDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNo { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string CoverageCities { get; set; } = string.Empty;
    public List<int> SelectedServiceIds { get; set; } = new();
}

public class AuthResultDto
{
    public bool Succeeded { get; set; }
    public string? Token { get; set; } // JWT Token if consumed by API / Mobile app
    public string? UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public UserType UserType { get; set; }
    public List<string> Errors { get; set; } = new();
}
