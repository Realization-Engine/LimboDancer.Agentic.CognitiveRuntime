# Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis

Date: 1 November 2026

Status: research and proposal. Nothing here is built by this document, and no ruling is made. The rulings in section 9 need the user's word before a design follows. The design would follow the pattern of the [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>).

## 1. Question and conclusion

The user asked (2026-11-01) how combat intelligence was gathered in WWII Europe, and how the project could make it a game mechanic at three levels: the Division (the Campaign), the Company (the Situation handed to ASL as a Scenario Card), and the Squad (ASL play).

Conclusions:

1. **ASL has more precedent than expected, but only for the last hour before the fight.** The rulebook hides units (concealment, HIP, Dummies, hidden Fortifications and mines) and reveals them by contact (LOS, Search, Mopping Up, Snipers, Gunflash, Starshells). It also prints three intelligence rules proper: E1.23 Recon (page 223), a pre-setup reveal bought by SSR or DYO; E2 Interrogation of prisoners (page 228); and E2.4 Civilians, whose Information Table includes the rulebook's only "False Information" result. None of the three is built.
2. **ASL models no intelligence about the enemy's order of battle, identity, or intentions.** Every OB is printed and read by both players. Nothing ages, nothing is partly true, and nothing carries between scenarios in Chapters A to H (the module Campaign Games' Recon rules, P8.622 and O11.622, are cited by the index but their texts are not in the eASLRB).
3. **The Campaign is the right owner of real intelligence.** History puts most collection, all processing, and all of the estimate at division and above (section 3). What reached a company commander was an overlay, a few lines of enemy information, and the last night's patrol reports. The Campaign should hold each side's picture of the enemy and hand down a filtered, possibly wrong briefing; ASL should keep deciding what happens on the board.
4. **The handoff can turn intelligence into existing ASL switches.** The quality of a side's picture can decide the card's HIP allowance (`hip:<side>:<n>`, built), its Dummies (built), whether the attacker gets an E1.23 Recon dr, and whether E2.4 Civilians are in force. A private briefing per side carries the rest.
5. **Radios become the channel of intelligence** (the user, 2026-11-01). In ASL today a radio matters only for OBA (C1.2 to C1.23) and for radio AFVs (D14, E1.21). In the layered game it is also how what a company saw reaches its headquarters, and how orders and the briefing reach a company's units. The radios and field phones a company has, and whether they survive, can decide how much of a scenario's knowledge returns to the Campaign and how fast (section 7.5).
6. **The briefing goes to one leader** (the user, 2026-11-01). The Campaign's intelligence reaches the side's senior leader, and from him spreads to other units by proximity and by radio, as an orders group did. ASL's player is omniscient over his own units (A12.1), so this can only act on what units may do, not on what the player knows. The rulebook's model for that is E1.21 Freedom of Movement (page 223), and a card SSR modeled on it is the proposal (section 7.6).
7. **The repository already has the discipline the Campaign lacks.** ASL's per-side `GameView`, sealed "?" handles, and per-fact disclosure (`ContractDisclosure`) are the model to copy. The Campaign today is omniscient: every view sees everything, and the Goodwood handoff hard-codes `Concealed` and `Hidden` to false (`SituationHandoff.cs:69`).

## 2. Method

- **History.** Web research on US, British, and German practice (FM 100-5 on EEI, FM 7-25 on the regimental I&R platoon, FM 2-30 cavalry reconnaissance, the 1943 Intelligence Bulletin on German reconnaissance, the Australian Army Journal on the capture of German intercept company 621, CMH's Siegfried Line Campaign, Panzerarmee Afrika Ic reports, and secondary works on Ultra, Arnhem, the Ardennes, Mortain, Kasserine, Sedan, Phantom, and the Ghost Army). Several primary sources could not be reached (the NSA Ardennes SIGINT study, the RAF Arnhem air reconnaissance study, the full text of FM 30-5 of 1940). Section 3 marks what rests on standard historiography rather than a source read in this session.
- **Rulebook.** A scan of every page of the registered `eASLRB_v3_01.pdf` (716 pages) for concealment, HIP, Dummy, Search, Mopping Up, Interrogation, Civilians, Recon, Observer, Spotting, Sighting, Starshells, Gunflash, LOS check, Pre-Registered Fire, Bore Sighting, Cloaking, Freedom of Movement, and the index entries for each. E1.23 and E2 were then read on their pages. Page numbers are physical PDF pages.
- **Repository.** The Rule Inventory A to E, the Rules project's calculators, the Units and Play projects' view code, `src/Campaign`, `prototypes/europe-hex-map`, and the Campaign analyses that mention intelligence.

## 3. How intelligence worked, 1939 to 1945

### 3.1 The cycle

US doctrine (FM 100-5, FM 30-5) built intelligence around the commander's **essential elements of information** (EEI): what he must know about the enemy, the ground he does not hold, and the weather. The G-2 (division and up) or S-2 (regiment and battalion) turned the EEI into a collection plan, tasked agencies, kept the situation map and journal, and issued a periodic report (usually daily), spot reports for urgent items, and an estimate listing the enemy's capabilities for each new operation. Doctrine told G-2s to list capabilities rather than guess intentions; in practice estimates leaned toward intentions.

The German equivalent was the Ic, the third general staff officer at division and above, who reported the enemy situation daily up to Fremde Heere West at OKH. Panzerarmee Afrika's surviving Ic reports (1941 to 1942) are a page each, drawn from combat reports, prisoners, intercept, and air reconnaissance, and their editor notes how often the day's picture was wrong. German doctrine (H.Dv. 300, Truppenführung) split reconnaissance into operational (air and higher echelons), tactical (reconnaissance battalions), and battle reconnaissance (every unit in contact). *(The German doctrinal terms rest on standard historiography, not a source read this session.)*

### 3.2 How information moved

- **Upward**, fast at low echelons: spot reports by phone or radio, then journals and periodic reports.
- **Downward**, thin and late. Army and corps summaries reached division daily; division sent regiments an overlay and a summary; Ultra stopped at army headquarters and could not be cited in orders.
- **Sideways**, through liaison officers, and a common gap.

What reached a company commander before an attack was typically an operations overlay (boundaries, phase lines, objective), a few lines such as "elements of the 352nd Division, strength unknown, MG positions reported at...", perhaps one air photo, and whatever patrols had found the night before. A squad leader got a verbal briefing and terrain pointed out. German patrol orders were verbal, and men going forward were forbidden to carry written orders or marked maps (Intelligence Bulletin, May 1943).

The pattern for a game: **information loses grain as it moves down** (identity, then strength, then exact location) **and gains age**.

### 3.3 Collection means

| Means | Owner | Yields | Lag | Typical errors |
|---|---|---|---|---|
| Patrols (reconnaissance, or combat patrol "to bring back a prisoner") | Company, battalion | Forward positions, MGs, wire; identity only from a prisoner or a body | Overnight | Going to ground short of the objective; overestimating fire; strongpoints taken for outposts |
| Observation and listening posts; the regimental I&R platoon | Company to regiment | Movement and fire in view | Minutes | Narrow view; night; misread noise |
| Reconnaissance troops and squadrons; the German Aufklärungsabteilung | Division, corps | The contact line, open roads, where the enemy is not | One to six hours | Stopped by screens; little identification |
| Reconnaissance by fire | Platoon, company (armor, cavalry) | Hidden positions that answer | Immediate | Gives away the firer; costs ammunition; the enemy may hold fire |
| Interrogation of prisoners (US IPW teams, the "Ritchie Boys"), captured documents | Regiment to army | Unit identity, parent formation, strength, morale, sometimes plans | Hours to a day | Lies; prisoners from the rear misdescribe the front |
| Tactical signals intelligence and direction-finding (German Horchkompanien; British Y service) | Corps to army group | Identity and location from call signs and DF; intentions from careless voice traffic | Minutes to hours | Silence read as absence; dummy traffic |
| Ultra | Army group, army (rarely corps) | Orders, tank returns, supply states | Hours to days | Landlines and radio silence leave nothing to read; commanders explain away what does not fit |
| Air photographs and tactical air reconnaissance | Army, corps; prints to division | Positions, gun pits, vehicle parks, movement, bridging | Hours to two days | Camouflage, weather, dummies |
| Artillery air observation (L-4 Cubs, Austers) | Artillery battalion, division artillery | Observed fire, batteries behind the crest | Minutes | Weather, flak |
| Sound ranging and flash spotting | Corps, army | Enemy battery locations | Minutes to an hour | Weak against mortars; terrain, wind |
| Civilians and the resistance (FFI, the Dutch resistance) | All | Local detail, guides, bridges, mines | Hours | Penetrated networks; rumor |
| Phantom (GHQ Liaison Regiment) | Army group | Where one's own front actually is | Under an hour | Not an enemy source |

Two collection facts matter for design. First, US cavalry was organized in 1943 to fight for information after North Africa showed the limits of "sneak and peek". Second, security was a variable each side set: the capture of company 621 at Tel el Eisa (10 July 1942) showed the British how much their radio procedure had given away, and they tightened it within weeks.

### 3.4 Deception and security

- **Fortitude South and Quicksilver (1944):** a notional army group kept the German Fifteenth Army at the Pas de Calais for weeks after D-Day.
- **The Ghost Army (23rd Headquarters Special Troops):** spoof radio, recorded sound, and dummies, simulating divisions (Operation Viersen, March 1945).
- **The German Ardennes deception (1944):** a defensive codename, radio silence, landlines, night moves.
- **Counter-intelligence:** radio discipline, call-sign changes, painted-out unit markings, and the rule against carrying orders forward.

### 3.5 Radios, wire, runners, and the orders group

*(This subsection rests on standard historiography, not on sources read in this session.)*

- **Who had radios.** Every German tank had a receiver from 1940, and command tanks transmitters, which let panzer platoons act together; most French tanks of 1940 had none, and most Soviet T-34s had none until 1943. ASL's D14 Radioless AFV models the cost. A US infantry company of 1944 had a few SCR-536 "handie-talkies" and, at battalion and company, the SCR-300 backpack set; a German infantry company relied mostly on runners and field telephones. Forward observers carried their own sets.
- **Wire and runners.** In the defense, and in any position held for a day, field telephone wire was the main link: secure from intercept, but cut by shellfire. In the attack, runners carried most messages below battalion, and a runner who was hit took his message with him.
- **Radios carried reports up.** Spot reports from patrols, OPs, and forward observers went back by radio; a unit without a working set reported only when a runner or the unit itself returned.
- **Radios also leaked.** Careless voice traffic gave German intercept companies identities and intentions (section 3.3), and a captured set with its codes and call-sign lists was itself intelligence.
- **The orders group.** Orders went down in steps: battalion briefed its company commanders at an orders group (the British "O Group", the German Befehlsausgabe), who briefed their platoon leaders, who briefed their squads. Each step took time and lost detail. When orders came late, a company attacked with only its commander and the leaders near him knowing the plan, and the rest followed. German mission tactics (Auftragstaktik) tried to answer this by passing down the commander's intent, so subordinates could act when they lost contact.

### 3.6 Failure patterns the game should produce, not script

1. **Silence read as absence.** The Ardennes, December 1944: radio silence starved Ultra; the indicators that existed were not added up.
2. **A prior belief overriding evidence.** Arnhem, September 1944: the Dutch resistance and Major Brian Urquhart's low-level photographs showed armor near Arnhem, and both were discounted. Kasserine, February 1943: Ultra was read as pointing north, and the blows came at Faïd and Sidi bou Zid. Sedan, May 1940: French air reconnaissance saw the columns in the Ardennes; the command believed tanks needed two weeks to reach the Meuse.
3. **Counting right, misjudging significance.** The Hürtgen, November 1944: the enemy to the front was counted, but the meaning of Schmidt and the Roer dams was missed.
4. **The warning arriving with the attack.** Mortain, August 1944 *(the timing of the Ultra warning is disputed in the sources)*.
5. **Deception inflating the enemy.** FUSAG.

None of these is a failure to collect. Each is the commander's reading of a picture. A game produces them when the player holds an uncertain, aging, partly false picture and must decide on it.

### 3.7 How other games handle it

ASL itself: concealment, Dummies, HIP. Panzer Leader: units are on the map in plain sight; spotting decides only who may be fired on *(not checked against the rules this session; the repository's Panzer Leader material has no spotting rules)*. Kriegsspiel and double-blind games: separate maps and an umpire who reports only what each side could see. Combat Mission: contact markers and information that travels only along command links. Command Ops: reports arrive late and wrong, and orders take time to go down. Flashpoint Campaigns: detection levels (unknown, suspected, identified). ASL's own footnote to A12.1 (A12 footnote 18, pages 101 to 102) names the referee as the "most realistic" way to play concealment; the project's adjudicator perspective is that referee.

## 4. What the ASL rulebook already provides

### 4.1 Hiding

| Rule | Page | What it hides | State |
|---|---|---|---|
| A12.1 to A12.13 Concealment | 76 to 77 | What is under a "?"; fire at a concealed unit is halved as Area Fire | Built |
| A12.11 Dummies | 76 to 77 | False stacks, created only at setup or on entry | Partly (Pass 120) |
| A12.12 Placement | 77 | The first side sets up out of the opponent's sight; non-OB "?" for units out of enemy LOS | Built |
| A12.3 to A12.32 HIP | 80 | Units recorded off board in Concealment Terrain | Built (the `hip:<side>:<n>` token, ruling R23.5) |
| A12.33 Hidden Fortifications | 80 | Fortifications until used, entered, or seen | Refused (Pass 115) |
| A12.34 Hidden Guns | 80 | An Emplaced Gun, revealed or only concealed when it fires | Built with a deviation |
| A2.9 | 47 | No enemy stack inspected before play | Partly (Pass 125) |
| B28.1, B28.47, B28.48 Minefields, Dummy and hidden mines | 148 to 149 | Mine type, strength, presence | Partly; dummy and hidden mines not built |
| E1.2, E1.16, E1.3, E1.4 Night: defender HIP and Dummies, hidden Fortifications, night "?", Cloaking | 223 to 224 | Much more at night | Night "?" mostly built; the rest not built |

### 4.2 Revealing by contact

| Rule | Page | How it reveals | State |
|---|---|---|---|
| A6.11 LOS checks | 53 | No speculative LOS checks before an attack is declared; probing sight lines costs a declared attack | Built with a deviation |
| A12.14 "?" loss | 77 to 78 | Movement, Open Ground, firing, CC, failed MC, Sniper hit, in a Good Order enemy's LOS within 16 hexes | Built (the proving slice) |
| A12.15 Detection | 78 | Moving into a concealed Location forces a reveal | Partly (Pass 73) |
| A12.152 Search, A12.154 Search casualties | 79 | A MMC spends 1 MF, becomes TI, reveals "?", HIP, minefields; the defender may inflict Casualty Reduction | Partly (Pass 120) |
| A12.153 Mopping Up | 79 | A MMC clears a building of hidden units and Dummies | Partly (Pass 135) |
| A14 Snipers | 82 to 83 | A Sniper on a concealed stack forces its owner to declare its targets; Dummies are eliminated | Mostly built |
| E1.8 Gunflash | 226 | At night a firer's Location (not identity) is shown to all with LOS | Partly (claim built) |
| E1.9 to E1.93 Illumination, Starshells, IR | 226 to 227 | Make Locations visible | Mixed |

### 4.3 Intelligence rules proper

**E1.23 Recon (page 223), not built (Pass 90).** By SSR or DYO purchase, the Scenario Attacker makes one Recon dr after the Defender has set up and before his own setup. The Final dr is the number of hexes, of the Attacker's choice, in which the Defender must reveal what he has: hidden units are placed concealed, concealed units lose their "?" and may be inspected regardless of LOS, and hidden Fortifications are placed (not mine type or strength, Fortified Buildings, or tunnels). The drm depend on the Attacker's majority squad type (+1 for British, Partisans, Russians, Japanese, Finns, and for Stealthy; 0 for Germans and U.S.; -1 for all others and for Lax). Its DYO price is 10 BPV (H1.6, page 334). This is the closest published model of a reconnaissance effort: a point-in-time reveal of a number of hexes, bought before play.

**E2 Interrogation (page 228), not built (Pass 74), and no calculator exists.** Every captured Personnel unit is interrogated at once, by one DR that can never be remade. The table: 6 or more no effect; 5 the closest concealed hex loses its "?"; 4 the closest hidden units are placed under "?"; 3 the closest hidden Fortification is revealed, with mine type and strength; 2 or less Defenses Compromised (all three). DRM: -1 if the prisoner is a squad, -1 if Inexperienced; +1 for its heroic DRM, if Elite, if an SMC, or if the guard is Inexperienced. E2.3: nothing is revealed that is both out of the prisoner's LOS and more than eight hexes away.

**E2.4 Civilians (page 228), not built.** By SSR, as part of the Wind Change DR (B25.65): a DR of 3 gives the ATTACKER information in a hostile or neutral country, 4 in a friendly one (France and Italy are friendly to the Allies, hostile to the Germans). The information reaches Good Order units at a Random Location from the Attacker's Sniper counter, and a dr on the Information Table decides it (+1 in a hostile country, -1 in a friendly one): 0 Defenses Compromised; 1 or 2 hidden Fortifications; 3 or 4 HIP units; 5 or 6 concealed units; **7 False Information, the informed units become TI**. The index cites civilian-only interrogation in two modules (SSR KGP18, SSR PB21).

### 4.4 Pre-game knowledge of the ground, and observation

- **C1.73 Pre-Registered Fire** (page 166, not built, Pass 155) and **C6.4 Bore Sighting** (page 174, built in part): ground studied before the fight. A6.11 lets the side setting up first check LOS before setup only if it is the Defender or holds one of these.
- **E1.21 Freedom of Movement** (page 223, not built, Pass 90): in a night scenario each on-board unit of the Scenario Defender may move or advance only once it has been attacked (other than by OBA or a Sniper) or has seen a Known enemy unit. Once the Attacker has resolved an attack other than a successful Ambush, the Defender's single best leader (by Random Selection if tied), in Good Order, gains Freedom of Movement on a dr below the ELR, once per friendly MPh. A Good Order SMC that may move frees any unit that begins the MPh in its stack. If one AFV with a radio is freed, every friendly Mobile AFV with a radio is freed too. Freedom, once gained, lasts the scenario. It is the one rule in which ignorance limits action, and the one that spreads permission to act by proximity to a leader and by radio.
- **C1.2 Radio Contact, C1.22 Maintaining Radio Contact, C1.23 Field Phones** (page 163, not built, Passes 150 and 155): only an Observer, a Good Order leader possessing a functioning radio or field phone, may attempt Contact, and only at the start of the PFPh or DFPh as his sole action. A radio is a 1PP SW that a leader must possess to use, and it breaks down on an Original Contact or Maintenance DR of 12. A Field Phone passes Contact on 11 or less, cannot be moved or repaired, and is eliminated by enemy troops or HE in its prerecorded Security Area (a line of hexes to the friendly board edge, the wire). Today these rules exist only for OBA.
- **C1.21 Battery Access, C1.6 Observer, C1.63 Offboard Observer** (pages 163 to 166, not built): C1.21 draws an extra chit when the Observer sees no Known enemy, the cost of uncertainty in a die mechanic. C1.6 lets an Observer see through concealment in non-Concealment Terrain, but only for his OBA actions.
- **D14 Radioless AFV** (page 212, not built, Pass 105): every AFV has an inherent radio unless its counter says otherwise; radioless AFV must move in platoons of two or three (D14.2), and a radio counter cannot replace an AFV's missing inherent radio (D14.1).
- **A10.7 Leadership** (page 68, built): a leader's modifier acts on units in his Location. ASL has no command radius and no orders; outside E1.21 and D14, every unit may act every phase.
- **E7.3 Sighting TC, E7.6 Observation Plane** (pages 237 to 239, not built, Pass 160).
- **W9.11 TACP and FAC** (pages 666 to 667, not audited).

### 4.5 What the rulebook does not model

- The enemy's order of battle and identity: both are printed and public. The only exceptions are optional: secret DYO purchases (H1.1, footnote 1, page 336) and the referee of A12's footnote 18.
- Intentions and plans.
- Patrols as an action before setup: E1.23 is one abstract dr; no unit moves and nothing is risked.
- Intelligence that lasts between scenarios (possibly in the module Campaign Games, whose texts we do not have).
- Signals intercept, codes, jamming.
- Partial or aged reports. Information is true or absent; E2.4's result 7 and Dummies are the only falsehoods.
- Knowledge inside one side: the ASL player is omniscient about his own units, and sees what any of them sees (E2.41 and E1.21 are the exceptions).
- Orders and their delivery: no rule makes a unit wait for orders by day, and radios carry nothing but OBA.

## 5. What the repository already provides

### 5.1 The ASL knowledge model

- `Perspective` (`Units/State/GameTypes.cs`): a viewer is a side or the adjudicator, who sees everything.
- `GameEvent.Visibility` (`Units/State/GameEvent.cs`): each event names who may see it.
- `GameView.Of` (`Units/State/GameView.cs`): a side's view leaves out what it cannot know rather than marking it. An enemy's HIP units are absent; concealed units and Dummies become a `SealedPresence`, a "?" handle numbered by Location that never carries the unit's id.
- `ScenarioA1Visibility` and `ScenarioA1Sight` (Rules): the verdicts behind the view, including the three purposes of sight (forcing "?" loss, denying "?" gain, inspection).
- `ContractDisclosure { All, Owner, Opponent, Adjudicator }` (`Rules/ScenarioA1RuleContractModels.cs:26`): every fact of every rule contract declares who may know it, and the Rule Workbench and Play read facts only through the viewer's view.
- Setup plans (`Play/ScenarioSetupPlans.cs`): a placement may be `Dummy`, `Concealed`, `Hidden`, or `BoreSighted`; a side may keep up to three plans that look alike to the other side (the standing practice "maximize hidden setup").

### 5.2 The Campaign: omniscient today

- `src/Campaign` has no perspective, no sightings, and no estimates. `SituationHandoff.cs:69` and `PilotHandoff.cs` set every placement's `Concealed` and `Hidden` to false.
- In `prototypes/europe-hex-map`, a `contacts[]` entry is made when opposing counters come within one hex (`formation-state.js`). Contact is geometry seen by both sides, not reconnaissance.
- The Campaign documents already set the requirements:
  - [Military Command and Multiscale Simulation Design](<../../docs/Military Command and Multiscale Simulation Design.md>), section 8.1: adjudicator state kept apart from each side's observation; a contact has an estimated type, a location region, a confidence class, a source, and a last-observed cursor. Section 8.3: "recipient knowledge", reports carrying the time of the observed event as well as the time of delivery. Section 14.4: zooming changes resolution, not knowledge.
  - [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>): intelligence and reconnaissance listed as the "Next" facet; hidden enemy knowledge must not leak through disabled-button explanations; automatic decisions receive no hidden enemy knowledge.
  - [Campaign Atlas Supply and Shared Map Integration Analysis](<Campaign Atlas Supply and Shared Map Integration Analysis.md>): show only threats known to the player.
  - [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>), section 8: "an enemy pocket is known only as far as the Campaign's intelligence rules disclose it". This analysis is the first step toward those rules.

## 6. Proposed model: one truth, one picture per side

### 6.1 Three layers

1. **Ground truth.** The Campaign's authoritative state, seen only by the adjudicator perspective, as in ASL.
2. **A side's picture.** A set of **reports** held by each side (per headquarters later, per side first). A report is never a pointer into ground truth; it is a record of what the side was told, which may be wrong.
3. **The briefing.** What the picture gives a committed company: a filtered extract, at company grain, carried with the Scenario Card and shown only to that side.

### 6.2 The report

| Field | Values | Meaning |
|---|---|---|
| `Area` | a Campaign hex or region | Where the report places the enemy; coarser grain means a larger area |
| `Presence` | `Confirmed`, `Probable`, `Possible`, `NoneSeen` | How sure the side is that something is there; `NoneSeen` is the dangerous one (pattern 1 of section 3.6) |
| `Type` | unknown, or a coarse class (infantry, armor, guns, mixed) | |
| `Strength` | unknown, or a band (platoon, company, battalion, and up) | Bands, not numbers, as the Military Command design asks ("Unknown values remain unknown") |
| `Identity` | unknown, or a formation name | Comes almost only from prisoners, documents, and signals |
| `Source` | patrol, OP, reconnaissance unit, prisoner, signals, air photo, civilians, higher headquarters | Each source has its own grain, lag, and error mode (section 3.3) |
| `ObservedAt`, `DeliveredAt` | Campaign time | The gap is the lag; age since `ObservedAt` lowers `Presence` one step per authored interval |
| `Deceptive` | adjudicator only | Set when the report came from enemy deception; never shown to the side that holds it |

A side's picture is the set of its current reports. Its **grain** for a Situation's area is the best of the reports there: no picture, presence only, type and strength, identity.

### 6.3 Why steps and bands, again

As with supply, ASL's hooks are switches and counts (a Recon dr or none, a HIP allowance of n squad-equivalents, a number of Dummies). The Campaign's picture needs only enough resolution to set those, plus a briefing a person can read.

## 7. Operationalizing at each level

### 7.1 Division: the Campaign

**Collection is a Campaign action that spends time and assets.** Each side tasks its means against named areas, as a collection plan against EEI:

| Campaign action | Means | Result at the next boundary | Cost and risk |
|---|---|---|---|
| Reconnaissance in force / screen | Divisional reconnaissance unit | Contact line and roads in its area: `Presence` and `Type`; rarely `Identity` | The unit is committed and can be fixed or lose strength in a contact Situation |
| Patrolling | Front-line battalions | Presence and type in the next area forward; a chance of a prisoner (then `Identity`) | Fatigue; casualties on a failure; patrols that report `NoneSeen` wrongly |
| Air reconnaissance | Allocated by higher headquarters | Positions and movement over a wide area, one to two boundaries late | Weather; camouflage and dummies give false reports |
| Interrogation | Prisoners taken in the last Situation (from the inbound report, section 7.4) | `Identity` and parent formation | None beyond holding the prisoners |
| Signals intelligence | Corps or army asset | `Identity` and location of headquarters, scaled by the target side's radio discipline | Nothing when the target keeps radio silence |
| Civilians and resistance | Authored per region and side | Local presence; a share of false reports, larger where networks are penetrated | Unreliable; a false report is indistinguishable from a true one |
| Higher feeds (army group summaries; Ultra for the Allies) | Authored per Situation, from the history | Coarse operational reports, delivered to the division without their source | Authored, labeled as such |

**Security is a posture each side sets per formation.** Radio silence denies the enemy's signals results but slows the side's own orders (the Command design's message delays); concealment of a move costs time; deception (a notional formation, dummy traffic) creates `Deceptive` reports in the enemy's picture. Each posture is a Campaign rule with a cost.

**The estimate is the player's.** The Campaign shows the picture with its sources and ages; it does not tell the player what the enemy intends. A computer-run side reads only its own picture (the Command analysis: no hidden enemy knowledge for automatic decisions). This is how the failure patterns of section 3.6 arise without a script.

**The Intelligence Determination step.** At every Situation boundary, next to the Supply Determination and with the same discipline (deterministic, recorded as one Campaign event with its inputs' revision):

1. **Deliver.** Results of collection tasked since the last boundary become reports in the tasking side's picture, at the grain, lag, and error rate of their source.
2. **Age.** Reports older than their source's interval drop one `Presence` step; `Possible` reports past their interval are dropped.
3. **Deceive.** Each active deception posture adds its `Deceptive` reports to the other side's picture.
4. **Terms.** For the Situation about to be committed, compute the outbound terms (section 7.2) from each side's picture and the ground truth.

Whether the error rates use a die roll or an authored, seeded draw is ruling I3.

### 7.2 Company: the Situation and the Scenario Card

The handoff turns the two pictures into card terms. Every term is a rule the rulebook prints; the Campaign decides only whether and how many.

| Campaign state at commitment | Card term | Notes |
|---|---|---|
| The defender has held the ground for at least one boundary and the attacker's grain over the area is poor | A larger `hip:<defender>:<n>` and more Dummies for the defender's groups | The main lever. Poor intelligence is modeled as the defender's freedom to hide, which is how it felt at the company level |
| The attacker's grain is good (type and strength, recent) | A smaller HIP allowance and fewer Dummies | Bounded below by what the scenario's defense needs; above by A12.3 and E1.2 at night |
| The attacker patrolled the area or had a reconnaissance unit in contact at this boundary | SSR: E1.23 Recon for the attacker | Printed rule; one dr, printed drm. The Campaign decides only whether the SSR is on |
| The Situation's region has an active civilian or resistance network friendly to the attacker | SSR: E2.4 Civilians | The friendly or hostile country of E2.4 comes from the Campaign region and the side |
| The defender's batteries have registered the area (held ground, time) | Pre-Registered Fire hexes (C1.73) for the defender | Waits on OBA, Pass 155 |
| The defender's guns have held their positions | Bore Sighting (C6.4) for the defender | Built in part |
| Prisoners are always interrogated in ASL | No term: E2 applies to every capture once built | Its results stay in the scenario; the Campaign's identity result comes from the inbound report |

**The briefing.** Each side receives, with the card, a private briefing built from its own picture, never from the ground truth: the enemy reported in the area by type and strength band, with sources and ages, and the areas of the board where reports place them ("enemy infantry, about a company, reported in the woods north of the village, patrol, 6 hours old"). A briefing can be wrong because the picture can be wrong. It names Campaign areas, not ASL hexes, so a briefing never pinpoints a HIP unit that the card's own rules would hide.

**The enemy's order of battle.** ASL prints both OBs on the card. The Campaign can offer a **Hidden OB** mode, under A12 footnote 18's referee and H1.1 footnote 1's secret purchases: a side's view shows the enemy OB only at its picture's grain until units are revealed in play. ASL's rules are unchanged; only the Studio's card and OB panels read through the viewer's picture. Whether to offer it, and whether as a default or an option, is ruling I5.

**The patrol as a Situation.** A Campaign patrol order can itself be committed as a small night scenario (E1 night rules: Cloaking, Freedom of Movement, Starshells, Gunflash), whose victory conditions are information: Searching named Locations, capturing a prisoner, returning a unit off a board edge. Its result returns reports, not ground. This makes the most common real collection action playable at the squad level, and it needs the night rules first (ruling I6).

### 7.3 Squad: ASL play

Nothing in the ASL rules changes. The squad level is already served by concealment, Search, Mopping Up, Snipers, and the night rules, decided in the Rules project and shown through `GameView`. The rules to build for this layer, by claims on the board:

1. **E2 Interrogation** (Pass 74): the in-scenario counterpart of the Campaign's prisoners; no calculator exists.
2. **E1.23 Recon** (Pass 90): the card term of section 7.2.
3. **E2.4 Civilians**: the card term of section 7.2; it depends on the Wind Change DR (B25.65).
4. For the patrol Situation: E1.2 night HIP and Dummies, E1.4 Cloaking, E1.21 Freedom of Movement, E1.86 and E1.93.

Unit-level knowledge (a squad that knows only what it sees) is not proposed: ASL's player is omniscient over his own units, and changing that would change every rule.

### 7.4 Back up: the Knowledge Report

When an ASL game ends, the Campaign reads what each side's view held at the end, and decides nothing again:

- Enemy units **revealed** in play become `Confirmed` reports with `Type` and `Strength`.
- Enemy units seen only as **sealed** "?" become `Probable` reports, strength unknown; a Dummy unmasked removes nothing in the Campaign but its own false report.
- Enemy units **captured** become prisoners for the Campaign's interrogation action, which yields `Identity`.
- Areas a side Searched and found empty become `NoneSeen` reports, which age like any other.

How much of this reaches the side's headquarters, and when, depends on its radios (section 7.5).

The report is applied once, keyed by game id and last revision, as the Supply Report is.

### 7.5 Radios as the channel of intelligence

The user's point (2026-11-01): radios become crucial intelligence tools. In the layered game a radio does three jobs that ASL alone does not give it.

**Out: the Campaign decides the company's communications.** Each formation carries a communications state next to its supply steps: its radios (count and condition), and, for a formation that has held its ground for a boundary, laid wire. The card turns it into printed counters:

| Campaign state | Card term | Printed effect in play |
|---|---|---|
| Radios held by the company | Radio SW counters given to named leaders of the OB (C1.22: a 1PP SW that a leader must possess) | OBA Contact (C1.2), once OBA is built (Pass 150); none before |
| Wire laid in a held position | Field Phones with their Security Areas recorded at setup (C1.23) | OBA Contact on 11 or less; cut by enemy troops or HE in the Security Area |
| AFV radios | None: printed on the AFV counter (D14.1) | D14 platoon movement for radioless AFV; the E1.21 radio exception |
| A posture of radio silence | No radio counters issued to the company | The company calls no OBA by radio; the Campaign side of 7.1 denies the enemy's signals results |

**Back: the radio decides what reaches headquarters.** Proposed Campaign rule for the Knowledge Report of section 7.4, applied once at the result:

1. **Reported by radio or wire, at this boundary.** If at the end of the scenario a side has a Good Order leader on board possessing a functioning radio, or a Field Phone that was not eliminated, the side's whole end view is reported at its full grain.
2. **Carried back, one boundary late.** Otherwise, if any of its Good Order leaders survives, the report is delivered at the next boundary, one `Presence` step lower: the debrief of men who came back.
3. **Lost.** If no Good Order leader survives, only a `Possible` contact report for the Situation's area reaches headquarters.

ASL does not need to know any of this. The Campaign reads the end state: which leaders survive, in what state, and what they possess. All of it is already in the game record (each piece of equipment's possessor, each unit's status). No ASL rule changes, and nothing crosses during play.

**Radios as targets.** A radio is a SW, so it can be captured (A21.11 and the possessor record). A captured enemy radio is, in the Campaign, a captured set and its call signs: the captor's signals results against that formation improve for an authored number of boundaries, until the formation changes its codes (the British response to the capture of company 621, section 3.3). Radios lost or broken (C1.22's breakdown on a 12) lower the formation's communications state until replaced through supply.

**Not proposed.** Radios do not carry Campaign reports during play: the boundary principle of the Supply glue design (state crosses only at commitment and result) holds. A mid-scenario "call to headquarters" would need either a new ASL rule or a pause in play, and is left to the backlog.

### 7.6 The briefing goes to the senior leader

The user's point (2026-11-01): only the highest-ranking leader receives the intelligence from headquarters, and orders reach the rest of the company by proximity to leaders and by radio.

**The problem.** In ASL one player commands every unit of his side and sees what any of them sees (A12.1). The briefing is shown to that player; he cannot be made to forget it for some counters. So "only the senior leader knows" cannot be a rule about knowledge. It has to be a rule about what units may do before they have their orders. That is exactly how the rulebook treats ignorance in E1.21: a unit that has not been attacked and has not seen the enemy may not move; the best leader can free himself; units stacked with a free leader may move with him; radio AFVs free each other.

**Who receives it.** The side's single best leader, ranked as E1.21 ranks him (by Random Selection if tied); or, where the Campaign names the company's commander and the OB carries his leader counter, that leader.

**How it spreads before kick-off: the orders group.** The Campaign knows when the order reached the company (the Command design's order delay) and when the Situation starts. The time between them decides how far the briefing has spread at setup:

| Time to brief (a Campaign fact) | Units briefed at setup |
|---|---|
| Ample (an authored threshold, for example one boundary) | Every unit: no effect, as in any printed scenario |
| Short | The senior leader; every leader who sets up in his Location or adjacent to it; every leader possessing a radio; and every unit that sets up in the Location of a briefed leader |
| None (the order arrived at the start) | The senior leader and the units that set up in his Location |

**What an unbriefed unit may not do.** A card SSR modeled on E1.21, proposed for the Scenario Attacker (the side that needs orders to go forward; a defender without orders still defends in place):

- An unbriefed unit may not move or advance until it has been attacked, or has seen a Known enemy unit, or begins its MPh in the Location of a briefed Good Order leader (it is then briefed).
- A briefed leader possessing a radio briefs every other leader of his side possessing a radio at the start of each friendly MPh, as E1.21 frees radio AFVs together.
- Radio AFVs are briefed when any radio AFV is (E1.21's exception, unchanged).
- Briefing, once gained, lasts the scenario, as Freedom of Movement does.
- Fire is not restricted: a unit without orders still shoots at what it sees.

**What it does to the game before kick-off.** Setup becomes a decision about command: stack around the senior leader to be briefed and moving on Turn 1, at the price of a target worth a Sniper or a Critical Hit; or spread out and accept units that wait. Radios, otherwise only an OBA tool, become a way to move a whole company at once. Losing the senior leader early no longer costs only his modifier. And the Campaign's choice to commit a company on short notice now has a visible cost on the board, which is the point of the Command design's order delays.

**Status in the rulebook.** This is a scenario SSR the project writes, not a printed rule: E1.21 applies only to the Defender at night. Printed scenarios often restrict movement by SSR, so it is within what a card may do; but its effects must be decided by a Rules calculator, through a rule claim, built on E1.21 itself (not built, Pass 90). It is labeled as manufactured on every card that carries it, and it is an exception to ruling I4's "printed rules only" (ruling I11).

**Rejected.** A command radius for every action (a whole new command-and-control system that changes every ASL rule); hiding the briefing from the human player (impossible on one screen). For a computer-run side, though, knowledge per unit is real: such a side can be made to decide only from what briefed units know (ruling I12), which matches the Command analysis's rule that automatic decisions receive no hidden enemy knowledge.

## 8. Ownership and disclosure

| Part | Owner |
|---|---|
| Reports, pictures, collection, security postures, the Intelligence Determination, the briefing, the outbound rules of 7.2, the Knowledge Report | Campaign (`src/Campaign`), labeled as authored Campaign rules |
| `hip:<side>:<n>` and group Dummies | Rules and Play (built) |
| E1.23, E2, E2.4 and their SSR tokens | Rules, by rule claims; their application at setup and in play, track 3 |
| The Hidden OB mode's view of the card and OB panels | MapStudio, reading the side's picture |
| The communications state, the radio and wire card terms, the report channel rule of 7.5, captured-radio effects | Campaign |
| Radio and Field Phone counters and their OBA effects (C1.2 to C1.23), D14 | Rules, by rule claims (Passes 150, 155, 105) |
| E1.21 and the orders-group SSR of 7.6: its token, who is briefed, what an unbriefed unit may not do | Rules, by a rule claim; applying it at setup and in movement, track 3 |
| Who is briefed at setup (the time to brief) | Campaign decides the time; the card's SSR carries it; Rules decides the effect |

Disclosure follows the ASL discipline, extended to the Campaign:

- A side's picture, briefing, collection tasks, and security postures are known only to that side and the adjudicator.
- Card SSRs are public, as in ASL. An E1.23 Recon SSR tells the defender that the attacker reconnoitered; that is true of any printed scenario with Recon. The card's HIP allowance and Dummies are public numbers; where they are is not.
- A report's `Deceptive` flag is adjudicator-only, always.
- No Campaign surface (a disabled action's reason, a route highlight, a tactical preview, a computer side's choice) may reveal ground truth the viewer's picture does not hold. The Campaign needs a viewer-scoped view of its state, as `GameView` is for ASL, before any of this is built.

## 9. Rulings needed

Each carries a proposal.

| Id | Question | Proposal |
|---|---|---|
| I1 | Should the Campaign hold a separate picture per side, with the ground truth seen only by the adjudicator | Yes; first per side, per headquarters later under the Command design's recipient knowledge |
| I2 | The report record of section 6.2 (presence steps, type, strength bands, identity, source, two times, an adjudicator-only deception flag) | Yes, as written |
| I3 | Collection errors and false reports by seeded draw or by authored outcome per Situation | A seeded draw recorded in the Determination event, so a replay gives the same picture; authored feeds (higher headquarters, Ultra) stay authored |
| I4 | Intelligence reaches ASL only through printed rules (HIP allowance, Dummies, E1.23, E2.4, C1.73, C6.4) and a private briefing | Yes; no new ASL rule, no drm on E1.23 beyond its printed ones |
| I5 | A Hidden OB mode for cards made from the Campaign | Offer it as an option, off by default, until the Studio's card and OB panels read through the viewer |
| I6 | The patrol as a playable night Situation | Later, after the E1 night rules it needs are claimed and built; record it in the backlog |
| I7 | Order of rule claims for this layer | E2 Interrogation first (wanted in every scenario), then E1.23 Recon, then E1.21 Freedom of Movement (needed by 7.6), then E2.4 Civilians; the radio rules C1.2 to C1.23 with OBA |
| I8 | Viewer-scoped Campaign state before any Campaign intelligence feature | Yes; it is the prerequisite, and it also serves supply's disclosure (the Supply glue design, section 8) |
| I9 | A per-formation communications state (radios, wire) that sets the card's Radio and Field Phone counters | Yes; their printed effects wait on OBA, their Campaign effects (7.5) do not |
| I10 | The report channel rule of 7.5 (radio or wire now, carried back one boundary late, or lost) | Yes, as written; nothing crosses during play |
| I11 | The orders-group SSR of 7.6, modeled on E1.21, for the Scenario Attacker | Offer it as an option, off by default, labeled manufactured; build E1.21 first and decide the SSR by a rule claim |
| I12 | A computer-run side decides only from what its briefed units know | Yes, when a computer side is built; record it in the backlog |

## 10. Next steps

If the rulings are given, the next document is a **Campaign Intelligence to ASL Glue Design**, with the shape of the Supply glue design: the model (section 6), the Intelligence Determination, the `IntelligenceTerms` record carried in `SituationHandoff.CampaignContext` and in the handoff hash, the card terms, the briefing, the Knowledge Report, ownership, disclosure, and tasks. The rule claims of I7 can be taken from the board independently of the Campaign work.

Items this analysis leaves out, and which need the backlog when a design is approved: the module Campaign Games' Recon rules (P8.622, O11.622) and civilian SSRs (KGP18, PB21), whose texts we do not have; per-headquarters knowledge; signals and deception beyond a posture switch; air reconnaissance tied to Campaign weather; radio reports during play; and an orders rule for the Scenario Defender.

## Sources

Historical, read in this session:

- FM 100-5 on essential elements of information: https://ibiblio.org/hyperwar/USA/ref/FM/FM100-5/FM-100-5-5.html
- FM 7-25 (1942), the regimental headquarters company and I&R platoon: https://ibiblio.org/hyperwar/USA/ref/FM/PDFs/FM7-25.PDF
- FM 2-30 (1943, 1944) cavalry reconnaissance: https://archive.org/download/FM2-301943/FM2-301943.pdf
- Mechanized cavalry in WWII (DTIC ADA600606): https://apps.dtic.mil/sti/pdfs/ADA600606.pdf
- German reconnaissance doctrine, Intelligence Bulletin, May 1943: https://mobile.lonesentry.com/articles/reconlt/index.html
- The capture of Unit 621, Australian Army Journal: https://researchcentre.army.gov.au/library/australian-army-journal-aaj/volume-12-number-1-winter/capture-unit-621-lessons-information-security-management-north-africa-campaign
- Panzerarmee Afrika Ic reports: https://rommelsriposte.com/panzerarmee-intelligence-reports/
- CMH, The Siegfried Line Campaign (the Roer dams): https://ibiblio.org/hyperwar/USA/USA-E-Siegfried/USA-E-Siegfried-14.html
- Bocage patrolling: https://warhistory.org/2018/06/13/fighting-in-the-bocage-the-us-army-experience-part-i/
- Arnhem's air photographs: https://warfarehistorynetwork.com/article/arnhems-other-urquhart/
- Air observation posts: https://warontherocks.com/2017/12/when-the-piper-cub-roamed-the-battlefield/
- Sound ranging and flash spotting: https://britishartillery.co.uk/p_artyintcb3945.htm
- Phantom: https://en.wikipedia.org/wiki/GHQ_Liaison_Regiment
- The Ghost Army: https://ghostarmy.org/about/history
- Operation Fortitude: https://en.wikipedia.org/wiki/Operation_Fortitude

Not reached in this session and worth reading before a design: the NSA study of SIGINT in the Ardennes campaign; the RAF Air Historical Branch study of Arnhem air reconnaissance; FM 30-5 (1940) and FM 21-75 in full. Disputed: the timing of the Mortain Ultra warning.

Rulebook: `eASLRB_v3_01.pdf`, pages 47, 53, 68, 76 to 80, 82 to 83, 101 to 102, 148 to 149, 163 to 166, 174, 212, 223 to 228, 237 to 239, 334, 336, 666 to 667, and the index (pages 3 to 44).

## Related documents

- [Campaign Supply and ASL Rulebook Supply Hooks Analysis](<Campaign Supply and ASL Rulebook Supply Hooks Analysis.md>)
- [Campaign Supply to ASL Glue Design](<Designs/Campaign Supply to ASL Glue Design.md>)
- [Campaign Command, Supply, Leadership and Optional Complexity Analysis](<Campaign Command Supply Leadership and Optional Complexity Analysis.md>)
- [Military Command and Multiscale Simulation Design](<../../docs/Military Command and Multiscale Simulation Design.md>)
- [Formation to ASL Map Alignment Analysis](<Formation to ASL Map Alignment Analysis.md>)
