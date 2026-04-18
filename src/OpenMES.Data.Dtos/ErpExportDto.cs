namespace OpenMES.Data.Dtos;

/// <summary>
/// Request body for confirming ERP acquisition of exported records.
/// Contains the list of IDs assigned by the ERP for each record transmitted.
/// </summary>
public class ErpConfirmationDto
{
	/// <summary>
	/// List of confirmation items — one per exported record.
	/// Each item maps an OpenMES record ID to the counter/ID assigned by the ERP.
	/// </summary>
	public List<ErpConfirmationItemDto> Items { get; set; } = [];
}

/// <summary>
/// Maps a single OpenMES record to its ERP-assigned counter/ID.
/// </summary>
public class ErpConfirmationItemDto
{
	/// <summary>OpenMES internal Id of the WorkSession or ProductionDeclaration.</summary>
	public int RecordId { get; set; }

	/// <summary>
	/// Counter or ID assigned by the ERP after successful acquisition of this record.
	/// Will be stored as ExternalCounterId on the entity.
	/// </summary>
	public string ExternalCounterId { get; set; } = null!;
}

/// <summary>
/// Result of an ERP export operation.
/// </summary>
public class ErpExportResultDto
{
	/// <summary>How many records were included in this export batch.</summary>
	public int ExportedCount { get; set; }

	/// <summary>Timestamp of the export operation (UTC).</summary>
	public DateTimeOffset ExportedAt { get; set; }

	/// <summary>The records that were exported, ready for transmission to the ERP.</summary>
	public List<ErpExportRowDto> Rows { get; set; } = [];
}

/// <summary>
/// A single row in an ERP export batch.
/// Contains the minimum fields the ERP needs to acquire the record.
/// </summary>
public class ErpExportRowDto
{
	/// <summary>OpenMES internal Id.</summary>
	public int RecordId { get; set; }

	/// <summary>Confirmation number of the phase in the ERP (from ProductionOrderPhase.ExternalId).</summary>
	public string? PhaseExternalId { get; set; }

	/// <summary>Type of record: WorkSession or ProductionDeclaration.</summary>
	public string EntityType { get; set; } = null!;

	/// <summary>
	/// For WorkSession: session type (Setup, Work, Wait, Rework).
	/// For ProductionDeclaration: null.
	/// </summary>
	public string? SessionType { get; set; }

	/// <summary>
	/// For WorkSession: allocated minutes (positive = normal, negative = reversal).
	/// For ProductionDeclaration: null.
	/// </summary>
	public decimal? AllocatedMinutes { get; set; }

	/// <summary>
	/// For ProductionDeclaration: confirmed good quantity (positive = normal, negative = reversal).
	/// For WorkSession: null.
	/// </summary>
	public decimal? ConfirmedQuantity { get; set; }

	/// <summary>
	/// For ProductionDeclaration: scrap quantity (positive = normal, negative = reversal).
	/// For WorkSession: null.
	/// </summary>
	public decimal? ScrapQuantity { get; set; }

	/// <summary>True if this row is a reversal (storno) of a previously exported record.</summary>
	public bool IsReversal { get; set; }

	/// <summary>If reversal: the ExternalCounterId of the original record being reversed.</summary>
	public string? ReversalOfExternalCounterId { get; set; }

	/// <summary>Operator name (for WorkSession).</summary>
	public string? OperatorName { get; set; }

	/// <summary>Machine code (for WorkSession and ProductionDeclaration).</summary>
	public string? MachineCode { get; set; }

	/// <summary>Declaration date (for ProductionDeclaration) or start time (for WorkSession).</summary>
	public DateTimeOffset RecordDate { get; set; }
}
