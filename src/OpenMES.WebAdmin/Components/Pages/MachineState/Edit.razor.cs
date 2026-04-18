using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.MachineState;

partial class Edit : BaseEdit<MachineStateDto>
{
	protected DateTime? EventDate
	{
		get => Content.EventTime == default ? null : Content.EventTime.LocalDateTime;
		set => Content.EventTime = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: DateTimeOffset.UtcNow;
	}
}
