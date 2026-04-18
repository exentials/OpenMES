# OpenMES.WebClient Instructions

## Purpose
`OpenMES.WebClient` is the terminal/operator Blazor UI. It is terminal-authenticated and multi-user.

## Functional wiki references
- `docs/webclient/operating-principles.md`
- `docs/webclient/operational-flow.md`

## Core business rules
- Terminal login authenticates the device, not the operator.
- Operator presence is tracked via shift events (`CheckIn`, `CheckOut`, `BreakStart`, `BreakEnd`).
- Operator check-in/check-out must remain separate from machine declaration, because presence does not depend on the machine.
- When opening a machine/bolla work session, the operator must be selected among currently present operators.

## Key files
- `src/OpenMES.WebClient/Components/Pages/Home.razor*`
- `src/OpenMES.WebClient/Components/Pages/Action.razor*`
- `src/OpenMES.WebClient/Components/Actions/*`
- `src/OpenMES.WebClient/ViewModels/*`

## Integration points
- `MesClient.OperatorShift.GetPresentAsync(...)`
- `MesClient.WorkSession.OpenSessionAsync(...)`
- `MesClient.ProductionDeclaration.CreateAsync(...)`

## Implementation checklist
1. Keep UI flow machine-centric, not operator-login-centric.
2. Use `RunAsync` wrappers and existing error service pattern.
3. Keep screen-state transitions explicit (`ActionScreen`).
4. Add EN/IT localization keys for all new labels/errors.
5. Prefer component-level reuse (`NumericPad`, dialogs).

## Tests to touch when changing WebClient flow
- `OpenMES.Tests.WorkSessionOpenValidationTests`
- `OpenMES.Tests.ProductionDeclarationTests`
- Add UI tests (bUnit/Playwright) where missing.

## Current known gaps
- Add dedicated WebClient tests for declaration UX and present-operator selection behavior.
- Validate terminal/plant scoping for present operators end-to-end in UI.
