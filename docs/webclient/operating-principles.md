# WebClient Operating Principles

## 1. Authentication model
- Terminal login authenticates the **device** (`ClientDevice`), not the operator.
- The terminal can be used by multiple operators during the same shift window.

## 2. Presence model
Operator presence is tracked by shift events:
- `CheckIn` (start shift)
- `BreakStart` (pause start)
- `BreakEnd` (pause end)
- `CheckOut` (end shift)

An operator is considered **present** when the latest shift event is:
- `CheckIn` or
- `BreakEnd`

An operator is considered **not available** when the latest shift event is:
- `BreakStart` (on break)
- `CheckOut` (not in shift)

## 3. Session model on machine/order phase
- Work sessions are opened on a machine for a selected order phase (bolla/phase).
- At session opening, an operator must be chosen among **currently present** operators.
- Presence is enforced both in UI flow and server validation.

## 4. Declaration model
A production declaration is accepted only if:
- phase/order constraints are valid (Rule A and Rule B), and
- operator is currently present (not checked out, not on break).

## 5. Multi-user terminal behavior
- Operator selection is explicit for operational actions.
- The terminal does not hold a single "logged operator" identity.
- Shift events and session/declaration actions can involve different operators over time.
