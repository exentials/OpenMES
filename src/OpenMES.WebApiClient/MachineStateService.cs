using System.Text.Json;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

/// <summary>
/// Typed HTTP service for MachineState, extending standard CRUD with
/// domain-specific endpoints (current per machine, current all).
/// </summary>
public class MachineStateService(HttpClient httpClient, string requestUri)
    : CrudApiService<MachineStateDto, int>(httpClient, requestUri)
{
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>GET /machinestate/current/all — current state of every machine.</summary>
    public async Task<IEnumerable<MachineStateDto>> GetAllCurrentAsync(CancellationToken ct = default)
    {
        using var response = await HttpClientApi.GetAsync($"{RequestUriApi}/current/all", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return [];
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<IEnumerable<MachineStateDto>>(content, _json) ?? [];
        }
        catch { return []; }
    }
}
