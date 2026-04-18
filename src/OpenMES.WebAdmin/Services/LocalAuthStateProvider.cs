using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Services;

/// <summary>Login/logout contract for WebAdmin.</summary>
public interface ILoginService
{
    Task LoginAsync(AdminLoginResultDto result);
    Task LogoutAsync();
}

/// <summary>
/// Blazor Server AuthenticationStateProvider for WebAdmin.
/// Persists JWT in ProtectedLocalStorage (encrypted with ASP.NET Core Data Protection).
/// On each SignalR circuit reconnect:
///   1. Reads JWT from storage
///   2. Validates signature and expiration locally (without API round-trip)
///   3. Rebuilds ClaimsPrincipal with Name, Email, and Roles
///   4. Restores token into MesClient HttpClient
/// </summary>
public class LocalAuthStateProvider(
    IAdminAuthStorage                 storage,
    MesClient                         mesClient,
    IConfiguration                    configuration,
    ILogger<LocalAuthStateProvider>   logger)
    : AuthenticationStateProvider, ILoginService
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    // ── AuthenticationStateProvider ───────────────────────────────────────────

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var stored = await storage.GetAsync();
            if (stored is not null && !string.IsNullOrEmpty(stored.AuthToken))
            {
                var principal = ValidateAndBuildPrincipal(stored.AuthToken);
                if (principal is not null)
                {
                    mesClient.SetAuthToken(stored.AuthToken);
                    return new AuthenticationState(principal);
                }

                // Expired or tampered token: clear storage silently.
                await storage.DeleteAsync();
                mesClient.SetAuthToken(null);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Storage is unavailable during SSR prerender and can be ignored.
            logger.LogDebug(ex, "Storage unavailable during prerender.");

        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogWarning(ex, "Unexpected error in GetAuthenticationStateAsync.");
        }

        return new AuthenticationState(Anonymous);
    }

    // ── ILoginService ─────────────────────────────────────────────────────────

    public async Task LoginAsync(AdminLoginResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var principal = ValidateAndBuildPrincipal(result.AuthToken)
            ?? throw new InvalidOperationException("JWT received from server is invalid.");

        await storage.SetAsync(result);
        mesClient.SetAuthToken(result.AuthToken);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task LogoutAsync()
    {
        try { await storage.DeleteAsync(); }
        catch (Exception ex) { logger.LogWarning(ex, "Error while deleting auth storage."); }

        mesClient.SetAuthToken(null);
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(Anonymous)));
    }

    // ── Local JWT validation ──────────────────────────────────────────────────

    /// <summary>
    /// Validates JWT signature and expiration and builds ClaimsPrincipal.
    /// Returns null when token is expired, tampered, or unreadable.
    /// </summary>
    private ClaimsPrincipal? ValidateAndBuildPrincipal(string token)
    {
        try
        {
            var jwtSection = configuration.GetSection("Jwt");
            var secretKey  = jwtSection["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                // Fallback without signature validation: read claims and only check expiration.
                logger.LogWarning("JWT:SecretKey is not configured in WebAdmin. Signature validation is disabled.");
                return ReadClaimsWithoutSignatureValidation(token);
            }

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSection["Issuer"]   ?? "openmes-webapi",
                ValidAudience            = jwtSection["Audience"] ?? "openmes-webadmin",
                IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(secretKey)),
                NameClaimType            = ClaimTypes.Name,
                RoleClaimType            = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30),
            }, out _);

            // Rebuild identity with explicit authenticationType so IsAuthenticated is true.
            return WrapWithAuthenticationType(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT validation failed.");
            return null;
        }
    }

    private static ClaimsPrincipal? ReadClaimsWithoutSignatureValidation(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow) return null;
            return WrapWithAuthenticationType(new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims)));
        }
        catch { return null; }
    }

    /// <summary>
    /// Creates a new ClaimsPrincipal with authentication type "identity-bearer"
    /// while copying all claims from the original principal (including ClaimTypes.Role).
    /// Needed to ensure IsAuthenticated is true and role-based authorization works
    /// correctly in Blazor Server components.
    /// </summary>
    private static ClaimsPrincipal WrapWithAuthenticationType(ClaimsPrincipal original)
    {
        var identity = new ClaimsIdentity(original.Claims, "identity-bearer");
        return new ClaimsPrincipal(identity);
    }
}
