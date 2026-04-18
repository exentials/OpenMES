using System.Text.Json.Serialization;

namespace OpenMES.Data.Common;

[JsonConverter(typeof(JsonStringEnumConverter<InspectionResult>))]
public enum InspectionResult : byte
{
	/// <summary>The measured value is within the specified tolerances.</summary>
	Conforming = 0,
	/// <summary>The measured value is outside the specified tolerances.</summary>
	NonConforming = 1,
	/// <summary>The reading requires further analysis before a verdict can be issued.</summary>
	ToVerify = 2,
}
