using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using OpenMES.Data.Dtos;
using OpenMES.Data.Dtos.Resources;
using OpenMES.Localization.Resources;
using OpenMES.WebClient.Services;

namespace OpenMES.WebClient.Components.Layout;

partial class MainLayout
{
	[Inject] NavigationManager Navigation { get; set; } = null!;
	[Inject] ProtectedLocalStorage LocalStore { get; set; } = null!;
	[Inject] ISessionService? SessionService { get; set; }
	[Inject] IJSRuntime? JSRuntime { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			var result = await LocalStore.GetAsync<TerminalLoginResultDto>("auth");
			if (result.Success && result.Value is not null)
			{
				TerminalLoginResultDto auth = result.Value;
				if (!Navigation.Uri.EndsWith('/'))
				{
					Navigation.NavigateTo("/", true);
				}
			}
			else
			{
				if (!Navigation.Uri.EndsWith("/login"))
				{
					Navigation.NavigateTo("/login", true);
				}
			}

		}
	}

	/// <summary>
	/// WEB-004: Logout handler - clears session and redirects to login.
	/// </summary>
	private async Task Logout()
	{
		try
		{
			// Confirm logout
			if (JSRuntime != null)
			{
				var confirmed = await JSRuntime.InvokeAsync<bool>(
					"confirm",
					UiResources.Label_LogoutConfirm
				);

				if (!confirmed)
					return;
			}

			// End session
			if (SessionService != null)
			{
				await SessionService.EndSessionAsync();
			}

			// Clear stored auth
			await LocalStore.DeleteAsync("auth");

			// Redirect to login
			Navigation.NavigateTo("/login", true);
		}
		catch (Exception)
		{
			// If error, still try to navigate
			Navigation.NavigateTo("/login", true);
		}
	}
}
