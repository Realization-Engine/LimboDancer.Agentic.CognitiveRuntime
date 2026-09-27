# ASL Unit Deviations, Ordnance, and Vehicles Plan

**Status:** Approved by the user on 2026-09-27 (order, rulings, and autonomous passes). Passes 1 (steps 26 to 28) and 2 (steps 29 and 30) are built; passes 3 and 4 are not. It plans the removal of the three recorded deviations of steps 19 to 23, then unit steps 24 (ordnance) and 25 (vehicles).

**Date:** 2026-09-27

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, steps 24 and 25 (acceptance U28, U29); steps 26 to 30 (acceptance U30 to U34) were added to it when this plan was accepted.

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
| R27.2 | May the owner decline the Leader Creation dr? | Added in pass 1: no; it is always rolled, and declining it is in the backlog. |
| R28.3 | What does a hero do in the built rules? | Added in pass 1: fires 1-4-9 (wounded 1-3-8) with the -1 heroic DRM at Normal Range (A15.24); is not Cowering alone; a failed MC wounds, then eliminates him (A15.2); SW use (A15.23) waits for review. |
| R28.4 | What is a heroic leader? | Added in pass 1: he keeps his counter and leadership, rallies, has a Morale Level of at least 9, and is wounded rather than broken by a failed MC (A15.21). |
| R28.5 | May the owner refuse Battle Hardening? | Added in pass 1: no; it is always taken, and refusing it is in the backlog. |
| R28.6 | Which unit is "the next higher quality" where several qualify? | Added in pass 1: the same size, no printed number lower, and the least gain: the smallest summed increase of the printed factors, then the fewest added capabilities. A Russian Conscript becomes NKVD 2nd Line (A25.25); an elite or NKVD MMC, or a 10-3, becomes Fanatic (A15.3, A10.8). |
| R28.7 | What does a Battle Hardening result do to a broken, pinned, or Disrupted unit, and when is a unit "broken" for the Heat of Battle DRM? | Added after the pass 1 review: the unit is exchanged "(even if broken)" for an unbroken, unpinned unit, so it is also no longer Disrupted, and a unit with no better class is unbroken and unpinned too (A15.3); on a rally, a Battle Hardening or heroic result rallies the unit though the Rally DR failed (A15.21). The +1 for a broken unit applies to a unit broken before the MC or by it. |
| R28.8 | May a unit take two Heat of Battle DRs in one attack (an Original 2 on its MC, then on its LLMC)? | Added after the pass 1 live review: no; the second is recorded as not taken, and taking it is in the backlog. |
| R29.1 | Close Combat scope? | Infantry against Infantry with Ambush and Melee; vehicles and Hand-to-Hand out, in the backlog. |
| R29.2 | Who may Advance, and where? | Added in pass 2: a Good Order Infantry unit of the phasing side, not pinned, berserk, in Melee, captured, or concealed, one ADJACENT Location at the same level into reviewed terrain, even into Known enemy units (A3.7, A4.7); a unit that fired in the PFPh may advance (A4.7 lists no such bar); an advance that would make the unit CX (A4.72) is refused, as are advances into concealed enemy units and overstacked Locations. A Guard's prisoners go with it (A20.53). |
| R29.3 | How is a Location's CC resolved? | Added in pass 2: once per CCPh, each Location in turn; all declared attacks of both sides at once, or with an Ambush the ambusher's round and then the other side's by its survivors (A11.12, A11.3, A11.32). No unit attacks or is attacked twice. |
| R29.4 | How are SMC grouped? | Added in pass 2: before the attacks, each SMC is declared stacked with one MMC of its side or alone; a stacked SMC attacks with its MMC or not at all and is attacked with it; an unstacked SMC combines with no MMC (referee D4); a SMC has one FP (A11.14). |
| R29.5 | Whose DRM is it? | Added in pass 2: the Ambush, leadership, and heroic DRM apply to the attack; the -2 against a broken unit applies to that defender alone, so each defender has its own Final DR (as A4.8 EX reads a status DRM in CC); a Partial Kill falls by Random Selection among the defenders whose Final DR equals the Kill Number. |
| R29.6 | Casualty Reduction in CC? | Added in pass 2: as A7.302: a squad becomes its HS, a HS is eliminated, a SMC is wounded with a Wound Severity dr (A17.11). |
| R29.7 | SW of units eliminated in CC? | Added in pass 2: lost on an Original colored dr of 1 (the first die) and a dr at most the black Kill Number (A11.13); otherwise left unpossessed. |
| R29.8 | Which leader directs CC? | Added in pass 2: the one the declaration names, a leader of the attack who is not pinned (A7.831, p. 58: a pinned leader cannot direct an attack) or berserk, and only when he does not attack alone and no attacker is berserk (A11.141). |
| R29.9 | Ambush drm? | Added in pass 2: each cause once when any unit of the force has it (A11.4: "even if only a portion of a player's CC force is qualified"): +1 broken, +1 pinned, +1 berserk, +1 Lax (Inexperienced or berserk, A19.36, A15.432), -1 Stealthy (a Good Order hero, A11.17), and the best unpinned Good Order leader's modifier unless he is alone or any unit is berserk. |
| R29.10 | Melee? | Added in pass 2: at the end of the CCPh, units of both sides sharing a Location are held in Melee (A11.15); Melee ends when either side is gone. Units in Melee do not move, advance, or fire, and fire into a Melee Location is refused (in the backlog). |
| R29.11 | Broken and Disrupted units in Melee? | Added in pass 2, revised after the referee's review (D3) and the table player's (items 4, 5, 11): Withdrawal from Melee is built (A11.2, A11.21): a unit held in Melee, not pinned, berserk, or Disrupted, may withdraw to a declared ADJACENT ground-level Location an advance could enter, in reviewed terrain, across any hexside but a cliff, holding no enemy unit other than a prisoner; A11.21 allows it even where it makes the unit CX, which is not built, so no CX counter is placed (in the backlog). Attacks on it take -2 and +1 per friendly unit in the Melee not withdrawing; it withdraws unless eliminated or Reduced. A broken unit that can withdraw must attempt it before the CCPh ends (A11.16); one that cannot, and a Disrupted one, is eliminated at the end of the CCPh (A11.16, A19.12), but never a Guard (A11.16: "non-guard"). |
| R29.12 | Field Promotion in CC? | Added in pass 2, revised after the referee's review (D2): an Original 2 by an attacking MMC calls for a Leader Creation dr with -1 per odds column below 1-1 (A18.2); the leader joins that attack with one FP and defends with its MMC (A18.12); his mandatory leadership takes the director's place, since leadership modifiers are not cumulative (A10.7), and applies to no attack with a berserk unit (A11.141, A15.42); when the base MMC or the MMC he defends with is ambiguous, the round is refused before any roll. |
| R29.13 | Infiltration? | Added in pass 2: the option to withdraw on an Original 2 or 12 (A11.22) is not offered (in the backlog); declining it is always legal. |
| R29.14 | What else refuses CC? | Added in pass 2: concealed or hidden units, Dummies, prisoners, TI units (referee D6), or an overstacked Location in the CC Location (in the backlog). |
| R29.15 | Berserk units in CC? | Added in pass 2: a berserk unit attacks in its side's round (A15.43), with no leadership DRM (A11.141), and returns to normal when its own group's attack made every elimination in its Location, at least one, and no enemy unit is left (A15.46; referee dispute 8). |
| R29.16 | Odds just above 10 to 1? | Added after the referee's review (dispute 1): odds above 10 to 1 but below 11 to 1, with or without a created leader's FP, may round down to 10-1 or fall in "> 10-1" (A11.11); the round is refused before any roll. |
| R29.17 | Reinforcing a Melee, and mandatory CC? | Added after the referee's review (D5), extended after the table player's (item 7): a unit that advanced this APh into a Location already in Melee attacks in its side's round (A11.15: "must engage in CC"), and so does a berserk unit (A15.43); the CCPh does not end while such a Location whose units the package reviews has had no CC. |
| R29.18 | Sequential attacks after an Ambush? | Added after the table player's review (item 6): the ambusher's attacks need not be predesignated (A11.3): each call resolves one or more of its attacks, and it may call again until the ambushed side's round, which closes the Location. The units the ambusher must attack with (R29.17) are checked when the ambushed side's round begins. |
| R29.19 | Does a Guard advance into CC? | Added after the table player's review (item 3): no; its prisoners would enter CC with it (A20.53, A20.55), which is not built (in the backlog). |
| R30.1 | Capture scope? | Surrender, prisoners with a guard, and No Quarter as Scenario A1's sides need; prisoner movement, escape, and interrogation out, in the backlog. |
| R30.2 | What does a Berserk result do? | Added in pass 2: a Final Heat of Battle DR of 9 to 11, or 12 or more for a Fanatic unit (the table's note), makes the unit berserk: rallied if broken, unpinned, not Disrupted, without DM or "?" (A15.4, A15.42); with no Known enemy unit in its LOS, it is Battle Hardening instead (A15.44). The planner reads the LOS before the attack; a unit whose read is missing is refused before any roll. |
| R30.3 | A berserk unit under fire? | Added in pass 2, revised after the referee's review (D1): Morale Level 10, one higher when Fanatic, never lowered (A15.42); a failed MC Casualty Reduces it, and an Original 12 eliminates a berserk MMC and wounds a berserk leader as if already wounded (A10.31); it is never broken or pinned, takes no PTC, LLMC, or LLTC, suffers no ELR Replacement, and gets no leadership from a friendly leader; a leader berserk before the attack gives none (A15.41). |
| R30.4 | A berserk leader's companions? | Added in pass 2: after the attack, each other friendly unit in his Location subject to Heat of Battle takes a NTC with his leadership DRM; a pass makes it berserk, rallying it if broken (A15.41). |
| R30.5 | Does a berserk unit fire, or charge? | Added in pass 2, revised after the table player's review (item 2): it does not fire (in the backlog). At the start of its MPh it charges before any other unit moves, abandoning each SW of more than one PP first (A15.431; 1PP SW beyond its IPC refuse the charge): each step on a shortest route in MF to the nearest Known enemy unit in its LOS (the ATTACKER chooses among the equidistant), keeping its target until a closer Known enemy unit comes into LOS, with eight MF (three if wounded), never by Assault Movement, into the enemy's Location (A15.43, A15.431). A charge the model cannot decide ends in place, a recorded deviation (in the backlog): a route over terrain the review does not admit, or a step into a Location holding prisoners (Massacre), concealed enemy units, or only a lone enemy SMC (an Infantry OVR, A15.432). It may not advance. |
| R30.6 | When does berserk end? | Added in pass 2: in CC as R29.15, and at the end of the MPh for a berserk unit with no Known enemy unit in its LOS (A15.431, A15.46). |
| R30.7 | What does a Surrender result do? | Added in pass 2: 12 or more (not Fanatic) breaks and Disrupts the unit (A15.5); if an ADJACENT Known Good Order armed enemy Infantry unit with Guard capacity (A20.51, US# of A1.6) exists, the unit surrenders: the captor's side chooses the Guard, the unit abandons its SW in its Location (A20.24), and it is placed with the Guard as its prisoner. A concealed firer that firing reveals counts as Known, and a captor the attack breaks is none (referee D7); when every captor lacks capacity the attack is refused before any roll (D8; Unarmed units are not built). The surrender is always accepted: No Quarter and Massacre are not offered (in the backlog). Until the Guard is chosen nothing else happens in the game. |
| R30.8 | What do prisoners do? | Added in pass 2: a prisoner moves and advances with its Guard (A20.53); a Guard does not fire, and fire at, or CC in, a Location holding prisoners is refused (A20.52, A20.54, A20.55; in the backlog). A Heat of Battle subject sharing a Location with prisoners is refused before any roll, since a berserk unit would massacre them (A20.4; referee D10). Prisoners are not Known enemy units for a charge (A20.4). |
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
