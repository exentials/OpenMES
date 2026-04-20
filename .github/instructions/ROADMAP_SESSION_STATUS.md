# OpenMES Development Status

**Last Updated:** 2026-04-20 (session 3)
**Branch:** `main`
**Build Status:** ✅ Passing
**Test status:** ✅ Targeted suites passing (MachinePhasePlacementControllerTests, WebClientShiftAndDeclarationFlowTests)

## Completed (recent)

### Domain and data
- Warehouse hierarchy implemented (`Plant -> Warehouse -> StorageLocation`).
- `Material` supports `DefaultWarehouseId` and `DefaultStorageLocationId`.
- Added machine/phase placement domain model:
  - `MachinePhasePlacement` entity + DTO + status enum
  - provider migrations and snapshots aligned (Pgsql + SqlServer)

### Migration tooling and instructions
- `tools/migrateData.ps1` and `tools/migrateIdentity.ps1` updated with:
  - comment-based help
  - non-interactive, Copilot-friendly behavior
  - `-MigrationName`, `-Reset`, `-Interactive`
  - automatic fallback naming (`INIT` / `AUTO_yyyyMMddHHmmss`)
- Instructions updated in:
  - `.github/instructions/AI-WORKFLOW.md`
  - `.github/instructions/projects/OpenMES.Data-and-Migrations.md`

### Production validation
- Quantity validation rules active (Rule A and Rule B).
- Declarations enforce operator presence server-side (blocked if absent or on break).

### Multi-user terminal flow
- Terminal login authenticates the device, not the operator.
- Work-session opening requires selecting a present operator.
- `GET /operatorshift/present` endpoint available and used by WebClient.

### Machine-phase placement APIs
- Added and validated placement endpoints:
  - `POST /machinephaseplacement/place`
  - `POST /machinephaseplacement/{id}/unplace`
  - `GET /machinephaseplacement/machine/{machineId}/open`
- Added transition endpoints:
  - `start-setup`, `pause-setup`, `resume-setup`
  - `start-work`, `pause-work`, `resume-work`
  - `close`
- Server-side checks include operator presence, placement status transitions, and active session constraints.

### WebApiClient
- `MachinePhasePlacementService` extended with typed methods for all transition endpoints.

### WebClient — Action flow
- Action page integrated with open placements board and per-placement transitions.
- Added placement selection and active placement context handling.
- Added busy-row lock behavior to prevent duplicate actions during in-flight calls.
- Added close-placement confirmation dialog.
- Fixed Start Work flow interruption after operator confirmation (dialog no longer loops).
- Enforced explicit phase/bolla selection from available list before opening setup/work.
- Added guided panel (step-by-step) and explicit CTA to open phase picker.

### Localization
- Localized new Action UI texts (EN/IT) for placement board and guided flow.
- Removed fragile string-splitting pattern by introducing dedicated short label resources.

- **Machine stop flow broken** (`Action.razor.cs` + `Action.razor` + localization):
  Three separate issues: (1) `OnStopReasonSelectedAsync` called only `Notify()` instead of
  `LoadContextAsync` → badge stayed gray after stop instead of turning red; (2)
  `MachineStateActionLabel` showed generic "Start"/"Resume" for non-Running states — confusing
  on a terminal. Simplified to "Avvia macchina" / "Fermo macchina" for all non-Running states.
  (3) New resource key `Action_MachineStart` added in EN/IT/CS.
  Also: branch non-Running in `HandleMachineStateAsync` changed `onlyPresent: true` → `false`
  to allow starting a machine even without a checked-in operator, plus added `LoadContextAsync`
  after state change to refresh badge immediately.

- **Switch to Work from InSetup failed** (`MachinePhasePlacementController.StartWork`):
  `CloseOpenSessionsAsync` marked Setup sessions Closed only in EF change-tracker without
  flushing; `OpenSessionForPlacementAsync` queried DB and found the Setup session still Open,
  blocking with "another session type is already active". Fixed by inserting
  `SaveChangesAsync` after `CloseOpenSessionsAsync` and before `OpenSessionForPlacementAsync`.
- **Close placement failed with "Unable to open session"** (`MachinePhasePlacementController.Close`):
  Controller rejected close if any open session existed instead of closing them automatically.
  Fixed by auto-closing all open sessions (any `WorkSessionType`) before closing the placement,
  with intermediate `SaveChangesAsync` to avoid stale state check.
- **Active phase quantities not updated after declaration** (`Action.razor.cs` `LoadContextAsync`):
  `ConfirmedQuantity`/`ScrapQuantity` in the context strip were stale after `ConfirmDeclareAsync`
  because `LoadContextAsync` set `ActivePhase = null` in the `else if (OpenPlacements > 0)` branch
  instead of re-fetching. Fixed: if `SelectedPlacementId` is valid, re-fetch the phase from the
  placement to pick up updated aggregates; only nullify if placement no longer exists.

- **False "Failed to update operator shift" error**: `POST /operatorshift` returns `int` (id),
  not `OperatorShiftDto`; removed `|| result.Data is null` guard in `OperatorShiftScreen`.
- **`IApiService` error messages**: Added `ExtractErrorMessage()` to parse ProblemDetails JSON
  (RFC 7807) and return the `detail` field instead of raw JSON. Applied to all `CrudApiService`
  error branches.
- **Phase picker never shown**: `IApiService.JsonOptions` lacked `JsonStringEnumConverter` —
  enum fields in DTOs (e.g. `OrderStatus`) were all deserialized as `0` (Created), so the
  `Released/Setup/InProcess` filter in `EnsureActivePhaseAsync` returned 0 phases. Fixed by
  adding `JsonStringEnumConverter` to `JsonOptions`.

### Seed data
- Added PO4000 (3 phases on Turning), PO5000 (2 on Milling), PO6000 (2 on Grinding)
  to `MesDataSeeding.cs` so the phase picker is exercised during WebClient testing.

### Test coverage
- `OperatorShiftPresentTests.cs` — 15 tests for GET /operatorshift/present.
- `WebClientShiftAndDeclarationFlowTests.cs` — 48 tests for shift transitions,
  presence derivation, declaration API preconditions, ActionViewModel derived properties.
- `MachinePhasePlacementControllerTests.cs` — coverage on place/unplace/open-list and lifecycle transitions.

### Quality gates
- Build passing after each incremental change-set.
- Targeted suites passing for placement flow and WebClient shift/declaration behavior.

## Remaining work
1. Validate plant/terminal scoping for present operator list in real environment.
2. Expand docs wiki with state-transition and machine-stop troubleshooting examples.
3. Complete WebClient operator flow polish:
   - disable/tooltip reasons for blocked actions,
   - stronger inline guidance for missing prerequisites.
4. Complete remaining WebClient areas: quality readings integration.
5. Remaining WebAdmin pages: ProductionDeclaration details, WorkSession details, ERP Export UI.
6. Add regression tests for E2E bugs fixed in sessions 2–3.

## Risks / attention points
- Presence filtering depends on latest shift event consistency — DB inconsistency breaks it.
- Keep EN/IT resource parity when adding new UI labels.
- `CanApplyEvent` logic is duplicated between `OperatorShiftClientService` and test suite
  — update both if logic changes.
- Seed data guard (`if (!AnyAsync)`) means new orders only appear on a clean DB.

## Next recommended action
UX polish: disable/tooltip su azioni bloccate (Start Work senza operatore presente, Declare senza sessione Work), poi quality readings WebClient.
