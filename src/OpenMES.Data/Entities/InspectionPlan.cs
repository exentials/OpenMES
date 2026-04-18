using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a quality inspection plan for a specific part number.
/// An inspection plan defines the set of characteristics (<see cref="InspectionPoint"/>)
/// that must be measured or verified during production of that part.
///
/// Plans are versioned: each revision increments <see cref="Version"/> while
/// preserving the previous version for historical traceability. The alternate key
/// (Code, Version) ensures uniqueness.
///
/// During production, operators record <see cref="InspectionReading"/> values
/// against the points defined in the active plan for the phase's part number.
/// </summary>
[Table(nameof(InspectionPlan))]
[PrimaryKey(nameof(Id))]
public class InspectionPlan : IKey<int>, IBaseDates, IDtoAdapter<InspectionPlan, InspectionPlanDto>
{
	public int Id { get; set; }
	[Required, StringLength(20)]
	public string Code { get; set; } = null!;
	[StringLength(80)]
	public string Description { get; set; } = null!;
	[StringLength(20)]
	public string PartNumber { get; set; } = null!;
	public int Version { get; set; } = 1;
	public bool Disabled { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[InverseProperty(nameof(InspectionPoint.InspectionPlan))]
	public virtual ICollection<InspectionPoint> InspectionPoints { get; set; } = [];

	public static InspectionPlanDto AsDto(InspectionPlan entity) => new()
	{
		Id = entity.Id,
		Code = entity.Code,
		Description = entity.Description,
		PartNumber = entity.PartNumber,
		Version = entity.Version,
		Disabled = entity.Disabled,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		InspectionPoints = entity.InspectionPoints?.Select(InspectionPoint.AsDto).ToList() ?? []
	};

	public static InspectionPlan AsEntity(InspectionPlanDto dto) => new()
	{
		Id = dto.Id,
		Code = dto.Code,
		Description = dto.Description,
		PartNumber = dto.PartNumber,
		Version = dto.Version,
		Disabled = dto.Disabled
	};

}
