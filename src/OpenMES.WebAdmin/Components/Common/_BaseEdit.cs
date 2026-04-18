using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace OpenMES.WebAdmin.Components.Common;

public class BaseEdit<TDto> : ComponentBase, IDialogContentComponent<TDto> where TDto : class
{
	[Parameter]
	public TDto Content { get; set; } = default!;

	[CascadingParameter]
	public FluentDialog Dialog { get; set; } = default!;

	protected async Task SaveAsync()
	{
		await Dialog.CloseAsync(Content);
	}

	protected async Task CancelAsync()
	{
		await Dialog.CancelAsync();
	}
}
