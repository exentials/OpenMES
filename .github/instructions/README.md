# AI/Developer Instructions Index

## Important: Documentation Policy
**All contributors must read and follow `.github/DOCUMENTATION_POLICY.md`**

Public-facing documentation (README, docs/, user guides) must NOT contain:
- References to Copilot or AI-assisted development tools
- Internal prompts, instructions, or development workflows
- Mentions of configuration files used for AI-assisted coding

---

## Instruction Map

### 1. **Global Development Workflow**
- **AI-WORKFLOW.md** — Day-to-day workflow for Copilot-assisted development

### 2. **Copilot custom instruction files (`*.instructions.md`)**
- **solution.instructions.md** — Rules on solution files (`*.sln`, `*.slnx`)
- **project.instructions.md** — Rules on project files (`*.csproj`)
- **issues.instructions.md** — Mandatory issue tracking format and categories
- **tests.instructions.md** — Test quality and coverage-gap analysis
- **windows.instructions.md** — Windows + PowerShell execution constraints

### 3. **Architectural Patterns**
- **PATTERNS.md** — Technology stack, entities, DTOs, enums, controllers, authentication
- **PATTERNS_WEBADMIN.md** — Blazor WebAdmin patterns, forms, grids, components
- **PATTERNS_WEBCLIENT.md** — WebClient patterns, ViewModels, terminal UI
- **PATTERNS_EDIT_DIALOG_CONTAINER.md** — Unified Edit dialog component pattern

### 4. **Domain Model & Business Rules**
- **DECLARATIONS.md** — Operator presence, work sessions, machine state, ERP export

### 5. **Project Status**
- **ROADMAP_SESSION_STATUS.md** — Development status, completed work, remaining tasks

### 6. **Product Documentation (Read-Only for Context)**
- `../../docs/` — Functional wiki for end-users and operators

### 7. **Project-Specific Instructions**
- `projects/OpenMES.WebApi.md` — API controller guidelines and operational rules
- `projects/OpenMES.WebClient.md` — terminal UI flow and operator presence integration
- `projects/OpenMES.WebAdmin.md` — admin UI behavior and page-level conventions
- `projects/OpenMES.Tests.md` — test targeting and coverage guardrails
- `projects/OpenMES.Data-and-Migrations.md` — data model and migration guidance

---

## Canonical Entry Points

- **For Copilot**: Start with `.github/copilot-instructions.md`
- **For developers**: Start with this file (README.md)
- **For governance**: See `.github/DOCUMENTATION_POLICY.md`

---

## Quick Reference: Which File to Read

| Situation | File |
|-----------|------|
| Working on solution-level docs/structure | solution.instructions.md |
| Working on a project file | project.instructions.md |
| Tracking defects and risks | issues.instructions.md |
| Reviewing tests/coverage gaps | tests.instructions.md |
| Enforcing Windows shell/path rules | windows.instructions.md |
| Adding new entity or DTO | PATTERNS.md § 3-5 |
| Building WebAdmin page | PATTERNS_WEBADMIN.md § 11 |
| Building WebClient screen | PATTERNS_WEBCLIENT.md |
| Implementing Edit dialog | PATTERNS_EDIT_DIALOG_CONTAINER.md |
| Working with declarations/presence | DECLARATIONS.md |
| Working on WebApi endpoints | projects/OpenMES.WebApi.md |
| Working on WebClient flow | projects/OpenMES.WebClient.md |
| Working on WebAdmin pages | projects/OpenMES.WebAdmin.md |
| Working on migrations/data model | projects/OpenMES.Data-and-Migrations.md |
| Working on tests | projects/OpenMES.Tests.md |
| Understanding daily workflow | AI-WORKFLOW.md |
| Checking project status | ROADMAP_SESSION_STATUS.md |
| Learning business rules | PATTERNS.md § 0, DECLARATIONS.md |

---

## Non-Negotiable Rules

### Business Logic
- Terminal authentication identifies the **device**, not the operator
- Operator presence managed via shift events (CheckIn, CheckOut, BreakStart, BreakEnd)
- Work session opening requires selecting a present operator
- Operator check-in/check-out separate from machine declarations

### Technical Patterns
- Use `RestApiControllerBase<TEntity, TDto, TKey>` for standard CRUD
- Use `RestKeyValueApiControllerBase<TEntity, TDto, TKey>` for keyvalue lookup endpoints
- DTOs must have `[Display]` attributes with `DtoResources` localization
- Grid columns in Blazor use `PropertyColumnExt` for auto-localization
- Validation errors thrown as `ProblemException` with user-friendly messages

---

## File Organization

```
.github/
  DOCUMENTATION_POLICY.md          ← READ FIRST: Public doc governance
  copilot-instructions.md          ← Entry for Copilot
  instructions/
    README.md                       ← You are here
    AI-WORKFLOW.md                  → Daily workflow
    solution.instructions.md        → Solution-scope copilot rules
    project.instructions.md         → Project-scope copilot rules
    issues.instructions.md          → Issues tracking rules
    tests.instructions.md           → Test analysis rules
    windows.instructions.md         → Windows execution rules
    PATTERNS.md                     → Core patterns
    PATTERNS_WEBADMIN.md            → WebAdmin UI patterns
    PATTERNS_WEBCLIENT.md           → WebClient UI patterns
    PATTERNS_EDIT_DIALOG_CONTAINER.md → Edit dialog component
    DECLARATIONS.md                 → Domain model (presence, declarations)
    ROADMAP_SESSION_STATUS.md       → Project status
    projects/
      OpenMES.WebApi.md             → API controller guidelines
      OpenMES.WebClient.md          → Terminal UI guidelines
      OpenMES.WebAdmin.md           → Admin UI guidelines
      OpenMES.Tests.md              → Testing guidelines
      OpenMES.Data-and-Migrations.md → Data/migration guidelines
```

---

## Getting Started

1. **Read DOCUMENTATION_POLICY.md** — Understand what goes in public docs
2. **Read AI-WORKFLOW.md** — Understand the daily development process
3. **Read `*.instructions.md` relevant to file scope** — Enforce Copilot rule binding
4. **Read PATTERNS.md** — Foundation of architectural patterns
5. **Read domain-specific pattern** — PATTERNS_WEBADMIN.md, PATTERNS_WEBCLIENT.md, DECLARATIONS.md as needed
6. **Read project-specific instruction** — Select the touched project file under `instructions/projects/`
7. **Check ROADMAP_SESSION_STATUS.md** — Know what's completed and what's next

---

## Repository layout reminders

- Solution file is at repository root: `OpenMES.slnx`
- Automation/maintenance scripts are in root `tools/`

---

**Last Updated**: 2026-04-21
**Status**: Complete
