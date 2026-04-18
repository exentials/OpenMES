namespace OpenMES.Data.Dtos;

/// <summary>DTO for admin login through ASP.NET Core Identity.</summary>
public class AdminLoginDto
{
	public string Email { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Admin login response containing JWT and user roles.
/// Persisted in encrypted form in Blazor Server ProtectedLocalStorage.
/// </summary>
public class AdminLoginResultDto
{
	public string Email { get; set; } = string.Empty;
	public string AuthToken { get; set; } = string.Empty;
	public string[] Roles { get; set; } = [];
}
