using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages.PhasePickingList;

partial class Details(MesClient mesClient) : BaseDetails<PhasePickingListDto, int>("phasepickinglist", mesClient.PhasePickingList)
{
	protected List<PhasePickingItemDto> PickingItems { get; set; } = [];

	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		await LoadPickingItems();
	}

	private async Task LoadPickingItems()
	{
		if (Model is null) return;
		// Load all picking items for this picking list line (first page, large page size)
		var result = await mesClient.PhasePickingItem.ReadsAsync(0, 500);
		if (result.Success && result.Data is not null)
		{
			PickingItems = (result.Data.Items ?? [])
				.Where(x => x.PhasePickingListId == Model.Id)
				.ToList();
		}
	}
}
