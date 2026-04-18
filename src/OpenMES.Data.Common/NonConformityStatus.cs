using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<NonConformityStatus>))]
public enum NonConformityStatus : byte
{
	/// <summary>Non-conformity has been raised and is awaiting assignment.</summary>
	Open = 0,
	/// <summary>Corrective action is in progress.</summary>
	InProgress = 1,
	/// <summary>Non-conformity has been resolved and verified.</summary>
	Closed = 9,
}
