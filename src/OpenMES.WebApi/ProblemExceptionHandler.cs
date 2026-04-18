using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OpenMES.WebApi
{
	public class ProblemExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
	{
		private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

		public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
		{
			if (exception is ProblemException problemException)
			{
				var problemDetails = new ProblemDetails
				{
					Status = StatusCodes.Status400BadRequest,
					Title = problemException.Error,
					Detail = problemException.Message,
					Type = "Bad Request",
					//Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
				};

				httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
				return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
				{
					HttpContext = httpContext,
					ProblemDetails = problemDetails
				});
			}
			return false;
		}
	}
}
