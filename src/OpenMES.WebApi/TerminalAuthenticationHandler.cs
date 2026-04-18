using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenMES.Data.Contexts;
using OpenMES.Data.Entities;

namespace OpenMES.WebApi;

public class TerminalAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	OpenMESDbContext dbContex
	) 
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public const string SchemeName = "TerminalScheme";

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var authHeader = Request.Headers.Authorization.ToString();
		if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
			return AuthenticateResult.Fail("Bearer Token missing");

		var token = authHeader["Bearer ".Length..].Trim();

		var clientDevice = await dbContex.ClientDevices.Where(t => t.AuthToken == token).FirstOrDefaultAsync();
		if (clientDevice is null)
		{			
			return AuthenticateResult.Fail("Token non valido");
		}

		var claims = new[]
		{
			new Claim(ClaimTypes.Name, clientDevice.Name),
			new Claim(ClaimTypes.Role, "device"),
		};
		var identity = new ClaimsIdentity(claims, Scheme.Name);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme.Name);

		return AuthenticateResult.Success(ticket);
	}
}