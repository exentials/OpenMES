using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>Picking list lines per production order phase.</summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class PhasePickingListController(OpenMESDbContext dbContext, ILogger<PhasePickingListController> logger)
	: RestApiControllerBase<PhasePickingList, PhasePickingListDto, int>(dbContext, logger)
{
	protected override IQueryable<PhasePickingList> Query => base.Query
		.Include(x => x.ProductionOrderPhase)
		.Include(x => x.Material)
		.Include(x => x.StorageLocation);

	/// <summary>Returns all picking lines for a specific production order phase.</summary>
	[HttpGet("byphase/{phaseId:int}")]
	public async Task<ActionResult<IEnumerable<PhasePickingListDto>>> GetByPhase(int phaseId, CancellationToken cancellationToken)
	{
		var items = await Query
			.Where(x => x.ProductionOrderPhaseId == phaseId)
			.OrderBy(x => x.Material.PartNumber)
			.Select(x => PhasePickingList.AsDto(x))
			.ToListAsync(cancellationToken);
		return Ok(items);
	}
}
