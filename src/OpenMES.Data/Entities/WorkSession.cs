using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Data.Entities.Interfaces;
using OpenMES.Data.Interfaces;

namespace OpenMES.Data.Entities;

/// <summary>
/// Represents a period of work declared by an operator on a production order phase
/// on a specific machine.
///
/// A session has a type (<see cref="WorkSessionType"/>: Setup, Work, Wait, Rework)
/// and moves through two states: Open → Closed.
///
/// Lifecycle:
/// 1. Operator opens a session → Status = Open, EndTime = null.
/// 2. Operator closes explicitly → Status = Closed, EndTime = now,
///    AllocatedMinutes recomputed for all closed sessions on the same phase.
/// 3. Operator checks out → all open sessions are force-closed.
///
/// Multiple operators can work simultaneously on the same phase (each has their own
/// session). Whether an operator can have concurrent sessions on the same machine
/// is controlled by <see cref="Machine.AllowConcurrentSessions"/>.
///
/// <see cref="AllocatedMinutes"/> is computed at close time using the machine's
/// <see cref="Machine.TimeAllocationMode"/>:
/// - Uniform: total phase minutes / number of distinct operators.
/// - Proportional: each session's share of the total raw duration.
///
/// Sessions opened by an automatic machine controller have Source = "Machine".
///
/// ERP export:
/// <see cref="PhaseExternalId"/> is a snapshot of <see cref="ProductionOrderPhase.ExternalId"/>
/// copied at session creation time. It is the key the ERP uses to identify the phase.
/// When exported, the ERP returns <see cref="ExternalCounterId"/> to confirm acquisition.
/// Corrections to already-exported records are handled via the reversal pattern:
/// a mirror record with <see cref="IsReversal"/> = true and negated <see cref="AllocatedMinutes"/>
/// is created, followed by a new corrected record.
/// </summary>
[Table(nameof(WorkSession))]
[PrimaryKey(nameof(Id))]
public class WorkSession : IKey<int>, IBaseDates, IDtoAdapter<WorkSession, WorkSessionDto>
{
	public int Id { get; set; }

	/// <summary>FK to the operator who opened this session.</summary>
	public int OperatorId { get; set; }

	/// <summary>FK to the production order phase being worked on.</summary>
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the machine on which the work is performed.</summary>
	public int MachineId { get; set; }

	/// <summary>Type of activity: Setup, Work, Wait, or Rework.</summary>
	public WorkSessionType SessionType { get; set; }

	/// <summary>Lifecycle state: Open (active) or Closed (ended).</summary>
	public WorkSessionStatus Status { get; set; } = WorkSessionStatus.Open;

	/// <summary>When the session started.</summary>
	public DateTimeOffset StartTime { get; set; }

	/// <summary>When the session ended. Null while the session is Open.</summary>
	public DateTimeOffset? EndTime { get; set; }

	/// <summary>
	/// Minutes attributed to this session after applying the machine's time
	/// allocation rule. Computed when the session is closed and recomputed
	/// whenever another session on the same phase is closed.
	/// Zero while the session is Open.
	/// </summary>
	[Precision(9, 3)]
	public decimal AllocatedMinutes { get; set; }

	/// <summary>
	/// Origin: "Manual" (operator via terminal) or "Machine" (automatic controller).
	/// </summary>
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>Optional free-text note for this session.</summary>
	[StringLength(500)]
	public string? Notes { get; set; }

	// ── ERP export fields ─────────────────────────────────────────────────────

	/// <summary>
	/// Snapshot of <see cref="ProductionOrderPhase.ExternalId"/> at session creation time.
	/// This is the confirmation number the ERP assigned to the phase at import.
	/// Included in every export so the ERP can identify which phase the data belongs to.
	/// Copied once at creation and never updated — preserves the value that was
	/// valid when the session was recorded, even if the phase is later modified.
	/// </summary>
	[StringLength(30)]
	public string? PhaseExternalId { get; set; }

	/// <summary>
	/// Counter or ID returned by the ERP when it successfully acquires this record.
	/// Null until the record has been exported and confirmed by the ERP.
	/// Once set, the record is considered "exported" and corrections must use the
	/// reversal pattern instead of direct modification.
	/// </summary>
	[StringLength(50)]
	public string? ExternalCounterId { get; set; }

	/// <summary>Timestamp when this record was sent to the ERP. Null if not yet exported.</summary>
	public DateTimeOffset? ErpExportedAt { get; set; }

	/// <summary>
	/// When true, this record is a reversal (storno) of a previously exported record.
	/// The <see cref="AllocatedMinutes"/> value is negative to cancel the original.
	/// <see cref="ReversalOfId"/> identifies the original record being reversed.
	/// </summary>
	public bool IsReversal { get; set; }

	/// <summary>
	/// FK to the original WorkSession that this record reverses.
	/// Populated only when <see cref="IsReversal"/> = true.
	/// </summary>
	public int? ReversalOfId { get; set; }

	/// <summary>
	/// FK to the reversal WorkSession that has cancelled this record.
	/// Populated on the original record after a reversal is created.
	/// Allows tracing the full reversal chain in both directions.
	/// </summary>
	public int? ReversedById { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }

	[ForeignKey(nameof(OperatorId))]
	public virtual Operator Operator { get; set; } = null!;

	[ForeignKey(nameof(ProductionOrderPhaseId))]
	[InverseProperty(nameof(ProductionOrderPhase.WorkSessions))]
	public virtual ProductionOrderPhase ProductionOrderPhase { get; set; } = null!;

	[ForeignKey(nameof(MachineId))]
	public virtual Machine Machine { get; set; } = null!;

	public static WorkSessionDto AsDto(WorkSession entity) => new()
	{
		Id = entity.Id,
		OperatorId = entity.OperatorId,
		ProductionOrderPhaseId = entity.ProductionOrderPhaseId,
		MachineId = entity.MachineId,
		SessionType = entity.SessionType,
		Status = entity.Status,
		StartTime = entity.StartTime,
		EndTime = entity.EndTime,
		AllocatedMinutes = entity.AllocatedMinutes,
		Source = entity.Source,
		Notes = entity.Notes,
		PhaseExternalId = entity.PhaseExternalId,
		ExternalCounterId = entity.ExternalCounterId,
		ErpExportedAt = entity.ErpExportedAt,
		IsReversal = entity.IsReversal,
		ReversalOfId = entity.ReversalOfId,
		ReversedById = entity.ReversedById,
		CreatedAt = entity.CreatedAt,
		UpdatedAt = entity.UpdatedAt,
		OperatorName = entity.Operator?.Name,
		PhaseNumber = entity.ProductionOrderPhase?.PhaseNumber,
		MachineCode = entity.Machine?.Code,
	};

	public static WorkSession AsEntity(WorkSessionDto dto) => new()
	{
		Id = dto.Id,
		OperatorId = dto.OperatorId,
		ProductionOrderPhaseId = dto.ProductionOrderPhaseId,
		MachineId = dto.MachineId,
		SessionType = dto.SessionType,
		Status = dto.Status,
		StartTime = dto.StartTime,
		EndTime = dto.EndTime,
		Source = dto.Source,
		Notes = dto.Notes,
		PhaseExternalId = dto.PhaseExternalId,
		ExternalCounterId = dto.ExternalCounterId,
		ErpExportedAt = dto.ErpExportedAt,
		IsReversal = dto.IsReversal,
		ReversalOfId = dto.ReversalOfId,
		ReversedById = dto.ReversedById,
	};
}
