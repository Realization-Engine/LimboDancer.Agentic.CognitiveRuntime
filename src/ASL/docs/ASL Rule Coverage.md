# ASL Rule Coverage

**Status:** First assessment, 2026-10-05, of `main` at 038dca8 with the day's uncommitted document edits. It is a snapshot: a pass that builds a rule changes its row here.

**Question answered:** which rules of the rulebook does the game implement today, wherever the code sits, and which does it not.

**How it was made.** Every dotted rule citation under `src/ASL` was indexed by rulebook section and by project (75 sections have at least one). Five read-only audits then went through the rulebook section by section, one each for A1 to A12, A13 to A26, B, C with D, and E to H. For each subsection they looked for real logic in the code (a branch, a check, a computed value, not a comment) and for a test that reaches it. The backlog and the rulings were used to find gaps, never as proof that a rule is built. I checked the claims the summary rests on myself: the terrain lists, the Hindrance rule, the ordnance refusals, the catalog's counts, and the absence of code for the rules listed as wholly missing.

**Limits, stated plainly.**

- The rule text was read from the repository's transcription (`docs/ASL/Rulebook_Markdown`, Chapters A to E), not from the PDF. Chapters F, G, and H are not transcribed and are assessed at chapter level only.
- No test was run. "Built" means logic and a test were found by reading, not that the suite was executed for this document.
- A subsection marked *unconfirmed* is one the audit could not settle. Section 8 lists them. They are not counted as built.
- Chapters I to Z of the PDF (campaign games, Deluxe, the historical modules, Solitaire, Korea) are outside this assessment. Nothing in the code cites them.

## 1. How to read the tables

| Status | Meaning |
|---|---|
| **Built** | Logic is present and a test or a clear call path reaches it. |
| **Mostly built** | The section's main mechanics are built; named parts are missing. |
| **Partly** | Some mechanics are built and some of comparable weight are not. |
| **Mostly not built** | A small part exists. |
| **Refused** | The game meets the situation and refuses it with a reason. Nothing wrong can happen, and nothing can be played. |
| **Not built** | No logic was found. |

"Lives in" names the projects that hold the logic: **Play** (the planner, `LimboDancer.Domains.Asl.Play`), **Rules** (the calculators and packages, `LimboDancer.Domains.Asl.Rules`), **Units** (state and the projector), **Maps** (LOS and terrain). "Pass" is the planned pass of the [redesign plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 22, that would build what is missing, where one exists.

## 2. The summary

**By section, Chapters A to E (104 numbered sections).**

| Chapter | Sections | Built | Mostly built | Partly | Mostly not built | Refused | Not built |
|---|---:|---:|---:|---:|---:|---:|---:|
| A. Infantry and basic rules | 26 | 2 | 16 | 6 | 0 | 0 | 2 |
| B. Terrain | 37 | 3 | 5 | 3 | 1 | 14 | 11 |
| C. Ordnance and OBA | 13 | 0 | 6 | 5 | 0 | 0 | 2 |
| D. Vehicles | 16 | 0 | 4 | 5 | 1 | 0 | 6 |
| E. Miscellaneous | 12 | 0 | 0 | 2 | 0 | 0 | 10 |
| **A to E** | **104** | **5** | **31** | **21** | **2** | **14** | **31** |

Chapters F (13 sections), G (18), and H are not built. The LOS code reads some Chapter F and G terrain because it reproduces VASL's LOS; no rule of those chapters is played.

**In words.** Chapter A is the game: 18 of its 26 sections are built or mostly built, and the six partial ones miss parts that mostly concern vehicles, special weapons, and nationalities. Chapter B is played on a short list of terrain. Chapters C and D have a working core for two tanks and two Guns and stop where armor begins to manoeuvre. Chapter E has night and weather in part and nothing else.

**Seven limits that cut across the sections.** These decide more of what can be played than any one row below.

1. **Terrain is a short list.** Movement and fire accept Open Ground, roads, brush, woods, orchard, grain, marsh, rubble, and ordinary wooden and stone buildings with their upper levels; walls and hedges as hexsides; hills. Every other terrain name is refused, in movement ("is not a reviewed entry") and as a fire target ("has no TEM"). A board that prints a gully, a stream, a shellhole, a crag, or a graveyard cannot be moved through or fired into at those hexes.
2. **Only three Hindrances are decided.** A LOS that crosses brush, in-season grain, or same-level marsh takes its Hindrance. A LOS that crosses any other Hindrance (orchard, crag, graveyard, light woods, debris) leaves the attack refused as undecided.
3. **Ordnance fires on one level, at hexes with no hexside terrain.** A Gun's or a tank's shot at a target on another level is refused, and so is any ordnance or vehicle fire at a target hex with a wall, hedge, or cliff hexside.
4. **Mixed Locations are refused, not resolved.** Infantry with a vehicle in one target Location, two vehicles in one Location under small arms fire, an AFV in terrain with a positive TEM under small arms fire, a Gun's crew stacked with other units: each is refused.
5. **The catalog is small.** 173 counters of eight nationalities (German, Russian, American, British, French, Italian, Finnish, Axis Minor). Five are vehicles (PzKpfw IIIH, T-34 M41, SPW 251/1, Opel Blitz, GAZ-MM) and two are Guns (7.5cm leIG 18, 45mm PTP obr. 32). A rule that needs an armored car, a non-turreted AFV, a medium mortar, a bazooka, or a Japanese squad has no counter to act on, even where the code admits the type.
6. **Fortification counters are ignored.** Wire, foxhole, trench, minefield, roadblock, and pillbox counters can be placed, and no planner code reads them: a unit moves through wire and stands in a foxhole with no effect. This is the one place where the game is silent instead of refusing.
7. **Passengers are cargo.** They load, ride, and unload. They do not fire, rout, or fight, they are never a target, and they die with their vehicle without a roll.

## 3. Chapter A: Infantry and basic game rules

| § | Title | Status | Lives in | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|
| A1 | Personnel Counters | Mostly built | Play, Units, Rules | Printed FP, range, morale; Deployment (1.31); Recombining (1.32); US# in the Concealment dr | Squad Spraying Fire off the underscored range (see A7.34); a manned Gun is not reassigned on Deployment | |
| A2 | The Mapboard | Mostly built | Play, Maps | Boards and playable area; Locations by level; entry and its delay; exit; setup limits (2.9) | Overlays (2.7 to 2.76) not built; offboard setup simplified (2.51); terrain with no entry cost refused; a card that sets units up broken is refused | 40 (overlays) |
| A3 | Basic Sequence of Play | Built | Units, Play | All eight phases, their restrictions and clean-up; the turn record and the game's end | The first-move dr is manufactured (R20.2) | |
| A4 | Infantry Movement | Mostly built | Play, Rules | MF, leader bonus, terrain costs, Minimum Move, stacks and the DEFENDER's window, portage and Recovery, Double Time and CX, FFMO and FFNAM, Assault Movement, the advance, PAATC, TI | **Dash (4.63) not built**; Infantry OVR (4.15) refused outside one reviewed case; Bypass only at ground level of woods and buildings, with vertex LOS refused (4.34); the road rate always taken (4.132, R10.1); AFPh limits for a moved MMG or HMG (4.41) not built; Hazardous Movement only for a pushing crew | |
| A5 | Stacking Limits | Partly | Play, Rules | Limits at setup, entry, and advance; squad-equivalents; overstacking penalties in Close Combat and on ordnance To Hit | **No overstacking penalty on small arms fire** (5.12, 5.131); the extra MF is charged in the APh only (5.11); vehicular stacking (5.132) not built; any number of vehicles share a Location (R11.6) | |
| A6 | Line of Sight | Mostly built | Maps, Play | LOS between Locations, obstacles by height, depressions, Blind Hexes, buildings; a Hindrance total of six blocks | Only three Hindrances decided in fire (limit 2 above); Bypass vertex LOS refused; LOS returns "unsupported" for bocage Blind Hexes, some slope and hillock cases, factory rooftops; LOS checks are free (R31d.4, against 6.11); no Random Events on a blocked shot | |
| A7 | Fire Attacks | Mostly built | Rules, Play | The IFT and its results; PBF, TPBF, Long Range, Area Fire, AFPh halving; Opportunity Fire; SW usage; Assault Fire; fire groups, their direction and mandatory formation; TEM; pins; Cowering; Encirclement | Squad Spraying Fire (7.34) and upper-level Encirclement (7.72) not built; fire at another level of the firer's own hex refused; a vehicle's fire at another level undecided; a multi-Location group's chain not checked; Opportunity Fire for small arms and MGs only | 33 (levels) |
| A8 | Defensive Fire Principles | Mostly built | Rules, Play, Units | First Fire by MF spent, the DEFENDER's window, Residual FP, Snap Shots, Subsequent First Fire, FPF with its NMC, Final Fire | **Mandatory TPBF against an entering unit (8.312) and the FPF restrictions (8.311) not found**; Snap Shots refused at wall, hedge, SMOKE, and rubble hexes and at a Bypass step; a vehicle fires once a Player Turn unless it keeps ROF | |
| A9 | Machine Guns and SW Malfunction | Partly | Rules, Play | MG usage by unit size, Multiple ROF, Sustained Fire, Spraying Fire, malfunction and MG repair | **MG fire against armor (9.6, 9.61) not built**; Mandatory Fire Direction and the 16-hex limit (9.4), Field of Fire (9.21), self-destruction (9.73), Random SW Destruction (9.74) not built; Fire Lanes simplified to a straight Hex Grain against entering Infantry (R12.7); dismantling for the German MMG only; mortar and Gun repair not built | 32 (MG against armor) |
| A10 | Morale | Mostly built | Rules, Play | MC and TC, LLMC and LLTC, breaking, rout with Low Crawl and Interdiction, Failure to Rout, DM, Rally, Self-Rally, Fate | Voluntary Break (10.41) and Voluntary Rout (10.711) not built; only Infantry Interdict and concealed units never do; Rally terrain bonus for woods and buildings only; Fanaticism by SSR not enforced | 36 (Rally terrain) |
| A11 | Close Combat | Mostly built | Rules, Play | Odds and Kill Numbers, Ambush, Melee, withdrawal, Infiltration, sequential CC, CC against vehicles, PAATC, capture of an unarmed vehicle | **Street Fighting (11.8) not built**; Infantry against Infantry in a Location holding a vehicle refused; odds above 10-1 refused; withdrawal refused when over IPC; Stealth for heroes only; no Ambush dr with a vehicle present (R11.16) | |
| A12 | Concealment | Mostly built | Play, Rules, Units | "?" at setup, gain and loss, Dummies, HIP by SSR, detection by entry, Mopping Up, a vehicle's entry with PAATC | **Searching (12.152) not built**; hidden Fortifications and vehicles refused; hidden Guns only Emplaced in Concealment Terrain; the loss of "?" always forced (R10.10, R31d.2); movement's read of who sees differs from fire's (backlog 51) | |
| A13 | Cavalry | Not built | | | No counter, no code, no backlog row | 42 |
| A14 | Snipers | Partly | Play | The Sniper attack on Personnel from fire, MC, and TC DRs; a concealed stack as one target; night SAN | Sniper Check and SAN reduction (14.4), attacks on vehicles (14.33) and on the enemy Sniper (14.31), setup placement not built; the Location is chosen by the game, not the player (R15.5); no LLMC after a Sniper hits a leader | |
| A15 | Heat of Battle | Mostly built | Rules, Play | The table, heroes, Battle Hardening, Fanatic, berserk and its charge, surrender and No Quarter | DRM for the eight catalog nationalities only; a hero firing a LATW, light mortar, or Gun refused; a charge the model cannot decide ends in place (R27.2); berserk terrain limits (15.45) unconfirmed | |
| A16 | Battlefield Integrity | Not built | Play (card check only) | The card's printed total is validated | No Casualty Tally, Integrity Check, or ELR change | none |
| A17 | Wounds | Mostly built | Rules, Units, Play | Occurrence, severity, morale and leadership one worse, 3 MF | **The Wound Severity dr is skipped in two paths** (a Casualty Reduction in the RtPh; a PF firer's): the SMC is wounded with no dr. Not in the backlog. A SMC wounded after spending more than 3 MF is not pinned; carrying a wounded man not built | |
| A18 | Field Promotions | Built | Rules, Play | Leader Creation on a Self-Rally Original 2 and a CC Original 2, with the table and its drm | The drm for CC against an AFV not built | |
| A19 | Unit Substitution | Mostly built | Rules, Units, Play | ELR by side and OB group, Replacement, Disruption, Green and Conscript units and their penalties | Ammunition Shortage (19.131) and an SSR ELR for underscored units (19.132) not built; a MG keeps its printed B# for Inexperienced users (R9.10) | |
| A20 | Prisoners | Mostly built | Play, Rules, Units | Surrender in the RtPh, capture in CC, Mopping Up, Guards, No Quarter, Massacre, escape and rearming, double VP | Scrounging (20.552), recapture by entry (20.54), the exchange of prisoners (20.221) not built; fire from within the Location refused; one Guard for all (R24.2); Massacre for Russian or berserk Infantry only | |
| A21 | Captured Equipment | Partly | Play, Rules | Captured MG, FT, and DC penalties; a squad of the Gun's own nationality manning it | **Captured Guns, ordnance, and vehicles cannot be used**; no Temporary Driver or Crew; "captured" is read as "of another nationality", so an ally's SW counts too (R13.7) | none |
| A22 | Flamethrowers and Molotov Cocktails | Mostly built, against Personnel | Rules, Play | FT fire in full against Personnel; MOL by SSR with its Check | **FT and MOL against an AFV refused** (22.34, 22.612); Flame and Kindling never placed (22.35, 22.613) | 43 (Flame); against AFVs none |
| A23 | Demolition Charges | Mostly built, against Personnel | Play, Rules | Placed and Thrown DC at the same level | **DC against an AFV refused** (23.5); Set DC (23.7) not built; Rubble, Flame, and Breach (23.41) never made; thrown only at the thrower's level | none |
| A24 | SMOKE | Partly | Play, Units | Infantry smoke grenades, their Hindrance and entry cost | **WP (24.3), ordnance SMOKE, SMOKE height and duration (24.4), Drift and Gusts (24.61, 24.62) not built**; grenades at other levels refused (R9.5) | 33 |
| A25 | Nationality Distinctions | Partly | Rules, Play, catalog | Counters and class progressions for eight nationalities; Commissars and the NKVD in full; Russian no-Deployment; British Cowering immunity; Finnish ranks and Self-Rally; Axis Minor Heat of Battle | SS, Human Wave, Partisans, Paratroops, Gurkha, ANZAC, Allied Minors, Ethiopians not built; Italian and Axis Minor Lax, 1PAATC, and no-escape not built; Finnish Stealth not built; no Japanese or Chinese | 37, 41, 42 |
| A26 | Victory Conditions | Mostly built | Play, Units | Control of hexes, buildings, and Locations; VP, CVP, Exit VP; Avoidance; Balance | Control forfeited to Fire (26.16) not built; Bridge and Pillbox hex Control not built; a vehicle's PRC neither gains nor prevents Control (R24.5) | 43 |

## 4. Chapter B: Terrain

For each terrain the game has up to four things to do: read it in LOS, cost it in movement, give its TEM or Hindrance in fire, and play its special rules. "Refused" below means refused in movement and as a fire target (limit 1 of section 2). LOS is read from VASL's terrain data for nearly every type, so LOS alone is not named as "built" in the rows that are refused.

| § | Title | Status | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|
| B1 | Open Ground | Built | LOS, movement, TEM 0, FFMO | | |
| B2 | Shellholes | Refused | | Movement and fire | 40 |
| B3 | Roads | Mostly built | Road rate, Road Bonus, vehicles' road costs, Mud on unpaved roads | The road rate is always taken (R10.1) | |
| B4 | Sunken Road | Refused | | Movement and fire; its LOS unconfirmed | 40 |
| B5 | Elevated Road | Refused | | Movement and fire; its LOS unconfirmed | 40 |
| B6 | Bridges | Not built | A bridge is a Hindrance in LOS | Movement and fire refused; no bridge rule in the planner | 40 |
| B7 | Runways | Refused | | Movement and fire | none |
| B8 | Sewers and Tunnels | Not built | | No Sewer Movement; cards record the sewer provision as text | 41 |
| B9 | Walls and Hedges | Mostly built | LOS; Infantry and vehicle crossing costs with Bog; wall and hedge TEM for Infantry fire; Wall Advantage | Wall Advantage is inferred from arrival order, not claimed (9.32, R10.6); **ordnance and vehicle fire refused at any hex with hexside terrain**; vertex LOS refused; Hull Down (9.36) not built; **Bocage (9.5) refused**; hillside walls and cactus hedge refused | 41 (bocage) |
| B10 | Hills | Mostly built | Levels and Blind Hexes in LOS; elevation costs for Infantry and vehicles; Height Advantage | Continuous Slopes refused; a Double-Crest off a road refused for vehicles; one base level a hex, no Crest Line inside it (10.11) | |
| B11 | Cliffs | Refused | Blind Hexes in LOS | Crossing refused for all; Climbing (11.4) not built; fire at a hex with a cliff hexside refused | 40 |
| B12 | Brush | Built | LOS Hindrance, costs, Hindrance in fire | Vineyard refused | |
| B13 | Woods | Mostly built | LOS, costs, vehicles' entry with Bog, TEM, Air Bursts | Trail Break (13.421) not built; paths, forest, and pine woods refused | |
| B14 | Orchard | Partly | Infantry movement; TEM 0 | **Vehicles are refused entry, though B14.4 gives the Open Ground cost**: a fault, not a recorded deviation. A LOS through orchard leaves fire undecided. The season is not tied to the scenario month | |
| B15 | Grain | Built | Season by scenario month in movement and fire | Refused when the game has no month | |
| B16 | Marsh | Mostly built | Infantry entry for the whole allotment; Hindrance at the same level; vehicles barred | Fire from a marsh hex refused (16.32) | |
| B17 | Crag | Refused | | Movement and fire; fire through it undecided | 40 |
| B18 | Graveyard | Refused | | The same | 40 |
| B19 | Gullies | Refused | Depression rules in LOS | Movement and fire | 40 |
| B20 | Streams and Crest Status | Refused | Depression rules in LOS | Movement and fire; depth, fords, Crest Status not built | 40 |
| B21 | Water Obstacles | Refused | | Movement and fire | 40 |
| B22 | Valley | Not built | | No code | none |
| B23 | Buildings | Partly | Ordinary buildings: LOS by height, upper levels, stairwells, TEM, Infantry fire between levels | **Cellars and rooftops refused; Rowhouses refused in movement and fire; Factories refused; Fortified Buildings not built in live play**; a vehicle may not enter a building; a mortar may not fire from one | 41 |
| B24 | Rubble | Partly | Printed rubble: LOS, costs, its building's TEM | **Rubble is never created** (24.11), does not fall (24.12), and is not cleared (24.7) | none |
| B25 | Fire | Mostly not built | A burning wreck's Blaze, its smoke, its entry cost; the Wind Change DR | **No Kindling, spread, terrain Blaze, or EC**; the EC SSR is not enforced on any card | 43 |
| B26 | Wire | Not built | | Counters placed and ignored (limit 6) | 37 |
| B27 | Entrenchments | Not built | LOS reads foxholes and trenches printed on a map | Counters ignored; no Entrenching, capacity, or TEM | 36 |
| B28 | Minefields | Not built | A counter's strength is checked for plausibility | No attack, clearance, or hidden minefield | 37 |
| B29 | Roadblocks | Not built | | Counter ignored; printed roadblocks refused | 36 |
| B30 | Pillboxes | Not built | | Vocabulary only | 36 |
| B31 | Village Terrain | Not built | | | none |
| B32 | Railroads | Refused | Embankment height in LOS | Movement and fire | none |
| B33 | Stream-Hex Terrain | Not built | | | 40 |
| B34 | Towers | Refused | | Movement and fire | none |
| B35 | Light Woods | Refused | LOS | Movement and fire | none |
| B36 | Prepared Fire Zone | Not built | | | none |
| B37 | Debris | Refused | | Movement and fire | none |

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
| PF, PSK, ATR | An AFV | Built |
| Bazooka, PIAT, MOL-P, ATMM | Anything | Not built: no counter, no code |
| Any fire | Passengers | Not built |

**What a tank can fire:** its main armament, in the PFPh, the DFPh, and the AFPh if it did not enter a new hex. Its MA does not fire as Bounding First Fire, in Motion, or as Intensive Fire. Its BMG and CMG do not fire at all outside an Overrun's FP and Close Combat.

| § | Title | Status | Lives in | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|---|
| C1 | Offboard Artillery | Not built | | | Everything: radio, battery access, SR, FFE | 38 |
| C2 | Gun Classifications | Mostly built | Rules | Crew, caliber, type, facing, Multiple ROF, range, M#, target size, malfunction | **Gun Duels (2.2401) not built; a malfunctioned Gun or MA cannot be repaired**; IFE (2.29) not built; a 360-degree mount refused | 32 |
| C3 | The To Hit Process | Mostly built | Rules, Play | Covered Arc, the three Target Types, Critical Hits, hull or turret, improbable hits | Area Target Type for light mortars only; Multiple Hits (3.8), Harassing Fire (3.75), WP (3.76), a hit against terrain (3.73) not built; Infantry and a vehicle in one Location must be attacked as the vehicle | 32 |
| C4 | Basic TH# Modifications | Mostly built | Rules | Barrel length, small calibers, APCR | APDS and SMOKE not built | 33 |
| C5 | Firer-Based DRM | Partly | Rules | Cases A, B, C3, D, F (Guns), H, I | **Cases C, C1, C2, C4 refused: no moving firer**; Case G Deliberate Immobilization not built; Case E only for a Gun at Infantry in its own Location; OVR Prevention (5.64) not built; a vehicle's Intensive Fire refused | 32 |
| C6 | Target-Based DRM | Mostly built | Rules, Play | Cases J to J4, K, L, M (Bore Sighting), N (Acquisition), P, R | Case O not built; Case Q without hexside TEM or Hull Down; Gyrostabilizer (6.55) not built | none |
| C7 | To Kill Tables | Mostly built | Rules, Play | Basic TK# less armor; the AP, APCR, HEAT, and HE tables; Shock; Immobilization; burning wrecks | Case B and Underbelly hits, indirect HE, FT and MOL, mortars, and DC against vehicles not built; aerial hits not built | 32 |
| C8 | Special Ammunition | Partly | Rules, Play | Depletion Numbers; APCR; HEAT by date | **APDS, Canister, SMOKE, WP, limited stowage, Elite +1 not built** | 33 |
| C9 | Mortars | Mostly built | Rules, Play | Light mortars as SW against Infantry: Area fire, Spotters, minimum range | Medium mortars have no counter; mortar SMOKE not built; firing from a building refused | 33 |
| C10 | Gun and Ammo Movement | Partly | Play | Towing, hooking up, unhooking, pushing with Manhandling, for QSU Guns | **Limbering (10.2) refused, so a non-QSU Gun cannot be moved**; En Portee (10.5) not built; a push only into Open Ground or grain | none |
| C11 | Guns as Targets | Partly | Rules, Play | Target size, Emplacement, gunshield, Direct Hit and Near Miss, destruction | A crew sharing its Location is refused as a target; AP at a Gun and an unmanned Gun not built | 32 |
| C12 | Recoilless Rifles | Not built | | | No counter, no code | none |
| C13 | Light Anti-Tank Weapons | Partly | Rules, Play | PF, PSK, ATR in full, with Backblast | **Bazooka, PIAT, MOL-P, ATMM not built**; a PF only at an AFV; Desperation fire refused | none |
| D1 | Vehicle Counters | Partly | Rules, Play | The values the five vehicles use: movement type, armor, turret type, ground pressure, target size, PP | Armored cars, partial armor, NT fire, optional armament not built | 32 |
| D2 | Vehicular Movement | Mostly built | Play, Units | MP, VCA, Start and Stop, Reverse, VBM, Motion, ESB, Mechanical Reliability, an enemy AFV's hex | Motion attempts (2.401) not built; Reverse Motion refused; movement only over the short terrain list | 33 |
| D3 | AFV Combat | Partly | Play, Rules | VCA and TCA, Target Facing, the halftrack's AAMG with Bounding First Fire | **The MA cannot fire while moving; BMG and CMG do not fire; no single fire marker for a tank's weapons; Armor Leaders (3.4) not built; MG against AFV (3.54) not built**; the turret turns only as part of a shot | 32 |
| D4 | Terrain and Anti-Vehicle Fire | Mostly not built | Rules | The target hex's TEM on a vehicle shot | **Hull Down (4.2) and Underbelly hits (4.3) not built** | none |
| D5 | Inherent Crew | Mostly built | Play, Rules | BU and CE, Stun, Recall, Abandonment by a failed TC, Crew Survival after ordnance | Voluntary Abandonment (5.4), entering an Abandoned vehicle (5.42), Brew Ups (5.7) not built; no Crew Survival after IFT, CC, or an Unconfirmed Kill | 32 |
| D6 | Transporting Personnel | Partly | Play, Units | Capacity, loading, unloading, entry loaded | **Riders and Bailing Out (6.2), all Passenger fire and morale (6.6, 6.7), fire at Passengers, their Survival (6.9) not built** | 33 |
| D7 | Overruns | Mostly built | Play, Rules | The OVR and its FP, Reaction Fire, CC Reaction Fire | OVR of a Location with a vehicle in Motion or of units in Melee refused; Gun crews (7.23) not built | |
| D8 | Immobilization and Bog | Mostly built | Play, Units | Immobilization, Bog Check and its DRM, Bog Removal, Mired | Bog TC (8.22) and towing out (8.32) not built | |
| D9 | Vehicles as Cover | Partly | Play, Rules | +1 TEM and +1 Hindrance from an AFV or wreck | Armored Assault (9.31), Dug-In AFVs (9.5) not built | none |
| D10 | Wrecks | Partly | Play | Creation, movement cost, cover | Removal (10.4) and Scrounging (10.5) not built | |
| D11 | Gyrostabilizers and Schuerzen | Not built | | | | none |
| D12 | Horse-Drawn Transport | Not built | | | | none |
| D13 | Vehicular Smoke Dispensers | Not built | | | | none |
| D14 | Radioless AFV | Not built | | | No Platoon Movement | none |
| D15 | Motorcycles and Bicycles | Not built | | | | none |
| D16 | DD Tanks and Amphibians | Not built | | | | none |

## 6. Chapters E, F, G, and H

| § | Title | Status | Built | Missing, refused, or departing | Pass |
|---|---|---|---|---|---|
| E1 | Night | Partly | NVR by SSR and its change, fire beyond NVR refused, the night LV DRM, Gunflashes, Starshells and Illumination, night movement costs, night rout and DM, "?" gained without a dr | **The Scenario Defender at night (1.2), Cloaking (1.4), Straying (1.53), Jitter Fire (1.55), Lax and Stealthy (1.6), IR (1.93), trip flares (1.95) not built**; ordnance at a Gunflash refused; the NVR is named by SSR, not rolled (R16.1); Acquisition at night kept (R16.2, against 1.74) | 33b |
| E2 | Interrogation | Not built | | | none |
| E3 | Weather | Partly | Mist, rain, falling and ground snow, Deep Snow, Mud, Extreme Winter, their movement and Bog costs, no SMOKE in rain or Mud | **EC, wind force and direction, Fog, Drifts, Ice, Winter Camouflage not built**; a Gust is rolled and nothing reads it; the weather is named by SSR, not rolled (R16.9) | 33b |
| E4 | Ski Troops | Not built | | | 42 |
| E5 | Boats | Not built | | | 42 |
| E6 | Swimming | Not built | | | none |
| E7 | Air Support | Not built | | | 39 |
| E8 | Gliders | Not built | | | 42 |
| E9 | Paratroop Landings | Not built | | | 42 |
| E10 | Ammo Vehicles | Not built | | | none |
| E11 | Convoys | Not built | | | none |
| E12 | Barrage | Not built | | | none |
| F | North Africa (13 sections) | Not built | LOS reads slopes and hillocks as VASL does | Every desert terrain refused in play; Dust and Heat Haze not built | none (the user's ruling of 2026-09-30) |
| G | Pacific Theatre (18 sections) | Not built | LOS reads jungle, bamboo, palm trees, huts | No Japanese or Chinese counters or rules; caves, landing craft, beaches absent. Hand-to-Hand CC is built from an SSR, not from G1.64 | none (the same ruling) |
| H | Design Your Own | Not built | The catalog carries BPV for 151 of 173 counters (none for vehicles, MGs, or other SW) | No purchase, roster, or ELR Chart | 34, 35 (deferred) |

## 7. What no planned pass covers

Section 22 of the plan lists eleven rule passes. These gaps are in none of them. Some are small and some are whole sections.

**Whole sections:** A16 Battlefield Integrity; B7 Runways, B22 Valley, B31 Village Terrain, B32 Railroads, B34 Towers, B35 Light Woods, B36 Prepared Fire Zone, B37 Debris; C12 Recoilless Rifles; D4 Hull Down and Underbelly hits, D11 to D16; E2 Interrogation, E6 Swimming, E10 Ammo Vehicles, E11 Convoys, E12 Barrage; Chapters F and G.

**Inside sections that are otherwise built:**

- **Infantry:** Dash (A4.63); Infantry OVR beyond one case (A4.15); overstacking penalties on small arms fire (A5.12, A5.131); mandatory TPBF and the FPF restrictions (A8.311, A8.312); MG fire direction, Field of Fire, and SW destruction (A9.4, A9.21, A9.73, A9.74); Voluntary Break and Rout (A10.41, A10.711); Street Fighting (A11.8); Searching (A12.152); Squad Spraying Fire (A7.34).
- **Special weapons and units:** FT, MOL, and DC against an AFV (A22.34, A22.612, A23.5), which the plan's pass 32 does not name; Set DC (A23.7); Rubble and Breach from a DC (A23.41) and rubble's creation and clearance (B24.11, B24.7); Sniper Check and Sniper attacks on vehicles (A14.4, A14.33); Scrounging (A20.552, D10.5); captured Guns and vehicles with Temporary Crews (A21.13 to A21.22); Human Wave, SS, Gurkha (A25).
- **Terrain already on the list:** an orchard for vehicles (B14.4); Hindrances other than brush, grain, and marsh (A6.7); Wall Advantage as a player's claim (B9.32); Continuous Slopes and Crest Lines (B10); Trail Breaks (B13.421).
- **Ordnance and vehicles:** bazooka, PIAT, MOL-P, ATMM (C13.4 to C13.7); Gun and MA repair; limbering and En Portee (C10.2, C10.5); Case O and Hull Down in the To Hit DRM (C6); Deliberate Immobilization (C5.7); Armored Assault (D9.31); wreck removal (D10.4).
- **Night and weather:** EC and wind as states the game reads are in pass 43 only as far as Fire needs them.

## 8. Found by this assessment and not in the backlog

| Finding | Where | What it is |
|---|---|---|
| A wounded SMC with no Wound Severity dr | `GamePlanner.Rout.cs`, the `CasualtyReduction` helper; the PF firer's Casualty Reduction in `GamePlanner.Ordnance.cs` | A17.11's dr is rolled in Fire, CC, Rally, and Sniper attacks and skipped in these two paths. Checked in the code by me. |
| Vehicles refused entry to an orchard | `GamePlanner.VehicleTerrain.cs`, the vehicle cost table | Orchard is missing from the table, so the message says "not allowed", where B14.4 gives the Open Ground cost. A fault. |
| Fortification counters placed and ignored | No reader in Play or Rules (checked by a search for the six counter kinds) | Wire, foxholes, trenches, mines, roadblocks, pillboxes have no effect and no refusal. A card that fields them would play wrongly without saying so. |
| A Gust is rolled and unread | `GamePlanner.Night.cs`, the Wind Change DR | The event records it; no SMOKE or fire rule reads it. |
| A tank's Defensive First Fire with its MA | `GamePlanner.Ordnance.cs` against the Ordnance package's own exclusion list | The code admits it and the package's list says it is out; no test was found. Unconfirmed which is right. |
| Stale backlog rows | Backlog sections 1 to 15 | Some early rows describe gaps that later passes closed (vehicle CC, TI placement). The backlog overstates what is open there. |

**Unconfirmed subsections**, not counted as built: A2.3 half-hexes at setup; A5.4 and A5.6; A6.5 reciprocity; A7.54, A7.211, A7.33, A7.371; A8.14, A8.25 (ordnance Residual FP: the code and backlog section 1 disagree), A8.221, A8.23, A8.24, A8.41; A9.51; A11.41, A11.612, A11.622; A12.151, A12.42; A15.45; A19.12 (whether Disrupted units stay put); A21.2 (capture of an Abandoned AFV); A25.01 Assault Engineers; A26.21; B4 and B5 LOS; B16 FFMO in marsh; B22 negative levels in LOS; B23.9 in the bounded packages; C6.44, C6.52, C6.53, C6.58; C11.1, C11.51; C13.26, C13.9; D2.18, D2.321, D2.38; D7.14, D7.221; D8.4, D8.5; whether Japanese units are refused in Heat of Battle.

## 9. What this means for the rule passes

- **The plan's order still fits what is missing.** The largest played-with gap is armor in motion and a tank's other weapons (C5 Case C, D3), which is passes 32 and 33.
- **Terrain is the widest gap by section count** (25 of Chapter B's 37 sections are refused or not built), and it is passes 40 and 41, late in the order. Until then the game plays on boards, or parts of boards, that hold only the short list.
- **Fortifications want attention before they are built**, since their counters are ignored today. A refusal for a placed fortification counter would be a small and honest stopgap.
- **Nearly half of what is missing has no pass.** Of the 47 sections of Chapters A to E that are refused, not built, or mostly not built, 22 are in no planned pass, and section 7 adds the gaps inside built sections. That is the list to plan from: either new passes, or a ruling that a rule stays out.
- **The Infantry gaps inside built sections are small each and many.** Dash, Searching, Street Fighting, Voluntary Break, the overstacking penalty on fire, and mandatory TPBF have no pass, and together they would make one.
