using System.ComponentModel.DataAnnotations;

namespace MakeupArt.Application.Dtos.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Phone { get; set; }

    public bool MarketingOptIn { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? TwoFactorCode { get; set; }

    public bool RememberMe { get; set; }
}

public record UserDto(Guid Id, string Email, string FirstName, string LastName, string DisplayName, string? Phone, string? ProfileImageUrl, IReadOnlyList<string> Roles, bool TwoFactorEnabled, bool IsActive, DateTime CreatedAtUtc, bool MarketingOptIn);

public class AuthResponse
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public UserDto? User { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? Email { get; set; }
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required, MinLength(10)] public string NewPassword { get; set; } = string.Empty;
}

public class ConfirmEmailRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(10)] public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, MaxLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string LastName { get; set; } = string.Empty;
    [Phone, MaxLength(30)] public string? Phone { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool MarketingOptIn { get; set; }
}

public record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);
public class TwoFactorCodeRequest
{
    [Required, StringLength(6, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    public string? Password { get; set; }
}
