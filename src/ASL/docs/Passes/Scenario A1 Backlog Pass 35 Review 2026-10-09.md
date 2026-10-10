# Scenario A1 Backlog Pass 35: Repairs

The review of pass 35, written on 2026-10-10 after the five reviews of 2026-10-09, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 35 Design](<ASL Unit Backlog Pass 35 Design.md>); its section 19 says what was built against what was designed.

## Status

Built on branch `feature/asl-backlog-pass-35`: the seventeen tasks of the design in three sessions (to 5dd9934), then in the fourth session the five reviews, their fixes in seven commits (d27713d2 to 39d5045c), the Studio check, and these documents. **The gate was run on 2026-10-10 at the user's word, at c22b70ce, and passed** (the section "The gate" below). **Merged into main as 5355b7f6 on 2026-10-10 at the user's word and pushed; the CI run of the merge passed.** Main had moved six commits past the branch point, to 4b30d429 (the europe-hex-map prototype and eight documents); no file was changed on both sides, the merge had no conflict, and CI on the merged tree is the test of the two together.

Run in the fourth session, and no wider: MapStudio built with warnings as errors after every fix, and the Rules, Play, and page test classes each fix touches. At the last run 127 Rules tests, 105 Play tests, and 39 page tests passed in those classes.

## What a player now meets

Each line is a change a player can see at the table, with the rule it rests on. The first group is the pass as designed; the second is what the reviews changed.

**The pass as designed (sections 7 to 13 of the design)**

- A unit that will surrender owes no rout and holds nobody's rout up; a Disrupted unit routs only when it must and never by Low Crawl (A19.12, A20.21).
- Interdiction is read as fire reads it: Hindrances, a wall or hedge, Height Advantage, a wreck or AFV in the Location, and the range a lone leader may fire a MG at (A10.531, A10.532).
- A rout into a concealed unit's Location is repulsed, and one concealed unit there loses its "?" (A10.533).
- ADJACENT is A.8's: a LOS and an Infantry advance, so a hex up a hill or across a wall is ADJACENT and one across a cliff is not.
- One wound procedure, with its Wound Severity dr said in the record; Good Order with a vehicle's stun and shock; a Fanatic unit's broken Morale Level; the Russian 4-2-6's Battle Hardening.
- SMOKE's MF doubled with the COT one level up; grain's season on a LOS; HE halved into a marsh; the orchard for vehicles; a Wreck Blaze.
- Ordnance: towing bars, rubble, the Backblast Location, Case L for an ATR, Case O, Opportunity Fire, the loss of an Acquisition.
- Vehicles: the road rate in snow and at an exit, Non-Stopped, the CMG of a CE tank, the Stun +1 in an Overrun and in Close Combat, a bogged vehicle's unloading, a Recalled vehicle's Stop to unload and its Abandonment, the ESB table.
- Night and weather: the MP of a Reverse, a Bypass, and an exit; the unpaved road and the wall's gap in Mud.
- A game that holds a fortification, rubble, or Flame counter is refused, since none of their rules is built.

**What the reviews changed**

- A unit repulsed from a concealed unit's Location is eliminated as the RtPh ends and is not taken prisoner (A10.533's example, p. 68).
- A unit routing up a hill across the Crest Line that its enemy's LOS crosses is Interdicted there, and one whose only way out is such a climb surrenders (B1.14, B10.31, the example on p. 69).
- No Case J against a vehicle that began its MPh bogged and has not left its Bog hex, whatever its MP went on (D8.4, C.8).
- An exit off the map by an unpaved road in Mud costs what an entry costs: the Open Ground cost and the Mud MP (E3.6).
- A tank that changes its VCA without having fired on its acquired target loses its Acquisition (C6.5).
- A hero alone with a MG Interdicts at the MG's range (A15.23).
- A unit that has already surrendered by the page (a Disrupted unit; an ATTACKER's unit before the DEFENDER routs) no longer bars the other side's rout routes (A19.12, A20.21, the example on p. 69).
- Two marsh hexes are not ADJACENT, since no advance enters a marsh (B16.4).
- A Bypass one level up into SMOKE costs 4 MF, not 3 (B.2).
- The Wound Severity record names the man, the +1 of a man already wounded, and what the dr comes to. The refusal of an unbuilt counter names it as the counter is named ("Wire").
- The Rout panel says, in Rules' words, when a rout only passes through the places within reach (A10.5, A10.51).
- A Recalled AFV is offered an exit only by its Friendly Board Edge (D5.341).
- The page no longer keeps a thrown DC, a wrecked target vehicle, a broken Spotter, an exit edge, or a CC Reaction Fire choice from one action, view, or game to the next.

## Rulings

R35.1 to R35.10 are in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>). The reviews changed three:

| Ruling | Change | Why |
|---|---|---|
| R35.3 | A repulsed unit left ADJACENT to a Known enemy unit is eliminated and never surrenders. As first built it surrendered when a captor could take it | A10.533's example: the enemy would have had to drop its "?" by choice before the rout entered to take prisoners |
| R35.2 | One sentence added: for the other side's rout routes a unit bound to surrender is a prisoner from the moment the page has it surrender. The event stays at the phase's end | A19.12 "at the start of any RtPh"; the Comprehensive Rout Example, p. 69 |
| R35.9 | The Non-Stopped reading now cites C.8 and is marked as a reading. No code changed | The referee found the page silent between C.8's "moving" and D8.3 |

## The five reviews

All read-only, run side by side on 2026-10-09, between five and ten minutes each: the referee in two halves (chapters A and B; chapters C to E), the table player, the Rules boundary, and the UI and Blazor review. Each gave its findings in its closing message. Nothing was fixed before the user had seen them; the user approved the proposed fixes, then each fix was read on its page, repaired in Rules, shown in a Studio game where one could be staged, and tested.

| # | Finding | From | What was done |
|---|---|---|---|
| 1 | A repulsed unit is offered surrender where A10.533 gives elimination (graded a blocker) | Referee A, B | Fixed, d27713d2; R35.3 corrected |
| 2 | Interdiction never reaches a unit climbing across the Crest Line the LOS crosses | Referee A, B | Fixed, 1d395595 |
| 3 | Case J against a bogged vehicle that unloads first | Referee C to E | Fixed, 4d4a594b. The wider, older fault (Case J for a Start MP alone) is a backlog row |
| 4 | A Recall exit by an unpaved road in Mud keeps the road rate; the road rate written twice | Rules boundary | Fixed, 6b891ff1: one member for both |
| 5 | A tank's VCA change keeps its Acquisition | Referee C to E | Fixed, 486a893c. A TCA kept apart from the VCA is a backlog row |
| 6 | A lone hero with a MG denied its range for Interdiction | Referee A, B | Fixed, 505a998f. A lone leader's mortar is a backlog row |
| 7 | A unit bound to surrender bars the other side's routes | Referee A, B | Fixed, 39d5045c, narrower than first proposed: only a Disrupted unit and an ATTACKER's unit |
| 8 | Two marsh hexes read as ADJACENT | Referee A, B | Fixed, 39d5045c; no Studio game (board 3 has no marsh) |
| 9 | Non-Stopped is defined in C.8, which R35.9 did not cite | Referee C to E | R35.9 amended; no code change |
| 10 | SMOKE not doubled one level up in Bypass | Both referees | Fixed, 39d5045c |
| 11 to 14 | Stale drafts in the Play page: the thrown DC, the ordnance target, CC Reaction Fire, the exit edge, the berserk "keep" text, the editor's boxes; another game not clearing every draft | UI and Blazor | Fixed, 39d5045c |
| 15 | A page test that passed with the fix removed; two smaller test slips | UI and Blazor | Fixed, 39d5045c |
| 16 | The page tested "a bogged vehicle may unload" itself | Rules boundary | Fixed, 39d5045c: Rules answers |
| 17 | The page inferred the A10.51 reason; the sentence cited the wrong rule | Rules boundary, table player | Fixed, 39d5045c: Rules words it |
| 18 | The rout's MF allotment still two functions | Rules boundary | Fixed, 39d5045c |
| 19, 20 | The `asl:ti` step bar in two places; the marsh level and marsh in Snow; the projector's literal fact to BoggedMaySpend; the D9.4 geometry and the own-hex Hindrance filter in Play; the rout cover reader's hidden side | Rules boundary, both referees | Backlog rows |
| 21 to 23, 25, 26 | Wording: the unbuilt-counter refusal, the Wound Severity record, "stun recovery", the Replay title of a repulse, two rout sentences | Table player, UI and Blazor | Fixed, 39d5045c |
| 24, 26 | Wording not fixed: "No cc armament", the reason for a morale level in an NMC record, the unit in the CC counter refusal | Table player | Backlog rows |
| 27 | The design cites a Studio game for repairs whose file holds only its setup; six labels promise more than their events | Table player | The design's section 19.3 says which games are recorded; no game was replayed for it |
| Notes | Low Crawl for a Disrupted unit under No Quarter; a unit pinned while ADJACENT; the repulse's dice count; no record sentence for a vehicle Close Combat; the gaps of the view sweep test; the "With SMC" list | All | Backlog rows |

**The readings the referees were asked about.** ADJACENT in either direction: the page is silent on direction, and marsh in the APh is its one one-way case. A repulsed unit surrendering: contradicted, and fixed. Surrender as the RtPh ends: "owes no rout" supported, the timing contradicted for a Disrupted unit, and fixed for the routes. Weather and night MP after the Reverse multiplier: supported. "Touching" a Bypassed hexside: silent; the hex reading is an approximation. Non-Stopped: "never while bogged or immobilized" supported, the rest a reading. The Acquisition's loss errs only toward keeping.

**Found sound by the referees:** the wound procedure, Good Order, the Morale ceiling, the 4-2-6's hardening, the uphill SMOKE in an entry, the orchard rows, grain's season, HE halved twice, the Wreck Blaze, the towing bars, rubble, the Backblast Location, the ATR's Case L, Case O, Opportunity Fire, the ESB table, the Recall's ESB bar and Stop, BoggedMaySpend, the CC counter, the Bounding First Fire bar on CE changes, the Stun +1, the turret rows in an Overrun and in Close Combat, the Mud road and gap, the night unload. The table player found every recorded DRM, Final DR, and result it checked right by its page. The boundary review found no reference from Rules to Units, Maps, or Play, and no new required parameter on a serialized record.

## The Studio check

On 2026-10-10, in my Studio on port 6671, from the build of 39d5045c.

- **The width sweep:** the game `p35-rst-cc`, with the vehicle Close Combat panel open, in the German, the Russian, and the adjudicator's view at 1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, and 320 wide. No horizontal overflow and no element past the viewport in any of the fifteen.
- **One full Player Turn with vehicles, twice.** `p35-check-wide` at 1920 by 1080 in the two sides' views, with a hand-over for every DEFENDER's pass: a tank's Start, entry, and Stop, a halftrack's unload, and the eight phases to the Russian RPh. `p35-check-narrow` at 320 wide in the adjudicator's view, the same turn, its first phase change by the keyboard (Enter on the focused button, Enter on Confirm; the focus then on the actions, the Actions tab open).
- **The records of the new events at 320,** in both sides' views (`p35-wound-again`): the lines wrap and nothing overflows.

**Seen on the way, older than the pass, each a backlog row:** the Gun's target list offers a Location the Gun has no LOS to, and the refusal reads "Vehicle fire outside"; the phasing side's view cannot end the DFPh, which is right, but says so only by a disabled button; the "Latest" line did not follow five phase changes in a row; at 320 the hand-over prompt lies over the foot of the Activity list.

**Not covered:** the keyboard for a whole turn; and, unseen in the Studio, the thrown DC's draft (no live game holds a DC), the CC Reaction Fire binding, the "Stun +1" words, and the Replay title of a repulse. Those four rest on the build, the tests, and reading.

## Tests

Added or changed in the fourth session: in Rules, assertions in `ScenarioA1RoutRallyRulesTests`, `ScenarioA1Pass35RulesTests`, and `ScenarioA1VehicleMovementRulesTests` for each fix's verdict; in Play, five cases in `BacklogPass35Tests` (the repulsed unit's elimination, the Crest Line in three cases, the surrendered unit in two, marsh in two) and one changed assertion in `ConcealedEntryTests`; in the page tests, `PlayPageVehicleTests`, `SetupPlansPageTests`, and `CloseCombatComponentTests`. Fixes 3 to 6 have no Play test: their Studio games' records stand for the planner's side.

The marsh test's marsh-to-marsh case may pass for a weak reason if the test board does not know "Marsh" as terrain; its dry-ground control does pass as ADJACENT.

## The gate

Run on 2026-10-10 from 08:28 to 09:38, at c22b70ce, by two scripts kept in the tools folder (`gate35-baseline.sh`, `gate35-gate.sh`) and the Docker check.

| Step | Result |
|---|---|
| The solution build with warnings as errors, into `bin/gate` | 0 warnings, 0 errors |
| The whole local suite, one project at a time | All pass: Rules 737, Play 722, MapStudio 390, Units 409, Authoring 167, Maps 244, Units Rendering 352, Maps Vasl 121 (28 skipped), Maps Rendering 34 (2 skipped), Counter Sheets 19, Dice 24 |
| CI's Node viewport test | exit 0 |
| The chart supplement | EQUAL |
| Docker, Linux, the committed branch | restore, build, test, and both regenerations exit 0; every project passes with the local counts |

**The three proofs,** against a baseline of main at f3b7c47 built from its own sources (checked: it holds `ScenarioA1SequenceCalculator` and no `UnbuiltCounterBar`), over the proof store brought up to date with the live folder first (309 to 380 games). Every difference was read and named; none is a fault.

- **The text list:** 15 differing lines, each a task's sentence: the Low Crawl bar (35.2), the bogged vehicle's expenditures (35.13 f), the Recall's Abandonment and ESB bar (35.13 g), the CE bar after Bounding First Fire (35.13 i), the CC counter's bar, the unbuilt-counter refusal (35.15), the Recombine review (35.16), and one more Random Selection roll (the repulse). No literal was dropped without a task.
- **The replay digest:** 380 games, 91,587 states on main and 91,623 on the gate build. 370 games are identical, among them every game that is not the pass's own. Ten differ, all `p35-*` games:
  - nine that main cannot replay to their end and the gate build can, since their records hold a fact or a result only the pass gives (p35-bog-unload, p35-bog2, p35-ord-draft: a bogged vehicle's unloading; p35-grain-nov: grain out of season; p35-harden: the 4-2-6; p35-ordnance: the Opportunity Fire fact; p35-rst-cc and p35-rst-ovr: the Stun +1; p35-vca-acquire: the Acquisition lost by a VCA change). They have no "before" and are listed, not compared;
  - `p35-acquire`, which stops at its shot's record on the gate build (`UNIT-STATE-033`, revision 9): the Non-Stopped fact of 35.13 b and h, as the design's section 14.3 gives. Class 2 of section 14.4.

  No review fix made any other recorded game fail its check.
- **The planner sweep:** ten games (the four of pass 32, and p35-interdict-trap, p35-rally-enemy-turn, p35-smoke-hill, p35-tow2, p35-bog, p35-mud), 515,010 lines a side, 401 lines differing on each, in four classes:
  - **ADJACENT** (A.8): 22 pairs of Locations read False on main and True now, across eight games (p35-bog-recall was swept with p35-bog); none the other way.
  - **The rout step's record** has one more optional property (the Location a repulsed step attempted), so the digest of a state's view differs in Tractor Works from its first rout on (115 states in three views, exactly the cuts at or after revision 278), and the digest of 12 rout plans with it; their summaries and event counts are the same.
  - **A concealed mover's "?"** (35.3; A12.14): 15 move plans into [Y7] of Tractor Works plan one event fewer, the loss of concealment, since the unit that sees a mover must now be Good Order, not hidden, and not a Passenger. Read by a copy of the proof tool that prints a plan's events (`proof-dump`).
  - **The pass's own swept games:** a Recalled halftrack with Passengers may Stop (35.13 g); a Recalled vehicle is offered no exit but by its Friendly Board Edge; the bog refusal's words (35.13 f); a CT AFV that is BU by default is refused "button up" as already BU (35.13 a).

## Left out

Everything the pass and its reviews left out is a row of the [backlog](<../ASL Unit Backlog.md>)'s section 55.2.

## Carried to the gate

- **Recorded games may fail their checks, and that is expected** where a fix changes a fact or a route their record holds: the repulse's outcome (fix 1), Interdiction on a climb (fix 2), the Bog hex fact (fix 3), an exit's cost in Mud (fix 4), a tank's Acquisition (fix 5), a hero's Interdiction (fix 6), and the routes beside a surrendered unit (fix 7). `p35-acquire` is the known one (the design's section 14.3). Each difference is read and named at the gate.
- **The three readings** the rulings record as readings (R35.3 as corrected, R35.7 to R35.9).
