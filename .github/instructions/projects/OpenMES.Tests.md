# OpenMES.Tests Instructions

## Purpose
`OpenMES.Tests` protects business rules and integration-sensitive behavior.

## Priority suites
- `ProductionDeclarationTests`
- `WorkSessionOpenValidationTests`
- `OperatorShiftTests`
- `QuantityValidationTests`

## Test design rules
- Use `ProblemException.Error` for assertions where applicable.
- Keep seed setup explicit and deterministic (`TestDbFactory`).
- Prefer scenario names in the form `Action_Context_ExpectedResult`.

## Mandatory updates when behavior changes
- If declaration or session validation changes, update related tests in same PR.
- If seed model changes, update `TestDbFactory` once, then all impacted tests.

## Current gaps to close
- Add tests for `operatorshift/present` endpoint filtering.
- Add WebClient-focused declaration flow tests (component or e2e).
