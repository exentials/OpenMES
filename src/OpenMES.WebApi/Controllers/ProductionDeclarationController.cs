using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Production declarations (confirmed and scrap quantities per phase).
/// On every create/correct, updates ProductionOrderPhase.ConfirmedQuantity
/// and ScrapQuantity as the sum of all active (non-reversed) declarations.
/// Supports the correction/reversal pattern for ERP-exported records.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class ProductionDeclarationController(OpenMESDbContext dbContext, ILogger<ProductionDeclarationController> logger)
	: RestApiControllerBase<ProductionDeclaration, ProductionDeclarationDto, int>(dbContext, logger)
{
	protected override IQueryable<ProductionDeclaration> Query => base.Query
		.Include(x => x.ProductionOrderPhase)
		.Include(x => x.Operator)
		.Include(x => x.Machine);

	/// <summary>
	/// Creates a declaration and immediately updates the phase aggregates
	/// (ConfirmedQuantity and ScrapQuantity).
	/// Also copies PhaseExternalId from the phase at creation time.
	/// </summary>
	protected override async Task<int> CreateAsync(
		ProductionDeclarationDto dto, CancellationToken cancellationToken = default)
	{
		var phase = await DbContext.ProductionOrderPhases
			.FirstOrDefaultAsync(x => x.Id == dto.ProductionOrderPhaseId, cancellationToken)
			?? throw new ProblemException("Phase not found",
				$"ProductionOrderPhase {dto.ProductionOrderPhaseId} does not exist.");

		var lastShift = await DbContext.OperatorShifts
			.Where(x => x.OperatorId == dto.OperatorId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(cancellationToken);

		if (lastShift is null || lastShift.EventType == OperatorEventType.CheckOut)
			throw new ProblemException(
				"Operator not present",
				$"Operator {dto.OperatorId} is not checked in.");

		if (lastShift.EventType == OperatorEventType.BreakStart)
			throw new ProblemException(
				"Operator on break",
				$"Operator {dto.OperatorId} is currently on break.");

		// ── Quantity validation ───────────────────────────────────────────────

		// Load order with all its phases and the material
		var order = await DbContext.ProductionOrders
			.Include(o => o.ProductionOrderPhases)
			.Include(o => o.Material)
			.FirstOrDefaultAsync(x => x.Id == phase.ProductionOrderId, cancellationToken)
			?? throw new ProblemException("Order not found",
				$"ProductionOrder for phase {phase.Id} not found.");

		var orderedPhases = order.ProductionOrderPhases
			.OrderBy(p => p.PhaseNumber, StringComparer.Ordinal)
			.ToList();

		bool isFirstPhase = orderedPhases.First().Id == phase.Id;

		// Rule A — intra-phase cap
		var alreadyDeclared = phase.ConfirmedQuantity + phase.ScrapQuantity;
		var remaining = phase.PlannedQuantity - alreadyDeclared;
		var newTotal = dto.ConfirmedQuantity + dto.ScrapQuantity;

		if (!(isFirstPhase && order.Material.AllowOverproduction))
		{
			if (newTotal > remaining)
				throw new ProblemException(
					"Quantity exceeds phase plan",
					$"Remaining capacity on phase {phase.PhaseNumber}: {remaining}. " +
					$"Attempted: {newTotal} (confirmed={dto.ConfirmedQuantity}, scrap={dto.ScrapQuantity}).");
		}

		// Rule B — inter-phase constraint (non-first phases only)
		if (!isFirstPhase)
		{
			var prevPhase = orderedPhases
				.LastOrDefault(p => string.Compare(p.PhaseNumber, phase.PhaseNumber,
								StringComparison.Ordinal) < 0);

			if (prevPhase is not null)
			{
				var maxFromPrev = prevPhase.ConfirmedQuantity - phase.ConfirmedQuantity;
				if (dto.ConfirmedQuantity > maxFromPrev)
					throw new ProblemException(
						"Quantity exceeds previous phase output",
						$"Phase {phase.PhaseNumber} can confirm at most {maxFromPrev} pieces " +
						$"(previous phase {prevPhase.PhaseNumber} confirmed {prevPhase.ConfirmedQuantity}, " +
						$"already confirmed on this phase: {phase.ConfirmedQuantity}).");
			}
		}

		var entity = ProductionDeclaration.AsEntity(dto);
		entity.PhaseExternalId = phase.ExternalId;   // snapshot at creation
		entity.CreatedAt       = DateTimeOffset.UtcNow;
		entity.UpdatedAt       = DateTimeOffset.UtcNow;

		DbContext.ProductionDeclarations.Add(entity);
		await DbContext.SaveChangesAsync(cancellationToken);

		await UpdatePhaseAggregates(dto.ProductionOrderPhaseId, cancellationToken);
		await ProcessAutomaticPicking(entity, phase, cancellationToken);
		return entity.Id;
	}

	/// <summary>Returns all declarations pending ERP export (not yet exported).</summary>
	[HttpGet("pending-export")]
	public async Task<ActionResult<IEnumerable<ProductionDeclarationDto>>> GetPendingExport(CancellationToken ct)
	{
		var items = await Query
			.Where(x => x.ExternalCounterId == null && x.ReversedById == null)
			.OrderBy(x => x.DeclarationDate)
			.Select(x => ProductionDeclaration.AsDto(x))
			.ToListAsync(ct);
		return Ok(items);
	}

	/// <summary>
	/// Corrects a production declaration by applying the appropriate strategy based on
	/// whether it has already been exported to the ERP.
	///
	/// Not yet exported (ExternalCounterId = null):
	///   The original declaration is deleted and a new one is created with the corrected data.
	///
	/// Already exported (ExternalCounterId is set):
	///   1. A reversal declaration is created with negated quantities and IsReversal = true.
	///   2. The original is marked with ReversedById pointing to the reversal.
	///   3. A new corrected declaration is created with the data from the request body.
	/// </summary>
	[HttpPost("{id:int}/correct")]
	public async Task<ActionResult<ProductionDeclarationDto>> Correct(
		int id, [FromBody] ProductionDeclarationDto corrected, CancellationToken ct)
	{
		var original = await Query.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (original is null) return NotFound();
		if (original.IsReversal) return BadRequest("Cannot correct a reversal record.");
		if (original.ReversedById is not null) return BadRequest("This declaration has already been reversed.");

		var now = DateTimeOffset.UtcNow;

		if (original.ExternalCounterId is null)
		{
			// Not yet exported — delete and recreate
			DbContext.ProductionDeclarations.Remove(original);

			var replacement = ProductionDeclaration.AsEntity(corrected);
			replacement.Id = 0;
			replacement.PhaseExternalId = original.PhaseExternalId;
			replacement.CreatedAt = now;
			replacement.UpdatedAt = now;

			DbContext.ProductionDeclarations.Add(replacement);
			await DbContext.SaveChangesAsync(ct);
			await UpdatePhaseAggregates(original.ProductionOrderPhaseId, ct);

			var created = await Query.FirstAsync(x => x.Id == replacement.Id, ct);
			return Ok(ProductionDeclaration.AsDto(created));
		}
		else
		{
			// Already exported — create reversal + new corrected record
			var reversal = new ProductionDeclaration
			{
				ProductionOrderPhaseId = original.ProductionOrderPhaseId,
				OperatorId             = original.OperatorId,
				MachineId              = original.MachineId,
				DeclarationDate        = now,
				ConfirmedQuantity      = -original.ConfirmedQuantity,
				ScrapQuantity          = -original.ScrapQuantity,
				PhaseExternalId        = original.PhaseExternalId,
				IsReversal             = true,
				ReversalOfId           = original.Id,
				Notes                  = $"Reversal of declaration {original.Id} (ERP: {original.ExternalCounterId})",
				CreatedAt              = now,
				UpdatedAt              = now,
			};
			DbContext.ProductionDeclarations.Add(reversal);
			await DbContext.SaveChangesAsync(ct);

			original.ReversedById = reversal.Id;
			original.UpdatedAt    = now;

			var newDecl = ProductionDeclaration.AsEntity(corrected);
			newDecl.Id              = 0;
			newDecl.PhaseExternalId = original.PhaseExternalId;
			newDecl.IsReversal      = false;
			newDecl.CreatedAt       = now;
			newDecl.UpdatedAt       = now;

			DbContext.ProductionDeclarations.Add(newDecl);
			await DbContext.SaveChangesAsync(ct);
			await UpdatePhaseAggregates(original.ProductionOrderPhaseId, ct);

			var created = await Query.FirstAsync(x => x.Id == newDecl.Id, ct);
			return Ok(ProductionDeclaration.AsDto(created));
		}
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Recomputes ConfirmedQuantity and ScrapQuantity on the phase as the sum of
	/// all active declarations (not reversed, not reversals themselves which carry
	/// negative values that cancel out automatically in the sum).
	/// </summary>
	private async Task UpdatePhaseAggregates(int phaseId, CancellationToken ct)
	{
		var phase = await DbContext.ProductionOrderPhases
			.FirstOrDefaultAsync(x => x.Id == phaseId, ct);
		if (phase is null) return;

		// Sum all declarations where the record has not been superseded (ReversedById = null).
		// Reversals (IsReversal = true) carry negative quantities and are included to correctly
		// cancel previously counted values when a correction has been exported and reversed.
		var totals = await DbContext.ProductionDeclarations
			.Where(x => x.ProductionOrderPhaseId == phaseId && x.ReversedById == null)
			.GroupBy(x => x.ProductionOrderPhaseId)
			.Select(g => new
			{
				ConfirmedQty = g.Sum(x => x.ConfirmedQuantity),
				ScrapQty     = g.Sum(x => x.ScrapQuantity),
			})
			.FirstOrDefaultAsync(ct);

		phase.ConfirmedQuantity = totals?.ConfirmedQty ?? 0;
		phase.ScrapQuantity     = totals?.ScrapQty     ?? 0;
		phase.UpdatedAt         = DateTimeOffset.UtcNow;

		await DbContext.SaveChangesAsync(ct);
		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformation(
				"Phase {PhaseId} aggregates updated: ConfirmedQty={Confirmed}, ScrapQty={Scrap}",
				phaseId, phase.ConfirmedQuantity, phase.ScrapQuantity);
		}
	}

	/// <summary>
	/// For each PhasePickingList line on the phase with IsAutomatic=true (and not
	/// IsConsumable/IsPhantom), generates a PhasePickingItem + StockMovement of type
	/// ProductionIssue proportional to the declared confirmed quantity.
	///
	/// Formula: pickQty = line.RequiredQuantity * (confirmedQty / phase.PlannedQuantity)
	/// Skipped when PlannedQuantity = 0 or confirmedQty = 0.
	/// StorageLocationId must be set on the line — if null, the line is skipped with a warning.
	/// </summary>
	private async Task ProcessAutomaticPicking(
		ProductionDeclaration declaration,
		ProductionOrderPhase phase,
		CancellationToken ct)
	{
		if (declaration.ConfirmedQuantity <= 0) return;
		if (phase.PlannedQuantity <= 0)
		{
			logger.LogWarning("Automatic picking skipped for phase {PhaseId}: PlannedQuantity is 0", phase.Id);
			return;
		}

		var lines = await DbContext.PhasePickingLists
			.Where(x => x.ProductionOrderPhaseId == phase.Id
					 && x.IsAutomatic
					 && !x.IsConsumable
					 && !x.IsPhantom)
			.ToListAsync(ct);

		if (lines.Count == 0) return;

		var now = DateTimeOffset.UtcNow;
		var ratio = declaration.ConfirmedQuantity / phase.PlannedQuantity;

		foreach (var line in lines)
		{
			if (line.StorageLocationId is null)
			{
				logger.LogWarning(
					"Automatic picking skipped for PhasePickingList {LineId}: StorageLocationId not set", line.Id);
				continue;
			}

			var pickQty = Math.Round(line.RequiredQuantity * ratio, 3);
			if (pickQty <= 0) continue;

			// Create StockMovement of type ProductionIssue
			var movement = new StockMovement
			{
				MaterialId        = line.MaterialId,
				StorageLocationId = line.StorageLocationId.Value,
				MovementType      = StockMovementType.ProductionIssue,
				Quantity          = pickQty,
				MovementDate      = now,
				OperatorId        = declaration.OperatorId,
				ReferenceType     = nameof(ProductionDeclaration),
				ReferenceId       = declaration.Id,
				Notes             = $"Auto-pick for declaration {declaration.Id} on phase {phase.PhaseNumber}",
				CreatedAt         = now,
				UpdatedAt         = now,
			};
			DbContext.StockMovements.Add(movement);
			await DbContext.SaveChangesAsync(ct);

			// Update MaterialStock
			var stock = await DbContext.MaterialStocks
				.FirstOrDefaultAsync(x => x.MaterialId == line.MaterialId
									   && x.StorageLocationId == line.StorageLocationId.Value, ct);
			if (stock is not null)
			{
				stock.Quantity          -= pickQty;
				stock.LastMovementDate   = now;
			}

			// Create PhasePickingItem
			var item = new PhasePickingItem
			{
				PhasePickingListId = line.Id,
				StockMovementId    = movement.Id,
				PickedQuantity     = pickQty,
				PickedAt           = now,
				OperatorId         = declaration.OperatorId,
				IsAutomatic        = true,
				Notes              = $"Auto-pick from declaration {declaration.Id}",
				CreatedAt          = now,
				UpdatedAt          = now,
			};
			DbContext.PhasePickingItems.Add(item);

			// Update PickedQuantity and Status on the line
			line.PickedQuantity += pickQty;
			line.Status = line.PickedQuantity >= line.RequiredQuantity
				? PickingStatus.Completed
				: PickingStatus.PartiallyPicked;
			line.UpdatedAt = now;
		}

		await DbContext.SaveChangesAsync(ct);
		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformation(
				"Automatic picking processed for declaration {DeclId}: {Count} line(s)", declaration.Id, lines.Count);
		}
	}
}