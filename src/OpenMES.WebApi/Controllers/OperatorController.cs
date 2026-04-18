using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Shop floor operators.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class OperatorController(OpenMESDbContext dbContext, ILogger<OperatorController> logger)
	: RestApiControllerBase<Operator, OperatorDto, int>(dbContext, logger)
{
}
