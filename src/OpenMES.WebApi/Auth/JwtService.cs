using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace OpenMES.WebApi.Auth;

public interface IJwtService
{
    string GenerateToken(IdentityUser user, IList<string> roles);
}

/// <summary>
/// Generates an HMAC-SHA256 signed JWT containing user claims and roles.
/// Key, issuer, audience, and expiration are read from the "Jwt" appsettings section.
/// </summary>
public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(IdentityUser user, IList<string> roles)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secretKey  = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT:SecretKey non configurata in appsettings.");
        var issuer     = jwtSection["Issuer"]   ?? "openmes-webapi";
        var audience   = jwtSection["Audience"] ?? "openmes-webadmin";
        var expMinutes = jwtSection.GetValue<int>("ExpirationMinutes", 480);

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Name,               user.Email ?? string.Empty),
        };

        // Add roles as standard plus normalized and compact claim variants
        // to avoid claim-type/casing mismatches in [Authorize(Roles = "...")].
        var emittedClaims = new HashSet<(string Type, string Value)>();
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            var normalizedRole = NormalizeRole(role);
            AddRoleClaim(claims, emittedClaims, ClaimTypes.Role, role);
            AddRoleClaim(claims, emittedClaims, ClaimTypes.Role, normalizedRole);
            AddRoleClaim(claims, emittedClaims, "role", role);
            AddRoleClaim(claims, emittedClaims, "role", normalizedRole);
        }

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddRoleClaim(List<Claim> claims, HashSet<(string Type, string Value)> emittedClaims, string type, string value)
    {
        var key = (type, value);
        if (emittedClaims.Add(key))
        {
            claims.Add(new Claim(type, value));
        }
    }

    private static string NormalizeRole(string role)
    {
        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
        if (role.Equals("user", StringComparison.OrdinalIgnoreCase)) return "User";
        return role;
    }
}
