using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<PickingStatus>))]
public enum PickingStatus : byte
{
	/// <summary>No material has been picked yet.</summary>
	Pending = 0,
	/// <summary>Material has been partially picked.</summary>
	PartiallyPicked = 1,
	/// <summary>All required material has been picked.</summary>
	Completed = 9,
}
