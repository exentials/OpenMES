# EditDialogContainer Pattern Guide

## Overview
`EditDialogContainer` è un componente Razor riutilizzabile che unifica la struttura comune di tutti i dialog di modifica in WebAdmin. Riduce la duplicazione di codice, garantisce coerenza visiva e comportamentale, e implementa gestione centralizzata degli errori e loading state.

## Architecture

### Struttura del Componente
```
EditDialogContainer
├── Header (icon + title)
├── Body (error area + form fields - RenderFragment)
└── Footer (primary + cancel buttons with loading indicators)
```

### Componente: `OpenMES.WebAdmin/Components/Common/EditDialogContainer.razor`

**Parametri:**
- `ChildContent` (RenderFragment): Contenuto del form (campi specifici dell'entità)
- `Icon` (Icon?): Icona nel header
- `Title` (string?): Titolo del dialog
- `OnSaveAsync` (EventCallback): Callback al click del bottone Save
- `OnCancelAsync` (EventCallback): Callback al click del bottone Cancel
- `SaveButtonText` (string?): Testo bottone Save (default: UiResources.Button_Save)
- `CancelButtonText` (string?): Testo bottone Cancel (default: UiResources.Button_Cancel)
- `CustomPrimaryLabel` (string?): Testo bottone custom (es. "Declare", "Register", "Close")
- `CustomPrimaryCallback` (EventCallback?): Callback del bottone custom (se null, usa OnSaveAsync)

## Features

### 1. Gestione Centralizzata degli Errori
- Eccezioni lanciate da `OnSaveAsync` o `CustomPrimaryCallback` sono catturate
- Messaggio d'errore mostrato in un `<FluentMessageBar Intent="Error">`
- Dialog rimane aperto per permettere retry o correzione
- Utente può dismissare il messaggio manualmente

### 2. Loading State
- Durante l'esecuzione di Save/Custom action, tutti i pulsanti vengono disabilitati
- `<FluentProgressRing Width="16px" Height="16px">` sostituisce l'icona Save
- Icona `<FluentIcon>` mostrata quando idle
- Previene double-submission e fornisce feedback visivo

### 3. FluentUI Only
- Utilizza esclusivamente componenti Microsoft.FluentUI.AspNetCore.Components
- Nessun CSS custom o HTML grezzo
- Layout gestito con `<FluentStack>`
- Messaggi d'errore con `<FluentMessageBar>`
- Spinner con `<FluentProgressRing>`

## Usage Examples

### Standard Edit Dialog (Machine)
```razor
@inherits BaseEdit<MachineDto>

<EditDialogContainer Icon="@(new Icons.Regular.Size24.Wrench())"
                     Title="@Dialog.Instance.Parameters.Title"
                     OnSaveAsync="@SaveAsync"
                     OnCancelAsync="@CancelAsync">
    <FluentNumberField Label="@DtoResources.Machine_PlantId" @bind-Value="@Content.PlantId" Required="true" />
    <FluentNumberField Label="@DtoResources.Machine_WorkCenterId" @bind-Value="@Content.WorkCenterId" Required="true" />
    <FluentTextField Label="@DtoResources.Machine_Code" @bind-Value="@Content.Code" Required="true" />
    <!-- more fields -->
</EditDialogContainer>
```

### Dialog with Custom Primary Button (MachineState: "Declare")
```razor
@inherits BaseEdit<MachineStateDto>

<EditDialogContainer Icon="@(new Icons.Regular.Size24.Pulse())"
                     Title="@Dialog.Instance.Parameters.Title"
                     OnCancelAsync="@CancelAsync"
                     CustomPrimaryLabel="Declare"
                     CustomPrimaryCallback="@(EventCallback.Factory.Create(this, SaveAsync))">
    <FluentNumberField Label="Machine Id" @bind-Value="@Content.MachineId" Required="true" />
    <FluentSelect TOption="string" Label="Status" ...>
        <!-- enum options -->
    </FluentSelect>
</EditDialogContainer>
```

### Dialog with Custom Action and Error Handling
```razor
// SaveAsync in BaseEdit<TDto> throws exceptions on validation/API errors
// EditDialogContainer cattura automaticamente e mostra il messaggio
// L'utente vede il messaggio d'errore e può correggere i dati

private async Task SaveAsync()
{
    // Lanciare eccezione con messaggio user-friendly
    throw new InvalidOperationException("Plant code already exists");
}
// → EditDialogContainer mostra "Plant code already exists" in MessageBar
// → Pulsanti rimangono attivi per permettere nuovo tentativo
```

## Implementation Status

### ✅ All Files Refactored (17/17)
✅ Machine/Edit.razor
✅ Plant/Edit.razor
✅ Material/Edit.razor
✅ InspectionPlan/Edit.razor
✅ MachineStopReason/Edit.razor
✅ ClientDevice/Edit.razor
✅ InspectionReading/Edit.razor
✅ Operator/Edit.razor
✅ PhasePickingList/Edit.razor
✅ WorkCenter/Edit.razor
✅ NonConformity/Edit.razor
✅ MachineStop/Edit.razor
✅ ProductionOrder/Edit.razor
✅ StorageLocation/Edit.razor
✅ MachineState/Edit.razor (custom: "Declare")
✅ OperatorShift/Edit.razor (custom: "Register")
✅ StockMovement/Edit.razor (custom: "Register")
✅ WorkSession/Edit.razor (custom: "Close")

### Enhancement Status
✅ Loading state (button disable + spinner)
✅ Centralized error handling (FluentMessageBar)
✅ FluentUI-only (no custom CSS/HTML)

## Benefits

| Aspetto | Beneficio |
|---------|-----------|
| **Riduzione Codice** | Da 40-50 linee a 10-15 linee per file Edit.razor |
| **Manutenzione** | Un solo punto di modifica per header/footer structure e error handling |
| **Coerenza UI** | Tutti i dialog hanno stessa struttura visiva, errori, e loading |
| **Localizzazione** | Button labels automaticamente localizzati (UiResources) |
| **Error Handling** | Gestione centralizzata eccezioni; messaggio user-friendly |
| **UX** | Double-submit prevention, spinner feedback, error dismissable |
| **Testabilità** | Component logic testato una volta per tutti |
| **Estensibilità** | Facile aggiungere features comuni senza duplicazione |

## Error Handling Flow

```
User clicks Save
       ↓
Button disabled + Spinner shown
       ↓
OnSaveAsync invoked
       ↓
    Exception?
    /         \
   YES        NO
   ↓          ↓
Catch ex   Dialog closes
Show msg   automatically
Buttons
remain
active
   ↓
User can retry/correct
```

## Code Reduction Example

**PRIMA (Machine/Edit.razor)**: 60 linee
```razor
@inherits BaseEdit<MachineDto>

<FluentDialogHeader ShowDismiss="false">
    <FluentStack VerticalAlignment="VerticalAlignment.Center">
        <FluentIcon Value="@(new Icons.Regular.Size24.Wrench())" />
        <FluentLabel Typo="Typography.PaneHeader">@Dialog.Instance.Parameters.Title</FluentLabel>
    </FluentStack>
</FluentDialogHeader>

<FluentDialogBody>
    <FluentStack Orientation="Orientation.Vertical" VerticalGap="8">
        <FluentNumberField Label="@DtoResources.Machine_PlantId" ... />
        <!-- 25+ more lines of form fields -->
    </FluentStack>
</FluentDialogBody>

<FluentDialogFooter>
    <FluentButton Appearance="Appearance.Accent" IconStart="@(new Icons.Regular.Size20.Save())" OnClick="@SaveAsync">@UiResources.Button_Save</FluentButton>
    <FluentButton Appearance="Appearance.Neutral" OnClick="@CancelAsync">@UiResources.Button_Cancel</FluentButton>
</FluentDialogFooter>
```

**DOPO (Machine/Edit.razor)**: 13 linee
```razor
@inherits BaseEdit<MachineDto>

<EditDialogContainer Icon="@(new Icons.Regular.Size24.Wrench())"
                     Title="@Dialog.Instance.Parameters.Title"
                     OnSaveAsync="@SaveAsync"
                     OnCancelAsync="@CancelAsync">
    <FluentNumberField Label="@DtoResources.Machine_PlantId" ... />
    <!-- form fields only, no structure boilerplate -->
</EditDialogContainer>
```

**Reduction**: ~75% meno codice ripetitivo + error handling + loading state automatici

## Component Markup

```html
<FluentDialogHeader>
  [Icon] Title
</FluentDialogHeader>

<FluentDialogBody>
  <FluentMessageBar Intent="Error">
    {error message}
  </FluentMessageBar>
  <FluentStack>
    [form fields]
  </FluentStack>
</FluentDialogBody>

<FluentDialogFooter>
  <FluentButton>
    <FluentStack>
      [FluentProgressRing OR FluentIcon]
      Label
    </FluentStack>
  </FluentButton>
  <FluentButton>Cancel</FluentButton>
</FluentDialogFooter>
```

## Related Patterns

- **BasePage<TDto, TKey, TEdit>**: Base class per pages
- **BaseEdit<TDto>**: Base class per edit dialogs
- **DtoResources**: DTO field labels
- **UiResources**: Generic UI labels (Button_Save, Button_Cancel)

