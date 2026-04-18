using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Quality inspection readings taken during production phases.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class InspectionReadingController(OpenMESDbContext dbContext, ILogger<InspectionReadingController> logger)
	: RestApiControllerBase<InspectionReading, InspectionReadingDto, int>(dbContext, logger)
{
	protected override IQueryable<InspectionReading> Query => base.Query
		.Include(x => x.InspectionPoint)
		.Include(x => x.ProductionOrderPhase)
		.Include(x => x.Operator);
}
