using OpenMES.Data.Dtos;
using OpenMES.WebAdmin.Components.Common;

namespace OpenMES.WebAdmin.Components.Pages.StockMovement;

partial class Edit : BaseEdit<StockMovementDto>
{
	protected DateTime? MovementDate
	{
		get => Content.MovementDate == default ? null : Content.MovementDate.LocalDateTime;
		set => Content.MovementDate = value.HasValue
			? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
			: default;
	}
}
