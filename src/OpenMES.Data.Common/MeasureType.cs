using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<MeasureType>))]
public enum MeasureType : byte
{
	/// <summary>Measurement expressed as a numeric value (e.g. mm, kg, °C).</summary>
	Numeric = 0,
	/// <summary>Measurement expressed as a pass/fail boolean check.</summary>
	Boolean = 1,
	/// <summary>Measurement expressed as free text or a coded value.</summary>
	Text = 2,
}
