using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

public record ApiResult : IApiResult
{
	public bool Success { get; init; }
	public string? ErrorMessage { get; init; }
	public int StatusCode { get; init; }

	public static IApiResult Ok(int statusCode = 200)
	 => new ApiResult { Success = true, StatusCode = statusCode };

	public static IApiResult Fail(string? message, int statusCode)
		=> new ApiResult { Success = false, ErrorMessage = message, StatusCode = statusCode };
}

public record ApiResult<T> : IApiResult<T>
{
	public bool Success { get; init; }
	public string? ErrorMessage { get; init; }
	public int StatusCode { get; init; }

	public T? Data { get; init; }

	public static IApiResult<T> Ok(T? data, int statusCode = 200)
		=> new ApiResult<T> { Success = true, Data = data, StatusCode = statusCode };

	public static IApiResult<T> Fail(string? message, int statusCode)
		=> new ApiResult<T> { Success = false, ErrorMessage = message, StatusCode = statusCode };


}
