using System.Net.Http.Json;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

/// <summary>
/// Typed HTTP service for MachinePhasePlacement with domain-specific endpoints.
/// </summary>
public class MachinePhasePlacementService(HttpClient httpClient, string requestUri)
    : CrudApiService<MachinePhasePlacementDto, int>(httpClient, requestUri)
{
    /// <summary>GET /machinephaseplacement/machine/{machineId}/open — open placements for a machine.</summary>
    public async Task<IEnumerable<MachinePhasePlacementDto>> GetOpenByMachineAsync(int machineId, CancellationToken ct = default)
    {
        using var response = await HttpClientApi.GetAsync($"{RequestUriApi}/machine/{machineId}/open", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return [];
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<IEnumerable<MachinePhasePlacementDto>>(content) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>POST /machinephaseplacement/place — place a phase on machine.</summary>
    public async Task<MachinePhasePlacementDto?> PlaceAsync(MachinePhasePlacementDto dto, CancellationToken ct = default)
    {
        using var response = await HttpClientApi.PostAsJsonAsync($"{RequestUriApi}/place", dto, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<MachinePhasePlacementDto>(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>POST /machinephaseplacement/{id}/unplace — close an open placement.</summary>
    public async Task<MachinePhasePlacementDto?> UnplaceAsync(int id, CancellationToken ct = default)
    {
        using var response = await HttpClientApi.PostAsync($"{RequestUriApi}/{id}/unplace", content: null, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return IApiService.Deserialize<MachinePhasePlacementDto>(content);
        }
        catch
        {
            return null;
        }
    }
}
