using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.WebApiClient;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient.Components.Pages;

partial class Home(
	HomeViewModel model,
	ILogger<Home> logger,
	IMessageService messageService,
	MesClient mesClient)
	: ClientComponentBase<HomeViewModel>(model, logger, messageService, mesClient), IDisposable
{
	private Timer? _refreshTimer;
	private bool _refreshRunning;
	private bool IsOperatorDeclarationOpen { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender && CurrentIdentity?.IsAuthenticated == true)
		{
			await LoadHomeDataAsync();
			_refreshTimer = new Timer(async _ => await RefreshTickAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
		}
	}

	private async Task RefreshTickAsync()
	{
		if (_refreshRunning)
		{
			return;
		}

		_refreshRunning = true;
		try
		{
			await InvokeAsync(async () => await LoadHomeDataAsync());
		}
		finally
		{
			_refreshRunning = false;
		}
	}

	private async Task LoadHomeDataAsync()
	{
		await RunAsync(async ct =>
		{
			var machinesTask = MesClient.Terminal.GetMachinesAsync(CurrentIdentity!.Name, ct);
			var statesTask = MesClient.MachineState.GetAllCurrentAsync(ct);
			var sessionsTask = MesClient.WorkSession.GetOpenAsync(ct);

			await Task.WhenAll(machinesTask, statesTask, sessionsTask);

			ViewModel.Machines = machinesTask.Result;
			ViewModel.MachineStates = statesTask.Result.ToDictionary(s => s.MachineId);
			ViewModel.OpenSessions = sessionsTask.Result
				.GroupBy(s => s.MachineId)
				.ToDictionary(g => g.Key, g => g.First());
		});
	}

	private Task OpenOperatorDeclarationAsync()
	{
		IsOperatorDeclarationOpen = true;
		return Task.CompletedTask;
	}

	private Task CloseOperatorDeclarationAsync()
	{
		IsOperatorDeclarationOpen = false;
		return Task.CompletedTask;
	}

	private async Task OnOperatorShiftChangedAsync()
	{
		await LoadHomeDataAsync();
	}

	private async Task RefreshAsync() => await LoadHomeDataAsync();

	public new void Dispose()
	{
		_refreshTimer?.Dispose();
		base.Dispose();
	}
}
