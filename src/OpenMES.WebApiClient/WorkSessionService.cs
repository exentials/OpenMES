using System.Net.Http.Json;
using System.Text.Json;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

/// <summary>
/// Typed HTTP service for WorkSession, extending standard CRUD with
/// domain-specific endpoints (open, close, pending-export).
/// </summary>
public class WorkSessionService(HttpClient httpClient, string requestUri)
    : CrudApiService<WorkSessionDto, int>(httpClient, requestUri)
{

    /// <summary>GET /worksession/open — all currently open sessions.</summary>
    public async Task<IEnumerable<WorkSessionDto>> GetOpenAsync(CancellationToken ct = default)
        => await GetListAsync($"{RequestUriApi}/open", ct);

    /// <summary>GET /worksession/pending-export — closed sessions not yet confirmed by ERP.</summary>
    public async Task<IEnumerable<WorkSessionDto>> GetPendingExportAsync(CancellationToken ct = default)
        => await GetListAsync($"{RequestUriApi}/pending-export", ct);

    /// <summary>POST /worksession/open — open a new session.</summary>
    public async Task<WorkSessionDto?> OpenSessionAsync(WorkSessionDto dto, CancellationToken ct = default)
    {
        using var response = await HttpClientApi.PostAsJsonAsync($"{RequestUriApi}/open", dto, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<WorkSessionDto>(content);
        }
        catch { return null; }
    }

    /// <summary>POST /worksession/{id}/close — close a session and reallocate minutes.</summary>
    public async Task<WorkSessionDto?> CloseSessionAsync(int id, CancellationToken ct = default)
    {
        using var response = await HttpClientApi.PostAsync($"{RequestUriApi}/{id}/close", null, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<WorkSessionDto>(content);
        }
        catch { return null; }
    }

    private async Task<IEnumerable<WorkSessionDto>> GetListAsync(string url, CancellationToken ct)
    {
        using var response = await HttpClientApi.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return [];
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<IEnumerable<WorkSessionDto>>(content) ?? [];
        }
        catch { return []; }
    }

}
