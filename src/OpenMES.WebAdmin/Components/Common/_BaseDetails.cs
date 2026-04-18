using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebAdmin.Components.Common
{
	public abstract class BaseDetails<TDto, TKey> : ComponentBase 
		where TDto : class, IKey<TKey>
		where TKey : notnull
	{
		[Inject] IDialogService DialogService { get; set; } = null!;
		[Inject] NavigationManager NavigationManager { get; set; } = null!;

		[Parameter] public TKey ItemId { get; set; } = default!;

		protected TDto? Model;

		protected string PagePath { get; }
		protected ICrudApiService<TDto,TKey> ApiService { get; }

		public BaseDetails(string pagePath, ICrudApiService<TDto,TKey> apiService)
		{
			PagePath = pagePath ?? throw new ArgumentNullException(nameof(pagePath), "PagePath cannot be null");
			ApiService = apiService ?? throw new ArgumentNullException(nameof(apiService), "ApiService cannot be null");
		}

		protected override async Task OnInitializedAsync()
		{
			await LoadData();
		}

		private async Task LoadData()
		{
			var result = await ApiService.ReadAsync(ItemId);
			if (!result.Success)
			{
				await DialogService.ShowErrorAsync("Error", result.ErrorMessage ?? "Failed to load item.");
				return;
			}

			Model = result.Data;
		}

		protected async Task Delete()
		{
			if (Model is null)
			{
				return;
			}

			DialogParameters dialogParameters = new()
			{
				Title = "Delete",
				PreventDismissOnOverlayClick = true,
				PreventScroll = true
			};

			var dialog = await DialogService.ShowConfirmationAsync(
				$"Are you sure you want to delete {Model}?",
				"Yes",
				"No",
				"Delete?");

			var result = await dialog.Result;
			if (!result.Cancelled)
			{
				var del = await ApiService.DeleteAsync(Model.Id);
				if (!del.Success)
				{
					await DialogService.ShowErrorAsync("Error", del.ErrorMessage ?? "Failed to delete item.");
					return;
				}
				NavigationManager.NavigateTo($"/{PagePath}");
			}
		}
	}
}
