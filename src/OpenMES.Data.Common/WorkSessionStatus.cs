using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Represents the lifecycle state of a WorkSession.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkSessionStatus>))]
public enum WorkSessionStatus : byte
{
	/// <summary>
	/// The session is currently active. EndTime is null and
	/// AllocatedMinutes has not yet been computed.
	/// </summary>
	Open = 0,

	/// <summary>
	/// The session has ended. EndTime and AllocatedMinutes are set.
	/// The record is immutable after reaching this state.
	/// </summary>
	Closed = 9,
}
