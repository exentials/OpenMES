using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Machine stop reason lookup table (breakdown causes, setup, etc.).</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MachineStopReasonController(OpenMESDbContext dbContext, ILogger<MachineStopReasonController> logger)
	: RestApiControllerBase<MachineStopReason, MachineStopReasonDto, int>(dbContext, logger)
{
}
