using System.Net.Http.Json;
using OpenMES.Data.Dtos;

namespace OpenMES.WebApiClient;

public class ErpExportService(HttpClient httpClient)
{
    private readonly HttpClient _http = httpClient;

    public async Task<ErpExportResultDto?> ExportWorkSessionsAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/erpexport/worksession", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ErpExportResultDto>(cancellationToken: ct);
    }

    public async Task<ErpExportResultDto?> ExportDeclarationsAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/erpexport/productiondeclaration", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ErpExportResultDto>(cancellationToken: ct);
    }
}
