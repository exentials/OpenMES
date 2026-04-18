using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Manages the export of production data (WorkSession and ProductionDeclaration)
/// to the ERP system.
///
/// Export flow:
///   1. Caller queries GET /worksession/pending-export or /productiondeclaration/pending-export
///      to get the records to transmit.
///   2. Caller transmits the records to the ERP using the ErpExportRowDto payload.
///   3. ERP returns a counter/ID for each acquired record.
///   4. Caller calls POST /erpexport/worksession/confirm or /productiondeclaration/confirm
///      with the mapping RecordId → ExternalCounterId.
///   5. This controller writes back ExternalCounterId + ErpExportedAt on each record.
///
/// Pre-export batch endpoint:
///   POST /erpexport/worksession  → returns pending rows formatted for ERP transmission
///   POST /erpexport/productiondeclaration → same for declarations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin, User")]
public class ErpExportController(OpenMESDbContext dbContext, ILogger<ErpExportController> logger)
	: ControllerBase
{

	// ── WorkSession export ────────────────────────────────────────────────────

	/// <summary>
	/// Returns all WorkSession records pending ERP export, formatted as ErpExportRowDto.
	/// Marks them as "in export" by setting ErpExportedAt = now on all returned records.
	/// Call /worksession/confirm after the ERP confirms acquisition.
	/// </summary>
	[HttpPost("worksession")]
	public async Task<ActionResult<ErpExportResultDto>> ExportWorkSessions(CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;

		var pending = await dbContext.WorkSessions
			.Include(x => x.Operator)
			.Include(x => x.Machine)
			.Include(x => x.ProductionOrderPhase)
			.Where(x => x.Status == WorkSessionStatus.Closed
					 && x.ExternalCounterId == null
					 && x.ReversedById == null)
			.OrderBy(x => x.StartTime)
			.ToListAsync(ct);

		if (pending.Count == 0)
			return Ok(new ErpExportResultDto { ExportedCount = 0, ExportedAt = now, Rows = [] });

		// Build rows — reversal rows carry the original ExternalCounterId for ERP matching
		var rows = new List<ErpExportRowDto>();
		foreach (var s in pending)
		{
			string? reversalOfCounter = null;
			if (s.IsReversal && s.ReversalOfId.HasValue)
			{
				var original = await dbContext.WorkSessions
					.Where(x => x.Id == s.ReversalOfId.Value)
					.Select(x => x.ExternalCounterId)
					.FirstOrDefaultAsync(ct);
				reversalOfCounter = original;
			}

			rows.Add(new ErpExportRowDto
			{
				RecordId                  = s.Id,
				PhaseExternalId           = s.PhaseExternalId,
				EntityType                = "WorkSession",
				SessionType               = s.SessionType.ToString(),
				AllocatedMinutes          = s.AllocatedMinutes,
				IsReversal                = s.IsReversal,
				ReversalOfExternalCounterId = reversalOfCounter,
				OperatorName              = s.Operator?.Name,
				MachineCode               = s.Machine?.Code,
				RecordDate                = s.StartTime,
			});

			// Mark as exported (timestamp only — ExternalCounterId set after ERP confirms)
			s.ErpExportedAt = now;
			s.UpdatedAt     = now;
		}

		await dbContext.SaveChangesAsync(ct);

		logger.LogInformation("ERP export: {Count} WorkSession rows prepared at {At}", pending.Count, now);

		return Ok(new ErpExportResultDto
		{
			ExportedCount = pending.Count,
			ExportedAt    = now,
			Rows          = rows,
		});
	}

	/// <summary>
	/// Confirms ERP acquisition of WorkSession records by storing the ExternalCounterId
	/// returned by the ERP for each record.
	/// </summary>
	[HttpPost("worksession/confirm")]
	public async Task<ActionResult<int>> ConfirmWorkSessions(
		[FromBody] ErpConfirmationDto confirmation, CancellationToken ct)
	{
		if (confirmation.Items.Count == 0)
			return BadRequest("No confirmation items provided.");

		var ids = confirmation.Items.Select(x => x.RecordId).ToList();
		var sessions = await dbContext.WorkSessions
			.Where(x => ids.Contains(x.Id))
			.ToListAsync(ct);

		int confirmed = 0;
		foreach (var item in confirmation.Items)
		{
			var session = sessions.FirstOrDefault(x => x.Id == item.RecordId);
			if (session is null)
			{
				logger.LogWarning("ERP confirm: WorkSession {Id} not found", item.RecordId);
				continue;
			}
			if (session.ExternalCounterId is not null)
			{
				logger.LogWarning("ERP confirm: WorkSession {Id} already confirmed", item.RecordId);
				continue;
			}

			session.ExternalCounterId = item.ExternalCounterId;
			session.UpdatedAt         = DateTimeOffset.UtcNow;
			confirmed++;
		}

		await dbContext.SaveChangesAsync(ct);
		logger.LogInformation("ERP confirm: {Count} WorkSessions confirmed", confirmed);
		return Ok(confirmed);
	}


	// ── ProductionDeclaration export ──────────────────────────────────────────

	/// <summary>
	/// Returns all ProductionDeclaration records pending ERP export, formatted as ErpExportRowDto.
	/// Marks them with ErpExportedAt = now. Call /productiondeclaration/confirm after ERP confirms.
	/// </summary>
	[HttpPost("productiondeclaration")]
	public async Task<ActionResult<ErpExportResultDto>> ExportProductionDeclarations(CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;

		var pending = await dbContext.ProductionDeclarations
			.Include(x => x.Operator)
			.Include(x => x.Machine)
			.Include(x => x.ProductionOrderPhase)
			.Where(x => x.ExternalCounterId == null
					 && x.ReversedById == null)
			.OrderBy(x => x.DeclarationDate)
			.ToListAsync(ct);

		if (pending.Count == 0)
			return Ok(new ErpExportResultDto { ExportedCount = 0, ExportedAt = now, Rows = [] });

		var rows = new List<ErpExportRowDto>();
		foreach (var d in pending)
		{
			string? reversalOfCounter = null;
			if (d.IsReversal && d.ReversalOfId.HasValue)
			{
				var original = await dbContext.ProductionDeclarations
					.Where(x => x.Id == d.ReversalOfId.Value)
					.Select(x => x.ExternalCounterId)
					.FirstOrDefaultAsync(ct);
				reversalOfCounter = original;
			}

			rows.Add(new ErpExportRowDto
			{
				RecordId                    = d.Id,
				PhaseExternalId             = d.PhaseExternalId,
				EntityType                  = "ProductionDeclaration",
				ConfirmedQuantity           = d.ConfirmedQuantity,
				ScrapQuantity               = d.ScrapQuantity,
				IsReversal                  = d.IsReversal,
				ReversalOfExternalCounterId = reversalOfCounter,
				OperatorName                = d.Operator?.Name,
				MachineCode                 = d.Machine?.Code,
				RecordDate                  = d.DeclarationDate,
			});

			d.ErpExportedAt = now;
			d.UpdatedAt     = now;
		}

		await dbContext.SaveChangesAsync(ct);

		logger.LogInformation("ERP export: {Count} ProductionDeclaration rows prepared at {At}", pending.Count, now);

		return Ok(new ErpExportResultDto
		{
			ExportedCount = pending.Count,
			ExportedAt    = now,
			Rows          = rows,
		});
	}

	/// <summary>
	/// Confirms ERP acquisition of ProductionDeclaration records by storing ExternalCounterId.
	/// </summary>
	[HttpPost("productiondeclaration/confirm")]
	public async Task<ActionResult<int>> ConfirmProductionDeclarations(
		[FromBody] ErpConfirmationDto confirmation, CancellationToken ct)
	{
		if (confirmation.Items.Count == 0)
			return BadRequest("No confirmation items provided.");

		var ids = confirmation.Items.Select(x => x.RecordId).ToList();
		var declarations = await dbContext.ProductionDeclarations
			.Where(x => ids.Contains(x.Id))
			.ToListAsync(ct);

		int confirmed = 0;
		foreach (var item in confirmation.Items)
		{
			var decl = declarations.FirstOrDefault(x => x.Id == item.RecordId);
			if (decl is null)
			{
				logger.LogWarning("ERP confirm: ProductionDeclaration {Id} not found", item.RecordId);
				continue;
			}
			if (decl.ExternalCounterId is not null)
			{
				logger.LogWarning("ERP confirm: ProductionDeclaration {Id} already confirmed", item.RecordId);
				continue;
			}

			decl.ExternalCounterId = item.ExternalCounterId;
			decl.UpdatedAt         = DateTimeOffset.UtcNow;
			confirmed++;
		}

		await dbContext.SaveChangesAsync(ct);
		logger.LogInformation("ERP confirm: {Count} ProductionDeclarations confirmed", confirmed);
		return Ok(confirmed);
	}
}
