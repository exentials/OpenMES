using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class OperatorDto : IKey<int>
{
	/// <summary>Unique identifier of the operator.</summary>
	public int Id { get; set; }

	/// <summary>Company employee number, used as the primary login identifier.</summary>
	[Required, StringLength(10)]
	[Display(Name = nameof(DtoResources.Operator_EmployeeNumber), ResourceType = typeof(DtoResources))]
	public string EmployeeNumber { get; set; } = null!;

	/// <summary>Full name of the operator.</summary>
	[Required, StringLength(40)]
	[Display(Name = nameof(DtoResources.Operator_Name), ResourceType = typeof(DtoResources))]
	public string Name { get; set; } = null!;

	/// <summary>Badge or RFID tag code used for physical access and terminal login.</summary>
	[StringLength(10)]
	[Display(Name = nameof(DtoResources.Operator_Badge), ResourceType = typeof(DtoResources))]
	public string Badge { get; set; } = null!;

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

}
