using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ClarityBelongs.Web.Services;

public sealed record AccountSession(
    long UserId,
    long WorkspaceId,
    string Email,
    string DisplayName,
    string PlanCode,
    string MembershipStatus);

public sealed class CurrentAccountService(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    ClarityDbContext db)
{
    public async Task<AccountSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal is null || principal.Identity?.IsAuthenticated != true)
            principal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (principal.Identity?.IsAuthenticated != true)
            return null;

        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(rawUserId, out var userId))
            return null;

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
            return null;

        var workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OwnerUserId == userId, cancellationToken);

        if (workspace is null)
            return null;

        var membership = await db.Memberships
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return new AccountSession(
            user.Id,
            workspace.Id,
            user.Email,
            user.DisplayName,
            membership?.PlanCode ?? MembershipPlans.Free,
            membership?.Status ?? MembershipStatuses.Free);
    }

    public async Task<AccountSession> RequireAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("Sign in to continue.");
    }
}

public sealed class AccountService(
    ClarityDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IClarityEmailSender emailSender,
    IConfiguration configuration)
{
    public async Task<AppUser> CreateAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        ValidatePassword(password);

        if (await db.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
            throw new InvalidOperationException("An account already exists for that email address.");

        var user = new AppUser
        {
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? normalizedEmail.Split('@')[0]
                : displayName.Trim(),
            EmailVerified = false
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var workspace = new Workspace
        {
            OwnerUserId = user.Id,
            Name = "My Clarity"
        };

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        db.Memberships.Add(new Membership
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            PlanCode = MembershipPlans.Free,
            Status = MembershipStatuses.Free
        });

        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<AppUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return null;

        var result = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync(cancellationToken);
        }

        user.LastSeenAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return;

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(rawToken);

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        });

        await db.SaveChangesAsync(cancellationToken);

        if (!emailSender.IsEnabled)
            return;

        var root = string.IsNullOrWhiteSpace(baseUrl)
            ? configuration["PublicBaseUrl"] ?? "https://claritybelongs.com"
            : baseUrl.TrimEnd('/');

        var resetUrl = $"{root}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        await emailSender.SendAsync(
            user.Email,
            "Reset your Clarity Belongs password",
            $"Use this link within one hour to reset your password:\n\n{resetUrl}\n\nIf you did not request this, you can ignore this email.",
            cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(
        string rawToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(newPassword);
        var tokenHash = HashToken(rawToken);

        var token = await db.PasswordResetTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash
                    && x.UsedAtUtc == null
                    && x.ExpiresAtUtc > DateTime.UtcNow,
                cancellationToken);

        if (token is null)
            return false;

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Id == token.UserId, cancellationToken);

        if (user is null)
            return false;

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        token.UsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeEmail(string email)
    {
        var value = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(value)
            || !value.Contains('@')
            || value.Length > 254)
        {
            throw new InvalidOperationException("Enter a valid email address.");
        }

        return value;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new InvalidOperationException("Use a password with at least 10 characters.");

        if (password.Length > 128)
            throw new InvalidOperationException("Password is too long.");
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
