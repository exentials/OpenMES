using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.WebClient.ViewModels;

namespace OpenMES.WebClient.Components.Machine;

public partial class MachineActionsDialog : IDialogContentComponent<MachineActionsDialogViewModel>
{
	[Parameter]
	public MachineActionsDialogViewModel Content { get; set; } = null!;
	[CascadingParameter]
	public FluentDialog? Dialog { get; set; }

	private async Task StartSetup() {
		if (Dialog is not null)
		{
			await Dialog.CloseAsync("StartSetup");
		}
	}

	void PauseSetup() { /* logica */ }
	void ResumeSetup() { /* logica */ }
	void AbortSetup() { /* logica */ }
	void SetupCompleted() { /* logica */ }

	void StartProduction() { /* logica */ }
	void PauseProduction() { /* logica */ }
	void ResumeProduction() { /* logica */ }
	void StopProduction() { /* logica */ }
	void EndBatch() { /* logica */ }

	void StartInspection() { /* logica */ }
	void PauseInspection() { /* logica */ }
	void ResumeInspection() { /* logica */ }
	void InspectionCompleted() { /* logica */ }

	void StartMaintenance() { /* logica */ }
	void EndMaintenance() { /* logica */ }
	void ReportDowntime() { /* logica */ }

	void LoadMaterial() { /* logica */ }
	void UnloadMaterial() { /* logica */ }
	void MaterialShortage() { /* logica */ }
	void MaterialOK() { /* logica */ }

	void ShowStatus() { /* logica */ }
	void OperatorLogin() { /* logica */ }
	void OperatorLogout() { /* logica */ }
	void EmergencyStop() { /* logica */ }
	void ResetAlarm() { /* logica */ }
	void SendReport() { /* logica */ }

}
