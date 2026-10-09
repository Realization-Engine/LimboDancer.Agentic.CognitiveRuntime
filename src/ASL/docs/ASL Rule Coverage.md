# ASL Rule Coverage

**Status:** First assessment 2026-10-05, of `main` at 038dca8. **Corrected the same day from the rulebook PDF**, of `main` at 16fb5f0: every section's status and counts now come from the [ASL Rule Inventory](<ASL Rule Inventory A to E.md>), which has one row for each of the 2,127 numbered rules of Chapters A to E. It is a snapshot: a pass that builds a rule changes its rows in both documents. **Pass 35 (2026-10-09, on its branch, not yet merged)** changed the rows of 25 sections and 59 rows of the inventory; the counts of sections 3 to 6 are recomputed from the inventory, and the faults of section 8.3 it repaired are marked there.

**Question answered:** which rules of the rulebook does the game implement today, wherever the code sits, and which does it not.

**Scope decided by the user, 2026-10-05:** the aim is complete rule coverage of Chapters A to E. Chapters F, G, and H are deferred, as the legacy scenario cards are. Their rows in section 6 stay as a record and are not work to plan.

**How it was made.** The first version indexed every dotted rule citation under `src/ASL` and had five read-only audits go through the rulebook by section, from the repository's transcription. The correction started again from the PDF: a script listed every numbered rule from the PDF's outline and its page text, and 19 read-only audits read each rule on its page and judged it against the code and the tests. A citation in the Authoring project or in a comment was not counted as an implementation. What I checked myself is said in the inventory's section 1.

**Limits, stated plainly.**

- No test was run. "Built" means logic and a test were found by reading, not that the suite was executed for this document.
- A section's status is computed from its subsections by the rule in section 1, not judged by eye. The text in the "Built" and "Missing" columns is the first version's, corrected where the reading showed it wrong; the inventory's rows are the fuller and the later account, and where the two differ the inventory is right.
- Most statuses are an audit's, taken as reported. The audits' own findings say where a claim rests on a search that found nothing, or on a test's name and not its body.
- Chapters F, G, and H are assessed at chapter level only. Chapters I to Z of the PDF are outside this assessment.

## 1. How to read the tables

A section's status is computed from its rows in the inventory. Rows that are not applicable (headings, definitions, table procedure) are left out. Each built row, and each row built with a deviation, counts one; each partly built row counts a half; the sum is divided by the number of rows.

| Status | Rule |
|---|---|
| **Built** | Every row is built, or built with a deviation. |
| **Mostly built** | The share is 0.65 or more. |
| **Partly** | The share is 0.30 or more. |
| **Mostly not built** | The share is above 0. |
| **Refused** | Nothing is built, and at least half of the rows are refused: the game meets the situation and refuses it with a reason. |
| **Not built** | Nothing is built, and fewer than half of the rows are refused. |

"Subsections" gives the number of rows that carry a rule and how they stand. "Lives in" names the projects that hold the logic: **Play** (the planner, `LimboDancer.Domains.Asl.Play`), **Rules** (the calculators and packages, `LimboDancer.Domains.Asl.Rules`), **Units** (state and the projector), **Maps** (LOS and terrain). "Pass" lists the passes of the [redesign plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, that take the section's rows; "ruling" means some rows are left out by a ruling named there. Section 22.1 awaits the user's approval, and pass numbers above 44 are its proposals.

## 2. The summary

**By section, Chapters A to E (105 numbered sections).** Counted by script from the rows of sections 3 to 6.

| Chapter | Sections | Built | Mostly built | Partly | Mostly not built | Refused | Not built |
|---|---:|---:|---:|---:|---:|---:|---:|
| A. Infantry and basic rules | 26 | 0 | 14 | 9 | 1 | 0 | 2 |
| B. Terrain | 37 | 0 | 3 | 7 | 20 | 0 | 7 |
| C. Ordnance and OBA | 13 | 0 | 4 | 5 | 2 | 0 | 2 |
| D. Vehicles | 17 | 0 | 2 | 5 | 3 | 0 | 7 |
| E. Miscellaneous | 12 | 0 | 0 | 1 | 1 | 0 | 10 |
| **A to E** | **105** | **0** | **23** | **27** | **27** | **0** | **28** |

**By rule.** Of the 2,127 rows of the inventory, 212 carry no rule. Of the other 1915: 401 are built, 42 built with a deviation, 391 partly built, 92 refused, and 989 not built.

Chapters F (13 sections), G (18), and H are not built. The LOS code reads some Chapter F and G terrain because it reproduces VASL's LOS; no rule of those chapters is played.

**In words.** Chapter A is the game: its sections are built in their main lines, and the reading found them thinner at the edges than the first version said, with many small faults. Chapter B is played on a short list of terrain. Chapters C and D have a working core for two tanks and two Guns and stop where armor begins to manoeuvre. Chapter E has night and weather in part and nothing else.

**Seven limits that cut across the sections.** These decide more of what can be played than any one row below.

1. **Terrain is a short list.** Movement and fire accept Open Ground, roads, brush, woods, orchard, grain, marsh, rubble, and ordinary wooden and stone buildings with their upper levels; walls and hedges as hexsides; hills. Every other terrain name is refused, in movement ("is not a reviewed entry") and as a fire target ("has no TEM"). A board that prints a gully, a stream, a shellhole, a crag, or a graveyard cannot be moved through or fired into at those hexes.
2. **Six Hindrances are decided.** A LOS that crosses brush, in-season grain, same-level marsh, an AFV or wreck, a burning wreck's smoke, or SMOKE grenades takes its Hindrance. A LOS that crosses any other Hindrance (orchard, crag, graveyard, Light Woods, a bridge) leaves the attack refused as undecided; none is silently dropped. (The first version said three.)
3. **Ordnance fires on one level, at hexes with no hexside terrain.** A Gun's, a mortar's, a LATW's, or a tank's shot at a target on another level is refused, and so is any ordnance or vehicle fire at a target hex with a wall, hedge, or cliff hexside.
4. **Mixed Locations are refused, not resolved.** Infantry with a vehicle in one target Location, two vehicles in one Location under small arms fire, an AFV in terrain with a positive TEM under small arms fire, a Gun's crew stacked with other units: each is refused.
5. **The catalog is small.** 173 counters of eight nationalities (German, Russian, American, British, French, Italian, Finnish, Axis Minor). Five are vehicles (PzKpfw IIIH, T-34 M41, SPW 251/1, Opel Blitz, GAZ-MM) and two are Guns (7.5cm leIG 18, 45mm PTP obr. 32). A rule that needs an armored car, a non-turreted AFV, a medium mortar, a bazooka, or a Japanese squad has no counter to act on, even where the code admits the type.
6. **Fortification counters are ignored.** Wire, foxhole, trench, minefield, roadblock, and pillbox counters can be placed, and the planner reads them in one place only (a mover forced back into a Location that holds one is refused): a unit moves through wire and stands in a foxhole with no effect. This is the one place where the game is silent instead of refusing. **Pass 35 (2026-10-09): a game that would place or holds one, or a rubble or Flame counter, is now refused whole (ruling R35.10), until passes 115 and 120 build them.**
7. **Passengers are cargo.** They load, ride, and unload. They do not fire, rout, or fight, they are never a target, and they die with their vehicle without a roll.

An eighth, found by the reading: **ADJACENT is narrower in the code than in the rule** (A.8, p. 43). The code asks for the same level and no terrain on the shared hexside; the rule asks only for a LOS and that Infantry could advance between the two Locations. A hex one level up a hill, a hex across a wall or hedge, and the next level of a stairwell hex are ADJACENT by the rule and not by the code, and rout, surrender, DC placement, and multi-Location fire groups all read it.

## 3. Chapter A: Infantry and basic game rules

| § | Title | Status | Subsections | Lives in | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|---|
| A.1 to A.18 | The chapter's introduction | Mostly built | 8: 3 built, 2 deviation, 3 partly | Play, Rules | A.5, A.9, A.10 and the rest of the conventions | **ADJACENT (A.8) is narrower in the code than in the rule**: the same level and no hexside terrain are required, and rout, surrender, DC placement, and fire groups read it. Good Order (A.7) counts a berserk unit and not a TI one. No Morale ceiling of 10 (A.18). Sustained Fire has no X# (A.11) | 45, 46, 47; ruling |
| A1 | Personnel Counters | Mostly built | 9: 5 built, 2 deviation, 1 partly, 1 not built | Play, Units, Rules | Printed FP, range, morale; Deployment (1.31); Recombining (1.32); US# in the Concealment dr | Squad Spraying Fire off the underscored range (see A7.34); a manned Gun is not reassigned on Deployment; the leader exemptions of 1.31 and 1.32 (Finns, a Guard of prisoners, unarmed units) missing | 46, 64 |
| A2 | The Mapboard | Partly | 15: 2 built, 3 deviation, 5 partly, 5 not built | Play, Maps, Rules | Boards and playable area; Locations by level; entry and its delay; exit; setup limits (2.9) | Overlays (2.7 to 2.76) not built; offboard setup simplified (2.51); terrain with no entry cost refused; a card that sets units up broken is refused | 37, 46, 60; ruling |
| A3 | Basic Sequence of Play | Mostly built | 10: 8 built, 2 partly | Units, Play, Rules | All eight phases, their restrictions and clean-up; the turn record and the game's end | The first-move dr is manufactured (R20.2) | 46 |
| A4 | Infantry Movement | Mostly built | 35: 10 built, 4 deviation, 20 partly, 1 not built | Play, Rules | MF, leader bonus, terrain costs, Minimum Move, stacks and the DEFENDER's window, portage and Recovery, Double Time and CX, FFMO and FFNAM, Assault Movement, the advance, PAATC, TI | **Dash (4.63) not built**; no ordinary Infantry OVR completes (4.15), though a berserk one is built (R27.3); Bypass only at ground level of woods and buildings, with vertex LOS refused (4.34); the road rate always taken (4.132, R10.1); AFPh limits for a moved MMG or HMG (4.41) not built; Hazardous Movement only for a pushing crew; a road-rate entry into a woods or building hex keeps the obstacle's TEM and takes no FFMO (4.132); a moved MMG, HMG, or pushed Gun may fire in the AFPh (4.41); a SW dropped in mid-move gives its MF back (4.4) | 46, 52; ruling |
| A5 | Stacking Limits | Partly | 11: 1 built, 1 deviation, 6 partly, 3 not built | Play, Rules | Limits at setup, entry, and advance; squad-equivalents; overstacking penalties in Close Combat and on ordnance To Hit | **No overstacking penalty on small arms fire** (5.12, 5.131); the extra MF is charged in the APh only (5.11); vehicular stacking (5.132) not built; any number of vehicles share a Location (R11.6); an MPh entry that overstacks is free and unrefused; R11.6's +1 for a second vehicle is not in the code | 37, 46 |
| A6 | Line of Sight | Mostly built | 14: 11 built, 1 deviation, 2 partly | Maps, Play | LOS between Locations, obstacles by height, depressions, Blind Hexes, buildings; a Hindrance total of six blocks | Six Hindrances decided in fire and any other leaves the attack refused (limit 2 above); Bypass vertex LOS refused; LOS returns "unsupported" for bocage Blind Hexes, some slope and hillock cases, factory rooftops; LOS checks are free (R31d.4, against 6.11); no Random Events on a blocked shot | 52, 60; ruling |
| A7 | Fire Attacks | Mostly built | 50: 29 built, 14 partly, 7 not built | Rules, Play | The IFT and its results; PBF, TPBF, Long Range, Area Fire, AFPh halving; Opportunity Fire; SW usage; Assault Fire; fire groups, their direction and mandatory formation; TEM; pins; Cowering; Encirclement | Squad Spraying Fire (7.34) and upper-level Encirclement (7.72) not built; fire at another level of the firer's own hex refused; a vehicle's fire at another level undecided; a multi-Location group's chain not checked; Opportunity Fire for small arms and MGs only; Cowering shifts the column and marks no unit (7.9); a leader cannot direct a MG's later ROF shots (7.53); fire between levels of one hex is PBF by the rule's own example (7.21, p. 55) and is refused; the Incremental IFT (7.37) is optional and not built | 47, 50, 51; ruling |
| A8 | Defensive Fire Principles | Mostly built | 23: 11 built, 1 deviation, 6 partly, 5 not built | Rules, Play, Units | First Fire by MF spent, the DEFENDER's window, Residual FP, Snap Shots, Subsequent First Fire, FPF with its NMC, Final Fire | **Mandatory TPBF against an entering unit (8.312) not built**; of 8.311 the target limit is enforced and FPF by Infantry manning ordnance is not; Snap Shots refused at wall, hedge, SMOKE, and rubble hexes and at a Bypass step; a vehicle fires once a Player Turn unless it keeps ROF; Residual FP is wrong in four ways (the cap applied before the reduction, a MG that keeps ROF, a malfunctioned MG, a CX firer: 8.2, 8.23, 8.221, 8.26) and ordnance leaves none (8.25) | 46, 47, 52 |
| A9 | Machine Guns and SW Malfunction | Partly | 23: 7 built, 10 partly, 6 not built | Rules, Play | MG usage by unit size, Multiple ROF, Sustained Fire, Spraying Fire, malfunction and MG repair | **MG fire against armor (9.6, 9.61) not built**; Mandatory Fire Direction and the 16-hex limit (9.4), Field of Fire (9.21), self-destruction (9.73), Random SW Destruction (9.74) not built; Fire Lanes simplified to a straight Hex Grain against entering Infantry (R12.7); dismantling for the German MMG only, though the catalog now holds a German HMG and three light mortars that 9.8 names; mortar and Gun repair not built; Sustained Fire has no X# (A.11); a captured SW may be repaired (9.72) | 33, 47, 50 |
| A10 | Morale | Mostly built | 25: 13 built, 10 partly, 2 not built | Rules, Play | MC and TC, LLMC and LLTC, breaking, rout with Low Crawl and Interdiction, Failure to Rout, DM, Rally, Self-Rally, Fate | Voluntary Break (10.41) and Voluntary Rout (10.711) not built; only Infantry Interdict and concealed units never do; Rally terrain bonus for woods and buildings only; Fanaticism by SSR not enforced; a rout moves at ground level only; a rout into a Location of concealed enemy units is accepted (10.533); no LLMC outside fire (10.2); a leader with a MG Interdicts **Pass 35 (2026-10-09):** Open Ground for a rout and for Interdiction is read by the FFMO a given enemy could apply (R35.1); a rout into concealed units is repulsed (R35.3); a unit bound to surrender owes no rout (R35.2); a Fanatic's Interdiction NMC is right; no voluntary rout without DM (R35.4). | 37, 45, 48 |
| A11 | Close Combat | Mostly built | 35: 19 built, 1 deviation, 12 partly, 3 not built | Rules, Play | Odds and Kill Numbers, Ambush, Melee, withdrawal, Infiltration, sequential CC, CC against vehicles, PAATC, capture of an unarmed vehicle | **Street Fighting (11.8) not built**; Infantry against Infantry in a Location holding a vehicle refused; CC with a Gun's crew refused; withdrawal refused when over IPC; Stealth for heroes only; no Ambush dr with a vehicle present (R11.16); odds above 10-1 are decided (R14.13), which this row first had as refused; a vehicle's sN (11.622) not built | 48, 51, 53, 56, 63; ruling |
| A12 | Concealment | Partly | 23: 4 built, 17 partly, 1 refused, 1 not built | Play, Rules, Units | "?" at setup, gain and loss, Dummies, HIP by SSR, detection by entry, Mopping Up, a vehicle's entry with PAATC | **Searching (12.152) not built**; hidden Fortifications and vehicles refused; hidden Guns only Emplaced in Concealment Terrain; the loss of "?" always forced (R10.10, R31d.2); movement's read of who sees differs from fire's (backlog 51); Right of Inspection is not limited by LOS (12.16); a hidden unit bars an enemy's "?" gain without being placed (12.32); no voluntary removal of "?" **Pass 35 (2026-10-09):** the enemy that forces a concealed mover's loss of "?" is Good Order, not hidden, and not a Passenger (R35.5). | 37, 48 |
| A13 | Cavalry | Not built | 19: 19 not built |  |  | No counter, no code, no backlog row | 43 |
| A14 | Snipers | Partly | 11: 2 built, 1 deviation, 6 partly, 2 not built | Play, Rules | The Sniper attack on Personnel from fire, MC, and TC DRs; a concealed stack as one target; night SAN | Sniper Check and SAN reduction (14.4), attacks on vehicles (14.33) and on the enemy Sniper (14.31), setup placement not built; the Location is chosen by the game, not the player (R15.5); no LLMC after a Sniper hits a leader; nothing makes a Sniper counter for a side with a SAN, which then makes no attacks | 49 |
| A15 | Heat of Battle | Mostly built | 16: 9 built, 1 deviation, 6 partly | Rules, Play | The table, heroes, Battle Hardening, Fanatic, berserk and its charge, surrender and No Quarter | DRM for the eight catalog nationalities only; a hero firing a LATW, light mortar, or Gun refused; a charge the model cannot decide ends in place (R27.2); no fall back to the next nearest enemy when a charge has no route (15.45); no return to normal after AFPh TPBF (15.46); Unarmed units and PRC not exempt (15.1) | 49 |
| A16 | Battlefield Integrity | Not built | 7: 7 not built | Play, Rules (card check only) | The card's printed total is validated | No Casualty Tally, Integrity Check, or ELR change. Optional by its own text (p. 84) | ruling |
| A17 | Wounds | Partly | 4: 2 built, 2 partly | Rules, Units, Play | Occurrence, severity, morale and leadership one worse, 3 MF | **The Wound Severity dr is skipped in three paths** (a rout's Interdiction, a Mopping Up casualty, a PF firer's): the SMC is wounded with no dr, and an already wounded one is eliminated with none, where 17.11 gives a dr with +1. Not in the backlog. A SMC wounded after spending more than 3 MF is not pinned; carrying a wounded man not built **Pass 35 (2026-10-09):** the Wound Severity dr is made in every path. | 45, 49 |
| A18 | Field Promotions | Mostly built | 3: 1 built, 2 partly | Rules, Play | Leader Creation on a Self-Rally Original 2 and a CC Original 2, with the table and its drm | No Leader Creation at all in CC against a vehicle (18.12), and so no drm for it | 49 |
| A19 | Unit Substitution | Mostly built | 14: 8 built, 1 deviation, 3 partly, 2 not built | Rules, Units, Play | ELR by side and OB group, Replacement, Disruption, Green and Conscript units and their penalties | Ammunition Shortage (19.131) and an SSR ELR for underscored units (19.132) not built; a MG keeps its printed B# for Inexperienced users (R9.10); **a Disrupted unit may rout and Low Crawl, against 19.12**; an Unarmed unit is not exempt from Replacement (19.11) **Pass 35 (2026-10-09):** a Disrupted unit routs only when it must, and never by Low Crawl. | 45, 49 |
| A20 | Prisoners | Mostly built | 16: 6 built, 1 deviation, 8 partly, 1 not built | Play, Rules, Units | Surrender in the RtPh, capture in CC, Mopping Up, Guards, No Quarter, Massacre, escape and rearming, double VP | Scrounging (20.552), recapture by entry (20.54), the exchange of prisoners (20.221) not built; fire from within the Location refused; one Guard for all (R24.2); Massacre for Russian or berserk Infantry only; the double VP for a prisoner its own side eliminates is announced and not counted; Commissars surrender in the RtPh; Italian and Axis Minor prisoners may attempt escape **Pass 35 (2026-10-09):** a Commissar does not surrender in the RtPh; a repulsed unit may (R35.2, R35.3). | 46, 49 |
| A21 | Captured Equipment | Partly | 7: 1 deviation, 4 partly, 2 not built | Play, Rules | Captured MG, FT, and DC penalties; a squad of the Gun's own nationality manning it | **Captured Guns, ordnance, and vehicles cannot be used**; no Temporary Driver or Crew; "captured" is read as "of another nationality", so an ally's SW counts too (R13.7); an Abandoned AFV cannot be captured (21.2); a captured light mortar or LATW is refused; a non-qualified crew takes only the +2 of its penalties (21.13) | 51, 54 |
| A22 | Flamethrowers and Molotov Cocktails | Mostly built, against Personnel | 17: 10 built, 2 deviation, 1 partly, 1 refused, 3 not built | Rules, Play | FT fire in full against Personnel; MOL by SSR with its Check | **A MOL against an AFV refused (22.612); a FT has no way to attack one (22.34)**; Flame and Kindling never placed (22.35, 22.613); a leader makes no MOL attack | 44, 49, 50 |
| A23 | Demolition Charges | Partly | 13: 5 built, 3 partly, 1 refused, 4 not built | Play, Rules | Placed and Thrown DC at the same level | **DC against an AFV refused** (23.5); Set DC (23.7) not built; Rubble, Flame, and Breach (23.41) never made; thrown only at the thrower's level | 42, 49, 50 |
| A24 | SMOKE | Partly | 13: 4 built, 3 partly, 6 not built | Play, Units | Infantry smoke grenades, their Hindrance and entry cost | **WP (24.3), ordnance SMOKE, SMOKE height and duration (24.4), Drift and Gusts (24.61, 24.62) not built**; grenades at other levels refused (R9.5); the Hindrance applies to a LOS at any height (24.4) | 44, 56 |
| A25 | Nationality Distinctions | Mostly not built | 79: 10 built, 9 partly, 60 not built | Rules, Play, catalog | Counters and class progressions for eight nationalities; the NKVD in full; Commissars but for the setup substitution and the PAATC; Russian no-Deployment; British Cowering immunity; Finnish ranks and Self-Rally; Axis Minor Heat of Battle | SS, Human Wave, Partisans, Paratroops, Gurkha, ANZAC, Allied Minors, Ethiopians not built; Italian Lax and Deploy limit, Italian and Axis Minor 1PAATC and no-escape not built; Finnish Stealth not built; no Japanese or Chinese; **a Russian 4-2-6 Battle Hardens to the NKVD 6-2-8, where 25.2 (p. 93) says 5-2-7**; Finns pay Captured Use on a Russian MG (25.75); Assault Engineers (25.01) not built **Pass 35 (2026-10-09):** a Russian 4-2-6 Battle Hardens to the 5-2-7. | 37, 45, 63, 64; ruling |
| A26 | Victory Conditions | Mostly built | 21: 13 built, 1 deviation, 2 partly, 5 not built | Play, Units, Rules | Control of buildings and of their hexes and Locations, where a card names them; VP, CVP, Exit VP; Avoidance; Balance | Control forfeited to Fire (26.16) not built; Bridge and Pillbox hex Control not built; a vehicle's PRC neither gains nor prevents Control (R24.5); Control of any other hex (26.13) not built | 37, 44, 49, 51, 59 |

## 4. Chapter B: Terrain

For each terrain the game has up to four things to do: read it in LOS, cost it in movement, give its TEM or Hindrance in fire, and play its special rules. "Refused" below means refused in movement and as a fire target (limit 1 of section 2). LOS is read from VASL's terrain data for nearly every type, so LOS alone is not named as "built" in the rows that are refused.

| § | Title | Status | Subsections | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|
| B.1 to B.10 | The chapter's introduction | Partly | 8: 1 built, 6 partly, 1 not built | Random Direction (B.8); COT, Hindrance Level, Inherent Terrain in part | Continuous Slope (B.5) has no code. SMOKE's MF is not doubled uphill (B.2). A Hindrance total of six from SMOKE or vehicles does not block (B.10) | 45, 60 |
| B1 | Open Ground | Partly | 10: 5 built, 2 partly, 3 refused | LOS, movement, TEM 0, FFMO | Interdiction and the must-rout test ignore Height Advantage, hexside TEM, and AFV or wreck cover (1.14, 1.16, 1.17) **Pass 35 (2026-10-09):** Interdiction and the must-rout test read Height Advantage, hexside TEM, and AFV or wreck cover, as fire does (R35.1). | 45, 59, 60 |
| B2 | Shellholes | Mostly not built | 4: 1 built, 2 refused, 1 not built |  | Movement and fire; creation (2.1) not built | 58, 60 |
| B3 | Roads | Mostly built | 8: 3 built, 1 deviation, 3 partly, 1 not built | Road rate, Road Bonus, vehicles' road costs, Mud on unpaved roads | The road rate is always taken (R10.1) | 37, 46, 60 |
| B4 | Sunken Road | Mostly not built | 7: 1 partly, 4 refused, 2 not built |  | Movement and fire; its LOS unconfirmed | 37, 41 |
| B5 | Elevated Road | Mostly not built | 8: 3 partly, 4 refused, 1 not built |  | Movement and fire; its LOS unconfirmed | 37, 60 |
| B6 | Bridges | Mostly not built | 17: 2 partly, 5 refused, 10 not built | Units on and beneath a bridge do not see each other; the bridge is a Hindrance at its own level only | Movement and fire refused; no bridge rule in the planner | 37, 59 |
| B7 | Runways | Mostly not built | 4: 1 built, 2 refused, 1 not built |  | Movement and fire | 37, 60 |
| B8 | Sewers and Tunnels | Not built | 14: 14 not built |  | No Sewer Movement; cards record the sewer provision as text | 61 |
| B9 | Walls and Hedges | Partly | 29: 2 built, 5 deviation, 7 partly, 6 refused, 9 not built | LOS; Infantry and vehicle crossing costs with Bog; wall and hedge TEM for Infantry fire; Wall Advantage | Wall Advantage is inferred from arrival order, not claimed (9.32, R10.6); **ordnance and vehicle fire refused at any hex with hexside terrain**; vertex LOS refused; Hull Down (9.36) not built; **Bocage (9.5) refused**; fire across a hillside wall refused; cactus hedge absent; the claim rules of Wall Advantage (9.322, 9.324) absent | 37, 52, 61 |
| B10 | Hills | Mostly built | 11: 6 built, 1 deviation, 3 partly, 1 not built | Levels and Blind Hexes in LOS; elevation costs for Infantry and vehicles; Height Advantage for Infantry fire | VASL's Slope hexsides refused under the name "Continuous Slopes", while B.5 itself has no code; a Double-Crest off a road refused for vehicles; one base level a hex, no Crest Line inside it (10.11); Interdiction ignores Height Advantage | 34, 60; ruling |
| B11 | Cliffs | Mostly not built | 13: 2 built, 4 refused, 7 not built | Blind Hexes in LOS | Crossing refused for all; Climbing (11.4) not built; fire at a hex with a cliff hexside refused | 60 |
| B12 | Brush | Partly | 6: 3 built, 3 not built | LOS Hindrance, costs, Hindrance in fire | Vineyard absent; Kindling and Deep Snow (12.5, 12.6) not built | 44, 57, 60 |
| B13 | Woods | Mostly not built | 17: 3 built, 3 partly, 11 not built | LOS, costs, vehicles' entry with Bog, TEM, Air Bursts | Trail Break (13.421) not built; paths, forest, and pine woods absent; woods-road hexes (13.31, 13.32) not built; Air Bursts for a light mortar against Infantry only | 37, 44, 46, 50, 51, 60 |
| B14 | Orchard | Partly | 9: 3 built, 2 partly, 4 not built | Infantry movement; TEM 0 | **Vehicles are refused entry, though B14.4 gives the Open Ground cost**: a fault, not a recorded deviation. A LOS through orchard leaves fire undecided. The orchard is always in season: from November to March a LOS from above is blocked where 14.2 gives +1 **Pass 35 (2026-10-09):** vehicles pay the Open Ground cost; the orchard's season in the LOS (14.2) moved to pass 135. | 44, 45, 60 |
| B15 | Grain | Partly | 5: 3 built, 1 partly, 1 not built | Season by scenario month in movement and fire | Refused when the game has no month; fire across out-of-season grain is refused too, where 15.6 makes it Open Ground **Pass 35 (2026-10-09):** out-of-season grain on a LOS is Open Ground; only a game that names no month leaves it undecided. | 43, 44, 45 |
| B16 | Marsh | Mostly not built | 15: 6 built, 1 refused, 8 not built | Infantry entry for the whole allotment; Hindrance at the same level; vehicles barred | Fire from a marsh hex refused (16.32); no Bog Check beside marsh (16.43); HE into it not halved (16.31); depth, mudflats, and weather (16.5 to 16.8) not built **Pass 35 (2026-10-09):** ordnance HE is halved into a marsh (R35.6), and a hex beside a marsh is a Bog hex. | 37, 41, 45, 55, 57 |
| B17 | Crag | Mostly not built | 4: 1 partly, 2 refused, 1 not built |  | Movement and fire; fire through it undecided | 37, 60 |
| B18 | Graveyard | Mostly not built | 6: 1 partly, 3 refused, 2 not built |  | The same | 60 |
| B19 | Gullies | Partly | 6: 4 partly, 2 refused | Depression rules in LOS | Movement and fire | 41 |
| B20 | Streams and Crest Status | Mostly not built | 24: 1 partly, 6 refused, 17 not built | Depression rules in LOS | Movement and fire; depth, fords, Crest Status not built | 37, 41 |
| B21 | Water Obstacles | Mostly not built | 13: 1 partly, 2 refused, 10 not built |  | Movement and fire | 37, 41 |
| B22 | Valley | Mostly built | 3: 1 built, 2 partly |  | No valley code, and none is needed: levels are signed. No test has a hex below level 0 outside a Depression | 60 |
| B23 | Buildings | Partly | 51: 11 built, 12 partly, 8 refused, 20 not built | Ordinary buildings: LOS by height, upper levels, stairwells, TEM, Infantry fire between levels | **Cellars and rooftops refused; Rowhouses refused in movement and fire; Factories refused; Fortified Buildings not built in live play**; a vehicle may not enter a building; a mortar may not fire from one; a mortar hit ignores the building's levels (23.32); a Factory by SSR plays as a stone building; the two Fortified Building cases live in a bounded package no game reaches; levels of one hex are not ADJACENT (23.25) **Pass 35 (2026-10-09):** Locations a stairwell joins are ADJACENT, with A.8 (R35.9). | 42, 44, 47, 50, 54, 60 |
| B24 | Rubble | Mostly not built | 18: 1 built, 3 partly, 14 not built | Printed rubble: LOS, costs, its building's TEM | **Rubble is never created** (24.11), does not fall (24.12), and is not cleared (24.7) | 37, 38, 42, 44, 49 |
| B25 | Fire | Mostly not built | 19: 1 built, 3 partly, 15 not built | A burning wreck's Blaze, its smoke, its entry cost; the Wind Change DR as Chapter E needs it | **No Kindling, spread, terrain Blaze, or EC**; the EC SSR is not enforced on any card; a wreck burned in Close Combat gets no Blaze (25.14) **Pass 35 (2026-10-09):** a wreck burned in Close Combat gets its Blaze. | 44, 45, 56 |
| B26 | Wire | Not built | 13: 13 not built |  | Counters placed and ignored (limit 6), but for one check: a forced-back mover is refused when a fortification is in its origin Location **Pass 35 (2026-10-09):** a game with a fortification counter is refused (R35.10); the backlog's section 55.1 lists each counter's rules. | 38 |
| B27 | Entrenchments | Mostly not built | 19: 1 partly, 18 not built | LOS reads foxholes and trenches printed on a map | Counters ignored; no Entrenching, capacity, or TEM | 37 |
| B28 | Minefields | Mostly not built | 25: 2 partly, 23 not built | A counter's strength is checked for plausibility | No attack, clearance, or hidden minefield | 38 |
| B29 | Roadblocks | Not built | 5: 5 not built |  | Counter ignored; printed roadblocks refused | 37 |
| B30 | Pillboxes | Not built | 23: 23 not built |  | Vocabulary only | 37 |
| B31 | Village Terrain | Mostly not built | 17: 1 partly, 16 not built |  |  | 42, 61 |
| B32 | Railroads | Mostly not built | 25: 1 partly, 2 refused, 22 not built | Embankment height in LOS | Movement and fire | 61 |
| B33 | Stream-Hex Terrain | Not built | 4: 4 not built |  |  | 41 |
| B34 | Towers | Mostly not built | 11: 1 partly, 3 refused, 7 not built |  | Movement and fire; LOS reads towers | 61 |
| B35 | Light Woods | Mostly not built | 3: 1 partly, 2 refused | LOS | Movement and fire | 60 |
| B36 | Prepared Fire Zone | Not built | 7: 7 not built |  |  | 61 |
| B37 | Debris | Not built | 14: 14 not built |  | A counter that exists only by SSR; there is no counter kind and no SSR for it | 61 |

## 5. Chapters C and D: Ordnance and vehicles

**Which fire reaches which target today.**

| Fire | Target | Status |
|---|---|---|
| A Gun's or a tank's HE | Infantry | Built, on the Infantry Target Type |
| A light mortar | Infantry | Built, on the Area Target Type; refused if the hex holds a vehicle or a manned Gun |
| Ordnance HE | A Gun | Built only when the Gun's crew is alone in its Location |
| Ordnance | An unarmored vehicle | Built |
| Ordnance (AP, APCR, HEAT, HE) | An AFV | Built: hull or turret, facing, armor, To Kill |
| Small arms and MGs | An unarmored vehicle | Built, on the IFT's vehicle line |
| Small arms and MGs | An AFV | The vehicle is unharmed; a CE crew takes a Collateral Attack. MG To Kill is not built |
| FT, MOL, DC | An AFV | Refused (MOL, DC) or impossible to declare (FT) |
| Any ordnance | A target on another level, or in a hex with a wall, hedge, or cliff hexside | Refused |
| PF, PSK, ATR | An AFV | Built |
| Bazooka, PIAT, MOL-P, ATMM | Anything | Not built: no counter, no code |
| Any fire | Passengers | Not built |

**What a tank can fire:** its main armament, in the PFPh, the DFPh, and the AFPh if it did not enter a new hex. Its MA does not fire as Bounding First Fire, in Motion, or as Intensive Fire. Its BMG and CMG do not fire at all outside an Overrun's FP and Close Combat.

| § | Title | Status | Subsections | Lives in | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|---|
| C.1 to C.9 | The chapter's introduction | Mostly built | 9: 5 built, 1 deviation, 2 partly, 1 not built | Play, Rules | To Hit and effects DRM kept apart (C.3), ordnance Area Fire (C.4), range and CA by hex (C.5A, C.5B), a moving vehicular target (C.8) | No vertex aiming point (C.5, R10.7); Heavy Payload (C.7) not built | 39, 52, 54 |
| C1 | Offboard Artillery | Not built | 51: 1 refused, 50 not built |  |  | Everything: radio, battery access, SR, FFE. A mortar's Area shot at a hex with a vehicle (1.55) is refused | 39, 50, 58 |
| C2 | Gun Classifications | Partly | 18: 8 built, 6 partly, 1 refused, 3 not built | Rules | Crew, caliber, type, facing, Multiple ROF, range, M#, target size, malfunction | **Gun Duels (2.2401) not built; a malfunctioned Gun or MA cannot be repaired**; IFE (2.29) not built; a 360-degree mount refused; every ordnance shot at another level refused (2.6); Prohibited Hexes (2.7) unchecked at setup; against an unarmored vehicle the default ammunition is AP, where 2.21 has HE | 33, 34, 53, 54 |
| C3 | The To Hit Process | Partly | 24: 7 built, 2 deviation, 10 partly, 2 refused, 3 not built | Rules, Play | Covered Arc; the Infantry Target Type where the Location holds no vehicle, the Area Target Type for light mortars, the Vehicle Target Type, Critical Hits, hull or turret, improbable hits | Area Target Type for light mortars only; Multiple Hits (3.8), Harassing Fire (3.75), WP (3.76) not built; Infantry and a vehicle in one Location must be attacked as the vehicle; the TH# color is red for every nation but the German; HE into marsh not halved (3.53); a non-turreted AFV is always hit in the hull (R7.4) **Pass 35 (2026-10-09):** HE is halved into a marsh (3.53). | 33, 45, 50, 53, 54, 56, 58 |
| C4 | Basic TH# Modifications | Mostly built | 8: 6 built, 1 partly, 1 not built | Rules | Barrel length, small calibers, APCR | APDS and SMOKE not built | 34, 56 |
| C5 | Firer-Based DRM | Partly | 25: 5 built, 8 partly, 7 refused, 5 not built | Rules | Cases A, B, C3, D, F (Guns), H (the non-qualified half), I | **Cases C, C1, C2, C4 refused: no moving firer**; Case G Deliberate Immobilization not built; Case E only for a Gun at Infantry in its own Location; OVR Prevention (5.64) not built; a vehicle's Intensive Fire refused; rubble is left out of the firer's terrain in Cases A, B, and E **Pass 35 (2026-10-09):** rubble counts as a firer's terrain in Cases A, B, C3, and E and for the CA lock; an Opportunity Firer takes no Case B or AFPh C3; the own-hex Hindrance of Case E is read. | 33, 45, 53, 54 |
| C6 | Target-Based DRM | Mostly built | 29: 17 built, 8 partly, 4 not built | Rules, Play | Cases J to J4, K, L, M (Bore Sighting), N (Acquisition), P, R | Case O not built; Case Q without hexside TEM or Hull Down; Gyrostabilizer (6.55) not built; Case J for vehicles only; the ATR denied Case L; Acquisition kept when 6.5 removes it and not following a vehicle target **Pass 35 (2026-10-09):** Case O is built; the ATR takes Case L; an Acquisition is lost by a move or a turn without firing, and follows a vehicle target. | 33, 45, 46, 47, 50, 52, 53, 54, 56 |
| C7 | To Kill Tables | Mostly built | 26: 16 built, 5 partly, 2 refused, 3 not built | Rules, Play | Basic TK# less armor; the AP, APCR, HEAT, and HE tables; Shock; Immobilization; burning wrecks | Case B and Underbelly hits, indirect HE, FT and MOL, mortars, and DC against vehicles not built; aerial hits not built | 34, 40, 50, 52, 53 |
| C8 | Special Ammunition | Partly | 18: 5 built, 2 partly, 1 refused, 10 not built | Rules, Play | Depletion Numbers; APCR; HEAT by date | **APDS, Canister, SMOKE, WP, limited stowage, Elite +1 not built**; HE Equivalency (8.31) and Illuminating Rounds (8.7) not built | 34, 50, 56, 58 |
| C9 | Mortars | Mostly built | 6: 3 built, 3 partly | Rules, Play | Light mortars as SW against Infantry: Area fire, Spotters, minimum range | Medium mortars have no counter; mortar SMOKE not built; firing from a building refused | 50, 54 |
| C10 | Gun and Ammo Movement | Mostly not built | 21: 1 built, 7 partly, 1 refused, 12 not built | Play, Rules | Towing, hooking up, unhooking, pushing with Manhandling, for QSU Guns | **Limbering (10.2) refused, so a non-QSU Gun cannot be moved**; En Portee (10.5) not built; a push only into Open Ground or grain; **a towing vehicle may Bypass and cross a hedge, against 10.1**; trailers (10.4) not built **Pass 35 (2026-10-09):** a towing vehicle is refused Bypass, a wall or hedge, and rubble. | 45, 54, 55 |
| C11 | Guns as Targets | Partly | 7: 6 partly, 1 refused | Rules, Play | Target size, Emplacement, gunshield, Direct Hit and Near Miss, destruction, for a direct HE hit on a crew alone | A crew sharing its Location is refused as a target; AP at a Gun and an unmanned Gun not built; small arms, a FT, a MOL, a DC, and an OVR never destroy a Gun (11.6) | 50 |
| C12 | Recoilless Rifles | Not built | 9: 9 not built |  |  | No counter, no code | 54 |
| C13 | Light Anti-Tank Weapons | Mostly not built | 47: 7 built, 1 deviation, 8 partly, 3 refused, 28 not built | Rules, Play | PF, PSK, and ATR against vehicles, with Backblast from buildings | **Bazooka, PIAT, MOL-P, ATMM not built**; a PF only at an AFV; Desperation fire refused; no Case C3 from rubble; the PSK at Infantry, the PFk, and SMC usage not built **Pass 35 (2026-10-09):** Backblast from rubble; no AFPh +2 for an Opportunity Firer's LATW. | 45, 50, 54; ruling |
| D.1 to D.8B | The chapter's introduction | Partly | 7: 2 built, 3 partly, 2 not built | Play, Rules | Prep Fire (D.3), Mobile and Immobile (D.7); Collateral Attacks for an Inherent crew (D.8B) | Specific Collateral Attacks (D.8A) not built: an ordnance hit that fails to destroy an AFV does nothing to its CE crew; animal transport (D.2) not built | 50, 51, 55 |
| D1 | Vehicle Counters | Mostly built | 40: 27 built, 8 partly, 5 not built | Rules, Play | The values the five vehicles use: movement type, armor, turret type, ground pressure, target size, PP | Armored cars, partial armor, NT fire, optional armament not built; Secondary Armament, rear MGs, the vehicle FT not built; a CE RST tank still uses its CMG (1.321) **Pass 35 (2026-10-09):** a CE RST AFV adds neither MA nor CMG to an OVR, and no CMG to CC. | 33, 45, 53; ruling |
| D2 | Vehicular Movement | Mostly built | 33: 16 built, 3 deviation, 7 partly, 2 refused, 5 not built | Play, Units | MP, VCA, Start and Stop, Reverse, VBM, Motion, ESB, Mechanical Reliability, an enemy AFV's hex | Motion attempts (2.401) not built; Reverse Motion refused; movement only over the short terrain list; Delay (2.17) and the Bypass TCA (2.321) not built; a tank that is BU by default pays the CE road rate (2.16); a Bypassing vehicle gets the obstacle's TEM (2.38) **Pass 35 (2026-10-09):** the BU road rate for an AFV that is BU by default; no beneficial TEM in Bypass on the Vehicle Target Type; the ESB table by nationality; Non-Stopped read from the move in hand (R35.9). | 33, 34, 45, 52, 53; ruling |
| D3 | AFV Combat | Partly | 21: 2 built, 10 partly, 9 not built | Play, Rules | VCA and TCA, Target Facing, the halftrack's AAMG with Bounding First Fire | **The MA cannot fire while moving; BMG and CMG do not fire; one fire marker for all of a tank's weapons, where 3.5 gives each its own; Armor Leaders (3.4) not built; MG against AFV (3.54) not built**; the turret turns only as part of a shot; BMG and CMG malfunctions have no repair | 33, 34, 53 |
| D4 | Terrain and Anti-Vehicle Fire | Mostly not built | 12: 1 partly, 1 refused, 10 not built | Rules | The target hex's TEM on a vehicle shot | **Hull Down (4.2) and Underbelly hits (4.3) not built** | 52 |
| D5 | Inherent Crew | Partly | 19: 3 built, 9 partly, 7 not built | Play, Rules | BU and CE, Stun, Recall, Abandonment by a failed TC, Crew Survival after ordnance | Voluntary Abandonment (5.4), entering an Abandoned vehicle (5.42), Brew Ups (5.7) not built; no Crew Survival after an IFT kill (after CC the rule gives none); the Stun +1 is missing in OVR and CC; Unprotected Crews (5.311) not built **Pass 35 (2026-10-09):** the Stun +1 on the OVR and CC DR; no CE change after Bounding First Fire; a bogged Recalled AFV is Abandoned, ESB is barred to it, and it may Stop to unload. | 45, 51 |
| D6 | Transporting Personnel | Mostly not built | 27: 1 built, 1 deviation, 7 partly, 18 not built | Play, Units | Capacity, loading, unloading, entry loaded | **Riders and Bailing Out (6.2), all Passenger fire and morale (6.6, 6.7), fire at Passengers, their Survival (6.9) not built**; Carriers (6.8) not built | 51 |
| D7 | Overruns | Partly | 16: 3 built, 8 partly, 1 refused, 4 not built | Play, Rules | The OVR and its FP, Reaction Fire, CC Reaction Fire | OVR of a Location with a vehicle in Motion or of units in Melee refused; Gun crews (7.23) not built; Street Fighting and FPF Reaction Fire (7.211, 7.212) not built; the CC Reaction counter is set and never read **Pass 35 (2026-10-09):** the CC counter of CC Reaction Fire bars Non-CC Reaction Fire. | 34, 45, 51, 53 |
| D8 | Immobilization and Bog | Partly | 11: 2 built, 6 partly, 3 not built | Play, Units | Immobilization, Bog Check and its DRM, Bog Removal, Mired | the Immobilization TC's second cause (8.22), help from another AFV (8.3), and the whole-MPh Bog DR of Mud and Deep Snow (8.23) not built; a bogged vehicle cannot unload (8.5) **Pass 35 (2026-10-09):** a bogged vehicle loads and unloads; no Case J for Bog Removal MP in the Bog hex. | 45, 51, 53, 57 |
| D9 | Vehicles as Cover | Mostly not built | 10: 4 partly, 6 not built | Play, Rules | +1 TEM and +1 Hindrance from an AFV or wreck | Armored Assault (9.31), Dug-In AFVs (9.5) not built; a Bypassing AFV or wreck always hinders (9.4) **Pass 35 (2026-10-09):** a Bypassing AFV or wreck hinders only a LOS that touches its hexside (R35.8). | 45, 51, 52, 53 |
| D10 | Wrecks | Partly | 8: 2 built, 1 partly, 5 not built | Play | Creation, movement cost, cover | Removal (10.4) and Scrounging (10.5) not built | 44, 51, 53 |
| D11 | Gyrostabilizers and Schuerzen | Not built | 9: 9 not built |  |  |  | 53; ruling |
| D12 | Horse-Drawn Transport | Not built | 5: 5 not built |  |  |  | 55 |
| D13 | Vehicular Smoke Dispensers | Not built | 8: 8 not built |  |  |  | 56 |
| D14 | Radioless AFV | Not built | 15: 15 not built |  |  | No Platoon Movement | 53 |
| D15 | Motorcycles and Bicycles | Not built | 26: 26 not built |  |  |  | 55 |
| D16 | DD Tanks and Amphibians | Not built | 13: 13 not built |  |  |  | 55 |
| D17 | Aerosans | Not built | 12: 12 not built | | | No counter, no code. In the PDF on pages 216 to 217; this document's first version had no row for it | 55 |

## 6. Chapters E, F, G, and H

E.1 (p. 222) makes every rule of Chapter E optional unless a card cites it or the players agree. The NVR Table (E1.11) and the Weather, Fog, and Snow Charts are for DYO play; a card that names the night or the weather by SSR follows the rule and departs from nothing.

| § | Title | Status | Subsections | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|
| E.1 to E.6 | The chapter's introduction | Mostly not built | 5: 1 built, 4 not built |  | Random Location DR (E.3) and Majority Squad Type (E.4) not built, though E1 and E3 need both; Aerial Range and LOS Hindrances (E.5, E.6) not built. E.1 makes all of Chapter E optional unless a card cites it | 34b, 40 |
| E1 | Night | Mostly not built | 73: 14 built, 1 deviation, 14 partly, 44 not built | NVR by SSR and its change, fire beyond NVR refused, the night LV DRM, Gunflashes, Starshells and Illumination, night movement costs, night rout and DM, "?" gained without a dr | **The Scenario Defender at night (1.2), Cloaking (1.4), Straying (1.53), Jitter Fire (1.55), Lax and Stealthy (1.6), IR (1.93), trip flares (1.95) not built**; ordnance at a Gunflash refused; the NVR is named by SSR, as the rule has it outside DYO (R16.1); Acquisition at night kept (R16.2, against 1.74); a Gunflash follows its firer, not the Location; no night MP in VBM (1.52) **Pass 35 (2026-10-09):** the night's MP in VBM and on the Recall exit, after the Reverse multiplier (R35.7); the unload from a BU AFV at an NVR of 0. | 34b, 37, 38, 39, 42, 44, 45, 54, 58; ruling |
| E2 | Interrogation | Not built | 11: 11 not built |  |  | 49 |
| E3 | Weather | Partly | 49: 17 built, 2 deviation, 11 partly, 5 refused, 14 not built | Mist, rain, falling and ground snow, Deep Snow, Mud, Extreme Winter, their movement and Bog costs, no SMOKE in rain or Mud | **EC, wind force and direction, Fog, Drifts, Ice, Winter Camouflage not built**; a Gust is rolled and nothing reads it; the weather is named by SSR, as the rule has it outside DYO (R16.9); weather costs are not charged in Bypass (3.9) **Pass 35 (2026-10-09):** the weather's costs in Infantry Bypass and VBM, after the Reverse multiplier (R35.7); an unpaved road in Mud for Infantry. | 37, 38, 40, 41, 45, 56, 57; ruling |
| E4 | Ski Troops | Not built | 16: 16 not built |  |  | 43 |
| E5 | Boats | Not built | 23: 23 not built |  |  | 62 |
| E6 | Swimming | Not built | 8: 8 not built |  |  | 62 |
| E7 | Air Support | Not built | 32: 32 not built |  |  | 40 |
| E8 | Gliders | Not built | 15: 15 not built |  |  | 62 |
| E9 | Paratroop Landings | Not built | 15: 15 not built |  |  | 62 |
| E10 | Ammo Vehicles | Not built | 10: 10 not built |  |  | 55 |
| E11 | Convoys | Not built | 31: 31 not built |  |  | 55 |
| E12 | Barrage | Not built | 21: 21 not built |  |  | 58 |
| F | North Africa (13 sections) | Not built | by chapter only | LOS reads slopes and hillocks as VASL does | Every desert terrain refused in play; Dust and Heat Haze not built | deferred (the user, 2026-10-05; not built by the ruling of 2026-09-30) |
| G | Pacific Theatre (18 sections) | Not built | by chapter only | LOS reads jungle, bamboo, palm trees, huts | No Japanese or Chinese counters or rules; caves, landing craft, beaches absent. Hand-to-Hand CC is built from an SSR, not from G1.64 | deferred (the same) |
| H | Design Your Own | Not built | by chapter only | The catalog carries BPV for 151 of 173 counters (none for vehicles, MGs, or other SW) | No purchase, roster, or ELR Chart | 34, 35, deferred (2026-10-02, and again 2026-10-05) |

## 7. Where each gap goes

The first version listed here what no planned pass covered. That list is replaced by the inventory: each of its 1514 rows that is not fully built carries the pass that takes it, or the ruling that leaves it out, and the plan's section 22.1 has the passes with their tasks. Thirty-one passes take 1484 rows: the eleven the plan already had, amended, and twenty new ones. Rulings leave out 30 rows: optional rules, DYO charts, rules that serve Chapters F and G, and deviations that stand.

## 8. What the reading of the PDF changed

### 8.1 The first version's errors

- **It counted 104 sections; there are 105.** D17 Aerosans (pp. 216 to 217) had no row. The 57 numbered rules of the chapters' introductions (A.1 to A.18, B.1 to B.10, C.1 to C.9, D.1 to D.8, E.1 to E.6) had none either, and some of them matter: see ADJACENT above.
- **It called built what is not.** B1 Open Ground (Interdiction), A18 Field Promotions (none in CC against a vehicle), "PF, PSK, ATR in full" (C13), "Commissars in full" (A25), "Mostly built" for B16, D5, D7, and D8.
- **It called refused what is decided, and refused what is absent.** Close Combat odds above 10-1 are decided (R14.13). Cactus hedge, vineyard, paths, forest, pine woods, and Debris are not refused: nothing in the game knows them.
- **It miscounted the Hindrances** (six are decided, not three) and **misnamed a refusal**: the "Continuous Slopes" the game refuses are VASL's Slope hexsides; Continuous Slope (B.5) has no code.
- **It took rule numbers from the transcription's headings.** D8.32 is the unhooking of a Gun, not towing out, and D8 ends at D8.5. D3.6 is the vehicular FT, not Canister. C9.5 is the mortar's Critical Hit. There is no A16.4 and no B13.422.
- **It read a departure where the rule agrees.** A night or weather named by SSR is the rule's ordinary case; the tables are DYO's. Crew Survival after Close Combat is not missing: D5.6 gives none.
- **It found two faults.** The audits list 143 (section 8.3).

**Statuses changed by the recount** (44 of the 104 rows it had). Not every change is an error found: some come from the rule of section 1 replacing a judgment made by eye, and a section that was "Refused" as a whole now shows the LOS it has as partly built rows.

| § | Was | Is | Subsections |
|---|---|---|---|
| A2 | Mostly built | Partly | 15: 2 built, 3 deviation, 5 partly, 5 not built |
| A3 | Built | Mostly built | 10: 8 built, 2 partly |
| A12 | Mostly built | Partly | 23: 4 built, 17 partly, 1 refused, 1 not built |
| A17 | Mostly built | Partly | 4: 1 built, 3 partly |
| A18 | Built | Mostly built | 3: 1 built, 2 partly |
| A23 | Mostly built, against Personnel | Partly | 13: 5 built, 3 partly, 1 refused, 4 not built |
| A25 | Partly | Mostly not built | 79: 10 built, 9 partly, 60 not built |
| B1 | Built | Partly | 10: 4 built, 3 partly, 3 refused |
| B2 | Refused | Mostly not built | 4: 1 built, 2 refused, 1 not built |
| B4 | Refused | Mostly not built | 7: 1 partly, 4 refused, 2 not built |
| B5 | Refused | Mostly not built | 8: 3 partly, 4 refused, 1 not built |
| B6 | Not built | Mostly not built | 17: 2 partly, 5 refused, 10 not built |
| B7 | Refused | Mostly not built | 4: 1 built, 2 refused, 1 not built |
| B9 | Mostly built | Partly | 29: 2 built, 5 deviation, 7 partly, 6 refused, 9 not built |
| B11 | Refused | Mostly not built | 13: 2 built, 4 refused, 7 not built |
| B12 | Built | Partly | 6: 3 built, 3 not built |
| B13 | Mostly built | Mostly not built | 17: 3 built, 3 partly, 11 not built |
| B15 | Built | Partly | 5: 2 built, 2 partly, 1 not built |
| B16 | Mostly built | Mostly not built | 15: 4 built, 1 refused, 10 not built |
| B17 | Refused | Mostly not built | 4: 1 partly, 2 refused, 1 not built |
| B18 | Refused | Mostly not built | 6: 1 partly, 3 refused, 2 not built |
| B19 | Refused | Partly | 6: 4 partly, 2 refused |
| B20 | Refused | Mostly not built | 24: 1 partly, 6 refused, 17 not built |
| B21 | Refused | Mostly not built | 13: 1 partly, 2 refused, 10 not built |
| B22 | Not built | Mostly built | 3: 1 built, 2 partly |
| B24 | Partly | Mostly not built | 18: 1 built, 3 partly, 14 not built |
| B27 | Not built | Mostly not built | 19: 1 partly, 18 not built |
| B28 | Not built | Mostly not built | 25: 2 partly, 23 not built |
| B31 | Not built | Mostly not built | 17: 1 partly, 16 not built |
| B32 | Refused | Mostly not built | 25: 1 partly, 2 refused, 22 not built |
| B34 | Refused | Mostly not built | 11: 1 partly, 3 refused, 7 not built |
| B35 | Refused | Mostly not built | 3: 1 partly, 2 refused |
| B37 | Refused | Not built | 14: 14 not built |
| C2 | Mostly built | Partly | 18: 8 built, 6 partly, 1 refused, 3 not built |
| C3 | Mostly built | Partly | 24: 7 built, 2 deviation, 10 partly, 2 refused, 3 not built |
| C10 | Partly | Mostly not built | 21: 1 built, 7 partly, 1 refused, 12 not built |
| C13 | Partly | Mostly not built | 47: 7 built, 1 deviation, 8 partly, 3 refused, 28 not built |
| D1 | Partly | Mostly built | 40: 27 built, 8 partly, 5 not built |
| D5 | Mostly built | Partly | 19: 3 built, 9 partly, 7 not built |
| D6 | Partly | Mostly not built | 27: 1 built, 1 deviation, 7 partly, 18 not built |
| D7 | Mostly built | Partly | 16: 3 built, 8 partly, 1 refused, 4 not built |
| D8 | Mostly built | Partly | 11: 1 built, 7 partly, 3 not built |
| D9 | Partly | Mostly not built | 10: 4 partly, 6 not built |
| E1 | Partly | Mostly not built | 73: 13 built, 1 deviation, 15 partly, 44 not built |

### 8.2 The subsections it left unconfirmed

Each is settled in the inventory. "By" is as there: the audit's judgment unless marked mine.

| § | Page | Settled as | How |
|---|---:|---|---|
| A2.3 | 45 | partly | Edge half-hexes are playable, and a seam hex takes the non-Open Ground terrain. The limit on setting up, entering, or counting occupation in a half-hex butted against a board where the unit is not allowed has no logic: a seam hex simply belongs to the later board. |
| A5.4 | 53 | partly | Holds by construction: vehicles, SW, and Guns are never counted against Personnel limits. The limit of three Guns in use follows from A5.5's crew counting as a squad, which is applied at setup only. |
| A5.6 | 53 | not built | No pillbox, entrenchment, sewer, or tunnel exists in play, so no capacity for them. |
| A6.5 | 54 | built | Holds by construction: one geometric walk serves both directions. No test asserts it, and one case is asymmetric on purpose (a tunnel Location in the same hex, kept as VASL has it). |
| A7.54 | 57 | not built | Nothing bars a berserk unit from a multi-Location fire group: the firer facts carry no berserk state and FromState does not check it. Tiny: one refusal. |
| A7.211 | 55 | partly | Infantry may fire TPBF at the CE crew of an AFV in their own Location, with the +2 CE DRM. Missing: fire from a higher Location of the hex, Passengers and OT crews that are not CE as targets (Passengers are never attacked, R26.2), the CC counter and Melee wording. |
| A7.33 | 56 | built | Inherent FP is never split; a squad fires its SW apart from it in the same phase (ruling R12.4). The exception for A7.34 is not built. |
| A7.371 | 56 | not built | Part of the optional IIFT: column shifts fall to standard columns. Nothing to shift on, since only standard columns exist. |
| A8.14 | 59 | built | A broken or pinned mover is attacked again as it now is; the same firer again only when the MF spent allow it (ruling R31.1); a broken mover keeps FFNAM and FFMO. |
| A8.25 | 60 | not built | An ordnance hit resolves with no fire kind, so Residual returns none for it (ruling R9.3, backlog section 1 row 19). The code and the backlog agree; the ordnance term inside Residual is unreachable. |
| A8.221 | 60 | not built | Residual counts the whole column even when a MG of the attack malfunctioned on that DR; the rule gives no Residual FP for the malfunctioning weapon. Ammunition Shortage is absent. Tiny: leave the malfunctioned weapon's FP out of Residual. |
| A8.23 | 60 | not built | No choice between keeping Multiple ROF and leaving Residual FP: a MG that keeps its ROF still adds its FP to the Residual FP placed and fires again. Needs the DEFENDER's choice after the attack and Residual from the other firers' FP alone. |
| A8.24 | 60 | not built | Spraying Fire is refused in the MPh (ruling R12.6), so it never leaves Residual FP in two Locations. |
| A8.41 | 61 | built | An unmarked MG keeps firing in Final Fire at any target; a First-Fire-marked MG fires once more as Sustained Fire. Suspected fault: the adjacent-only limit reads the unit's marker, so a First-Fire-marked MG held by an unmarked squad may Sustained Fire at any range. |
| A9.51 | 64 | built | A spray reaches vehicles through the same path as other IFT fire: no effect on an AFV, the Vehicle line for an unarmored one, Collateral on a CE crew. No test of a spray at a vehicle was found. |
| A11.41 | 73 | built | Ruling R14.9: before the first round or once the CC is over, not pinned, berserk or Disrupted. It shares A11.21's destination limits. |
| A11.612 | 75 | built | Nothing in CC reads an AF; the Kill Number is the CCV alone. |
| A11.622 | 76 | not built | No sN capability in the catalog or the code. Needs D13.3 usage numbers and a German AFV that carries it. |
| A12.151 | 78 | partly | Bypass of a hex holding a concealed unit causes no detection, as the rule says. Missing: the loss of all "?" when a Bypassing unit ends its MPh there, the TPBF and -2 that follow. Refused: occupying an obstacle that holds enemy units (R10.7). |
| A12.42 | 80 | partly | Built: a vehicle in Bypass makes no OVR and forces no reveal or PAATC. Missing: the reveal of the concealed units when the vehicle or its Passengers end the MPh in that Location. |
| A15.45 | 84 | partly | A charge routes only over entries the movement model allows, so it never crosses a cliff or water. Missing: the fall back to the next nearest Known enemy unit (the charge ends in place instead, R30.5), the A15.44 fall back when there is none, any Blaze bar (no Blaze terrain), and minefields, FFE, and Wire. |
| A19.12 | 86 | partly | Built: Disruption when no lesser unit exists, never for Fanatic or a Commissar, no Self-Rally, surrender in the RtPh when ADJACENT to captors and when enemy units advance in, elimination in Melee, rally removes it. Missing: a Disrupted unit does not stay put: MayRout and PlanRout let it rout and Low Crawl like any broken unit when no captor is ADJACENT; surrender in other phases (backlog R14.11); the SS against Russians and PRC exceptions. |
| A21.2 | 88 | partly | Ruling R11.16. Built: an unarmed vehicle not in Motion, alone with enemy Infantry, is captured as the CCPh begins (the rule says at its end). Missing: capture of an Abandoned AFV, automatic or by a CC capture attempt, with its -1 DRM and Personnel Escort; a captured vehicle is never used. |
| A25.01 | 93 | not built | The catalog records the trait asl:assault-engineer (false on every counter) and no code reads it; no Assault Engineer counter exists. Designation is DYO (H1.22, deferred). Missing: AE counters and the loss of AE capability on Replacement. |
| A26.21 | 100 | built | A unit's VP follow its present counter and state: a created or Replaced leader, a malfunctioned or disabled MA. |
| C6.44 | 174 | not built | No MG or IFE Bore Sighting: the minus 2 on the IFT DR does not exist (settles the coverage document's unconfirmed entry). |
| C6.52 | 174 | partly | The Infantry and Vehicle Target Types share the Location's Acquisition, so that transfer falls out; no test names it. Transfers to and from the Area Target Type are not built (only light mortars use it, and they may not transfer). |
| C6.53 | 175 | built | Gained on each consecutive TH attempt and kept across fire phases (settles the unconfirmed entry). |
| C6.58 | 175 | not applicable | Counter lettering and colors: table procedure the program replaces by recording each Acquisition under its Gun's id. |
| C11.1 | 181 | not applicable | Definition of Near Miss and Direct Hit; the mechanics are C11.4. |
| C11.51 | 182 | partly | Built: no gunshield against a FT attack, and a FG faces the gunshield when any firer is within the CA. Missing: HEAT at a Gun (refused, no HE Equivalency), Gun destruction by FT or MOL through A9.74, MOL in a FG. |
| C13.26 | 183 | built | B11 malfunction (one lower for Inexperienced), repair number 2 in the catalog read by the general SW repair. No test of an ATR repair was found. |
| C13.9 | 185 | refused | A SCW's HEAT at Personnel (C8.31 HE Equivalency) is not built: a PF or PSK shot with no vehicle target is refused. The rest of the rule is a definition. |
| D2.18 | 196 | not built | The enter step always charges the computed cost; no argument declares a higher expenditure (the action's mp field is ESB only). |
| D2.321 | 197 | not built | A tank in Bypass fires its MA with its ordinary hexspine TCA and hexspine-counted Case A; nothing refuses it. See findings, Faults. |
| D2.38 | 198 | partly | Small arms and Residual FP give a vehicle no TEM anyway. Ordnance on the Vehicle Target Type gives a Bypassing vehicle the obstacle hex's Case Q TEM, against the rule and R11.2. See findings, Faults. |
| D7.14 | 207 | built | A vehicle with MP left may OVR again on a later entry; each vehicle attacks alone and once per entry. The call path is clear but no test makes a second OVR. |
| D7.221 | 208 | not built | No DEFENDER marked Final Fire is made to attack an OVRing vehicle, and no attack DR doubles as its NMC here (backlog section 21, ruling R11.13). |
| D8.4 | 209 | partly | A vehicle that entered a hex or moved in Motion stays a moving target for the Player Turn, bogged or not. The second clause is not built: a Defensive First Fire shot at a vehicle spending Bog Removal MP in its Bog hex takes a Case J DRM from MpInLos. |
| D8.5 | 209 | partly | An immobilized vehicle unloads and fires as before. Missing: a bogged vehicle is refused every expenditure but Bog Removal, so it cannot unload; the bow MG limits after Immobility are not built (a BMG fires only in an OVR). |
| B4 and B5 LOS | 114 | not settled | No Sunken Road or Elevated Road rule in the LOS code: they take VASL's generic Depression and level 1 readings. No fixture has board 13 or 14, and no oracle run was made. |
| B16 FFMO in marsh | 130 | built | FFMO is given only on Open Ground; a test asserts none in marsh. |
| B22 negative levels in LOS | 135 | partly settled | Levels are signed and gullies at level -1 are tested; no fixture has a valley, so the hill rules below level 0 are unverified. |
| B23.9 in the bounded packages | 140 | partly | B23.922 and B23.9221 are two cases of the bounded Occupied package, fed by supplied facts and reached by tests. The live planner never sets them, so no game can meet a Fortified Building. |
| Japanese units in Heat of Battle | 83 | refused | Any nationality outside the catalog's eight is refused as unreviewed; the catalog has no Japanese counter. |
| A tank's Defensive First Fire with its MA | 167 | built | The code admits it and ruling R8.1 allows it; only the Ordnance package's exclusion list text is stale. No Play test was found with a tank firing in the MPh. |

### 8.3 Faults the audits found

143 entries, as the audits wrote them, by group of sections. A fault here is code that gives a result the rule forbids, or omits part of a built rule in silence, with no ruling that records it. Some entries are one fault seen from two sections (Interdiction, the wound procedure, rubble as a firer's terrain); a few repeat what a ruling or the backlog already records, and say so; some are marked by their audit as read from the code and not settled without a run. I checked these in the code myself: the wound procedure (A17.11), Disrupted units (A19.12), ADJACENT (A.8), the Russian hardening (A25.2), orchards and vehicles (B14.4), Interdiction's Open Ground (B1.14, B1.16), and the BU read (D2.16). The plan's pass 35 takes the ones that give wrong results in play today; the others are repaired by the pass that reworks their rule. **Pass 35 (2026-10-09)** repaired 51 of them and made two refusals of two more; each is marked where it stands, and the entry's text is kept as the audit wrote it. The pass's design, sections 7 to 13, says what each repair is.

**A.1 to A.18, A1 to A6**

- **Repaired in pass 35 (2026-10-09).** **A.8 ADJACENT too narrow.** `GamePlanner.Map.cs`, `IsAdjacent`: requires the same absolute level and no hexside terrain on the shared hexside. The rule asks only for a LOS and that Infantry could advance between the Locations, so a hex one level up a hill, a hex across a wall or hedge, and the next level of a stairwell hex are ADJACENT by the rule and not by the code. It is read by rout (`GamePlanner.Rout.cs`, lines 49 and 141), surrender (`GamePlanner.CloseCombat.cs`, line 1288), DC placement and throwing (`GamePlanner.DemolitionCharges.cs`), and multi-Location fire groups (`GamePlanner.Fire.cs`, line 1643).
- **Repaired in pass 35 (2026-10-09).** **A.18 Morale ceiling.** `ScenarioA1FireCalculator.cs`, `Morale`: a berserk Fanatic unit returns 11 (the comment names ruling R30.3, which then contradicts A.18), and a Fanatic unit is never capped unless it is a hero or heroic leader. `ScenarioA1CloseCombatCalculator.cs`, line 564, does the same for Leader Creation.
- **A4.132 road entry.** `GamePlanner.Terrain.cs`, `GroundStep` charges the road rate; `GamePlanner.Fire.cs`, `FireMapFacts` then takes `TerrainKey` of the hex center. A unit that enters a woods or building hex along its road for one MF is fired on with the obstacle's TEM and no FFMO, where the rule gives FFMO and no TEM (the p. 48 example of 5I4). R10.1 records "always the road rate" but not this effect. I did not confirm which board hexes have a road hexside and an obstacle center in the VASL data.
- **A4.61 Assault Movement and in-Location MF.** `GamePlanner.Movement.cs`, `PlanMove`, line 309: once a stack has made any step, a step with or after Assault Movement is refused. A SMOKE grenade or DC attempt is a step in the stack's own Location, so "SMOKE, then Assault Move" is refused, against the rule's exception. Read from the code; no test of the sequence was found.
- **A4.72 counts SMOKE.** `GamePlanner.CloseCombat.cs`, `PlanAdvanceUnits` passes `entry.HalfMf`, which includes SMOKE's one MF (`BlazeEntryHalfMf`), to `DifficultAdvance`. The rule says "MF cost (excluding SMOKE)".
- **A5.11 in the MPh.** `PlanMove` has no stacking test: a MPh entry that overstacks a Location costs no extra MF and is not refused.
- **A4.41.** A MMG, HMG, or pushed Gun that moved in the MPh may fire in the AFPh (only `asl:light-mortar` is tracked in `MovedWeapons`).
- **A4.4 portage cost recouped.** `PlanDrop` allows a drop at any point of the move and `MfAllotment` reads only what the unit holds now, so dropping a SW mid-move gives its MF back. The rule says the unit "may not recoup the portage cost".
- **Repaired in pass 35 (2026-10-09).** **A.7 Good Order.** `GamePlanner.SupportWeapons.cs`, `GoodOrder`: a TI unit is not Good Order and a berserk unit is. A berserk squad or leader may therefore Deploy, Recombine, Transfer, or Recover, and a TI enemy unit does not strip a firer's "?" in `WithSeen`.
- **A4.11 conveyance exception.** `GamePlanner.Passengers.cs`, `PlanLoad` and `PlanUnload` use the full allotment: a SMC keeps six MF, and A4.5's bar on Double Time for a unit that boards or unloads is not applied.
- **R11.6 against the code.** The ruling says a second vehicle in a Location adds +1 to its side's attacks from there (A5.12); no code counts vehicles.

**A7 to A9**

- **Cowering marks nothing (A7.9, p. 58).** `ScenarioA1FireCalculator.Resolution.Arithmetic` shifts the column, but `FireCounter` and `WeaponEffects` ignore `cowered`. The rule marks a cowering unit and all its SW with a Final or Prep Fire counter. So a unit that Cowers in Defensive First Fire stays First Fire (it may Subsequent First Fire), and its MG keeps Multiple ROF on a low colored dr (Doubles of 1, 2, 3). In a group, no Random Selection picks the marked unit.
- **A MG that keeps ROF still leaves Residual FP (A8.23, p. 60).** `Residual` uses the whole column. The rule makes the DEFENDER choose: keep the ROF, or leave Residual FP with that weapon.
- **A malfunctioning MG still leaves Residual FP (A8.221, p. 60).** Same member: no test of `WeaponEffects` in `Residual`.
- **CX does not lower Residual FP (A8.26, p. 60; the A9.222 example on p. 64 says a CX firer's +1 reduces it).** `Residual` subtracts Hindrance, positive leadership, and hexside TEM only. An Encircled firer's +1, a hero's two-man +1, and a vehicle's Stun +1 are also outside the target hex and are not subtracted.
- **Residual FP cap applied too early (A8.2, p. 60: "up to a maximum of 12 after adjustment as per 8.26").** `Residual` caps at 12 and then reduces, so a 36 FP attack with a +1 outside DRM leaves 8 where the rule leaves 12.
- **Sustained Fire has no X# (A9.3 p. 63, A.11 p. 43, the A9.71 example p. 65).** `WeaponEffects` lowers the B# by two but an Original DR at the weapon's Original B# only malfunctions it; the rule removes it. The same applies to a captured MG.
- **A lone SMC may use Sustained Fire (A9.3 p. 63, A8.312 EXC p. 61).** Nothing in `Outside` or `PlanFire` bars a leader or hero with a First-Fire-marked MG from Subsequent First Fire, FPF, or Final Fire with it. Not found by search; judged from reading `Outside`.
- **Final Fire range for a First-Fire-marked MG (A8.41, p. 61).** `Outside` tests `IsFinalFireAgain` on the firer's marker. A squad that fired its MG alone in First Fire is not marked (`Marked` leaves it out), so in the DFPh the squad may fire that MG as Sustained Fire at any range. Suspected; a test would settle it.
- **Casualty Reduction of a crew (A7.302, p. 55).** `Reduce` has branches for a SMC, a HS, and a squad. A Gun crew goes to `HalfSquadOf`, gets none, and the resolution ends as `reduction-counter-missing` after the dice are rolled, which `AddFireEvents` throws on. Reached by a K/# result, a Casualty MC, or a failed MC by a broken crew. No test covers it.
- **A prisoner under a KIA (A7.301, p. 55).** `Apply` breaks every survivor that is not broken, heroic, or berserk; an unarmed unit should suffer Casualty Reduction, and a prisoner is never broken (A20.54).
- **IFT row clamp (A7.3).** `ScenarioA1FireReference.Result` clamps the Final DR to 0..15, so a Final DR of 16 or more on the 36 column returns the DR 15 cell (PTC) where the IFT has no row.
- **Berserk firers Cower (A7.9, p. 58).** The exemption list in `Arithmetic` has SMC, Fanatic, British, Finns; berserk is absent although berserk fire is allowed (R12.10). The Inexperienced double shift reads the unit's class (green, conscript), not the Inexperience fact, so a Green unit stacked with a non-directing leader is shifted twice.
- **A leader cannot direct a MG's ROF shots (A7.53, p. 57).** See section 1.
- **A berserk unit in a multi-Location fire group (A7.54, p. 57).** Allowed silently.
- **Residual FP attacks a vehicle at each MP expenditure (A8.22, p. 60).** `GamePlanner.Vehicles.VehicleStepPlan` attacks whenever the Location holds Residual FP; the state keeps no record of who was attacked. The rule allows one attack per Location unless the FP grew or the DRM worsened. R6.6 says a vehicle is attacked "as Infantry are" and does not record this.
- **A captured SW may be repaired (A9.72, p. 65).** `GamePlanner.Rally.PlanRepair` has no captured test, though `LiveFire.CapturedBy` exists.
- **Spraying Fire with a squad's inherent FP (A9.52, p. 64).** Recorded in R12.6 as a reading of the A9.5 example, but that example's squad (a 5-4-8) has Spraying Fire capability. Any squad holding a MG may add its inherent FP to a spray, at any range the squad reaches.
- **Dismantling (A9.8).** Not a wrong result, but the refusal text "only the German MMG is" misstates the rule for the German HMG and the mortars now in the catalog.

**A10 to A12**

- **Repaired in pass 35 (2026-10-09).** **A rout into concealed or hidden enemy units is accepted (A10.533, A12.15).** `GamePlanner.Rout.cs: RoutStepBar` and `PlanRout` read Known enemy units only, and `GameProjector.cs: Rout` checks no occupancy. A broken unit may step into, and end in, a Location that holds only concealed or hidden enemy units: no reveal, no repulse, no elimination.
- **Repaired in pass 35 (2026-10-09).** **Interdiction ignores Fanaticism (A10.8, A10.53).** `GamePlanner.Rout.cs: BrokenMorale` does not add the +1; Fire, Rally and PAATC do.
- **A hidden unit bars an enemy's "?" gain without showing itself (A12.32, A12.121).** `GamePlanner.FireExtensions.cs: ConcealmentGains` counts every unbroken, uncaptured enemy with a Location, hidden ones included. A12.32 makes the hidden unit give up its hidden status to do this. A unit that fails to gain "?" with no enemy in sight tells its owner a hidden unit sees it. Abandoned vehicles are counted too, against A12.1.
- **A withdrawal's refusal tells of a hidden unit (A11.21, A11.41).** `GamePlanner.CloseCombat.cs: WithdrawalDestinations` leaves out any Location with an enemy unit, hidden ones included, and the destinations are offered to the page.
- **Repaired in pass 35 (2026-10-09).** **A leader with a MG Interdicts (A10.532).** `NormalRange` gives a leader the range of a SW he possesses; A10.532 bars units whose FP is halved, and the page 70 example says a lone SMC manning the HMG "would not be able to Interdict".
- **Open Ground read two ways.** The loss of "?" on a move or an advance reads `terrain == "open-ground"` (`GamePlanner.Movement.cs: Unmask`, `GamePlanner.CloseCombat.cs` line 264); the rout reads `OpenGround`, which adds grain out of season and SMOKE. A10.531 is one definition for both.
- **Concealment dr without Lax and Stealthy (A12.122).** `ConcealmentGains` omits both drm with no note in the reasons.
- **Finns are never Stealthy (A11.17).** The catalog holds Finnish units; `AmbushDrm` gives the -1 to heroes and heroic leaders only.
- **Assault Engineers get no CCV +1 (A11.5).** The catalog records the trait `asl:assault-engineer`; nothing in Play or Rules reads it.
- **Repaired in pass 35 (2026-10-09).** **Recorded already, still true:** a leader's Casualty Reduction in a rout (and in the Mopping Up Casualty dr, which uses the same helper) has no Wound Severity dr (`GamePlanner.Rout.cs: CasualtyReduction`); movement's read of who sees a concealed mover (backlog section 51).

**A13 to A19**

- **Repaired in pass 35 (2026-10-09).** **Wound Severity dr skipped, and a wounded SMC eliminated with no dr** (A17.11): `GamePlanner.Rout.cs: CasualtyReduction` (used by the rout Interdiction and by `GamePlanner.MoppingUp.cs`), and `GamePlanner.Ordnance.cs: FirerEffectEvents`. Not a recorded deviation. The summary comments there state the wrong rule ("a SMC is wounded, or eliminated if already wounded").
- **Repaired in pass 35 (2026-10-09).** **A Disrupted unit may rout and Low Crawl** (A19.12): `GamePlanner.Rout.cs: MayRout, PlanRout`. Not recorded.
- **Unarmed units take Heat of Battle and ELR Replacement** (A15.1, A19.11): `ScenarioA1HeatOfBattle.Subject` and `ScenarioA1FireCalculator.cs: ElrImmune, Round.Replace` never read the Unarmed condition; `LiveFire.cs` does not pass it. Prisoners are handled apart (the `GuardId` branch); a freed or Scrounging Unarmed unit is not. Judged from the code; I found no test either way.
- **No Leader Creation in CC against a vehicle** (A18.12): `ScenarioA1VehicleCloseCombat.cs`. Not recorded.
- **A SAN with no Sniper counter is silent** (A14.01): `GamePlanner.Snipers.cs: AddSniperAttacks` skips the trigger when no active `asl:sniper` entity of that side exists; nothing warns at setup.
- **Stale comment**: `ScenarioA1HeatOfBattle.Subject` says crews are not in the catalog; the catalog has two crew definitions. The logic is still right (a crew is not `IsMmc`).
- Recorded, not faults: Interior Building Locations as Sniper targets, no LLMC after a Sniper hit, a MG's B# for Inexperienced users (R9.10), the charge ending in place (R27.2, R30.5).

**A20 to A24, A26**

- **Double VP for a prisoner eliminated by its own side is said and not counted (A20.54, A26.222 EXC).** `GamePlanner.Consequences.cs` (the `play.fire-own-units` text) tells the players that a prisoner its own side's fire eliminates "counts double for the Victory Conditions". `ScenarioVictory.cs: Cvp` gives every eliminated unit `vp(unit)` once; nothing records who eliminated a prisoner.
- **Repaired in pass 35 (2026-10-09).** **Commissars surrender in the RtPh (A20.21).** `GamePlanner.Rout.cs: PlanRout` and `FailureToRout` exempt only Fanatic units and a side under No Quarter. A20.21 lists Commissars (A25.22) among those that never surrender by the RtPh method, and the catalog has Commissars. No exemption was found in Play (searched for "commissar" in the rout and CC planners). Not confirmed by a run.
- **Escape by Italian and Axis Minor prisoners (A20.55).** The rule says they will not attempt escape unless abandoned. The CC package admits any prisoner to the escape round; no nationality check was found. No ruling records it.
- **A non-qualified crew's penalties (A21.13).** A squad or HS manning its own side's Gun gets Case H +2 only (`ScenarioA1OrdnanceCalculator.cs: PassEightFirerDrm`). A21.13 applies the A21.11 and A21.12 penalties: B# two lower, ROF one lower, red To Hit Numbers. Ruling R8.8 names only the +2. Found by search for `NonQualified`; not confirmed by a run.
- **SMOKE Hindrance at any height (A24.4).** `GamePlanner.Wrecks.cs: VehicleHindrance` adds the smoke DRM whenever the LOS crosses the hex. A LOS passing more than two levels above grenade smoke should take none. Not recorded as a deviation.
- **The weather exception for SMOKE in a building (A24.6).** `GamePlanner.Smoke.cs: PlanSmoke` allows placement in rain, Mud, or Deep Snow whenever the target is a building hex; the rule requires the placer to be in the same Location or an ADJACENT Location of the same building.
- **Capture of an unarmed vehicle at the start of the CCPh (A21.2).** The rule captures at the end of a CCPh. Recorded in R11.16 as a referee finding, so a known reading, listed here because the timing differs from the page.

**A25**

- **Repaired in pass 35 (2026-10-09).** **Russian Conscript Battle Hardening (A25.2, p. 93).** `ScenarioA1FireReference.cs`, `Hardened`: `defender-conscript-squad` (4-2-6) becomes `defender-nkvd-squad` (6-2-8) and the 2-2-6 HS becomes the NKVD 3-2-8. The rule says "A 4-2-6 squad Battle Hardens to a 5-2-7", and the figure shows the same. `ScenarioA1FireExtensionTests` (near line 423 and 454) asserts the NKVD result. I found no ruling that records this as a deviation; the code comment reasons from class order.
- **Italian non-elite squads may Deploy (A25.61, p. 97).** `ScenarioSetup.cs`, `MayDeploy`, and `GamePlanner.SupportWeapons.cs`, `PlanDeploy`: only Russians are barred.
- **Capture attempt against a non-elite Italian (A25.63, p. 97).** `ScenarioA1CloseCombatCalculator.cs`, near line 987: the +1 capture DRM is added unless the defender is Inexperienced, so an Italian 3-4-7 or 3-4-6 defender wrongly gets it.
- **Italian and Axis Minor prisoners may attempt escape (A25.63, A25.82).** The escape round has no nationality test.
- **Finns firing a Russian MG take Captured Use penalties (A25.75, p. 97).** `LiveFire.cs`, `CapturedBy`: any weapon of another nationality is captured. The catalog has no Finnish MG, so this is the only way a Finn fires one.
- **Finnish HS cannot Recombine, and Finnish squads cannot Deploy, without a leader (A25.71, p. 97).** `PlanDeploy` and `PlanRecombine` require a Good Order leader for every nationality. The game refuses a legal action; nothing wrong happens.
- **Commissar ignored in a PAATC (A25.221).** `GamePlanner.Overrun.cs`, `PaatcFacts`: no +1 Morale Level, and the best leader's DRM is taken even with a Commissar present.
- **Possible catalog inconsistency, not settled.** The German squared-E HS 2-4-8 (`attacker-elite-half-squad`) has `asl:elr-5` true while its squad 4-6-8 has it false (transcription row 256 says "8 underlined"). At the render's resolution I could not read the underline on p. 695. Worth a look at the chart at higher zoom.

**B.1 to B.10, B1 to B8**

- **Repaired in pass 35 (2026-10-09).** **B.2, SMOKE's MF is not doubled uphill.** `GamePlanner.Terrain.cs: GroundStep` doubles the terrain cost for the level climbed and then adds `BlazeEntryHalfMf` (one MF). Open Ground one level up with SMOKE costs 3 MF; B.2's own example says 2 x 2 = 4. No ruling found (searched the rulings and the backlog for SMOKE with elevation).
- **Repaired in pass 35 (2026-10-09).** **B.10, a total of 6 from SMOKE or vehicles is not a block.** `LosCalculator` blocks on map Hindrance alone; `GamePlanner.Night.cs: NightAndWeatherFacts` tests the total only when a Low Visibility DRM is present. With none, terrain plus SMOKE plus AFV or wreck Hindrance of 6 or more goes to the IFT as a +6 or higher DRM instead of no LOS.
- **Repaired in pass 35 (2026-10-09).** **B1.14, B1.16, B1.17, Interdiction where FFMO could not apply.** `GamePlanner.Rout.cs: OpenGround`, `Interdictor`, and `ExposedInOpenGround` test the terrain name, SMOKE in the hex, and the map's own LOS Hindrance. They do not read Height Advantage, a wall or hedge on the hexside the LOS crosses, a wreck or AFV giving cover in the hex, or SMOKE and vehicle Hindrance along the LOS (`VehicleHindrance` is not called). A broken unit is Interdicted, and must rout, in cases the rules exempt. Ruling R13.3 does not record this.
- **B6.2, the Hindrance when one end is below the bridge** is omitted (VASL's rule, reproduced). Low weight: no unit can use a bridge hex yet, but a LOS between admitted hexes across a bridge can be affected.
- **Naming:** the refusals "Continuous Slopes are not reviewed" (`GroundStep`, `VehicleCost`) concern Slope hexsides, not B.5.

**B9 to B13**

- **Repaired in pass 35 (2026-10-09).** **Interdiction and the rout's Open Ground test ignore wall or hedge TEM and Height Advantage.** `GamePlanner.Rout.cs: OpenGround`, used by `Interdictor` and `ExposedInOpenGround`, reads only the hex's terrain and SMOKE. By B10.31 (p. 125) a unit eligible for the Height Advantage TEM is not subject to Interdiction or FFMO by that attacker, and B10.3 makes a hill hex Open Ground for Interdiction and Rout only barring Height Advantage; by B9.3 and B9.33 (pp. 119 to 120) a wall or hedge TEM bars Interdiction until elevation reduces it to 0. A routing unit behind a wall from, or above, its only Interdictor is Interdicted, and a broken unit there is forced to rout. Not in the backlog or the rulings (R13.3 does not mention it).
- **Woods-road hexes probably read as Open Ground (not settled on a real board).** `GamePlanner.Map.cs: TerrainKey` takes the Location's terrain from the hex center (`CenterTerrainSampler`), and `FireTerrain` maps a road to `open-ground`. In a woods-road hex whose center dot is on the road, a unit would then have TEM 0 and take FFMO from any LOS, enter for 1 MF across a woods hexside, and a vehicle would enter off road at the Open Ground cost with no Bog DR. B13.31 (p. 128) gives the woods TEM unless the LOS misses the woods symbol and the mover entered at the road rate. `GamePlanner.Vehicles.cs` line 626 (`entry.Road && entry.Terrain == "woods"`) shows the planner expects such a hex to read as woods. I read the code only; a board read of a woods-road hex (for example 19X1) would settle it.
- **B9.35 over-refusal.** `HexsideTemAt` refuses fire whenever the crossed wall lies between hexes of different Base Levels, calling it a Hillside wall. A wall that B9.35 puts on the lower level (lower terrain shown between wall and crest line, for example 3U4-V4) is a normal wall. The result is a refusal, not a wrong outcome.
- **B9.33 has no test.** The reduction in `HexsideTemAt` (`height - range`) was not found in any test.

**B14 to B22**

- **Repaired in pass 35 (2026-10-09).** **Vehicles refused in an orchard** (`GamePlanner.VehicleTerrain.cs`: `VehicleTerrainHalfMp`, `VehicleCost`): B14.4 gives the Open Ground cost. A refusal with a false reason ("not allowed").
- **Moved to pass 135 (2026-10-09).** **Orchard always in season** (`LosCalculator.cs`: `CheckTerrainIsHigherRule`, `CheckTerrainHeightRule`; no reader of `ScenarioMonth` in Maps or in the planner's LOS call): November to March, a LOS to or from a higher level across an orchard is blocked where B14.2 gives +1. Silent.
- **Repaired in pass 35 (2026-10-09).** **Fire through out-of-season grain refused** (`ScenarioA1FireCalculator.cs`: `Undecided`, both the Infantry and the vehicle branch; `ScenarioA1OrdnanceCalculator.cs` line 510 the same): with a declared month outside June to September the attack is refused as hindrance-unattributed. B15.6 makes it Open Ground. A needless refusal; `GamePlanner.Fire.cs: FireMapFacts` already leaves grain out of the DRM out of season.
- **Repaired in pass 35 (2026-10-09).** **No Bog Check next to marsh** (`GamePlanner.VehicleTerrain.cs`: `VehicleCost`): B16.43 requires one for any vehicle entering a ground level or level -1 hex adjacent to marsh by a non-road hexside. Silent, and no ruling records it.
- **Repaired in pass 35 (2026-10-09).** **HE into marsh at full FP** (B16.31): recorded in the backlog (ruling R10.1), so not a new fault, but it is a wrong result and not a refusal.
- **Equipment left in marsh survives** (B16.1): unpossessed portaged equipment in a marsh hex should be eliminated. Silent (judged from a search that found no logic).
- The vehicle refusal for marsh cites the Terrain Chart rather than B16.41; the result is right.

**B23 to B25**

- **Repaired in pass 35 (2026-10-09).** **A wreck burned in Close Combat does not burn.** `GamePlanner.VehicleCloseCombat.cs` (the resolution near line 371) emits `VehicleWrecked(vehicle.Id, burning: true)` for a `burning-wreck` result and creates no Blaze entity. `GamePlanner.Wrecks.cs`, `IsBurning`, reads only the entity `BlazeId(wreck)`, which `GamePlanner.Fire.cs` and `GamePlanner.Ordnance.cs` create with `asl:fire`. `GameProjector.cs`, `Wreck`, does not create one either. So the wreck gives the +1 wreck TEM and Hindrance, no smoke, and no extra entry cost, against B25.14 and B25.2. `ScenarioA1Pass11Tests` checks the package result only.
- **Indirect Fire against a building ignores B23.32.** `ScenarioA1FireCalculator.cs` (the TEM block near line 1847) gives an Area Target Type hit the Location's plain TEM. A mortar hit on the ground level of a multi-level building takes no +1 per level above and does not attack the other levels. Nothing refuses it. (Ordnance at a target at another level is undecided, so the case is a same-level mortar shot at a building.)
- **Repaired in pass 35 (2026-10-09).** **Vertical ADJACENT is missing (B23.25).** `GamePlanner.Map.cs`, `IsAdjacent`: Locations of one hex joined by a stairwell are not ADJACENT. Whatever reads it (Fire Group adjacency in `FireMapFacts`, and its other callers, which I did not trace one by one) treats such units as not ADJACENT without saying so.
- **A Gun may set up where B23.423 bars it.** I found no setup check in `ScenarioSetup.cs` that keeps a 5/8" weapon off an upper level or a large-target Gun out of a building. Judged from searches, not from a full read of the setup code.
- **Refused since pass 35 (ruling R35.10): a game with such a counter is not set up or played.** **Counters placed and ignored** (already in the coverage document's section 8 for fortifications; it holds for this group too): `asl:fortified-location`, `asl:rubble`, and `asl:flame` exist in the vocabulary and nothing in Play or Rules reads them. A game that fields them plays as if they were absent.
- **A Factory by SSR plays as a stone building** (ruling R17.13, recorded in the backlog, so a known gap and not a new fault): no refusal tells the players.
- **To check, not a settled fault:** ruling R10.7 has Residual FP attack a Bypassing stack in the Bypass lane's terrain (`GamePlanner.Fire.cs`, `FireMapFacts`). B23.31 (p. 136) says a Bypassing unit does not qualify for building TEM "vs non-Residual-FP" attacks, which reads as leaving the building TEM in place against Residual FP.
- **Smoke height.** B25.2 limits a Blaze's smoke to four levels above the Fire (ruling R6.3 repeats it). `GamePlanner.Wrecks.cs`, `VehicleHindrance`, adds the +2 for any LOS that crosses the hex, whatever its height. Small, and only reachable with large height differences.

**B26 to B37**

- **Refused since pass 35 (ruling R35.10): a game with such a counter is not set up or played.** **Fortification counters are silent** (already in the coverage document's section 8): a game can hold wire, foxhole, trench, minefield, roadblock, and pillbox entities, and units inside one (`ContainmentRole.InFortification`, validated in `GameProjector.cs` line 3937). Movement, fire, rout, rally, CC, and Control ignore them, with the single exception named in part 1. A card that fields them plays wrongly without a message.
- **Possible silent fault, not settled: depression terrain under admitted terrain.** In `LimboDancer.Domains.Asl.Play` only the A1 entry facts (`GamePlanner.cs` lines 1703 to 1704) read `DepressionTerrain`. `InfantryStep` and `FireMapFacts` decide by the Location's terrain name alone. If a stream-woods hex (B33.1, board 47) reads "Woods" at its Location with the stream as depression terrain, the game would play it as plain woods at 2 MF with woods TEM. If it reads as a stream, it is refused. I could not check which, having no such board to read.

**C.1 to C.9, C1 to C4**

- **Repaired in pass 35 (2026-10-09).** **Ordnance HE into a marsh is not halved (C3.53, p. 170).** `ScenarioA1OrdnanceCalculator.HitAttack` and `ScenarioA1FireCalculator.Arithmetic` (the `hit is not null` branch) give the full HE FP whatever the target terrain. Marsh is a readable target terrain (`GamePlanner.Fire.cs` terrain keys; TEM 0 in `ScenarioA1FireReference`), Infantry can enter it, and only fire from a marsh is refused. Nothing refuses or halves a Gun's shot into one. Read in code, not run. With the Area Target Type it should be halved twice.
- **TH# color default (C3.3, p. 169; A25 chart, p. 109).** `ScenarioA1OrdnanceReference.Color` returns red for every nationality except German. Wrong for British (black), Japanese (black), American from 1944 (black), and French non-vehicular (black). Latent today: the only non-German, non-Russian ordnance is the British ATR (forced to black by its own rule) and the British light mortar (Area row, red for all). It becomes a live error with the first British or American Gun or tank.
- **Upper-superstructure hits (C3.9, p. 171).** `ScenarioA1ArmorCalculator.Kill` sets the hit location to "turret" only when the target is turreted. A non-turreted AFV (the SPW 251/1 halftrack) is always hit in the hull, so a Final TK DR equal to the TK# always immobilizes it and never Shocks it, and a superstructure AF could never differ. Ruling R7.4 records the choice but does not call it a departure from the rule.
- **Heavy Payload (C.7, p. 162) absent with no guard.** A CH doubles the FP (`ScenarioA1FireCalculator.Arithmetic`) and takes the highest column; nothing adds the -1 per eight FP over 36. Unreachable with the catalog (largest HE FP 12, doubled 24). The first Gun of 100mm or more would be resolved wrongly without a refusal.
- **Prohibited Hexes (C2.7, pp. 168 to 169) not checked at setup.** `ScenarioSetup.Check` requires a Gun to be manned or towed and handles HIP; I found no check of the Gun's Location (upper building level, large Gun in a building, marsh, water, crag). Judged from a search that found nothing, not from a setup run.
- **Default ammunition against an unarmored vehicle (C2.21, p. 167).** `LiveOrdnance.FromState` defaults a named vehicle target to AP. Against a truck the rule's default is HE. A Gun with no AP (the 7.5cm leIG 18) is refused until the player declares HE or HEAT, which is safe; a Gun with AP fires AP at a truck unless told otherwise, which is a different TK# from the rule's default.

**C5 to C9**

- **Repaired in pass 35 (2026-10-09).** **Rubble is left out of the firer's terrain.** `GamePlanner.Ordnance.cs: WoodsOrBuilding` is `woods, wooden-building, stone-building`; the terrain keys `wooden-rubble` and `stone-rubble` exist. So Case A is not doubled, Case B is plus 2 not plus 3, Case E is not doubled, and the C5.11 CA lock does not apply when the firer is in rubble (C5.11, C5.2, C5.5; the chart on p. 189 says "woods/building/rubble").
- **Repaired in pass 35 (2026-10-09).** **Acquisition is kept when the rule removes it** (C6.5, p. 174). The only places that drop or replace it are `GameProjector.Ordnance` (a new shot) and `GameProjector.KeepAcquisitions` (crew not Good Order, not manned). It survives: - a CA change without firing (`GameProjector.TurnGun`), - the Gun being pushed or hooked up (Bore Sights are removed there, Acquisitions are not), - a tank moving out of its Location (no Gyrostabilizer exists, so C6.55 gives it no right to keep it), - a light mortar carried to another Location.
- **Repaired in pass 35 (2026-10-09).** **Acquisition does not follow a vehicle target** (C6.5, C6.51). `AcquiredUnits` reads `Hit.Targets`, which is empty on the Vehicle Target Type, so the counter stays on the Location: the vehicle that moves away in LOS loses it, and any unit entering that Location inherits it. `AcquisitionFollowUp` also skips an Acquisition whose Gun is a tank (`is not EquipmentInstance`).
- **Repaired in pass 35 (2026-10-09).** **ATR denied Case L** (`ScenarioA1ArmorCalculator.Run`: `!latw && range <= 2`). C6.3 exempts only non-ATR LATW using their own To Hit table.
- **Repaired in pass 35 (2026-10-09).** **Opportunity Fire by ordnance takes Case B** (and a LATW Case C3) in the AFPh. A crew or squad can be declared an Opportunity Firer (`PlanOpportunityFire` accepts any Personnel); `PlanFireOrdnance` bars its Gun in the PFPh, and in the AFPh the calculators add the AFPh DRM and deny ROF without regard to it (C5.2, C5.34). Read from the code, not run.
- **Repaired in pass 35 (2026-10-09).** **Case O missing where Hazardous Movement exists.** A Gun's Defensive First Fire at a crew pushing a Gun (A4.62, built for IFT attacks) gets Cases J3 and J4 from `ScenarioA1OrdnanceCalculator.Run` instead of Case O minus 2 (C6.6 bars Case J with it).
- **Repaired in pass 35 (2026-10-09).** **Case E Hindrance set to zero.** `OrdnanceMapFacts` gives an own-Location shot `new FireLos(false, 0, true, false)`, so smoke (grenades exist) or a wreck in the hex adds nothing (C5.5).
- **Red CS# note of the C7.7 table** (p. 176, note A) is not applied when judging a burning wreck; the transcription records the note but `ScenarioA1ArmorCalculator.Kill` ignores it. No catalog vehicle has a red CS# today.
- **A concealed vehicle hit and revealed is never acquired** (`acquires = ... target.Concealed != true`), though C6.57 lets the shot that costs the concealment acquire it. Small.
- Outside this group but seen: the D5.34 Stun "+1" DRM and the A7.7 Encircled +1 (both in the C5 "Other" list, p. 173 and 189) are absent from `ScenarioA1OrdnanceCalculator.Run` (the Infantry Target Type path); the Stun DRM exists only on the Vehicle Target Type path, Encircled on neither.

**C10 to C13**

- **Repaired in pass 35 (2026-10-09).** **A vehicle towing a Gun may use Bypass.** `GamePlanner.VehicleTerrain.cs: VehicleBypass` adds 2 half-MP for a towed Gun and returns the entry. C10.1 forbids Bypass while towing (except Narrow Streets). No ruling records this.
- **Repaired in pass 35 (2026-10-09).** **A vehicle towing a Gun may cross a hedge.** `GamePlanner.VehicleTerrain.cs: VehicleCost` lets a half-tracked vehicle cross a hedge and adds the towing MP. C10.1: "No vehicle may tow a Gun over a wall or hedge". The catalog halftrack has T7. No ruling records this.
- **Repaired in pass 35 (2026-10-09).** **A PF or PSK fired from rubble takes no Case C3 and no restriction.** `GamePlanner.Ordnance.cs: SupportWeaponMapFacts` sets `FromBuilding` only for `wooden-building` and `stone-building`; rubble terrain keys exist (`wooden-rubble`, `stone-rubble`). C13.8 and the C3 chart name rubble with ground-level buildings. A pinned firer in rubble is not refused either.
- **Repaired in pass 35 (2026-10-09).** **An Opportunity Firer's LATW shot in the AFPh takes Case C3.** `ScenarioA1ArmorCalculator.Run` adds `case-c3:afph` to every LATW shot in the AFPh; the shot's facts carry no Opportunity Fire flag, and `PlanFireOrdnance` bars an Opportunity Firer only in the PFPh. C13.1 and the C13.33 EX except Opportunity Fire. The same flag is the C13.8 alternative to Case C3 from a ground-level building.
- **A push in Mud or Deep Snow omits the +3 and keeps the -2 for a road.** `GamePlanner.Movement.cs` passes `halfMf / 2 - (entry.RoadRate ? 2 : 0)` to `PushPlan`; weather is in the game since pass 16. C10.3 DRM table: +3 into or from mud or deep snow, and the road -2 does not apply there unless paved or plowed.
- **The on-map push has no QSU, NM, or RFNM check** (`GamePlanner.Movement.cs`, the `pushGun` block). Latent: both catalog Guns are QSU.
- **A hook-up right after a push is admitted without C10.31's conditions.** `PlanHookGun` checks the vehicle's TI, not the crew's or the Gun's, so a crew whose push ended on a DR equal to the M# (it "cannot be moved any farther") can still have its Gun hooked up that MPh.
- **A Gun first manned by a HS or squad is not recorded as having lost Emplacement.** `LiveOrdnance.Emplaced` reads the present manner's kind; `UnemplacedGuns` is only written on a push or a hook-up. If a crew later takes the Gun over, it reads as Emplaced again, against C11.3 ("may not regain"). Reaching it needs Recovery of a Gun by a crew; I did not check whether that is possible.

**D.1 to D.8, D1 to D4**

- **Repaired in pass 35 (2026-10-09).** **D2.16, BU road rate (p. 196).** `GamePlanner.VehicleTerrain.cs: VehicleCost` doubles the road rate only when `Is(vehicle, Conditions.ButtonedUp)`, which is true only when the condition is recorded. A CT AFV with no recorded state is BU by default (`LiveFire.CrewExposed`, R7.11), so a tank that never touched its BU counter pays 1/2 MP per road hexside. The same read is in `GamePlanner.Recall.cs` line 114 (Recall route cost), `GamePlanner.Vehicles.cs` line 335 (E1.52), `GamePlanner.Night.cs` line 172, and `PlanButtonUp` line 1058, where a default-BU tank asking to become CE is answered "already CE". The last is D5.33, outside this group; I note it because it has the same cause.
- **Repaired in pass 35 (2026-10-09).** **D2.38, TEM in Bypass (p. 198).** `GamePlanner.Ordnance.cs: OrdnanceMapFacts` sets `TargetTerrain = read.TargetTerrain` for a named vehicle target with no Bypass exception; `ScenarioA1ArmorCalculator.Run` then adds `case-q`. A vehicle in Bypass of woods or a building gets +1 to +3 it is not entitled to. `FireMapFacts` has the exception only for an Infantry stack in Bypass (`state.Movement.Bypass`).
- **D2.321, Bypass TCA (p. 197).** Silent omission, as in part 1. Not a recorded deviation: R11.2 records only the hex-center LOS.
- **D2.33, Target Facing after a VCA change at the CAFP (p. 198).** The rule keeps the vehicle at its last CAFP and Target Facing if fired on before it completes the move. After the turn the vehicle's facing no longer runs along its hexside, so `LaneEnds` returns nulls, `BypassTargetFacing` returns null, and `OrdnanceMapFacts` falls back to the ordinary hex facing from the new VCA. Moderate confidence: read from the geometry, not traced with values.
- **Repaired in pass 35 (2026-10-09).** **D1.321, RST and the CMG (p. 194).** An RST AFV "can fire neither its MA nor its CMG while CE". `ScenarioA1OrdnanceCalculator` bars the MA shot, but `ScenarioA1FireCalculator.OverrunWeapons` and `ScenarioA1VehicleCloseCombat.VehicleFirepower` do not read the MA type, so a CE T-34 M41 adds its MA base and CMG to an OVR and its CMG to CC.
- **D3.7, repair and Recall (p. 201).** - `GamePlanner.Rally.cs: PlanVehicleRepair` requires `LiveFire.CrewExposed` for every vehicle repair. The rule asks a CE crew only for an AAMG, so a BU tank cannot repair a malfunctioned MA. - `Conditions.BmgMalfunctioned` and `CmgMalfunctioned` are set by an OVR's Original 12 and never cleared: no repair path. - A repair dr of 6 sets `Disabled` and nothing more. A tank whose MA (and all SA) is disabled should be Recalled at once (it has no Passenger or towing capability); `GamePlanner.Recall.cs` does not read `Disabled`.
- **Repaired in pass 35 (2026-10-09).** **D2.5, ESB nationality DRM (p. 198), latent.** `PlanEsb` uses German +2, Russian +1, anything else +3. The table gives U.S. and Czech 0 and British +2. Harmless with today's catalog (German and Russian vehicles only); wrong the day another nationality's vehicle is added.
- **D2.3, failed clearance (p. 196), minor.** The rule spends the announced MP and a Stop MP when the hexside proves too tight. The code refuses the step at no cost. This follows from R11.2's map read and may be meant; R11.2 does not say so.

**D5 to D10**

- **Repaired in pass 35 (2026-10-09).** **Stun +1 missing on an OVR DR (D5.34, p. 203).** `ScenarioA1FireModels.cs: FireOverrun` carries no StunRecovery, and `ScenarioA1FireCalculator.cs` adds "stun-recovery" only for `attack.VehicleFire`. A vehicle under a +1 counter OVRs with no +1.
- **Repaired in pass 35 (2026-10-09).** **Stun +1 missing on the vehicle's CC DR (D5.34).** `ScenarioA1VehicleCloseCombat.cs` reads Stunned, Shocked, Abandoned, never StunRecovery.
- **The +1 counter does not follow the crew (D5.34).** `GamePlanner.Ordnance.cs: CrewCounter` (failed Immobilization TC, Crew Survival) creates the crew without StunRecovery; only `GamePlanner.Recall.cs: AbandonEvents` carries it.
- **Repaired in pass 35 (2026-10-09).** **The CC counter of CC Reaction Fire is never read (D7.21, p. 207).** `Conditions.CcReaction` is set in `GamePlanner.VehicleCloseCombat.cs: PlanVehicleAttack` and cleared at the MPh's end, and nothing else refers to it, so a unit that made CC Reaction Fire may still make Non-CC Reaction Fire.
- **Repaired in pass 35 (2026-10-09).** **A Bypassing AFV or wreck always hinders (D9.4, p. 210).** `GamePlanner.Wrecks.cs: VehicleHindrance` does not look at `Straddling`; the rule gives no Hindrance when the LOS does not touch the Bypassed hexside.
- **Repaired in pass 35 (2026-10-09).** **A bogged vehicle cannot unload (D8.5, D6.5).** `GamePlanner.Vehicles.cs: PlanMoveVehicle` refuses a bogged vehicle everything but its Bog Removal before it reaches the Unload case, and `GameProjector.cs: StepVehicle` fails the step too. The exemption list covers Prep Fire, immobilized, and Abandoned only.
- **Repaired in pass 35 (2026-10-09).** **ESB by a Recalled AFV is not barred (D5.341: "ESB attempts are NA").** `PlanMoveVehicle` returns `PlanEsb` before its Recall route check, and `PlanEsb` has no Recall test.
- **Repaired in pass 35 (2026-10-09).** **CE status after Bounding First Fire (D5.33).** `PlanButtonUp` bars the change after Prep Fire only; the rule also bars it in an MPh after the vehicle's or its PRC's Bounding First Fire.
- **Repaired in pass 35 (2026-10-09).** **A bogged Recalled AFV (D5.341).** The rule Abandons a bogged or immobilized Recalled AFV. `MustLeave` and the end-of-Player-Turn check in `GamePlanner.cs` test Immobilized only; a bogged one must leave but may only attempt Bog Removal.
- **Repaired in pass 35 (2026-10-09).** **Probable: Case J against a vehicle spending Bog Removal MP (D8.4, p. 209).** `GamePlanner.Ordnance.cs: PassEightFacts` sets `MpInLos` for any moving vehicle target, and `ScenarioA1ArmorCalculator` then applies Case J, J1, or J2. The rule allows the shot "with no Case J". I read the code path; I did not find a test and did not run it.
- **Crew Survival DRM (D5.6, D5.34), minor.** A crew that is Shocked and also under a +1 counter gets +1, not +2 (`target.CrewImpaired == true || target.StunRecovery == true`). The two DRM come from different rules and look cumulative.

**D11 to D17**

- **A lone radioless T-34 M41 moves with no NTC** (D14.23) and never needs a platoon (D14.2). Recorded: R7.1, backlog line 182. The catalog has no radioless trait, so the planner cannot tell.
- **No smoke dispenser for the PzKpfw IIIH** (D13.1 to D13.31). The catalog has no dispenser attribute, so the counter's printed value is lost at the data level. Not in the backlog.
- **No vehicular smoke grenades** (D13.35) for any armed vehicle's crew. `GamePlanner.Smoke.cs` admits only a squad of the moving stack with a Smoke Placement Exponent. Not in the backlog.

**E.1 to E.6, E1 to E3**

- **Repaired in pass 35 (2026-10-09).** **VBM pays no night or weather MP.** `GamePlanner.VehicleTerrain.cs`, the VBM cost path (the `VehicleEntry` built near line 400) adds SMOKE, wrecks, and towing, and never calls `VehicleWeatherHalfMp`. E1.52 names "transited via VBM" (p. 224); E3.9 says weather costs are added "per hexside crossed (or Bypassed)" (p. 231). Not recorded as a deviation.
- **Repaired in pass 35 (2026-10-09).** **Infantry Bypass pays no weather MF.** `GamePlanner.Terrain.cs: BypassStep` never calls `InfantryWeatherHalfMf`. At night that is right (E1.51 excepts Bypass), but the rain level cost (E3.54), the Mud half MF (E3.64), and the Deep Snow half MF (E3.733) are dropped. Not recorded.
- **Repaired in pass 35 (2026-10-09).** **Infantry on an unpaved road in Mud, in a hex whose other terrain is not Open Ground, pays 1 MF, not 1.5.** `GamePlanner.Night.cs: InfantryWeatherHalfMf` adds the half MF only when the terrain key is `open-ground`. E3.6 and E3.65 make an unpaved road Open Ground when used (p. 230). The vehicle path does this correctly (`GamePlanner.VehicleTerrain.cs`, the Mud branch).
- **Dismantling or assembling the MMG at night marks a Gunflash (likely; read, not run).** `GamePlanner.SupportWeapons.cs: PlanDismantle` sets Prep Fire or Final Fire on the weapon; `GamePlanner.Night.cs: Gunflash` counts any unit or weapon in the Location with such a condition. E1.89 (p. 226) says no fire counter is placed for it at night and no Gunflash results.
- **A Gunflash follows the firer, not the Location.** `Gunflash` reads the conditions of what is in the Location now. E1.8 says the Location stays marked "even if any/all the units have in the meantime left" (p. 226). A unit that Prep Fired and advances in the APh takes the flash with it. A firer that kept ROF and carries no counter leaves no flash at all (E1.8 EX). Ruling R16.2 lists the counters but does not record either point.
- **The first Starshell is allowed by any Gunflash.** `GamePlanner.Starshells.cs` accepts any fire or Melee counter on the map, on either side, including an Opportunity Fire declaration that has not fired (it sets the Bounding Fire condition). E1.91 (p. 226) needs a Gunflash "due to an enemy FFE or an attack vs an enemy unit". Small.
- **Starshells and Interior Building Hexes.** `PlanStarshell` does not refuse a firer in an Interior Building Hex, and `Illuminated` lights Interior Building Locations (E1.921, E1.923). Ruling R16.8 does not record it. Small and rare on the boards in use.
- **A CE AFV with an Armor Leader needs a Usage dr of 2, not 4** (`PlanStarshell`: `need = leader ? 4 : 2`; E1.921). Small; applies only if Armor Leaders exist as vehicle state.
- **Repaired in pass 35 (2026-10-09).** **In Mud a road gap in a wall may be charged as a wall for a vehicle (unsettled).** `GamePlanner.VehicleTerrain.cs` sets `road = false` for an unpaved road in Mud before the wall test `wall is not null && !road`, so a truck on such a road across a hexside that also carries a wall or hedge would be refused. I did not find a board hexside to confirm it.
- **Repaired in pass 35 (2026-10-09).** **Reverse movement multiplies the night and weather MP (unsettled).** The reverse multiplier is applied after the weather cost is added; E3.9 says weather costs are added "after calculating total cost". I could not settle from D2 text in this group whether the reverse multiple covers them.

### 8.4 Still true from the first version

- **Stale backlog rows.** Some rows of backlog sections 1 to 15 describe gaps that later passes closed (vehicle CC, TI placement, "vehicles do not Bypass"). Two cite rule numbers the PDF does not have (D8.32 to D8.35; B13.422).
- **Two ruling numbers cited in code do not match the rulings table:** "R24.3" for the refusal of CC with a Gun's crew, and "R24.7" for a reading of C3.7. Both rulings are about Victory Conditions.

## 9. What this means for the rule passes

The plan's section 22.1 is rewritten from the inventory and awaits the user's approval. In short:

- **Repairs come first.** The faults of section 8.3 that give wrong results today make a pass of their own.
- **The planned passes held more than their figures.** Armored combat II held three passes' work; terrain is five passes, not three.
- **Some foundations must come before what was planned to use them:** wind and Environmental Conditions before SMOKE, Fog, and Drifts; the vertex LOS and Wall Advantage before Hull Down, Rowhouses, and bocage; Impulse Movement before Platoon Movement, Human Wave, and Convoys.
- **Fortifications still want attention before they are built**, since their counters are ignored today; the repairs pass adds a refusal as a stopgap.

