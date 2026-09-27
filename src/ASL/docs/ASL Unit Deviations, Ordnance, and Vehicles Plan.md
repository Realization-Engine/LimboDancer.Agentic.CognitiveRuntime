# ASL Unit Deviations, Ordnance, and Vehicles Plan

**Status:** Draft of 2026-09-27, for the user's approval. Nothing here is built. It plans the removal of the three recorded deviations of steps 19 to 23, then unit steps 24 (ordnance) and 25 (vehicles).

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 24 and 25 (acceptance U28, U29); new steps 26 to 30 are proposed here and enter the requirements when this plan is accepted.

**Related documents:** the [ASL Unit Rally and Fire Extensions Plan](<ASL Unit Rally and Fire Extensions Plan.md>) (sections 9 and 10 outline steps 24 and 25), the [ASL Unit Backlog](<ASL Unit Backlog.md>), and the [ASL Unit Rally and Fire Extensions in Live Play Design](<ASL Unit Rally and Fire Extensions in Live Play Design.md>).

Rule citations give physical pages of `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`). Since 2026-09-27 the whole rulebook is in scope: Chapter H's listings and the back-matter charts are used like any other page.

## 1. Outcome

- **The deviations are gone.** A moving stack may split (A4.2). The first MMC Self-Rally's Original 2 creates a leader (A18.11). Every Original MC or Rally DR of 2 is followed by its Heat of Battle DR and all four results: Hero Creation, Battle Hardening, Berserk, and Surrender (A15.1 to A15.5).
- **Ordnance (step 24).** A Gun fires HE at Infantry through the To Hit process and the IFT, with ROF, acquisition, Critical Hits, and breakdown.
- **Vehicles (step 25).** Vehicles are placed, faced, and moved; Infantry fire at an unarmored vehicle on the IFT Vehicle line; a vehicle's MGs fire on the IFT, with collateral attacks on an exposed crew.

## 2. How the work is done

- **The same pipeline as steps 17 to 23.** Each step has a review stage (rule fragments compared with their pages, chart transcriptions checked against the page, a digest-pinned package with no execution authority, reachability walks) and a live stage (records, replay verifiers, governed actions, the Play page, acceptance tests).
- **Every outcome decided before any roll.** A result a package does not decide either refuses the action before the dice or, where this plan says so, is recorded as a named deviation until a later step decides it.
- **Values.** Printed values come from the counter pages, Chapter H, and the back-matter charts. A value no page gives is manufactured under ruling R0.3 and marked `manufactured`.
- **A second-pass reviewer in place of the user's review.** After the first pass of each review stage, an independent reviewer (a separate agent with a fresh context, briefed as a skeptical ASL rules referee) re-extracts every cited page, re-checks each transcription and fragment, and challenges each proposed ruling against the rule text. Disagreements are resolved by the rule text; any that the text cannot settle are recorded in the review document as open and resolved conservatively (refuse rather than guess). For the live stage, a second reviewer briefed as a table player plays the acceptance scenarios on the Play page and reports anything that differs from play at the table. Both reports are recorded in the step's review document.
- **The merge gate.** The full local ASL test run, then the Docker check on the committed branch; merge when both pass.

## 3. Order of work

The deviations change behavior that is already live, and each is small next to ordnance and vehicles, so they come first. Heat of Battle cannot be finished without Close Combat (a berserk unit charges into it, A15.43) and capture (Surrender, A15.5; A20.2), so those come before it.

| Pass | Steps | Content | Size |
|---|---|---|---|
| 1 | 26, 27, 28 | Stack splitting; Leader Creation; Heat of Battle with Hero Creation and Battle Hardening (Berserk and Surrender still recorded as not taken) | Medium |
| 2 | 29, 30 | Close Combat and Melee; Berserk and Surrender with capture; the Heat of Battle deviation removed | Large |
| 3 | 24 | Ordnance: a Gun firing HE at Infantry | Large |
| 4 | 25 | Vehicles: placement, movement, unarmored targets, vehicular MGs | Large |

Each pass is one branch with one merge, as steps 19 to 23 were. Passes 3 and 4 do not depend on passes 1 and 2 and could run first if the user prefers.

## 4. Step 26: stack splitting

A4.2 (p. 49): units may move as a stack "and may break up the stack during the MPh to continue to move separately but all members of that moving stack must end their MPh before another unit not in that stack may move."

- **Model.** `MovementState` keeps the stack's members (those that began the move together) and, separately, the units moving in the current step. A move names any subset of the members that have not ended; the subset must share a Location.
- **Broken and pinned members.** A member broken or pinned by fire ends its MPh (A7.8, A10.5); the rest may continue. The effect event already records the break or pin; the projector ends that member's move.
- **Ending.** `asl.game.end-move` names the members it ends (default: all); the stack's move is over, and another stack may move, when every member has ended.
- **Readings to confirm in review:** the DEFENDER's window applies to each step of any subset; Residual FP attacks each entering subset; FFNAM applies per A8.14 to a unit that breaks and stays in the Location.
- **Acceptance (U30):** a two-squad stack loses one squad to Defensive First Fire and the other moves on; the stack's move ends only when both have ended; another stack is refused until then.

## 5. Step 27: Leader Creation

A18.11 (p. 85): the first MMC Rally attempt of a player's own RPh that rolls an Original 2 creates a leader; A18.2 (p. 85, and the chart on p. 108 and p. 694) gives its quality by a dr with DRM.

- **Definitions.** German and Russian leaders of every quality the table can produce (A18.2 gives the range, for example 6+1 to 9-1), with morale, broken morale, and leadership from the counter pages and Chapter H; manufactured under R0.3 where no page prints them.
- **The package.** `scenario-a1-rally` is revised: an Original 2 on the first MMC Self-Rally asks for the Leader Creation dr and resolves the created leader's definition; the `leaderCreationNotTaken` note is retired. A18.12 (an Original 2 in CC) waits for step 29.
- **The live record.** The created leader is a new unit through `instance-created`, caused by the rally record, in the rallying unit's Location, Good Order.
- **Acceptance (U31):** a first MMC Self-Rally on 1, 1 rallies the squad and creates the leader the dr gives; replay recreates it without dice.

## 6. Step 28: Heat of Battle, with Hero Creation and Battle Hardening

A15.1 (p. 83): an Original MC or Rally (not Self-Rally) DR of 2 is followed by a Heat of Battle DR with cumulative DRM (-1 Elite, British, Finnish; +1 broken, Inexperienced; +2 Russian; and so on); the Final DR gives Hero Creation (6 or less), Battle Hardening (5 to 8; both on 5 or 6), Berserk (9 to 11), or Surrender (12). Some units are exempt (unarmed, crews, heroes, already berserk, and others).

- **Heroes (A15.2 to A15.24, p. 83).** Hero definitions for both nationalities (the hero counter's values, from the counter pages or manufactured), creation in the unit's Location (A15.21), and what a hero does in the rules already built: its FP in a fire group, the heroic DRM to a fire group it is part of (A15.24), and its SW use (A15.23). Hero movement and wounds follow SMC rules already built.
- **Battle Hardening (A15.3, p. 83).** The unit is Replaced by its next better class, or gains what A15.3 gives a unit that has no better class; lineage records the Replacement, as ELR Replacement does.
- **Berserk and Surrender are not taken yet.** A Final DR of 9 or more is recorded as `berserk-not-taken` or `surrender-not-taken`, the narrowed successor of `heat-of-battle-not-taken`. The review proves every other branch decided.
- **The packages.** `scenario-a1-fire` and `scenario-a1-rally` are revised and republished with their earlier digests kept.
- **Acceptance (U32):** an Original 2 on a MC is followed by its Heat of Battle DR; a Final DR of 4 creates a hero, and a Final DR of 7 Battle Hardens the unit; each record shows the DR and its DRM.

## 7. Step 29: Close Combat

A11 (pp. 72 to 76): in the CCPh, units in the same Location attack each other on the Close Combat Table (A11.11), with CC DRM (A11.51), Ambush (A11.4), Melee (A11.7), and leader effects (A11.14). A18.12 (Leader Creation on an Original 2 in CC) comes with it.

- **Scope proposed for review.** Infantry against Infantry in one Location; attack declaration and sequential resolution (A11.12); Ambush; Melee and its markers; withdrawal from Melee (A11.2); leaders and heroes; SW in CC as A11 allows. Vehicles in CC wait for step 25 and later; Hand-to-Hand CC is scenario-specific and stays out.
- **Entry into an enemy Location.** The move of step 22 gains Advance in the APh (A4.7) into an enemy-occupied Location, which is how Infantry reach CC; the building entry of steps 7 to 11 is folded into it where the reviewed cases allow.
- **Acceptance (U33):** a squad advances into a Location holding a Known enemy squad, CC is resolved with its DR and DRM, and a surviving pair is in Melee.

## 8. Step 30: Berserk, Surrender, and capture

- **Berserk (A15.4 to A15.46, pp. 83 to 84).** Morale 10, eight MF, the forced charge at the start of the MPh toward the nearest Known enemy (A15.43, A15.432), terrain restrictions (A15.45), CC (step 29), and return to normal (A15.46); a leader's consequences (A15.41).
- **Surrender and capture (A15.5, p. 84; A20.1 to A20.5, pp. 86 to 87).** A Final DR of 12 breaks and Disrupts the unit, which surrenders to any ADJACENT Known Good Order armed enemy Infantry as if they shared a Location; otherwise it is only Disrupted. Capture creates prisoners with a guard (A20.2, A20.5); No Quarter, Massacre, and the nationality exceptions follow A20.3 and A20.4 as far as Scenario A1's sides need them.
- **The Heat of Battle deviation is removed.** `berserk-not-taken` and `surrender-not-taken` are retired; every Heat of Battle result is taken.
- **Acceptance (U34):** a Final Heat of Battle DR of 10 makes a squad berserk, and it charges the nearest Known enemy in the next MPh; a Final DR of 12 next to a Good Order enemy squad makes it that squad's prisoner.

## 9. Step 24: ordnance

Chapter C (pp. 162 to 191); the To Hit and To Kill charts on pp. 189 to 191 and pp. 700 to 702; Gun values from Chapter H (German ordnance from p. 351, Russian from p. 363).

- **The slice.** A Gun firing HE at Infantry: the Gun's Covered Arc and facing (C3.2, C5.1), the To Hit number by target type and range (C3.3, the To Hit Table), Gun and ammunition modifications (C4), the firer-based and target-based DRM (C5, C6) that a Gun firing at Infantry in the built terrain can meet, acquisition (C6.5) as state across attacks, ROF on the colored die (C2.24), Critical Hits (C3.7), and breakdown on the To Hit DR (C2.3); a hit resolves HE on the IFT with the Gun's HE equivalent (C8.31).
- **The Guns.** One German and one Russian Gun that fire HE at Infantry, chosen in review from Chapter H (for example the German 7.5cm leIG 18 and the Russian 45mm M1937), with every printed value transcribed from their listings and counter depictions.
- **Crews.** A Gun is manned by its crew (C10, A21); crews exist as SMC-like units with their own definitions.
- **Left for later passes, recorded in the backlog:** AP and To Kill (they wait for step 25's armored targets), mortars (C9), OBA (C1), LATW (C13), RCL (C12), Gun movement (C10.1), Guns as targets (C11), and Special Ammunition (C8).
- **Acceptance (U28):** a Gun fires HE at an Infantry target in its Covered Arc: the record shows the To Hit arithmetic and the IFT effect; ROF allows a second shot on a colored die at most the Gun's ROF; acquisition lowers the next To Hit DR; a Critical Hit resolves as C3.7 says.

## 10. Step 25: vehicles

Chapter D (pp. 192 to 221); vehicle listings in Chapter H (German vehicles from p. 337, Russian from p. 355).

- **Step 25a: the vehicle model and movement.** Vehicle definitions (a German and a Russian unarmored truck, and a German armored halftrack with MGs, chosen in review), placement with a vehicle's facing (D1.3), Motion and stopping (D2.4), MP expenditure by terrain (D2.1, the B Terrain Chart's vehicle column), and the DEFENDER's fire during vehicular movement.
- **Step 25b: fire at and by vehicles.** Infantry fire at an unarmored vehicle on the IFT Vehicle line (A7.308, p. 55); the halftrack's MGs on the IFT (D1.8, D3.5) with Target Facing on the colored die (D3.2); crew exposure (CE and BU, D5.2 to D5.32) and collateral attacks on a CE crew (D.8).
- **Left for later passes:** attacks by and at an AFV's main armament (they need AP and To Kill from step 24's successors), Overruns (D7), transport of Personnel (D6), bog and immobilization (D8), wrecks (D10).
- **Acceptance (U29):** Infantry fire at a moving truck resolves on the IFT Vehicle line; the halftrack's MGs fire on the IFT, and fire at it with its crew exposed takes a collateral attack on the crew.

## 11. Proposed rulings

The second-pass reviewer checks each against the rule text before any code; the table records the proposal.

| Ruling | Question | Proposal |
|---|---|---|
| R26.1 | May any subset of a moving stack continue after the stack splits? | Yes, per A4.2; members that broke or pinned end their move. |
| R27.1 | Where do created leaders' values come from? | The counter pages and Chapter H; manufactured under R0.3 where no page prints them. |
| R28.1 | May Heat of Battle be taken in part before Close Combat exists? | Yes: Hero Creation and Battle Hardening are taken; Berserk and Surrender are recorded as not taken until step 30. |
| R28.2 | Which exemptions of A15.1 apply to the built units? | All that name built units (crews, heroes, already berserk); the rest (Cavalry, boats, Human Wave) name nothing that exists and are recorded as such. |
| R29.1 | Close Combat scope? | Infantry against Infantry with Ambush and Melee; vehicles and Hand-to-Hand out, in the backlog. |
| R30.1 | Capture scope? | Surrender, prisoners with a guard, and No Quarter as Scenario A1's sides need; prisoner movement, escape, and interrogation out, in the backlog. |
| R24.1 | Which Guns? | One German and one Russian Gun that fire HE at Infantry, chosen from Chapter H in review. |
| R24.2 | Which To Hit Cases? | Those a Gun firing at Infantry in the built terrain can meet; each other Case refuses the attack until built. |
| R25.1 | Which vehicles? | A German and a Russian truck and a German armored halftrack with MGs, from Chapter H. |
| R25.2 | Fire at AFVs? | Infantry small arms at an AFV only through collateral attacks on a CE crew; everything else waits for To Kill. |

## 12. Execution

- **Pass by pass.** Each pass is a branch: the review stage (with the second-pass reviewer), then the live stage (with the table-player reviewer), then documents (a review document per package, a design per pass, the requirements' done notes, the backlog), then the local run and the Docker check, then the merge.
- **The docs branch.** `docs/asl-whole-rulebook` (the whole-rulebook ruling and this plan) merges with pass 1.
- **When to stop and ask.** Only for something this plan does not cover: a rule question the text cannot settle that would change the plan's scope, or a merge failure that needs a design change.

## 13. Decisions for the user

1. Approve this plan, or change its order (for example, ordnance and vehicles before the deviations).
2. Approve the proposed rulings, subject to the second-pass reviewer's findings.
3. Authorize working through the passes without stopping, merging each pass when its local run and Docker check pass.
