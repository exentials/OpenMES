using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Warehouse stock movements (append-only ledger).
/// Update and Delete are intentionally not exposed — movements are immutable once recorded.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class StockMovementController(OpenMESDbContext dbContext, ILogger<StockMovementController> logger)
	: RestApiControllerBase<StockMovement, StockMovementDto, int>(dbContext, logger)
{
	protected override IQueryable<StockMovement> Query => base.Query
		.Include(x => x.Material)
		.Include(x => x.StorageLocation)
		.Include(x => x.Operator)
		.OrderByDescending(x => x.MovementDate);

	/// <summary>Not supported — stock movements are immutable.</summary>
	[ApiExplorerSettings(IgnoreApi = true)]
	public override Task<IActionResult> Update(int id, StockMovementDto data, CancellationToken cancellationToken)
		=> Task.FromResult<IActionResult>(StatusCode(405, "Stock movements are immutable."));

	/// <summary>Not supported — stock movements cannot be deleted.</summary>
	[ApiExplorerSettings(IgnoreApi = true)]
	public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
		=> Task.FromResult<IActionResult>(StatusCode(405, "Stock movements cannot be deleted."));
}
