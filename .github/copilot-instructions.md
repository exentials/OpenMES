# Copilot Instructions

## Documentation Policy
All contributors must follow `.github/DOCUMENTATION_POLICY.md` — public documentation must remain neutral and must not reference AI tools, internal prompts, or development workflows.

## Canonical source
Use `.github/instructions/README.md` as the mandatory index for all AI/developer instructions.

## Instruction hierarchy
1. Documentation governance: `.github/DOCUMENTATION_POLICY.md`
2. Global workflow: `.github/instructions/AI-WORKFLOW.md`
3. Project rules: `.github/instructions/projects/*.md`
4. Shared patterns: `.github/instructions/PATTERNS*.md`, `.github/instructions/DECLARATIONS.md`
5. Functional product docs (read-only for behavior context): `docs/**`
6. Source code: `src/**`

## Non-negotiable business rules
- Terminal authentication identifies the device, not the operator.
- Terminal usage is multi-user.
- Operator presence is managed by shift events (`CheckIn`, `CheckOut`, `BreakStart`, `BreakEnd`).
- Opening a work session requires selecting a currently present operator.
- In `OpenMES.WebClient`, operator check-in/check-out remains separate from machine declaration flow.

## Non-negotiable technical rules
- Use `RestApiControllerBase<TEntity, TDto, TKey>` for standard CRUD controllers.
- Use `RestKeyValueApiControllerBase<TEntity, TDto, TKey>` for controllers exposing lookup endpoint `/keyvalue`.
- Keep `Query` overrides for shared includes only; rely on base `ReadQuery` for no-tracking reads.
- In WebAdmin lookup UIs, prefer shared select components backed by `ICrudKeyValueApiService` and `KeyValueDto<TKey>`.
- In the `OpenMES.WebClient` project, localize UI texts using resources (UiResources/DtoResources) to avoid hardcoded strings in Razor and code-behind.

## Governance rules
- Keep AI/developer instructions only under `.github/instructions/`.
- Keep project/product README files in place.
- Consolidate before deleting; do not duplicate instruction trees (including `src/.github`).
- Keep development reports (analysis, status) in `.github/internal/` or equivalent, never in public doc areas.