using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using OpenMES.Data.Dtos;

namespace OpenMES.WebAdmin.Services;

/// <summary>
/// ProtectedLocalStorage-backed implementation for admin auth persistence.
/// </summary>
public class AdminAuthStorage(ProtectedLocalStorage storage) : IAdminAuthStorage
{
    private const string StorageKey = "admin-auth";

    public async Task<AdminLoginResultDto?> GetAsync()
    {
        var stored = await storage.GetAsync<AdminLoginResultDto>(StorageKey);
        return stored.Success ? stored.Value : null;
    }

    public Task SetAsync(AdminLoginResultDto result)
        => storage.SetAsync(StorageKey, result).AsTask();

    public Task DeleteAsync()
        => storage.DeleteAsync(StorageKey).AsTask();
}
