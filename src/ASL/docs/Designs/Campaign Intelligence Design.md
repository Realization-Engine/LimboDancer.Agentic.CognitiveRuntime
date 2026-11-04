# Campaign Intelligence Design

Date: 1 November 2026

Status: proposed design. Nothing is built. It rests on rulings I1 to I12 of the [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>), approved by the user on 2026-11-01. The decisions this design makes on its own (section 16) were researched by sub-agents and are open to the user's veto. Implementation waits for the user's approval of this design.

It is built on the [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>), which holds the foundation it shares with supply (the user, 2026-11-01). Companion: the [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>) maps what this design produces onto the Scenario Card and reads the ASL result back.

## 1. Purpose and scope

On the core's event log, side view, and boundary (ruling I8 is met by the core's view), the design gives the Campaign layer what it needs to hold intelligence:

- each side's picture of the enemy, made of reports (rulings I1, I2);
- collection, security postures, and a communications state per formation (ruling I9);
- the Intelligence Determination at every Situation boundary, with seeded draws (ruling I3);
- the briefing and the time to brief (the analysis, section 7.6);
- the intake of what an ASL game revealed, by the report channel rule (ruling I10).

In scope: the Campaign side of all of the above. Out of scope: the card terms and the reading of the ASL game record (the glue design); the Campaign's movement, orders, and supply chains, which stay where they are until the Campaign's migration stages C02 and C06 of the [Implementation Plan](<../LimboDancer.Campaign Implementation Plan.md>); any ASL rule.

## 2. Built on the core

The [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>) provides what this design stands on: the library `src/Campaign/LimboDancer.Campaign.Domain` and PilotHost's place beside it (core, section 4); identities, the viewer, and the event log, adjudicator-only by default (section 5); the view per side (section 6); the boundary facts, the boundary event with its Supply, Intelligence, and Terms sections, and the seeded streams (section 7); the handoff onto a card with its `CampaignTerms` field and setup areas (section 8); and the result intake (section 9). What exists today, and why the core lives where it does, is in the core's sections 2 and 4.

## 3. Principles

The core's principles hold (core, section 3). Two more are this subject's:

1. **Pictures are records of what was told, never pointers into the truth.** A report can be wrong, late, or planted.
2. **Every number is an authored parameter.** The record supplies the shape of each source; it does not supply probabilities (the Military Command design: "Unknown values remain unknown"). Parameters live in a versioned profile, labeled as authored, with their rationale.

## 4. The intelligence part of the view

A side's view (core, section 6) holds, for intelligence: its picture (its `SideReport`s, section 7.1), its briefings (section 9), its collection tasks and their results, its own postures, and its communications state. It holds nothing of a report's `Deceptive` flag or of the formation behind a report.

## 5. The intelligence part of the boundary facts

The core's boundary facts (core, section 7.1) carry, for intelligence:

| Part | Contents |
|---|---|
| Identities | For each formation: the name an enemy would learn (its identity) |
| Notional formations | For each active deception: the notional formation's side, area, type, strength band, and name |
| Collection | Each side's tasks for this boundary: source, target area, and the asset used (section 6) |
| Postures | Each formation's security posture (section 6.3) |
| Communications inputs | Radios issued, lost, or captured, and wire laid or cut, since the last boundary (section 6.4) |
| Orders | For a Situation about to be committed: the time each committed formation received its order, and the Situation's start time (section 9) |
| Prisoners | Prisoners each side holds, by the formation they came from, and the boundary they were taken |

The Intelligence section of the boundary event (core, section 7.2) runs after Supply, whose isolation it reads. Its draws use the core's streams (core, section 7.3), keyed by each collection task's or notional formation's stable id.

## 6. Collection, security, and communications

### 6.1 The intelligence profile

All parameters of sections 6 to 9 sit in an `IntelligenceProfile`: a versioned, immutable record, published with the campaign, whose every row carries its rationale and is labeled as an authored parameter. A campaign pins one profile version; changing it publishes a new version and never alters a running campaign. The starting values below were proposed by a sub-agent from the historiography (section 16, decision CD8) and are tuning knobs, not measurements.

### 6.2 Sources

A tasking against one area makes one draw. If the enemy is present, the draw is **Full** (a true report at the source's best grain), **Reduced** (true, one grain lower and one `Presence` step lower), or **Miss** (a `NoneSeen` report, false). If the area is empty, the draw is a **false positive** (a `Possible` report of a guessed type) or a true `NoneSeen`. Bands are stored as integers summing to 100, so each draw is auditable.

| Source | Best grain | Present: Full / Reduced / Miss | Empty: false positive | Lag (boundaries) | Ageing interval | Cost and risk |
|---|---|---|---|---|---|---|
| Reconnaissance unit | Type and strength; identity only on Full in contact | 50 / 30 / 20 | 10 | 0 | 2 | On the worst 10 of the draw, the unit loses a strength step (the worst 20 against a screen) |
| Front-line patrols | Presence and type; strength only on Full | 40 / 35 / 25 | 10 | 0 | 1 | One Full result in six costs the battalion a fatigue or strength step |
| Air reconnaissance, visual | Presence and type, in open ground | 40 / 30 / 30 | 5 | 0 | 1 | Needs flying weather and local air parity; authored sorties per boundary |
| Air reconnaissance, photographic | Type and strength | 50 / 25 / 25 | 5 | 1 (interpretation) | 2 | As visual; dummies fool it (section 6.3) |
| Interrogation of prisoners | Identity of the units in contact, sometimes the next behind | 60 / 25 / 15 | 5 | 0 at division, +1 per echelon up | 3 for identity, 2 for presence | Needs prisoners taken this boundary or the last |
| Signals intelligence | Presence and identity; type weak; strength never | 50 / 25 / 25 | 0 (only dummy traffic misleads) | 0 tactical, 1 decrypts | 2 | Authored intercept assets per side |
| Civilians and resistance | Presence and type; identity sometimes | 35 / 30 / 35 | 15, or 40 where the network is penetrated | 1 | 2 | None to the tasking side; a penetrated network passes deceptive reports |
| Higher headquarters (authored; Ultra for the Allies) | Authored per Situation | Authored | Authored | 1, +1 per echelon down | 3 | Ultra reaches a division only disguised: `Presence` at most `Probable`, grain one step lower |

Modifiers (all authored):

| Condition | Effect |
|---|---|
| Enemy radio silence | Signals: Full and Reduced become Miss; at most a `Possible` "no traffic" report for the sector |
| Enemy concealment of moves | Reconnaissance and civilians: Full halved, the difference to Reduced; air: Full and Reduced halved, the difference to Miss |
| Woods, bocage, towns | Reconnaissance Full 50 to 35; patrols' Miss -5; air Full halved, visual air Full 0; towns: civilians' Full +10 |
| Low cloud, fog | Air unavailable; reconnaissance Full -10 |
| Night | Reconnaissance unit not tasked; patrols normal; air unavailable; signals Full +10 |
| Enemy screen in front of the area | Reconnaissance Full becomes Reduced, its risk doubled; patrols' Miss +15 |
| Enemy air superiority | Air unavailable, or one sortie in three aborts |

Reports from a Miss are kept: negative evidence exists, and it can be false (the Ardennes pattern of the analysis, section 3.6).

### 6.3 Security postures and deception

Each formation's posture is a set of flags, each with its cost:

| Posture | Effect on the enemy's collection | Cost to the side using it |
|---|---|---|
| Radio silence | Denies signals results (6.2) | No radios issued on its cards (I9; the glue design); once the Campaign's order system exists (C06), orders that cross two or more echelons arrive one boundary later unless a landline links the units. Silence ends at the start of the side's own attack or at first contact; it is read at commitment after this end rule, so a formation committed to its own attack is not silent |
| Concealment of moves | Halves air and reconnaissance results (6.2) | The move takes longer: an authored delay in the prototype's movement, applied by the caller |
| Deception (a notional formation) | Creates deceptive reports (below) | Ties up an authored signals asset per notional formation; it decays unless sustained each boundary |

A notional formation feeds the enemy's channels at each boundary:

| Means | Channel fed | Reports created | Grain | `Presence` at most |
|---|---|---|---|---|
| Dummy wireless traffic | The enemy's signals tasking on its area | One, drawn as Full | Identity and type | `Probable` |
| Dummy equipment and camps | The enemy's photographic air tasking on its area | One on Full or Reduced | Type and strength | `Probable` |
| Controlled agents, penetrated networks | The enemy's civilian channel, or its higher-headquarters feed | Authored, one or two per boundary | Identity and intent | `Probable`, or `Confirmed` where the feed vouches for the agent (authored) |

Two rules bound deception:

- **Corroboration.** Reports from two independent channels on the same area and type raise `Presence` one step (at most `Confirmed`). Two reports on one channel count once. A deception planned across channels can therefore reach `Confirmed`, as Fortitude did (the 1944 Allied deception that kept a notional army group in southeast England in the German picture for weeks after D-Day).
- **Contact breaks it.** A patrol or a reconnaissance tasking in a notional formation's area draws against the true formations there only; the notional one contributes nothing. If the area holds no true formation, the draw is the empty-area draw, and a true `NoneSeen` contradicts the deceptive report; the side sees the contradiction between two of its reports. Interrogation targets prisoners, not areas: a prisoner never names a notional formation, so an interrogation can contradict a deceptive identity but never confirm it.

Every deceptive report carries `Deceptive = true` and its seed in the boundary event, visible to the adjudicator alone.

### 6.4 The communications state

Each formation carries, next to its supply steps:

| Field | Values | Changed by |
|---|---|---|
| `Radios` | a count of sets in working order, by kind (infantry set, vehicle set) | issue at the formation's establishment (authored); loss, breakdown, and capture reported from ASL (the glue design); replacement through supply |
| `Wire` | `None`, `Laid` | laid when the formation has held its ground for one boundary; cut when the formation moves or its area is shelled in a Situation |
| `CodesCompromised` | the boundary until which the enemy's signals results against it are improved, or none | a captured radio (section 10); ends after an authored number of boundaries, when the codes are changed |

## 7. The knowledge model and the Intelligence Determination

### 7.1 The report

As ruling I2: `Area`, `Presence` (`Confirmed`, `Probable`, `Possible`, `NoneSeen`), `Type` (unknown, infantry, armor, guns, mixed), `Strength` (unknown, platoon, company, battalion, regiment and up), `Identity` (unknown, or a formation name), `Source`, `ObservedAt`, `DeliveredAt`. Two types carry it:

- `SideReport`: what the side holds. It has no `Deceptive` field and no reference to a formation id, so no code reading a side's report can reach the truth behind it.
- `AdjudicatorReport`: a `SideReport` with `Deceptive`, the `FormationId` it came from (if any), and the draw that made it.

A side's **picture** is its current `SideReport`s. Its **grain** over an area is the best of the reports there: none, presence, type and strength, or identity.

### 7.2 The Determination

At every boundary, after Supply, the Intelligence section runs these pure steps in order:

1. **Deliver.** Each collection task of the facts makes its draw (6.2, with its modifiers and the target's postures) and creates reports with `ObservedAt` = this boundary and a delivery boundary: a lag of N means the Nth boundary after this one (0, this one). `DeliveredAt` is that boundary's campaign time once it occurs. Reports whose delivery boundary has come join the side's picture.
2. **Deceive.** Each notional formation feeds the channels the enemy tasked against its area (6.3).
3. **Brief.** For a Situation about to be committed, build each side's briefing (section 9) from that side's view.
4. **Hand over.** The Intelligence section ends here. The boundary event's separate Terms section (core, section 7.2) then reads the two pictures, the truth, and the communications states to build `IntelligenceTerms` (the glue design, section 3).

The draws and their seeds are written in the boundary event. A replay reads them and never draws again; the same facts and seed always give the same pictures.

**Effective presence is computed when a report is read, never stored.** A report keeps the `Presence` it was made with (its base). Whenever a picture is read, each report's effective `Presence` is:

- its base, lowered one step for each whole ageing interval of its source that has passed since `ObservedAt` (age is counted from observation, never delivery: a late report is not fresh; the Military Command design, section 8.3);
- raised one step if another current report from an independent channel agrees with it on area and type (corroboration, 6.3), at most `Confirmed`;
- dropped from the picture once it would fall below `Possible`.

A `NoneSeen` report has no step below it: it is dropped once one ageing interval of its source has passed. A contradiction (a `NoneSeen` and a positive report on one area, both current) is shown to the side with both reports. Because effective presence is a function of the reports and the current boundary, running the Determination at every boundary never applies ageing or corroboration twice.

## 8. Leaks

The core's view (core, section 6) is the only seam a side reads; its intelligence part is section 4 above. Every Campaign surface must pass the leak audit the Campaign analyses ask for (the Command analysis, lines 63 and 172; the Supply Integration analysis, line 182; the Formation to Squad analysis, line 157):

- a disabled action's reason;
- route highlighting and path costs;
- tactical previews of a Situation;
- forecasts and history (the Implementation Plan, lines 263 to 267);
- a computer-run side's choices, which read only its view (and, under the orders-group option, only its briefed units' knowledge; ruling I12, backlog section 95).

The audit is a test per surface: built from a fixture where the two sides' truths differ, it checks that the surface's output for one side is the same whatever the enemy's hidden state is.

## 9. The briefing and the time to brief

### 9.1 The briefing

For each side and each Situation about to be committed, the briefing lists the reports of that side's picture whose areas overlap the Situation's area: type and strength band, identity where known, source, and age ("enemy infantry, about a company, reported in the woods north of the village; patrol; 6 hours old"). It names Campaign areas, never ASL hexes, so a briefing never pinpoints a unit the card would hide. It is built from the side's view only (section 4; core, section 6), so it can be wrong.

The briefing is delivered to the side's senior leader (the analysis, section 7.6; its optional orders-group SSR is ruling I11). Who that leader is, and what follows on the board, is the glue design's.

### 9.2 The time to brief

From the facts' order and start times (section 5), each committed formation's time to brief falls in a band:

| Echelon receiving the order | Ample | Short | None |
|---|---|---|---|
| Division, corps | 24 hours or more | 8 to 24 hours | under 8 hours |
| Brigade, regiment | 12 hours or more | 4 to 12 hours | under 4 hours |
| Battalion (the company's orders) | 6 hours or more, with daylight to reconnoiter | 2 to 6 hours | under 2 hours |

German formations of good quality shift one band up at battalion and regiment (mission tactics; the 1933 Truppenführung); scratch and Volksgrenadier formations do not. The battalion row decides the card (the glue design). The rule of leaving two thirds of the time to subordinates is post-war US doctrine and serves here only as the rationale for the bands.

## 10. The knowledge part of the result intake

The core's result intake (core, section 9) carries, for intelligence, one `KnowledgeReportFacts` record per side, built by PilotHost (the glue design, section 6): the enemy units revealed (with their Campaign area, kind, and size), the enemy stacks seen only as "?", the enemy units captured, the areas searched and found empty, and the side's Good Order leaders at the end with the radios and field phones they possess. The Intelligence section of the `SituationResultEvent` applies the report channel rule (ruling I10):

1. **Radio or wire: delivered at this boundary.** A Good Order leader on board with a working radio, or a field phone not eliminated: every report at full grain.
2. **Carried back: next boundary, one step lower.** Otherwise, if any Good Order leader survives.
3. **Lost.** Otherwise: one `Possible` report for the Situation's area.

Then:

- revealed units become `Confirmed` reports of type and strength (an ASL unit seen does not tell its formation's name); "?" stacks become `Probable`, strength unknown, and the adjudicator's copy of one made from Dummies alone is marked `Deceptive`; searched-empty areas become `NoneSeen`;
- captured units become prisoners in the facts of the next boundary, where interrogation (6.2) can yield identity;
- radios lost, broken, or captured change the communications state (6.4); a radio captured by the enemy sets `CodesCompromised` on its formation.

The intake is applied exactly once by the core's rule (core, section 9).

## 11. Interaction with supply

- The boundary event's Supply section runs before Intelligence (core, section 7.2).
- Isolation (the Supply Determination's `Isolated`) cuts `Wire` and blocks radio replacement through supply.
- The Supply glue design's disclosure section ("an enemy pocket is known only as far as the Campaign's intelligence rules disclose it") is met by this design: a side learns of an enemy pocket only through reports in its picture.

## 12. Ownership

| Part | Owner |
|---|---|
| The intelligence profile, reports, the Intelligence Determination, postures, deception, the communications state, the briefing, the time to brief, the intake rules | `LimboDancer.Campaign.Domain` |
| Reading the ASL game record into `KnowledgeReportFacts` | `LimboDancer.Campaign.PilotHost` (the glue design) |
| The core's records, endpoints, card, and intake | The core design |
| The card terms and every ASL effect | The glue design; the Campaign Dynamic SSR Design; ASL Rules |

## 13. Memory and performance

The core's answers hold (core, section 11). For intelligence: the Determination makes one draw per collection task (tens per side) and reads each side's reports (hundreds at most) once per boundary; effective presence is computed per read, over that side's current reports only. Nothing is added to any ASL Play step, render, or read path, and no ASL counter is added.

## 14. Tasks, in order

After the core's CB1 to CB5 (and CB9 for K6):

| Task | Work | Tests |
|---|---|---|
| K1 | `SideReport`, `AdjudicatorReport`, the picture, grain, effective presence | Ageing from `ObservedAt`; corroboration; grain per area; no `Deceptive` reachable from a side's view |
| K2 | The intelligence profile and the Determination (7.2) | Same facts and seed give the same picture; replay draws nothing; each historical case of the analysis, section 3.6, reproduced by a fixture |
| K3 | Postures, deception, corroboration | Contact breaks deception; two channels confirm |
| K4 | Communications state | Issue, loss, wire laid and cut, codes compromised |
| K5 | Briefing and time to brief | Two sides get different briefings from one truth; bands by echelon |
| K6 | Knowledge intake rules | The three channel cases |

All are Campaign work with no ASL reference. The reading of the ASL record that feeds K6 is PilotHost's (the glue design). The glue design's tasks follow.

## 15. Acceptance

The core's acceptance holds (core, section 14). For intelligence:

- A side's view never contains a value from which the enemy's ground truth, or a report's `Deceptive` flag, can be read.
- The same boundary facts and seed always give the same pictures and briefings; a replay draws nothing.
- Every number the Determination uses comes from a pinned, versioned profile, labeled as authored.
- A late report is never fresh.

## 16. Decisions made in this design

Made on 2026-11-01 under the user's instruction to resolve open questions by expert sub-agents and act on the best recommendation. Each is open to the user's veto. CD1 to CD7 (where the core lives, the viewer, default visibility, one boundary, `SeedRandom`, disclosure in the prototype period) moved with the core to the [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>), section 13.

| Id | Question | Decision | Basis |
|---|---|---|---|
| CD8 | Starting values of the intelligence profile | The tables of section 6, labeled as authored | A historian sub-agent's proposal; every number is a tuning knob |
| CD9 | A Miss produces a report or nothing | A `NoneSeen` report | Negative evidence exists and can be false |
| CD10 | Corroboration | Two independent channels raise `Presence` one step | Lets planned multi-channel deception succeed, single-channel deception fail |
| CD11 | Ageing and corroboration: stored or computed | Computed when a report is read, from its base and the current boundary; `NoneSeen` dropped after one interval | A reviewer found that applying them at every boundary would repeat them |
| CD12 | Random streams | The core's rule (core, section 7.3) applied to intelligence: one stream per collection task or notional formation, keyed by its stable id | A task added must not shift another task's draw |
| CD13 | A deception area holding true formations | Contact draws against the true formations only; the notional contributes nothing | Otherwise a notional formation would hide real ones from patrols |

## Related documents

- [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>)
- [Campaign Dynamic SSR Design](<Campaign Dynamic SSR Design.md>)
- [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>)
- [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>)
- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>)
- [LimboDancer.Campaign Implementation Plan](<../LimboDancer.Campaign Implementation Plan.md>)
- [Military Command and Multiscale Simulation Design](<../../../docs/Military Command and Multiscale Simulation Design.md>)
