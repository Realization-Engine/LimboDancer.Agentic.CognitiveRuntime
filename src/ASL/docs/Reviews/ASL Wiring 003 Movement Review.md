# Wiring 003 movement implementation and review

## Unresolved scope

W-A23.3 still refuses placement from a Bypass position and placement into a Cave. The Bypass path needs a record that preserves that position; the Cave procedure is not implemented. Backlog section 86 records both. W-A10.531's old terrain-only public members remain: automatic approval review rejected their removal under the compatibility requirement. Their callers now use the fact-based calculators. These omissions prevent an unqualified claim that every listed removal is complete.

## Implementation

The user approved implementation on 2026-10-30. `codex/wire-movement-1` depends on the Rules supplements in PRs 75 and 77. The Rules library and callers remain separate claims and PRs.

Movement, advance, Dash, and Infantry Overrun gather terrain, month, sight, and cover for the Open Ground calculator. Concealment choices use the same qualifying seers as the loss decision. Advance applies the result after a successful PAATC too. The Workbench gathers smoke and cover from the viewer's units and visible locations; it cannot use a hidden unit to answer the other side's question. Unknown seasonal facts remain unknown when they change the verdict.

New DC placements record nullable `SettlementVersion = 1`, omitted when null. Null retains the prior settlement reading. A new placement settles on departure, another unit's MPh movement, or phase end. Entry attempts and MF-bearing position changes count as another unit's movement; unrelated position effects do not. Pinning, breaking, or elimination before settlement permanently cancels the pending placement. After settlement they do not undo it. Eliminating the placer before settlement leaves the DC at the placer's location. Placement, PAATC, cost, detonation window, and removal ask the DC calculator.

## Review and evidence

The independent read-only movement review found and prompted fixes for advance after PAATC, concealment choices using the wrong seers, unknown facts that cannot change a verdict, and entry attempts settling DC placement. Its final boundary audit included SW recovery, Gun hooking, and failed Manhandling: budget changes on existing units and the Manhandling event cover those without treating lineage's inherited expenditure as new movement. Two focused recovery/failed-push regressions passed before the final strict run. The seasonal adapter review accepted its corrections at 2284e684.

- `test-classes/Play.log`: nine focused DC and seasonal movement cases passed after the entry-boundary fix. Earlier, 18 cases including minigame checks passed.
- `test-classes/Units.log`: three DC record round-trip cases passed, including omission of the new property on legacy records.
- `test-classes/Rules.log`: three seasonal adapter methods passed after review corrections.
- `test-classes/MapStudio.log`: the Rules-only Workbench expectation passed. The movement gatherer's expectation is checked separately after its final build.
- `gate-build-wire-movement-1.log`: the first strict build passed with zero warnings and errors; final review changes require the current strict run and replay gate before acceptance.

All logs above are under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`. The three fresh Studio games are `wire-dc-departure`, `wire-dc-other-mover`, and `wire-dc-phase-end`. Each was played through the governed UI on port 6680 in Codex's own live-game folder. The first settles when the placer leaves [D10]; the second remains pending at end-move and settles when the other squad moves; the third settles at phase end, then detonates in the AFPh and removes the DC. The actual events are included in the gate's own-game list. The initial Studio launch omitted the LOS fixture path and refused adjacency; supplying the path corrected the harness without changing game code.

The baseline has 126 games, 3,471 replay lines, and 55,153 sweep lines. `p71-sniper-residual` reports NOSTATE before these changes too. Changed-side comparison and CI are pending; those counts alone are not a gate result.

PR 78's first CI run found that a broken placer replaced through lineage lost its DC. Pending new-reading placements now follow an active successor only when it is the actual DC holder and its lineage names the consumed placer. The original placement and attack facts stay recorded. Independent review accepted that correction; legacy and already-operable charges retain their existing paths. Nine affected DC cases passed against the strict build (`wiring003-dc-lineage.log`). A further focused run passed both the broken-placer regression and a new unbroken-successor case: inherited MF leaves placement pending, the successor holds the DC, and phase end settles it (`test-classes/Play.log`).

## Memory and performance

Concealment reads only when a concealed or dummy mover needs the day/assault decision. It reuses the state's sight/map reads and collects qualifying seer indexes once for the decision and later choices. The collection is local to that request, with no retained cache or concurrent mutable state. Infantry Overrun materializes only the neighboring candidates and gives Rules their entry results. DC projection scans the existing pending-charge list. Only when a new-reading placement remains pending and the unit list changes in the MPh does it compare budgets, once for all pending charges using a local ID index. It retains no new process-wide state. No rendering or counter-fragment path changes. No speed or memory improvement is claimed.

Focused tests reuse their existing fixture infrastructure. No new project or project reference is added. Versioned records and the replay gate protect old games; side-specific Workbench checks protect disclosure. Codex's Studio process is stopped after use. Registered outputs remain until their gate and CI use is complete, then only this work's outputs are cleaned.
