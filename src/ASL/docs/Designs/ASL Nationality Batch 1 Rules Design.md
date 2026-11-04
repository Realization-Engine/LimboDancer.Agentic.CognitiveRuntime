# Nationality Batch 1: Rules-only design

Status: revised design approved by the user and implemented Rules-only, 2026-11-04. Prepared for PR review on `codex/nationality-batch-1` in Codex's separate worktree. Research baseline: main `83740016`; the claim lock reached refreshed main at `25ecd2fd`. Downstream wiring and merge remain separate approvals.

Review corrections, 2026-11-04: conditional contract inputs now have explicit representations; nationality requirements are separated from full action admission; the inherited Italian treatment of Eritreans is included for deployment, capture and escape, without changing legacy callers.

## 1. Approved scope

The user approved four Rules claims, A25.61, A25.63, A25.71 and A25.75, as one coordinated implementation batch. Each retains its own inventory owner, contract, checks and wiring row. Implement the missing nationality decisions, audit the already-built clauses, and reuse their existing calculators. Do not make a second implementation of prisoner escape, ELR replacement, leader loss or captured-weapon penalty arithmetic.

The delivery includes Rules models/calculators/contracts, focused tests, four minigame bundles and the authoritative documentation updates. It changes no Play, Units, ASL.Turns, Maps, catalog, event reader/writer or live Studio action. The existing generic Rule Workbench should need no changes. Hand-entered facts let the new calculators be proved before downstream fact sources exist.

Approval means approval of this design and its Rules implementation. It does not approve downstream wiring or any merge. The proposed wiring rows in section 8 are a separate future queue, not work in this batch.

**Boundaries stated first:**

- A25.61's printed elite identities and ELR replacement and A25.71's rank structure, leader-loss implications and prohibition on Field Promotion are audited against existing Rules members. Existing correct members remain the authority; the batch is not only a deployment patch.
- A25.75 explicitly refers to A25.77. Its Finnish PSK date and class restrictions are a necessary supporting slice of this claim, included below. This does not take the rest of Finnish nationality rules.
- A25.66's inherited Italian treatment is included for deployment, the capture-attempt modifier and prisoner escape. Its separate ELR, Battle Hardening, Lax and PAATC rules are not implemented by this batch. Nationality is never globally rewritten from Eritrean to Italian.
- Missing counter definitions or weapon provenance are catalog work. The calculator accepts explicit facts and the workbench labels entered facts. A missing catalog counter is not evidence that its rule is unavailable or that the rule may assume an exemption.
- Broader deployment/recombination rules, new vehicle-crewing commands, other nations' captured-equipment exceptions and night Sniper resolution remain with their existing or future claims. This batch provides the nationality verdicts those callers need.

## 2. Evidence and research

Authority: registered eASLRB v3.01, physical PDF pages. All four primary rules and A25.77 are on p. 97. The full PDF text was searched for these rule numbers and related exceptions, including the Index and national-capabilities tables; the clauses below were read in the PDF text layer.

| Source | Relevance |
|---|---|
| A25.61, p. 97 | Italian elite identities, deployment restriction and exceptions, elite squad ELR replacement |
| A25.63, p. 97 | Capture-attempt modifier exception and prohibition on Italian prisoner escape |
| A25.66, p. 97 | Eritrean inheritance of Italian treatment, with its own replacement and other exceptions preserved |
| A25.71, p. 97 | Finnish leader ranks, no Field Promotion, optional leaderless deployment and leaderless recombination |
| A25.75 and A25.77, p. 97 | Directional MG/PSK exemptions, MG exceptions, PSK date and class restrictions |
| A1.31 and A1.32, p. 45; A2.9, p. 47 | Ordinary deployment/recombination requirements and setup deployment context |
| A19.35, p. 86; A20.22, p. 87 | Inexperienced defenders' capture modifier versus the ordinary capture modifier |
| A20.5, pp. 87-88; A21.22, p. 88 | Guard/Unarmed and Temporary Crew exceptions; distinguish automatic exchange from an ordinary RPh attempt |
| A21.1-.13, p. 88; E1.76, p. 225 | Captured status, use penalties and the night captured-MG consequence |
| A10.2, p. 65; A18.1, p. 85 | Existing leader-loss and Field Promotion rules reused by the Finnish audit |

The inventory's A25.63 note contradicts itself. The PDF removes the ordinary +1 capture DRM against non-elite Italians; it does not add +1. Correct that text when the claim is recorded. The existing A20.54 delivery already implements Italian escape refusal in `ScenarioA1PrisonerCalculator`; its old inventory omission must not create a duplicate task.

Research used `rule_sites.py` against `origin/main`, with nationality, deployment, recombination and captured-use words, followed by direct symbol searches and reading the relevant members. The initial worktree search returned no sites because Git rejected its ownership; that empty result was discarded. The successful search against the fetched main found 47 production files, including unrelated nationality uses. Those unrelated sites, such as Extreme Winter, Cowering and victory calculations, are not new scope.

## 3. What decides it today

Paths below are relative to `src/ASL/`. Rules, Play, Units and Studio abbreviate their existing `LimboDancer.Domains.Asl.*` project names. Member names are the durable anchors; line numbers move.

| Site | Role today | Treatment |
|---|---|---|
| Rules/ScenarioA1Definitions.cs: `MayDeploy` | Decides a nationality-only deployment admission, excluding Russians but not non-elite Italians | Preserve old answer; new Italian facts-based verdict is explicit |
| Rules/ScenarioA1SetupCalculator.cs: `Fill` | Decides setup substitutions using the owner's nation, not each squad's class | Keep the legacy setup path; record migration under W-A25.61 |
| Play/ScenarioSetup.cs: `MayDeploy` | Forwards to Rules | Future adapter needs squad-level facts |
| Play/GamePlanner.SupportWeapons.cs: `MayNotDeploy`, `MayOfferDeploy`, `PlanDeploy`, `PlanRecombine`, `DirectingLeader` | Gathers units and also decides nationality/leader admission, DRM and timing | No edits; future calls to the new verdicts |
| Rules/ScenarioA1SupportWeaponRules.cs: `NeedsNoLeader`, `GuardDeploysAtWill`, `LeaderDrm`, `RecombineStartBar` | Decides existing exceptions, leader DRM and timing | Reuse correct clauses; preserve old overloads lacking nationality/context |
| Rules/ScenarioA1OrdnanceProjection.cs: `DeploymentPassed`; Units/State/GameProjector.cs: deployment verification | Rules resolves supplied dice/DRM; projector validates and records | Keep arithmetic and records unchanged; wiring supplies the new required DRM |
| Rules/ScenarioA1FireReference.cs: replacement and leader tables | Decides existing Italian replacement and Finnish rank progression | Audit existing values, do not copy these tables into new calculators |
| Rules/ScenarioA1FieldPromotion.cs: `Applicable`; CloseCombat, VehicleCloseCombat and Rally calculators | Decide/consume the Finnish no-promotion rule | Reuse `Applicable`; protect existing callers with regression checks |
| Rules/ScenarioA1MoraleCheck.cs and FireCalculator: leader loss | Decide leader-loss effects from actual leadership values | Audit Finnish cases; do not impose an invented blanket nationality cap |
| Rules/ScenarioA1CloseCombatCalculator.cs: per-defender capture modifiers | Decides -1 against Inexperienced defenders, otherwise +1; lacks Italian exception | New modifier calculator; legacy attack resolution unchanged |
| Rules/ScenarioA1PrisonerCalculator.cs: `Escape`; PrisonerContract: `a20.55-prisoner-escape` | Already decides Italian escape prohibition, but the nationality check matches only `italian` | Keep the original calculator entry point unchanged. The explicit current-reading overload also applies A25.66; the single existing Workbench contract calls it and accepts Eritrean nationality |
| Rules/ScenarioA1FireEligibility.cs: `CapturedBy` | Decides from nationality inequality; returns null for both same-nationality and missing facts | Preserve legacy semantics; new exemption API has an explicit unknown result |
| Play/LiveFire.cs: `CapturedBy`, fire/DC fact construction; GamePlanner.FireExtensions.cs; Studio/Pages/Play.razor: `FireLanesOffered` | Gathers/consumes captured-use facts, including Fire Lane offering | Record future shared fact-reader migration; no changes now |
| Rules/ScenarioA1OrdnanceCalculator.cs: `Breakdown`; ArmorCalculator: `DepletionShortage`; FireCalculator and NonQualifiedUseCalculator | Consume captured classification and calculate effects | Retain existing arithmetic and legacy entry points; future callers provide the exemption verdict |
| Rules/ScenarioA1SupportWeaponRepairRules.cs: `CapturedForRepair`; Play/GamePlanner.Rally.cs; Units/State/GameProjector.cs | Separate repair classification and checks | Do not equate a use-penalty exemption with permission to repair captured equipment |
| Studio/Components/Play/RecombineActionPanel.razor and Play.razor organization actions | Present candidates and leader selection; text assumes a required leader | Separate future UI wiring |
| Units/State lineage events, reader/writer; Studio PlayRecords/ReplaySteps/ReplayDiff | Record or display accepted outcomes | Unchanged; these are not nationality calculators |

Before implementation, refresh the symbol map at the claim baseline, enumerate every caller of the listed legacy members, and extend the held-file list before touching an additional Rules file. No ASL.Turns-specific nationality decision was found in the search; its existing organization hints still need checking when wiring starts.

## 4. Proposed rule decisions

### 4.1 A25.61: Italian elite and deployment

**RN1.1.** Only the printed Italian elite MMC categories qualify as elite under this section: the 4-4-7 squad, its 2-4-7 HS and the 2-2-7 crew. An ordinary deployment candidate must be a squad. Supplied counter-class facts distinguish these from other Italian units; a matching strength string alone must not classify a counter of another nationality. Existing ELR replacement of a 4-4-7 by a 3-4-6 is checked through the existing replacement table.

**RN1.2.** An ordinary non-elite Italian squad fails the nationality admission for deployment. An elite squad passes that admission, not every other deployment condition. Setup uses the same nationality/class restriction but retains A2.9's separate allowance and does not demand an RPh TC.

**RN1.3.** A20.5 and A21.22 are explicit contexts, not an unrestricted `ignoreNationality` flag. Reuse the existing Guard automatic-deployment verdict and Unarmed exception. For a capturing squad's Temporary Crew exchange, expose that the Italian restriction does not bar the exchange; the existing crew calculator must still validate the vehicle/manning action. This batch does not implement that missing command or certify the rest of A21.22 as complete.

**RN1.4.** Missing relevant class or context facts return not-evaluated. A25.66 applies the Italian deployment restriction to Eritreans too. A single narrowly named Rules helper, `ScenarioA1ItalianNationalityRules.UsesItalianDeploymentAndCapture`, identifies this inherited treatment from the actual nationality. Both Italian calculators and the current-reading prisoner overload use it. It does not classify Eritrean counter strength, ELR progression, Lax status or PAATC as Italian. Other known nationalities return not-applicable from the deployment restriction, not unconditional permission to Deploy.

### 4.2 A25.63: capture and escape

**RN1.5.** The capture-attempt modifier is decided separately for each defender. Preserve A19.35's -1 for Inexperienced Personnel; otherwise return 0 for a non-elite Italian or Eritrean and +1 for an ordinary defender. These are alternative capture terms, not cumulative nationality and inexperience bonuses. Preserve independent modifiers, including Passenger capture, concealment and leadership.

**RN1.6.** A mixed defending group must not inherit the most favorable member's modifier. Unknown class or inexperience returns not-evaluated only when it can change that defender's answer. The ordinary capture term is not a full CC attack permission or resolution.

**RN1.7.** Every Italian prisoner remains barred from escape, elite included; A25.66 extends that prohibition to Eritrean prisoners. `ScenarioA1PrisonerCalculator` remains the sole escape calculator. Its explicit current-reading context overload applies the shared inheritance helper in its nationality check. The original calculator overload retains its existing answers. Following PR review, the single `a20.55-prisoner-escape` Workbench contract calls the current overload; it has no recorded-game caller. The bundle checks Italian and Eritrean refusal through that contract; calculator tests preserve the legacy boundary. Do not duplicate escape checks in `ItalianCapture`.

The existing escape contract retains its facts and All disclosure, adds `eritrean` to its nationality choices, and cites A25.66. Its explicit context is a separate Rules value, not a new property on an existing recorded fact. The shared helper is used only for the three named decisions; the actual nationality remains available to every other rule.

### 4.3 A25.71: Finnish organization and leadership

**RN1.8.** A Finnish squad may choose ordinary leader-directed deployment with its leader-modified NTC, or leaderless deployment with a 1TC. Presence of a leader does not force selection of the leader-directed route. Explicit mode is part of the facts. This calculator returns the leader requirement and base TC DRM, not a leader's eligibility or the final modified roll. Future wiring must validate any nominated leader and must not silently change an invalid directed attempt into a leaderless one.

**RN1.9.** Leaderless deployment returns a +1 TC requirement and consumes no leader's RPh action. It does not waive Good Order, phase, one-attempt, location or other A1.31 conditions. A Guard's automatic deployment remains the stronger existing exception and requires no roll. Keep the existing Unarmed/Carrier exceptions distinct rather than adding +1 to every leaderless attempt.

**RN1.10.** Finnish HS do not require a leader to Recombine. A1.32's other requirements remain, including compatible HS, Good Order, the same Location and terrain, and the start-of-RPh boundary. The nationality calculator supplies a leader requirement, not a substitute for the whole recombination calculator.

**RN1.11.** Reuse the existing Finnish rank order and `ScenarioA1FieldPromotion.Applicable`. Audit leader-loss outcomes from the printed leadership values. Do not add a nationality-only LLMC cap or rebuild rank/ELR tables. Any discovered wrong answer is fixed behind a new explicit Rules entry point if changing it in place would alter legacy callers.

### 4.4 A25.75: equipment-use exemptions

**RN1.12.** The new verdict distinguishes physical capture/provenance, exemption from specified Captured Use consequences, and permission to use the weapon. An exemption is not a declaration that the weapon was never captured, is qualified for every user, or can be repaired. Do not overwrite the general captured flag everywhere.

**RN1.13.** Finnish Personnel using Russian MG are exempt except Russian LMG in 1939 and .50-cal HMG. Russian Personnel using Finnish-color MG require the printed `(r)` origin marking for the reciprocal exemption. Do not infer that marking from color, name substrings or present ownership. Date matters only on a date-dependent branch.

**RN1.14.** Germans using Finnish-color PSK and Finns using German PSK receive the stated reciprocal exemption, subject to A25.77. Finnish Personnel may use PSK only in July 1944 through 1945; Finnish 2nd-Line, Green and Conscript Personnel use all PSK with Captured Use penalties, including Finnish-color weapons. Use printed class, not the contextual Green-unit Inexperience calculation. Apply the restriction to Finnish Personnel, not to Germans merely because their PSK is Finnish-color.

**RN1.15.** Return separate applicability to A21.11-.12 and E1.76. The MG exemption includes the captured-MG night consequence expressly referenced by A25.75. The existing non-qualified-use and penalty arithmetic remain authoritative. Other causes of penalties are not erased by an exemption. Complete night Sniper attack generation and unrelated nationalities' exceptions are not part of this calculator.

**RN1.16.** For combinations outside these nationality rules, return not-applicable, allowing the existing general rules to decide. Missing weapon origin, kind, class or date returns not-evaluated when material. Known unrelated combinations do not require irrelevant dates or markings. No caller can treat not-evaluated as exemption or permission.

## 5. Calculator and contract design

Proposed names are a design contract, subject to normal naming corrections during implementation. Each new family has `Models.cs`, `Calculator.cs` and `Contract.cs` under Rules.

| Claim | Proposed family and entry points | Facts and result | Contract |
|---|---|---|---|
| A25.61 | `ScenarioA1ItalianDeployment`: `NationalityRefusal`, `Evaluate` | Subject nationality, kind/class, deployment context and existing exception verdicts; applicability, nationality refusal or admission | `a25.61-italian-deployment` |
| A25.63 | `ScenarioA1ItalianCapture`: `CaptureAttemptModifier` | Per-defender nationality, elite class and Inexperienced status; known -1/0/+1 capture term or not-evaluated | `a25.63-capture-modifier`; existing `a20.55-prisoner-escape` calls the prisoner calculator's explicit context overload |
| A25.71 | `ScenarioA1FinnishOrganization`: `DeploymentRequirement`, `RecombineLeaderRequirement` | Nationality, unit kind, action, explicit leader mode, relevant exception facts; leader requirement, automatic/NTC/1TC, base TC DRM and leader-use consequence | `a25.71-organization` |
| A25.75 | `ScenarioA1CapturedEquipmentUse`: `Evaluate`, `UseRefusal` | Explicit Personnel flag, nationality/class, weapon kind/color nationality, printed origin marker, MG subtype/caliber and scenario date; applicability, nationality-specific use refusal, A21.11-.12 exemption/forced penalty and E1.76 exemption | `a25.75-equipment-use` |

Facts are typed immutable values, not Units or Maps objects. No catalog lookup, state access, clock read or random draw occurs in a calculator. Scenario date is an input, never the computer's date. Verdicts distinguish not-applicable, not-evaluated, refused and evaluated; refusal text cites the relevant rule and physical page. Contracts preview requirements and effects only, never write events.

### 5.1 Required facts, conditional facts and validation

The current contract registry checks all required fields before calling `Decide`. The design therefore does not rely on the calculator being able to ignore an absent required JSON field. Every bundle supplies each required field; a missing required field or invalid JSON type returns not-evaluated at the registry boundary.

| Input | Contract representation | Calculator treatment |
|---|---|---|
| `scenarioDate` | Required All Text: `YYYY-MM` or the explicit value `not-applicable` | Parse and validate real year/month values. `not-applicable` carries no date and is accepted only on a date-independent branch. On a Russian LMG/Finnish user or Finnish PSK date-dependent branch it returns not-evaluated. Do not set `MayBeUnknown` on this public fact or invent a date |
| Organization/equipment subject nationality, unit kind, `personnel` and class/elite | Owner Choice/Flag facts concerning the subject; nullable only where the opposing viewer may lack them | Require only facts that can change the current answer. Hidden irrelevant fields may be null; hidden material fields return not-evaluated. `personnel=false` gives no Personnel exemption and is not a general prohibition on vehicle fire |
| Capture defender nationality, elite and Inexperienced status | All at CC attack declaration; unavailable facts remain nullable | Use only publicly available printed combat facts at that step, matching the existing CCV contract; no hidden roster lookup |
| Weapon kind, color nationality, MG subtype/caliber and `(r)` marking | Owner facts concerning the firer; `originMark` is a Choice with `r`, `none`, `not-applicable`; private fields may be null when hidden | `none` is a known absence of the printed mark, not missing evidence. `not-applicable` is valid only when origin cannot affect the branch. A relevant null or not-applicable value yields not-evaluated. A missing future catalog attribute is entered explicitly for the workbench, never inferred |
| Organization action/mode | Required public Choice: action `deploy`/`recombine`; mode `directed`/`leaderless`/`not-applicable` | Require an explicit deployment mode when a roll route is being selected. Recombination uses `not-applicable`. Contradictory combinations return not-evaluated, never silently select a different route |
| Guard, Unarmed and broken exception facts | Owner facts concerning the unit, nullable only for disclosure | Call the existing Rules exception helpers from these plain facts. A label such as `guard` or `temporary-crew` identifies a context; it is not proof that all requirements for that context are satisfied. Carrier status is not an input to the Finnish requirements, because it cannot change them |

For a date-independent case, the typed calculator may receive no date; its JSON contract must receive the explicit `not-applicable` value. Tests distinguish this valid representation from an omitted required JSON field. For private conditional fields, the contract's `MayBeUnknown` allowance exists because of disclosure, and the calculator still decides whether the null matters. No generic registry or Workbench change is proposed.

### 5.2 Requirements are not full action admission

`FinnishOrganization` validates its subject kind, nationality and selected mode, then returns requirements. It does not take a caller-computed `leaderEligible` or `recombineAllowed` flag. Directed deployment returns a base NTC modifier of 0 and requires eligible leader direction; leaderless Finnish deployment returns +1 and no leader use. The existing `LeaderDrm` computes any nominated leader's modifier separately, and `DeploymentPassed` resolves the supplied final DRM and dice. This batch can test their Rules-only arithmetic composition without validating a live nominated leader.

`RecombineLeaderRequirement` returns whether a leader is needed. It does not certify compatible HS, terrain, phase, prior activity or Good Order. Its tests assert only that requirement. Where an existing Rules helper owns a prerequisite, test that helper separately or compose it explicitly; prerequisite decisions still in Play are recorded in W-A25.71, not rewritten here or treated as already verified by a nationality test. `UseRefusal` similarly reports A25.77 restrictions only, not full fire eligibility.

### 5.3 Disclosure, sequence and compatibility

**Disclosure:** organization subject IDs are Owner facts concerning themselves; their class, condition, possessions and nationality are Owner facts concerning that selected unit. Equipment-use contracts anchor private weapon facts to the selected owning firer, with weapon identity restricted to that viewer. Public scenario date is All. The capture contract's defender and printed nationality/class/Inexperienced facts are All at CC attack declaration, matching `ScenarioA1CloseCombatValueContract`. A11.19 (p. 73) requires Strength Factor revelation before declaration; this does not authorize exposing hidden roster information or treating every concealed counter as unconcealed. Supply only facts publicly available at that step. Unavailable material facts remain null and yield not-evaluated. The existing prisoner contract retains its All disclosure. Adjudicator checks may supply all facts.

**Places and previews:** organization contracts identify setup and RPh contexts, and the exceptional Guard/Temporary Crew contexts as applicable. Capture uses CCPh; equipment-use applies wherever the weapon is used, including night fire. Before publishing contracts, use `asop_steps.py` to identify exact sequence steps. Do not guess a step number or claim an entire mixed-rule ASOP row. Previews name deployment roll/lineage, recombination lineage, per-defender capture term and weapon-use modifiers. No new serialized event type is required by this Rules design.

**Compatibility:** retain `MayDeploy(string?)`, `NeedsNoLeader(...)`, `CapturedBy(string?, string?)`, the existing setup path and the old CC resolving overload. Document the new replacement beside each old member. Equal-answer delegation is allowed only after boundary checks prove equivalence, including null behavior and refusal order. New current-reading context overloads may compose the new calculators, but no existing caller opts into them in this batch. No existing recorded fact shape or event serializer changes.

## 6. Proposed file holds and delivery order

The following table records the proposed holds considered at approval. The inventory, not this table, records the actual locks. The claim-only commit reserved the new families and only the existing edges actually changed, after `claim_check.py --touched` and the A20.54 review released its prisoner-calculator hold. No ASOP row cites these nationality sections alone; the mixed steps remain with track 3.

| Claim | New Rules family held whole | Existing Rules edges to reserve if changed |
|---|---|---|
| A25.61 | `ScenarioA1ItalianDeployment{Models,Calculator,Contract}.cs`, `ScenarioA1ItalianNationalityRules.cs` | `ScenarioA1Definitions.cs (shared)`, `ScenarioA1SetupCalculator.cs (shared)`, `ScenarioA1FireReference.cs (shared)` |
| A25.63 | `ScenarioA1ItalianCapture{Models,Calculator,Contract}.cs`, `ScenarioA1PrisonerCalculator.cs`, `ScenarioA1PrisonerContract.cs` | `ScenarioA1CloseCombatCalculator.cs (shared)`; the existing escape contract is extended, with its file reserved on main before editing |
| A25.71 | `ScenarioA1FinnishOrganization{Models,Calculator,Contract}.cs` | `ScenarioA1SupportWeaponRules.cs (shared)`, `ScenarioA1FieldPromotion.cs (shared)`, `ScenarioA1FireReference.cs (shared)` |
| A25.75 | `ScenarioA1CapturedEquipmentUse{Models,Calculator,Contract}.cs` | `ScenarioA1FireEligibility.cs (shared)`, `ScenarioA1OrdnanceCalculator.cs (shared)`, `ScenarioA1ArmorCalculator.cs (shared)`, `ScenarioA1FireCalculator.cs (shared)` |

Shared holds cover delegation, explicit new context overloads or legacy-member documentation only. No nationality decision is newly embedded in a large shared calculator. An audit correction requiring another file gets its hold first. The new inheritance helper is owned once by A25.61 and read by A25.63 on the same batch branch. Do not list that file as held by both claims. A25.77's supporting row belongs with A25.75's claim. A25.66's row remains partial: its claim owner is A25.61 for the shared inheritance slice, with the A25.63 application linked in its notes, not a second owner. Do not mark surrounding A25.7 rows or all of A25.66 complete. A1.31/A1.32 remain partial outside this nationality slice.

Implement A25.61, A25.71, A25.63, then A25.75. The first pair shares organization concepts; the second pair exercises distinct capture/use concepts and must not conflate them. A single batch branch can hold all four claims, with separate commits and evidence per claim, following the existing multi-claim batch precedent. All work starts from current main; no unmerged implementation branch is a base.

## 7. Verification and acceptance

Create four bundles: `a25-italian-deployment`, `a25-italian-capture`, `a25-finnish-organization`, `a25-finnish-equipment`. Use an already imported board such as `bd01`, a small playable band and the minimum existing Italian/Finnish squads, HS, leaders and opposing units. Select actual legal hexes during bundle authoring. Missing weapon variants are explicit hand-entered contract facts, not counterfeit catalog definitions. Bundle descriptions distinguish the visible position from entered weapon/date variants.

Inspect each bundle through the generic Rule Workbench in Codex Studio, port 6680, with Owner, Opponent and Adjudicator checks. Then write/run focused test methods covering the following matrices. Parameterized rows count as one concern, not a reason to run a whole test class.

| Concern | Required boundaries |
|---|---|
| Italian deployment | Elite/non-elite; squad versus HS/crew; setup versus ordinary attempt; Guard, Unarmed and Temporary Crew contexts; unknown material class; Eritrean inheritance and unrelated-nationality control; passing a nationality restriction is not full action admission |
| Existing Italian clauses | Printed elite classifications; 4-4-7 ELR replacement uses the existing table |
| Capture modifier | Italian elite +1, non-elite experienced 0, Inexperienced -1; corresponding Eritrean cases and unrelated-nationality controls; separate results in a mixed group; independent Passenger modifier preserved in explicit Rules composition |
| Escape compatibility | One registered escape contract; Italian and Eritrean prisoners refused by it; original calculator overload preserves its prior answers; Japanese/Axis Minor branches unchanged; one escape calculator |
| Finnish deployment requirements | Directed base NTC DRM 0 versus chosen leaderless +1, explicit mode unchanged even if a leader is available, subject-kind and mode validation, automatic Guard exception; separate arithmetic composition with `LeaderDrm` and `DeploymentPassed` at equal-to-target and one-over boundaries |
| Finnish recombination requirements | Finnish HS require no leader; unrelated nationality returns not-applicable to this Finnish rule, leaving general requirements intact; no allowed-action verdict is returned for compatibility/timing/terrain |
| Existing Finnish leadership | Rank order, no Field Promotion in Rally/Infantry CC/vehicle CC, leader-loss calculations from actual leadership values |
| MG exemptions | Russian LMG in 1939 versus 1940; .50-cal HMG versus other MG; reciprocal Finnish `(r)` marker present/absent; non-Personnel and unrelated users |
| PSK exemptions | June/July 1944, December 1945/January 1946; Elite/1st-Line versus 2nd-Line/Green/Conscript; German using Finnish-color and Finnish using German/Finnish-color PSK |
| Meaning and disclosure | Use exemption does not clear physical capture or grant repair; night exemption explicit; unknown material facts cannot grant use/exemption; irrelevant unknown facts do not block a known answer; opponent view does not leak class or weapon origin |
| Contract validation | Required date missing versus explicit `not-applicable`; date-independent branch accepts that value, date-dependent branch rejects it; invalid months rejected; hidden irrelevant private facts accepted, hidden material facts not evaluated; non-Personnel receives no Personnel exemption |
| Legacy compatibility | Original members retain answers, null semantics and refusal order; old CC resolution and existing records remain on their original reading |

Acceptance requires calculator verdicts and contract checks to agree; bundle/contract registry checks; read-only review by the other agent (or an independent sub-agent if only one agent is working); focused unit checks and green CI. No current claim is called wired or phase-accepted. Rules-only replay/sweep differences must be zero at the standing batch gate. Count four rule claims, not one branch; run a gate when the cumulative total since the last proved baseline reaches five, even if that occurs during this batch.

Future wiring tests, not acceptance evidence for this batch: an invalid nominated leader is refused without changing mode; a unit failing general deployment/recombination eligibility remains refused despite satisfying its nationality requirement; command and candidate-list results agree. Existing Rules prerequisite tests may still run when directly affected, but no live planner behavior is claimed as fixed here.

## 8. Downstream wiring, recorded separately and not authorized

When each claim reaches built, add its corresponding open row to inventory section 7. The following is the proposed row content. Future wiring claims hold their own files, choose replay-version boundaries, run their own gates and seek their own merge approval.

| Future row | Caller migration | Behavior change and legacy treatment |
|---|---|---|
| W-A25.61, setup and work | `ScenarioSetup.MayDeploy`, Rules setup `Fill` adapter, `GamePlanner.SupportWeapons` admission and Studio candidate lists gather each squad's actual nationality/class/context and ask `ItalianDeployment.Evaluate` | Non-elite Italian and Eritrean ordinary deployment rejected, exceptions retained. Do not change an old saved setup or recorded lineage merely by replaying it. Replace the nationality-only helper only after every caller is accounted for |
| W-A25.71, rally | Deployment/recombination planner, organization panels and projector verification use `FinnishOrganization` requirements; separately validate the nominated leader and general A1.31/A1.32 prerequisites, compose `LeaderDrm` with base TC DRM, then ask `DeploymentPassed` | Selectable leaderless 1TC and leaderless recombination. Invalid nominated leaders never trigger automatic mode switching. Nationality requirements never replace full admission. Persist explicit mode/reading evidence as needed; old roll/lineage records retain their reading. Old `NeedsNoLeader` is removed only when its other exception callers have migrated |
| W-A25.63, close combat | CC fact assembly and current-reading resolver invoke `CaptureAttemptModifier` per defender; preview and resolution use the same terms | Remove +1 in the specified Italian/Eritrean cases, retain Inexperienced -1 and independent modifiers. Use a nullable recorded reading or equivalent established version boundary for new CC; old CC records keep the old arithmetic. Coordinate the new Eritrean escape context with W-A20.54, which retains ownership of the escape caller migration; add that dependency to its row rather than creating duplicate escape wiring |
| W-A25.75, fire | A shared viewer-safe fact reader supplies date, personnel class, weapon identity/color/origin to `CapturedEquipmentUse`; migrate LiveFire/ordnance fact construction, Fire Lane candidates and their command checks together | Apply exemptions and PSK restrictions consistently. Preserve physical capture, repair classification, shortage/non-qualified-use distinctions and old recorded fire. Old `CapturedBy` cannot be globally replaced with an exemption boolean. E1.76 night consequence consumes this verdict when its separate resolution work is wired |

The wiring designs must enumerate transitive consumers, including Ammunition Shortage, Breakdown/X#, ROF, Case H, To Hit color and night effects. An exemption from named use penalties must not silently change every use of `Captured`. This batch's calculator tests prove the distinction; the future wiring gate proves its application to recorded games.

## 9. Memory and performance checklist

- Added work is O(1) per queried unit/weapon or defender. The caller may perform an O(n) loop over the explicit CC defender list; no calculator scans a game state, catalog or map. No per-state/hex background work is added.
- No caches, retained game objects, file access, locks or mutable global collections. Ownership is the call; immutable returned values outlive it only if the caller chooses. Concurrent calls share no mutable state.
- Facts are tied to one view and snapshot by the existing workbench. No new map reads, seam rules, pinned-version changes or rout-settlement reads. Typed facts do not bypass disclosure.
- No drawing, fragment-key, patch, full-layer or transport changes. Existing counter rendering is reused.
- Focused pure tests and contract checks detect wrong verdicts; legacy comparison cases detect accidental behavior changes. No performance gain is claimed. Full replay timing is not warranted for this design-only change; implementation uses existing gate checks, with a small allocation probe only if new hot-path allocations are actually introduced.
- Reuse the existing test projects and immutable fixture/reference data. Do not repeatedly load the whole catalog or PDF per case. No new CI test project is proposed.
- Record measured failures, bounded limitations and catalog gaps when discovered. Do not call an unmeasured speed or allocation result a pass.

## 10. Deferred work and authoritative records

At claim creation/build, update the existing backlog rather than creating a second work board. Record or link these boundaries:

| Boundary | Where it belongs |
|---|---|
| Actual Play/projector/Studio integration and replay-version design | Four W-A25 rows above; existing W-A20.54 for escape |
| Italian 2-2-7 crew and missing Finnish weapon variants/origin metadata, if absent at implementation baseline | Catalog track, referencing existing pass 80/180 inventory gaps; calculator checks still use explicit facts |
| A21.22 capturing-squad exchange command | Existing A21.22/pass 110 backlog; this design only supplies the nationality exception |
| Broader deployment/recombination legality and other nationality exceptions | Existing A1.31/A1.32 claims/backlog; no claim of full completion here |
| Remaining A25.66 clauses and any missing Eritrean counters | Existing nationality/catalog work; this batch covers only inherited deployment/capture/escape treatment. Keep A25.66 partial and record its remaining ELR, Battle Hardening, Lax and PAATC work in the backlog at claim creation |
| E1.76 automatic captured-MG Sniper attack resolution | Existing night backlog; the A25.75 exemption verdict is supplied now |

Inventory and coverage updates must distinguish calculator completion, missing catalog data and unwired behavior. Audit A25.61 and A25.71 clause by clause before moving their rows to built. Preserve known limitations rather than closing an entire rule on one new test. Regenerate Minigames and pass Rules status lines with their scripts; never edit generated status prose by hand.

## 11. Build outputs outside the repository

The source worktree is `C:/Users/dland/.codex/worktrees/nationality-batch-design/LimboDancer.MCP`. Design preparation produced only the document and temporary PDF research text. Implementation registered the separate `studio-6680` and `test-classes-bin` outputs under the Codex work folder. The four runtime positions and cache use `nationality-boards` and `nationality-cache` there, not another checkout's live games. Studio was stopped after its proof.

For approved implementation, set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` before every `code-builders` tool. Register each external Studio/test/gate build when created, keep it under that Codex working folder, and use port 6680. Remove batch-owned outputs at the earliest opportunity, at latest after successful CI; list the registry at completion and explain any unrelated retained entries. Do not remove another agent's outputs or running Studio.

## 12. Approval and review boundary

The user approved these four Rules claims with the explicit A25.77 supporting slice, A25.66's deployment/capture/escape inheritance, existing-calculator reuse and legacy-answer boundary. Four open wiring rows and the W-A20.54 dependency update record the separate caller work. This new design file accompanies the implementation PR; it does not bypass CI or grant merge permission.

The claims remain built for Claude/user review. The user gives the word for merge. C6.17 is the one rule claim merged since the recorded batch gate; merging these four will reach five and require the batch gate on updated main before a sixth claim can merge. This PR does not claim that future gate has run.

## 13. As built and verification

### 13.1 Delivered APIs and boundaries

| Claim | Authoritative members | Result boundary |
|---|---|---|
| A25.61 | `ScenarioA1ItalianDeploymentCalculator.Evaluate`, `EliteMmc`; `ScenarioA1ItalianNationalityRules.UsesItalianDeploymentAndCapture` | Nationality restriction only. Printed elite identification explicitly requires actual Italian nationality; Eritrean identity is never rewritten |
| A25.63 | `ScenarioA1ItalianCaptureCalculator.CaptureModifier`; `ScenarioA1PrisonerCalculator.Escape(facts, ItalianEscapeContext.WithEritreanInheritance)` | One defender's alternative -1/0/+1 capture term; one existing escape calculator with explicit opt-in inheritance |
| A25.71 | `ScenarioA1FinnishOrganizationCalculator.Evaluate` | Explicit directed/leaderless requirements and leaderless recombination, never full action legality |
| A25.75 | `ScenarioA1CapturedEquipmentUseCalculator.Evaluate` | Captured Use and E1.76 treatment, plus A25.77 use refusal; no physical-capture, repair, qualification or ammunition rewrite |

Each family has its own facts/verdict models and contract file. Four new contract IDs and the existing escape contract are listed in section 5. PR review consolidated escape into `a20.55-prisoner-escape`, which retains public prisoner disclosure, adds Eritrean nationality and uses the explicit inheritance overload. The contract places are capture declaration 8.21B, escape 8.14B, and Finnish organization 1.13B/1.21A. Setup/Guard/Temporary Crew and equipment use have no single applicable ASOP step.

Existing gameplay callers keep their answers: `MayDeploy`, `NeedsNoLeader`, `CapturedBy`, setup filling and the old CC resolver remain unchanged. The original `Escape` overload forwards with `ItalianEscapeContext.Legacy`, preserving its refusal order/text and unknown behavior. `EscapeRefusal` also retains its legacy reading. The Workbench escape adapter selects the current reading explicitly. No recorded record, event or serializer changes. The unused `CarrierHalfSquad` input was removed: deployment requires a squad and Finnish recombination already waives a leader. The existing Carrier helper remains authoritative for its other callers. Equipment verdicts now carry the deciding rule citation so the contract does not repeat the class decision; both the date refusal and low-class PSK penalty cite A25.77.

### 13.2 Audit of existing clauses

- A25.61: `FireReference.ReplacementOf("italian-elite-squad")` remains `italian-line-squad`, the 3-4-6. The new tests assert the existing table and the actual-Italian 4-4-7, 2-4-7 and 2-2-7 elite identities, including negative Finnish/Eritrean controls. No replacement table was duplicated.
- A25.71: tests assert the complete existing Finnish rank order in both directions. Rally, Infantry CC and vehicle CC already call `FieldPromotion.Applicable`, which excludes Finns; source audit confirmed all three call sites.
- Finnish leader loss is a source audit, not a claim that the new rank test exercises LLMC/LLTC. `ScenarioA1MoraleCheck.Resolve` hands the supplied leaders and units to the existing Fire calculator. `ScenarioA1FireCalculator.LeaderLoss` chooses LLMC for elimination and LLTC for breaking, uses the actual leader morale to select affected units, and adds the reversed actual negative leadership value plus the existing Allied adjustment. Finnish rank values therefore produce the stated lesser checks without a new nationality clamp. No leader-loss implementation was changed.
- The old CC resolver retains the independent A11.52 Passenger capture term of -2 and its existing capture term. The new capture calculator supplies only the replacement ordinary term; W-A25.63 must compose it per defender while preserving Passenger, concealment and leadership modifiers. New tests assert independent mixed-defender answers; they do not claim that the unwired CC resolver already consumes them.

### 13.3 Demonstration, tests and review

Initial builder proof, before PR review, used Codex Studio on 6680 and the four committed minimal positions on imported bd01. The owner could choose its subject at [G4]; the opponent could select only its sealed enemy stack for Owner facts, with private facts hidden and no identifying verdict. Switching viewers cleared entered private facts. All four bundles were inspected with owner, opponent and adjudicator perspectives. Demonstrated results included Italian restriction, Finnish leaderless 1TC versus directed NTC, Finnish HS recombination with no leader, Italian capture term zero, Eritrean escape refusal, Russian LMG exemption in 1940 versus penalties in 1939, and PSK refusal in June 1944. Existing catalog counters were displayed correctly; absent variants stayed explicitly entered facts. The revised capture disclosure and single escape form are checked separately in section 13.4.

The first focused run passed 85 Rules cases across 17 named methods. The three affected bundle methods passed 30 cases: all 28 new contract checks, manifest soundness and game header association. The final review added one PSK unknown-class boundary and additional Guard materiality assertions, verified by rerunning only the two affected methods. Strict builds treat warnings as errors. Full-suite execution belongs to PR CI; no per-claim replay gate is required for this Rules-only batch.

An independent read-only reviewer checked the PDF, calculators, contracts, compatibility boundaries and actual initial test logs. Review findings were fixed during the work: actual-nationality guarding of elite classification, conditional private-fact handling for non-Finnish organization and unrelated equipment, irrelevant Guard facts for elite Italian restriction, the A21.22 page citation, foreign-color PSK class materiality, and the Guard/Unarmed combination when automatic deployment is already ruled out. Builder UI observations are distinct from that reviewer's evidence. Inventory states remain built pending Claude/user PR review.

Calculators are bounded constant-time decisions over facts. No map/state traversal, service reference, retained cache or mutable shared result was added. Contract registry metadata follows the existing lazy registry; each verdict allocates only its small result/reason/roll records. The memory/performance checklist in section 9 still applies and needs no new benchmark for this change.

### 13.4 PR 121 review corrections

The user authorized the five findings in Claude's review of head 5fcddff6. The additional `ScenarioA1PrisonerContract.cs` hold was recorded on main in ff43c0f0 before editing. Escape now has one contract and one nationality list; the approved opt-in calculator overload and legacy entry point remain. Capture disclosure follows the existing CCV contract at declaration, with unknown material facts still producing not-evaluated. The unused Carrier input and unreachable elite arm were removed. The equipment verdict carries its deciding citation, including A25.77 for both PSK date refusals and the low-class penalty.

The revised Studio build passed with zero warnings and errors in `E:/Archive/GitHub/dlandi/asl-gate-work-codex/nationality-build.log`. The Workbench showed one escape choice, an Eritrean refusal citing A25.66, public capture inputs and a plus-one verdict from the German attacker's view, no Carrier input, and a hand-entered Finnish Green PSK penalty citing A25.77. The map continued to conceal the enemy stack; these contracts do not reveal or gather hidden roster facts.

`verify.ps1 -Review` passed 59 cases in ten named Rules methods and 40 cases in the three bundle methods, including existing a20-prisoners checks. Evidence is `E:/Archive/GitHub/dlandi/asl-gate-work-codex/test-classes/Rules.log`, `Rules.trx`, `Play.log` and `Play.trx`. The initial strict test compile caught xUnit2031 in the new assertion; the assertion was corrected before the successful run. Both projects were compiled with warnings as errors. No downstream implementation or recorded-game facts changed. Claims remain built for reviewer acceptance; this correction does not mark them reviewed or authorize merging.
