using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Picking events against a picking list line.
/// Each item is append-only — it generates a StockMovement and cannot be deleted.
/// On create, updates PickedQuantity and Status on the parent PhasePickingList.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class PhasePickingItemController(OpenMESDbContext dbContext, ILogger<PhasePickingItemController> logger)
	: RestApiControllerBase<PhasePickingItem, PhasePickingItemDto, int>(dbContext, logger)
{
	protected override IQueryable<PhasePickingItem> Query => base.Query
		.Include(x => x.Operator)
		.Include(x => x.PhasePickingList).ThenInclude(x => x.Material)
		.Include(x => x.PhasePickingList).ThenInclude(x => x.StorageLocation);

	/// <summary>
	/// Creates a picking item and updates PickedQuantity + Status on the parent line.
	/// </summary>
	protected override async Task<int> CreateAsync(
		PhasePickingItemDto dto, CancellationToken cancellationToken = default)
	{
		var line = await DbContext.PhasePickingLists
			.FirstOrDefaultAsync(x => x.Id == dto.PhasePickingListId, cancellationToken)
			?? throw new ProblemException("Picking line not found",
				$"PhasePickingList {dto.PhasePickingListId} does not exist.");

		var entity = PhasePickingItem.AsEntity(dto);
		entity.CreatedAt = DateTimeOffset.UtcNow;
		entity.UpdatedAt = DateTimeOffset.UtcNow;

		DbContext.PhasePickingItems.Add(entity);
		await DbContext.SaveChangesAsync(cancellationToken);

		// Update parent line PickedQuantity and Status
		line.PickedQuantity += dto.PickedQuantity;
		line.Status = line.PickedQuantity >= line.RequiredQuantity
			? PickingStatus.Completed
			: PickingStatus.PartiallyPicked;
		line.UpdatedAt = DateTimeOffset.UtcNow;

		await DbContext.SaveChangesAsync(cancellationToken);
		return entity.Id;
	}

	/// <summary>Returns all picking items for a specific picking list line.</summary>
	[HttpGet("bypickinglist/{pickingListId:int}")]
	public async Task<ActionResult<IEnumerable<PhasePickingItemDto>>> GetByPickingList(
		int pickingListId, CancellationToken cancellationToken)
	{
		var items = await Query
			.Where(x => x.PhasePickingListId == pickingListId)
			.OrderByDescending(x => x.PickedAt)
			.Select(x => PhasePickingItem.AsDto(x))
			.ToListAsync(cancellationToken);
		return Ok(items);
	}
}
