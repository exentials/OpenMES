using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<NonConformitySeverity>))]
public enum NonConformitySeverity : byte
{
	/// <summary>Minor issue with negligible impact on quality or safety.</summary>
	Low = 0,
	/// <summary>Issue with limited impact, requires monitoring.</summary>
	Medium = 1,
	/// <summary>Significant issue requiring prompt corrective action.</summary>
	High = 2,
	/// <summary>Severe issue that blocks shipment or requires immediate escalation.</summary>
	Critical = 3,
}
