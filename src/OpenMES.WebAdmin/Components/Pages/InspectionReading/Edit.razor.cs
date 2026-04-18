using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.InspectionReading;

partial class Edit : BaseEdit<InspectionReadingDto>
{
	// Bridge properties for DateTimeOffset ↔ DateTime? (FluentDatePicker)
	protected DateTime? ReadingDate
	{
		get => Content.ReadingDate == default ? null : Content.ReadingDate.LocalDateTime;
		set => Content.ReadingDate = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: default;
	}

	// FluentCheckbox requires bool, not bool?
	protected bool BooleanValue
	{
		get => Content.BooleanValue ?? false;
		set => Content.BooleanValue = value;
	}
}
