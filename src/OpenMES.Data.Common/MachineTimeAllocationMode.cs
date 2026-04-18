using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

/// <summary>
/// Defines how accumulated work minutes are distributed among operators
/// when more than one person has worked on the same production order phase.
/// Configured per machine via <c>Machine.TimeAllocationMode</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MachineTimeAllocationMode>))]
public enum MachineTimeAllocationMode : byte
{
	/// <summary>
	/// Total phase minutes are divided equally among all operators
	/// who had at least one closed WorkSession on the phase,
	/// regardless of how long each one actually worked.
	/// </summary>
	Uniform = 0,

	/// <summary>
	/// Each operator's AllocatedMinutes equals their own raw session duration.
	/// The share is proportional to actual time spent relative to the total.
	/// </summary>
	Proportional = 1,
}
