# OpenMES – Architectural Patterns

> Reference document for code consistency across the project.
> **Update whenever a new pattern is introduced or consolidated.**
> This file must be read at the start of every development session before producing any code.

> **See also:**
> - `PATTERNS_WEBADMIN.md` -- Blazor WebAdmin pages, FluentUI, localization/culture
> - `PATTERNS_WEBCLIENT.md` -- Terminal UI, ViewModels, touchscreen layout

---

## 0. Development Environment

**Primary development platform: Windows + PowerShell**

This project is developed on Windows 11 with PowerShell (pwsh.exe). When writing documentation, scripts, or instructions, use Windows/PowerShell syntax.

### Shell commands to use
- Use **PowerShell commands only** — do not assume Unix/Linux tools are available
- Convert `grep` → `Select-String`
- Convert `head` → `Select-Object -First`
- Convert `cat` → `Get-Content`
- Convert `ls` → `Get-ChildItem`

### Example: PowerShell file operations
```powershell
# View first 50 lines
Get-Content file.txt | Select-Object -First 50

# Search for pattern
Select-String -Path *.cs -Pattern "MyPattern"

# List and filter files
Get-ChildItem -Recurse -Include "*.razor" | Where-Object { $_.Name -like "*Page*" }

# Create directory if not exists
if (!(Test-Path "src/NewFolder")) { New-Item -ItemType Directory "src/NewFolder" }
```

### Build and test commands
```powershell
# Build solution
dotnet build OpenMES.slnx --no-restore -v minimal

# Run tests
dotnet test

# Restore packages
dotnet restore
```

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core |
| ORM | Entity Framework Core (Code First) |
| Database | SQL Server + PostgreSQL (separate providers) |
| Frontend | Blazor Server + FluentUI |
| Orchestration | .NET Aspire |
| Containerization | Docker (Linux) |
| Authentication | ASP.NET Core Identity + JWT (HMAC-SHA256) |

---

## 2. Solution Structure

```
OpenMES.Data.Common       → enums, IKey<T>, shared interfaces
OpenMES.Data              → EF entities, DbContext, IBaseDates / IDtoAdapter interfaces
OpenMES.Data.Dtos         → DTOs, IKeyValue<T>, localization resources, Identity DTOs
OpenMES.Data.SqlServer    → SQL Server migrations
OpenMES.Data.Pgsql        → PostgreSQL migrations
OpenMES.WebApi            → REST controllers + JWT auth + Identity user management
OpenMES.WebApiClient      → typed HTTP client (MesClient + UserManagementService)
OpenMES.WebAdmin          → Blazor admin app (server-side) + user management UI
OpenMES.WebClient         → Blazor operational client
OpenMES.MigrationService  → Aspire worker for applying migrations + Identity seeding
OpenMES.AppHost           → Aspire orchestration + JWT secret parameter injection
```

---

## 3. Entities (OpenMES.Data)

### Required conventions

```csharp
[Table(nameof(MyEntity))]
[PrimaryKey(nameof(Id))]
public class MyEntity : IKey<int>, IBaseDates, IDtoAdapter<MyEntity, MyEntityDto>
{
    public int Id { get; set; }
    // ...properties...
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties always virtual
    [ForeignKey(nameof(ParentId))]
    [InverseProperty(nameof(Parent.Children))]
    public virtual Parent Parent { get; set; } = null!;

    // IDtoAdapter implemented inline in the entity
    public static MyEntityDto AsDto(MyEntity entity) => new() { ... };
    public static MyEntity AsEntity(MyEntityDto dto) => new() { ... };
}
```

- `DeleteBehavior.NoAction` configured globally in DbContext
- Decimal quantities: `[Precision(9, 3)]`
- Technical measurements: `[Precision(18, 6)]`
- Dates always `DateTimeOffset` (never `DateTime`)
- FK + navigation property always paired: `[ForeignKey]` + `[InverseProperty]`

### Entities without IBaseDates
Use only for append-only or lookup entities that are never updated
(e.g. `MaterialStock` managed via `StockMovement`).

---

## 4. Enums (OpenMES.Data.Common)

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<MyEnum>))]
public enum MyEnum : byte
{
    /// <summary>Description of value A.</summary>
    ValueA = 0,
    /// <summary>Description of value B.</summary>
    ValueB = 1,
    /// <summary>Catch-all or terminal state.</summary>
    Other = 9,
}
```

- Base type always `byte`
- One file per enum
- `[JsonConverter]` always present
- `/// <summary>` on every value
- Catch-all or terminal values use 9 by convention

---

## 5. DTOs (OpenMES.Data.Dtos)

### Structure

```csharp
using System.ComponentModel.DataAnnotations;
using OpenMES.Data.Common;
using OpenMES.Data.Dtos.Interfaces;
using OpenMES.Data.Dtos.Resources;

public class MyEntityDto : IKey<int>, ISelectableDto
{
    /// <summary>Unique identifier.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Semantic code used for lookups and identification.</summary>
    [Required, StringLength(20)]
    [Display(Name = nameof(DtoResources.MyEntity_Code), ResourceType = typeof(DtoResources))]
    public string Code { get; set; } = null!;

    /// <summary>Human-readable description.</summary>
    [StringLength(40)]
    [Display(Name = nameof(DtoResources.MyEntity_Description), ResourceType = typeof(DtoResources))]
    public string Description { get; set; } = null!;

    // Denormalized fields (from navigation) — always at the bottom
    /// <summary>Name of the parent entity (denormalized for display).</summary>
    public string? ParentName { get; set; }
}
```

### Rules
- `/// <summary>` on **every** property (English only)
- `[Display(Name = nameof(...), ResourceType = typeof(DtoResources))]` — never hardcoded strings
- Use `ISelectableDto` on DTOs that must be exposed by key/value lookup endpoints
- Standard lookup response is `KeyValueDto<TKey>` (`Id`, `Key`, `Value`)
- Denormalized display fields grouped at the bottom with comment `// denormalized for display`
- `AsEntity` does not map `CreatedAt`/`UpdatedAt` (managed server-side)

---

## 6. Localization (DtoResources)

### Files
```
OpenMES.Data.Dtos/Resources/
  DtoResources.resx       → default strings (EN)
  DtoResources.it.resx    → Italian translation
  DtoResources.cs         → manually maintained strongly-typed class
```

### Key naming convention
```
{DtoName}_{PropertyName}
e.g.: MachineStop_StartDate, NonConformity_ClosedAt
```

### Critical rule for .resx files
`.resx` files are XML with a **single** `<root>` element.
Correct multi-chunk writing:
1. **First write** (`rewrite`): XML header + open `<root>` + first `<data>` entries
2. **Intermediate appends**: only `<data>` elements, no closing tag
3. **Last append**: final `<data>` entries + closing `</root>`

⚠️ **NEVER** write `</root>` in an intermediate chunk and then continue appending.

### DtoResources.cs class
- One `static string` property per key
- `/// <summary>` referencing the corresponding DTO property
- Sections per DTO separated by `// ── DtoName ──...` comments

### UiResources — generic UI labels and actions

**Files**
```
OpenMES.Localization/Resources/
  UiResources.resx        → default strings (EN)
  UiResources.it.resx     → Italian translation
  UiResources.cs          → strongly-typed class
```

**What belongs in UiResources** ✅
- Generic action labels: `Add`, `Edit`, `Delete`, `Cancel`, `Save`
- Generic column headers: `Label_Actions`
- UI state indicators: `Label_Open`, `Label_Enabled`
- Context-specific strings (not tied to a DTO property)
- Messages, error text, placeholders
- Role-based labels: `User_EditButton`, `User_DeleteButton`
- Business logic labels: `Home_Title`, `Section_MachineStatus`, `Action_Started`

**What should NOT be in UiResources** ❌
- DTO property labels (e.g., `Machine_Code`, `MachineStop_StartDate`) — these belong in **DtoResources**
- Any label that duplicates a `[Display]` attribute on a DTO

**Key naming convention**
- Action/UI labels: `Label_{ConceptName}` (e.g., `Label_Actions`, `Label_Open`)
- Feature-specific: `{Feature}_{Label}` (e.g., `User_EditButton`, `Home_Title`)

### Usage in Blazor components

**For DTO properties in form fields — always use DtoResources**

```razor
<!-- ✅ Correct — uses DtoResources for DTO property -->
<FluentTextField Label="@DtoResources.Machine_Code" @bind-Value="@Content.Code" Required="true" />

<FluentSelect Label="@DtoResources.Machine_Status" ...>...</FluentSelect>

<!-- ❌ Wrong — hardcoded string -->
<FluentTextField Label="Code" @bind-Value="@Content.Code" Required="true" />

<!-- ❌ Wrong — duplicates from UiResources, breaks DRY principle -->
<FluentTextField Label="@UiResources.Label_Code" @bind-Value="@Content.Code" Required="true" />
```

**For column headers — PropertyColumnExt (automatic)**

`PropertyColumnExt` automatically reads localized labels from DTO `[Display]` attributes:
```razor
<!-- ✅ Correct — PropertyColumnExt auto-localizes via [Display] attribute -->
<PropertyColumnExt Property="@(p => p.Code)" Sortable="true" />

<!-- No manual Title needed -->
```

**For custom TemplateColumn titles tied to DTO properties — use DtoResources**

When a `TemplateColumn` renders a computed or formatted version of a DTO property:
```razor
<!-- ✅ Correct — "Disabled" is a DTO property, use DtoResources -->
<TemplateColumn Title="@DtoResources.InspectionPlan_Disabled">
    @if (context.Disabled)
    {
        <FluentIcon Value="@(new Icons.Regular.Size16.CheckboxChecked())" />
    }
    else
    {
        <FluentIcon Value="@(new Icons.Regular.Size16.CheckboxUnchecked())" />
    }
</TemplateColumn>
```

**For generic action columns — use UiResources**

```razor
<!-- ✅ Correct — generic "Actions" column header (context-specific, not a DTO property) -->
<TemplateColumn Title="@UiResources.Label_Actions">
    <FluentButton IconStart="@(new Icons.Regular.Size16.Edit())" OnClick="..." />
    <FluentButton IconStart="@(new Icons.Regular.Size16.Delete())" OnClick="..." />
</TemplateColumn>
```

### Key principle: DRY (Don't Repeat Yourself)

**Never duplicate a DTO property label between DtoResources and UiResources.**

Example of **incorrect** architecture:
```
❌ DtoResources.resx:  Machine_Code = "Code"
❌ UiResources.resx:   Label_Code = "Code"  ← REDUNDANT
```

Maintenance burden:
- Two places to update translations
- Risk of desynchronization (EN differs from IT)
- Confusion about which resource to use

**Correct** approach:
```
✅ DtoResources.resx:  Machine_Code = "Code" (DTO property)
✅ UiResources.resx:   Label_Actions = "Actions" (generic UI only)
```

Benefits:
- Single source of truth for DTO labels
- Clear separation: DTO property labels vs. generic UI labels
- Easier maintenance and fewer translation errors

### Localization workflow

1. **Define DTO with [Display] attribute:**
   ```csharp
   [Display(Name = nameof(DtoResources.Machine_Code), ResourceType = typeof(DtoResources))]
   public string Code { get; set; }
   ```

2. **Add keys to DtoResources.resx (EN) and DtoResources.it.resx (IT):**
   ```xml
   <!-- DtoResources.resx -->
   <data name="Machine_Code"><value>Code</value></data>

   <!-- DtoResources.it.resx -->
   <data name="Machine_Code"><value>Codice</value></data>
   ```

3. **Add typed property to DtoResources.cs:**
   ```csharp
   public static string Machine_Code => GetString(nameof(Machine_Code));
   ```

4. **Use in Blazor (automatic via PropertyColumnExt or explicit):**
   ```razor
   <!-- Automatic via [Display] attribute -->
   <PropertyColumnExt Property="@(p => p.Code)" Sortable="true" />

   <!-- Explicit in custom forms -->
   <FluentTextField Label="@DtoResources.Machine_Code" @bind-Value="@Content.Code" />
   ```

---

## 7. Authentication Architecture

### Overview

OpenMES uses a **dual-scheme** authentication model in `WebApi`:

| Scheme | Name | Used by | Token type |
|---|---|---|---|
| JWT Bearer | `"JWT"` (default) | WebAdmin → WebApi | Signed JWT (HMAC-SHA256) |
| Terminal | `"TerminalScheme"` | WebClient → WebApi | Static token stored in DB |

The two schemes are completely independent. Controllers declare which scheme they use via `[Authorize(AuthenticationSchemes = "...")]`. The default policy applies `"JWT"` to all controllers that only have `[Authorize]` (inherited from `ApiControllerBase`).

---

### Login flow (WebAdmin)

```
Login.razor
  └─► POST /api/admin/login          [AllowAnonymous]
        AdminAuthController
          └─► SignInManager.CheckPasswordSignInAsync()  (lockout enabled)
          └─► UserManager.GetRolesAsync()
          └─► JwtService.GenerateToken(user, roles)
          └─► AdminLoginResultDto { Email, AuthToken (JWT), Roles[] }

LocalAuthStateProvider.LoginAsync(result)
  └─► ProtectedLocalStorage.SetAsync("admin-auth", result)   ← encrypted
  └─► MesClient.SetAuthToken(jwt)     ← Authorization: Bearer <jwt>
  └─► ValidateAndBuildPrincipal(jwt)  ← verify signature + expiry
  └─► NotifyAuthenticationStateChanged()
```

On every **circuit reconnect** (Blazor Server SignalR):
```
LocalAuthStateProvider.GetAuthenticationStateAsync()
  └─► ProtectedLocalStorage.GetAsync("admin-auth")
  └─► ValidateAndBuildPrincipal(jwt)
        ├─ OK  → restore ClaimsPrincipal + MesClient token
        └─ Expired/invalid → delete from storage, return Anonymous
```

---

### JWT structure

Claims included in every token:

| Claim | Value |
|---|---|
| `sub` | Identity `user.Id` (GUID) |
| `email` | User email address |
| `jti` | New `Guid` (unique token ID) |
| `ClaimTypes.Name` | User email address |
| `ClaimTypes.Role` | One claim per role (e.g. `"admin"`) |

Configuration (via `appsettings.json` section `"Jwt"`):

```json
"Jwt": {
  "SecretKey": "— set via User Secrets or env var —",
  "Issuer": "openmes-webapi",
  "Audience": "openmes-webadmin",
  "ExpirationMinutes": 480
}
```

> **The `SecretKey` must be identical in both `WebApi` and `WebAdmin`.**
> It is injected at runtime via Aspire parameter `jwt-secret-key` (dev) or
> environment variable `Jwt__SecretKey` (Docker/production).

---

### Key files

| File | Responsibility |
|---|---|
| `WebApi/Auth/JwtService.cs` | Generates the signed JWT token |
| `WebApi/Controllers/AdminAuthController.cs` | `POST /api/admin/login` and `POST /api/admin/logout` |
| `WebApi/Controllers/AdminUsersController.cs` | CRUD users + roles, `[Authorize(Roles = "admin")]` |
| `WebApi/Controllers/_ApiControllerBase.cs` | Base class with `[Authorize]` — applies JWT default to all controllers |
| `WebApi/Controllers/TerminalController.cs` | `[Authorize(AuthenticationSchemes = "TerminalScheme")]` — overrides the default |
| `WebApiClient/IdentityService.cs` | HTTP client for `POST /api/admin/login` |
| `WebApiClient/UserManagementService.cs` | HTTP client for `GET/POST/PUT/DELETE /api/admin/users` |
| `WebApiClient/MesClient.cs` | `Identity` + `Users` properties; `SetAuthToken()` |
| `WebAdmin/Services/LocalAuthStateProvider.cs` | Blazor `AuthenticationStateProvider`; persists + validates JWT |

---

### Dual-scheme authorization on operational controllers

Controllers accessible by BOTH WebAdmin (JWT) and WebClient (TerminalScheme) use:

```csharp
[Authorize(AuthenticationSchemes = "JWT,TerminalScheme", Roles = "Admin, User, device")]
```

The `device` role is injected by `TerminalAuthenticationHandler` when a valid `ClientDevice`
token is presented. The `Admin` and `User` roles are issued by `JwtService` at admin login.

Authorization matrix:

| Controller type | Scheme accepted | Role required |
|---|---|---|
| Operational (CRUD + shop) | JWT or TerminalScheme | Admin, User, or device |
| Admin-only (users, ERP) | JWT only | admin |
| Terminal connect | none (AllowAnonymous) | — |
| Admin login | none (AllowAnonymous) | — |

---

### `ApiControllerBase` — default authorization

All controllers that inherit `ApiControllerBase` are protected by JWT by default:

```csharp
[Authorize]   // ← uses the "JWT" default scheme set in Program.cs AddAuthorization()
public abstract class ApiControllerBase(...) : ControllerBase { }
```

To apply a different scheme on a derived controller, declare it at class level:

```csharp
[Authorize(AuthenticationSchemes = TerminalAuthenticationHandler.SchemeName)]
public class TerminalController(...) : ApiControllerBase(...) { }
```

To restrict to a specific role:

```csharp
[Authorize(AuthenticationSchemes = "JWT", Roles = "admin")]
public class AdminUsersController(...) : ControllerBase { }
```

---

### Identity user management

The `AdminUsersController` exposes:

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/admin/users` | List all users with roles |
| `GET` | `/api/admin/users/{id}` | Get single user |
| `POST` | `/api/admin/users` | Create user with password and roles |
| `PUT` | `/api/admin/users/{id}` | Update password and/or roles |
| `DELETE` | `/api/admin/users/{id}` | Delete user (cannot self-delete) |
| `POST` | `/api/admin/users/{id}/unlock` | Unlock account after lockout |
| `GET` | `/api/admin/users/roles` | List all available roles |

All endpoints require JWT authentication and role `"admin"`.

The corresponding WebAdmin page is at `/users` (visible only in NavMenu for users with role `"admin"` via `<AuthorizeView Roles="admin">`).

---

### Seeded default user

`IdentityMigrationWorker` seeds one user on first run (only if `Users` table is empty):

| Field | Value |
|---|---|
| Email | `admin@localhost.local` |
| Password | `Admin@123` |
| Role | `admin` |

> **Change this password immediately after first login.**

---

### Secret key configuration reference

| Context | Variable name | File |
|---|---|---|
| Aspire parameter name | `jwt-secret-key` | `AppHost.cs` |
| Aspire dev value | `Parameters:jwt-secret-key` | `AppHost/appsettings.Development.json` |
| Aspire dev (User Secrets) | `Parameters:jwt-secret-key` | `dotnet user-secrets` on AppHost project |
| Docker Compose env var | `JWT_SECRET_KEY` | `.env` |
| ASP.NET Core config key | `Jwt:SecretKey` | Injected as `Jwt__SecretKey` env var |

---

## 8. Controllers (OpenMES.WebApi)

### Basic pattern (full CRUD)

```csharp
[Authorize(Roles = "Admin, User")]
public class MyEntityController(OpenMESDbContext dbContext, ILogger<MyEntityController> logger)
    : RestApiControllerBase<MyEntity, MyEntityDto, int>(dbContext, logger)
{
}
```

### KeyValue-enabled pattern (CRUD + `/keyvalue`)

Use this when `TEntity` implements `IKeyValueDtoAdapter<TEntity, TDto, TKey>` and `TDto` is selectable:

```csharp
public class MyEntityController(OpenMESDbContext dbContext, ILogger<MyEntityController> logger)
    : RestKeyValueApiControllerBase<MyEntity, MyEntityDto, int>(dbContext, logger)
{
}
```

### Include pattern with centralized no-tracking reads

Override `Query` only to define includes once; base class applies:
- `ReadQuery` (`AsNoTracking`) for `Reads`/`Read`/`ReadKeyValues`
- tracking operations for writes

```csharp
protected override IQueryable<MyEntity> Query => base.Query
    .Include(x => x.Parent)
    .Include(x => x.Operator);
```

### Query pipeline rules
- Put all common includes in `Query`
- Do not call `AsNoTracking()` manually in controllers for standard reads
- Keep write operations on tracked entities (`FindAsync` / tracked context)

### Append-only entities (e.g. StockMovement)
- Inherit from `RestApiControllerBase` normally
- Hide `Update` and `Delete` with `new` + `[ApiExplorerSettings(IgnoreApi = true)]`
- Return HTTP 405

```csharp
[ApiExplorerSettings(IgnoreApi = true)]
public new Task<IActionResult> Update(int id, TDto data, CancellationToken ct)
    => Task.FromResult<IActionResult>(StatusCode(405, "Immutable."));
```

> **Note**: public HTTP methods on `RestApiControllerBase` are not `virtual`,
> so `override` cannot be used — use `new` instead.

---

## 9. WebApiClient

### Available services
- `ICrudApiService<TDto, TKey>` — full CRUD (Create/Read/Reads/Update/Delete)
- `ICrudKeyValueApiService<TDto, TKey>` — full CRUD + `ReadKeyValuesAsync` for lookup/select scenarios
- `IAppendApiService<TDto, TKey>` — Create/Read/Reads only (for immutable entities)
- `IdentityService` — `POST /api/admin/login` (returns JWT + roles)
- `UserManagementService` — CRUD users and roles via `/api/admin/users`

### Registration in MesClient

```csharp
// Auth — fixed endpoints, not configurable
public IdentityService Identity { get; }
    = new IdentityService(httpClient, "api/admin/login");

public UserManagementService Users { get; }
    = new UserManagementService(httpClient, "api/admin/users");

// KeyValue-enabled endpoint
public ICrudKeyValueApiService<MyEntityDto, int> MyEntity { get; }
    = new CrudKeyValueApiService<MyEntityDto, int>(httpClient, "myentity");

// Standard CRUD endpoint
public ICrudApiService<MyOtherEntityDto, int> MyOtherEntity { get; }
    = new CrudApiService<MyOtherEntityDto, int>(httpClient, "myotherentity");

// Append-only endpoint
public IAppendApiService<StockMovementDto, int> StockMovement { get; }
    = new AppendApiService<StockMovementDto, int>(httpClient, "stockmovement");
```

> The route string must match the controller name in lowercase without the `Controller` suffix.

---
