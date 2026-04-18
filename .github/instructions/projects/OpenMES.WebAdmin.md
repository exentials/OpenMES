# OpenMES.WebAdmin Instructions

## Purpose
`OpenMES.WebAdmin` is the master-data and admin operations Blazor UI.

## UI conventions
- Use `BasePage` and `BaseEdit` standard patterns.
- Add button pattern: `@OnClick="@AddInDialog"` with add icon.
- Dialog methods naming: `AddInDialog`, `EditInDialog`, `DeleteInDialog`.
- Keep dialog protections: `PreventDismissOnOverlayClick=true`, `PreventScroll=true`.

## Field conventions
- Use `DtoResources` for labels.
- Keep EN/IT resources aligned.
- Avoid hardcoded user-facing strings.

## Affected areas for domain changes
- Material defaults (warehouse/location)
- StorageLocation CRUD (Warehouse FK)
- OperatorShift admin views

## Tests/validation
- Ensure builds compile generated Razor artifacts.
- Add integration tests when changing form semantics or data contracts.
