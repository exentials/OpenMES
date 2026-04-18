using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using OpenMES.Data.Dtos.Resources;
using OpenMES.Localization.Resources;
using OpenMES.WebApiClient;
using OpenMES.WebClient.Models;
using OpenMES.WebClient.Services;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient.Components.Pages;

public abstract class ClientComponentBase<Model>(
	Model model,
	ILogger logger,
	IMessageService messageService,
	MesClient mesClient)
	: ComponentBase, IDisposable
	where Model : ViewModelBase
{
	[Inject] protected NavigationManager Navigation { get; set; } = null!;
	[Inject] protected IErrorService ErrorService { get; set; } = null!;
	[Inject] protected ISessionService SessionService { get; set; } = null!;
	[Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
	[Inject] ProtectedLocalStorage LocalStore { get; set; } = null!;

	protected Model ViewModel { get; } = model;
	protected ILogger Logger { get; } = logger;
	protected IMessageService Message { get; } = messageService;
	protected MesClient MesClient { get; set; } = mesClient;

	protected CancellationTokenSource? CancellationTokenSource { get; private set; } = new CancellationTokenSource();
	protected CancellationToken CancellationToken => CancellationTokenSource?.Token ?? default;

	protected bool IsLoading { get; private set; }
	protected bool IsCancelled { get; private set; }

	protected TerminalIdentity? CurrentIdentity { get; private set; }
	private IJSObjectReference? _jsModule;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			var result = await LocalStore.GetAsync<TerminalIdentity>("auth");
			if (result.Success && result.Value is not null)
			{
				CurrentIdentity = result.Value;
			}
			if (CurrentIdentity?.IsAuthenticated == true)
			{
				MesClient.SetAuthToken(CurrentIdentity.AuthToken);

				// WEB-003: Restore session state from stored identity
				await SessionService.StartSessionAsync(CurrentIdentity.Name, CurrentIdentity.AuthToken);
			}

			ViewModel.StateChanged += StateHasChanged;

			// WEB-003: Initialize JS activity tracking
			try
			{
				_jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
					"import", "./js/session-activity-interop.js");

				// Start activity tracking (pass DotNet reference for callbacks)
				var objRef = DotNetObjectReference.Create(SessionService);
				await _jsModule.InvokeVoidAsync("initializeActivityTracking", objRef);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to initialize JS activity tracking");
			}
		}
	}

	protected async Task RunAsync(Func<CancellationToken, Task> operation, [CallerMemberName] string? callerName = null)
	{
		IsLoading = true;
		IsCancelled = false;
		StateHasChanged();
		try
		{
			// Clear any previous errors before starting new operation
			ErrorService.ClearError();
			await operation(CancellationToken);
		}
		catch (OperationCanceledException)
		{
			IsCancelled = true;
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarning("Operation canceled {callerName}", callerName);
			}
		}
		catch (HttpRequestException ex)
		{
			// WEB-002: Handle HTTP errors (network, API unavailable, etc.)
			var errorMsg = ex.StatusCode switch
			{
				System.Net.HttpStatusCode.Unauthorized => UiResources.Error_HttpUnauthorized,
				System.Net.HttpStatusCode.Forbidden => UiResources.Error_HttpForbidden,
				System.Net.HttpStatusCode.NotFound => UiResources.Error_HttpNotFound,
				System.Net.HttpStatusCode.BadRequest => $"{UiResources.Error_HttpBadRequestPrefix}: {ex.Message}",
				System.Net.HttpStatusCode.InternalServerError => UiResources.Error_HttpInternalServer,
				System.Net.HttpStatusCode.ServiceUnavailable => UiResources.Error_HttpServiceUnavailable,
				_ => $"{UiResources.Error_HttpCommunicationPrefix}: {ex.Message}"
			};

			ErrorService.AddError(errorMsg, ErrorSeverity.Error);

			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogError(ex, "HTTP Error in {callerName}: {message}", callerName, ex.Message);
			}
		}
		catch (Exception ex)
		{
			// WEB-002: Handle all other exceptions via ErrorService
			ErrorService.AddError($"{UiResources.Error_GenericOccurredPrefix}: {ex.Message}", ErrorSeverity.Error);

			if (Logger.IsEnabled(LogLevel.Error))
			{
				Logger.LogError(ex, "Error in {callerName}: {message}", callerName, ex.Message);
			}
		}
		finally
		{
			IsLoading = false;
			StateHasChanged();
		}
	}


	public void CancelOperations()
	{
		if (CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
		{
			CancellationTokenSource.Cancel();
			CancellationTokenSource.Dispose();
			CancellationTokenSource = new CancellationTokenSource();
		}
	}

	/// <summary>
	/// WEB-004: Logout handler - clears session and redirects to login.
	/// </summary>
	public async Task Logout()
	{
		try
		{
			// End session
			await SessionService.EndSessionAsync();

			// Clear stored auth
			await LocalStore.DeleteAsync("auth");

			// Redirect to login
			Navigation.NavigateTo("/login", forceLoad: true);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error during logout");
			Navigation.NavigateTo("/login", forceLoad: true);
		}
	}

	public void Dispose()
	{
		if (CancellationTokenSource != null)
		{
			CancellationTokenSource.Cancel();
			CancellationTokenSource.Dispose();
			CancellationTokenSource = null;
		}
		GC.SuppressFinalize(this);
	}
}
