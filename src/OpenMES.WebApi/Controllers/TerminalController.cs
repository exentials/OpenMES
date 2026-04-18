using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Contexts;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Controller for physical terminals (WebClient).
/// Uses the TerminalScheme authentication scheme (static token from DB)
/// instead of admin JWT, overriding the default inherited from ApiControllerBase.
/// </summary>
[Authorize(AuthenticationSchemes = TerminalAuthenticationHandler.SchemeName)]
public class TerminalController(OpenMESDbContext dbContext, ILogger<TerminalController> logger) : ApiControllerBase(dbContext, logger)
{
	[HttpPost(nameof(Connect))]
	[ProducesResponseType<TerminalLoginResultDto>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[AllowAnonymous]
	public async Task<IActionResult> Connect(TerminalLoginDto data, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			Logger.LogWarning("Request cancelled by client.");
			return StatusCode(StatusCodes.Status499ClientClosedRequest);
		}

		if (data is null || string.IsNullOrWhiteSpace(data.Name) || string.IsNullOrWhiteSpace(data.Password))
		{
			return BadRequest("Missing terminal credentials.");
		}
		try
		{
			var terminal = await DbContext.ClientDevices.FirstOrDefaultAsync(t => t.Name == data.Name && t.Password == data.Password, cancellationToken);
			if (terminal is null)
			{
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					Logger.LogWarning("Unauthorized access attempt for terminal '{TerminalName}'.", data.Name);
				}
				return Unauthorized("Invalid credentials.");
			}
			if (Logger.IsEnabled(LogLevel.Information))
			{
				Logger.LogInformation("Terminal '{TerminalName}' connected successfully.", terminal.Name);
			}
			var result = new TerminalLoginResultDto
			{
				Name = terminal.Name,
				AuthToken = terminal.AuthToken,
			};
			return Ok(result);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error occurred while accessing the database for terminal '{TerminalName}'.", data.Name);
			return InternalServerError("An error occurred while processing your request.");
		}
	}

	[ProducesResponseType<IEnumerable<MachineDto>>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[HttpGet(nameof(Machines))]
	public async Task<ActionResult<IEnumerable<MachineDto>>> Machines(string terminal,CancellationToken cancellationToken)
	{

		var clientDevice = await DbContext.ClientDevices
			.Include(t => t.Machines)
			.Where(t => t.Name == terminal)			
			.Select(t => ClientDevice.AsDto(t))
			.FirstOrDefaultAsync(cancellationToken);
		
		return Ok(clientDevice?.Machines);
	}


}
