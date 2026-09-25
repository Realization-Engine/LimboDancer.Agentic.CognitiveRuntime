# Scenario A1 post-reveal forced back: execution boundary review

**Date:** 2026-09-26

**Status:** Execution review for unit step 8. It admits one governed transition over live games, built under the [ASL Unit Occupied and Concealed Entry Design](<ASL Unit Occupied and Concealed Entry Design.md>). The reviewed PostReveal package, its matrix, and its digests are unchanged.

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 8, part 5, and acceptance scenario U7.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## What the sources establish

The [post-reveal review](<Scenario A1 Concealment Post-Reveal Review.md>) admits a read-only Definitive conclusion, `A1-post-reveal-nondummy-forced-back`. It applies when an ordinary, unconcealed, non-Dummy Infantry mover attempts an obstacle entry, not Bypass, in the MPh into a location where a concealed unit is revealed, and the revealed unit is not a Dummy. It also requires no A4.14 exception, no Infantry OVR election, and no special modifier. Under A12.15 (p. 78) the conclusion says:

- the mover is forced back to its previous location;
- the MF of the attempt count as spent in that location;
- its MPh ends.

It also states that follow-on fire is not resolved.

A12.15 attaches more to the return than the conclusion decides:

- Existing Residual FP in the returned-to location attacks on re-entry. So does an FFE or a minefield there.
- Wire, a Depression, and an entrenchment or shellhole change where the unit is placed, or its TEM against Defensive First Fire.
- The mover may be attacked by Defensive First Fire and Snap Shots on its return. It cannot be attacked during its brief presence in the attempted location.
- The mover loses concealment. The case already requires an unconcealed mover, so this is a no-op.

A12.15 also decides which unit is revealed when there are several: hidden units go beneath a "?", then one unit is chosen by Random Selection. A revealed SMC gives the mover the option of an Infantry OVR (A4.15, p. 49). A4.14 (p. 49) lists the units that may enter an enemy location in the MPh: Berserk, Human Wave, Disrupted, Unarmed, and Infantry OVR.

## The admitted subset

A live game may commit the forced back only when every one of these holds. Each is derived from the committed game and the board in play, never supplied by the caller.

| Condition | Derived from |
|---|---|
| Exactly one enemy unit is in the attempted location, and it is concealed or hidden | The adjudicator's whole-game state |
| That unit is a non-Dummy MMC | Its kind. The model has no Dummy kind yet, so every live unit is non-Dummy. A Dummy stays out of scope. |
| No friendly unit is in the attempted location | The whole-game state |
| The mover is Good Order, unconcealed, not hidden, and not Berserk, Disrupted, or Unarmed | Its conditions and vocabulary states. An unknown condition is a refusal. |
| No Human Wave or Infantry OVR applies | The live source has no event for either, so neither can apply |
| The entry is not Bypass | The action is an entry. Bypass is not an action of the live source. |
| The return location is clear | See the next table |

| Return hazard (A12.15) | Why a live game excludes it |
|---|---|
| Residual FP, FFE | The live source has no fire or artillery event, so none can exist. The planner also refuses if an entity of either kind is at the return location. |
| Minefield, Wire, entrenchment, shellhole | Setup places only catalog definitions, so none can be placed. The planner also refuses if an entity of those kinds is at the return location. |
| Depression | Derived from the map: the planner requires the return location to have no depression terrain, as the first case already does |
| Defensive First Fire and Snap Shots on return | Opportunities, not effects. The forced back does not decide them, and the event records that follow-on fire is unresolved. When Fire becomes an action, it must be able to act on this return. |

Out of scope, and each a refusal:

- several concealed or hidden units (Random Selection);
- a revealed SMC (the OVR option);
- Dummies;
- a concealed, Berserk, or routing mover (the RtPh is not an entry phase);
- any Bypass;
- every hazard above that the planner finds present or cannot rule out.

## Required state and runtime contract

These are the same conditions as the [second-defender review](<Scenario A1 Second Defender Execution Boundary Review 2026-09-24.md>), applied to the live game store:

- **A conclusion is never an authorization token.** The executor plans again and commits only under a gate-authorized, confirmed action at the validated revision.
- **One atomic append.** The attempt, any hidden-to-concealed placement, the reveal, and the forced back are committed together or not at all.
- **Cost and debit stay separate.** The attempt event records the 2 MF cost; the forced-back event debits it in the previous location. A replay never debits twice.
- **Stable attempt key.** The same attempt again is a Replay. A different payload under the same attempt id is refused.
- **Stale state is refused.** A later event, or a changed board version, requires a fresh plan.
- **Disclosure.** The reveal is disclosed to the mover's side only by committing it. A proposal must not show the mover's side what it would reveal (the design's section 7).

## Outcome

The subset above is admitted for execution over live games. Everything the package leaves unresolved stays unexecuted: follow-on fire, and the effects on placement and TEM. Each needs its own reviewed case before any action decides it.
