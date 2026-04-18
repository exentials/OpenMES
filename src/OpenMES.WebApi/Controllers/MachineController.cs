using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Machines assigned to work centers.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MachineController(OpenMESDbContext dbContext, ILogger<MachineController> logger)
	: RestApiControllerBase<Machine, MachineDto, int>(dbContext, logger)
{
	protected override IQueryable<Machine> Query => base.Query
		.Include(x => x.WorkCenter);
}
