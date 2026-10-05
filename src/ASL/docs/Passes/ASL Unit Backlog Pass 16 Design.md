# ASL Unit Backlog Pass 16 Design

**Status:** Built. Backlog pass 16 (night and weather), as the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 3, sets out. The scenario cards, first planned here, are pass 17.

**Date:** 2026-09-29

**Requirements:** [ASL Unit Requirements](<../Requirements/LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13.

**Related documents:** the [Scenario A1 Backlog Pass 16 Review](<Scenario A1 Backlog Pass 16 Review 2026-09-29.md>) (the review stage, with the referee's and the table player's findings), the [ASL Unit Backlog Pass 15 Design](<ASL Unit Backlog Pass 15 Design.md>), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 26.

Rulings R16.1 to R16.14 are in the plan, section 5. Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

- **SSRs (R16.1, R16.9).** A new game names night and weather among its special rules: `night:n` (the Base NVR), `night-clouds:` and `night-moon:` (the sky), `weather:` with `overcast`, `gusty`, `mist`, `rain`, `heavy-rain`, `mud`, `falling-snow`, `ground-snow`, `deep-snow`, or `extreme-winter`, and `plowed-roads`. Setup refuses Fog and the other weather not built, combinations the charts never make, a Base NVR out of its limits, and Extreme Winter without the scenario's month and year.
- **Sight at night (R16.2).** A Location beyond the firer's NVR is out of its LOS unless Illuminated or marked by a Gunflash (a fire counter or a Melee there); fire at a Gunflash beyond NVR is Area Fire, halved once. An Illuminated firer sees only Illuminated Locations and Gunflashes. A moving vehicle is within NVR at 1.5 times it (twice if tracked; 1 or 2 hexes at an NVR of 0). A BU AFV's NVR is halved, for its MA too. At night First and Final Fire counters stay until the end of the AFPh. Ordnance at a Gunflash beyond NVR is refused (not built).
- **The Low Visibility DRM (R16.3, R16.11).** +1 at night, and for Mist, rain, and Falling Snow +1 per six hexes beyond six (heavy precipitation +1 more), uncapped, blocking the LOS at 6 with the other Hindrances; never in the firer's own hex, with Height Advantage, or where the target hex tops out a full level above every firer (night), nor for Residual FP, DC, or Fire Lanes. It never negates FFMO. An ordnance TH DR takes it as a Case R Hindrance of its own, which cancels neither FFMO nor the Open Ground cases. No fire group spans Locations at night.
- **Concealment (R16.4).** At night a mover loses "?" only by Non-Assault Movement in an Illuminated Location (or by entering an enemy Location); an advance keeps it. A unit that would need a Concealment dr gains "?" without one, and an enemy sees it only within its NVR or Illuminated.
- **Movement (R16.5, R16.12, R16.13).** At night one MF more into Concealment Terrain and one MP more per hexside for a vehicle; no Double Time or road bonus with an NVR of 0. A stairwell move at night costs one MF more too, and a BU AFV whose NVR is 0 only Stops. In and after rain (any rain the game has had) one MF or MP more per level changed. In Mud half an MF (one MP) more into Open Ground, the road rate lost on unpaved roads (a vehicle pays the Open Ground COT there). In Ground Snow one MF more per level changed and the road bonus only on plowed roads; a vehicle's road entry is at least one MP, and non-tracked vehicles pay one more per hexside. Deep Snow adds half an MF per hexside for Infantry (not into woods, buildings, or rubble, nor across a plowed road) and one or two MP for vehicles. A Bog DR takes +1 for Mud or snow and +1 more for Deep Snow, not in a building. No SMOKE grenades in rain, Mud, or Deep Snow but inside a building.
- **Rout, surrender, and DM (R16.6).** At night a broken unit Low Crawls (out of an enemy Location and into marsh too), is never eliminated for Failure to Rout, and surrenders only in CC. DM stays after a RPh with no Rally Original DR at most the printed morale.
- **Other night rules (R16.7).** +1 to Recovery, SAN two higher (at most 7), and the ATTACKER's Ambush needs a Final dr only two lower unless Illuminated.
- **Starshells (R16.8).** `asl.game.fire-starshell`: a leader, CE AFV, or MMC, one attempt per hex per phase, a Usage dr of 4 or less (leader) or 2 or less, one of three placement methods, a Random Direction walk; after the Player Turn of the first one, a firer other than a leader fires it only before other fire or movement; an enemy counts as seen only within NVR or Illuminated; the Starshell Illuminates three hexes until the end of the CCPh. A burning wreck Illuminates two hexes.
- **The Wind Change DR (R16.1, R16.10).** At the start of each RPh after the opening Player Turn, when night or changing weather is in effect, the game makes it: a colored 6 changes the Base NVR (a further dr with Scattered clouds and a moon); 10 or more starts or intensifies rain or snowfall, 3 or less ends it; 10 or more is a Gust in Gusty weather.
- **Extreme Winter (R16.14).** With a snow condition named, B# and X# of every weapon but a DC are one lower for Russians before April 1941 and two lower for Axis but Finns (the Japanese included) before April 1942; such a unit's Original Rally DR of 11 outside a building is Fate.

## 2. The packages

The Fire package is revised with its prior manifest digest kept (`0b69f01b...`, prior `f4ce61fa...`): four facts (`lowVisibilityDrm`, `beyondNvr`, `cushionedOpenGround`, `breakdownReduction`), four rulings, four cases, and 13 new source fragments. The Rally package (`68c60503...`, prior `3eb6e214...`) adds `extremeWinterFate`; the Close Combat package (`0a6875cc...`, prior `7ca1ea7c...`) the night Ambush's `darkNight`; the Ordnance package (`5f21cf2b...`, prior `b10deb34...`) the Low Visibility Case R term, the cushion on the TH DR, and the Extreme Winter B#. The pass 16 PDF comparison (`asl-scenario-a1.pass16-pdf-comparison.json`) (`7318898a...`) registers 72 fragments of E1, E3, B25.65, and B.8.

## 3. The game model and records

- `GameState.Nvr`, `Precipitation`, `Rained`, `StarshellUsed`, `StarshellTurn`, and `StarshellAttempts`; the SSRs' Base NVR and precipitation at the start.
- The records `wind-changed` (the Wind Change DR and what it set) and `starshell-fired` (the attempt, its rolls, and the Starshell placed); the vocabulary 1.15.0 adds the `asl:starshell` marker, removed at the end of each CCPh.
- The fire, ordnance, Ambush, and Rally records carry the new facts; the verifiers take the map and weather facts as recorded.

## 4. The Play page

- The new game form takes the special rules.
- A line under the game summary names the NVR, the weather, the precipitation, and the Starshells.
- A Starshells panel fires one at night; a Night and weather list shows each Wind Change DR and Starshell.
