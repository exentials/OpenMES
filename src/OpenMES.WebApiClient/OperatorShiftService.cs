using System.Text.Json;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

/// <summary>
/// Typed HTTP service for OperatorShift, extending standard CRUD with
/// presence-specific endpoint(s).
/// </summary>
public class OperatorShiftService(HttpClient httpClient, string requestUri)
	: CrudApiService<OperatorShiftDto, int>(httpClient, requestUri)
{
	private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };


	/// <summary>
	/// GET /operatorshift/operator/{operatorId}/current — latest shift event for one operator.
	/// Returns null if the operator has no shift history.
	/// </summary>
	public async Task<OperatorShiftDto?> GetCurrentStatusAsync(int operatorId, CancellationToken ct = default)
	{
		using var response = await HttpClientApi
			.GetAsync($"{RequestUriApi}/operator/{operatorId}/current", ct)
			.ConfigureAwait(false);
		if (!response.IsSuccessStatusCode) return null;
		try
		{
			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return IApiService.Deserialize<OperatorShiftDto?>(content);
		}
		catch { return null; }
	}

	/// <summary>GET /operatorshift/present — operators currently present.</summary>
	public async Task<IEnumerable<OperatorDto>> GetPresentAsync(int? plantId = null, CancellationToken ct = default)
	{
		var query = plantId.HasValue ? $"?plantId={plantId.Value}" : string.Empty;
		using var response = await HttpClientApi.GetAsync($"{RequestUriApi}/present{query}", ct).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode) return [];
		try
		{
			var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return IApiService.Deserialize<IEnumerable<OperatorDto>>(content) ?? [];
		}
		catch
		{
			return [];
		}
	}

}
