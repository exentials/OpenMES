using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Classifies a presence event recorded for a shop floor operator.
/// The full shift timeline is reconstructed by reading all events in
/// chronological order for a given operator and day.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OperatorEventType>))]
public enum OperatorEventType : byte
{
	/// <summary>
	/// Operator arrives and begins their shift.
	/// Required before any WorkSession can be opened.
	/// </summary>
	CheckIn = 0,

	/// <summary>
	/// Operator leaves. All open WorkSessions for this operator
	/// are force-closed with EndTime = CheckOut.EventTime.
	/// </summary>
	CheckOut = 1,

	/// <summary>
	/// Operator starts a break. Open WorkSessions remain open but
	/// time stops accumulating until BreakEnd is recorded.
	/// </summary>
	BreakStart = 2,

	/// <summary>
	/// Operator returns from break. Time resumes on open WorkSessions.
	/// </summary>
	BreakEnd = 3,
}
