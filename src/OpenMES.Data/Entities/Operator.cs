using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a shop floor operator (worker) who can interact with the system
/// via a terminal or badge reader.
///
/// An operator can:
/// - Check in and out of shifts via <see cref="OperatorShift"/> events.
/// - Open and close work sessions (<see cref="WorkSession"/>) on production order
///   phases, declaring setup, work, wait, or rework activity.
/// - Declare machine state changes on behalf of a machine.
/// - Record quality readings and production declarations.
///
/// An operator identified by their <see cref="Badge"/> code can authenticate
/// on a shop floor terminal without a password (badge scan login).
/// The <see cref="Disabled"/> flag deactivates an operator without deleting
/// their historical data.
/// </summary>
[Table(nameof(Operator))]
[PrimaryKey(nameof(Id))]
public class Operator : IKey<int>, IBaseDates, IDtoAdapter<Operator, OperatorDto>
{
	[Key]
	public int Id { get; set; }
	[Required]
	public int PlantId { get; set; }
	[StringLength(40)]
	[Required]
	public string Name { get; set; } = null!;
	[StringLength(10)]
	public string EmployeeNumber { get; set; } = null!;
	[StringLength(10)]
	public string Badge { get; set; } = null!;
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	public bool Disabled { get; set; }


	[ForeignKey(nameof(PlantId))]
	[InverseProperty(nameof(Plant.Operators))]
	public virtual Plant Plant { get; set; } = null!;

	public static OperatorDto AsDto(Operator entity) => new()
	{
		Id = entity.Id,
		Name = entity.Name,
		EmployeeNumber = entity.EmployeeNumber,
		Badge = entity.Badge,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt
	};

	public static Operator AsEntity(OperatorDto dto) => new()
	{
		Id = dto.Id,
		Name = dto.Name,
		EmployeeNumber = dto.EmployeeNumber,
		Badge = dto.Badge,
		CreatedAt = dto.CreatedAt,
		UpdatedAt = dto.UpdatedAt
	};

}
