using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Services;
using OpenMES.WebApiClient;

namespace OpenMES.Tests;

public class LocalAuthStateProviderTests
{
    private const string Secret = "local-auth-tests-secret-key-minimum-32-chars!!";
    private const string Issuer = "openmes-webapi";
    private const string Audience = "openmes-webadmin";

    [Fact]
    public async Task ValidJwt_GetAuthenticationStateAsync_ReturnsAuthenticatedPrincipal()
    {
        var token = BuildToken(expiresUtc: DateTime.UtcNow.AddMinutes(20));
        var storage = new Mock<IAdminAuthStorage>();
        storage.Setup(x => x.GetAsync()).ReturnsAsync(new AdminLoginResultDto
        {
            Email = "admin@openmes.local",
            AuthToken = token,
            Roles = ["admin"],
        });

        var provider = CreateProvider(storage.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("admin@openmes.local", state.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
    }

    [Fact]
    public async Task ExpiredJwt_GetAuthenticationStateAsync_ReturnsAnonymousAndClearsStorage()
    {
        var token = BuildToken(expiresUtc: DateTime.UtcNow.AddMinutes(-5));
        var storage = new Mock<IAdminAuthStorage>();
        storage.Setup(x => x.GetAsync()).ReturnsAsync(new AdminLoginResultDto
        {
            Email = "admin@openmes.local",
            AuthToken = token,
            Roles = ["admin"],
        });

        var provider = CreateProvider(storage.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        storage.Verify(x => x.DeleteAsync(), Times.Once);
    }

    [Fact]
    public async Task TamperedJwt_GetAuthenticationStateAsync_ReturnsAnonymousAndClearsStorage()
    {
        var validToken = BuildToken(expiresUtc: DateTime.UtcNow.AddMinutes(20));
        var tamperedToken = validToken + "tampered";
        var storage = new Mock<IAdminAuthStorage>();
        storage.Setup(x => x.GetAsync()).ReturnsAsync(new AdminLoginResultDto
        {
            Email = "admin@openmes.local",
            AuthToken = tamperedToken,
            Roles = ["admin"],
        });

        var provider = CreateProvider(storage.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        storage.Verify(x => x.DeleteAsync(), Times.Once);
    }

    [Fact]
    public async Task MissingStorageEntry_GetAuthenticationStateAsync_ReturnsAnonymousWithoutException()
    {
        var storage = new Mock<IAdminAuthStorage>();
        storage.Setup(x => x.GetAsync()).ReturnsAsync((AdminLoginResultDto?)null);

        var provider = CreateProvider(storage.Object);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
        storage.Verify(x => x.DeleteAsync(), Times.Never);
    }

    private static AuthenticationStateProvider CreateProvider(IAdminAuthStorage storage)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = Secret,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
            })
            .Build();

        var mesClient = new MesClient(new HttpClient());

        return new LocalAuthStateProvider(
            storage,
            mesClient,
            config,
            NullLogger<LocalAuthStateProvider>.Instance);
    }

    private static string BuildToken(DateTime expiresUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(ClaimTypes.Name, "admin@openmes.local"),
                new Claim(ClaimTypes.Role, "admin"),
            ],
            notBefore: expiresUtc.AddMinutes(-30),
            expires: expiresUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
