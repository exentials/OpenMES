using OpenMES.Data.Dtos;

namespace OpenMES.WebAdmin.Services;

/// <summary>
/// Abstraction over browser storage for admin authentication payload.
/// </summary>
public interface IAdminAuthStorage
{
    Task<AdminLoginResultDto?> GetAsync();
    Task SetAsync(AdminLoginResultDto result);
    Task DeleteAsync();
}
