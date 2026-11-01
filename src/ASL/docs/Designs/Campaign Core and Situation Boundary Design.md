# Campaign Core and Situation Boundary Design

Date: 1 November 2026

Status: proposed design. Nothing is built. Implementation waits for the user's approval. The user asked on 2026-11-01 that the foundation shared by supply and intelligence be one design, with each subject's own design built on it. This design is that foundation. Its decisions (section 13) were researched by sub-agents and are open to the user's veto.

Built on it:

- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>) (its rulings G1 to G7 approved 2026-11-01);
- [Campaign Intelligence Design](<Campaign Intelligence Design.md>) and [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>) (on rulings I1 to I12, approved 2026-11-01);
- [Campaign Dynamic SSR Design](<Campaign Dynamic SSR Design.md>), which turns the Campaign's terms into the card's special rules and gives them an authoring workflow.

## 1. Purpose and scope

Every Campaign subject that reaches ASL does the same four things: it reads the Campaign's state at a Situation boundary, decides something by a deterministic step, puts terms on the Scenario Card, and reads the finished game back once. This design provides those four things once:

1. **The Campaign core**: where the code lives, identities, viewers, the event log, and a view per side (section 4 to 6).
2. **The Situation boundary**: the facts snapshot, one boundary event with a section per subject, and seeded randomness (section 7).
3. **The handoff onto a Scenario Card**: the card built from the handoff, the terms field, and setup areas (section 8).
4. **The result intake**: one event per finished game, with a section per subject, applied exactly once (section 9).

Out of scope: each subject's own rules (its design), every ASL rule (the Rules project), and the Campaign's movement, orders, and supply chains, which stay in the prototype until the [Implementation Plan's](<../LimboDancer.Campaign Implementation Plan.md>) stages C02 and C06.

### Words used across these designs

- **`CampaignTerms`**: the record of inputs one committed Situation hands to ASL (section 8.2): each subject's states and counts, never the card's rules themselves.
- **Term rule**: a C# rule of the Campaign that reads `CampaignTerms` and decides one kind of card term (the Campaign Dynamic SSR Design, section 4).
- **Card term**: what a term rule decides: a special rule, a card setting (a group's Dummies, setup areas), or a side's private cause.

## 2. What exists today

Surveyed on 2026-11-01:

- **The C# Campaign** (`src/Campaign`) is a Goodwood terrain refinement and setup bridge into ASL: the Refinement library, PilotHost (a minimal web host on port 8093 that serves the JS prototype and prepares the handoff), a command-line tool, and one test project. It has no campaign state, no event log, no boundaries, and no intake of ASL results.
- **Campaign play** lives in the browser prototype `prototypes/europe-hex-map`. Its `CampaignContext` reaches C# as an opaque JSON string, hashed into `SituationHandoff`.
- **The handoff** (`SituationHandoffs.Prepare`) starts an ASL game through the raw start path, with every placement fixed at its deployment position and `Concealed` and `Hidden` false (`SituationHandoff.cs:69`). It builds no Scenario Card and sends no SSR. The raw path (`GamePlanner.Start`) checks only the form of night, weather, HIP, Fanaticism, and substitution tokens (`NightAndWeatherRulesBar`); the card path adds the card checks, including the binding of HIP and Fanaticism tokens to the card's sides and groups (`ScenarioCards.Validate`, `ScenarioA1SetupCalculator.RuleDiagnostics`). Unknown tokens pass both paths silently (backlog section 42, "SSR tokens with an unknown prefix refused").
- **Determinism tools exist**: `SeedRandom` (`Refinement/SeedRandom.cs`, stream `sha256-counter-v1`) and content-addressed immutable JSON saves (`SituationSessions`).
- **The direction is set.** The Campaign moves to C# and Blazor (the user, 2026-11-01). C01 to C07 build without references to ASL projects (Plan, line 21); domain code does not depend on Razor, the DOM, or JavaScript (line 91); events carry a visibility policy (line 275); authoritative state is kept apart from viewer-filtered knowledge (line 277).

## 3. Principles

1. **Ground truth stays on the server.** A browser that holds the whole truth cannot keep it from its user. Every per-side value is computed in C# and reaches a viewer only through that viewer's view.
2. **Hidden unless granted.** A Campaign event is visible to the adjudicator alone unless it names who else may see it; the reverse of ASL's `GameEvent`, where no list means public.
3. **A Determination is a calculator.** It takes facts, prior state, parameters, and a seed, and returns a verdict. Its caller writes the event. This is the Rules boundary of AGENTS.md applied inside the Campaign.
4. **The Campaign decides the terms; ASL decides their effects** (the Supply glue design, principle 1).
5. **Every game is a card** (the user's ruling of 2026-09-29).
6. **State crosses at the boundary only**: at commitment and at the result, never during play.
7. **Deterministic, with recorded randomness.** A Determination that needs chance uses a seeded draw recorded in its event; one that needs none (the Supply Determination) draws nothing. A replay never draws.
8. **The core does not own campaign play.** Until C02 and C06, movement, orders, and supply chains are the prototype's; the core receives their result at each boundary as a facts snapshot.
9. **ASL counts come from Rules.** Where a Campaign rule needs an ASL quantity (a side's on-board squad-equivalents, A5.5 and ruling R70.22; whether a vehicle may serve as an Ammo Vehicle, E10.11), PilotHost asks the Rules calculator that decides it and hands the Domain library the answer as a number or a flag. The Domain library never counts by ASL rules itself.

## 4. Where it lives

**A new library, `src/Campaign/LimboDancer.Campaign.Domain`**: a plain net10.0 class library, with no Razor, no ASP.NET, and no ASL reference. Its tests are `src/Campaign/tests/LimboDancer.Campaign.Domain.Tests`. Both join `src/Campaign/Campaign.slnx`, so `campaign-ci.yml` runs them, outside the ASL gate (the user's ruling of 2026-10-15), with lock files restored in locked mode.

- It is the Implementation Plan's Domain and Application code, written before the Blazor app exists. When C01 starts, `LimboDancer.Campaign` references it.
- It is not part of Refinement, which references ASL Units and Maps.
- Everything that touches ASL stays in **PilotHost**, which already references ASL Play: building the card, reading an ASL game record. PilotHost turns ASL facts into Campaign records; the Domain library applies Campaign rules to them. No ASL project references the Campaign.

**`SeedRandom` moves** into the Domain library, with a small canonical serializer of its own for its string-array key, so the library needs no ASL reference. Refinement then references Domain. A golden-vector test proves the `sha256-counter-v1` stream is unchanged byte for byte, so every existing seed replays.

**In the prototype period**, PilotHost hosts the library behind two endpoints:

- `POST /api/campaign/{campaignId}/boundaries`: the prototype submits a boundary's facts (section 7.1); PilotHost runs the Determinations, appends the boundary event, and returns its id.
- `GET /api/campaign/{campaignId}/view?viewer=<side>`: returns that side's view.

During this period the prototype still holds the truth for movement and supply, so disclosure is not enforced against a determined user; the prototype's screens are labeled "disclosure not enforced in the prototype". Enforcement becomes real when the Blazor app (C02) takes the Campaign state onto the server.

## 5. Identities, viewers, events

### 5.1 Identities

`CampaignId`, `SideId`, `FormationId` (opaque, taken from the prototype's formation ids, which the Refinement roster carries as `SituationFormation.Id`), `AreaId` (a Campaign hex or an authored region), `SituationId`, `BoundaryId`, and `CampaignTime` (minutes from the campaign's start).

### 5.2 The viewer

`CampaignViewer`: a side, or the adjudicator, who sees everything. It mirrors ASL's `Perspective` in shape (a name and an adjudicator constant) but is its own type: the Domain library may not reference ASL, and a Campaign viewer will become a headquarters later (intelligence ruling I1), which an ASL perspective never will. PilotHost maps one to the other.

### 5.3 The event and the log

`CampaignEvent`: the Implementation Plan's fields (line 275): `Id`, `CampaignId`, `Sequence`, `EffectiveAt`, `RecordedAt`, `Kind`, `PayloadVersion`, `CommandId`, `CorrelationId`, `AffectedEntityIds`, `Visibility`, and the hash of the log's prefix through this event. The prefix hash is this design's addition: the Military Command design binds its checkpoints to an event-prefix hash (section 7), and storing it on every event lets any revision be checked the same way.

- `Visibility` is a list of viewers; an empty list means the adjudicator alone.
- Recorded time comes from the host's clock; effective time is campaign time. Replays use the recorded values and never read the clock.

`CampaignLog`: append-only, one writer, each append checked against the expected revision so a stale writer fails rather than overwrites. Stored as content-addressed immutable JSON, as `SituationSessions` stores saves today.

## 6. The view

`CampaignView.Of(log, viewer)` is the viewer-scoped projection (intelligence ruling I8). Like ASL's `GameView`, it leaves out what the viewer may not know rather than marking it. A side's view holds its own formations in full, its own subject state (supply steps, pictures, briefings, communications), the events whose `Visibility` names it, and nothing of the enemy but what its own records say. Its type has no member that holds ground truth.

Every Campaign screen, briefing builder, and computer-run side takes a `CampaignView`, never the log or the facts, so a leak cannot compile. Each subject design adds its part of the view and its leak tests. The leak audit (the Campaign Intelligence Design, section 8) applies to every surface, supply's included: a disabled action's reason, route highlighting and path costs, tactical previews, forecasts and history, a computer side's choices.

## 7. The Situation boundary

### 7.1 Boundary facts

`BoundaryFacts` is the ground truth the Determinations need, submitted by the caller (the prototype now, the Blazor app later), hashed, and stored with the boundary event, visible to the adjudicator alone. It has a common part and a part per subject:

| Part | Contents |
|---|---|
| Common | `SituationId`; phase (`Before` commitment or `After` the result); campaign time; weather class per area; day or night; each formation's id, side, area, type class, strength band, quality class, and whether it has held its ground since the previous boundary |
| Supply | The route graph's segments with their state (open, enemy-controlled, blocked); supply sources and receiving points; shipments received and packages issued since the last boundary (the Supply glue design, section 4) |
| Intelligence | Formation identities as an enemy would learn them; notional formations; collection tasks; security postures; radios and wire changes; order and start times; prisoners held (the Campaign Intelligence Design, section 5) |

A subject's part may be empty; its Determination then runs on the common part alone or records nothing.

### 7.2 The boundary event

`SituationBoundaryEvent` is one Campaign event per boundary: `SituationId`, phase, the input revision, the facts hash, the boundary's seed, and one section per subject. Sections run in a fixed order, each reading the sections before it:

1. **Supply** (the Supply Determination; the Supply glue design, section 4). No draw.
2. **Intelligence** (the Intelligence Determination; the Campaign Intelligence Design, section 7). Seeded draws. It reads Supply's isolation, which cuts wire and blocks radio replacement.
3. **Terms** (`Before` phase only): each subject's terms for the Situation about to be committed (section 8.2), then the card's special rules generated from them (the Campaign Dynamic SSR Design).

A subject not yet built leaves its section empty; the event's shape does not change when it is built.

### 7.3 Randomness

Each draw has a stream of its own: `SeedRandom(campaignSeed, boundaryId, "<section>", <itemId>)`, where the item id is the stable id of the task or record being drawn for. An item added or removed shifts no other draw, and a draw added to one section never shifts another. Items are processed in the canonical order of their ids. The draws are written in the event; a replay reads them.

## 8. The handoff onto a Scenario Card

### 8.1 The card (Supply task S3)

`SituationHandoffs.Prepare` builds a Scenario Card for the Situation, starts the game from it through the card path, and so checks every SSR token through `ScenarioCards.Validate` and `ScenarioA1SetupCalculator.RuleDiagnostics`. Each field the card checks require comes from a named place:

| Card field | Source |
|---|---|
| Title, date, introduction, source, turns, victory conditions, Scenario Defender, SAN, ELR, entry edges, Situation SSRs | The Situation's **base card**, one authored file per Situation (the Campaign Dynamic SSR Design, section 7.2). For Goodwood, the first, it is written by hand; the editor's base mode comes later |
| Sides and nationalities | The handoff (`allied` and `german` today) |
| Boards | The Situation's assembled board, which the Studio already receives through the campaign import (`CampaignImport`) |
| OB groups and placements | The accepted deployment, one group per parent formation |
| Setup areas | At fixed positions, each group's area is the set of hexes its placements occupy, so the card's setup check holds; with setup areas requested, section 8.3 |
| Generated SSRs and card settings | The term rules (the Campaign Dynamic SSR Design) |

A card start records a `scenario` reference (id, SHA-256, title) in the game's `GameStarted` event and runs the card checks, which today's raw starts do not. Games already recorded keep their records and replay unchanged; only new handoffs start from cards.

### 8.2 The terms field

The handoff gains one field, `CampaignTerms`, beside `CampaignContext` (not inside it: `CampaignContext` is an opaque string the prototype builds, and the terms are computed in C#). It holds:

- `Version`, `CampaignId`, `SituationId`, the boundary event it was computed from, and that event's hash;
- one section per subject, holding that subject's inputs only: `Supply` (the Supply glue design's `SupplyTerms`) and `Intelligence` (`IntelligenceTerms`), including the ASL counts PilotHost obtained from Rules (principle 9);
- the card terms the term rules decided from those inputs, with their origins, and the adjudicator's suppressions (the Campaign Dynamic SSR Design, sections 4 and 5.5);
- each side's private part (supply causes, briefings), visible to that side and the adjudicator alone;
- the id and SHA-256 of the card built from it, and the hash of the Situation's map ownership (8.4).

The handoff version moves from `campaign-goodwood-handoff-1` to a new version, and the version and `CampaignTerms` (with the card's SHA-256) enter the handoff hash, so every new handoff, terms or none, has a new hash and a new game id. Games already recorded keep their records. Both Studio readers of the version, `CampaignContextSummary.Read` and `CampaignCounterOrigin`, read both versions.

### 8.3 Setup areas

A subject may need the card to give OB groups setup areas instead of fixed positions: Dummies need somewhere to be placed, and E1.23 Recon needs the Attacker to set up after it (the intelligence glue design, section 4.1). When a subject's terms ask for it, each group's setup area is the ASL hexes of the formation hex its parent formation occupies in the accepted deployment (the Military Command design, section 14.3: a parent hex is a coarse location, not an instruction to place every squad at its center). Setup then runs in the Studio by setup plans, in the card's setup order. Otherwise positions stay fixed, as today.

### 8.4 The map ownership

The Situation's map ownership (`SituationMapOwnership`, which maps each owned ASL tactical center to its formation hex) is documented as "never part of game history". The result intake needs it, so the handoff stores the ownership with the Situation's session and its hash in `CampaignTerms`. The board's margin hexes have no parent; a margin hex maps to the parent of the nearest owned center, ties going to the lower formation hex id.

### 8.5 Where generated cards are kept

The planner finds a card only in its library, by id and SHA-256, at the start and again during setup (`GamePlanner.cs:661-666`). `ScenarioCardLibrary` reads the built-in cards and one user folder (`boards/cards` in the Studio, `LivePlay.cs:37`); PilotHost's planner has no library at all and uses the built-in cards. So:

- `ScenarioCardLibrary` gains a second, read-only folder for Campaign cards (a change to ASL Play, by pull request and the merge gate). Its cards are found by id and SHA like any other, are left out of the user's card lists, and cannot be saved over or deleted through the library's `Save` and `Delete`.
- PilotHost's planner and the Studio's `LivePlay` are both given that folder.
- A Campaign card's id is `campaign-` followed by 12 hexadecimal characters of the hash of its campaign, Situation, boundary event, SSR library version, and suppression set (the Campaign Dynamic SSR Design, section 5.5). The id fits the card name rule (`^[a-z0-9][a-z0-9-]{0,63}`) whatever the campaign id's length, and no user card can take it, since user cards never use the `campaign-` prefix (a check the library gains with the folder).

## 9. The result intake

When an ASL game ends, PilotHost replays its record once and builds a `SituationResultFacts` record: a common part (each unit's status and possessor at the end, losses, captures, each leader's Good Order state judged by `ScenarioA1Definitions.GoodOrderOf` in Rules) and a part per subject (the Supply glue design, section 6.1; the intelligence glue design, section 6.1), with every ASL hex turned into a Campaign area through the stored map ownership.

The Domain library applies it as one `SituationResultEvent` with a section per subject, in the boundary's order (Supply, then Intelligence). The event is keyed by the ASL game id and the revision of its last event. An intake with a key already applied is refused; a later revision of a game already taken in is refused unless the Campaign explicitly reopens that result. Reopening a screen applies nothing. The `After` boundary then runs on the updated state.

## 10. Ownership

| Part | Owner |
|---|---|
| Identities, viewer, event, log, view, boundary facts and event, randomness, the terms field's shape, the result event | `LimboDancer.Campaign.Domain` |
| The boundary and view endpoints; building the card; storing the card and map ownership; reading the game record into `SituationResultFacts`; mapping viewers to ASL perspectives | `LimboDancer.Campaign.PilotHost` |
| Submitting boundary facts; showing the view | The prototype now; the Blazor app from C02 |
| Each subject's Determination, terms, and intake rules | Its design |
| Every ASL effect | The Rules project |

## 11. Memory and performance

Answers to the [guidelines'](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>) checklist:

- **Work added.** Per Situation: one boundary event before, one result intake, one boundary event after; one card build; one replay of the finished game. A view is projected per request; no cache until a measurement asks for one. Nothing is added to any ASL Play step, render, or read path.
- **Ownership, keys, lifetime.** Immutable records keyed by event sequence; facts by hash; the intake by game id and revision; the card and map ownership stored with the Situation's session.
- **Disclosure, snapshots, seams.** Section 6. The view is the only seam a side reads.
- **Counters and fragments.** None added by the core.
- **Checks.** Unit tests in the Domain test project; replay equality; a handoff round trip; existing handoff games replay unchanged.
- **Tests.** Fixtures built once per test class; the test project joins the Campaign solution and its CI.
- **Benefit and noise.** No performance claim is made.
- **Records.** Each task records its results, limits, backlog rows, and any build outputs outside the repository.

## 12. Tasks, in order

| Task | Work | Tests |
|---|---|---|
| CB1 | Scaffold `LimboDancer.Campaign.Domain` and its test project in `Campaign.slnx`, with lock files | Green Campaign CI |
| CB2 | Identities, `CampaignViewer`; move `SeedRandom`, Refinement references Domain | Golden vectors: the `sha256-counter-v1` stream is unchanged |
| CB3 | `CampaignEvent` and `CampaignLog` | Append, stale-revision refusal, prefix hash, adjudicator-only default, replay equals live |
| CB4 | `CampaignView` | No ground-truth member reachable from a side's view |
| CB5 | `BoundaryFacts`, the boundary event with empty sections, the PilotHost endpoints | Facts hash and seed recorded; sections in order; round trip from the prototype |
| CB6 | The Campaign card folder in `ScenarioCardLibrary` (8.5); the Goodwood base card; the handoff builds a Scenario Card (was Supply task S3); the map ownership stored | Existing handoff games replay unchanged; a new handoff's card passes the card checks and is found during setup; the folder's cards are not in user lists and cannot be saved over |
| CB7 | `CampaignTerms` beside `CampaignContext`; the new handoff version and hash | Both versions read by both Studio readers; the card's SHA-256 in the hash |
| CB8 | Setup areas from the deployment (8.3) | A group's area is its formation hex's ASL hexes; fixed positions without the request |
| CB9 | `SituationResultFacts`, `SituationResultEvent`, exactly once | Applied once; a second apply refused; a later revision refused unless reopened |

The subject designs' tasks follow: the Supply glue design's S2, S4 to S9; the Campaign Intelligence Design's K1 to K6; the intelligence glue design's IG1 to IG9; the Campaign Dynamic SSR Design's DS1 to DS9.

## 13. Decisions made in this design

Made on 2026-11-01 under the user's instruction to resolve open questions by expert sub-agents and act on the best recommendation. Each is open to the user's veto. The decisions CD1 to CD7 were first made in the Campaign Intelligence Design and move here with the core.

| Id | Question | Decision | Basis |
|---|---|---|---|
| CD1 | Where the core lives | `src/Campaign/LimboDancer.Campaign.Domain`, with no ASL reference; PilotHost hosts it until the Blazor app does | An architecture sub-agent's review of the Implementation Plan and the Campaign code; the user's note that the Campaign moves to C# and Blazor |
| CD2 | Where the planned Blazor app goes | Recommended, not decided: under `src/Campaign/`, so the Campaign CI trigger covers it. The decision belongs to C01 | The CI workflow triggers on `src/Campaign/**` |
| CD3 | Reuse ASL's `Perspective` | No: a `CampaignViewer` of the same shape, mapped in PilotHost | The Plan's no-ASL-reference rule; viewers become headquarters later |
| CD4 | Default visibility of a Campaign event | The adjudicator alone | Hiding is the safe failure |
| CD5 | One boundary for supply and intelligence | One event, one section per subject, Supply first, then Intelligence, then Terms | Isolation feeds communications; the user asked for one foundation |
| CD6 | `SeedRandom`'s home | Moved into Domain with its own canonical key serializer; golden-vector test | Domain may not reference ASL Maps |
| CD7 | Disclosure in the prototype period | Not enforced; labeled so on the prototype's screens | The prototype holds the truth in the browser until C02 |
| CD14 | Where the terms ride | One `CampaignTerms` field beside `CampaignContext`, one section per subject, inputs only | `CampaignContext` is opaque and built by the prototype; the Supply design had put its terms inside it |
| CD15 | One result intake or one per subject | One event with a section per subject, keyed by game id and revision | Two intakes of one game could disagree |
| CD16 | Who owns the card-building task | The core (CB6), formerly Supply's S3 | Every subject's terms need it |
| CD17 | Setup areas | In the core, used when a subject's terms ask for them | Intelligence needs them now; others may later |
| CD18 | Where generated cards are kept | A second, read-only Campaign folder in `ScenarioCardLibrary`, given to PilotHost and the Studio; ids `campaign-` and 12 hexadecimal characters | A reviewer found that no planner could find a generated card, and that user-folder cards could be overwritten |
| CD19 | Who counts by ASL rules | Rules calculators, asked by PilotHost; the Domain library receives numbers | Squad-equivalents and Ammo Vehicle eligibility are ASL rules |

Ids CD8 to CD13 are the Campaign Intelligence Design's; the core and that design share one sequence.

## 14. Acceptance

- The Domain library references no ASL project, and no ASL project references the Campaign.
- A side's view has no member from which ground truth can be read.
- The same boundary facts and seed always give the same boundary event; a replay draws nothing.
- Every committed Situation is played from a Scenario Card whose tokens pass the card checks.
- A finished game is taken in exactly once.
- Existing handoff games replay unchanged; existing `SeedRandom` seeds replay unchanged.

## Related documents

- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>)
- [Campaign Intelligence Design](<Campaign Intelligence Design.md>)
- [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>)
- [Campaign Dynamic SSR Design](<Campaign Dynamic SSR Design.md>)
- [LimboDancer.Campaign Implementation Plan](<../LimboDancer.Campaign Implementation Plan.md>)
- [Military Command and Multiscale Simulation Design](<../../../docs/Military Command and Multiscale Simulation Design.md>)
