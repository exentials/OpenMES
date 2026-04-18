using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenMES.Data.Contexts;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Base controller for all APIs. Requires JWT authentication by default.
/// Controllers that need a different scheme (for example TerminalController)
/// override it with [Authorize(AuthenticationSchemes = "...")] at class level.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]   // JWT is the default scheme configured in Program.cs authorization.
public abstract class ApiControllerBase(OpenMESDbContext dbContext, ILogger logger) : ControllerBase
{
    protected OpenMESDbContext DbContext { get; set; } = dbContext;
    protected ILogger Logger { get; set; } = logger;

    protected IActionResult InternalServerError<T>(T value) where T : class
    {
        return StatusCode((int)HttpStatusCode.InternalServerError, value);
    }
}
