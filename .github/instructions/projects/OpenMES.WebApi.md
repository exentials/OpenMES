# OpenMES.WebApi Instructions

## Purpose
`OpenMES.WebApi` contains operational business rules and terminal/admin API endpoints.

## Core API base patterns
- Use `RestApiControllerBase<TEntity, TDto, TKey>` for standard CRUD endpoints.
- Use `RestKeyValueApiControllerBase<TEntity, TDto, TKey>` when the entity must expose `/keyvalue` lookup data.
- `RestKeyValueApiControllerBase` requires:
  - `TEntity` implementing `IKeyValueDtoAdapter<TEntity, TDto, TKey>`
  - `TDto` implementing `ISelectableDto`

## Query pipeline conventions
- Override `Query` only to apply includes/filters shared by both reads and writes.
- Read endpoints (`Reads`, `Read`, `ReadKeyValues`) use `ReadQuery` (no tracking).
- Write paths keep tracked entity updates via context tracking/`FindAsync`.
- Do not duplicate manual `AsNoTracking()` logic in each controller when base pipeline already applies it.

## KeyValue endpoint conventions
- Standard route: `GET /{controller}/keyvalue`
- Standard payload: `IEnumerable<KeyValueDto<TKey>>`
- Request headers supported by client service:
  - `x-term`
  - `x-limit`
  - optional `x-filter-*` headers for future filter expansion

## Critical controllers
- `WorkSessionController`
- `ProductionDeclarationController`
- `OperatorShiftController`
- `TerminalController`

## Non-negotiable rules
- Enforce operator presence server-side (never rely only on UI).
- Keep validations deterministic and return consistent `ProblemException` semantics.
- Preserve terminal scheme support (`JWT,TerminalScheme`) on operational endpoints.

## Recent domain behavior
- Declarations are blocked if operator is not present or is on break.
- Work sessions are blocked for invalid presence/machine/phase states.
- `operatorshift/present` endpoint is available for current presence lookup.

## Implementation checklist
1. Validate business constraints before persistence.
2. Update phase/order aggregates after declaration/session changes.
3. Keep ERP correction/reversal behavior backward-compatible.
4. Add/adjust tests whenever validation logic changes.

## Tests to run
- `OpenMES.Tests.ProductionDeclarationTests`
- `OpenMES.Tests.WorkSessionOpenValidationTests`
- `OpenMES.Tests.OperatorShiftTests`
