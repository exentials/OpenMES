using Microsoft.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Localization.Resources;

namespace OpenMES.WebClient.Components.Machine;

partial class MachineCard
{
	[Inject] private NavigationManager NavigationManager { get; set; } = null!;

	[Parameter, EditorRequired] public MachineDto Model { get; set; } = null!;
	[Parameter] public MachineStatus Status { get; set; } = MachineStatus.Idle;
	[Parameter] public string? ActiveOperator { get; set; }
	[Parameter] public WorkSessionType? SessionType { get; set; }

	private void Navigate() => NavigationManager.NavigateTo($"/action/{Model.Id}");

	private string StatusColor => Status switch
	{
		MachineStatus.Running     => "var(--success-foreground)",
		MachineStatus.Setup       => "var(--warning-foreground)",
		MachineStatus.Fault       => "var(--error-foreground)",
		MachineStatus.Stopped     => "var(--error-foreground)",
		MachineStatus.Maintenance => "#7F77DD",
		MachineStatus.Blocked     => "var(--warning-foreground)",
		MachineStatus.Starved     => "var(--warning-foreground)",
		_                         => "var(--neutral-foreground-hint)"
	};

	private string StatusBg => Status switch
	{
		MachineStatus.Running     => "var(--success-background)",
		MachineStatus.Setup       => "var(--warning-background)",
		MachineStatus.Fault       => "var(--error-background)",
		MachineStatus.Stopped     => "var(--error-background)",
		MachineStatus.Maintenance => "#EEEDFE",
		MachineStatus.Blocked     => "var(--warning-background)",
		MachineStatus.Starved     => "var(--warning-background)",
		_                         => "var(--neutral-layer-2)"
	};

	private string StatusLabel => Status switch
	{
		MachineStatus.Running     => UiResources.Machine_Running,
		MachineStatus.Setup       => UiResources.Machine_Setup,
		MachineStatus.Idle        => UiResources.Machine_Idle,
		MachineStatus.Stopped     => UiResources.Machine_Stopped,
		MachineStatus.Fault       => UiResources.Machine_Fault,
		MachineStatus.Maintenance => UiResources.Machine_Maintenance,
		MachineStatus.Blocked     => UiResources.Machine_Blocked,
		MachineStatus.Starved     => UiResources.Machine_Starved,
		MachineStatus.Manual      => UiResources.Machine_Manual,
		_                         => "–"
	};

	private string SessionLabel => SessionType switch
	{
		WorkSessionType.Work   => UiResources.Session_Work,
		WorkSessionType.Setup  => UiResources.Session_Setup,
		WorkSessionType.Wait   => UiResources.Session_Wait,
		WorkSessionType.Rework => UiResources.Session_Rework,
		_                      => ""
	};

	private string SessionBg => SessionType switch
	{
		WorkSessionType.Work   => "var(--success-background)",
		WorkSessionType.Setup  => "var(--warning-background)",
		WorkSessionType.Rework => "var(--error-background)",
		_                      => "var(--neutral-layer-2)"
	};

	private string SessionFg => SessionType switch
	{
		WorkSessionType.Work   => "var(--success-foreground)",
		WorkSessionType.Setup  => "var(--warning-foreground)",
		WorkSessionType.Rework => "var(--error-foreground)",
		_                      => "var(--neutral-foreground-hint)"
	};

	// ── Computed style strings (keep interpolation out of .razor) ─────────────
	private string CardStyle =>
		"background:var(--neutral-layer-1);" +
		"border:1px solid var(--neutral-stroke-layer-rest);" +
		"border-radius:12px;" +
		"padding:20px 24px;" +
		"cursor:pointer;" +
		"user-select:none;" +
		"margin-bottom:12px;";

	private string DotStyle =>
		$"width:14px;height:14px;border-radius:50%;flex-shrink:0;background:{StatusColor};";

	private string StatusBadgeStyle =>
		$"font-size:13px;font-weight:500;padding:4px 14px;border-radius:99px;" +
		$"white-space:nowrap;background:{StatusBg};color:{StatusColor};";

	private string SessionBadgeStyle =>
		$"font-size:12px;font-weight:500;padding:3px 12px;border-radius:99px;" +
		$"white-space:nowrap;background:{SessionBg};color:{SessionFg};";
}
