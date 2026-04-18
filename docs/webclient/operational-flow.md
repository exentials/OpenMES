# WebClient Operational Flow

This page describes the typical operational flow for shop-floor usage.

## A) Shift presence flow (attendance)
1. Operator arrives at department.
2. Operator performs `CheckIn` (start shift).
3. During shift, operator may perform `BreakStart` and `BreakEnd`.
4. At shift end, operator performs `CheckOut`.

### Notes
- Shift events track attendance and availability.
- Shift events are independent from terminal login.

## B) Machine and work-session flow
1. Terminal is authenticated (`ClientDevice` login).
2. User selects a machine from Home.
3. User opens Action page for the machine.
4. If required, user selects active phase/bolla.
5. User starts a work session (`Work`, `Setup`, etc.).
6. Operator is selected from the **present operator list**.
7. Session is opened.
8. Session can be closed later by user action or by shift end logic.

## C) Production declaration flow
1. Operator/session context is active on Action page.
2. User enters confirmed and scrap quantities.
3. User confirms declaration.
4. API validates:
   - operator presence,
   - phase/order quantity rules,
   - declaration consistency.
5. If valid, declaration is saved and aggregates are updated.

## D) Stop / suspension flow
1. While machine is running, user can request stop/suspension.
2. User selects stop reason.
3. Machine state and machine-stop record are created.
4. Resume/start actions move machine back to valid operational state.

## E) Error/guardrail cases
- If no present operators exist, session opening is blocked.
- If operator is on break or checked out, declarations are blocked.
- If quantities violate rules, declaration is rejected with a domain error.

## F) Sequence snapshot
1. `Terminal login`
2. `Operator CheckIn`
3. `Machine selection`
4. `Phase/bolla selection`
5. `Open session with present operator`
6. `Declare production`
7. `Optional stop/suspend/resume`
8. `Close session`
9. `Operator CheckOut`
