using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Machine stop events with denormalized machine and reason data.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MachineStopController(OpenMESDbContext dbContext, ILogger<MachineStopController> logger)
	: RestApiControllerBase<MachineStop, MachineStopDto, int>(dbContext, logger)
{
	protected override IQueryable<MachineStop> Query => base.Query
		.Include(x => x.Machine)
		.Include(x => x.MachineStopReason);
}
