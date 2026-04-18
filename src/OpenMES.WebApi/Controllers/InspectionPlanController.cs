using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Inspection plans with their control points included.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class InspectionPlanController(OpenMESDbContext dbContext, ILogger<InspectionPlanController> logger)
	: RestApiControllerBase<InspectionPlan, InspectionPlanDto, int>(dbContext, logger)
{
	protected override IQueryable<InspectionPlan> Query => base.Query
		.Include(x => x.InspectionPoints);
}
