# Wiring Prompt 001

**Batch gate:** `f525ea78` (2026-10-29). Written by Claude with `code-builders/documents/wiring_prompt.py` from the [rule inventory](<../../ASL Rule Inventory A to E.md>), sections 6 to 8; the first prompt.

## For Codex

You take track 3, the wiring (the user, 2026-10-29): Play, the Units projector, ASL.Turns, and the Studio's services moved onto the calculators the rule claims built. How to claim, build, gate, and review a wiring claim is in [AGENTS.md](<../../../../../AGENTS.md>) ("Wiring claims (track 3)") and the [Rules-first plan](<../../Plans/ASL Rules-First Four-Track Plan.md>), section 9.1; this prompt says only what is open on the board at this batch gate. Each rule claim's design (linked) has its map of the sites and its rulings.

## Open wiring rows (1)

### Fire

| Wiring | Rule claim | Sites to rewire | Old members to remove | Behavior changes |
|---|---|---|---|---|
| W-C9.3 | [C9.3](<../../Designs/ASL C9.3 Spotters Claim Design.md>) | New: the replacement designation at the owner's MPh opening after a loss (ASOP 3.11A), asking `ScenarioA1SpotterCalculator.Timing` and `CandidateRefusal` (`GamePlanner.Spotters.cs`, `GameProjector.SpotterDesignation.cs`); one hex-range read for Play's `GamePlanner.SupportWeaponMapFacts` and the projector's `SpotterHexRange`, handed to `InReach` (backlog section 80) | None: the C9.3 claim moved its callers itself, before the rule that a claim builds Rules only | A broken, eliminated, or captured Spotter can be replaced at its owner's next MPh opening |

## Wiring claims in progress

None.

## Rule claims waiting for review

The agent that did not build a claim reviews it (AGENTS.md, step 4). Built and not yet reviewed:

- proving-slice (Claude)
- [C9.3](<../../Designs/ASL C9.3 Spotters Claim Design.md>) (Claude)
