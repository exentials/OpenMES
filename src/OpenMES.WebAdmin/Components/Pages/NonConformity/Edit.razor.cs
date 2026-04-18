using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.NonConformity;

partial class Edit : BaseEdit<NonConformityDto>
{
	// OpenedAt is set server-side on creation; exposed here for editing existing records
	protected DateTime? OpenedAt
	{
		get => Content.OpenedAt == default ? null : Content.OpenedAt.LocalDateTime;
		set => Content.OpenedAt = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: DateTimeOffset.UtcNow;
	}

	protected DateTime? ClosedAt
	{
		get => Content.ClosedAt.HasValue ? Content.ClosedAt.Value.LocalDateTime : null;
		set => Content.ClosedAt = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: null;
	}
}
