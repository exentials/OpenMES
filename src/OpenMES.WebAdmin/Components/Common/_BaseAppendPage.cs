using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebAdmin.Components.Common;

/// <summary>
/// Base page for append-only entities (Create + Read only — no Update or Delete).
/// Uses ItemsProvider for server-side pagination.
/// </summary>
public abstract class BaseAppendPage<TDto, TEditComponent>(IAppendApiService<TDto, int> apiService)
	: ComponentBase
	where TDto : class, IKey<int>
	where TEditComponent : IDialogContentComponent<TDto>
{
	[Inject] IDialogService DialogService { get; set; } = null!;

	protected readonly PaginationState pagination = new() { ItemsPerPage = 15 };

	protected async ValueTask<GridItemsProviderResult<TDto>> ItemsProvider(
		GridItemsProviderRequest<TDto> request)
	{
		var page = request.StartIndex / pagination.ItemsPerPage;
		var result = await apiService.ReadsAsync(page, pagination.ItemsPerPage);

		if (!result.Success || result.Data is null)
			return GridItemsProviderResult.From<TDto>(Array.Empty<TDto>(), 0);

		var paged = result.Data;
		return GridItemsProviderResult.From<TDto>(
			(paged.Items ?? Array.Empty<TDto>()).ToList(),
			paged.TotalCount);
	}

	protected async Task RefreshAsync()
	{
		await pagination.SetTotalItemCountAsync(pagination.TotalItemCount ?? 0);
		await InvokeAsync(StateHasChanged);
	}

	protected async Task AddInDialog()
	{
		var newItem = Activator.CreateInstance<TDto>();
		var parameters = new DialogParameters
		{
			Title = "Register",
			PreventDismissOnOverlayClick = true,
			PreventScroll = true
		};
		var dialog = await DialogService.ShowDialogAsync<TEditComponent>(newItem, parameters);
		var result = await dialog.Result;
		if (result.Cancelled || result.Data is not TDto created) return;

		var created2 = await apiService.CreateAsync(created);
		if (!created2.Success)
		{
			await DialogService.ShowErrorAsync("Error", created2.ErrorMessage ?? "Failed to register item.");
			return;
		}
		await RefreshAsync();
	}
}
