---
applyTo:
  - "**/*Test*/**/*.cs"
---

# Tests Instructions

## Scope
- Analyze test code quality.
- Analyze coverage confidence.

## Check points
- Missing edge cases.
- Missing negative paths.
- Fragile tests (time/random/order/env dependent).
- Slow tests without reason.
- Missing integration boundaries.
- Assertions too weak.

## Coverage gaps
- Map major production paths.
- Mark untested critical behavior.
- Mark security/performance-sensitive gaps.

## Reporting
- Report only in:
  - `ISSUES.md`
  - `README.md` (Testing section)

## Rules
- No fake coverage numbers.
- Use factual observations.
- Keep findings actionable.
