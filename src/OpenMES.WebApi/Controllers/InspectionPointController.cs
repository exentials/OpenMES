using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Individual control points within an inspection plan.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class InspectionPointController(OpenMESDbContext dbContext, ILogger<InspectionPointController> logger)
	: RestApiControllerBase<InspectionPoint, InspectionPointDto, int>(dbContext, logger)
{
}
