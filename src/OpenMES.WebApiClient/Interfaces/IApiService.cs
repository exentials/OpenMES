using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenMES.WebApiClient.Interfaces;

/// <summary>
/// Defines the contract for an API service.
/// </summary>
/// <remarks>Implement this interface to provide API-related operations. The specific methods and behaviors are
/// determined by the implementing class.</remarks>
public interface IApiService
{
	HttpClient HttpClientApi { get; }
	string RequestUriApi { get; }

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() },
	};

	static async Task<string?> ReadContentSafeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.Content is null) return null;
		try { return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
		catch { return null; }
	}

	static T? Deserialize<T>(string? content)
	{
		if (content is null) return default;
		try { return JsonSerializer.Deserialize<T>(content, JsonOptions); } catch { return default; }
	}

	/// <summary>
	/// Extracts a human-readable error message from an API error response.
	/// Tries to parse ProblemDetails JSON (RFC 7807) and return the 'detail'
	/// field, falling back to 'title', then the raw content string.
	/// </summary>
	static string? ExtractErrorMessage(string? content)
	{
		if (string.IsNullOrWhiteSpace(content)) return null;
		try
		{
			using var doc = JsonDocument.Parse(content);
			var root = doc.RootElement;

			if (root.TryGetProperty("detail", out var detail) &&
				detail.ValueKind == JsonValueKind.String)
				return detail.GetString();

			if (root.TryGetProperty("title", out var title) &&
				title.ValueKind == JsonValueKind.String)
				return title.GetString();
		}
		catch { /* not JSON — fall through */ }

		return content;
	}
}
