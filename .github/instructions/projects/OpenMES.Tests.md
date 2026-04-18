# OpenMES.Tests Instructions

## Purpose
`OpenMES.Tests` protects business rules and integration-sensitive behavior.

## Priority suites
- `ProductionDeclarationTests`
- `WorkSessionOpenValidationTests`
- `OperatorShiftTests`
- `OperatorShiftPresentTests`
- `WebClientShiftAndDeclarationFlowTests`
- `QuantityValidationTests`

## Test design rules
- Use `ProblemException.Error` for assertions where applicable.
- Keep seed setup explicit and deterministic (`TestDbFactory`).
- Prefer scenario names in the form `Action_Context_ExpectedResult`.

## Mandatory updates when behavior changes
- If declaration or session validation changes, update related tests in same PR.
- If seed model changes, update `TestDbFactory` once, then all impacted tests.

## Current gaps to close
- Add UI-level tests (bUnit/Playwright) for WebClient start-work flow continuity after operator selection.
- Add UI coverage for phase-picker branching (single phase auto-select vs multi-phase explicit selection).
- Expand `operatorshift/present` scenarios for plant/terminal scoping once rules are finalized.
