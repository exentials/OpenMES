---
applyTo:
  - "**/*.sln"
  - "**/*.slnx"
---

# Solution Instructions

## Scope
- Run on solution files.
- Main solution: `OpenMES.slnx`.

## Discovery
- Discover projects recursively from solution root.
- Include all real app projects (`*.csproj`).
- Include all real test projects.
- Exclude local-only projects by name/path:
  - `Sandbox`
  - `Playground`
  - `DemoLocal`
  - `LocalTest`
  - `ManualHost`

## Output files
- Generate/update root `README.md`.
- Keep architecture summary current.
- Keep project list current.
- Keep dependency overview current.

## README rules
- Show solution purpose.
- Show high-level architecture.
- Show major project groups.
- Show run/build/test commands for Windows + PowerShell.
- Keep text concise.
- No AI/internal workflow text in public docs.

## Architecture overview rules
- Describe real layers only.
- Show dependency direction only from code/facts.
- Flag cyclic dependencies into `ISSUES.md`.

## Dependency overview rules
- Capture key shared libs and frameworks.
- Capture provider split (SqlServer/Pgsql) when present.
- Capture orchestration (AppHost/MigrationService) when present.

## Validation
- Verify excluded local-only projects are not documented.
- Verify real test projects are listed.
- Verify README reflects actual repo state.
