# Campaign Intelligence to ASL Glue Design

Date: 1 November 2026

Status: proposed design. Nothing is built. It rests on rulings I1 to I12 of the [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>), approved by the user on 2026-11-01, and on the [Campaign Intelligence Design](<Campaign Intelligence Design.md>), which produces the pictures, briefings, and communications states this design reads. The decisions it makes on its own (section 12) were researched by sub-agents and are open to the user's veto. Implementation waits for the user's approval.

## 1. Purpose and scope

The glue connects the Campaign's intelligence to an ASL scenario and back, in the pattern of the [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>):

- **Out:** when a Situation is committed, the two sides' pictures, the ground truth, and the communications states become terms of the Scenario Card (section 4) and a private briefing per side (section 5).
- **Back:** when the ASL game ends, what each side's view held becomes `KnowledgeReportFacts` for the Campaign's intake (section 6).

Out of scope: every ASL rule's effect (decided in the Rules project, by the claims of section 8); the Campaign's own rules (the Campaign Intelligence Design).

## 2. Principles

1. **Printed rules only** (ruling I4), with one labeled exception, the optional orders-group SSR (ruling I11).
2. **The Campaign decides whether and how many; Rules decides what follows.** A term's count is a Campaign rule. Its token's format, parsing, validation, and effects are Rules', by a rule claim.
3. **Boundary only.** Intelligence crosses between the games at commitment and at the result, never during play (the Supply glue design, principle 3).
4. **Every game is a card** (the user's ruling of 2026-09-29). The terms ride on the Scenario Card the Supply glue design's task S3 makes the handoff build.
5. **The terms are computed in C#.** PilotHost computes them from the Campaign Domain library's records; they enter the handoff hash.

## 3. The Intelligence Terms record

One `IntelligenceTerms` record per committed Situation, computed at the `Before` boundary:

- `Version`, `CampaignId`, `SituationId`, the boundary event it was computed from, and its hash;
- the card's Scenario Defender and Attacker;
- for each side: its grain over the Situation's area (the Campaign Intelligence Design, section 7.1), whether its formations hold their ground (prepared or hasty), whether it patrolled the area within one boundary, its time to brief, and its communications state;
- the card terms (section 4), each with the inputs that produced it;
- the two briefings (section 5), each visible to its side alone.

It travels with the handoff beside `CampaignContext` and enters the handoff hash. The handoff version moves to a new version, read alongside the old; a handoff without terms keeps its present hash (task IG2). It is a field of its own beside `CampaignContext`, not inside it: `CampaignContext` is an opaque string the prototype builds, and the terms are computed in C#.

## 4. Card terms

### 4.1 Hidden setup: HIP and Dummies (A12.3, A12.11)

The main lever. The Scenario Defender's HIP allowance (the `hip:<side>:<n>` token, built, ruling R23.5, read by `ScenarioA1Definitions.HipAllowance` as squad-equivalents) and the Dummies of each of its groups (`ScenarioCardGroup.Dummies`, built) are set from the **Attacker's grain** over the Situation's area and whether the **Defender is prepared** (its formations held their ground for at least one boundary) or hasty.

S is the Defender's on-board squad-equivalents (a half-squad counts one half, A5.5). Every result is a whole number of squad-equivalents; "at least 1" applies only when S is 1 or more. FRU and FRD below are the rulebook's "fractions rounded up" and "fractions rounded down". By day:

| Attacker's grain | Prepared: HIP | Prepared: Dummies | Hasty: HIP | Hasty: Dummies |
|---|---|---|---|---|
| None | 25% of S, rounded up; at least 1, at most 6 | S | 15%, rounded up; at least 1, at most 4 | S/2, rounded up |
| Presence only | 15%, rounded up; at least 1, at most 4 | S/2, rounded up | 10%, rounded up; at most 3 | S/3, rounded up |
| Type and strength | 10%, rounded up; at most 3 | S/3, rounded up | 10%, rounded down; at most 2 | S/4, rounded down |
| Identity, recent | 10%, rounded down; at most 2 | S/5, rounded down | 0 | 0 |

Why these values:

- 25% and Dummies equal to the squad-equivalents of the OB are E1.2's night grant for a Scenario Defender (page 223): daylight never hides more than darkness. The table counts on-board S only, so reinforcements entering later bring no Dummies by day.
- 10% is H1.6's daytime DYO cap on HIP squads (note 7, page 334), the normal printed level.
- Rounding up follows the rulebook's own HIP rounding (E1.2, G1.631, H1.6); rounding down appears only where good intelligence should strip the grant from a small OB.
- The cap of 6 keeps a large OB from becoming a blind hunt. Dummies never exceed S by day; the built Tractor Works card (from the 1986 printed card) runs at about 1.5 per squad, so S is a safe ceiling.

Rules the terms respect, as the code reads them today (`ScenarioA1SetupCalculator`, rulings R23.5, R26.5, R70.22):

- **One token, the Campaign's own grant.** `HipAllowance` takes the largest n among a side's `hip:` tokens; it does not add them. The card therefore carries exactly one `hip:` token for the Defender, the Campaign's grant. Whether a grant by SSR adds to a grant by rule (E1.2 at night, G1.631 for the Japanese) is a question for those rules' claims (E1.2: not built, Pass 90), not for the Campaign: the Campaign never computes E1.2's 25% itself.
- **What the grant counts.** A squad or a vehicle takes one squad-equivalent; a half-squad or a crew takes one half (ruling R70.22); the SMC set up with a hidden MMC hide with it (R23.5). Emplaced Guns use HIP by A12.34 (ruling R26.5), manned and with their crew, and do not count against the grant. Hidden Fortifications (A12.33) are refused by the code today; they gain nothing from this grant until a claim builds them (section 9).
- **Dummies by group.** The side's Dummies are spread over its groups in proportion to each group's squads, rounded down, the remainder to the largest groups. Every "?" must be placed where A12.12 allows (red-listed terrain), which needs setup areas, not fixed positions (4.1, "Who sets up where").
- **Concealment Terrain.** HIP is only in Concealment Terrain (A12.3, page 80). A grant larger than the Defender's setup areas can hold is legal but empty; a Rules diagnostic that warns of it is proposed to the A12.3 claim (section 8).
- **Night.** Until E1.2 is built, a night Situation gets the same table as by day. When E1.2 is built, its claim decides how the Campaign's token combines with E1.2's grant; the Campaign's proposal to that claim is "the larger of the two, plus up to 10% (FRU, at most 3) when the Attacker's grain is None".
- **Other nationalities.** G1.631 (Japanese) and the grant to the Chinese People's Volunteer Army (CPVA, W7) are outside the European theater the Campaign covers; their combination with this grant is G1.631's claim's.
- **Balance.** Where a Situation's card is authored with a printed HIP level P, the "Presence only, prepared" cell is P, and every other cell is P times the ratio of its percentage to 15%, rounded as the cell rounds, within the cell's bounds. Dummies scale the same way from a printed Dummy count.

**What the numbers disclose.** HIP and Dummy counts are public on a printed card, and stay public here. A Defender that knows S, whether it is prepared, and this table can infer roughly how well it was watched: one or two cells of the table. This is accepted. A real defender knew much of it too (the patrols it met, the aircraft overhead), and the inference names no report, source, or area. Under the Hidden OB option (4.7), where S itself is hidden from the Attacker, the HIP and Dummy counts are shown only to the Defender.

**Who sets up where.** Today `SituationHandoff.Prepare` places every unit at its accepted deployment position with `Concealed` and `Hidden` set false (`SituationHandoff.cs:69`). Fixed positions leave a Dummy nowhere to go and leave the Attacker no setup in which to use a Recon dr (4.2). So when a handoff carries `IntelligenceTerms`, the card gives each group a setup area instead of fixed positions: the ASL hexes of the formation hex its parent formation occupies in the accepted deployment (the Military Command design, section 14.3: a parent hex is a coarse location, not an instruction to place every squad at its center). Setup then runs as on any printed card, in the Studio, by setup plans (`ScenarioSetupPlans`, which already carry `Hidden`, `Concealed`, and `Dummy` per placement): the Defender sets up first, choosing within the allowance which units are hidden or concealed and where its Dummies go; then any Recon dr; then the Attacker. A handoff without terms keeps today's fixed positions (task IG4).

### 4.2 Recon (E1.23)

The Attacker gets one Recon dr (page 223) when it tasked a patrol or a reconnaissance unit against the Situation's area at this boundary or the one before, unless its grain is already "Identity, recent". E1.23 is triggered by SSR or DYO purchase and its text does not limit it to night; H1.6 sells it without restriction.

The patrol is counted twice and no more: once in the Campaign, where its reports raise the Attacker's grain and so lower the Defender's HIP (4.1), and once in ASL, where the Recon dr reveals chosen hexes after the Defender has set up. The two are different goods (how much the Defender may hide; where some of it is), as a real patrol gave both a general picture and particular positions. Recon does not also shift the HIP row.

E1.23's dr comes "after the Scenario Defender has set up, but prior to his own setup", so the Attacker must have a setup to make: the card's setup areas (4.1, "Who sets up where").

Proposed token: `recon:<side>`. Its format and effects belong to the E1.23 rule claim (not built, Pass 90).

### 4.3 Civilians (E2.4)

E2.4 (page 228) works "in every RPh" through the Wind Change DR, and its recipient is the ATTACKER of that player turn: both sides receive information in their own turns, each by its own country (a DR of 3 in a hostile or neutral country, 4 in a friendly one), and the Information Table's dr is +1 in a hostile country, -1 in a friendly one. So the SSR states each side's country, never one side's.

The SSR is on when the Situation's Campaign region has a civilian population that the Campaign Intelligence Design's profile authors as present (not evacuated). Each side's country follows from the region and the side's nationality (France and Italy are friendly to the Allies and hostile to the Germans; E2.4's example).

A penetrated resistance network is not modeled through E2.4. E2.4's "False Information" result (7) can arise only in a hostile country (a dr of 6 plus 1), so the Allies in France can never draw it; penetration stays a Campaign matter, in the civilian source's false-positive band (the Campaign Intelligence Design, section 6.2).

Proposed token: `civilians:<side>=<friendly|hostile|neutral>,<side>=<...>`, for example `civilians:allied=friendly,german=hostile`. Its format and effects belong to the E2.4 rule claim (not built, Pass 74), which also depends on the Wind Change DR (B25.65, partly built).

### 4.4 Ground already studied

- **Bore Sighting (C6.4, page 174)** is the Scenario Defender's by rule; no Campaign term is needed.
- **Pre-Registered Fire (C1.73, page 166)** for a prepared Defender's batteries waits on OBA (Passes 150 and 155).

### 4.5 Radios and field phones (ruling I9)

When the ASL catalog carries a radio and a field phone counter (none exists yet; only an example entry in `units/examples`) and the C1 rules are built (C1.2, C1.22, C1.23; Passes 150 and 155):

- each committed formation's working radios become Radio SW counters given to its leaders in the OB, best leader first, at most one per leader (C1.22: a leader must possess a radio to use it);
- a formation with `Wire` `Laid` gets Field Phones, one per leader of its prepared positions, at most its radio count plus one, with their Security Areas recorded at setup (C1.23);
- a formation under radio silence gets no Radio counters. Silence is read at commitment, after its own end rule (it ends at the start of the side's own attack): a formation committed to its own attack is not silent and gets its radios; a defending formation under silence does not.

Until then the card carries no radio counters, and the report channel uses the Campaign's communications state instead (section 6.3).

### 4.6 The orders group (ruling I11; optional, off by default)

When the Campaign enables it, the card carries a manufactured SSR for the Scenario Attacker, from the battalion row of the time to brief (the Campaign Intelligence Design, section 9.2):

- **Ample:** no SSR.
- **Short:** briefed at setup are the senior leader, every leader set up in or adjacent to his Location, every leader possessing a radio, and every unit set up in a briefed leader's Location.
- **None:** briefed are the senior leader and the units set up in his Location.

The senior leader is the side's single best leader, ranked as E1.21 ranks him, by Random Selection if tied. An unbriefed unit may not move or advance until it has been attacked, has seen a Known enemy unit, or begins its MPh in a briefed Good Order leader's Location; a briefed leader with a radio briefs every leader with a radio at the start of each friendly MPh; radio AFVs are briefed together (E1.21's exception); briefing lasts the scenario; fire is not restricted.

Proposed token: `orders-group:<side>:<short|none>`. The SSR's text carries the label "manufactured, modeled on E1.21". The paragraphs above are this design's specification for that rule claim, not Campaign code: who is briefed, what an unbriefed unit may not do, and how briefing spreads are all decided by a Rules calculator, built on E1.21 (not built, Pass 90).

What it discloses: the token tells the Defender that the Attacker was short of time, as a printed scenario's movement SSRs tell both players. This is accepted, as for 4.1.

### 4.7 The Hidden OB option (ruling I5; off by default)

When the Campaign enables it, the handoff marks the card's play "Hidden OB". It is a presentation flag, not an SSR: the Studio's card and OB panels show each viewer the enemy OB only at that viewer's grain (a group of "about a company of infantry" rather than its counters) until units are revealed in play. ASL's rules are unchanged; A12's footnote 18 (pages 101 to 102) and H1.1's footnote 1 (page 336) are its precedents. The Studio reads the grain from the viewer's briefing.

## 5. The briefing in the Studio

The briefing (the Campaign Intelligence Design, section 9.1) travels in `IntelligenceTerms`, one per side. The Studio's campaign context panel (`CampaignContextPanel.razor`) shows a side's briefing only to that side's perspective, and never to the other side's or in a shared view. The adjudicator sees both. The card's public text (its SSRs) names no cause from either picture; the HIP allowance and Dummies are public numbers, as on any printed card.

## 6. Back: the knowledge report

### 6.1 What PilotHost reads

At the end of a game PilotHost replays its record and builds, for each side, that side's `GameView` at the last revision, then reads:

| Fact | Source in the record | Campaign meaning |
|---|---|---|
| Enemy units revealed | Enemy counters shown in the side's view | `Confirmed` reports of type and strength |
| Enemy "?" stacks | `SealedPresence` in the side's view | `Probable` reports, strength unknown; a stack that was only Dummies makes a false report, which the adjudicator's copy marks `Deceptive` |
| Enemy units captured | `InstanceCaptured`, `VehicleCaptured` | Prisoners for the next boundary's interrogation |
| Searched and empty | `UnitsSearched` hexes in which no enemy was revealed | `NoneSeen` reports |
| Good Order leaders at the end | Each leader's status in the final state, judged by `ScenarioA1Definitions.GoodOrderOf` (Rules), never by PilotHost | The report channel (6.3) |
| Radios and phones held | Each SW's possessor in the final state (once radios exist) | The report channel; capture sets `CodesCompromised` |

Each ASL hex maps to a Campaign area through the Situation's map ownership (`SituationMapOwnership`). That map covers only the tactical centers each formation hex owns; the board's margin hexes have no parent. A margin hex maps to the parent of the nearest owned center, ties going to the lower formation hex id. The ownership map is documented as "never part of game history", so it is stored with the Situation's session at handoff and its hash enters `IntelligenceTerms`; the report reads the stored copy, never a regenerated one. A unit's kind gives its type; its squad-equivalents, summed per area, give the strength band.

### 6.2 What PilotHost hands over

One `KnowledgeReportFacts` record per side, keyed by the ASL game id and its last revision, carrying the facts above in Campaign terms only (areas, types, bands, counts). The Campaign Domain library applies the intake (the Campaign Intelligence Design, section 10). No ASL type crosses into the Domain library.

### 6.3 The channel before radio counters exist

Until the catalog has radio counters (4.5), the first case of the report channel reads: "a Good Order leader of the side survives on board at the end, and the committed formation either had at least one working radio at commitment and was not under radio silence (read as in 4.5), or had `Wire` `Laid` and did not move off its position". The other two cases are unchanged. When radio counters exist, the case reads possession at the end, as ruling I10 writes it.

## 7. Disclosure

- SSRs and their numbers are public, as in ASL. An Attacker's Recon SSR tells the Defender that the Attacker reconnoitered, as any printed scenario does.
- Briefings, pictures, and the inputs of each term are known to their own side and the adjudicator alone.
- A side's knowledge report is computed from its own view, never from the other side's or the adjudicator's.
- The Hidden OB option filters only what the Studio shows; the game record and the Rules see everything, as they do today.

## 8. Ownership

| Part | Owner |
|---|---|
| The counts and switches of section 4 (which term, how many) | Campaign: computed in PilotHost from the Domain library's records |
| `IntelligenceTerms`, the handoff's new version and hash | PilotHost |
| Setup areas, and opening both sides' setups in the Studio with the allowance | PilotHost and MapStudio (task IG4) |
| `hip:` and group Dummies | Rules and Play, built (vehicle HIP by ruling R70.22); a claim on A12.3 for the Concealment Terrain warning; claims on A12.33 and E1.2 for hidden Fortifications and the night grant |
| `recon:` token and E1.23 | Rules, by the E1.23 claim (Pass 90); its setup step, track 3 |
| `civilians:` token and E2.4 | Rules, by the E2.4 claim (Pass 74) |
| `orders-group:` token and its effects | Rules, by a claim on E1.21 and the SSR (Pass 90) |
| Radio and Field Phone counters and C1.2 to C1.23 | Catalog and Rules (Passes 150, 155) |
| The Hidden OB view and the briefing panel | MapStudio |
| Reading the record into `KnowledgeReportFacts` | PilotHost, read only |

The claim order is ruling I7's: E2 first (wanted in every scenario, though it adds no card term), then E1.23, then E1.21, then E2.4, then the radio rules with OBA. This design takes no claim.

## 9. Backlog rows this design adds

When the design is approved, these join the backlog, section 95:

- hidden Fortifications (A12.33), refused by the code today, so a prepared Defender's Fortifications do not hide;
- how a Campaign `hip:` grant combines with E1.2's night grant, for the E1.2 claim;
- the Concealment Terrain capacity warning for a HIP grant;
- Pre-Registered Fire for a prepared Defender, with OBA;
- Radio and Field Phone counters from the communications state, with C1.

## 10. Example

Goodwood (the only Situation the C# Campaign hands off today). Illustrative values, not a historical claim. The British attack; the German defenders have held their villages for weeks (prepared). The British picture over the area holds type-and-strength reports from photographic air reconnaissance, so the grain is "Type and strength". With S = 12 German squad-equivalents on board, the card carries `hip:german:2` (10% of 12 rounded up) and 4 Dummies (12/3), spread over the German groups, which set up in the ASL hexes of their formations' hexes. The British patrolled no village this boundary: no Recon SSR. The region is France, with its population present: SSR `civilians:allied=friendly,german=hostile`, once E2.4 is built (tokens name the card's side ids, `allied` and `german`; the British are the `allied` side). The British time to brief at battalion is Short; if the campaign enables the orders group, the card carries `orders-group:allied:short`. At the end, a British Good Order leader survives with the committed formation's radios working: the British report reaches its headquarters at this boundary, at full grain.

## 11. Tasks, in order

| Task | Work | Depends on |
|---|---|---|
| IG1 | `IntelligenceTerms` record and its computation in PilotHost (sections 3, 4.1) | Campaign Intelligence Design K6, K9; Supply glue design S3 |
| IG2 | Handoff version, hash, and round trip; handoffs without terms keep their hash; the map ownership stored with the session | IG1 |
| IG3 | HIP and Dummies on the card, spread over groups | IG1, IG2 |
| IG4 | Setup areas from the deployment's formation hexes; both sides' setups opened in the Studio, Defender first, within the allowance | IG3 |
| IG5 | The briefing panel, per perspective | IG2 |
| IG6 | `KnowledgeReportFacts` from the record; the interim channel (6.3) | Campaign Intelligence Design K11 |
| IG7 | `recon:`, `civilians:`, `orders-group:` on the card | Their rule claims |
| IG8 | Hidden OB view | IG5 |
| IG9 | Radio and Field Phone counters | C1 and the catalog |

## 12. Decisions made in this design

Made on 2026-11-01 under the user's instruction to resolve open questions by expert sub-agents and act on the best recommendation. Each is open to the user's veto.

| Id | Question | Decision | Basis |
|---|---|---|---|
| GD1 | HIP and Dummies by grain and preparation | The table of 4.1 | An ASL scenario-design sub-agent's reading of A12.3, A12.11, E1.2, G1.631, H1.6 and the built cards |
| GD2 | Recon by day, and its weight | Allowed; the patrol counts once in the Campaign grain and once as the Recon reveal, with no shift of the HIP row | E1.23's text and H1.6 do not limit it to night; a reviewer found the row shift counted the patrol a third time |
| GD3 | Who sets up where | Setup areas from the deployment's formation hexes; the Defender's player chooses hidden units, concealment, and Dummies in a Studio setup plan; Defender first, then Recon, then Attacker | Hidden setup is the player's choice on every printed card; Dummies and E1.23 need setup areas |
| GD4 | Bore Sighting as a term | No term: C6.4 is the Scenario Defender's by rule | C6.41, page 174 |
| GD5 | The report channel before radio counters exist | The formation's radios at commitment, or laid wire and no move, stand in for possession at the end (6.3) | No radio counter exists in the catalog |
| GD6 | Civilians and a penetrated network | The E2.4 SSR states each side's country; penetration stays in the Campaign's civilian source | E2.4 informs the ATTACKER of every player turn (page 228); its False Information result is reachable only in a hostile country |
| GD7 | Hidden OB as an SSR or a flag | A presentation flag, not an SSR; HIP and Dummy counts shown to the Defender only under it | It changes what the Studio shows, not any rule; the counts would reveal S |
| GD8 | Public numbers that let the Defender infer the Attacker's grain | Accepted, as on any printed card | The inference is coarse (one or two cells) and names no report or source |
| GD9 | One `hip:` token or several | One, the Campaign's own grant; combining it with E1.2 or G1.631 is those rules' claims' | `HipAllowance` takes the largest token, and E1.2 is not built |

## 13. Memory and performance

- **Work added.** Terms are computed once per handoff; the knowledge report replays the record once and builds two views at its last revision. Nothing is added to any Play step, render, or read path; the briefing panel reads a record already in the handoff.
- **Ownership, keys, lifetime.** `IntelligenceTerms` is immutable, keyed by its boundary event and hash; the report by game id and revision.
- **Disclosure, snapshots, seams.** Section 7. The Studio shows a briefing through the perspective it already uses.
- **Counters and fragments.** Dummies are "?" counters the counter layer already draws.
- **Checks.** Campaign tests for the terms over every grain and preparation cell; a handoff round trip; ASL replay of existing handoff games unchanged. The merge gate is needed only for tasks that change an ASL project (IG4, IG5, IG7, IG8, IG9).
- **Benefit and noise.** No performance claim is made.

## 14. Acceptance

- Every committed Situation has an `IntelligenceTerms` record, and the same boundary always produces the same card.
- A Defender's HIP and Dummies follow the table of 4.1 from the Attacker's grain and the Defender's preparation, within the rulebook's limits.
- No SSR carries a cause from either side's picture; each briefing is seen only by its side and the adjudicator.
- No ASL rule is decided outside the Rules project; the one manufactured SSR is labeled so.
- An ASL result's knowledge reaches the Campaign once, by the channel rule.
- Existing handoff games replay unchanged.

## Related documents

- [Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis](<../Campaign Intelligence and ASL Rulebook Intelligence Hooks Analysis.md>)
- [Campaign Intelligence Design](<Campaign Intelligence Design.md>)
- [Campaign Supply to ASL Glue Design](<Campaign Supply to ASL Glue Design.md>)
