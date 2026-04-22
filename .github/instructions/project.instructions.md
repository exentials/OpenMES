---
applyTo:
  - "**/*.csproj"
---

# Project Instructions

## Scope
- Run per project file.
- Project folder = doc output folder.

## Required files per project folder
- `README.md`
- `AI-CONTEXT.md`
- `PATTERNS.md`
- `ISSUES.md`

## README.md
- Purpose of project.
- Public API/surface (if any).
- Main dependencies.
- Build/run/test command (PowerShell + dotnet).
- Keep short.

## AI-CONTEXT.md
- Minimal facts only.
- Project type.
- Key entry points.
- External dependencies.
- Config/environment notes.

## PATTERNS.md
- Deduced architecture.
- Dependency direction.
- Naming patterns.
- Data access patterns.
- Error handling patterns.
- Localization rules.
- Add anti-patterns found.

## ISSUES.md
- Track bugs and risks for this project.
- Use strict issue format from `issues.instructions.md`.

## Tooling detection
- Detect and record when present:
  - EF Core
  - MediatR
  - Serilog
  - FluentValidation
  - xUnit/NUnit/MSTest
  - Aspire
  - Blazor

## Anti-pattern tracking
- Record:
  - God classes
  - Layer violations
  - Hardcoded strings in UI
  - Missing cancellation tokens
  - Unbounded queries
  - Missing tests for critical paths

## Rules
- Merge existing content.
- Remove duplicates.
- Keep intent.
- Do not invent behavior.
