# OpenMES - WebClient Patterns

> WebClient-specific terminal UI patterns (touchscreen shop floor).
> Read this file **only when working on `OpenMES.WebClient`**.
> Always read `PATTERNS.md` first -- this file is a supplement, not a replacement.

---

## 14. WebClient (OpenMES.WebClient) — Terminal UI

The WebClient is an operator-facing Blazor Server app designed for touchscreen shop floor
terminals. It uses plain HTML elements (`<div>`, `<button>`) for maximum layout control,
rather than FluentUI components, except where FluentUI is specifically needed (icons, progress).

### Architecture

```
Components/
  Layout/
    MainLayout.razor / .razor.cs    ← header + logout; guards auth redirect
  Machine/
    MachineCard.razor / .razor.cs   ← touchscreen card per machine
    NumericPad.razor                ← 3×4 numeric keypad, two-way Value binding
  Pages/
    _ClientComponentBase.cs         ← abstract base for all pages
    Login.razor / .razor.cs         ← device login (Name + Password)
    Home.razor / .razor.cs          ← machine grid
    Action.razor / .razor.cs        ← main operator screen per machine
ViewModels/
  _ViewModelBase.cs                 ← IsBusy, ErrorMessage, StateChanged event
  LoginViewModel.cs
  HomeViewModel.cs
  ActionViewModel.cs
Models/
  TerminalIdentity.cs               ← auth token + device name, stored in ProtectedLocalStorage
```

### `ClientComponentBase<TViewModel>` — base class for pages

All page components inherit from this. Key properties:
- `ViewModel` — the scoped ViewModel injected via primary constructor
- `CurrentIdentity` — loaded from `ProtectedLocalStorage` on first render (key: `"auth"`)
- `MesClient` — pre-configured with auth token after `CurrentIdentity` is loaded
- `Navigation` — `protected NavigationManager` (accessible in subclasses)
- `IsLoading` — toggled automatically by `RunAsync`
- `RunAsync(async ct => { ... })` — wraps all API calls; handles loading state,
  cancellation and error message bar

```csharp
partial class MyPage(MyViewModel model, ILogger<MyPage> logger,
                     IMessageService msg, MesClient mesClient)
    : ClientComponentBase<MyViewModel>(model, logger, msg, mesClient)
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);  // always call base first
        if (firstRender && CurrentIdentity?.IsAuthenticated == true)
            await RunAsync(async ct => { ... });
    }
}
```

### `HomeViewModel` — machine grid state

```csharp
public IEnumerable<MachineDto> Machines { get; set; } = [];
public Dictionary<int, MachineStateDto> MachineStates { get; set; } = [];
public Dictionary<int, WorkSessionDto> OpenSessions { get; set; } = [];

public MachineStatus GetStatus(int machineId) => ...
public string? GetActiveOperator(int machineId) => ...
public WorkSessionType? GetSessionType(int machineId) => ...
```

Load all three in parallel in `OnAfterRenderAsync`:
```csharp
var machinesTask  = MesClient.Terminal.GetMachinesAsync(CurrentIdentity.Name, ct);
var statesTask    = MesClient.MachineState.GetAllCurrentAsync(ct);
var sessionsTask  = MesClient.WorkSession.GetOpenAsync(ct);
await Task.WhenAll(machinesTask, statesTask, sessionsTask);
```

### `MachineCard` — touchscreen card

- Full surface clickable (`@onclick="Navigate"` on outer `<div>`)
- Navigates to `/action/{machine.Id}` — no dialog
- Parameters: `Model` (MachineDto), `Status`, `ActiveOperator?`, `SessionType?`
- All dynamic styles are C# string properties in the code-behind (`CardStyle`, `DotStyle`,
  `StatusBadgeStyle`, `SessionBadgeStyle`) — never interpolate CSS variables inside
  Razor attribute strings directly
- Renders: status dot + machine code + description + status badge + operator row

### `ActionViewModel` — operator action page state

```csharp
public MachineDto? Machine { get; set; }
public MachineStateDto? CurrentState { get; set; }
public WorkSessionDto? OpenSession { get; set; }
public OperatorShiftDto? OperatorShift { get; set; }
public ProductionOrderPhaseDto? ActivePhase { get; set; }
public List<ProductionDeclarationDto> LastDeclarations { get; set; } = [];

public ActionScreen Screen { get; set; } = ActionScreen.Main;
public decimal ConfirmedQty { get; set; }
public decimal ScrapQty { get; set; }

// Derived
public MachineStatus Status => CurrentState?.Status ?? MachineStatus.Idle;
public bool IsOperatorPresent => ...;
public bool IsOperatorOnBreak => ...;
public bool HasOpenSession => OpenSession is not null;
public bool CanDeclare => HasOpenSession && OpenSession!.SessionType == WorkSessionType.Work && ActivePhase is not null;
public void Notify() => NotifyStateChanged();
```

`ActionScreen` enum controls which sub-screen is active:
```csharp
public enum ActionScreen
{
    Main,
    Declare,
    CheckIn,
    StopReason,
    SessionTypePicker,
    PhasePicker,
}
```

### `Action.razor` — main operator screen

Route: `@page "/action/{Id:int}"`

**Main screen** — 2×2 grid of large context-aware buttons:
| Slot | If no open session | If open session |
|---|---|---|
| Top-left | Start work | Close session |
| Top-right | Declare qty (disabled) | Declare qty (enabled if Work+Phase) |
| Bottom-left | Machine stop / Start | Machine stop / Start |
| Bottom-right | Check-in | Check-out |

**Start work flow**:
- Select a present operator
- Resolve active phase (auto-select if only one, otherwise show `PhasePicker`)
- Show `SessionTypePicker` (`Work` / `Setup`)
- Open session and return to Main

**Declare screen** — activated by "Declare qty":
- `NumericPad` for `ConfirmedQty`
- `NumericPad` for `ScrapQty` (optional)
- Confirm → `POST /productiondeclaration` → reload context → back to Main

### Action sub-screen patterns (`Components/Actions`)

#### `DeclareScreen.razor`
- Parameters: `Phase`, `OnDeclared`, `OnCancel`
- Use `Components/Common/NumericPad.razor` for both quantities
- Show total `ConfirmedQty + ScrapQty` with target validation
- Show latest 5 declarations for the current phase
- Confirm action uses modal confirmation before API submit
- Submit with `MesClient.ProductionDeclaration.CreateAsync()`

#### `OperatorShiftScreen.razor`
- Parameters: `CurrentShift`, `OnShiftChanged`, `OnCancel`
- Context-aware transitions based on latest `OperatorEventType`
- Show running shift duration with 1-second timer refresh
- If operator is missing, open `OperatorSelectionDialog`
- On CheckOut, if open work sessions exist, require warning confirmation

#### `StateTransitionScreen.razor`
- Parameters: `Machine`, `CurrentState`, `OnStateChanged`, `OnCancel`
- Context-aware state transition actions (Idle/Running/Paused/Stopped)
- Show running state duration with 1-second timer refresh
- Stop flow requires `StopReasonSelectionDialog`
- On stop: create `MachineState`(Stopped) then `MachineStop` with reason and duration

### Dialog pattern for WebClient action overlays

For fullscreen touch overlays in action screens:
- Use an in-component conditional overlay (`@if (IsOpen)`) with centered `FluentCard`
- Keep cancel and confirm actions explicit with `EventCallback`
- Reset local dialog state in `OnParametersSet` when dialog closes

### `NumericPad.razor` — reusable numeric keypad

```razor
<NumericPad @bind-Value="ViewModel.ConfirmedQty" Label="Good pieces" />
```

- Layout: 3×4 grid (7 8 9 / 4 5 6 / 1 2 3 / . 0 ⌫)
- Key height: 64px — touch-optimised
- Parameters: `Value` (decimal), `ValueChanged` (EventCallback), `Label?` (string)
- Max 10 digits; leading zero replaced on first non-zero digit; backspace keeps value valid

### ViewModel registration in `Program.cs`

All ViewModels must be registered as `AddScoped`:
```csharp
builder.Services.AddScoped<LoginViewModel>();
builder.Services.AddScoped<HomeViewModel>();
builder.Services.AddScoped<ActionViewModel>();
```

### Style rules for terminal UI

Since the WebClient uses plain HTML elements, all dynamic styles must be C# string properties.
Button style helper pattern used in `Action.razor`:
```csharp
private static string ActionBtnStyle(string variant) =>
    "display:flex;flex-direction:column;align-items:center;justify-content:center;" +
    "height:110px;border-radius:14px;cursor:pointer;font-size:16px;font-weight:500;" +
    "border:1px solid transparent;" +
    variant switch
    {
        "accent"   => "background:var(--accent-fill-rest);color:var(--foreground-on-accent-rest);...",
        "green"    => "background:var(--success-background);color:var(--success-foreground);...",
        "warn"     => "background:var(--warning-background);color:var(--warning-foreground);...",
        "disabled" => "background:var(--neutral-layer-2);opacity:0.5;cursor:not-allowed;",
        _          => "background:var(--neutral-layer-2);color:var(--neutral-foreground-rest);..."
    };
```


---


