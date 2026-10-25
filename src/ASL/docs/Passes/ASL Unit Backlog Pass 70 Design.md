# ASL Unit Backlog Pass 70 Design

**Status:** The frame, written on 2026-10-25 under the user's word of that day ("Start Pass 70 (Infantry III: morale, Close Combat, and concealment). Run it without my intervention, up to the merge gate. Questions go to read-only sub-agents; each decision is a numbered ruling R70.n, open to my veto."). The gate, the push of the pass's branch, its pull request, and the merge are each the user's to start. The frame's questions were put to four read-only agents, as the standing rules ask in this mode. Their answers were judged against the pages and the code. Each decision below is a ruling decided under that word, and each is open to the user's veto.

**Date:** 2026-10-25

**Related documents:**
- the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the entry "Pass 70: Infantry III: morale, Close Combat, and concealment";
- the [rule inventory](<../ASL Rule Inventory A to E.md>) (48 rows give pass 70);
- the [backlog](<../ASL Unit Backlog.md>), the rows that name pass 70;
- the [pass 65 design](<ASL Unit Backlog Pass 65 Design.md>), sections 2 to 6;
- the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>);
- the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>);
- the session's prompt, `Prompts/Pass 70 Prompt I.md`.

**The rulebook.** Four read-only agents read it for this frame, from the page texts of the working folder: pages 55, 65 to 80, 83, 85 to 87, 200, and 203 to 206. I read on their pages the sentences the rulings lean on: A10.2 (p. 65), A10.41 (p. 66), A10.5 (p. 66), A10.711 (p. 69), A11.13 (p. 72), A12.152 (p. 79), A12.16 and A12.2 (p. 79), and A15.1 (p. 83). Each task reads its own pages again before it builds.

**What the frame rests on.** The four agents' reports are kept in `code-builders/pass-history/pass-70-infantry-iii/`: `frame_morale.md` (A: morale outside fire and rout), `frame_sight.md` (B: who sees, concealment, HIP, the Right of Inspection), `frame_cc.md` (C: Close Combat), and `frame_search_codemap.md` (D: Bypass and detection, Searching and Mopping Up, `CheckInvariants`, and the code map). The session's running record is `notes.md` in the same folder.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The rows, by task and increment.
4. What is certain, and what needs a read.
5. What the frame found outside the rows, and corrections to the prompt.
6. The frame's questions, each with its ruling.
7. Memory and performance: the checklist and the measurement.

Then, as each is built: one section per increment, then the reviews, the Studio check, as built, and the gate.

## 2. What the pass is

Pass 70 is three things Infantry do that the engine has only in part:
- **morale outside fire:** one Morale Check member for every MC made outside the Fire and Rally packages, the Voluntary Break and Voluntary Rout, and rout by level, by vehicles and Guns, and by SSR Fanaticism;
- **concealment:** one read of who sees, the owner's choices on a "?", concealment for vehicles and an Emplaced Gun's crew, Bypass and detection, Searching and Mopping Up anywhere, and the Right of Inspection by LOS;
- **Close Combat:** Withdrawal completed, CC with a Gun's crew, Infantry against Infantry in a Location with a vehicle, its Ambush, Street Fighting, and the CC rows pass 65 and the 2026-10-25 move put here.

**What departs from the plan's entry, first:**

- **The pass is four times the plan's size by the agents' estimates.** The plan gives 7:20 of building. The agents' estimates sum to about 33:00 (section 6, F1). Pass 65 built in 7:13 against its frame's 15:35, so the build may run about 16 hours. The pass stays one pass (ruling R70.1), built in the order of the table below, with the last increment (Street Fighting, A11.52's Passengers, A18.12 against a vehicle, and D5.42) the one a session's end would leave out.
- **Moved out of the pass** (ruling R70.2):
  - to pass 110: CC against a wreck (D10.1), with the rest of D10.1, which the inventory already gives pass 110;
  - to pass 135: Withdrawal to another level of a building;
  - to pass 75 (task 75.6): naming an AFV in a Location that also holds concealed or hidden units or a Dummy (pass 60's row), since it is a fault of the FT, MOL, and DC records 75.6 rebuilds;
  - to pass 75 (task 75.3): the SMC crew's Stun wound in the projector (D5.34 with A17.11);
  - to the backlog: an Infantry OVR offered on a lone SMC revealed outside the reviewed building case (A12.15); 5/8" Dummies, Cavalry, horses, and bicycles under concealment, a vehicle's secret Target Size, and the Smoke Dispenser (A12.2); entrenching, setting a Fire, and Clearance as actions that cost a "?" (A12.141), with the passes that build those actions; Searching for minefields and Booby Traps and the Fortified Building's reveal; a rout's destination refigured when an enemy becomes newly Known; the minefield, FFE, shellhole, entrenchment, and Blaze rout options and Spotted Fire Interdiction.
- **The documents correct five inventory rows** the frame found stale (section 5).

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 3a | 70.3, 70.10 | One read of who sees (`ScenarioA1Sight`, memoized per state); its callers; the actions that cost a "?"; the Right of Inspection by LOS in every side's view. | 4:30 |
| 3b | 70.1 (I) | One Morale Check member for every MC outside Fire and Rally; Unarmed in Fire (A15.1, A19.11); the Interdiction NMC's Heat of Battle and LLMC; LLMC outside fire; the Bail Out's leadership, wound, and ELR; PRC leadership and LLMC; D5.311's broken crew. | 6:05 |
| 3c | 70.1 (II) | Voluntary Break (A10.41) and Voluntary Rout (A10.711). | 2:00 |
| 3d | 70.2 | Rout from the building it starts in; Interdiction by vehicles and Guns; Fanaticism by SSR. | 2:25 |
| 3e | 70.4, 70.5 | Voluntary removal of a "?" and the viewer's option; "?" for an Emplaced Gun's crew and for vehicles; entering concealed; HIP for vehicles and an Emplaced Gun out of Concealment Terrain. | 5:30 |
| 3f | 70.8 | Bypass endings and detection: Minimum Move entry, the forced-back lane, broken or pinned in Bypass, occupying the obstacle, TPBF inside the hex, a vehicle's Bypass ending. | 1:45 |
| 3g | 70.9 | Searching; Mopping Up in any building of the map; one pass over the occupied Locations. | 4:15 |
| 3h | 70.6 | Withdrawal completed (dropping SW, a concealed destination, the view's bar); CC with a Gun's crew and A11.13's Gun loss; a Carrier HS recombining (D6.82). | 1:35 |
| 3i | 70.7 (I) | Infantry against Infantry in a Location with a vehicle, in sequence; its Ambush both ways; PRC in CC and the Melee of PRC; the CC counter's wording. | 3:00 |
| 3j | 70.7 (II) | Street Fighting; A11.52's capture of Passengers; A18.12 against a vehicle; D5.42's entry at a capture. | 2:00 |
| | Overhead | The reviews, the Studio check, the documents (the plan's 1:15). | 1:15 |
| | **Total** | build 33:05 | **34:20** |

## 3. The rows, by task and increment

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 70.1 | A10.2 (65) | LLMC and LLTC outside fire | 3b |
| 70.1 | A10.41 (66) | Voluntary Break | 3c |
| 70.1 | A10.711 (69) | Voluntary Rout | 3c |
| 70.1 | A15.1 (83) | Heat of Battle's Unarmed and PRC exemptions; its DR in MCs outside Fire and Rally | 3b |
| 70.1 | A17.1 (85) | The wound procedure in every Casualty Reduction | 3b |
| 70.1 | A19.11 (86) | An Unarmed unit's ELR immunity | 3b |
| 70.2 | A10.5, A10.51 (66) | Rout by level and inside a building | 3d (largely built; the starting-building exception) |
| 70.2 | A10.53, A10.532 (67) | Interdiction by vehicles and Guns | 3d |
| 70.2 | A10.8 (69) | Fanaticism by SSR | 3d |
| 70.3 | A12.1, A12.14, A12.141 (76 to 78) | One Good Order read; the actions that cost a "?" | 3a |
| 70.4 | A12.14 (77) | Voluntary removal; the viewer's option | 3e |
| 70.5 | A12.11, A12.12, A12.121 (76, 77) | "?" for an Emplaced Gun's crew and vehicles | 3e |
| 70.5 | A12.2, A12.3, A12.34 (79, 80) | Entering concealed; HIP for vehicles and out of Concealment Terrain | 3e |
| 70.6 | A11.1, A11.21 (72, 73) | Withdrawal completed; CC with a Gun's crew | 3h |
| 70.7 | A11.31, A11.34, A11.4 (73) | Infantry against Infantry with a vehicle; Ambush | 3i |
| 70.7 | A11.5, A11.51 (74) | CC DRM with a vehicle; the Ambush -1 and +1 | 3i |
| 70.7 | A11.52 (74) | Capture of a vehicle, its Passengers included | 3j |
| 70.7 | A11.8 (76) | Street Fighting | 3j |
| 70.7 | A18.12 (85) | Leader Creation in CC against a vehicle | 3j |
| 70.8 | A12.13 (77) | Ordnance at a mixed Location | built in pass 60 (R60.9); documents only |
| 70.8 | A12.15, A12.151 (78) | Detection; Bypass | 3f |
| 70.8 | A12.42 (80) | A vehicle's Bypass ending | 3f |
| 70.9 | A12.152, A12.153, A12.154 (79) | Searching; Mopping Up; Search Casualties | 3g |
| 70.10 | A12.16 (79) | Right of Inspection by LOS | 3a |

The items pass 65 moved under the entry are placed with their task:
- with 3b: Heat of Battle, leadership, and LLMC for PRC, and D6.651's reach of a Passenger leader; the Bail Out NMC's leadership, Wound Severity, and ELR; D5.311's crew;
- with 3h: a Carrier HS recombining without a TC or a leader (D6.82);
- with 3i: PRC in CC (A11.611, A11.71); the CC counter on an unload or a Bail Out into an enemy Location (D6.5, D6.24), and TPBF's at PRC (A7.211);
- with 3j: entering a vehicle at its capture in the CCPh (D5.42);
- to pass 110: CC against a wreck (D10.1).

The older rows: the LLMC after a leader's loss outside fire (pass 35's) with 3b; naming an AFV in a mixed Location (pass 60's) to pass 75; `GameProjector.CheckInvariants` (pass 32.i's) closed by ruling R70.29.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **The morale checks outside fire are ten paths, each with its own gaps** (frame A, section 1): the rout Interdiction NMC has no Heat of Battle, no leadership, and no LLMC; the Bail Out NMC has no leadership, no wound, and no ELR, and a SMC Rider's Casualty Reduction throws (GamePlanner.Riders.cs:164 passes a null severity to RoutCalculator.cs:312); D5.311's crew is checked as unbroken when already broken; Mopping Up, a Sniper, Rally Fate, a PF firer's Casualty Reduction, and Failure to Rout lose a leader with no LLMC. The Fire calculator already resolves Heat of Battle, ELR, wounds, the LLMC cascade, prisoners, berserk units, heroes, and PRC.
- **A15.1 (p. 83) exempts** Unarmed units, Cavalry, PRC, Heroes, crews, and already berserk units from Heat of Battle. So the Bail Out NMC and D5.311's crew owe none, and pass 65's backlog rows that ask for it are closed by that reading.
- **Unarmed exists** as the condition `asl:unarmed` (GameTypes.cs:308); the Fire facts carry no Unarmed fact, so a freed Unarmed unit takes Heat of Battle and Replacement today.
- **Who sees is read in six ways** (frame B, section 1). `EnemyGoodOrderInLosWithin16` and `WithSeen` agree with A12.14 since pass 35's R35.5; `NearestGoodOrderEnemyInLos` counts hidden viewers; `Watching` counts hidden units and Passengers; the gain watchers count hidden units; each scans every unit with a `state.Location` read per call.
- **The view reads no LOS.** `GameView.Of` (Units) shows an unconcealed enemy stack in full whatever the LOS. Before play it already shows a stack's top counter and counts the rest as `SealedUninspected` (A2.9, R23.3). Units references Maps and Rules; the LOS map lives in `GamePlanner`. No planner proposal builds a view; the page, the library, Replay, `UnitNames`, and the sweep do.
- **The replay digest hashes states only;** the text list is a scan of source literals. So 70.10 changes only the sweep's view lines, and only if the sweep builds the view through the new read.
- **CC with a Gun's crew is refused in five places** (frame C, Q1), though no rule of A11 or C bars it; A11.13 (p. 72) eliminates a Gun of a unit eliminated in CC on a colored dr of 1 and a dr at or below the black Kill Number.
- **Infantry CC in a vehicle's Location is refused** (`VehiclePresentBar`, GamePlanner.CloseCombat.cs:425); only the vehicle sequence runs, and no Ambush is given where a vehicle stands.
- **A vehicle's PRC are probably marked in Melee** at the CCPh's end: `GameProjector.InMelee` groups by `state.Location`, which follows containment. A11.71 holds only the PRC of an Immobile vehicle. To be confirmed by a test at 3i.
- **Mopping Up is limited to a card's building** because Play reads no building from the map; only the Studio's `CardMaps.Building` flood fill finds one.
- **Withdrawal's bar counts enemy units the side cannot see,** in the refusal and on the page (Play.razor:4371, :4548, :4603).

**Needs a read at its task.**

- 3b: Fire's `Replace` and A19.12: whether a PRC with no lesser unit is Disrupted (A19.12 says PRC are never Disrupted); the Sniper's and Rally Fate's leader loss paths.
- 3d: a vehicle's weapon ranges from the armor reference, and how a positive DRM bars Interdiction (D3.44, A10.532's Gun CA change).
- 3e: the Concealment Loss/Gain Table's Cases J and K for a crew and a vehicle, rendered from the PDF (the text layer scrambles it).
- 3g: the Index's "Accessible" (R70.22 takes ADJACENT hexes the stack could enter until it is read).
- 3i: how the Infantry CC calculator's simultaneous rounds and the vehicle sequence share a Location.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and the absent-safety of each):
- on a fire attack: `unarmed` and the PRC's leadership facts for each target (3b);
- on a Bail Out: its `drm` and `elr` (3b);
- on a vehicle target: a crew already broken (3b);
- on a CC resolution: the Guns each unit possesses (3h); on a vehicle CC: `leaderCreation` (3j); on an Ambush: the vehicle drm per side (3i);
- on the CCPh-ending `phase-changed`: the PRC held in Melee (3i);
- on a forced-back move step: the Bypass lane kept (3f);
- on a leaving Carrier crew's `instance-created`: the vehicle it left (3h).
New events: `morale-check-resolved`, `voluntary-break`, `units-searched`, and the owner's removal of a "?" (which may reuse `concealment-lost`; see R70.17).

## 5. What the frame found outside the rows, and corrections

1. **A crash:** a SMC Rider's Casualty Reduction in a Bail Out throws (GamePlanner.Riders.cs:164, a null severity). Repaired in 3b.
2. **Stale inventory notes,** corrected in the documents step: A12.1 and A12.14 (movement, advance, and Rally read a Good Order enemy since R35.5); A10.5 (rout by level is built since 2026-10-08); A12.13 (built in pass 60, R60.9); A12.15 (a routing unit's entry is built in pass 35, task 35.4).
3. **`NearestGoodOrderEnemyInLos` counts hidden viewers** (A12.34's reveal of a Hidden Gun), against R35.5. Repaired in 3a.
4. **Corrections to the prompt.** Main is at 25aa97fd (pull request 39), not 7a734fc3; the user said PR 39 merged after the prompt was written. The user's Studio was not running on 5178 at the start. A third worktree, `.claude/worktrees/replay-reenact-scope-9c9f9a`, exists beside the two Codex worktrees; it touches nothing of this pass.

## 6. The frame's questions, each with its ruling

**F1. Is the pass kept whole?** The agents' estimates sum to 33:05 of building against the plan's 7:20. Pass 65 built at about half its frame's estimate.
- **R70.1:** the pass stays one pass, as R65.1 kept pass 65. The increments run in the table's order; whatever a session's end leaves out is the last increment's (3j), and it goes to the backlog with its rows' pass.

**F2. What leaves the pass?**
- **R70.2:** the moves of section 2, each with a backlog row in the documents step. CC against a wreck goes with the rest of D10.1 to pass 110: it needs a wreck's own CC sequence (no PAATC, no Ambush, only a burning result), which is D10's, not A11's.

**F3. Morale outside fire: one member or several?**
- **R70.3:** one Rules member, `ScenarioA1MoraleCheck`, built on the Fire calculator: an internal check-only branch of `ScenarioA1FireCalculator` runs the existing resolution of a check on its subjects and then the leader-loss cascade, with no IFT roll. Every MC, NMC, PTC, LLMC, and LLTC made outside the Fire and Rally packages calls it: the rout Interdiction NMC, the Bail Out NMC, D5.311's crew attack, and the LLMC after a leader lost to Mopping Up, a Sniper, Rally Fate, a PF firer's Casualty Reduction, Failure to Rout, or an Interdiction. Its facts are the Fire calculator's (`FireTarget` with the new optional facts, the rolls, the choices, the side's ELR), so Heat of Battle, ELR, wounds, and the cascade are decided once. The OVR NTC, the Deployment NTC, and PAATC are Task Checks with no cascade of their own and keep their paths.
- **R70.4:** a new event `morale-check-resolved` carries the facts and the verdict of a check made outside an attack, as fire records do; its effects are written through the Fire planner's own effect events. A check appended after an existing record (Heat of Battle after an Interdiction NMC's Original 2, every LLMC outside fire) is new, so it needs no gating fact: an older game holds none and replays as it did.
- **R70.5:** a Heat of Battle that rallies a routing unit or makes it berserk ends its rout in the Location where it was interdicted.
- **R70.6:** only the Voluntary Rout leader adds his leadership DRM to an Interdiction NMC (A10.711); a leader already in the hex a unit routs into gives none.

**F4. Unarmed and PRC under fire.**
- **R70.7:** a new optional fact `unarmed` on each target of a fire attack and a morale check, read by Heat of Battle's subject (A15.1) and by ELR immunity (A19.11). Absent means as before, so an older attack resolves as it did.
- **R70.8:** PRC leadership and LLMC in Fire (D6.651, A10.2): `LeaderReaches` is read in the check both ways; a Passenger leader's loss checks fellow Passengers of his vehicle only; Riders neither take nor cause LLMC. The facts are new optional ones on the PRC entries, so an older attack resolves as it did. Heat of Battle for PRC stays none (A15.1).
- **R70.9:** the Bail Out NMC takes a Rider leader's DRM (A10.7's EXC), Wound Severity for a SMC, and ELR Replacement, never Disruption (A19.12), through R70.3's member. Its `drm` and `elr` are recorded on `rider-bailed-out`; the projector's check compares them only when present. The pass 65 backlog row's Heat of Battle is closed as not owed (A15.1).
- **R70.10:** D5.311's attack on a crew already broken eliminates it by Casualty Reduction; a recorded fact on the vehicle target says the crew was broken, absent meaning as before. Its Heat of Battle, ELR, and leadership are not owed (A15.1, A19.11, D3.44).

**F5. Voluntary Break and Voluntary Rout.**
- **R70.11:** Voluntary Break is an action of the RtPh's start, before any rout step of the phase, by the owner of units (even pinned) in the LOS and Normal Range of an armed, unbroken Known enemy ground unit or ADJACENT to any unbroken enemy ground unit, refused when breaking would Reduce or eliminate the unit at once and for heroes, berserk units, and Unarmed units (A10.3). It writes `voluntary-break`, then the units broken and under DM (A10.62). A leader's Voluntary Break causes a LLTC (A10.2, p. 65: "Should an unbroken leader break").
- **R70.12:** Voluntary Rout is a `withLeader` argument on the rout action: a non-berserk, non-pinned, unbroken leader stacked with the broken unit before it routs moves with it, takes no NMC, adds his DRM to its Interdiction NMC (recorded in `RoutInterdicted.Drm` with the leader named), and is eliminated if it fails, which causes an LLMC through R70.3.

**F6. Rout (70.2).**
- **R70.13:** a routing unit may ignore the hexes of the building it starts in when choosing its destination (A10.532's EXC, p. 67).
- **R70.14:** a vehicle and a manned Gun Interdict within their Normal Range (the longest of their weapons, at most 16) when no positive DRM would apply to their attack, and they enter must-rout and RtPh DM as "unit/its-SW/Gun" (A10.5). Interdiction is decided at plan time and recorded, so no record fact is needed.
- **R70.15:** Fanaticism by SSR is a `game-started` special rule `fanatic:<side>` (optionally `:<group>`), setting `asl:fanatic` at setup; absent-safe.

**F7. Who sees (70.3).**
- **R70.16:** one Rules member, `ScenarioA1Sight`, with three purposes: Loss (Good Order, not a Dummy, not hidden, not a Passenger, not an empty vehicle, A12.14), GainDenial (unbroken, Dummies included, not hidden, not an empty vehicle, A12.122), and Inspection (Good Order ground units, A12.16). One Play read, the nearest seer's range by (side, purpose, Location), memoized per state in `PlayStepMemo`, serves movement's Unmask, the advance, Rally, a Gun's CA change, ordnance's crew reveal, fire's `WithSeen`, A12.34's reveal, Case H and vehicle movement, Opportunity Fire reveals, the OVR PAATC subjects, and the gain watchers. A Nondefinitive, null, or unsupported LOS read is taken as each caller takes it today (the frame does not change how the LOS is read, only who is a seer). Reveals are recorded events, so no older record changes; only new proposals do, as R35.5's did.
- **R70.17:** the actions that cost a "?" (A12.141, p. 78) add every built "other activity" a concealed unit engages in while in the LOS and within 16 hexes of a Good Order enemy ground unit (the Loss read): Deploy, Recombine, SMOKE grenades, a LLTC or LLMC, a PF or MOL check, a leadership modifier applied to anything but a PAATC (a MC, a Rally, a Search), Recover, Transfer, Dismantle, Scrounge, Hook Gun, and the Mopping Up declaration. A12.141's exceptions stay free: taking a PAATC or a Set DC NTC, dropping or repairing a SW or Gun, Spotting, a radio or field phone, and a leadership modifier applied to another concealed unit's PAATC. (The frame first wrote that a check costs none; A12.141 names LLTC/LLMC among its examples. Corrected on the page before any build, the lesson of pass 60.)

**F8. The Right of Inspection (70.10).**
- **R70.18:** after setup closes, an enemy stack not concealed and out of the LOS of every Good Order ground unit of the viewing side (R70.16's Inspection purpose) is shown as a stack is before play: its top counter, the rest a sealed count, its equipment withheld. Play gives the set of such Locations per (state, viewer), memoized; `GameView.Of` takes it as it takes the set out of sight. One Play entry builds every view (the page, the library, Replay, `UnitNames`, the sweep). A null or unsupported LOS counts as in LOS, so it shows as today. Events and records are unchanged: A12.16 allows verification of actions.
- **R70.19:** the change applies to every game, with no marker on the record. The view is derived and never recorded, A12.16 is a gap the plan says to build (plan, the withdrawn ruling), and a marker would keep the fault in the user's games. The gate's sweep builds the view through the new entry, and each differing view line is named as 70.10's.

**F9. The owner's choices on a "?" (70.4).**
- **R70.20:** voluntary removal of a "?" is an action of the owner at any time in his own or the enemy's phase: it writes `concealment-lost` for the units; a Dummy so removed is eliminated; a hidden unit is placed and revealed in full (A12.31). The Sniper exception is noted and not built (no Sniper state to read).
- **R70.21:** the viewer's option not to force a loss (A12.14) is built where every Loss seer of the viewing side within 16 is itself concealed: a pending choice to that side at the one read of R70.16, its brief reveal shown to the moving side only. If its resumes in the move, advance, and fire paths cannot all be built inside 3e's estimate, R31d.2's deviation stays and the rest goes to the backlog.

**F10. 70.5.**
- **R70.22:** an Emplaced Gun's crew and a vehicle gain "?" under the Concealment Table's Cases J and K, read from a render of the table; A12.122's +1 Lax, -1 Stealthy, and a possessed Gun's US# apply. A vehicle's Concealment Terrain gains the woods-, orchard-, brush-, and grain-road hexes at setup only (A12.2). Units entering as reinforcements may enter concealed where the card's OB gives "?" (A12.12), lifting R20.5's refusal. HIP is allowed for vehicles by SSR (A12.3), and an Emplaced Gun may set up hidden out of Concealment Terrain when out of every enemy LOS, revealed when that changes (A12.34), read through R70.16's memo after each step and at each phase.

- **R70.35** (added at 3e, 2026-10-25, under the same word): R70.22's "units entering as reinforcements may enter concealed where the card's OB gives "?"" is too narrow. A12.12 (p. 77, read on the page): "Thereafter, a "?" may be placed on top of any non-concealed Good Order unit/stack before it enters the mapboard for the first time." So any non-concealed Good Order reinforcement stack may take one non-OB "?" (one per stack, never on top of another, A12.12's EXC) before it first enters; OB-designated "?" and Dummies stay as a card gives them (no card gives an entering group either today).

- **R70.36** (added at 3e): a hidden vehicle takes one squad-equivalent of a side's SSR HIP allowance. A12.3 (p. 80) gives the allowance in "squad-capacity" and prices no vehicle; a card whose SSR names the units it hides is read as it names them.

**F11. Bypass and detection (70.8).**
- **R70.23:** a Minimum Move into a concealed enemy's Location is no longer refused: the normal forced back applies, all MF spent. A stack forced back from a Bypass exit keeps its lane (a fact on the step, absent meaning as before) and reveals every concealed unit in the Location. A Bypass member broken or pinned with concealed enemy units in the Location reveals them. Attempting to occupy an obstacle that holds concealed units reveals them and forces the stack back into its lane, MF spent. A firer in a Bypassed hex may fire TPBF at a unit in Bypass there. A vehicle ending its MPh in Bypass of a Location with concealed enemy units reveals them unless it is "broken" (no crew and no PRC, A12.1); hidden units stay hidden (A12.42 names concealed ones).

- **R70.37** (added at 3f): lifting the refusal of TPBF inside a Bypassed hex does not make A8.312's Mandatory Fire owed against a stack entering Bypass of a Location that holds units of the other side: A12.151 says those units "may" use TPBF, and forcing it would reveal concealed units. `ScenarioA1MandatoryFire.Owed` owes nothing against a stack in Bypass, as before the pass.
- **R70.38** (added at 3f): a stack forced back by a Minimum Move is neither pinned nor CX (A4.134, p. 48, pins and CXes units that "entered a hex by Minimum Move"; a unit forced back entered none).

**F12. Searching and Mopping Up (70.9).**
- **R70.24:** Searching is an action of the moving stack as it ends its move: one more MF in its hex, a Search dr with A12.152's drm, the hexes named in the ATTACKER's order of preference before the dr (the same choice he would make after it), and the first (Accessible count less the Final dr) searched. Accessible hexes are, until the Index is read, its own hex and the ADJACENT hexes the stack could enter. The searchers are TI for the rest of the Player Turn. Concealed enemy units revealed lose "?"; hidden ones are placed beneath "?"; Dummies are removed. Search Casualties (A12.154) roll at once, one dr per Search dr where a Searched Location holds an armed Good Order DEFENDER unit, reusing Mopping Up's Casualty members. What each side learns is public: the dr, the hexes, and every reveal.
- **R70.25:** Mopping Up applies to any multi-hex or multi-level building of the map. Play finds a building's hexes by the Studio's flood fill moved into Play, memoized per board handle; a building a card names keeps the card's id, so older records keep theirs, and any other takes "board:lowest hex". Control folds for any building. One enumerator of the occupied Locations on `ObjectIndex` serves Searching and Mopping Up: one pass over the occupied stacks, never a lookup per Location.

- **R70.39** (added at 3g): the Search dr, its drm, and the hexes searched are public, though the drm shows a concealed searching stack's HS-equivalents and whether a leader leads it: A12.152 makes the dr in the open with its listed drm, and a leadership modifier applied to the Search costs the searchers' "?" (A12.141, R70.17). Accessible hexes stay R70.24's reading: the text layer has no Index definition of "Accessible".

**F13. Withdrawal and a Gun's crew (70.6).**
- **R70.26:** a withdrawal is barred only by a Known enemy unit, read from the side's view. Into a concealed (not Dummy) enemy's Location the withdrawer is eliminated, a Random Selection of the enemy units there loses "?", and hidden units are placed (A11.21, p. 73, with its example); into a Dummy-only Location the Dummies are removed. A unit above its IPC may withdraw by dropping SW, which stay unpossessed in the Location.
- **R70.27:** a Gun's crew is advanced against and fights in CC with its inherent FP (A1.123); the Gun is never used in CC (A11.13), and each Gun of a unit eliminated in CC is eliminated on a colored dr of 1 followed by a dr at or below the black Kill Number, otherwise left unpossessed. The five refusals are lifted. The Guns each unit possesses are a recorded fact of the CC resolution; absent, the Gun loss is not rolled, as before.
- **R70.28:** a Carrier HS recombines with no leader and no TC (A1.32, p. 45, names "Carrier HS"): the leaving crew's `instance-created` records the vehicle it left, absent meaning an ordinary HS.

**F14. Plumbing.**
- **R70.29:** `GameProjector.CheckInvariants` (UNIT-STATE-011, 012, 014) are plumbing, not rules: they guard the object graph and decide no outcome; A20.5's decisions are already in Rules. They stay in Units, and the backlog row is closed with this ruling. Their 12% of a replay (pass 31d) stays a performance item.

**F15. Close Combat with a vehicle (70.7).**
- **R70.30:** in a Location with a vehicle, Infantry against Infantry CC joins the vehicle sequence (A11.31, the H3 example): a sequential round, one attack by the side named next, no simultaneous reply; each unit attacks once per CCPh, being attacked by Infantry and by a vehicle counted apart (A11.12's EXC). An Ambush there takes the vehicle drm (+2 Vehicle, +1 for each BU or Stunned AFV, A11.4), the ambusher resolving all its attacks first (A11.34), and the -1 and +1 in vehicle CC both ways (A11.51). The Ambush's vehicle drm is a recorded fact, null where no vehicle stands.
- **R70.31:** PRC in CC are wired from `ScenarioA1PassengerActionRules.CloseCombat`: a Rider attacks apart with +1 and defends with -1 (A11.611); a CE halftrack's Passengers join the vehicle's attack (A11.62); the rest are offered nothing. Only the PRC of an Immobile vehicle are held in Melee at the CCPh's end (A11.71); the CCPh-ending record carries the PRC so held, absent meaning as before.
- **R70.32:** the CC counter on an unload, a Bail Out, or TPBF into an enemy Location (D6.5, D6.24, A7.211) is no stored condition: units are held in Melee only at the CCPh's end already. The record line says the units are in an enemy Location and not held in Melee, citing the rule.
- **R70.33:** Street Fighting (A11.8): a vehicle in a road hex ADJACENT to building hexes on both sides of the road may be attacked in CC by Infantry advancing from the ground level of those buildings with -1, the vehicle's attack on them +1, not with an actual Ambush; afterwards they return to the Location they came from or stay, unless broken or pinned. The building flank is a Maps read; where the unit came from is a state fact the projector sets from the advance. Its Reaction Fire (D7.211) stays with pass 105, Bypass with pass 100.
- **R70.34:** A11.52's capture of Passengers: ordinary Infantry CC at the Passengers of an unarmored stationary vehicle, -2 on attacks at them with the capture's +1 and +2 on their attacks at the captors; when all are captured and no enemy Personnel remain, the vehicle is captured (`vehicle-captured`). A18.12 against a vehicle: a MMC's Original 2 in vehicle CC draws a Leader Creation dr, the created leader adding his leadership and +1 CCV only if the attack had no SMC; `leaderCreation` is taken from the record, absent meaning no roll. D5.42: a captor may enter the captured vehicle as its crew in the CCPh of its capture.

## 7. Memory and performance

**The checklist** (the guidelines' section "Design and review checklist").

- **Work added per state:** R70.16's seer Locations per (side, purpose), built once from `ObjectIndex` (one pass over the units); R70.18's uninspected set per (state, viewer), one LOS read per pair of an enemy unconcealed stack Location and a seer Location, with an early exit at the first seer in LOS; R70.25's occupied-Location enumerator (built with the index, no extra pass) and the building flood fill per board handle (fixed for the map).
- **Per unit and per hex:** none outside the memo. The six who-sees reads stop rebuilding every unit's facts per call; Searching and Mopping Up read the occupied stacks once.
- **Per step and per render:** the view is built as today, once per (history, revision, viewer) on the page; the set it takes is the memo's.
- **Ownership and keys:** both memo stores are owned by `PlayStepMemo` (state lifetime, `ConditionalWeakTable`), keyed by side and purpose, and by viewer; read only; concurrent fills may compute equal answers twice, with no side effect. The building map is owned by the board handle's memo. Disclosure: the uninspected set is keyed by viewer; the seer reads by side.
- **LOS:** each LOS read goes through the planner's existing reads, which cache a (from, to) answer per map; the cold `LosCalculator.Check` is unmeasured and is the measurement's first item.

**The measurement** (stated before building, as the user asked on 2026-10-19).
- **Workload:** a copy of `dl-tractor-works-02` cut at revision 1316 (`perf_cut.py` of pass 65, under a new id; the user's game is not touched): the hand-over (German to Russian) and an "end the phase" proposal, three warm runs each; and the time to open the whole `dl-tractor-works-02` in Play from a cold Studio (the page's view and `UnitNames` over its 1,441 revisions, which R70.18 reaches).
- **Baseline:** the gate's `main-bin` build of 25aa97fd on port 6671, measured alone; then the pass's Studio on 6670, alone (the lesson of pass 65).
- **Expected:** the hand-over and the proposal level with the base (the who-sees reads get cheaper, the view adds a few cached LOS reads per state); the cold open at most 20% slower (one uninspected set per revision per viewer).
- **Stop threshold:** a warm median more than 20% over the base's, or a cold open more than 25% slower, stops the next increment until the cause is found by a trace.
- **When:** after 3a (the view and the seer memo) in the Studio first, before any costly test; again after 3g and at the whole-pass check.

**Build outputs outside the repository.** My Studio in `asl-gate-work/studio-6670` (registered by `build_studio.sh`); the gate's own (the base build, the proof tools, registered by `gate.sh`); the filtered test runs' `asl-gate-work/test-classes-bin/<project>` (registered by `run_test_classes.sh`); any strict build of a test project by an agent, registered as it is made. All are removed at the latest by the gate's `ci` stage; the last task confirms `outside_outputs.sh list` is empty.
