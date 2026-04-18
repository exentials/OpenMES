using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Classifies the type of activity being performed during a WorkSession.
/// The type drives validation rules — for example, a Work session cannot
/// be opened on a machine that is currently in Setup state.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkSessionType>))]
public enum WorkSessionType : byte
{
	/// <summary>
	/// Preparation of machine, tooling, or fixtures before production starts.
	/// Can be opened when the machine is in Setup or Idle state.
	/// </summary>
	Setup = 0,

	/// <summary>
	/// Active production on a production order phase.
	/// Cannot be opened when the machine is in Setup, Stopped, or Maintenance state.
	/// </summary>
	Work = 1,

	/// <summary>
	/// Operator is waiting — for material, instructions, a preceding operation,
	/// or a machine to become available.
	/// </summary>
	Wait = 2,

	/// <summary>
	/// Corrective work on pieces that did not pass quality control.
	/// Treated as a separate activity type for time tracking and OEE reporting.
	/// </summary>
	Rework = 3,

	/// <summary>Terminal value — reserved for future use.</summary>
	Other = 9,
}
