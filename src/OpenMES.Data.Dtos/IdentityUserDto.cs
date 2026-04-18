namespace OpenMES.Data.Dtos;

/// <summary>Identity user representation for WebAdmin.</summary>
public class IdentityUserDto
{
	public string Id { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public bool LockoutEnabled { get; set; }
	public DateTimeOffset? LockoutEnd { get; set; }
	public string[] Roles { get; set; } = [];
}

/// <summary>Payload used to create a new admin user.</summary>
public class CreateIdentityUserDto
{
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public string[] Roles { get; set; } = [];
}

/// <summary>Payload used to update a user's password or roles.</summary>
public class UpdateIdentityUserDto
{
	public string? NewPassword { get; set; }
	public string[] Roles { get; set; } = [];
}

/// <summary>All roles available in the system.</summary>
public class IdentityRoleDto
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}
