using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.MachineStop;

partial class Edit : BaseEdit<MachineStopDto>
{
	// FluentDatePicker works with DateTime? — bridge properties for DateTimeOffset fields

	protected DateTime? StartDate
	{
		get => Content.StartDate == default ? null : Content.StartDate.LocalDateTime;
		set => Content.StartDate = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: default;
	}

	protected DateTime? EndDate
	{
		get => Content.EndDate.HasValue ? Content.EndDate.Value.LocalDateTime : null;
		set => Content.EndDate = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: null;
	}
}
