using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenMES.Data.Contexts;
using OpenMES.WebApi;
using OpenMES.WebApi.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── ProblemDetails ─────────────────────────────────────────────────────────
builder.Services.AddProblemDetails(pb =>
{
    pb.CustomizeProblemDetails = ctx =>
    {
        var details  = ctx.ProblemDetails;
        details.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
        var activity = ctx.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        details.Extensions.TryAdd("traceId",   activity?.Id ?? ctx.HttpContext.TraceIdentifier);
        details.Extensions.TryAdd("requestId", ctx.HttpContext.TraceIdentifier);
    };
});

// ── Database ───────────────────────────────────────────────────────────────
var dbProvider = builder.Configuration.GetValue("DbProvider", "Pgsql");
if (dbProvider == "Pgsql")
{
    builder.AddNpgsqlDbContext<OpenMESIdentityDbContext>("openmes-identity-db");
    builder.AddNpgsqlDbContext<OpenMESDbContext>("openmes-db");
}
else if (dbProvider == "SqlServer")
{
    builder.AddSqlServerDbContext<OpenMESIdentityDbContext>("openmes-identity-db");
    builder.AddSqlServerDbContext<OpenMESDbContext>("openmes-db");
}
else
{
    throw new InvalidOperationException($"Unsupported database provider: {dbProvider}");
}

// ── Identity ────────────────────────────────────────────────────────────────
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount   = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<OpenMESIdentityDbContext>()
.AddSignInManager();

// ── JWT authentication + TerminalScheme ─────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey  = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT:SecretKey mancante in appsettings.");
var issuer     = jwtSection["Issuer"]   ?? "openmes-webapi";
var audience   = jwtSection["Audience"] ?? "openmes-webadmin";

builder.Services.AddAuthentication(options =>
{
    // All API requests authenticate through JWT by default.
    options.DefaultAuthenticateScheme = "JWT";
    options.DefaultChallengeScheme    = "JWT";
})
.AddJwtBearer("JWT", options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = issuer,
        ValidAudience            = audience,
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        NameClaimType            = ClaimTypes.Name,
        RoleClaimType            = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
})
// Dedicated scheme for physical terminals (static DB token — unchanged)
.AddScheme<AuthenticationSchemeOptions, TerminalAuthenticationHandler>(
    TerminalAuthenticationHandler.SchemeName, null);

// Default policy: require JWT authentication on all controllers
// except endpoints marked with [AllowAnonymous] (e.g., AdminAuthController.Login, TerminalController.Connect)
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("JWT")
        .RequireAuthenticatedUser()
        .Build();
});

// ── Servizi applicativi ─────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();

// ── Controllers + OpenAPI ───────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();   // Must run before UseAuthorization.
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.Run();

public partial class Program { }
