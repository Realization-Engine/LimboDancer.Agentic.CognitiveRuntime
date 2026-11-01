# Campaign Dynamic SSR Design

Date: 1 November 2026

Status: proposed design. Nothing is built. Implementation waits for the user's approval. Its decisions (section 12) were made under the user's instruction of 2026-11-01 to resolve open questions and act on the best recommendation; each is open to the user's veto.

Built on the [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>). Used by the [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>) and the [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>).

## 1. The problem

The user's point (2026-11-01): ASL play is built on free-standing SSRs, written once for a printed or authored scenario card. A Campaign Situation needs SSRs that change with the Campaign's decisions and its supply and intelligence outcomes: the same Situation, committed at a different boundary, carries different special rules. That calls for SSRs generated from Campaign state, and for a workflow to author how they are generated.

## 2. What exists today

Surveyed on 2026-11-01:

- **The model.** `ScenarioCardRule(Number, Text, Status, Tokens, Rules, Note)` (`Play/ScenarioCards.cs:75`, ruling R17.10). Status is `token` (the game reads its tokens), `game-default` (the game already plays it), or `not-enforced` (shown only, with a note). A rule carries no origin, side, visibility, or version. A card is identified by the SHA-256 of its LF text; its format is `asl-scenario-card/1`, parsed strictly (an unknown member is refused), so a new field must be optional. The optional `Minigame` property (`JsonIgnore` when null) shows how a field can be added without changing an existing card's text or SHA.
- **Validation.** `ScenarioCards.Validate` checks numbering, statuses, tokens present exactly when the status is `token`, notes for `not-enforced`, citations (each must be one of the 26 fragments in `CitableRules` or a ruling id `R...`), and binds `hip:`, `fanatic:`, and substitution tokens to the card's sides and groups; `ScenarioA1NightAndWeather.RulesBar` checks the forms of the night, weather, HIP, Fanaticism, and substitution tokens. Any other token passes silently (backlog section 42 asks for one token list and the refusal of unknown prefixes).
- **The game record.** `GameStarted` stores the tokens only (`SpecialRules`, as strings) and the card's reference (id, SHA-256, title). The text, status, and citations live in the card, which the planner must still find in its library by id and SHA during setup (`GamePlanner.cs:661-666`).
- **Authoring.** The Studio's card editor (`CardEditor.razor`, `CardRulesFields.razor`, redesigned in PR 95) edits each SSR by hand: a status, free text, and a "Details" fold with free-text tokens, cited rules, and note. It has no token picker, no templates, no locked rules, and no view of where a rule came from. Every reader sees the same SSR text (`PlayCardPanel.razor`).
- **The Campaign** builds no card yet (the core's task CB6 will). The prototype's Situations carry their printed special rules as prose (`situation.schema.json`, `specialRules`).

## 3. Principles

1. **Two kinds of SSR on one card.** *Situation SSRs* are authored once per Situation, as today's free-standing SSRs are: the historical and scenario rules that hold whatever the Campaign does. *Generated SSRs* are produced at each boundary from the Campaign's terms. A Campaign card carries both.
2. **Authors write parameters and words, not logic.** The logic that decides whether a generated SSR appears, and with what count, is a *term rule*: C# code in the Campaign Domain library, one per kind of term, with its tests. What an author changes in the workflow is a term rule's parameters (its thresholds and tables), its texts, and whether it is enabled. No scripting language is introduced.
3. **The Campaign decides whether and how many; Rules decides what follows** (the core's principle 4). A term rule decides a structured card term (a token family, a side, its arguments); PilotHost writes it as a token through the Rules project's token registry (section 6), so the Domain library never holds an ASL token's grammar. Every effect is Rules'.
4. **The card is public; causes are private.** A generated SSR's text says only what both players may know. Why it is there (which formations, which reports) is in the side's private part of `CampaignTerms` (core, section 8.2).
5. **A generated card is immutable.** Regenerating makes a new card with a new id; a game is bound to the card it started from.
6. **Generated and authored rules look alike in play** and differ in their origin, which is shown.

## 4. Term rules

### 4.1 What a term rule is

A term rule is a C# type in `LimboDancer.Campaign.Domain` with:

- an id and a version (for example `supply.ammo-shortage@1`);
- a parameter schema (typed fields with defaults and bounds);
- the terms it reads (a section of `CampaignTerms`, core section 8.2);
- an `Evaluate(terms, parameters)` that returns zero or more **card terms**, each with the inputs that produced it.

A card term is one of:

| Card term | Becomes | Examples |
|---|---|---|
| A special rule | A structured term (token family, side, arguments) with its public text and cited rules; PilotHost writes it as a `ScenarioCardRule` with tokens formatted by the registry | family `hip`, side `german`, count 2, written `hip:german:2` |
| A card setting | A field of the card's sides or groups | a group's `Dummies`; the request for setup areas (core, section 8.3); the card's setup order |
| A private cause | A line in one side's private part of `CampaignTerms` | "1st Airborne Division, isolated since boundary 2" |

Evaluate is a pure function. It never reads the clock or draws, so the same terms and parameters always give the same card terms. Any ASL quantity it needs (a side's squad-equivalents, a group's squads, a vehicle's eligibility as an Ammo Vehicle) is already in `CampaignTerms`, counted by Rules calculators that PilotHost asked (the core's principle 9).

### 4.2 The first term rules

| Term rule | Reads | Emits | Owner design |
|---|---|---|---|
| `supply.ammo-shortage` | Supply terms | `ammo-shortage:<side>`, citing A19.131 | Supply glue, 5.2 (task S4) |
| `supply.ammo-dump`, `supply.ammo-vehicle` | Supply terms | `ammo-dump:`, `ammo-vehicle:` (formats decided by the E10 claim) | Supply glue, 5.3 (task S6) |
| `intelligence.hidden-setup` | Intelligence terms | `hip:<defender>:<n>`; each Defender group's `Dummies`; the setup-areas request | Intelligence glue, 4.1 (task IG3) |
| `intelligence.recon` | Intelligence terms | `recon:<attacker>` (format decided by the E1.23 claim) | Intelligence glue, 4.2 |
| `intelligence.civilians` | Intelligence terms | `civilians:...` (format decided by the E2.4 claim) | Intelligence glue, 4.3 |
| `intelligence.orders-group` (optional, off by default; manufactured) | Intelligence terms | `orders-group:<attacker>:<band>` | Intelligence glue, 4.6 |

A term rule whose token family is not yet in the registry (section 6) cannot be enabled: the workflow shows it as waiting on its claim.

### 4.3 Order and numbering

A Campaign card's SSRs are numbered in one deterministic order: first the Situation SSRs in their authored order; then the generated SSRs, by term rule in the library's order (supply rules, then intelligence rules, then any later subject), and within one term rule by side id. Numbers are assigned after suppressions are removed (5.5), so a card's SSRs are always numbered 1 to n, as the card check requires.

**Who owns what.** Each enabled term rule owns its token families and card settings. A card setting that two term rules both set is refused by the library's check (section 7.3). So is a Situation's base card that carries a token family or a card setting an enabled term rule owns (a base card's own `hip:` SSR or `Dummies`, for instance): the base card's printed value becomes the term rule's parameter instead (the intelligence glue design's balance rule, section 4.1, uses a printed HIP level that way). No order decides a conflict, because none is allowed.

### 4.4 Texts

Each term rule has two text templates, edited in the workflow:

- the **public text**, which may use only *public placeholders*: side names, counts on the card (the HIP allowance, a Dummy count), the cited rules, and labels;
- the **private cause**, per side, which may use the term's private inputs (formation names, report sources, ages).

A public template that uses a private placeholder is refused when the library is checked (section 7.3). This is the leak check for SSR text: the card cannot name a cause even by an author's slip.

**Under the Hidden OB option** (the intelligence glue design, section 4.7, decision GD7) the Defender's HIP allowance and Dummy counts are shown to the Defender only. Then the hidden-setup term rule's public text uses no count placeholder ("The German side may set up some units hidden (A12.3)"), and the Studio's card panel shows the Attacker neither the `hip:` SSR's count nor any group's Dummies. The tokens stay in the game record, which the Rules read; the Hidden OB option is a display option, as GD7 says, and a leak test checks the Attacker's card panel under it.

## 5. The generated card

### 5.1 Assembly

In the `Before` boundary's Terms section (core, section 7.2), the Domain library evaluates the enabled term rules of the campaign's pinned SSR library against `CampaignTerms`, less the adjudicator's suppressions (5.5). PilotHost then assembles the card (core, section 8.1):

1. Start from the Situation's **base card** (section 7.2) and the fields the core fills from the handoff (core, section 8.1).
2. Write each generated special rule's tokens through the token registry, number the SSRs (4.3), and apply the card settings.
3. Validate with `ScenarioCards.Validate`. A card that fails is not committed; the commitment review (section 7.4) shows why.
4. Store it with the Situation's session and in the Campaign card folder (core, section 8.5), under the id `campaign-` and 12 hexadecimal characters of the hash of its campaign, Situation, boundary event, library version, and suppression set. The SHA-256 of its text is its identity, as for any card.

### 5.2 The rule's origin

`ScenarioCardRule` gains one optional property, `Origin`, written only when present (the `Minigame` pattern), so every existing card keeps its text and SHA-256:

| Field | Meaning |
|---|---|
| `Kind` | `situation` (authored for the Situation) or `generated` |
| `TermRule` | For a generated rule: the term rule's id and version |
| `Library` | The SSR library version it came from |
| `Boundary` | The boundary event it was generated at, and that event's hash |
| `Manufactured` | True when the rule is not a printed rule (the orders-group SSR) |

`Origin` carries no cause and no private input: it is public, like the rest of the card. A rule without `Origin` is an ordinary authored rule, as every card's rules are today.

### 5.3 Citations

A generated rule cites printed rules (A19.131, E10.1, E1.23, E2.4), which must be citable on the card: each term rule's task adds the fragments it cites to `CitableRules` (`ScenarioCards.cs:162-167`), so the card check accepts them. A term rule's own Campaign ruling (for example supply ruling G2) is not an ASL citation; it is named in the term rule's documentation and reached through `Origin`, never put in the rule's `Rules` list.

### 5.4 Status

Origin and status are separate. A generated rule whose token the game plays has status `token`; one whose token's rule is claimed but not yet built is not generated at all (section 4.2), so no generated rule is ever `not-enforced`. Situation SSRs use the three statuses as today.

### 5.5 Regeneration

A card's inputs are its boundary event, its library version, and its **suppression set** (the generated rules the adjudicator suppressed, 7.4). Changing any of them generates a new card with a new id; the previous card stays stored, so the review can show the difference. After a game starts, its card never changes. A result reopened by the Campaign (core, section 9) is replayed on its original card.

Suppressing a term rule drops every card term it emitted: suppressing `intelligence.hidden-setup` removes the `hip:` SSR, the Defender's Dummies, and its request for setup areas together. The suppressions are kept in `CampaignTerms` (core, section 8.2), and the card panel reads them there to show "suppressed by the adjudicator", since a suppressed rule is not on the card.

## 6. The token registry (Rules)

The Rules project publishes one list of every SSR token family the game reads: its prefix, its grammar, its cited rules, its status in the inventory (built, claimed, not built), and the calculator that parses it. Backlog section 42 already asks for this ("The token list in one place") so that unknown prefixes can be refused. This design needs it for three things:

- a term rule may emit only registered families, and the library check (section 7.3) refuses one that does not;
- the card check refuses an unknown prefix on a generated card (and, once the backlog row is done, on every card);
- the Studio's SSR fields offer tokens from the registry instead of free text.

The registry is Rules code, not a rule's verdict, and it is split by the board's tracks:

- **The list, a rule claim (Claude's track).** A claim with id `ssr-tokens`, which maps to no inventory row; its claims-table row lists the files it touches (the token parsers in `ScenarioA1NightAndWeather`, `ScenarioA1UnitSubstitution`, `ScenarioA1Definitions`, `ScenarioA1StealthCalculator.SsrRulesBar`, `ScenarioA1FactoryCalculator`, and the others the survey names), shared where other claims hold them. Its scope is the list and a formatting method per family: no rule's answer changes. A claim without inventory rows is a new case for the claim board; this design proposes it and leaves the form to the board's check.
- **Its use, a wiring row (track 3).** Refusing unknown prefixes in `ScenarioCards.Validate` (Play) and offering registered tokens in the card editor (MapStudio). This is backlog section 42's row.

New families (`ammo-dump:`, `recon:`, `civilians:`, `orders-group:`) join the registry with the claims that build their rules.

## 7. The authoring workflow

The workflow has four places, each in the Studio, which already holds the card editor, the card checks, and the token registry's readers. When the Campaign's Blazor app exists (C02), it links to them; the ASL-facing authoring stays with the ASL side, as the Implementation Plan's C08 places the ASL adapter.

### 7.1 The SSR library page

A new Studio page, **Campaign SSRs**, edits an *SSR library*: the ordered list of term rules a campaign uses, each with its enabled switch, its parameters, and its two text templates.

- **Parameters** are edited in typed fields drawn from the term rule's schema (the HIP table of the intelligence glue design's section 4.1 is an editable table; the supply majority threshold a number), with their bounds and defaults, and each change shows its rationale field.
- **Texts** are edited with the placeholders listed beside them, public and private apart.
- **Preview.** The author picks a fixture boundary (a stored `CampaignTerms` from a test fixture or a past boundary of a campaign) and sees the card that would be generated: its SSRs with their origins, its card settings, each side's private causes in that side's view, and the card check's results. Changing a parameter updates the preview.
- **Publish.** The library is checked (7.3); if it passes, it is published as an immutable version, identified by its hash. A campaign pins one version; adopting a newer one in a running campaign is a recorded Campaign event that takes effect at the next boundary, never on a card already generated.

A default library, with the term rules of 4.2 and the parameters of the glue designs, ships as an embedded resource of the Domain library. Published versions are stored beside the user's cards.

### 7.2 The Situation's base card

The card editor gains a **Situation base** mode: it edits a card whose boards and sides come from the Situation and whose OB groups are filled at handoff, and whose SSRs are the Situation SSRs. The prototype's printed special rules (prose) are the author's source: each becomes a Situation SSR with a status (`token` where a registered token plays it, `not-enforced` with a note where it does not). In this mode the editor shows, below the Situation SSRs, the generated SSRs a preview boundary would add, locked and marked with their origin.

### 7.3 The library check

Run before publish, and again at every generation, in two parts:

- **In the Domain library** (no ASL needed): parameters within their bounds; every public template uses public placeholders only; no two enabled term rules set the same card setting.
- **In a Studio and PilotHost service** that reaches the Rules project: every enabled term rule's token families are in the registry, and its cited rules are citable; no Situation base card carries a family or setting an enabled term rule owns (4.3); every fixture boundary in the library's test set generates a card that passes `ScenarioCards.Validate`.

### 7.4 The commitment review

When a Situation is about to be committed, the adjudicator opens the **commitment review** in the Studio. There is no game yet, so the review has a viewer choice of its own (adjudicator, or one side), as the Studio's perspective picker does for a game. Like that picker, it is a display choice, not enforcement: anyone at the Studio can switch it. This is labeled on the page, as the prototype's screens are labeled (core decision CD7), until the Campaign app gives each side its own seat. The review shows:

- the generated card in full, each SSR marked Situation or generated, with its term rule and library version;
- each side's private causes, shown only in that side's perspective (and both to the adjudicator);
- the card check's results; a failing card cannot be committed;
- the difference from the previous card generated for this Situation, if any.

The adjudicator may **commit** or **hold** (wait for another boundary). The adjudicator may also **suppress** a term rule's output, with a reason: the suppression is a Campaign event, recorded before generation and so part of the card's inputs (5.5); the card is regenerated without that term rule's card terms, under a new id; and the absence is shown in the review and in the game's card panel as "suppressed by the adjudicator". Editing a generated rule's tokens or text by hand is not allowed; a different result comes from the term rule's parameters, in the library.

### 7.5 In play

The card panel (`PlayCardPanel.razor`) shows every SSR's public text, as today, with a small "Campaign" mark on generated rules and "manufactured" on manufactured ones. The campaign context panel (`CampaignContextPanel.razor`, which today takes only the game id) gains the viewer, as `PlayCardPanel` takes it (`KnownTo`), and shows the viewing side's private causes next to the SSR numbers they explain, and nothing of the other side's.

## 8. Ownership

| Part | Owner |
|---|---|
| Term rules, their parameters and texts, the SSR library and its versions, evaluation, the library check's Domain part | `LimboDancer.Campaign.Domain` |
| Writing tokens through the registry; assembling, validating, and storing the generated card; the library check's Rules part | `LimboDancer.Campaign.PilotHost`, and a Studio service for the SSR library page |
| `ScenarioCardRule.Origin`; the Campaign card folder (core, section 8.5) | Play |
| The base-card mode, the card check's use of the registry, the token picker | MapStudio and Play, by a wiring row |
| The token registry; each token family's grammar and effects | Rules, by claims |
| The SSR library page, the commitment review, the play marks | MapStudio |

The optional `Origin` field and the Studio pages change ASL projects, so they go through the merge gate: no recorded game may differ, since no existing card gains a field.

## 9. Memory and performance

- **Work added.** Evaluation runs once per `Before` boundary (a handful of term rules); card assembly and its check once per generation; a preview once per parameter change in the Studio. Nothing is added to any Play step.
- **Ownership, keys, lifetime.** Library versions by hash; generated cards by id and SHA-256, stored for the life of the campaign; nothing cached beyond the stored cards.
- **Disclosure.** Public templates are checked for private placeholders; causes travel only in the sides' private parts.
- **Counters and fragments.** Dummies use existing "?" counters.
- **Checks.** Term rule unit tests over their parameter bounds; library check tests; a card round trip; existing cards' SHA-256 unchanged; the merge gate for the ASL tasks.

## 10. Tasks, in order

| Task | Work | Depends on |
|---|---|---|
| DS1 | The token registry's list and formatting, by the `ssr-tokens` claim | The claim board |
| DS2 | `ScenarioCardRule.Origin` (optional); existing cards' SHA-256 unchanged | None |
| DS3 | Term rules, the SSR library record, evaluation, numbering, templates, the library check's Domain part | Core CB1 to CB5 |
| DS4 | Writing tokens through the registry; generated card assembly, validation, id, storage in the Campaign card folder; the library check's Rules part | DS1, DS2, DS3; core CB6, CB7 |
| DS5 | The SSR library page with preview and publish | DS3, DS4 |
| DS6 | The card editor's Situation base mode | DS4 |
| DS7 | The commitment review, with suppression as a Campaign event | DS4 |
| DS8 | The marks in play, the private causes in the campaign panel (with its viewer), the Hidden OB display of the counts | DS2, DS4 |
| DS9 | The registry's use: unknown prefixes refused on the card path, registered tokens offered in the editor (backlog section 42), by a wiring row | DS1 |

Then each subject's term rules: the Supply glue design's S4 and S6, the intelligence glue design's IG3 and IG7.

## 11. Acceptance

- The same Campaign terms, library version, and suppression set always generate the same card.
- Under the Hidden OB option, the Attacker's card panel shows no HIP or Dummy count.
- A generated card passes the card checks, or is not committed.
- No public SSR text names a private cause; the library check refuses a template that would.
- Every generated rule's tokens are of registered families, and its origin is shown.
- A game is played on the card it started from; regeneration makes a new card.
- Existing cards keep their text and SHA-256.

## 12. Decisions made in this design

| Id | Question | Decision | Basis |
|---|---|---|---|
| DS-D1 | How authors express a dynamic SSR | Parameters and texts of C# term rules; no scripting language | Logic stays tested code; the Rules boundary stays clear; authors still control thresholds, tables, and words |
| DS-D2 | Where provenance lives | An optional public `Origin` on `ScenarioCardRule`; causes in `CampaignTerms`' private parts | The card is public; the optional field keeps existing cards' identity |
| DS-D3 | A new status for generated rules | No: origin is separate from status | Status says how the game enforces a rule; origin says where it came from |
| DS-D4 | Hand edits to a generated rule | Not allowed; the adjudicator may suppress a term rule's output with a recorded reason | A hand-edited token would bypass the term rule and its tests |
| DS-D5 | Where the workflow lives | The Studio, for both the library and the review | It holds the card editor, the card checks, and the registry's readers; the Campaign app links to it |
| DS-D6 | One token registry | Required for generated cards, by a Rules claim | Backlog section 42 asks for it; unknown prefixes pass silently today |
| DS-D7 | Generated card identity | A new id per Situation and boundary; immutable once a game starts | The planner binds a game to a card's id and SHA |
| DS-D8 | Situation SSRs | Authored once per Situation in the card editor's base mode, from the prototype's printed rules | They are the free-standing SSRs the ASL side already has |
| DS-D9 | Who writes token strings | PilotHost, through the registry; term rules emit structured terms | The Domain library may not hold ASL grammar (a reviewer's finding) |
| DS-D10 | Suppression and identity | The suppression set is an input of the card; suppressing a term rule drops all its card terms | A suppression must not reuse an id with a new SHA |
| DS-D11 | A base card and a term rule both setting one thing | Refused; the printed value becomes the term rule's parameter | No order should decide a conflict |
| DS-D12 | The registry claim | A rule claim for the list (`ssr-tokens`, no inventory rows) and a wiring row for its use | The refusal and the picker are Play and MapStudio work |

## Related documents

- [Campaign Core and Situation Boundary Design](<Campaign Core and Situation Boundary Design.md>)
- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>)
- [Campaign Intelligence to ASL Glue Design](<Campaign Intelligence to ASL Glue Design.md>)
- [ASL Card Editor Redesign Design](<ASL Card Editor Redesign Design.md>)
