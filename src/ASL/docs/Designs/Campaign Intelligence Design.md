# Campaign Intelligence Design

Date: 1 November 2026

Status: proposed design. Nothing is built. It rests on rulings I1 to I12 of the [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>), approved by the user on 2026-11-01. The decisions this design makes on its own (section 16) were researched by sub-agents and are open to the user's veto. Implementation waits for the user's approval of this design.

Companion: the [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>) maps what this design produces onto the Scenario Card and reads the ASL result back.

## 1. Purpose and scope

The design gives the Campaign layer what it needs to hold intelligence:

- a record of Campaign events whose default visibility is the adjudicator alone;
- a view of Campaign state for each side (ruling I8), which leaves out what the side may not know;
- each side's picture of the enemy, made of reports (rulings I1, I2);
- collection, security postures, and a communications state per formation (ruling I9);
- the Intelligence Determination at every Situation boundary, with seeded draws (ruling I3);
- the briefing and the time to brief (the analysis, section 7.6);
- the intake of what an ASL game revealed, by the report channel rule (ruling I10).

In scope: the Campaign side of all of the above. Out of scope: the card terms and the reading of the ASL game record (the glue design); the Campaign's movement, orders, and supply chains, which stay where they are until the Campaign's migration stages C02 and C06 of the [Implementation Plan](<../LimboDancer.Campaign Implementation Plan.md>); any ASL rule.

## 2. What exists today

Surveyed on 2026-11-01:

- **The C# Campaign** (`src/Campaign`) is a Goodwood terrain refinement and setup bridge into ASL: the Refinement library (Panzer Leader source boards to ASL terrain, 114 formations to 624 counters), PilotHost (a minimal web host on port 8093 that serves the JS prototype as static files and prepares the handoff), a command-line tool, and one test project. It has no campaign state, no event log, no boundaries, no supply, no side view, and no intake of ASL results.
- **Campaign play** (the clock, the command log and its replay, supply, the 17 Situations, saves) lives in the browser prototype `prototypes/europe-hex-map`. Its `CampaignContext` reaches C# as an opaque JSON object, hashed into `SituationHandoff`.
- **Determinism tools exist.** `SeedRandom` (`Refinement/SeedRandom.cs`, stream `sha256-counter-v1`) and content-addressed immutable JSON saves (`SituationSessions`).
- **The direction is set.** The Campaign will move to C# and Blazor (the user, 2026-11-01; the Implementation Plan). C01 to C07 must build without references to ASL projects (Plan, line 21); its domain code must not depend on Razor, the DOM, or JavaScript (line 91); its event record carries a visibility policy (line 275); and it separates authoritative state from viewer-filtered knowledge (line 277).
- **The patterns to mirror** are ASL's: `Perspective`, `GameEvent.Visibility`, and `GameView` (a side's view leaves out what the side cannot know), and the Rules boundary of AGENTS.md (a calculator takes facts and returns a verdict; its caller writes the events).

## 3. Principles

1. **Ground truth stays on the server.** A browser that holds the whole truth cannot keep it from its user. Every per-side value is computed in C# and sent to a viewer only through that viewer's view.
2. **Hidden unless granted.** A Campaign event is visible to the adjudicator alone unless it names who else may see it. This is the reverse of ASL's `GameEvent`, where no list means public; in the Campaign, the safe failure is to hide.
3. **The Determination is a calculator.** It takes facts, prior pictures, parameters, and a seed, and returns new pictures and terms. Its caller writes the boundary event. This is the Rules boundary applied inside the Campaign.
4. **Pictures are records of what was told, never pointers into the truth.** A report can be wrong, late, or planted.
5. **Every number is an authored parameter.** The record supplies the shape of each source; it does not supply probabilities (the Military Command design: "Unknown values remain unknown"). Parameters live in a versioned profile, labeled as authored, with their rationale.
6. **One boundary for supply and intelligence.** Both are decided at the Situation boundary, in one event, from one input revision.
7. **The core does not own campaign play.** Until C02 and C06, the Campaign's movement, orders, and supply are the prototype's. The core receives their result at each boundary as a facts snapshot.

## 4. Where it lives

**A new library, `src/Campaign/LimboDancer.Campaign.Domain`**: a plain net10.0 class library, with no Razor, no ASP.NET, and no ASL reference. Its tests are `src/Campaign/tests/LimboDancer.Campaign.Domain.Tests`. Both join `src/Campaign/Campaign.slnx`, so `campaign-ci.yml` runs them, outside the ASL gate (the user's ruling of 2026-10-15), with lock files restored in locked mode.

- It is the Implementation Plan's Domain and Application code, written before the Blazor app exists. When C01 starts, `LimboDancer.Campaign` references it rather than reimplementing it.
- It is not part of Refinement, which references ASL Units and Maps and so cannot hold code that C01 to C07 must use.
- Everything that touches ASL stays in **PilotHost**, which already references ASL Play: building the card terms into the handoff, and reading an ASL game record for the Knowledge Report (the glue design). PilotHost turns ASL facts into Campaign records; the Domain library applies Campaign rules to them. No ASL project references the Campaign.

**`SeedRandom` moves** into the Domain library, with a small canonical serializer of its own for its string-array key, so the library needs no ASL reference. Refinement then references Domain. A golden-vector test proves the `sha256-counter-v1` stream is unchanged byte for byte, so every existing seed replays.

**In the prototype period**, PilotHost hosts the library behind two endpoints:

- `POST /api/campaign/{campaignId}/boundaries`: the prototype submits a boundary's facts snapshot (section 5.4); PilotHost runs the Determination, appends the boundary event, and returns its id.
- `GET /api/campaign/{campaignId}/view?viewer=<side>`: returns that side's view (its picture, its briefings, its communications state).

During this period the prototype still holds the truth for movement and supply, so disclosure is not yet enforced against a determined user; the prototype's screens are labeled "disclosure not enforced in the prototype". What the period proves is the model, the API, and the card terms, which only C# computes. Enforcement becomes real when the Blazor app (C02) takes the Campaign state onto the server.

## 5. The Campaign core

Only what intelligence and supply need. Each record is immutable.

### 5.1 Identities

`CampaignId`, `SideId`, `FormationId` (opaque, taken from the prototype's formation ids, which the Refinement roster already carries as `SituationFormation.Id`), `AreaId` (a Campaign hex or an authored region), `SituationId`, `BoundaryId`, and `CampaignTime` (the prototype's campaign time, in minutes from the campaign's start).

### 5.2 The viewer

`CampaignViewer`: a side, or the adjudicator, who sees everything. It mirrors ASL's `Perspective` in shape (a name and an adjudicator constant) but is its own type: the Domain library may not reference ASL, and a Campaign viewer will become a headquarters later (ruling I1; the Military Command design's recipient knowledge), which an ASL perspective never will. The mapping between the two is in PilotHost.

### 5.3 The event and the log

`CampaignEvent`: the Implementation Plan's fields (line 275): `Id`, `CampaignId`, `Sequence`, `EffectiveAt`, `RecordedAt`, `Kind`, `PayloadVersion`, `CommandId`, `CorrelationId`, `AffectedEntityIds`, `Visibility`, and the hash of the log's prefix through this event. The prefix hash is this design's addition: the Military Command design binds its checkpoints to an event-prefix hash (section 7), and storing it on every event lets any revision be checked the same way.

- `Visibility` is a list of viewers. An empty list means the adjudicator alone.
- Recorded time comes from the host's clock; effective time is campaign time. Replays use the recorded values and never read the clock.

`CampaignLog`: append-only, one writer, each append checked against the expected revision so a stale writer fails rather than overwrites. Stored as content-addressed immutable JSON, as `SituationSessions` stores saves today, one file per revision range.

### 5.4 Boundary facts

`BoundaryFacts` is the ground truth the Determination needs, submitted by the caller (the prototype now, the Blazor app later), hashed, and stored with the boundary event. It is visible to the adjudicator alone.

| Part | Contents |
|---|---|
| Situation | `SituationId`, phase (`Before` commitment or `After` the result), campaign time, weather class per area, day or night |
| Formations | For each formation: id, side, area, type class (infantry, armor, guns, mixed), strength band, the name an enemy would learn (its identity), quality class, and whether it holds its ground since the previous boundary |
| Notional formations | For each active deception: the notional formation's side, area, type, strength band, and name |
| Collection | Each side's tasks for this boundary: source, target area, and the asset used (section 6) |
| Postures | Each formation's security posture (section 6.3) |
| Communications inputs | Radios issued, lost, or captured, and wire laid or cut, since the last boundary (section 6.4) |
| Orders | For a Situation about to be committed: the time each committed formation received its order, and the Situation's start time (section 9) |
| Prisoners | Prisoners each side holds, by the formation they came from, and the boundary they were taken |

### 5.5 The boundary event

`SituationBoundaryEvent` is one Campaign event per boundary: `SituationId`, phase, the input revision, the facts hash, the boundary's seed, and one section per Determination: `Supply` (filled by the Supply glue design's Supply Determination when its task S2 is built; empty until then) and `Intelligence` (section 7). Supply runs first, because isolation and resupply change the communications state that intelligence reads.

Each draw has a stream of its own: `SeedRandom(campaignSeed, boundaryId, "<section>", <taskId>)`, where the task id is the collection task's (or the notional formation's) stable id. A task added or removed therefore shifts no other task's draw, and a draw added to one section never shifts the other. Tasks are processed in the canonical order of their ids, so the event's contents are the same whoever submits them.

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
4. **Terms.** Hand the two pictures, the truth, and the communications states to the glue design's terms step.

The draws and their seeds are written in the boundary event. A replay reads them and never draws again; the same facts and seed always give the same pictures.

**Effective presence is computed when a report is read, never stored.** A report keeps the `Presence` it was made with (its base). Whenever a picture is read, each report's effective `Presence` is:

- its base, lowered one step for each whole ageing interval of its source that has passed since `ObservedAt` (age is counted from observation, never delivery: a late report is not fresh; the Military Command design, section 8.3);
- raised one step if another current report from an independent channel agrees with it on area and type (corroboration, 6.3), at most `Confirmed`;
- dropped from the picture once it would fall below `Possible`.

A `NoneSeen` report has no step below it: it is dropped once one ageing interval of its source has passed. A contradiction (a `NoneSeen` and a positive report on one area, both current) is shown to the side with both reports. Because effective presence is a function of the reports and the current boundary, running the Determination at every boundary never applies ageing or corroboration twice.

## 8. Views and leaks

### 8.1 The view

`CampaignView.Of(log, viewer)` is the viewer-scoped projection (ruling I8). Like ASL's `GameView`, it leaves out what the viewer may not know rather than marking it:

- own formations in full;
- the side's picture (its `SideReport`s), its briefings, its collection tasks and their results;
- its own postures and communications state;
- the events whose `Visibility` names it;
- nothing of the enemy but what its reports say. Its type has no member that holds ground truth.

A briefing builder, a computer-run side, and every Campaign screen take a `CampaignView`, never the log or the facts. The compiler then refuses a leak by construction: the types a side can reach have no field that holds the truth.

### 8.2 The leak audit

Every Campaign surface must pass the audit the Campaign analyses ask for (the Command analysis, lines 63 and 172; the Supply Integration analysis, line 182; the Formation to Squad analysis, line 157):

- a disabled action's reason;
- route highlighting and path costs;
- tactical previews of a Situation;
- forecasts and history (the Implementation Plan, lines 263 to 267);
- a computer-run side's choices, which read only its view (and, under the orders-group option, only its briefed units' knowledge; ruling I12, backlog section 95).

The audit is a test per surface: built from a fixture where the two sides' truths differ, it checks that the surface's output for one side is the same whatever the enemy's hidden state is.

## 9. The briefing and the time to brief

### 9.1 The briefing

For each side and each Situation about to be committed, the briefing lists the reports of that side's picture whose areas overlap the Situation's area: type and strength band, identity where known, source, and age ("enemy infantry, about a company, reported in the woods north of the village; patrol; 6 hours old"). It names Campaign areas, never ASL hexes, so a briefing never pinpoints a unit the card would hide. It is built from the side's view only (8.1), so it can be wrong.

The briefing is delivered to the side's senior leader (the analysis, section 7.6; its optional orders-group SSR is ruling I11). Who that leader is, and what follows on the board, is the glue design's.

### 9.2 The time to brief

From the facts' order and start times (5.4), each committed formation's time to brief falls in a band:

| Echelon receiving the order | Ample | Short | None |
|---|---|---|---|
| Division, corps | 24 hours or more | 8 to 24 hours | under 8 hours |
| Brigade, regiment | 12 hours or more | 4 to 12 hours | under 4 hours |
| Battalion (the company's orders) | 6 hours or more, with daylight to reconnoiter | 2 to 6 hours | under 2 hours |

German formations of good quality shift one band up at battalion and regiment (mission tactics; the 1933 Truppenführung); scratch and Volksgrenadier formations do not. The battalion row decides the card (the glue design). The rule of leaving two thirds of the time to subordinates is post-war US doctrine and serves here only as the rationale for the bands.

## 10. The Knowledge Report intake

PilotHost reads the finished ASL game (the glue design) and gives the Domain library a `KnowledgeReportFacts` record per side: the enemy units revealed (with their Campaign area, kind, and size), the enemy stacks seen only as "?", the enemy units captured, the areas searched and found empty, and the side's Good Order leaders at the end with the radios and field phones they possess. The library applies the report channel rule (ruling I10):

1. **Radio or wire: delivered at this boundary.** A Good Order leader on board with a working radio, or a field phone not eliminated: every report at full grain.
2. **Carried back: next boundary, one step lower.** Otherwise, if any Good Order leader survives.
3. **Lost.** Otherwise: one `Possible` report for the Situation's area.

Then:

- revealed units become `Confirmed` reports of type and strength (an ASL unit seen does not tell its formation's name); "?" stacks become `Probable`, strength unknown, and the adjudicator's copy of one made from Dummies alone is marked `Deceptive`; searched-empty areas become `NoneSeen`;
- captured units become prisoners in the facts of the next boundary, where interrogation (6.2) can yield identity;
- radios lost, broken, or captured change the communications state (6.4); a radio captured by the enemy sets `CodesCompromised` on its formation.

The intake is one Campaign event, keyed by the ASL game id and its last revision. A second intake with the same key is refused; a later revision of a game already taken in is refused unless the Campaign explicitly reopens that result. This is the Supply Report's rule (the Supply glue design, section 6.3).

## 11. Interaction with supply

- One boundary event holds both sections; Supply runs first.
- Isolation (the Supply Determination's `Isolated`) cuts `Wire` and blocks radio replacement through supply.
- The Supply glue design's section 8 ("an enemy pocket is known only as far as the Campaign's intelligence rules disclose it") is met by this design: a side learns of an enemy pocket only through reports in its picture.

## 12. Ownership

| Part | Owner |
|---|---|
| Identities, viewer, event, log, view, boundary event, intelligence profile, reports, Determination, postures, communications state, briefing, time to brief, intake rules | `LimboDancer.Campaign.Domain` |
| Boundary and view endpoints; reading the ASL game record into `KnowledgeReportFacts`; mapping viewers to ASL perspectives | `LimboDancer.Campaign.PilotHost` |
| Submitting boundary facts; showing the view | The prototype now; the Blazor app from C02 |
| The card terms and every ASL effect | The glue design; ASL Rules |

## 13. Memory and performance

Answers to the [guidelines'](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>) checklist:

- **Work added.** The Determination runs once per Situation boundary: one draw per collection task (tens per side), one pass over each side's reports (hundreds at most). A view is projected per request from the log; no cache until a measurement asks for one. Nothing is added to any ASL Play step, render, or read path.
- **Ownership, keys, lifetime.** Records are immutable and keyed by event sequence; facts by hash; the intake by game id and revision. The log grows by one boundary event and one intake event per Situation.
- **Disclosure, snapshots, seams.** Section 8. The view is the only seam a side reads.
- **Counters and fragments.** None; this design adds no ASL counter.
- **Checks.** Unit tests in the Domain test project; the leak audit per surface; replay equality.
- **Tests.** Fixtures built once per test class; the test project joins the Campaign solution and its CI.
- **Benefit and noise.** No performance claim is made.
- **Records.** Each task records its results, limits, backlog rows, and any build outputs outside the repository.

## 14. Tasks, in order

| Task | Work | Tests |
|---|---|---|
| K0 | Scaffold `LimboDancer.Campaign.Domain` and its test project in `Campaign.slnx`, with lock files | Green Campaign CI |
| K1 | Identities, `CampaignViewer`; move `SeedRandom`, Refinement references Domain | Golden vectors: the `sha256-counter-v1` stream is unchanged |
| K2 | `CampaignEvent` and `CampaignLog` | Append, stale-revision refusal, prefix hash, adjudicator-only default, replay equals live |
| K3 | `CampaignView` (I8) | No ground truth or `Deceptive` value reachable from a side's view |
| K4 | `SideReport`, `AdjudicatorReport`, the picture, grain, ageing | Ageing from `ObservedAt`; grain per area |
| K5 | `BoundaryFacts` and the boundary event with empty sections | Facts hash and seed recorded; Supply slot present |
| K6 | The intelligence profile and the Determination (7.2) | Same facts and seed give the same picture; replay draws nothing; each historical case of the analysis, section 3.6, reproduced by a fixture |
| K7 | Postures, deception, corroboration | Contact breaks deception; two channels confirm |
| K8 | Communications state | Issue, loss, wire laid and cut, codes compromised |
| K9 | Briefing and time to brief | Two sides get different briefings from one truth; bands by echelon |
| K10 | PilotHost endpoints | Round trip; the prototype shows a side's view |
| K11 | Knowledge Report intake rules | The three channel cases; applied once; second apply refused |

K0 to K9 and K11 are Campaign work with no ASL reference. K10, and the reading of the ASL record that feeds K11, are PilotHost's. The glue design's tasks follow.

## 15. Acceptance

- A side's view never contains a value from which the enemy's ground truth, or a report's `Deceptive` flag, can be read.
- The same boundary facts and seed always give the same pictures, briefings, and terms; a replay draws nothing.
- Every number the Determination uses comes from a pinned, versioned profile, labeled as authored.
- A late report is never fresh.
- An ASL result is taken in exactly once.
- The Domain library references no ASL project, and no ASL project references the Campaign.
- Existing `SeedRandom` seeds replay unchanged.

## 16. Decisions made in this design

Made on 2026-11-01 under the user's instruction to resolve open questions by expert sub-agents and act on the best recommendation. Each is open to the user's veto.

| Id | Question | Decision | Basis |
|---|---|---|---|
| CD1 | Where the knowledge model lives | A new C# library, `src/Campaign/LimboDancer.Campaign.Domain`, with no ASL reference; PilotHost hosts it until the Blazor app does | An architecture sub-agent's review of the Implementation Plan and the Campaign code; the user's note that the Campaign moves to C# and Blazor |
| CD2 | Where the planned Blazor app goes | Recommended, not decided: under `src/Campaign/` rather than the Plan's `src/LimboDancer.Campaign/`, so the Campaign CI trigger covers it. The decision belongs to C01 | The CI workflow triggers on `src/Campaign/**` |
| CD3 | Reuse ASL's `Perspective` | No: a Campaign `CampaignViewer` of the same shape, mapped in PilotHost | The Plan's no-ASL-reference rule; viewers become headquarters later |
| CD4 | Default visibility of a Campaign event | The adjudicator alone | Hiding is the safe failure |
| CD5 | One boundary pipeline for supply and intelligence | Yes, one event with one section each, Supply first, separate random streams | Isolation feeds communications; streams keep sections independent |
| CD6 | `SeedRandom`'s home | Move it into Domain with its own canonical key serializer; golden-vector test | Domain may not reference ASL Maps |
| CD7 | Disclosure in the prototype period | Not enforced; labeled so on the prototype's screens | The prototype holds the truth in the browser until C02 |
| CD8 | Starting values of the intelligence profile | The tables of section 6, labeled as authored | A historian sub-agent's proposal; every number is a tuning knob |
| CD9 | A Miss produces a report or nothing | A `NoneSeen` report | Negative evidence exists and can be false |
| CD10 | Corroboration | Two independent channels raise `Presence` one step | Lets planned multi-channel deception succeed, single-channel deception fail |
| CD11 | Ageing and corroboration: stored or computed | Computed when a report is read, from its base and the current boundary; `NoneSeen` dropped after one interval | A reviewer found that applying them at every boundary would repeat them |
| CD12 | Random streams | One stream per draw, keyed by the task's stable id, tasks in canonical order | A task added must not shift another task's draw |
| CD13 | A deception area holding true formations | Contact draws against the true formations only; the notional contributes nothing | Otherwise a notional formation would hide real ones from patrols |

## Related documents

- [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>)
- [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>)
- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>)
- [LimboDancer.Campaign Implementation Plan](<../LimboDancer.Campaign Implementation Plan.md>)
- [Military Command and Multiscale Simulation Design](<../../../docs/Military Command and Multiscale Simulation Design.md>)
