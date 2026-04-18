using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

public class ApiService(HttpClient httpClient, string requestUriApi) : IApiService
{
	public HttpClient HttpClientApi => httpClient;
	public string RequestUriApi => requestUriApi;

	//protected static async Task<string?> ReadContentSafeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	//{
	//	if (response.Content is null) return null;
	//	try { return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
	//	catch { return null; }
	//}
}
