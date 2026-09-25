# Scenario A1 Random Selection reveal: source and execution review

**Date:** 2026-09-26

**Status:** Source and execution review for unit step 9. It admits a bounded subset of live play, built under the [ASL Unit Random Selection and Declined OVR Design](<ASL Unit Random Selection and Declined OVR Design.md>). No reviewed package, matrix, or digest changes.

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 9, parts 1, 3, and 4, and acceptance scenarios U9 and U10.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## What the sources establish

**A12.15 (p. 78)**, for the case where the attempted location holds several concealed units:

- The DEFENDER must reveal at least one concealed unit when an enemy Infantry unit attempts to move into the location in the MPh.
- Random Selection decides which concealed unit or units lose concealment.
- All hidden units in the location are first placed on board beneath a "?".
- A revealed non-Dummy unit forces the mover back.
- If the only concealed unit revealed is an SMC, the ATTACKER may choose to attempt an Infantry OVR.

**A.9 (p. 43)** defines Random Selection:

- One dr is made for each unit.
- The unit with the highest dr is affected.
- Every unit tied for the highest dr is affected equally.

**A4.15 (p. 49)** adds that more than one SMC must be revealed to deny a location to an MMC capable of OVR. So two revealed SMC do not give the OVR option.

## Reviewed conclusions this relies on

- **PostReveal forced back** (`A1-post-reveal-nondummy-forced-back`). Its facts name the defender's reveal as `nonDummy` and do not count the units in the location. Its exclusions include an *unresolved* defender reveal. A reveal decided by a committed Random Selection is resolved, so the conclusion applies whatever the number of units in the location, provided that:
  - the revealed units include a non-Dummy MMC, or more than one SMC;
  - every other exclusion of the package still holds.
- **Declined OVR** (`A1-concealed-smc-declined`, ConcealedSmcOverrun matrix). A declined election is delegated to the PostReveal forced back, and no new conclusion is admitted.
  - A declined election means no OVR is elected. The PostReveal fact `overrunElection` is therefore `none` once the ConcealedSmcOverrun resolver has returned its delegation for the same attempt.
  - That package requires the SMC to have been **concealed**, not hidden, when the attempt began (`initialDefenderState = concealed`). A hidden SMC is outside it.

## The admitted subset

An entry may be resolved by Random Selection in a live game only when every one of these holds before any die is drawn. The planner derives each from the whole game.

| Condition | Why |
|---|---|
| The target holds two or more enemy units, all concealed or hidden, all non-Dummy MMC or SMC | The case A12.15 resolves by Random Selection. Known units, friendly units, and entities are outside it. |
| No SMC in the target is hidden | An outcome in which a hidden SMC is the only unit revealed could not be declined under the reviewed package, and a roll must never lead to an unreviewed state |
| The mover and the return location meet every condition of the [post-reveal forced-back review](<Scenario A1 Post-Reveal Forced-Back Execution Boundary Review.md>) | The forced back is the outcome of every branch except a lone SMC reveal, and a declined OVR also ends in it |

Each outcome of the roll then has a reviewed resolution:

| Units revealed by the roll | Resolution |
|---|---|
| Any non-Dummy MMC, with or without SMC | PostReveal forced back, committed with the roll |
| Two or more SMC | PostReveal forced back (A4.15: more than one SMC denies the OVR) |
| Exactly one SMC and nothing else | The attempt stays open as a pending OVR declaration. The reveal and the placement beneath a "?" are committed, since A12.15 requires them. A declined OVR later commits the forced back through the delegation above. |

A location holding exactly one concealed SMC needs no roll. It leads to the same pending declaration if that SMC was concealed, and is refused if it was hidden.

## Required state and runtime contract

Everything the forced-back review requires still applies. For dice, the [.NET Dice Roller Requirements](<NET Dice Roller Requirements.md>) (DICE-07 to DICE-12) add:

- the roll is drawn once, inside the game's commit lock, only after confirmation;
- the roll is recorded as an event, and replay never draws;
- a committed attempt returns its recorded roll;
- an unfavorable result is recorded like any other.

Because every branch is decided above before the roll, no roll can be discarded for its outcome.

## Out of scope

- Dummies, and their removal when no non-Dummy unit can be revealed.
- A hidden SMC.
- A concealed or Berserk mover, and Bypass.
- An elected OVR, which step 9 refuses with a generic reason until step 10 reviews the NTC and what follows it.
- Follow-on fire.
