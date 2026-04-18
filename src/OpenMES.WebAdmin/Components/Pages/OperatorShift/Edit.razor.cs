using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.OperatorShift;

partial class Edit : BaseEdit<OperatorShiftDto>
{
	protected DateTime? EventDate
	{
		get => Content.EventTime == default ? null : Content.EventTime.LocalDateTime;
		set => Content.EventTime = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: DateTimeOffset.UtcNow;
	}
}
