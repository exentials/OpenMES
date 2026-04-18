# Declaration System — Domain Model

> This document describes the data model and business rules for the declaration system
> in OpenMES. It covers operator presence tracking, work session management, machine
> state tracking, and time allocation logic.
>
> Read this document before implementing any entity, controller, or service related
> to declarations.

---

> **Status: IMPLEMENTED** — see commit history for entity/controller/DTO details.
> This document reflects the final implemented model.

## Overview

Declarations are events recorded by operators or machines that describe what is
happening on the shop floor at any given moment. They serve two purposes:

1. **Real-time monitoring** — knowing who is doing what, on which machine, on which
   production order phase, right now.
2. **Time tracking at completion** — computing how many hours each resource contributed
   to each phase, redistributed according to a configurable allocation rule.

Declarations can originate from two sources:

- **Manual** — an operator interacts with the shop floor terminal (WebClient).
- **Automatic** — a machine sends events directly via the API (e.g. a PLC or SCADA
  system).

The `Source` field on every declaration entity records which source generated it.

---

## Entities

### 1. `OperatorShift`

Records a single presence event for an operator. The full shift timeline is
reconstructed by reading all events for an operator in chronological order.

```
OperatorShift
├── Id                  int PK
├── OperatorId          int FK → Operator
├── EventType           OperatorEventType
│                         CheckIn      = 0  operator arrives and starts their shift
│                         CheckOut     = 1  operator leaves; all open WorkSessions
│                                           on this operator are force-closed
│                         BreakStart   = 2  operator goes on break; work sessions
│                                           are NOT closed but time stops accumulating
│                         BreakEnd     = 3  operator returns from break
├── EventTime           DateTimeOffset      when the event occurred
├── Source              string              "Manual" | "Terminal" | "Machine"
└── Notes               string?
```

**Derived state (computed, not stored):**

An operator is considered **present** if their last `OperatorShift` event is a
`CheckIn` or a `BreakEnd`.

An operator is considered **on break** if their last event is a `BreakStart`.

An operator is considered **absent** if they have no events today, or if their
last event is a `CheckOut`.

---

### 2. `WorkSession`

Represents a period of work by a single operator on a single production order phase
on a single machine. A session has a type (Setup, Work, Wait, Rework), a start time,
and optionally an end time.

```
WorkSession
├── Id                      int PK
├── OperatorId              int FK → Operator
├── ProductionOrderPhaseId  int FK → ProductionOrderPhase
├── MachineId               int FK → Machine
├── SessionType             WorkSessionType
│                             Setup   = 0  machine/tool preparation before production
│                             Work    = 1  active production on the phase
│                             Wait    = 2  operator is waiting (material, instruction, etc.)
│                             Rework  = 3  corrective work on already-produced pieces
├── Status                  WorkSessionStatus
│                             Open    = 0  session is currently active
│                             Closed  = 9  session has ended
├── StartTime               DateTimeOffset
├── EndTime                 DateTimeOffset?     null while the session is Open
├── AllocatedMinutes        decimal             computed at close; the share of total
│                                               phase minutes attributed to this session
│                                               after applying the machine's allocation rule
├── Source                  string              "Manual" | "Machine"
└── Notes                   string?
```

**Session lifecycle:**

1. Operator opens a session → `Status = Open`, `EndTime = null`.
2. Operator closes the session explicitly → `Status = Closed`, `EndTime = now`,
   `AllocatedMinutes` is recomputed for all sessions on the same phase.
3. Operator checks out → all their open sessions are force-closed with
   `EndTime = CheckOut.EventTime`.

**Concurrent sessions:**

Whether an operator can have more than one open session simultaneously is controlled
by the `AllowConcurrentSessions` flag on the `Machine` entity. If false, opening a
new session automatically closes any other open session on that machine for that
operator.

---

### 3. `MachineState`

An append-only log of machine status changes. Each record represents one state
transition. The current state of a machine is always the most recent record.

```
MachineState
├── Id          int PK
├── MachineId   int FK → Machine
├── Status      MachineStatus
│                 Running     = 0  producing normally
│                 Idle        = 1  on but not producing
│                 Setup       = 2  being prepared for a new production run
│                 Stopped     = 3  intentionally stopped (end of shift, break, etc.)
│                 Maintenance = 4  under maintenance, not available for production
├── EventTime   DateTimeOffset
├── Source      string              "Manual" | "Machine"
└── OperatorId  int? FK → Operator  the operator who declared the state change,
                                    null if the machine declared it automatically
```

**Records are never updated or deleted.** To correct an error, a new record is
inserted. This preserves a complete audit trail of all state transitions.

---

## Enums

### `OperatorEventType`
```
CheckIn    = 0   Operator arrives and begins their shift.
CheckOut   = 1   Operator leaves. All open WorkSessions are force-closed.
BreakStart = 2   Operator starts a break. Sessions remain open but time pauses.
BreakEnd   = 3   Operator returns from break. Time resumes on open sessions.
```

### `WorkSessionType`
```
Setup   = 0   Preparation of machine or tooling before starting production.
Work    = 1   Active production on a phase of a production order.
Wait    = 2   Operator is waiting (e.g. for material, instructions, or a machine).
Rework  = 3   Corrective work on pieces that did not pass quality control.
```

### `WorkSessionStatus`
```
Open   = 0   Session is currently active. EndTime is null.
Closed = 9   Session has ended. EndTime and AllocatedMinutes are set.
```

### `MachineTimeAllocationMode`
```
Uniform      = 0   Total phase minutes are divided equally among all operators
                   who had at least one WorkSession on the phase.
Proportional = 1   Each operator's share is weighted by their raw session duration
                   relative to the sum of all session durations on the phase.
```

---

## Machine configuration fields (added to `Machine`)

```
AllowConcurrentSessions   bool                        default: false
                          When true, an operator may have more than one open
                          WorkSession simultaneously on this machine.
                          When false, opening a new session auto-closes the previous one.

TimeAllocationMode        MachineTimeAllocationMode   default: Uniform
                          Determines how AllocatedMinutes is computed across operators
                          when multiple people work on the same phase.
```

---

## Business rules and validations

The following rules are enforced at the API level before any record is written.

### Opening a WorkSession

| # | Condition | Error |
|---|---|---|
| 1 | Operator is absent (no active CheckIn) | Cannot open session: operator is not checked in |
| 2 | Operator is on break | Cannot open session: operator is on break |
| 3 | Machine current state is Stopped or Maintenance | Cannot open session: machine is not available |
| 4 | Machine current state is Setup and SessionType = Work | Cannot open Work session: machine is in Setup |
| 5 | Phase Status is Closed or Completed | Cannot open session: phase is already closed |
| 6 | AllowConcurrentSessions = false and operator already has an open session on this machine | Previous session auto-closed before opening the new one |

### Recording an OperatorShift event

| # | Condition | Error |
|---|---|---|
| 7 | CheckIn when operator is already checked in | Operator already has an active shift |
| 8 | CheckOut when operator is absent | No active shift to check out from |
| 9 | BreakStart when operator is not present or already on break | Invalid break start |
| 10 | BreakEnd when operator is not on break | No active break to end |

### Declaring a MachineState

| # | Condition | Error |
|---|---|---|
| 11 | Transition to Running from Maintenance (only maintenance personnel can clear maintenance) | Handled at authorization level, not modeled here yet |

---

## Time allocation logic

`AllocatedMinutes` is recomputed on every WorkSession close event for **all closed
sessions on the same phase** (not just the one being closed). This ensures that as
more sessions close, the allocation is always up to date.

### Step-by-step

1. Load all `Closed` WorkSessions for the phase.
2. For each session, compute `RawMinutes = (EndTime - StartTime).TotalMinutes`.
   Break time is NOT subtracted from session duration at this level — breaks pause
   the declaration stream but do not split sessions.
3. Apply the machine's `TimeAllocationMode`:
   - **Uniform**: `AllocatedMinutes = TotalRawMinutes / NumberOfDistinctOperators`
     where `TotalRawMinutes` is the sum of all sessions' raw minutes.
   - **Proportional**: `AllocatedMinutes = RawMinutes / SumOfAllRawMinutes * TotalRawMinutes`
     This simply assigns each session its actual raw minutes (proportional to its
     own duration relative to the total).
4. Save the updated `AllocatedMinutes` on every session in the set.

---

## API endpoints (implemented)

### OperatorShift
- `POST /operatorshift` — record a presence event; validates state transitions; on CheckOut force-closes all open WorkSessions
- `GET /operatorshift` — paginated list of all events
- `GET /operatorshift/operator/{operatorId}/current` — current presence status of one operator
- `GET /operatorshift/operator/{operatorId}/date/{date}` — all events for an operator on a given date (yyyy-MM-dd)

### WorkSession
- `POST /worksession/open` — open a new session (validates rules 1–6; auto-closes previous if AllowConcurrentSessions = false)
- `POST /worksession/{id}/close` — close a session and recompute AllocatedMinutes for all closed sessions on the same phase
- `POST /worksession/{id}/correct` — correct a session (delete+recreate if not exported; reversal+new if exported)
- `GET /worksession` — paginated list of all sessions
- `GET /worksession/open` — all currently open sessions (live shop floor view)
- `GET /worksession/phase/{phaseId}` — all sessions for a phase
- `GET /worksession/pending-export` — closed sessions not yet sent to ERP

### MachineState
- `POST /machinestate` — record a state change (append-only)
- `GET /machinestate/machine/{machineId}/current` — current state of one machine
- `GET /machinestate/machine/{machineId}` — full state history for a machine
- `GET /machinestate/current/all` — current state of all machines (dashboard view)

### ProductionDeclaration
- `POST /productiondeclaration` — create a declaration
- `POST /productiondeclaration/{id}/correct` — correct a declaration (delete+recreate if not exported; reversal+new if exported)
- `GET /productiondeclaration` — paginated list
- `GET /productiondeclaration/pending-export` — declarations not yet sent to ERP

---

## ERP Export

### Overview

`WorkSession` and `ProductionDeclaration` are the two entities exported to the ERP.
They carry the actual production data: time per resource per phase and quantities
declared per phase.

Export batches are separate per entity type. The ERP identifies the phase via
`PhaseExternalId` (the confirmation number assigned by the ERP at order import,
copied from `ProductionOrderPhase.ExternalId` at record creation time).

### Fields on both entities

```
PhaseExternalId       string?           Snapshot of ProductionOrderPhase.ExternalId
                                        at record creation time. Copied once and never
                                        updated. Used by ERP to identify the phase.
                                        Transmitted in every export row.

ExternalCounterId     string?           Counter/ID returned by the ERP after successful
                                        acquisition. Null = not yet exported or confirmed.
                                        Once set, the record is considered exported and
                                        corrections must use the reversal pattern.

ErpExportedAt         DateTimeOffset?   When the record was transmitted to the ERP.
                                        Null if not yet exported.

IsReversal            bool              true = this record is a storno (reversal) with
                                        negated values, created to cancel a previously
                                        exported record.

ReversalOfId          int?              FK to the original record being reversed.
                                        Set only when IsReversal = true.

ReversedById          int?              FK to the reversal record that cancelled this
                                        record. Set on the original after reversal creation.
                                        Allows tracing the chain in both directions.
```

### Negated fields per entity

| Entity | Field negated in reversal |
|---|---|
| `WorkSession` | `AllocatedMinutes` |
| `ProductionDeclaration` | `ConfirmedQuantity`, `ScrapQuantity` |

### Correction flow

```
Correction requested on record R
│
├─ ExternalCounterId = null (not yet exported)
│   └─ DELETE R
│   └─ CREATE new record with corrected data
│      (PhaseExternalId copied from R)
│
└─ ExternalCounterId ≠ null (already exported)
    ├─ CREATE reversal S:
    │    IsReversal = true
    │    ReversalOfId = R.Id
    │    PhaseExternalId = R.PhaseExternalId
    │    Values = negated (AllocatedMinutes < 0 | Quantities < 0)
    │    Source = "Reversal"
    │    ExternalCounterId = null  ← to be exported
    │
    ├─ UPDATE R: ReversedById = S.Id
    │
    └─ CREATE corrected record C:
         IsReversal = false
         PhaseExternalId = R.PhaseExternalId
         Values = corrected
         ExternalCounterId = null  ← to be exported
```

### Guard rules on /correct endpoint

- Cannot correct a reversal record (`IsReversal = true`)
- Cannot correct a record already reversed (`ReversedById` is set)

### Pending export query

Records to include in the next export batch:
- `ExternalCounterId = null` (not yet confirmed by ERP)
- `ReversedById = null` (not cancelled — superseded records are excluded)
- For `WorkSession`: also `Status = Closed`

### ERP export endpoints (implemented in ErpExportController)

```
POST /api/erpexport/worksession
    → returns pending WorkSession rows as ErpExportResultDto
    → sets ErpExportedAt = now on all returned records
    → caller transmits Rows to ERP, then calls /confirm

POST /api/erpexport/worksession/confirm
    body: ErpConfirmationDto { Items: [ { RecordId, ExternalCounterId } ] }
    → writes ExternalCounterId on each confirmed session

POST /api/erpexport/productiondeclaration
    → same as above for ProductionDeclaration

POST /api/erpexport/productiondeclaration/confirm
    → same confirm flow for ProductionDeclaration
```

### ErpExportRowDto fields

| Field | Source |
|---|---|
| `RecordId` | Internal Id |
| `PhaseExternalId` | Snapshot of `ProductionOrderPhase.ExternalId` |
| `EntityType` | `"WorkSession"` or `"ProductionDeclaration"` |
| `SessionType` | WorkSession only (Setup/Work/Wait/Rework) |
| `AllocatedMinutes` | WorkSession only (negative if reversal) |
| `ConfirmedQuantity` | ProductionDeclaration only (negative if reversal) |
| `ScrapQuantity` | ProductionDeclaration only (negative if reversal) |
| `IsReversal` | true = storno |
| `ReversalOfExternalCounterId` | ERP counter of the original being reversed |
| `OperatorName` | Denormalized |
| `MachineCode` | Denormalized |
| `RecordDate` | StartTime (session) or DeclarationDate (declaration) |

### Two-phase export protocol

```
Phase 1 — Prepare batch:
  POST /api/erpexport/worksession
  → records get ErpExportedAt = now
  → rows returned to caller for transmission to ERP

Phase 2 — Confirm acquisition:
  POST /api/erpexport/worksession/confirm
  body: { Items: [ { RecordId: 42, ExternalCounterId: "ERP-7001" }, ... ] }
  → records get ExternalCounterId set
  → from this moment, corrections must use the reversal pattern
```

If Phase 2 never arrives (ERP rejected, network error), records have
`ErpExportedAt` set but `ExternalCounterId` still null — they will be
included in the next export batch automatically.

