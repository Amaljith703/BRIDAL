using System.Security.Claims;
using System.Text;
using MakeupArt.Api.Infrastructure;
using MakeupArt.Application.Common;
using MakeupArt.Application.Dtos.Auth;
using MakeupArt.Application.Mappings;
using MakeupArt.Domain.Identity;
using MakeupArt.Infrastructure.Options;
using MakeupArt.Infrastructure.Persistence;
using MakeupArt.Infrastructure.Security;
using MakeupArt.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MakeupArt.Api.Controllers;

[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    IEmailService emailService,
    IOptions<JwtOptions> jwtOptions,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<AuthController> logger) : BaseApiController
{
    private const string RefreshCookie = "ma_refresh";
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly FrontendOptions _frontend = frontendOptions.Value;

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw ApiException.Conflict("An account with this email already exists. Try signing in instead.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim(),
            PhoneNumber = request.Phone,
            MarketingOptIn = request.MarketingOptIn,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw ApiException.Validation(string.Join(" ", result.Errors.Select(x => x.Description)));

        await userManager.AddToRoleAsync(user, "Customer");
        await SendWelcomeAsync(user, cancellationToken);
        await SendEmailConfirmationAsync(user, cancellationToken);
        var (auth, _) = await IssueTokensAsync(user, cancellationToken);
        return auth;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            // Keep timing and wording deliberately generic.
            await Task.Delay(120, cancellationToken);
            throw ApiException.Unauthorized("Email or password is incorrect.");
        }

        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (check.IsLockedOut)
            throw ApiException.Forbidden("Too many attempts. This account is locked for 15 minutes.");
        if (!check.Succeeded)
            throw ApiException.Unauthorized("Email or password is incorrect.");

        if (await userManager.GetTwoFactorEnabledAsync(user) || user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                return new AuthResponse { RequiresTwoFactor = true, Email = email };

            var code = request.TwoFactorCode.Replace(" ", "").Replace("-", "");
            var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
            if (!valid)
            {
                var recovery = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
                valid = recovery.Succeeded;
            }
            if (!valid) throw ApiException.Unauthorized("That verification code is not valid.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var (auth, _) = await IssueTokensAsync(user, cancellationToken);
        return auth;
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var raw = Request.Cookies[RefreshCookie];
        if (string.IsNullOrWhiteSpace(raw))
            throw ApiException.Unauthorized("Your session has expired. Please sign in again.");

        var hash = TokenService.HashRefreshToken(raw);
        var token = await db.RefreshTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            DeleteRefreshCookie();
            throw ApiException.Unauthorized("Your session has expired. Please sign in again.");
        }

        if (token.RevokedAtUtc is not null)
        {
            // Refresh-token reuse is a strong signal of theft: revoke the whole family.
            var active = await db.RefreshTokens.Where(x => x.UserId == token.UserId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var item in active) item.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            DeleteRefreshCookie();
            logger.LogWarning("Refresh token reuse detected for user {UserId}; all sessions revoked.", token.UserId);
            throw ApiException.Unauthorized("Your session was revoked for security. Please sign in again.");
        }

        if (token.ExpiresAtUtc < DateTime.UtcNow || token.User is null || !token.User.IsActive || token.User.IsDeleted)
        {
            DeleteRefreshCookie();
            throw ApiException.Unauthorized("Your session has expired. Please sign in again.");
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (response, newRefreshToken) = await IssueTokensAsync(token.User, cancellationToken);
        token.ReplacedByTokenHash = TokenService.HashRefreshToken(newRefreshToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var raw = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var hash = TokenService.HashRefreshToken(raw);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
            if (token is not null && token.RevokedAtUtc is null)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                token.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        DeleteRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString())
                   ?? throw ApiException.Unauthorized();
        if (!user.IsActive || user.IsDeleted) throw ApiException.Unauthorized("This account is no longer active.");
        var roles = await userManager.GetRolesAsync(user);
        return user.ToDto(roles.ToList());
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DisplayName = $"{user.FirstName} {user.LastName}".Trim();
        user.PhoneNumber = request.Phone;
        user.ProfileImageUrl = request.ProfileImageUrl;
        user.MarketingOptIn = request.MarketingOptIn;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) throw ApiException.Validation(string.Join(" ", result.Errors.Select(x => x.Description)));
        var roles = await userManager.GetRolesAsync(user);
        return user.ToDto(roles.ToList());
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) throw ApiException.Validation(string.Join(" ", result.Errors.Select(x => x.Description)));
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is not null && user.IsActive)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = $"{_frontend.Origin.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email ?? "")}&token={Uri.EscapeDataString(token)}";
            await emailService.SendAsync(user.Email!, "Reset your Makeup Art password",
                $"""
                 <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
                   <h1 style="font-weight:400">Reset your password</h1>
                   <p>We received a request to reset the password for your Makeup Art account.</p>
                   <p><a href="{link}" style="display:inline-block;background:#8E4A5B;color:#fff;padding:12px 22px;border-radius:999px;text-decoration:none">Choose a new password</a></p>
                   <p style="font-size:13px;color:#7a666b">This link expires in 2 hours. If you did not ask for a reset, you can safely ignore this email.</p>
                 </div>
                 """, cancellationToken);
        }
        return Ok(new { message = "If an account exists for that email, a reset link is on its way." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant())
                   ?? throw ApiException.Validation("That reset link is invalid or has expired.");
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded) throw ApiException.Validation(string.Join(" ", result.Errors.Select(x => x.Description)));
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        return Ok(new { message = "Your password has been updated. You can sign in now." });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant())
                   ?? throw ApiException.Validation("That confirmation link is invalid or has expired.");
        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded) throw ApiException.Validation("That confirmation link is invalid or has expired.");
        return Ok(new { message = "Your email is confirmed. Thank you." });
    }

    [Authorize]
    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        if (!user.EmailConfirmed) await SendEmailConfirmationAsync(user, cancellationToken);
        return Ok(new { message = "If your email still needs confirming, a fresh link has been sent." });
    }

    [Authorize]
    [HttpGet("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupDto>> SetupTwoFactor(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }
        if (string.IsNullOrWhiteSpace(key)) throw ApiException.Validation("Could not create an authenticator key.");
        var issuer = Uri.EscapeDataString("Makeup Art");
        var account = Uri.EscapeDataString(user.Email ?? "account");
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6&period=30";
        return new TwoFactorSetupDto(FormatKey(key), uri);
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<ActionResult<object>> EnableTwoFactor(TwoFactorCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        if (!string.IsNullOrWhiteSpace(request.Password) && !await userManager.CheckPasswordAsync(user, request.Password))
            throw ApiException.Unauthorized("Your password is incorrect.");
        var code = request.Code.Replace(" ", "");
        var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code);
        if (!valid) throw ApiException.Validation("That authenticator code is not valid. Check your device clock and try again.");
        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        logger.LogInformation("Two-factor authentication enabled for {UserId}", user.Id);
        return Ok(new { message = "Two-factor authentication is on.", recoveryCodes = recoveryCodes?.ToList() ?? [] });
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor(TwoFactorCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Password) || !await userManager.CheckPasswordAsync(user, request.Password))
            throw ApiException.Unauthorized("Your password is incorrect.");
        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        logger.LogWarning("Two-factor authentication disabled for {UserId}", user.Id);
        return Ok(new { message = "Two-factor authentication is off." });
    }

    [Authorize]
    [HttpPost("2fa/recovery-codes")]
    public async Task<ActionResult<object>> RegenerateRecoveryCodes(TwoFactorCodeRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(RequireUserId().ToString()) ?? throw ApiException.Unauthorized();
        if (!user.TwoFactorEnabled) throw ApiException.Validation("Enable two-factor authentication first.");
        if (string.IsNullOrWhiteSpace(request.Password) || !await userManager.CheckPasswordAsync(user, request.Password))
            throw ApiException.Unauthorized("Your password is incorrect.");
        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return Ok(new { recoveryCodes = codes?.ToList() ?? [] });
    }

    private async Task<(AuthResponse Auth, string RefreshToken)> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var pair = tokenService.CreateTokenPair(user, roles);
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = pair.RefreshTokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(Math.Clamp(_jwt.RefreshTokenDays, 1, 60)),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        db.RefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(cancellationToken);

        Response.Cookies.Append(RefreshCookie, pair.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = refreshEntity.ExpiresAtUtc,
            Path = "/api/auth",
            IsEssential = true
        });

        return (new AuthResponse
        {
            AccessToken = pair.AccessToken,
            ExpiresAtUtc = pair.ExpiresAtUtc,
            User = user.ToDto(roles.ToList())
        }, pair.RefreshToken);
    }

    private async Task SendWelcomeAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await emailService.SendAsync(user.Email!, "Welcome to Makeup Art",
            $"""
             <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
               <h1 style="font-weight:400">Welcome, {user.FirstName}.</h1>
               <p>Your Makeup Art account is ready. Explore the studio edit, book a chair, or save your favourite looks for later.</p>
               <p><a href="{_frontend.Origin}" style="display:inline-block;background:#8E4A5B;color:#fff;padding:12px 22px;border-radius:999px;text-decoration:none">Visit the studio</a></p>
             </div>
             """, cancellationToken);
    }

    private async Task SendEmailConfirmationAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{_frontend.Origin.TrimEnd('/')}/confirm-email?email={Uri.EscapeDataString(user.Email ?? "")}&token={Uri.EscapeDataString(token)}";
        await emailService.SendAsync(user.Email!, "Confirm your Makeup Art email",
            $"""
             <div style="font-family:Georgia,serif;max-width:560px;margin:auto;color:#3d2a2f">
               <h1 style="font-weight:400">One small click</h1>
               <p>Please confirm your email so we can keep your account and order updates secure.</p>
               <p><a href="{link}" style="display:inline-block;background:#8E4A5B;color:#fff;padding:12px 22px;border-radius:999px;text-decoration:none">Confirm email</a></p>
             </div>
             """, cancellationToken);
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private void DeleteRefreshCookie()
        => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });

    private static string FormatKey(string key)
        => string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))));
}
