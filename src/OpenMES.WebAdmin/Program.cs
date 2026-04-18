using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.WebAdmin.Components;
using OpenMES.WebAdmin.Services;

namespace OpenMES.WebAdmin;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		builder.AddServiceDefaults();

		// Add services to the container.
		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents();

		builder.Services.AddFluentUIComponents();

		builder.Services.AddOutputCache();

		builder.Services.AddMesClient("openmes-webapi");

		// authentication for Blazor server interactive
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(options =>
			{
				options.LoginPath = new PathString("/login");
				options.AccessDeniedPath = new PathString("/login");
			});
		builder.Services.AddAuthorizationCore();  // Core-only: nessun middleware HTTP, solo Blazor
		builder.Services.AddScoped<IAdminAuthStorage, AdminAuthStorage>();
		builder.Services.AddScoped<AuthenticationStateProvider, LocalAuthStateProvider>();
		builder.Services.AddScoped<ILoginService, LocalAuthStateProvider>();


		// Localization — supports EN (default) and IT
		builder.Services.AddLocalization();
		builder.Services.Configure<RequestLocalizationOptions>(options =>
		{
			var supported = new[] { "en", "it" };
			options.SetDefaultCulture(supported[0])
				   .AddSupportedCultures(supported)
				   .AddSupportedUICultures(supported);
			options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
		});

		var app = builder.Build();

		app.UseForwardedHeaders();
		app.MapDefaultEndpoints();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error", createScopeForErrors: true);
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseRequestLocalization();
		app.UseAntiforgery();
		app.UseOutputCache();

		app.MapStaticAssets();

		// Endpoint to set culture cookie and redirect back
		app.MapGet("/setculture", (string culture, string redirectUri, HttpContext ctx) =>
		{
			var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
				new RequestCulture(culture, culture));
			ctx.Response.Cookies.Append(
				CookieRequestCultureProvider.DefaultCookieName,
				cookieValue,
				new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), SameSite = SameSiteMode.Lax });
			return Results.LocalRedirect(redirectUri);
		});

		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		app.Run();
	}
}
