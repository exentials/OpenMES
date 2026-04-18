using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<MachineStopCategory>))]
public enum MachineStopCategory : byte
{
	/// <summary>Unplanned stop due to a machine failure.</summary>
	Breakdown = 0,
	/// <summary>Planned stop for changeover or tooling setup.</summary>
	Setup = 1,
	/// <summary>Planned stop for scheduled or preventive maintenance.</summary>
	Maintenance = 2,
	/// <summary>Stop caused by missing or delayed materials.</summary>
	MaterialWaiting = 3,
	/// <summary>Stop due to organizational or logistic reasons.</summary>
	Organizational = 4,
	/// <summary>Any other stop not covered by the above categories.</summary>
	Other = 9,
}
