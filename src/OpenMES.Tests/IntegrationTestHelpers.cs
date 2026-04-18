using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMES.Tests;

internal static class IntegrationTestHelpers
{
    public static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var target = typeof(TContext);
        var descriptors = services.Where(d =>
            d.ServiceType == target
            || d.ServiceType == typeof(DbContextOptions<TContext>)
            || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Internal.IDbContextPool") == true
                && d.ServiceType.IsGenericType
                && d.ServiceType.GenericTypeArguments.Contains(target))
            || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease") == true
                && d.ServiceType.IsGenericType
                && d.ServiceType.GenericTypeArguments.Contains(target))
            || (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration") == true
                && d.ServiceType.IsGenericType
                && d.ServiceType.GenericTypeArguments.Contains(target))
            || (d.ImplementationType?.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                && d.ImplementationType.IsGenericType
                && d.ImplementationType.GenericTypeArguments.Contains(target))
        ).ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);
    }
}
