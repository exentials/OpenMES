# AI Continuous Development Workflow

This workflow is designed for AI coding agents (GitHub Copilot, Claude, Gemini) working continuously on this repository.

## Session start protocol
1. Read `.github/instructions/README.md`.
2. Read `.github/instructions/ROADMAP_SESSION_STATUS.md`.
3. Read the project instruction file(s) under `.github/instructions/projects/` for touched projects.
4. Read area-specific pattern files only if relevant.

## Execution protocol
1. Gather current code context from actual files (no assumptions).
2. Apply minimal, scoped changes.
3. Keep server-side validation authoritative.
4. Update tests in same change-set for behavioral modifications.
5. Run build and targeted tests before finalizing.

## Repository path reminders
- Solution file path: `OpenMES.slnx` (repository root)
- Automation scripts path: `tools/` (repository root)
- Example script invocation (PowerShell): `pwsh ./tools/scan-localization.ps1 -Root .`

## Documentation protocol
- All contributor/AI instructions live under `.github/instructions/`.
- Keep `README.md` in repo root for GitHub presentation only.
- Do not create mirrored instruction trees under `src/.github`.

## Handoff protocol
When ending a session, update `ROADMAP_SESSION_STATUS.md` with:
- what was completed,
- what remains,
- concrete next actions,
- risks/blockers.

## Decision log rule
If a business/process rule is clarified by the user:
1. encode it in code/tests,
2. update relevant project instruction file,
3. reflect status in roadmap file.

## Current critical business decisions
- Terminal login authenticates the terminal device, not the operator.
- Terminal usage is multi-user.
- Operator presence is shift-event based.
- Work session opening requires selecting a currently present operator.
