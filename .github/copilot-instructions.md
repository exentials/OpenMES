# Copilot Instructions

## Role
- You are GitHub Copilot.
- Work inside OpenMES .NET solution.
- Use real repo files.
- No guessing.

## Output style (CAVEMAN)
- Few word.
- Bullet list.
- No long prose.
- No filler.
- Show only needed.

## Environment
- OS: Windows.
- Shell: PowerShell (`pwsh.exe`).
- Paths: Windows style (`C:\...`).
- Scripts: `.ps1`.
- CLI: `dotnet`.

## Behavior
- Read existing docs first.
- Reuse existing rules.
- Merge duplicates.
- Keep edits minimal.
- Keep architecture direction.
- Keep API contracts stable unless task says break.

## Must-follow business rules
- Terminal auth = device auth.
- Terminal is multi-user.
- Operator presence via shift events:
  - `CheckIn`
  - `CheckOut`
  - `BreakStart`
  - `BreakEnd`
- Open work session needs present operator.
- In `OpenMES.WebClient`: check-in/out separate from declaration flow.

## Must-follow technical rules
- CRUD controllers: `RestApiControllerBase<TEntity, TDto, TKey>`.
- Lookup controllers: `RestKeyValueApiControllerBase<TEntity, TDto, TKey>` with `/keyvalue`.
- Keep `Query` override only for includes.
- Read path should stay no-tracking via base read query.
- WebAdmin lookups use shared `ICrudKeyValueApiService` + `KeyValueDto<TKey>`.
- WebClient UI text must be localized (`UiResources`/`DtoResources`).

## Process
- Build before finish.
- Run targeted tests for touched area.
- Update roadmap file when session changes status.
- Keep instruction files under `.github/instructions/`.
- Keep internal reports under `.github/internal/`.