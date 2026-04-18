using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Dtos;

namespace OpenMES.WebApi.Controllers;

/// <summary>
/// Identity users and roles management endpoint, accessible only to users in the "admin" role.
/// Allows WebAdmin to create/delete users and assign roles without direct database access.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "JWT", Roles = "admin")]
public class AdminUsersController(
    UserManager<IdentityUser>  userManager,
    RoleManager<IdentityRole>  roleManager,
    ILogger<AdminUsersController> logger) : ControllerBase
{
    // ── Roles ─────────────────────────────────────────────────────────────────

    /// <summary>Returns all available roles.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IEnumerable<IdentityRoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IdentityRoleDto>>> GetRoles()
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .Select(r => new IdentityRoleDto { Id = r.Id, Name = r.Name ?? string.Empty })
            .ToListAsync();
        return Ok(roles);
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of users with their assigned roles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<IdentityUserDto>), StatusCodes.Status200OK)]
    public async Task<PagedResponse<IdentityUserDto>> GetUsers(
        [FromHeader(Name = "x-page")]      int page,
        [FromHeader(Name = "x-page-size")] int pageSize,
        CancellationToken ct = default)
    {
        var query      = userManager.Users.AsNoTracking().OrderBy(u => u.Email);
        var totalCount = await query.CountAsync(ct);
        var pageUsers  = await query.Skip(page * pageSize).Take(pageSize).ToListAsync(ct);

        var result = new List<IdentityUserDto>(pageUsers.Count);
        foreach (var u in pageUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new IdentityUserDto
            {
                Id             = u.Id,
                Email          = u.Email ?? string.Empty,
                LockoutEnabled = u.LockoutEnabled,
                LockoutEnd     = u.LockoutEnd,
                Roles          = [.. roles],
            });
        }

        return new PagedResponse<IdentityUserDto>
        {
            PageNumber = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
            Items      = result,
        };
    }

    /// <summary>Creates a new user with password and optional roles.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdentityUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdentityUserDto>> CreateUser(CreateIdentityUserDto dto)
    {
        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email    = dto.Email,
        };
        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        if (dto.Roles.Length > 0)
        {
            foreach (var role in dto.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
            await userManager.AddToRolesAsync(user, dto.Roles);
        }

        var roles = await userManager.GetRolesAsync(user);
        logger.LogInformation("Creato utente {Email} con ruoli: {Roles}",
            dto.Email, string.Join(", ", roles));

        var created = new IdentityUserDto
        {
            Id    = user.Id,
            Email = user.Email ?? dto.Email,
            Roles = [.. roles],
        };
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, created);
    }

    /// <summary>Returns a user by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IdentityUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdentityUserDto>> GetUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new IdentityUserDto
        {
            Id             = user.Id,
            Email          = user.Email ?? string.Empty,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd     = user.LockoutEnd,
            Roles          = [.. roles],
        });
    }

    /// <summary>Updates password and/or roles for a user.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(string id, UpdateIdentityUserDto dto)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token  = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));
        }

        // Role synchronization: remove old roles, then add new ones.
        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);

        if (dto.Roles.Length > 0)
        {
            foreach (var role in dto.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
            await userManager.AddToRolesAsync(user, dto.Roles);
        }

        logger.LogInformation("Aggiornato utente {Id} — nuovi ruoli: {Roles}",
            id, string.Join(", ", dto.Roles));
        return NoContent();
    }

    /// <summary>Deletes a user. Self-deletion is not allowed.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Prevent self-deletion
        var currentUserId = userManager.GetUserId(User);
        if (currentUserId == id)
            return BadRequest("Non è possibile eliminare il proprio account.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        logger.LogInformation("Eliminato utente {Id} ({Email})", id, user.Email);
        return NoContent();
    }

    /// <summary>Unlocks an account locked after too many failed attempts.</summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("Account sbloccato per utente {Id}", id);
        return NoContent();
    }
}
