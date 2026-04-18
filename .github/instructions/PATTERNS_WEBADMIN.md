# OpenMES - WebAdmin Patterns

> WebAdmin-specific Blazor Server + FluentUI patterns.
> Read this file **only when working on `OpenMES.WebAdmin`**.
> Always read `PATTERNS.md` first -- this file is a supplement, not a replacement.

---

## 10. Reusable Select pattern (`KeyValueDto`)

Use shared lookup components for FK selection to avoid repeated dropdown logic.

### Base component
- Generic base: `SelectComponent<TDto, TKey, TValue>`
- Item payload: `Option<KeyValueDto<TKey>>`
- Derived per-entity components (example): `PlantSelect<TValue> : SelectComponent<PlantDto, int, TValue>`

### Data source
- Prefer `ICrudKeyValueApiService<TDto, TKey>.ReadKeyValuesAsync(...)`
- Use `KeyValueRequestDto` for term/limit/filter requests
- Keep lookup transport shape stable: `Id`, `Key`, `Value`

### Usage rules
- Use these selects for FK fields in `Edit.razor` dialogs where a lookup list exists.
- Keep labels from `DtoResources`.
- Keep value binding explicit (`Value`/`ValueChanged`) in reusable components.
- Avoid ad-hoc per-page select implementations when a shared select exists.

---

## 11. Blazor Pages (OpenMES.WebAdmin)

### Per-entity structure — 4/5 files in a dedicated folder

```
Components/
  Common/                   ← shared infrastructure (BasePage, BaseAppendPage, BaseEdit, BaseDetails, PropertyColumnExt)
  Layout/                   ← MainLayout, NavMenu
  Pages/
    MyEntity/
      MyEntityPage.razor        → paginated grid (@page "/myentity")
      MyEntityPage.razor.cs     → code-behind (partial class, primary constructor)
      Edit.razor                → create/edit dialog (inherits BaseEdit<TDto>)
      Edit.razor.cs             → Edit code-behind (only if bridge properties are needed)
      Details.razor             → detail page (@page "/myentity/{ItemId:int}")
      Details.razor.cs          → Details code-behind
    Users/                    ← Identity user management (non-standard, admin-only)
      UsersPage.razor           → user grid with roles (@page "/users")
      UsersPage.razor.cs        → code-behind with CRUD + dialog orchestration
      UserEditDialog.razor      → create/edit user dialog (email, password, role toggles)
```

> `Edit.razor.cs` is optional — needed only when the form requires extra logic
> (e.g. `DateTimeOffset` ↔ `DateTime?` conversion for `FluentDatePicker`).

### BasePage — server-side pagination via ItemsProvider

`BasePage<TDto, TKey, TEditComponent>` uses `GridItemsProvider<TDto>` for all data loading.
The grid calls `ItemsProvider` automatically on first render and on every page change.
No `OnAfterRenderAsync` or manual `LoadData()` is needed in page components.

After any CUD operation (create/update/delete), call `RefreshAsync()` — it triggers
a `StateHasChanged` which causes the grid to re-invoke `ItemsProvider` and reload the page.

**Critical:** `FluentDataGrid` requires `TGridItem` to be specified explicitly when using
`ItemsProvider` — the Razor compiler cannot infer it otherwise:
```razor
<FluentDataGrid TGridItem="PlantDto" ItemsProvider="@ItemsProvider" ...>
```

**`BaseAppendPage`** follows the same pattern for append-only entities (no Update/Delete).

### MyEntityPage.razor
```razor
@page "/myentity"
@attribute [Authorize]
@inherits BasePage<MyEntityDto, int, Edit>
@rendermode InteractiveServer

<FluentToolbar>
    <FluentButton Appearance="Appearance.Accent"
                  IconStart="@(new Icons.Regular.Size20.Add())"
                  OnClick="@AddInDialog">Add</FluentButton>
</FluentToolbar>

<FluentDataGrid TGridItem="MyEntityDto"
                ItemsProvider="@ItemsProvider"
                GenerateHeader="GenerateHeaderOption.Sticky"
                DisplayMode="DataGridDisplayMode.Table">
    <PropertyColumnExt Property="@(p => p.Code)" Sortable="true" />
    <PropertyColumnExt Property="@(p => p.Description)" Sortable="true" />
    <TemplateColumn Title="Actions">
        <FluentButton Appearance="Appearance.Stealth"
                      IconStart="@(new Icons.Regular.Size16.Edit())"
                      OnClick="@(() => EditInDialog(context))" />
        <FluentButton Appearance="Appearance.Stealth"
                      IconStart="@(new Icons.Regular.Size16.Delete())"
                      OnClick="@(() => DeleteItem(context))" />
        <FluentButton Appearance="Appearance.Stealth"
                      IconStart="@(new Icons.Regular.Size16.Glasses())"
                      OnClick="@(() => ShowItem(context))" />
    </TemplateColumn>
</FluentDataGrid>
<FluentPaginator State="@pagination" />
```

### MyEntityPage.razor.cs
```csharp
partial class MyEntityPage(MesClient mesClient)
    : BasePage<MyEntityDto, int, Edit>("myentity", mesClient.MyEntity)
{
}
```

### Edit.razor
```razor
@inherits BaseEdit<MyEntityDto>

<FluentDialogHeader ShowDismiss="false">
    <FluentStack VerticalAlignment="VerticalAlignment.Center">
        <FluentIcon Value="@(new Icons.Regular.Size24.Edit())" />
        <FluentLabel Typo="Typography.PaneHeader">@Dialog.Instance.Parameters.Title</FluentLabel>
    </FluentStack>
</FluentDialogHeader>
<FluentDialogBody>
    <FluentStack Orientation="Orientation.Vertical" VerticalGap="8">
        <FluentTextField Label="Code" @bind-Value="@Content.Code" Required="true" />
    </FluentStack>
</FluentDialogBody>
<FluentDialogFooter>
    <FluentButton Appearance="Appearance.Accent"
                  IconStart="@(new Icons.Regular.Size20.Save())"
                  OnClick="@SaveAsync">Save</FluentButton>
    <FluentButton Appearance="Appearance.Neutral" OnClick="@CancelAsync">Cancel</FluentButton>
</FluentDialogFooter>
```

### Details.razor / Details.razor.cs
```razor
@page "/myentity/{ItemId:int}"
@attribute [Authorize]
@inherits BaseDetails<MyEntityDto, int>
@rendermode InteractiveServer

@if (Model is not null)
{
    <FluentGrid>
        <FluentGridItem xs="12">
            <FluentLabel Typo="Typography.H4">@Model.Code — @Model.Description</FluentLabel>
        </FluentGridItem>
        <FluentGridItem xs="12">
            <FluentButton Appearance="Appearance.Stealth"
                          IconStart="@(new Icons.Regular.Size16.Delete())"
                          OnClick="@Delete">Delete</FluentButton>
        </FluentGridItem>
    </FluentGrid>
}
else { <FluentProgressRing /> }
```
```csharp
partial class Details(MesClient mesClient)
    : BaseDetails<MyEntityDto, int>("myentity", mesClient.MyEntity)
{
}
```

### Authorization — @attribute [Authorize]

**All protected pages must have `@attribute [Authorize]` immediately after `@page` directive.**

```razor
@page "/myentity"
@attribute [Authorize]                    ← Always include this on protected pages
@inherits BasePage<MyEntityDto, int, Edit>
@rendermode InteractiveServer
```

Rules:
- **All list pages** (`*Page.razor`) → `@attribute [Authorize]`
- **All detail pages** (`Details.razor`) → `@attribute [Authorize]`
- **Login page only** → `@attribute [AllowAnonymous]` (explicit permission to skip auth)
- **Admin-only pages** (e.g. Users) → `@attribute [Authorize(Roles = "admin")]`

When an unauthenticated user navigates to a protected page:
1. `AuthorizeRouteView` in `Routes.razor` detects the missing authentication
2. Shows the `<NotAuthorized>` component
3. `RedirectToLogin` redirects to `/login?returnUrl=<original-page>`

### Key rules

**`PropertyColumnExt`** — reads the column title automatically from `[Display]` on the DTO.
No need to specify `Title` manually. Always use instead of `PropertyColumn`.
Internally uses `display.GetName()` (not `display.Name`) to resolve the localized text via `ResourceType`.
`display.Name` returns the resource key string (e.g. `"MachineStop_StartDate"`);
`display.GetName()` returns the actual localized text (e.g. `"Start date"` / `"Data inizio"`).

**Conditional icons** — ternary between two different icon types cannot be inferred by the Razor compiler.
Always use `@if`/`else`:
```razor
@if (context.IsActive)
{
    <FluentIcon Value="@(new Icons.Regular.Size16.CheckboxChecked())" />
}
else
{
    <FluentIcon Value="@(new Icons.Regular.Size16.CheckboxUnchecked())" />
}
```

**`FluentSelect` with enum** — use `TOption="string"` with manual conversion:
```razor
<FluentSelect TOption="string"
              Label="Category"
              Value="@Content.Category.ToString()"
              ValueChanged="@(v => Content.Category = Enum.Parse<MyEnum>(v))">
    @foreach (var item in Enum.GetValues<MyEnum>())
    {
        <FluentOption TOption="string" Value="@item.ToString()">@item</FluentOption>
    }
</FluentSelect>
```

**`FluentDatePicker` with `DateTimeOffset`** — the picker uses `DateTime?`.
Create bridge properties in `Edit.razor.cs`:
```csharp
protected DateTime? StartDate
{
    get => Content.StartDate == default ? null : Content.StartDate.LocalDateTime;
    set => Content.StartDate = value.HasValue
        ? new DateTimeOffset(value.Value, TimeZoneInfo.Local.GetUtcOffset(value.Value))
        : default;
}
```

**`FluentCheckbox` with `bool?`** — the component only accepts `bool`, not `bool?`.
Create a bridge property in `Edit.razor.cs`:
```csharp
protected bool BooleanValue
{
    get => Content.BooleanValue ?? false;
    set => Content.BooleanValue = value;
}
```

**Inline child grid in Details** — when an entity has a child collection (e.g. `InspectionPlan.InspectionPoints`),
render it as an inline `FluentDataGrid` in the Details page:
```razor
<FluentDataGrid Items="@Model.InspectionPoints.AsQueryable()"
                GenerateHeader="GenerateHeaderOption.Sticky"
                DisplayMode="DataGridDisplayMode.Table">
    <PropertyColumn Property="@(p => p.Sequence)" Title="Seq" />
    <PropertyColumn Property="@(p => p.Description)" Title="Description" />
</FluentDataGrid>
```

**`_Imports.razor`** — must contain:
```razor
@using OpenMES.Data.Dtos
@using OpenMES.Data.Common               // required for enums in razor files
@using OpenMES.WebAdmin.Components.Common  // BasePage, BaseEdit, BaseDetails, PropertyColumnExt
@using OpenMES.WebApiClient
```

**NavMenu** — group entries by area using `<FluentNavGroup>`. Protect admin-only entries with `<AuthorizeView Roles="admin">`:
```razor
<FluentNavGroup Title="Amministrazione" Icon="@(new Icons.Regular.Size20.ShieldPerson())" IconColor="Color.Accent">
    <AuthorizeView Roles="admin">
        <FluentNavLink Href="users" Icon="@(new Icons.Regular.Size20.People())" IconColor="Color.Accent">Utenti</FluentNavLink>
    </AuthorizeView>
</FluentNavGroup>

<FluentNavGroup Title="Machine Stops" Icon="@(new Icons.Regular.Size20.Warning())" IconColor="Color.Accent">
    <FluentNavLink Href="machinestopreason" ...>Stop Reasons</FluentNavLink>
    <FluentNavLink Href="machinestop" ...>Machine Stops</FluentNavLink>
</FluentNavGroup>
```

**`BaseAppendPage` for append-only entities** — when the API service is `IAppendApiService` (no Update/Delete),
inherit from `BaseAppendPage` instead of `BasePage`. It provides `AddInDialog` and `LoadData` only:
```csharp
// Common/_BaseAppendPage.cs
public abstract class BaseAppendPage<TDto, TEditComponent>(IAppendApiService<TDto, int> apiService)
    : ComponentBase
    where TDto : class, IKey<int>
    where TEditComponent : IDialogContentComponent<TDto>
{ ... }
```
```razor
@page "/stockmovement"
@inherits BaseAppendPage<StockMovementDto, Edit>
@rendermode InteractiveServer
```
```csharp
partial class StockMovementPage(MesClient mesClient)
    : BaseAppendPage<StockMovementDto, Edit>(mesClient.StockMovement)
{ }
```

**Read-only list pages** — for entities managed entirely by another entity (e.g. `MaterialStock` updated only via `StockMovement`):
use `BasePage` normally but omit the Add button from the toolbar and provide a placeholder `Edit.razor` that only shows an info message.

**Icon names** — always verify icon names exist in the installed FluentUI version before using them.
Unknown icon names produce `CS0426` compile errors. When in doubt, use safe common icons:
`Building`, `Box`, `BoxMultiple`, `ArrowMove`, `Warning`, `ErrorCircle`, `ShieldCheckmark`.

---


---

## 13. Localization and Culture

### Supported cultures
`en` (default) and `it`. Configured via `RequestLocalizationOptions` in `Program.cs`.

### How it works
1. Browser sends cookie `.AspNetCore.Culture=c=it|uic=it`
2. `UseRequestLocalization()` middleware sets `CultureInfo.CurrentUICulture = "it"`
3. `DtoResources` resolves to `DtoResources.it.resx` automatically
4. `PropertyColumnExt` and `[Display]` attributes show localized column titles

### Changing culture
`GET /setculture?culture=it&redirectUri=%2Fplant`
Sets cookie (1 year), then `LocalRedirect` to `redirectUri`.
**`redirectUri` must be a relative path** — `LocalRedirect` rejects absolute URLs.
Extract with `new Uri(Nav.Uri).PathAndQuery` before `Uri.EscapeDataString`.

### CultureSelector component
`Components/Layout/CultureSelector.razor` — `FluentButton` in the header, reads
`CultureInfo.CurrentUICulture`, toggles EN↔IT via `/setculture` redirect.
Requires `@rendermode InteractiveServer` (or global render mode) to handle `OnClick`.

### Docker / Alpine
Alpine does not include ICU data. Required in `Dockerfile`:
```dockerfile
USER root
RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER $APP_UID
```
`OpenMES.WebAdmin.csproj` must have `<InvariantGlobalization>false</InvariantGlobalization>`.


