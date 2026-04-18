using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenMES.Data.Dtos;
using OpenMES.WebApi.Auth;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Admin authentication through ASP.NET Core Identity.
/// Generates a standard JWT (HMAC-SHA256 signed) with roles included in claims.
/// This controller replaces the /auth/login endpoint from MapIdentityApi for admin login.
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminAuthController(
    UserManager<IdentityUser>    userManager,
    SignInManager<IdentityUser>  signInManager,
    IJwtService                  jwtService,
    ILogger<AdminAuthController> logger) : ControllerBase
{
    /// <summary>
    /// Authenticates an admin user and returns a JWT with included roles.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminLoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminLoginResultDto>> LoginAsync(
        AdminLoginDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and password are required.");

        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            logger.LogWarning("Login attempt with unknown email: {Email}", dto.Email);
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user, dto.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            logger.LogWarning("Login failed for {Email}: {Reason}", dto.Email,
                result.IsLockedOut ? "account locked" : "incorrect password");
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);

        logger.LogInformation("Admin login successful for {Email} with roles: {Roles}",
            dto.Email, string.Join(", ", roles));

        return Ok(new AdminLoginResultDto
        {
            Email     = user.Email ?? dto.Email,
            AuthToken = token,
            Roles     = [.. roles],
        });
    }

    /// <summary>
    /// Server-side logout (informational — JWT is stateless).
    /// The client must remove the token from local storage.
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "JWT")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        logger.LogInformation("Explicit logout for user: {User}", User.Identity?.Name);
        return NoContent();
    }
}
