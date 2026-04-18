using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient;
using OpenMES.WebClient.Models;
using OpenMES.WebClient.Services;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient.Components.Pages;

partial class Login(
	LoginViewModel model,
	ILogger<Login> logger,
	ProtectedLocalStorage LocalStore,
	IMessageService messageService,
	MesClient mesClient)
	: ClientComponentBase<LoginViewModel>(model, logger, messageService, mesClient)
{
	private async Task SignIn()
	{
		var login = new TerminalLoginDto
		{
			Name = ViewModel.UserName,
			Password = ViewModel.Password
		};
		await RunAsync(async ct =>
		{
			var result = await MesClient.Terminal.ConnectAsync(login, ct);
			if (!string.IsNullOrEmpty(result?.AuthToken))
			{
				// Store auth token
				await LocalStore.SetAsync("auth", result);
				MesClient.SetAuthToken(result.AuthToken);

				// WEB-003: Start session tracking
				var identity = new TerminalIdentity();
				identity.SetIdentity(result.Name ?? ViewModel.UserName, result.AuthToken);
				await SessionService.StartSessionAsync(identity.Name, identity.AuthToken);

				Navigation.NavigateTo("/");
			}
		});
	}
}
