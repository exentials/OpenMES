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

    /// <summary>POST /machinephaseplacement/{id}/start-setup — start setup for a placement.</summary>
    public Task<MachinePhasePlacementDto?> StartSetupAsync(int id, int? operatorId = null, CancellationToken ct = default)
        => PostTransitionAsync($"{RequestUriApi}/{id}/start-setup", operatorId, ct);

    /// <summary>POST /machinephaseplacement/{id}/pause-setup — pause setup for a placement.</summary>
    public Task<MachinePhasePlacementDto?> PauseSetupAsync(int id, CancellationToken ct = default)
        => PostNoBodyAsync($"{RequestUriApi}/{id}/pause-setup", ct);

    /// <summary>POST /machinephaseplacement/{id}/resume-setup — resume setup for a placement.</summary>
    public Task<MachinePhasePlacementDto?> ResumeSetupAsync(int id, int? operatorId = null, CancellationToken ct = default)
        => PostTransitionAsync($"{RequestUriApi}/{id}/resume-setup", operatorId, ct);

    /// <summary>POST /machinephaseplacement/{id}/start-work — start work for a placement.</summary>
    public Task<MachinePhasePlacementDto?> StartWorkAsync(int id, int? operatorId = null, CancellationToken ct = default)
        => PostTransitionAsync($"{RequestUriApi}/{id}/start-work", operatorId, ct);

    /// <summary>POST /machinephaseplacement/{id}/pause-work — pause work for a placement.</summary>
    public Task<MachinePhasePlacementDto?> PauseWorkAsync(int id, CancellationToken ct = default)
        => PostNoBodyAsync($"{RequestUriApi}/{id}/pause-work", ct);

    /// <summary>POST /machinephaseplacement/{id}/resume-work — resume work for a placement.</summary>
    public Task<MachinePhasePlacementDto?> ResumeWorkAsync(int id, int? operatorId = null, CancellationToken ct = default)
        => PostTransitionAsync($"{RequestUriApi}/{id}/resume-work", operatorId, ct);

    /// <summary>POST /machinephaseplacement/{id}/close — close a placement.</summary>
    public Task<MachinePhasePlacementDto?> CloseAsync(int id, CancellationToken ct = default)
        => PostNoBodyAsync($"{RequestUriApi}/{id}/close", ct);

    private Task<MachinePhasePlacementDto?> PostTransitionAsync(string baseUrl, int? operatorId, CancellationToken ct)
    {
        var url = operatorId.HasValue ? $"{baseUrl}?operatorId={operatorId.Value}" : baseUrl;
        return PostNoBodyAsync(url, ct);
    }

    private async Task<MachinePhasePlacementDto?> PostNoBodyAsync(string url, CancellationToken ct)
    {
        using var response = await HttpClientApi.PostAsync(url, content: null, ct).ConfigureAwait(false);
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
