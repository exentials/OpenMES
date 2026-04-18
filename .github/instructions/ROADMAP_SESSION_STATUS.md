# OpenMES Development Status

**Last Updated:** 2026-04-18
**Branch:** `main`
**Build Status:** ✅ Passing
**Test count:** 175 (all passing)

## Completed (recent)

### Domain and data
- Warehouse hierarchy implemented (`Plant -> Warehouse -> StorageLocation`).
- `Material` supports `DefaultWarehouseId` and `DefaultStorageLocationId`.

### Production validation
- Quantity validation rules active (Rule A and Rule B).
- Declarations enforce operator presence server-side (blocked if absent or on break).

### Multi-user terminal flow
- Terminal login authenticates the device, not the operator.
- Work-session opening requires selecting a present operator.
- `GET /operatorshift/present` endpoint available and used by WebClient.

### Enum localization (WebAdmin)
- `EnumTextLocalizer` static class added to `PropertyColumnExt.cs`.
- EN/IT resource keys added for all display-relevant enums.
- Applied across 18 WebAdmin Razor files (badges, labels, select options).

### WebClient — check-in/check-out wiring
- 4th button in Action.razor 2×2 grid: "Check-in" / "Check-out" (label driven by `IsOperatorPresent`).
- `ActionScreen.CheckIn` renders `OperatorShiftScreen` inline.
- `GetCurrentStatusAsync` added to `OperatorShiftService` (GET /operatorshift/operator/{id}/current).
- `LoadContextAsync` loads current shift for selected operator on page load.

### WebClient — bug fixes
- **False "Failed to update operator shift" error**: `POST /operatorshift` returns `int` (id),
  not `OperatorShiftDto`; removed `|| result.Data is null` guard in `OperatorShiftScreen`.
- **`IApiService` error messages**: Added `ExtractErrorMessage()` to parse ProblemDetails JSON
  (RFC 7807) and return the `detail` field instead of raw JSON. Applied to all `CrudApiService`
  error branches.
- **Phase picker never shown**: `IApiService.JsonOptions` lacked `JsonStringEnumConverter` —
  enum fields in DTOs (e.g. `OrderStatus`) were all deserialized as `0` (Created), so the
  `Released/Setup/InProcess` filter in `EnsureActivePhaseAsync` returned 0 phases. Fixed by
  adding `JsonStringEnumConverter` to `JsonOptions`.
- **`StartWorkAsync` skipping phase selection**: Refactored flow so `StartWorkAsync` resolves
  operator + phase before showing `SessionTypePicker`. Added `PendingSessionType` to
  `ActionViewModel`; `OnPhaseSelectedAsync` completes the session open if a type is pending.

### Seed data
- Added PO4000 (3 phases on Turning), PO5000 (2 on Milling), PO6000 (2 on Grinding)
  to `MesDataSeeding.cs` so the phase picker is exercised during WebClient testing.

### Test coverage
- `OperatorShiftPresentTests.cs` — 15 tests for GET /operatorshift/present
- `WebClientShiftAndDeclarationFlowTests.cs` — 47 tests for shift transitions,
  presence derivation, declaration API preconditions, ActionViewModel derived properties.

### Quality gates
- Build passing. All 175 tests passing.

## Remaining work
1. Validate plant/terminal scoping for present operator list in real environment.
2. Review migration history cleanliness after recent reset/regeneration churn.
3. Expand docs wiki with state-transition and machine-stop troubleshooting examples.
4. Remaining WebClient screens: machine stop reason selection, setup session type, quality readings.
5. Remaining WebAdmin pages: ProductionDeclaration details, WorkSession details, ERP Export UI.

## Risks / attention points
- Presence filtering depends on latest shift event consistency — DB inconsistency breaks it.
- Keep EN/IT resource parity when adding new UI labels.
- `CanApplyEvent` logic is duplicated between `OperatorShiftClientService` and test suite
  — update both if logic changes.
- Seed data guard (`if (!AnyAsync)`) means new orders only appear on a clean DB.

## Next recommended action
Machine stop reason selection screen in WebClient (`StateTransitionScreen` wiring in `Action.razor`).
