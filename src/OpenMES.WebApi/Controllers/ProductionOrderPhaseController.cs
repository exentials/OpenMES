using Microsoft.AspNetCore.Authorization;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Individual phases within a production order.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class ProductionOrderPhaseController(OpenMESDbContext dbContext, ILogger<ProductionOrderPhaseController> logger)
	: RestApiControllerBase<ProductionOrderPhase, ProductionOrderPhaseDto, int>(dbContext, logger)
{
	protected override IQueryable<ProductionOrderPhase> Query => base.Query
		.OrderBy(x => x.ProductionOrderId)
		.ThenBy(x => x.PhaseNumber);
}
