using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenMES.Data.Contexts;

namespace OpenMES.Tests;

public class RoleAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestJwtSecret = "integration-test-secret-key-minimum-32-chars!!";
    private const string TestJwtIssuer = "openmes-webapi";
    private const string TestJwtAudience = "openmes-webadmin";

    private readonly WebApplicationFactory<Program> _factory;

    public RoleAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveDbContextRegistrations<OpenMESDbContext>(services);
                RemoveDbContextRegistrations<OpenMESIdentityDbContext>(services);

                services.AddDbContextPool<OpenMESDbContext>(options =>
                    options.UseInMemoryDatabase("RoleAuthTests_Data"));
                services.AddDbContextPool<OpenMESIdentityDbContext>(options =>
                    options.UseInMemoryDatabase("RoleAuthTests_Identity"));

                services.PostConfigure<JwtBearerOptions>("JWT", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = TestJwtIssuer,
                        ValidAudience = TestJwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret)),
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };
                });
            });

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = TestJwtSecret,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience,
                    ["ConnectionStrings:openmes-db"] = "Host=localhost;Database=role_auth_data;Username=test;Password=test",
                    ["ConnectionStrings:openmes-identity-db"] = "Host=localhost;Database=role_auth_identity;Username=test;Password=test",
                });
            });
        });
    }

    [Fact]
    public async Task Jwt_WithAdminRole_GetAdminUsers_Returns200()
    {
        using var client = CreateJwtClient("admin");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Jwt_WithoutAdminRole_GetAdminUsers_Returns403()
    {
        using var client = CreateJwtClient("User");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoJwt_GetAdminUsers_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TerminalSchemeToken_GetAdminUsers_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "terminal-device-token");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateJwtClient(string role)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims:
            [
                new Claim(ClaimTypes.Name, "auth-test@openmes.local"),
                new Claim(ClaimTypes.Role, role),
            ],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        return client;
    }

    private static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
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
