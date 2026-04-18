using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Append-only log of machine state transitions.
/// Records are never modified. The current state is the most recent record
/// for a given machine, accessible via the /machine/{id}/current endpoint.
/// </summary>
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
public class MachineStateController(OpenMESDbContext dbContext, ILogger<MachineStateController> logger)
	: RestApiControllerBase<MachineState, MachineStateDto, int>(dbContext, logger)
{
	protected override IQueryable<MachineState> Query => base.Query
		.Include(x => x.Machine)
		.Include(x => x.Operator);

	/// <summary>Returns the current state of a specific machine (most recent event).</summary>
	[HttpGet("machine/{machineId:int}/current")]
	public async Task<ActionResult<MachineStateDto?>> GetCurrent(int machineId, CancellationToken ct)
	{
		var latest = await Query
			.Where(x => x.MachineId == machineId)
			.OrderByDescending(x => x.EventTime)
			.FirstOrDefaultAsync(ct);

		return Ok(latest is null ? null : MachineState.AsDto(latest));
	}

	/// <summary>Returns the full state history for a specific machine.</summary>
	[HttpGet("machine/{machineId:int}")]
	public async Task<ActionResult<IEnumerable<MachineStateDto>>> GetHistory(int machineId, CancellationToken ct)
	{
		var items = await Query
			.Where(x => x.MachineId == machineId)
			.OrderByDescending(x => x.EventTime)
			.Select(x => MachineState.AsDto(x))
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>Returns the current state of all machines in a single call (dashboard view).</summary>
	[HttpGet("current/all")]
	public async Task<ActionResult<IEnumerable<MachineStateDto>>> GetAllCurrent(CancellationToken ct)
	{
		// Subquery: max EventTime per machine, then join back
		var latestIds = await Query
			.GroupBy(x => x.MachineId)
			.Select(g => g.OrderByDescending(x => x.EventTime).First().Id)
			.ToListAsync(ct);

		var items = await Query
			.Where(x => latestIds.Contains(x.Id))
			.Select(x => MachineState.AsDto(x))
			.ToListAsync(ct);

		return Ok(items);
	}
}
