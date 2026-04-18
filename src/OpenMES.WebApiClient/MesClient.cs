using System.Net.Http.Headers;
using OpenMES.Data.Dtos;
using OpenMES.WebApiClient.Interfaces;

namespace OpenMES.WebApiClient;

/// <summary>
/// Centralized entry point for all MES API services.
/// Each property exposes CRUD (or append-only) operations for a specific entity.
/// </summary>
public class MesClient(HttpClient httpClient)
{
	public void SetAuthToken(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			httpClient.DefaultRequestHeaders.Authorization = null;
			return;
		}

		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}

	// ── Auth ─────────────────────────────────────────────────────────────────
	/// <summary>ASP.NET Core Identity login for admin users (WebAdmin). Returns JWT with roles.</summary>
	public IdentityService Identity { get; } = new IdentityService(httpClient, "api/admin/login");

	/// <summary>Identity user and role management. Requires the "admin" role.</summary>
	public UserManagementService Users { get; } = new UserManagementService(httpClient, "api/admin/users");

	// ── Terminal ──────────────────────────────────────────────────────────────
	public TerminalService Terminal { get; } = new TerminalService(httpClient, "terminal");

	// ── Master data ───────────────────────────────────────────────────────────
	public ICrudKeyValueApiService<PlantDto, int> Plant { get; } = new CrudKeyValueApiService<PlantDto, int>(httpClient, "plant");
	public ICrudKeyValueApiService<WorkCenterDto, int> WorkCenter { get; } = new CrudKeyValueApiService<WorkCenterDto, int>(httpClient, "workcenter");
	public ICrudKeyValueApiService<MachineDto, int> Machine { get; } = new CrudKeyValueApiService<MachineDto, int>(httpClient, "machine");
	public ICrudKeyValueApiService<OperatorDto, int> Operator { get; } = new CrudKeyValueApiService<OperatorDto, int>(httpClient, "operator");
	public ICrudKeyValueApiService<MaterialDto, int> Material { get; } = new CrudKeyValueApiService<MaterialDto, int>(httpClient, "material");
	public ICrudApiService<ClientDeviceDto, int> ClientDevice { get; } = new CrudApiService<ClientDeviceDto, int>(httpClient, "clientdevice");

	// ── Production ────────────────────────────────────────────────────────────
	public ICrudApiService<ProductionOrderDto, int> ProductionOrder { get; } = new CrudApiService<ProductionOrderDto, int>(httpClient, "productionorder");
	public ICrudApiService<ProductionOrderPhaseDto, int> ProductionOrderPhase { get; } = new CrudApiService<ProductionOrderPhaseDto, int>(httpClient, "productionorderphase");
	public IAppendApiService<ProductionDeclarationDto, int> ProductionDeclaration { get; } = new AppendApiService<ProductionDeclarationDto, int>(httpClient, "productiondeclaration");
	public ICrudApiService<PhasePickingListDto, int> PhasePickingList { get; } = new CrudApiService<PhasePickingListDto, int>(httpClient, "phasepickinglist");
	public ICrudApiService<PhasePickingItemDto, int> PhasePickingItem { get; } = new CrudApiService<PhasePickingItemDto, int>(httpClient, "phasepickingitem");

	// ── Machine stops ─────────────────────────────────────────────────────────
	public ICrudApiService<MachineStopReasonDto, int> MachineStopReason { get; } = new CrudApiService<MachineStopReasonDto, int>(httpClient, "machinestopreason");
	public ICrudApiService<MachineStopDto, int> MachineStop { get; } = new CrudApiService<MachineStopDto, int>(httpClient, "machinestop");

	// ── Declarations ──────────────────────────────────────────────────────────
	public OperatorShiftService OperatorShift { get; } = new OperatorShiftService(httpClient, "operatorshift");
	public WorkSessionService WorkSession { get; } = new WorkSessionService(httpClient, "worksession");
	public MachinePhasePlacementService MachinePhasePlacement { get; } = new MachinePhasePlacementService(httpClient, "machinephaseplacement");
	public ErpExportService ErpExport { get; } = new ErpExportService(httpClient);
	public MachineStateService MachineState { get; } = new MachineStateService(httpClient, "machinestate");

	// ── Quality ───────────────────────────────────────────────────────────────
	public ICrudApiService<InspectionPlanDto, int> InspectionPlan { get; } = new CrudApiService<InspectionPlanDto, int>(httpClient, "inspectionplan");
	public ICrudApiService<InspectionPointDto, int> InspectionPoint { get; } = new CrudApiService<InspectionPointDto, int>(httpClient, "inspectionpoint");
	public ICrudApiService<InspectionReadingDto, int> InspectionReading { get; } = new CrudApiService<InspectionReadingDto, int>(httpClient, "inspectionreading");
	public ICrudApiService<NonConformityDto, int> NonConformity { get; } = new CrudApiService<NonConformityDto, int>(httpClient, "nonconformity");

	// ── Warehouse ─────────────────────────────────────────────────────────────
	public ICrudApiService<StorageLocationDto, int> StorageLocation { get; } = new CrudApiService<StorageLocationDto, int>(httpClient, "storagelocation");
	public ICrudApiService<MaterialStockDto, int> MaterialStock { get; } = new CrudApiService<MaterialStockDto, int>(httpClient, "materialstock");
	public IAppendApiService<StockMovementDto, int> StockMovement { get; } = new AppendApiService<StockMovementDto, int>(httpClient, "stockmovement");
}
