using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;
using System.Net.Http.Json;

namespace OpenMES.WebApiClient;

public class TerminalService(HttpClient httpClient, string requestUri) : IApiService
{
	private string authToken = string.Empty;
	public HttpClient HttpClientApi => httpClient;
	public string RequestUriApi => requestUri;

	public void SetAuthToken(string token)
	{
		authToken = token;
		if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
		{
			httpClient.DefaultRequestHeaders.Remove("Authorization");
		}
		if (!string.IsNullOrWhiteSpace(authToken))
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
		}
	}

	public async Task<TerminalLoginResultDto> ConnectAsync(TerminalLoginDto data, CancellationToken cancellationToken = default)
	{
		using var response = await HttpClientApi.PostAsJsonAsync(RequestUriApi + "/connect", data, cancellationToken: cancellationToken);
		if (response.IsSuccessStatusCode)
		{
			var result = await response.Content.ReadFromJsonAsync<TerminalLoginResultDto>(cancellationToken: cancellationToken) ?? new TerminalLoginResultDto();
			SetAuthToken(result.AuthToken);
			return result;
		}
		else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
		{
			var message = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new Exception(message ?? "An error occurred while connecting the terminal.");
		}
		else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
		{
			var message = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new UnauthorizedAccessException(message ?? "Unauthorized access while connecting the terminal.");
		}
		else
		{
			response.EnsureSuccessStatusCode();
			return new TerminalLoginResultDto();
		}
	}

	public async Task<IEnumerable<MachineDto>> GetMachinesAsync(string terminal, CancellationToken cancellationToken = default)
	{
		using var response = await HttpClientApi.GetAsync($"{RequestUriApi}/machines/?terminal={terminal}", cancellationToken);
		if (response.IsSuccessStatusCode)
		{
			IEnumerable<MachineDto> result = await response.Content.ReadFromJsonAsync<IEnumerable<MachineDto>>(cancellationToken: cancellationToken) ?? [];
			return result;
		}
		else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
		{
			var message = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new Exception(message ?? "An error occurred while retrieving machines.");
		}
		else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
		{
			var message = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new UnauthorizedAccessException(message ?? "Unauthorized access while retrieving machines.");
		}
		else
		{
			response.EnsureSuccessStatusCode();
			return [];
		}
	}
}
