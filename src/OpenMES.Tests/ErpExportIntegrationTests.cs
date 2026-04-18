using System.Net.Http.Json;
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

public class ErpExportIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestJwtSecret = "integration-test-secret-key-minimum-32-chars!!";
    private const string TestJwtIssuer = "openmes-webapi";
    private const string TestJwtAudience = "openmes-webadmin";

    private readonly WebApplicationFactory<Program> _factory;

    public ErpExportIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                IntegrationTestHelpers.RemoveDbContextRegistrations<OpenMESDbContext>(services);
                IntegrationTestHelpers.RemoveDbContextRegistrations<OpenMESIdentityDbContext>(services);

                services.AddDbContextPool<OpenMESDbContext>(options =>
                    options.UseInMemoryDatabase("ErpExportIntegrationTestDb"));
                services.AddDbContextPool<OpenMESIdentityDbContext>(options =>
                    options.UseInMemoryDatabase("ErpExportIntegrationIdentityDb"));

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

            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"]                         = TestJwtSecret,
                    ["Jwt:Issuer"]                            = TestJwtIssuer,
                    ["Jwt:Audience"]                          = TestJwtAudience,
                    ["Jwt:ExpirationMinutes"]                 = "60",
                    ["ConnectionStrings:openmes-db"]          = "Host=localhost;Database=openmes_test;Username=test;Password=test",
                    ["ConnectionStrings:openmes-identity-db"] = "Host=localhost;Database=openmes_identity_test;Username=test;Password=test",
                });
            });
        });
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims:
            [
                new Claim(ClaimTypes.Name, "test@test.com"),
                new Claim(ClaimTypes.Role, "Admin"),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        return client;
    }

    [Fact]
    public async Task I1_ExportWorkSessions_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        var response = await client.PostAsync("/api/erpexport/worksession", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task I2_ExportWorkSessions_WithAuth_Returns200()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/erpexport/worksession", null);

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
    }

    [Fact]
    public async Task I3_ConfirmWorkSessions_WithAuth_EmptyBody_Returns400()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/erpexport/worksession/confirm",
            new { items = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task I4_ExportDeclarations_WithAuth_Returns200()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/erpexport/productiondeclaration", null);

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
    }
}

