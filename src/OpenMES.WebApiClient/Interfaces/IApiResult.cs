namespace OpenMES.WebApiClient.Interfaces;

public interface IApiResult
{
	bool Success { get; }
	string? ErrorMessage { get;  }
	int StatusCode { get; }
}

public interface IApiResult<T> : IApiResult
{
	public T? Data { get; }
}