using System.Net.Http.Json;
using System.Text.Json;
using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient;

/// <summary>
/// Client for Identity user and role management (WebAdmin → WebApi).
/// All endpoints require the "admin" role.
/// </summary>
public class UserManagementService(HttpClient httpClient, string baseUri)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ── Users ─────────────────────────────────────────────────────────────────

    public async Task<PagedResponse<IdentityUserDto>> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, baseUri);
        request.Headers.Add("x-page",      $"{page}");
        request.Headers.Add("x-page-size", $"{pageSize}");

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResponse<IdentityUserDto>>(JsonOpts, ct)
               ?? new PagedResponse<IdentityUserDto> { PageNumber = page, PageSize = pageSize, TotalCount = 0, Items = [] };
    }

    public async Task<IdentityUserDto?> GetUserAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"{baseUri}/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IdentityUserDto>(JsonOpts, ct);
    }

    public async Task<IdentityUserDto> CreateUserAsync(
        CreateIdentityUserDto dto, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(baseUri, dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Creazione utente fallita ({(int)response.StatusCode}): {body}");
        }
        return await response.Content.ReadFromJsonAsync<IdentityUserDto>(JsonOpts, ct)
               ?? throw new InvalidOperationException("Risposta vuota dal server.");
    }

    public async Task UpdateUserAsync(
        string id, UpdateIdentityUserDto dto, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"{baseUri}/{id}", dto, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Aggiornamento utente fallito ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task DeleteUserAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"{baseUri}/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Eliminazione utente fallita ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task UnlockUserAsync(string id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"{baseUri}/{id}/unlock", null, ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    public async Task<List<IdentityRoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"{baseUri}/roles", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<IdentityRoleDto>>(JsonOpts, ct)
               ?? [];
    }
}
