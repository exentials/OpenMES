using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos;
using OpenMES.Localization.Resources;
using OpenMES.WebApiClient;

namespace OpenMES.WebAdmin.Components.Pages;

/// <summary>
/// Code-behind for the Home dashboard page.
/// Loads data for both tabs: Reparto and Operatori e macchine.
/// </summary>
public abstract class HomeBase : ComponentBase, IDisposable
{
    [Inject] protected MesClient MesClient { get; set; } = null!;

    // ── Shared ────────────────────────────────────────────────────────────────
    protected bool IsLoading { get; private set; } = true;
    protected string ActiveTab { get; set; } = "reparto";
    protected string PeriodTab { get; private set; } = "oggi";
    protected string CurrentTime { get; private set; } = string.Empty;
    private System.Threading.Timer? _clockTimer;

    // ── Tab 1: Reparto ────────────────────────────────────────────────────────
    protected string KpiOrdiniAttivi { get; private set; } = "–";
    protected string KpiOrdiniSub    { get; private set; } = string.Empty;
    protected string KpiSessioni     { get; private set; } = "–";
    protected string KpiSessioniSub  { get; private set; } = string.Empty;
    protected string KpiFermi        { get; private set; } = "–";
    protected string KpiFermiSub     { get; private set; } = string.Empty;
    protected string KpiNc           { get; private set; } = "–";
    protected string KpiNcSub        { get; private set; } = string.Empty;
    protected string KpiErpPending   { get; private set; } = "–";
    protected string KpiErpSub       { get; private set; } = string.Empty;
    protected string KpiPicking      { get; private set; } = "–";
    protected string KpiStockAlert   { get; private set; } = "–";

    protected IEnumerable<MachineStateDto>    MachineStates { get; private set; } = [];
    protected IEnumerable<MachineStopDto>     ActiveStops   { get; private set; } = [];
    protected IEnumerable<NonConformityDto>   OpenNc        { get; private set; } = [];
    protected IEnumerable<WorkSessionDto>     OpenSessions  { get; private set; } = [];
    protected IEnumerable<ProductionOrderDto> ActiveOrders  { get; private set; } = [];

    // ── Tab 2: Operators and machines ─────────────────────────────────────────
    protected string KpiPresenti    { get; private set; } = "–";
    protected string KpiPresentiSub { get; private set; } = string.Empty;
    protected string KpiOre         { get; private set; } = "–";
    protected string KpiOreSub      { get; private set; } = string.Empty;
    protected string KpiPezzi       { get; private set; } = "–";
    protected string KpiPezziSub    { get; private set; } = string.Empty;
    protected string KpiOee         { get; private set; } = "–";
    protected string OeeColor       { get; private set; } = "var(--neutral-foreground-rest)";
    protected string ScrapRate      { get; private set; } = "–";

    protected IEnumerable<OperatorPresenceVm>       OperatorPresence   { get; private set; } = [];
    protected IEnumerable<MachineEfficiencyVm>      MachineEfficiency  { get; private set; } = [];
    protected IEnumerable<TopProducerVm>            TopProducers       { get; private set; } = [];
    protected IEnumerable<ProductionDeclarationDto> RecentDeclarations { get; private set; } = [];

    // ── View models ───────────────────────────────────────────────────────────
    public record OperatorPresenceVm(
        string Name, string Initials, string Status, string StatusLabel,
        int WorkPct, int SetupPct, string TotalHours);
    public record MachineEfficiencyVm(string Code, int RunningPct, int StopPct);
    public record TopProducerVm(string Name, decimal Pieces, decimal Scrap, int BarPct);

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        _clockTimer = new System.Threading.Timer(_ =>
        {
            CurrentTime = DateTimeOffset.Now.ToString("ddd dd MMM · HH:mm:ss");
            InvokeAsync(StateHasChanged);
        }, null, 0, 1000);
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        IsLoading = true;
        StateHasChanged();
        try { await Task.WhenAll(LoadTab1Async(), LoadTab2Async()); }
        finally { IsLoading = false; StateHasChanged(); }
    }

    // ── Tab 1 ─────────────────────────────────────────────────────────────────
    private async Task LoadTab1Async()
    {
        var orders     = (await MesClient.ProductionOrder.ReadsAsync(1, 200)).Data?.Items ?? [];
        var sessions   = await MesClient.WorkSession.GetOpenAsync();
        var stops      = (await MesClient.MachineStop.ReadsAsync(1, 500)).Data?.Items ?? [];
        var nc         = (await MesClient.NonConformity.ReadsAsync(1, 200)).Data?.Items ?? [];
        var machStates = await MesClient.MachineState.GetAllCurrentAsync();
        var picking    = (await MesClient.PhasePickingList.ReadsAsync(1, 200)).Data?.Items ?? [];
        var stock      = (await MesClient.MaterialStock.ReadsAsync(1, 1000)).Data?.Items ?? [];
        var wsPending  = await MesClient.WorkSession.GetPendingExportAsync();
        var declPend   = (await MesClient.ProductionDeclaration.ReadsAsync(1, 1000)).Data?.Items ?? [];

        // ProductionOrderDto has no Status — use ConfirmedQuantity < PlannedQuantity as proxy
        var inProgressOrders = orders.Where(o => o.ConfirmedQuantity < o.PlannedQuantity).ToList();
        var completedOrders  = orders.Where(o => o.ConfirmedQuantity >= o.PlannedQuantity && o.PlannedQuantity > 0).ToList();
        KpiOrdiniAttivi = inProgressOrders.Count.ToString();
        KpiOrdiniSub    = $"{inProgressOrders.Count} in corso · {completedOrders.Count} completati";

        var openSessions = sessions.ToList();
        KpiSessioni    = openSessions.Count.ToString();
        KpiSessioniSub = $"{openSessions.Count(s => s.SessionType == WorkSessionType.Work)} Work"
                       + $" · {openSessions.Count(s => s.SessionType == WorkSessionType.Setup)} Setup";

        var activeStops = stops.Where(s => s.EndDate == null).ToList();
        KpiFermi    = activeStops.Count.ToString();
        KpiFermiSub = $"{activeStops.Count(s => s.MachineStopReasonCategory == MachineStopCategory.Breakdown)} {UiResources.Home_StopReason_Breakdown}"
                    + $" · {activeStops.Count(s => s.MachineStopReasonCategory != MachineStopCategory.Breakdown)} {UiResources.Home_StopReason_Other}";

        var openNc = nc.Where(n => n.Status != NonConformityStatus.Closed).ToList();
        KpiNc    = openNc.Count.ToString();
        KpiNcSub = $"{openNc.Count(n => n.Severity == NonConformitySeverity.Critical)} {UiResources.Home_NC_Severity_Critical}"
                 + $" · {openNc.Count(n => n.Severity == NonConformitySeverity.High)} {UiResources.Home_NC_Severity_High}";

        var wsPendingList   = wsPending.ToList();
        var declPendingList = declPend.Where(d => d.ExternalCounterId == null
                                               && d.ReversedById == null
                                               && !d.IsReversal).ToList();
        KpiErpPending = (wsPendingList.Count + declPendingList.Count).ToString();
        KpiErpSub     = $"{wsPendingList.Count} sessioni · {declPendingList.Count} dichiarazioni";
        KpiPicking    = picking.Count(p => p.Status is PickingStatus.Pending or PickingStatus.PartiallyPicked).ToString();
        KpiStockAlert = stock.Count(s => s.Quantity <= 0).ToString();

        MachineStates = machStates;
        ActiveStops   = activeStops.OrderByDescending(s => (DateTimeOffset.UtcNow - s.StartDate).TotalMinutes).Take(5);
        OpenNc        = openNc.OrderByDescending(n => (int)n.Severity).Take(5);
        OpenSessions  = openSessions;
        ActiveOrders  = inProgressOrders.Take(6);
    }

    // ── Tab 2 ─────────────────────────────────────────────────────────────────
    private async Task LoadTab2Async()
    {
        var now         = DateTimeOffset.UtcNow;
        var periodStart = PeriodTab switch
        {
            "settimana" => now.AddDays(-7),
            "mese"      => now.AddDays(-30),
            _           => new DateTimeOffset(now.Date, TimeSpan.Zero)
        };

        // All use .Items (PagedResponse<T> property name)
        var sessions   = (await MesClient.WorkSession.ReadsAsync(1, 2000)).Data?.Items ?? [];
        var decls      = (await MesClient.ProductionDeclaration.ReadsAsync(1, 2000)).Data?.Items ?? [];
        var operators  = (await MesClient.Operator.ReadsAsync(1, 200)).Data?.Items ?? [];
        var shifts     = (await MesClient.OperatorShift.ReadsAsync(1, 1000)).Data?.Items ?? [];
        var machStates = (await MesClient.MachineState.ReadsAsync(1, 5000)).Data?.Items ?? [];

        var periodSessions = sessions.Where(s => s.StartTime >= periodStart && !s.IsReversal).ToList();
        var periodDecls    = decls.Where(d => d.DeclarationDate >= periodStart && !d.IsReversal).ToList();

        var today       = now.Date;
        var todayShifts = shifts.Where(s => s.EventTime.Date == today).ToList();
        var presentIds  = GetPresentIds(todayShifts);
        var breakIds    = GetBreakIds(todayShifts);
        KpiPresenti    = presentIds.Count.ToString();
        KpiPresentiSub = $"{breakIds.Count} {UiResources.Home_KpiSubtext_InBreak} · {operators.Count() - presentIds.Count - breakIds.Count} {UiResources.Home_KpiSubtext_Absent}";

        var totalMin   = periodSessions.Where(s => s.Status == WorkSessionStatus.Closed).Sum(s => s.AllocatedMinutes);
        var totalHours = Math.Round((double)totalMin / 60, 1);
        KpiOre    = totalHours.ToString("0.0") + "h";
        KpiOreSub = PeriodTab == "oggi" ? UiResources.Home_KpiSubtext_CurrentShift : $"ultimi {(PeriodTab == "settimana" ? "7" : "30")} {UiResources.Home_KpiSubtext_Days}";

        var totPz    = periodDecls.Sum(d => d.ConfirmedQuantity);
        var totScrap = periodDecls.Sum(d => d.ScrapQuantity);
        var pct      = totPz > 0 ? Math.Round((double)totScrap / (double)totPz * 100, 1) : 0.0;
        KpiPezzi    = ((int)totPz).ToString();
        KpiPezziSub = $"{(int)totScrap} {UiResources.Home_KpiSubtext_Scrap} ({pct:0.0}%)";
        ScrapRate   = pct.ToString("0.0");

        var periodStatesList = machStates
            .Where(s => s.EventTime >= periodStart)
            .OrderBy(s => s.MachineId).ThenBy(s => s.EventTime).ToList();
        var oeeVal = ComputeOee(periodStatesList, periodStart, now);
        KpiOee   = oeeVal + "%";
        OeeColor = oeeVal >= 75 ? "var(--success-foreground)"
                 : oeeVal >= 60 ? "var(--warning-foreground)"
                 :                "var(--error-foreground)";

        OperatorPresence   = BuildOperatorPresence(operators, todayShifts, periodSessions);
        MachineEfficiency  = BuildMachineEfficiency(periodStatesList, periodStart, now);
        TopProducers       = BuildTopProducers(periodDecls);
        RecentDeclarations = decls
            .Where(d => d.DeclarationDate.Date == today && !d.IsReversal)
            .OrderByDescending(d => d.DeclarationDate).Take(8);
    }

    // ── Aggregation helpers ───────────────────────────────────────────────────
    private static HashSet<int> GetPresentIds(IEnumerable<OperatorShiftDto> shifts)
        => shifts.GroupBy(s => s.OperatorId)
                 .Where(g => g.OrderByDescending(x => x.EventTime).First().EventType
                             is OperatorEventType.CheckIn or OperatorEventType.BreakEnd)
                 .Select(g => g.Key).ToHashSet();

    private static HashSet<int> GetBreakIds(IEnumerable<OperatorShiftDto> shifts)
        => shifts.GroupBy(s => s.OperatorId)
                 .Where(g => g.OrderByDescending(x => x.EventTime).First().EventType
                             == OperatorEventType.BreakStart)
                 .Select(g => g.Key).ToHashSet();

    private static int ComputeOee(List<MachineStateDto> states, DateTimeOffset start, DateTimeOffset end)
    {
        if (states.Count == 0) return 0;
        double totalRun = 0, total = 0;
        foreach (var g in states.GroupBy(s => s.MachineId))
        {
            var ms = g.OrderBy(s => s.EventTime).ToList();
            for (int i = 0; i < ms.Count; i++)
            {
                var from = ms[i].EventTime < start ? start : ms[i].EventTime;
                var to   = i + 1 < ms.Count ? ms[i + 1].EventTime : end;
                var mins = Math.Max(0, (to - from).TotalMinutes);
                total += mins;
                if (ms[i].Status == MachineStatus.Running) totalRun += mins;
            }
        }
        return total > 0 ? (int)Math.Round(totalRun / total * 100) : 0;
    }

    private IEnumerable<OperatorPresenceVm> BuildOperatorPresence(
        IEnumerable<OperatorDto> ops, IEnumerable<OperatorShiftDto> todayShifts,
        IEnumerable<WorkSessionDto> sessions)
    {
        var presentIds = GetPresentIds(todayShifts);
        var breakIds   = GetBreakIds(todayShifts);
        var result     = new List<OperatorPresenceVm>();
        foreach (var op in ops.Take(8))
        {
            var opSess   = sessions.Where(s => s.OperatorId == op.Id && s.Status == WorkSessionStatus.Closed).ToList();
            var totMin   = (double)opSess.Sum(s => s.AllocatedMinutes);
            var workMin  = (double)opSess.Where(s => s.SessionType == WorkSessionType.Work).Sum(s => s.AllocatedMinutes);
            var setupMin = (double)opSess.Where(s => s.SessionType == WorkSessionType.Setup).Sum(s => s.AllocatedMinutes);
            var wPct     = totMin > 0 ? (int)(workMin  / totMin * 100) : 0;
            var sPct     = totMin > 0 ? (int)(setupMin / totMin * 100) : 0;
            var hours    = totMin > 0 ? $"{(int)(totMin / 60)}h {(int)(totMin % 60):00}m" : "—";
            var parts    = op.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var init     = (parts.Length >= 2 ? $"{parts[0][0]}{parts[1][0]}"
                           : op.Name.Length >= 2 ? op.Name[..2] : op.Name).ToUpper();
            var (st, lb) = presentIds.Contains(op.Id) ? ("present", UiResources.Home_Status_Present)
                          : breakIds.Contains(op.Id)   ? ("break",   UiResources.Home_Status_Break)
                          :                              ("absent",  UiResources.Home_Status_Absent);
            result.Add(new OperatorPresenceVm(op.Name, init, st, lb, wPct, sPct, hours));
        }
        return result.OrderBy(r => r.Status == "absent").ThenByDescending(r => r.WorkPct);
    }

    private static IEnumerable<MachineEfficiencyVm> BuildMachineEfficiency(
        List<MachineStateDto> states, DateTimeOffset start, DateTimeOffset end)
    {
        var result = new List<MachineEfficiencyVm>();
        foreach (var g in states.GroupBy(s => s.MachineId))
        {
            var ms = g.OrderBy(s => s.EventTime).ToList();
            double run = 0, stop = 0;
            for (int i = 0; i < ms.Count; i++)
            {
                var from = ms[i].EventTime < start ? start : ms[i].EventTime;
                var to   = i + 1 < ms.Count ? ms[i + 1].EventTime : end;
                var mins = Math.Max(0, (to - from).TotalMinutes);
                if (ms[i].Status == MachineStatus.Running) run += mins;
                else if (ms[i].Status is MachineStatus.Stopped or MachineStatus.Fault) stop += mins;
            }
            var tot = run + stop + 1;
            result.Add(new MachineEfficiencyVm(
                ms.Last().MachineCode ?? g.Key.ToString(),
                (int)Math.Round(run / tot * 100),
                (int)Math.Round(stop / tot * 100)));
        }
        return result.OrderByDescending(m => m.RunningPct).Take(6);
    }

    private static IEnumerable<TopProducerVm> BuildTopProducers(List<ProductionDeclarationDto> decls)
    {
        var grouped = decls.GroupBy(d => d.OperatorId)
            .Select(g => (Name: g.First().OperatorName ?? "–",
                          Pieces: g.Sum(d => d.ConfirmedQuantity),
                          Scrap: g.Sum(d => d.ScrapQuantity)))
            .OrderByDescending(x => x.Pieces).Take(5).ToList();
        var max = grouped.Count != 0 ? grouped.Max(x => x.Pieces) : 1;
        return grouped.Select(x => new TopProducerVm(x.Name, x.Pieces, x.Scrap,
            max > 0 ? (int)Math.Round((double)x.Pieces / (double)max * 100) : 0));
    }

    // ── Period change ────────────────────────────────────────────────────────────
    protected async Task SetPeriod(string period)
    {
        if (PeriodTab == period) return;
        PeriodTab = period;
        IsLoading = true;
        StateHasChanged();
        await LoadTab2Async();
        IsLoading = false;
        StateHasChanged();
    }

    // ── Color helpers for view ───────────────────────────────────────────────────
    protected static string GetMachineStatusColor(MachineStatus s) => s switch
    {
        MachineStatus.Running     => "var(--success-foreground)",
        MachineStatus.Setup       => "var(--warning-foreground)",
        MachineStatus.Stopped     => "var(--error-foreground)",
        MachineStatus.Fault       => "var(--error-foreground)",
        MachineStatus.Maintenance => "#534AB7",
        _                         => "var(--neutral-foreground-hint)"
    };

    protected static string GetMachineStatusBg(MachineStatus s) => s switch
    {
        MachineStatus.Running     => "var(--success-background)",
        MachineStatus.Setup       => "var(--warning-background)",
        MachineStatus.Stopped     => "var(--error-background)",
        MachineStatus.Fault       => "var(--error-background)",
        MachineStatus.Maintenance => "#EEEDFE",
        _                         => "var(--neutral-layer-2)"
    };

    // Icons are used directly in .razor via @GetMachineStatusIcon(ms.Status) — kept here as string helper
    protected static string GetMachineStatusIconName(MachineStatus s) => s switch
    {
        MachineStatus.Running     => "CheckmarkCircle",
        MachineStatus.Stopped     => "StopCircle",
        MachineStatus.Fault       => "ErrorCircle",
        MachineStatus.Maintenance => "Wrench",
        MachineStatus.Setup       => "Timer",
        _                         => "Circle"
    };

    protected static string GetSeverityColor(NonConformitySeverity s) => s switch
    {
        NonConformitySeverity.Critical => "var(--error-foreground)",
        NonConformitySeverity.High     => "var(--warning-foreground)",
        NonConformitySeverity.Medium   => "var(--warning-foreground)",
        _                              => "var(--success-foreground)"
    };

    protected static string GetSeverityBg(NonConformitySeverity s) => s switch
    {
        NonConformitySeverity.Critical => "var(--error-background)",
        NonConformitySeverity.High     => "var(--warning-background)",
        NonConformitySeverity.Medium   => "var(--warning-background)",
        _                              => "var(--success-background)"
    };

    protected static string GetSessionTypeBg(WorkSessionType t) => t switch
    {
        WorkSessionType.Work   => "var(--success-background)",
        WorkSessionType.Setup  => "var(--warning-background)",
        WorkSessionType.Rework => "var(--error-background)",
        _                      => "var(--neutral-layer-2)"
    };

    protected static string GetSessionTypeColor(WorkSessionType t) => t switch
    {
        WorkSessionType.Work   => "var(--success-foreground)",
        WorkSessionType.Setup  => "var(--warning-foreground)",
        WorkSessionType.Rework => "var(--error-foreground)",
        _                      => "var(--neutral-foreground-hint)"
    };

    protected static string GetPresenceBg(string s) => s switch
    {
        "present" => "var(--info-background)",
        "break"   => "var(--warning-background)",
        _         => "var(--neutral-layer-2)"
    };

    protected static string GetPresenceFg(string s) => s switch
    {
        "present" => "var(--info-foreground)",
        "break"   => "var(--warning-foreground)",
        _         => "var(--neutral-foreground-hint)"
    };

    protected static string GetOeeColor(int pct) =>
        pct >= 75 ? "var(--success-foreground)"
        : pct >= 60 ? "var(--warning-foreground)"
        : "var(--error-foreground)";

    protected static string FormatDuration(DateTimeOffset from)
    {
        var span = DateTimeOffset.UtcNow - from;
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes:00}m"
            : $"{span.Minutes}m";
    }

    public void Dispose() => _clockTimer?.Dispose();
}
