using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.WebClient.Components;
using OpenMES.WebClient.Models;
using OpenMES.WebClient.Services;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient;

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
		builder.Services.AddScoped<ProtectedLocalStorage>();

		// WEB-002: Add error service for centralized error handling
		builder.Services.AddScoped<IErrorService, ErrorService>();

		// WEB-003: Add session service for terminal session management
		builder.Services.AddScoped<ISessionService, SessionService>();

		// WEB-004: Add authorization service for route protection
		builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
		builder.Services.AddScoped<IOperatorShiftClientService, OperatorShiftClientService>();

		builder.Services.AddScoped<LoginViewModel>();
		builder.Services.AddScoped<HomeViewModel>();
		builder.Services.AddScoped<ActionViewModel>();

		builder.Services.AddMesClient("openmes-webapi");

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

		app.UseOutputCache();

		app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
