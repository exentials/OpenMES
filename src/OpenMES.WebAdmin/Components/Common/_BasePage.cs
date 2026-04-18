using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.WebApiClient;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebAdmin.Components.Common;

public abstract class BasePage<TDto, TKey, TEditComponent>(string pagePath, ICrudApiService<TDto, TKey> apiService) : ComponentBase
	where TDto : class, IKey<TKey>
	where TEditComponent : IDialogContentComponent<TDto>
{
	[Inject] IDialogService DialogService { get; set; } = null!;
	[Inject] NavigationManager NavigationManager { get; set; } = null!;

	protected readonly PaginationState pagination = new() { ItemsPerPage = 15 };

	protected string PagePath { get; } = pagePath ?? throw new ArgumentNullException(nameof(pagePath));
	protected ICrudApiService<TDto, TKey> ApiService { get; } = apiService ?? throw new ArgumentNullException(nameof(apiService));

	/// <summary>
	/// ItemsProvider for FluentDataGrid server-side pagination.
	/// Called automatically by the grid whenever the page or sort changes.
	/// </summary>
	protected async ValueTask<GridItemsProviderResult<TDto>> ItemsProvider(
		GridItemsProviderRequest<TDto> request)
	{
		var page = request.StartIndex / pagination.ItemsPerPage;
		var result = await ApiService.ReadsAsync(page, pagination.ItemsPerPage);

		if (!result.Success || result.Data is null)
			return GridItemsProviderResult.From<TDto>(Array.Empty<TDto>(), 0);

		var paged = result.Data;
		return GridItemsProviderResult.From<TDto>(
			(paged.Items ?? Array.Empty<TDto>()).ToList(),
			paged.TotalCount);
	}

	/// <summary>Forces the grid to reload the current page (called after CUD operations).</summary>
	protected async Task RefreshAsync()
	{
		await pagination.SetTotalItemCountAsync(pagination.TotalItemCount ?? 0);
		await InvokeAsync(StateHasChanged);
	}

	protected async Task DeleteItem(TDto item)
	{
		if (item is null) return;

		var dialog = await DialogService.ShowConfirmationAsync(
			$"Are you sure you want to delete the item '{item}'?",
			"Yes", "No", "Delete item?");
		var result = await dialog.Result;
		if (result.Cancelled) return;

		try
		{
			var del = await ApiService.DeleteAsync(item.Id);
			if (!del.Success)
			{
				await DialogService.ShowErrorAsync("Error", del.ErrorMessage ?? "Failed to delete item.");
				return;
			}
			await RefreshAsync();
		}
		catch (Exception exc)
		{
			await DialogService.ShowErrorAsync("Error", exc.InnerException?.Message ?? exc.Message);
		}
	}

	protected async Task EditInDialog(TDto originalItem)
	{
		var parameters = new DialogParameters
		{
			Title = "Edit",
			PreventDismissOnOverlayClick = true,
			PreventScroll = true
		};
		var dialog = await DialogService.ShowDialogAsync<TEditComponent>(originalItem.DeepCopy(), parameters);
		var dialogResult = await dialog.Result;
		await HandleEditDialogResult(dialogResult, originalItem);
	}

	protected async Task EditInPanel(TDto originalItem)
	{
		DialogParameters<TDto> parameters = new()
		{
			Title = "Edit",
			Alignment = HorizontalAlignment.Right,
			PrimaryAction = "Ok",
			SecondaryAction = "Cancel"
		};
		var dialog = await DialogService.ShowPanelAsync<TEditComponent>(originalItem.DeepCopy(), parameters);
		var dialogResult = await dialog.Result;
		await HandleEditDialogResult(dialogResult, originalItem);
	}

	private async Task HandleEditDialogResult(DialogResult result, TDto originalItem)
	{
		if (result.Cancelled) return;
		if (result.Data is not TDto updatedItem) return;

		var upd = await ApiService.UpdateAsync(originalItem.Id, updatedItem);
		if (!upd.Success)
		{
			await DialogService.ShowErrorAsync("Error", upd.ErrorMessage ?? "Failed to update item.");
			return;
		}
		await RefreshAsync();
	}

	protected async Task AddInDialog()
	{
		var newItem = Activator.CreateInstance<TDto>();
		var parameters = new DialogParameters
		{
			Title = "Add",
			PreventDismissOnOverlayClick = true,
			PreventScroll = true
		};
		var dialog = await DialogService.ShowDialogAsync<TEditComponent>(newItem, parameters);
		var dialogResult = await dialog.Result;
		await HandleAddDialogResult(dialogResult);
	}

	private async Task HandleAddDialogResult(DialogResult result)
	{
		if (result.Cancelled) return;
		if (result.Data is not TDto newItem) return;

		var created = await ApiService.CreateAsync(newItem);
		if (!created.Success)
		{
			await DialogService.ShowErrorAsync("Error", created.ErrorMessage ?? "Failed to create item.");
			return;
		}
		await RefreshAsync();
	}

	protected void ShowItem(TDto item)
	{
		NavigationManager.NavigateTo($"/{PagePath}/{item.Id}");
	}
}
