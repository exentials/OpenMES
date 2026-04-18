using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Resources;
using System.ComponentModel.DataAnnotations;

namespace OpenMES.Data.Dtos;

public class WorkSessionDto : IKey<int>
{
	/// <summary>Unique identifier of the work session.</summary>
	public int Id { get; set; }

	/// <summary>FK to the operator who opened this session.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_OperatorId), ResourceType = typeof(DtoResources))]
	public int OperatorId { get; set; }

	/// <summary>FK to the production order phase being worked on.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_ProductionOrderPhaseId), ResourceType = typeof(DtoResources))]
	public int ProductionOrderPhaseId { get; set; }

	/// <summary>FK to the machine on which the work is performed.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_MachineId), ResourceType = typeof(DtoResources))]
	public int MachineId { get; set; }

	/// <summary>Type of activity: Setup, Work, Wait, or Rework.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_SessionType), ResourceType = typeof(DtoResources))]
	public WorkSessionType SessionType { get; set; }

	/// <summary>Lifecycle state: Open or Closed.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_Status), ResourceType = typeof(DtoResources))]
	public WorkSessionStatus Status { get; set; }

	/// <summary>When the session started.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_StartTime), ResourceType = typeof(DtoResources))]
	public DateTimeOffset StartTime { get; set; }

	/// <summary>When the session ended. Null while Open.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_EndTime), ResourceType = typeof(DtoResources))]
	public DateTimeOffset? EndTime { get; set; }

	/// <summary>Minutes allocated to this session after time distribution. Zero while Open.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_AllocatedMinutes), ResourceType = typeof(DtoResources))]
	public decimal AllocatedMinutes { get; set; }

	/// <summary>Origin: Manual or Machine.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_Source), ResourceType = typeof(DtoResources))]
	[StringLength(20)]
	public string Source { get; set; } = "Manual";

	/// <summary>Optional free-text note.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_Notes), ResourceType = typeof(DtoResources))]
	[StringLength(500)]
	public string? Notes { get; set; }

	// ── ERP export ────────────────────────────────────────────────────────────

	/// <summary>Snapshot of ProductionOrderPhase.ExternalId at session creation. Used as the phase key in ERP export.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_PhaseExternalId), ResourceType = typeof(DtoResources))]
	[StringLength(30)]
	public string? PhaseExternalId { get; set; }

	/// <summary>Counter/ID returned by the ERP confirming acquisition. Null = not yet exported.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_ExternalCounterId), ResourceType = typeof(DtoResources))]
	[StringLength(50)]
	public string? ExternalCounterId { get; set; }

	/// <summary>When this record was sent to the ERP. Null if not yet exported.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_ErpExportedAt), ResourceType = typeof(DtoResources))]
	public DateTimeOffset? ErpExportedAt { get; set; }

	/// <summary>True if this record is a reversal (storno) with negated AllocatedMinutes.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_IsReversal), ResourceType = typeof(DtoResources))]
	public bool IsReversal { get; set; }

	/// <summary>FK to the original session this record reverses. Populated only when IsReversal = true.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_ReversalOfId), ResourceType = typeof(DtoResources))]
	public int? ReversalOfId { get; set; }

	/// <summary>FK to the reversal session that cancelled this record. Populated on the original after reversal.</summary>
	[Display(Name = nameof(DtoResources.WorkSession_ReversedById), ResourceType = typeof(DtoResources))]
	public int? ReversedById { get; set; }

	/// <summary>Timestamp of record creation (UTC).</summary>
	public DateTimeOffset CreatedAt { get; set; }

	/// <summary>Timestamp of last record update (UTC).</summary>
	public DateTimeOffset UpdatedAt { get; set; }

	// Denormalized
	/// <summary>Full name of the operator (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.WorkSession_OperatorName), ResourceType = typeof(DtoResources))]
	public string? OperatorName { get; set; }

	/// <summary>Phase number (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.WorkSession_PhaseNumber), ResourceType = typeof(DtoResources))]
	public string? PhaseNumber { get; set; }

	/// <summary>Machine code (denormalized for display).</summary>
	[Display(Name = nameof(DtoResources.WorkSession_MachineCode), ResourceType = typeof(DtoResources))]
	public string? MachineCode { get; set; }
}
