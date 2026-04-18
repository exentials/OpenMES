using System.Net.Http.Json;
using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient;

/// <summary>
/// Wraps the AdminAuthController POST /api/admin/login endpoint.
/// Returns a standard signed JWT with roles included in claims.
/// </summary>
public class IdentityService(HttpClient httpClient, string baseUri)
{
    /// <summary>
    /// Authenticates an admin user. Returns <see cref="AdminLoginResultDto"/> with JWT and roles.
    /// Throws <see cref="UnauthorizedAccessException"/> when credentials are invalid.
    /// </summary>
    public async Task<AdminLoginResultDto> LoginAsync(
        AdminLoginDto dto, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(baseUri, dto, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content
                .ReadFromJsonAsync<AdminLoginResultDto>(ct)
                ?? throw new InvalidOperationException("Empty response from /api/admin/login.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"Login failed ({(int)response.StatusCode}): {body}");
    }
}
