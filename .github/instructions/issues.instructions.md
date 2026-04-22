---
applyTo:
  - "**/*"
---

# Issues Instructions

## Mandatory
- Create/update `ISSUES.md` where analysis is done.
- Keep one file per logical area/project when possible.

## Track categories
- Bugs
- Bug risks
- Architecture flaws
- Security issues
- Performance risks
- Test gaps

## Strict format
For each issue use:

- `ID: ISS-###`
- `Severity: Critical | High | Medium | Low`
- `Priority: P1 | P2 | P3`
- `Area: <project/module>`
- `Problem: <one line>`
- `Fix: <one line>`
- `Reference: <file path + line if possible>`

## Rules
- One issue = one problem.
- No vague issue text.
- No duplicates.
- Keep IDs stable.
- New issue increments last number.

## Reporting locations
- `ISSUES.md` is source of truth.
- Important test gaps also summarized in project `README.md` testing section.
