# Dice Lab

Design approved by the user on 2026-10-31. Branch: `codex/dice-lab-01`.

## Scope and implementation

`/lab/dice` is listed under Lab and hosts `Components/Shared/AslDice.razor`.
The component uses the existing `LimboDancer.Dice.DiceRoller` through the Studio's
existing Play reference. No project references or lock files change.

ASL A.1, printed page A1, defines dr, DR, the separate subsequent dr, and
Original versus Final. The rulebook preview was read during the preceding
explanation. The lab displays the individual white and colored values, their
comparison, the pair's sum, an entered modifier, and their arithmetic result.
A third die remains separate. No combat outcome, eligibility, or threshold is
decided by this component. No game dice are consumed or recorded.

Three modes cover single dr, paired DR, and paired DR with subsequent dr.
Changing mode clears the previous roll. A labeled example loads white 4,
colored 2, and modifier +1. Random rolls use six-sided dice. SVG faces show
pips and have accessible names. A live result region announces results.
The short CSS animation respects reduced motion. Controls support keyboard
operation and wrap on narrow screens.

## Memory and performance checklist

- Work is constant per roll: at most three generated values and eighteen pips.
  No per-game-state, per-unit, or per-hex work is added.
- Only the current values are retained in each component instance. No shared
  mutable cache, history, subscriptions, timers, disk reads, or background loop.
- Static pip coordinates are immutable in use. Dice generation uses the
  existing cryptographic generator. The final arithmetic uses a long to avoid
  overflow when adding a valid integer modifier.
- Disclosure, snapshots, pinned maps, seams, and counter fragment rendering
  are not accessed. The component updates only its own render tree.
- Check all modes, modifiers, clearing on mode change, the explicit example,
  accessible names, keyboard operation, and desktop/narrow layout. The user's
  follow-up prioritizes Studio verification and avoids unnecessary unit tests.
  No performance improvement is claimed.
- Existing test infrastructure is sufficient; no expensive fixtures or new
  test projects are needed.

## Build outputs outside the repository

Use `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` for tools.
Register `dice-lab-01-studio` beneath that folder before the Studio build.
Use port 6680. Stop only this task's Studio and remove only this task's build
outputs after verification. Preserve the user's requested worktree and logs.

## Limits

This is a standalone dice laboratory. Game integration and contextual combat
outcomes are future work, recorded in the backlog. No merge or publication is
part of this implementation request.

## Studio verification

The Studio build passed with zero warnings and errors, recorded in
`E:/Archive/GitHub/dlandi/asl-gate-work-codex/dice-lab-01-build.log`.
Browser checks covered the worked example (6 + 1 = 7), immediate modifier
updates (6 - 2 = 4), keyboard rolling, single-die results, mode reset, and a
third die excluded from the pair's total. At 320 pixels the controls and dice
wrap without horizontal overflow. Accessible SVG names match the visible pips.

Review caught that integer binding on each input event discarded a separately
typed minus sign. The component now retains modifier text, parses whole
integers, and shows incomplete input explicitly rather than displaying a stale
final result. Rolling is disabled until the modifier is valid.
The final Studio check typed Backspace, minus, and 2 as separate keys: the
minus remained visible while incomplete; completing -2 restored rolling and
displayed 6 - 2 = 4. Restoring the worked example displayed 6 + 1 = 7.

A focused test run was stopped at the user's reminder to avoid unnecessary
tests. No unit-test pass is claimed, and the draft tests were removed.
