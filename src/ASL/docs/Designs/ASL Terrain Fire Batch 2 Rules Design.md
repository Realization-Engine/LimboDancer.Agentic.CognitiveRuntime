# Terrain Fire Batch 2 Rules Design

**Status:** Approved by the user and implemented Rules-only, 2026-11-04. Independent read-only review approved after corrections. PR review and merge approval remain separate.

**Source baseline:** main at `defd4f1900eda4a4502dbf586a57a3ff0616d347`. The last batch gate proved `aa25111d`; zero subsequent Rules claims had merged when this batch started. Claim-only commit `f70bd817` reserved B23.31 and B16.31 on main. This branch delivers two claims, not one claim for the branch.

**Wiring record:** [Rule inventory, section 7](<../ASL Rule Inventory A to E.md#7-wiring>) now holds open W-B23.31 and W-B16.31, coordinated with W-B23.74. Section 11 retains the design rationale for that handoff. No additional wiring document or assignment ledger is created.

## 1. Outcome and scope

Build two independent Rules calculators and their contracts, tests and minigame bundles:

| Claim | Calculator | Corrected current-reading answer |
|---|---|---|
| B23.31 | `ScenarioA1BuildingFireCalculator.BuildingTem` | Ordinary building TEM depends on the target's building, even for fire from inside that building. A Bypassing target loses the building contribution against non-Residual attacks. Standard-ASL Residual FP retains it. Factory answers delegate to the existing Factory calculator. |
| B16.31 | `ScenarioA1MarshHeCalculator.Evaluate` | A supported HE firepower component into applicable marsh is halved, including DC. Attacks against a bridge or units on it are exempt. The multiplier is separate from Area Fire, Critical Hit and IFT-column selection. |

These belong in one delivery because both distinguish attack facts from target terrain facts. They do not need a shared general-purpose terrain engine or a new project. Each can be tested and reviewed independently.

B16.31 includes the narrow inheritance necessary to decide its own multiplier: B16.6 flooding, B16.7 mudflat and B16.8 weather. Its claim reserves those three inventory rows for HE applicability only. Their overall coverage remains partial; this does not implement movement, Bog, water depth authoring or other consequences. This is two claims with supporting rows, not five completed rule sections.

No Play, Units, Maps, ASL.Turns, normal Studio page, event or recorded-fact schema changes. Existing public Rules members retain their names, refusal order, serialized results and answers. No existing game acquires the new reading through a changed default.

## 2. Rulebook evidence and rulings

Physical pages refer to the registered `eASLRB_v3_01.pdf`, not printed chapter page numbers. The primary pages 130 and 136 were rendered and inspected. All 716 PDF pages were scanned for marsh and the named cross-references; 74 pages matched. Relevant cross-references were read directly from their PDF pages. Scan evidence is retained in `E:/Archive/GitHub/dlandi/asl-gate-work-codex/batch2-reading/whole-book-scan.txt`. A search hit alone is not treated as a reviewed rule.

| Ruling | Evidence | Design decision |
|---|---|---|
| TF2.1 | B23.3, B23.31, p. 136 | Stone contributes +3 and wood +2. Ordinary same-building fire does not reduce that contribution. Reuse `ScenarioA1FactoryCalculator.BuildingTem` for the material table rather than add another table. |
| TF2.2 | B23.31, p. 136; A4.34, p. 50; A8.2, p. 60; A9.222, p. 63 | Bypass affects building TEM separately from lane terrain. Fire Lane FP is Residual FP for this decision, including retaining the building contribution during standard-ASL Bypass. Do not turn the target's entire terrain into Open Ground or a building merely to select TEM. This calculator does not decide FFMO, FFNAM, LOS, walls, hedges or Snap Shots. |
| TF2.3 | B23.741, p. 139; B23.743, p. 140 | Delegate eligible Factory cases to `ScenarioA1FactoryCalculator.Tem`. Residual entry across a building hexside gives its +1; entry across a non-building hexside or by Bypass gives the ordinary building TEM. Do not repeat the Factory decision tree. |
| TF2.4 | B23.31, p. 136; J2.23, p. 602 | The standard-ASL Residual exception must not silently apply to Deluxe Bypass. Deluxe plus Bypass plus Residual returns not-evaluated in this batch. J2.23's hexside-specific Residual placement and attack scope are a separate backlog item. Other supported cases do not depend on that exception. |
| TF2.5 | B16.31, p. 130; A23.1, p. 90; A23.71, p. 91; terrain chart, p. 326 | DC's protection from PBF/TPBF and ordinary Area Fire modifiers does not exempt it from marsh's HE reduction. Apply the marsh multiplier to its FP component, including the thrower's separate Location when that Location is marsh. A Set DC's different base FP is supplied, not hard-coded as 30. Its separate no-TEM rule must suppress any building contribution when composing the attack. This batch does not implement Set DC legality or detonation. |
| TF2.6 | B16.31, p. 130 | Bridge presence alone is not exemption. The effect must attack the bridge itself or a unit on it. A unit beneath or beside the bridge in marsh still gets the reduction. |
| TF2.7 | B16.6, B16.7, B16.8, p. 130 | Mudflat inherits the reduction. Connected flooded marsh is treated as water instead; scenario-defined Snow, Deep Snow or sub-zero temperatures remove marsh treatment. Rules decides these effects from facts, not Play. Dry/fordable conversion to mudflat does not change this multiplier. |
| TF2.8 | C3.53 and C3.71, p. 170 | Area Target Type halving and marsh halving are independent. Preserve exact decimal FP through multiplication, then let the existing final arithmetic choose an IFT column. Do not double or halve a component a second time here for Critical Hit or Area Fire. |
| TF2.9 | C8.31, p. 178; G7.2, p. 285; W.7B, p. 656; W.8, p. 657 | Ordinary-marsh HE equivalency is subject to this reduction. HE-dependent swamp and VT FFE cases return not-evaluated, rather than enter the ordinary-marsh branch. Known non-HE and Residual effects return factor 1 for this modifier even in swamp; this makes no ruling on their other modifiers. The calculator never decides whether a weapon may legally fire; W.8 and other eligibility restrictions still require their own verdicts. |

The review found that the inventory's old B23.31 note says the Factory exception is unbuilt. Its calculator is now merged, although W-B23.74 remains separate. Implementation updates that stale note without claiming the Factory wiring is done.

## 3. What decides it today

The map combines `rule_sites.py B23.31 B16.31 --words marsh BypassTerrain` with reads of the callers and the arithmetic. The first scan lists 31 files. Movement, entry, rout, rally, concealment, smoke, packing, terrain rendering and map-generation hits do not decide these two fire modifiers and are not implementation targets.

Paths below are relative to `src/ASL/`; method names are the durable anchors.

| Site | Role | Treatment |
|---|---|---|
| Rules `ScenarioA1FireMapRules.UsesBypassLane`, `.BypassTerrain` | Decides lane selection; the former admits Residual FP, and the returned lane terrain later supplies TEM | Retain legacy answers. Annotate the corrected building-TEM entry point and the separate downstream replacement. Do not make the old selector return a different answer. |
| Play `GamePlanner.Fire.cs:FireMapFacts`, around 1692-1750 | Gathers map and movement facts, then overwrites `terrain` with the lane result; this also selects the wrong Residual building TEM | Track 3 must keep lane facts and building TEM separate. No edit here in the Rules claim. |
| Play `GamePlanner.FireExtensions.cs:FireLaneFacts`, Rules `ScenarioA1FireFollowUps.FireLaneAttack` | Builds a Fire Lane attack from Residual facts | Future gathering must preserve the Residual classification for building TEM. Neither existing member changes in this Rules claim. |
| Rules `ScenarioA1FactoryCalculator.BuildingTem`, `.Tem` | Already decides material TEM and Factory exceptions | Read-only dependency. No new Factory algorithm or changed Factory contract. |
| Rules `ScenarioA1FireCalculator.Arithmetic`, around 3570-3615 | Ordnance hit branch halves whenever `TargetTerrain == "marsh"`; DC branch has no marsh term | New calculator owns the current reading. Preserve the old hit behavior through a compatibility entry point; leave old DC arithmetic unchanged until wiring. |
| Rules `ScenarioA1FireCalculator.Arithmetic`, around 3740-3815 | Consumes `FactoryTem` or terrain TEM and applies attack-specific rules such as FT, Snap Shot, CH and Area Target Type | This is the downstream consumer of the new component, not permission to apply building TEM to every attack. Preserve current behavior during this batch. |
| Play `GamePlanner.Ordnance.cs:FireMapFacts` consumers and `AreaLocations` | Gathers and copies terrain/LOS facts per target Location | Future wiring carries the appropriate per-Location facts and verdicts, including distinct bridge positions. |
| Play `GamePlanner.DemolitionCharges.cs` and `LiveFire.DemolitionChargeFromState` | Gathers charge mode, user, target and placement facts | Future wiring supplies a marsh context for each actual effect Location, including the thrower's return attack. |
| Play `GamePlanner.Fire.cs:AddFireEvents`, Units `FireRecords.cs`, `GameProjector.Fire`, Play `FireRecordVerifier` in `LiveFire.cs` | Records, verifies and projects the already resolved attack | No terrain decision added. Future reading selection must be identical for live evaluation and verification. |
| Rules `ScenarioA1ArmorCalculator.SpecificCollateral` | Separate FP-to-column path with its own D.8/D5.31 contract | Do not infer that the new generic FP multiplier automatically applies here. Collateral/To Kill integration requires a separate rule review and is explicitly deferred. |
| Existing tests `ScenarioA1OrdnanceTests.HeIsHalvedIntoAMarsh`, `BacklogPass10Tests.BypassMovesAlongTheOpenHexsidesOfAWoodsHex`, Factory calculator tests | Legacy and dependency evidence | Preserve assertions. Add focused tests of the new calculators rather than rewriting these tests to call the new behavior wired. |

No dedicated Units, ASL.Turns or Studio legality calculator for these two modifiers was found in this map. The absence of a direct hit is not a claim that future integration has no work there.

## 4. Building fire calculator

Files: `ScenarioA1BuildingFireModels.cs`, `ScenarioA1BuildingFireCalculator.cs`, `ScenarioA1BuildingFireContract.cs`.

`BuildingTem(BuildingFireFacts)` returns a component verdict with a disposition, nullable TEM, reason and deciding citation. Dispositions are evaluated, not-applicable and not-evaluated. An evaluated zero means no building contribution, not zero total TEM. A non-building target returns not-applicable, not a replacement for its actual terrain protection.

Facts:

- Building kind: none, wooden, stone, wooden Factory or stone Factory; normal ground/floor Location versus an unsupported special Location.
- Standard or Deluxe profile; attack classification of direct non-Residual, indirect non-Residual, or Residual (including Fire Lane FP); whether the target is Bypassing. Unknown classification is permitted only when genuinely unavailable.
- For eligible Factory cases, firer place, depiction trace, adjacent rubble trace, roofless status and Residual entry. Reuse the existing Factory enum types and semantics, but do not accept a second, independently populated `FactoryTemFacts` object. Construct it internally: `Stone` comes only from building kind, and `IndirectFire` and `Residual` come only from the attack classification. An indirect-Residual combination is therefore unrepresentable. Reject contradictory Bypass/entry facts before delegation.

Decision order:

1. Validate enum values and contradictory supplied facts. For Factory Residual, normalize entry and Bypass before testing any profile boundary: a Bypass entry establishes Bypass, while either other known entry establishes non-Bypass; reject any supplied disagreement. Known Bypass with unknown entry supplies the Bypass entry, but known non-Bypass does not distinguish the other two entries. Special building forms outside the supported ordinary/ground-Factory scope return not-evaluated; a known non-building case is not-applicable.
2. Short-circuit only when every possible value of an unknown fact gives the same answer. For an ordinary standard-ASL building, known Residual FP gives material TEM even with unknown Bypass; known non-Bypass gives material TEM even with unknown attack classification. Unknown Bypass under non-Residual fire, or unknown classification while Bypassing, remains not-evaluated. Deluxe with known or possible Bypass Residual returns not-evaluated with the J2.23 boundary unless known non-Bypass already excludes it. Do not demand profile information on branches whose answers do not depend on it.
3. Non-Residual fire at a Bypassing target returns zero building contribution, before considering Factory internal-fire TEM. Irrelevant unknown Factory LOS facts do not block that answer.
4. Before ordinary-building fallthrough, return not-evaluated for every remaining materially unknown input. In particular, unknown Bypass plus unknown attack classification cannot establish material TEM, and unknown profile with known Bypass Residual cannot exclude the Deluxe boundary. Otherwise return material TEM. Neither same-building origin nor missing Factory facts affects it.
5. For Factories, call `ScenarioA1FactoryCalculator.Tem` and preserve its not-evaluated result. Materially unknown attack classification remains not-evaluated because Factory branches can differ. Use the normalized entry from step 1; never turn an unknown entry into a non-building-hexside entry by default. A known Residual entry does not require irrelevant LOS facts. Call `Tem`, not the existing Factory contract, which also asks unrelated hindrance questions.

The component is consumed by the existing rules for total DRM later. FT's lack of ordinary TEM, Set DC's no-TEM resolution (A23.71, p. 91), Snap Shot treatment, Critical Hit reversal, Area/Infantry Target Type placement of TEM, Fortified Locations and other cover do not move into this calculator. A material-TEM answer is never authorization to add it to an attack whose own rule excludes TEM.

## 5. Marsh HE calculator

Files: `ScenarioA1MarshHeModels.cs`, `ScenarioA1MarshHeCalculator.cs`, `ScenarioA1MarshHeContract.cs`.

`Evaluate(MarshHeFacts)` returns disposition, nullable decimal multiplier, reason and citation. It returns a factor, not a final FP or IFT column. A supported unaffected case returns 1; affected marsh returns 0.5. A not-evaluated verdict has no numeric multiplier and cannot silently become 1.

Facts:

- Effect family: ordinary HE IFT component (including normal OBA HE), DC, HE-equivalency IFT component, non-HE, Residual FP, or a named unsupported special effect such as VT FFE. This is the effect being resolved, not the nationality or broad equipment class.
- Terrain kind: marsh, mudflat, other ordinary terrain, or special terrain such as swamp. No inference that all water-like terrain is marsh.
- For marsh/mudflat: scenario-defined Snow/Deep Snow and sub-zero facts; whether the marsh is adjacent to or connected through continuous marsh to a flooded river/stream. Map connectivity is a measured fact. The consequence of that connectivity is decided here. These facts are entered by hand in the Workbench until separately wired.
- Whether a bridge is present; target category of bridge structure, unit or ground Location; for a unit, whether it is on the bridge. A ground-Location effect is into the marsh, not an attack on the bridge or a statement about every unit in the hex. A concealed/hidden unit's materially unknown position remains unknown. Absence of a bridge or a ground-Location target makes the private on-bridge fact irrelevant.

Order: validate supplied values; reject supported-domain contradictions; short-circuit known non-HE and Residual to 1; reject special HE/terrain cases; ordinary non-marsh returns 1; weather/flooding removes this marsh modifier; a bridge structure or a unit on the bridge returns 1; a materially unknown bridge-position fact returns not-evaluated; remaining marsh/mudflat returns 0.5. Unsupported special cases must not be accepted by masquerading as ordinary HE or ordinary marsh.

Unknowns are tested for materiality after known exemptions: a known on-bridge unit gives factor 1 even if weather is unknown to a typed caller, and known qualifying cold/flood conditions give factor 1 even if the unit's bridge position is private. An unknown required condition with no decisive exemption returns not-evaluated. Invalid supplied combinations, such as an on-bridge unit with no bridge or an on-bridge assertion for a ground-Location effect, are rejected before these short circuits.

`LegacyOrdnanceDivisor(string? terrain)` preserves exactly the old hit branch: decimal 2 for the literal legacy marsh key, otherwise decimal 1. It is explicitly a compatibility adapter, not the current-reading API, and does not infer new bridge/weather/ammunition facts that old records lack. Replace only the old conditional divisor with this call. Keep the original multiplication/division order and decimal operand representation: an arithmetically equal multiplication by 0.5 can change decimal scale and serialized JSON. Require serialized arithmetic parity, not just numeric equality. Leave DC's legacy answer unchanged and document the replacement. Do not introduce a new field on `FireAttack` in this claim.

Examples: 12 HE FP becomes 6 in marsh; an existing CH-doubled 24 becomes 12; an already Area-halved 6 becomes 3; a 2 FP AP HE-equivalency component becomes 1. A 30 FP DC becomes 15; a concealed DC already reduced to 15 becomes 7.5; a supplied 36 FP Set DC component becomes 18, without authorizing Set DC play or adding TEM. Residual FP originating from HE stays factor 1 and is not halved again. The existing arithmetic, outside this component, selects the final IFT column. A thrown DC evaluates the target and thrower Locations separately.

## 6. Contracts and disclosure

Contract ids: `b23.31-building-fire-tem` and `b16.31-marsh-he`. Building outcomes expose building-tem, no-building-tem, not-applicable and not-evaluated. Marsh outcomes expose halved, unchanged and not-evaluated. Contract outcomes and typed verdicts must agree.

The building contract names B23.31 (p. 136) as its primary rule and cites the existing B23.3/B23.741 material and Factory dependencies without claiming those rows anew. The marsh contract names B16.31, B16.6, B16.7 and B16.8 (p. 130), with descriptions limiting the supporting rows to this FP modifier. Unsupported special-profile reasons cite their boundary; they do not register those rules as newly implemented.

Public facts are scenario profile, public terrain/material, Factory/roofless SSRs, weather and public bridge presence. These contracts evaluate declared effects or explicitly labelled hand-entered hypothetical effects. Their effect-family field is public in that context, with no on-map firer required for OBA or Residual. They do not gather an undeclared ammunition choice or expose a private attack-planning picker. A later pre-declaration preview must supply an owner-authorized context, not expose the private choice through this public declared-effect field. Typed calculator inputs still permit materially unknown classifications to produce not-evaluated.

Use self-owned `targetId` and `Concerns = "targetId"` for private Bypass, entry and on-bridge facts; use the existing Factory firer disclosure pattern for private origin and LOS facts. Each self-owned Unit fact permits only the viewer's units or the opposing side's anonymous stack handle, never an enemy identity. Public geometry never authorizes reading a hidden unit's position. Where a fact has become public through the actual declared movement/attack, future fact gathering may disclose that known fact without exposing its roster identity.

Contract validation checks required public facts, supported choice values, contradictory Factory entry, a claimed bridge target without a bridge, and applicability. Missing material information is not-evaluated. Irrelevant unknown private facts do not block a decidable contribution. Public facts that are not yet gathered are required hand-entered inputs, not default false values.

The existing registry validates all fact definitions before calling the contract. Use a flat schema, not a nested Factory object or conditional schema changes:

| Contract | Required public fields (`MayBeUnknown = false`) | Nullable owner fields (`MayBeUnknown = true`) |
|---|---|---|
| Building | `buildingKind` (none, wood, stone, wood-factory, stone-factory); `locationKind` (ordinary, unsupported-special); `profile` (standard, deluxe); `attackKind` (direct, indirect, residual); `roofless` (flag) | Self-owned `targetId`; `bypassing` and `residualEntry` concern targetId. Self-owned `firerId`; `firerPlace`, `tracedWithinDepiction` and `overAdjacentRubbleEnteringAcrossDepiction` concern firerId. Reuse Factory enum choice semantics for entry/place. |
| Marsh | `effectFamily` (he, dc, he-equivalency, non-he, residual, unsupported-vt-ffe); `terrainKind` (marsh, mudflat, other, swamp); `snowCover` (none, snow, deep-snow); `subZero`, `connectedToFloodedWater`, `bridgePresent` (flags); `targetKind` (unit, bridge, ground-location) | Self-owned `targetId`; nullable `onBridge` flag concerns targetId. |

Public environmental fields describe the selected scenario/Location even when a branch does not use them; the contract requires explicit values because the registry does. This is stricter input validation than the typed calculator's ability to answer from partial facts. No missing public field becomes false, and no new `unknown` choice disguises a missing feature. Nullable private fields may be absent when irrelevant or hidden. The contract reads and requires a private anchor only on a branch that needs its associated facts. It must not require a firer for Residual, indirect Factory TEM, or ordinary-building answers. Workbench checks supply no private value to a viewer who cannot know its anchor.

`ContractVerdict` has no numeric payload. The contract maps the typed disposition to an outcome and puts the exact TEM/factor and citation in `ContractReason`, formatted invariantly; tests assert those values as well as the outcome. Actual consumers use the typed verdict, never parse the displayed reason. No contract runtime/model extension is needed or authorized.

No dice or events are produced by either calculator. Contracts preview existing fire resolution with an adjusted component; they do not claim a new standalone event. Place uses PFPh / 2.22A as the primary ordinary-fire anchor, with descriptions also identifying MPh Defensive First Fire/Residual, DFPh and AFPh, including DC resolution. `asop_steps.py B23.31 B16.31 B16.6 B16.7 B16.8` found zero citing rows, so this proposal takes no ASOP rows; mixed phase rows remain track 3's.

## 7. Proposed claim holds and compatibility

These holds were reserved by claim-only commit `f70bd817` on refreshed main, with claim/status checks passing. `codex/terrain-fire-batch-2` starts from that main history and carries no other unmerged branch. The table below describes its reserved files; the inventory remains the lock.

| Claim | Proposed Rules holds | Inventory rows |
|---|---|---|
| B23.31 | Three new BuildingFire files; `ScenarioA1FireMapRules.cs (shared)` for compatibility documentation only | B23.31 |
| B16.31 | Three new MarshHe files; `ScenarioA1FireCalculator.cs (shared)` for the equivalent legacy adapter call and documentation | B16.31; B16.6, B16.7 and B16.8 limited to HE applicability |

The Factory files are read-only and are not claimed. Existing shared holds, notably the fire calculator, must be rechecked before the lock. No current public member is removed. The as-built record must identify the exact legacy adapter and every old member still awaiting callers' migration.

Claim state and coverage are distinct: reaching built means this approved scope is implemented, not that every special profile is supported. Retain explicit standard-ASL/supported-HE limits in the primary rows; mark supporting B16.6/B16.7/B16.8 coverage partial with their remaining consequences listed. Do not promote whole environmental sections to full coverage because their marsh modifier is complete.

## 8. Minigames and acceptance matrix

Two new bundles under `units/minigames`, each with the existing `<id>.minigame.json` and `<id>.scenario-card.json` convention. Manifest checks name a setup or revision of the bundle's game; there is no separate setups-file loader to extend. Stage and verify the positions through the existing isolated Studio workflow, and record the setup/revision used in the evidence.

- `b23-building-fire`: board 1, the existing [X3]/[X4] building area used by `b23-factory`, with two opposing squads and an ordinary-building card, without a Factory SSR. Factory, wood and special-profile variants in this new bundle are explicitly hypothetical hand-entered checks. Scenario SSRs belong to the card, so a setup does not switch this building into a Factory. Separately inspect the new contract on the existing `b23-factory` card/position using the Workbench's all-contracts selection; its Factory SSR supplies a real Factory demonstration without changing that bundle. Bypass, Residual entry and private geometry remain labelled hand-entered facts.
- `b16-marsh-he`: board 7 [P6], with an adjacent firer/DC carrier and a target in the marsh, using existing counter definitions. The checked-in board oracle confirms both [P6] and [G2] are marsh and have no bridge. Import/load the existing board through the normal isolated Studio path before staging; an oracle entry is not evidence that a Studio import already exists. Bridge, mudflat, cold and flooding checks are labelled hypothetical hand-entered variants, not fictitious features of [P6]. No board-source edits or new counter catalog are proposed.

Use Codex Studio 6680. First verify the real positions and the contracts from both sides and the adjudicator, then write/run only the new test methods and directly affected legacy methods. Hand-over must clear private inputs and previous verdicts. No live attack is represented as corrected by an unwired Workbench verdict.

| Tests | Required boundaries |
|---|---|
| Ordinary building | Stone +3, wood +2; same-building and outside origins equal; no building is not-applicable; non-Residual Bypass removes building contribution only; standard Residual Bypass, including Fire Lane FP, retains it; Deluxe Residual Bypass not-evaluated; standard Residual with unknown Bypass and ordinary non-Bypass with unknown attack kind still give material TEM; unknown Bypass plus unknown attack kind, or unknown profile with Bypass Residual, returns not-evaluated |
| Factory composition | Internal trace +1; trace leaves depiction uses building TEM; Residual building/non-building/Bypass entries; non-Residual Bypass returns zero before delegation; canonical material/attack fields reach the existing calculator; materially unknown Factory facts; contradictory entry rejected in both directions; inferred Bypass still respects Deluxe boundary, and known non-Bypass entries exclude that boundary even with an initially unknown Bypass flag; Roofless Indirect Fire retains existing refusal |
| Marsh applicability | Ordinary HE, normal OBA HE, DC and HE equivalency halved; other terrain, non-HE and Residual unchanged, including Residual originating from HE; mudflat inherits; Snow, Deep Snow and sub-zero each remove the modifier; flooded connection removes it, flooding elsewhere does not; bridge structure/on-bridge exempt, off-bridge and ground-Location effects not exempt; bridge presence alone insufficient |
| Arithmetic composition | 12 to 6, 24 to 12, 6 to 3, 2 to 1, 30 to 15, 15 to 7.5 and 36 to 18; no rounding, duplicate CH, Area Fire or marsh term; distinct thrower and target Locations. Set DC's example proves only marsh FP modification; its no-TEM composition and legality stay outside this calculator |
| Exclusions | HE-dependent swamp, VT FFE and unsupported special contexts explicitly not-evaluated; swamp non-HE/Residual gives factor 1 for this modifier; no weapon-use permission inferred from an FP verdict; no change to To Kill or Specific Collateral rules |
| Disclosure/validation | Opponent identities unavailable; unknown material Bypass/on-bridge/attack facts not-evaluated; irrelevant hidden facts short-circuit; known cold/flood exemption ignores private bridge position; known bridge exemption ignores unknown typed weather; ground-Location effects need no unit anchor; missing required public facts, malformed choices and contradictions rejected; contract outcome, exact numeric reason and typed verdict agree |
| Compatibility | `HeIsHalvedIntoAMarsh` unchanged; parity of legacy divisor for marsh, ordinary terrain and null; original decimal scale and serialized arithmetic preserved, including CH and fractional inputs; old DC result unchanged; UsesBypassLane/BypassTerrain unchanged; old record JSON and replay reading unchanged |

Use new test classes `ScenarioA1BuildingFireTests` and `ScenarioA1MarshHeTests` in the existing Rules tests project. Extend the existing parameterized bundle checks, not a new test project. Preserve other Factory tests; run only the methods exercising composition if the dependency is touched, which this design does not require.

Acceptance: Workbench evidence, focused tests, contract/bundle validation, independent read-only rule/disclosure review, corrected findings, accurate inventory/coverage/backlog and generated status lines, and green CI. No per-claim gate is needed while scope and legacy answers are preserved. The standing five-claim gate still applies cumulatively; these two claims do not alone reach five from the recorded baseline.

## 9. Memory, performance and completion

Both decisions are bounded scalar checks; no map/state traversal, service reference, retained cache, file read or static mutable answer. Factory composition makes one existing bounded call. Use a small immutable verdict; no per-target collection construction is needed. The legacy adapter must not allocate a new fact graph just to compare one terrain key.

No new state/map scans or retained per-unit/per-hex data are added. The only live compatibility change replaces an existing terrain comparison with one bounded, allocation-free adapter call; its result and arithmetic representation remain unchanged. Later gathering must use the existing state map context and memo, complete Location/version/view keys and one immutable snapshot. Rendering fragments, patches, rout settlement and storage remain untouched. No timing gain is claimed; no dedicated performance run is needed for these bounded scalar decisions. Regression checks target rule boundaries and, for wiring, equal old-record replay plus documented new-reading differences.

Tests reuse expensive immutable inputs; the existing Rules/Play CI shares already cover their projects. Register Studio/test build outputs in `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex`, stop Studio 6680 when finished and clean only owned temporary outputs. Preserve evidence. Deferred items are recorded in backlog section 100; downstream rows are in inventory section 7, with handoff rationale in section 11 below.

## 10. Design review and approval boundary

Self-review corrected the following gaps before delivery: separating TEM from movement terrain; preserving the Deluxe exception; including marsh weather/flooding inheritance rather than asking Play to decide it; returning only a modifier so Area Fire/CH cannot be applied twice; preserving legacy decimal division and serialized scale; and restricting public effect classification to declared or labelled hypothetical effects. It also distinguishes the board oracle from an actual Studio import and excludes unrevised collateral/special-fire paths explicitly.

The requested accuracy/correctness/consistency review, including an independent read-only reviewer, additionally corrected duplicate Factory fact authority, Fire Lane classification, Set DC's no-TEM boundary, conflicting swamp short-circuit outcomes, and missing HE-equivalency/HE-origin Residual tests. It made unknown-fact materiality explicit, added ground-Location effects, specified the existing flat contract schema and numeric-reason representation, and removed the nonexistent setups-file and per-setup SSR assumptions. The performance statement now accounts for the compatibility adapter. ASOP was rechecked with all five proposed inventory rows, yielding zero citing steps. These fixes change the design and its acceptance criteria, not production behavior or claim states.

The user's approval authorized the two Rules claims, the narrow supporting marsh rows, their contracts, two bundles, focused tests and delivery records. It did not authorize downstream wiring or a merge. The inventory holds the claims; this document records their design and implementation evidence under [AGENTS.md](<../../../../AGENTS.md>).


## 11. Downstream handoff rationale, outside this implementation

The [inventory, section 7](<../ASL Rule Inventory A to E.md#7-wiring>) is the authoritative wiring record, now containing W-B23.31 and W-B16.31. [Wiring Prompt 007](<../Prompts/Codex Wiring/Wiring Prompt 007.md>) remains the latest generated snapshot. The following design rationale supports those rows, assigns no work and authorizes no wiring. Regenerate the numbered handoff at the next required batch gate rather than edit an earlier snapshot.

### W-B23.31, fire capability

- `Play/GamePlanner.Fire.cs:FireMapFacts`: gather the target building, material, Bypass and declared attack facts. Ask `ScenarioA1BuildingFireCalculator.BuildingTem`. Keep Bypass lane terrain independently for FFMO and other decisions. Do not substitute a building terrain key for all movement facts to repair Residual TEM.
- `Play/GamePlanner.FireExtensions.cs:FireLaneFacts` and `ScenarioA1FireFollowUps.FireLaneAttack`: retain the Residual classification when supplying the new building component. Fire Lane creation, targeting and other modifiers remain with their existing rules.
- `Play/GamePlanner.Ordnance.cs` and `AreaLocations`: propagate the appropriate per-Location component. Apply TEM at the correct TH/effects stage through the existing ordnance calculators; do not add it at both stages.
- Coordinate with **W-B23.74**, which still owns Factory fact gathering and its callers. Use the existing Factory verdict, including Residual entry; neither wiring row implements another Factory algorithm. B23.31 may gather ordinary-building facts first, but must explicitly retain not-evaluated Factory cases until the needed facts are supplied.
- The future Rules consumer update supplies the new building component to fire arithmetic while preserving the existing FT, Snap Shot, cover and CH rules. Set DC's no-TEM resolution (A23.71, p. 91) must not receive this component; its implementation remains separately backlogged. This change belongs to wiring's reviewed compatibility work, not to this Rules-only batch.
- Legacy members: retire the Residual misuse of `UsesBypassLane` and the use of `BypassTerrain` as the sole building TEM source after all callers migrate. Do not remove lane geometry/terrain behavior still needed by non-Residual attacks or other modifiers.
- Expected new-reading difference: a standard-ASL Residual attack on a Bypassing unit in an ordinary stone/wood building hex uses +3/+2 building TEM; Factory entry distinctions use B23.741. Existing records keep their old reading. Deluxe hexside Residual remains separately deferred.

### W-B16.31, fire capability

- `Play/GamePlanner.DemolitionCharges.cs`, `LiveFire.DemolitionChargeFromState`, `GamePlanner.Fire.cs` and `GamePlanner.Ordnance.cs`: gather effect family, actual effect Location, terrain, bridge target/position, weather and flooded-marsh connectivity. Hand those raw facts to `ScenarioA1MarshHeCalculator.Evaluate`; do not compute its applicability in Play.
- Evaluate the target and thrower Locations separately for a thrown DC. Evaluate each Area Target Type Location independently, and retain any distinction between bridge structures, on-bridge units and effects into the marsh. A ground-Location verdict is not a blanket verdict for every unit in a hex containing a bridge. An already Area-halved or CH-doubled FP component receives the marsh factor once, before final IFT-column selection. Residual originating from HE is not an HE component to halve again.
- New-reading fire arithmetic applies the returned factor to DC and supported HE components. It must not use a generic `OrdnanceHit != null` test as proof that every hit is ordinary HE.
- Bridge presence, on-bridge position, weather and flood connectivity must be representable and verified from the selected snapshot. If those features are not yet available, do not invent defaults to claim the wiring complete. The Workbench's hand-entered facts do not supply production facts.
- Legacy member: remove `LegacyOrdnanceDivisor` only when its recorded-reading callers have a retained compatibility path. Keep old DC and ordnance answers, including serialized decimal representation, for recordings made before the new reading.
- Expected new-reading differences: DC FP is reduced in applicable marsh/mudflat, while a bridge target, an on-bridge unit, cold conditions or a flooded conversion can remove the reduction. Other independent FP modifiers remain independent. Special VT, swamp and unreviewed collateral paths remain outside this handoff's automatic application.

### Shared replay and UI requirements

`Units/State/FireRecords.cs`, `GameProjector.Fire` and `Play/LiveFire.cs:FireRecordVerifier` must agree on which reading applies. The wiring design chooses an explicit recorded version or nullable opt-in facts, omitted when null, with null selecting the existing reading. No property is removed, renamed or reordered. New events must carry enough verified facts to replay their own answer; they must not read today's weather or map instead of the recorded snapshot.

Studio candidate lists, previews and commands consume the same verdicts. No page decides the terrain modifier. Preserve the viewer's disclosure limits, clear private inputs at hand-over and do not present an unwired manual fact as gathered evidence.

Wiring has its own claimed paths, focused tests, recorded minigame play, merge gate, independent review, CI and user-authorized merge. Required cases include old replay equivalence; new Residual and Fire Lane Bypass building TEM; DC into marsh; separate thrower/target effects; per-Location bridge distinctions; weather/flood conversion; no duplicate halving of HE-origin Residual; no building TEM on Set DC when that separate feature becomes available; and preview/command/verifier agreement. Every changed replay or sweep answer must match an approved row's behavior change.

## 12. Implementation and review evidence, 2026-11-04

The six new Rules files implement the models, calculator and contract for each claim. `ScenarioA1FireCalculator` replaces only its exact legacy terrain divisor with `ScenarioA1MarshHeCalculator.LegacyOrdnanceDivisor`; multiplication, Critical Hit doubling, decimal division and serialized scale retain their original order and representation. `ScenarioA1FireMapRules` changes only comments identifying the compatibility boundary. No Factory dependency, caller, record schema, map source or ordinary Studio page changed.

The typed marsh calculator treats bridge presence as immaterial for a ground-Location effect or a unit known to be off the bridge. The contract still requires its declared public context and masks private position without a target anchor. Independent review found and corrected that unnecessary typed requirement, and aligned the new cards with their saved catalog version, ELR, phasing side and friendly edges. The reviewer approved the corrected code and tests with no remaining findings.

The inventory records that review in each claim's note and names the sub-agent reviewer. Claim state stays built until merge because `claim_check.py` treats reviewed claims as released holds, which would otherwise leave the unmerged implementation files unheld. User PR review remains pending.

Studio 6680 loaded the actual boards and these positions before the focused tests were authored:

| Position and revision | Observed evidence |
|---|---|
| `b23-building-fire-position`, revision 3 | Board 1 [X3] is a stone building; [X4] holds the firer. Ordinary standard Residual returned +3 without an irrelevant private anchor, direct Bypass returned zero building TEM, and Deluxe Bypass Residual returned not-evaluated. German view concealed the Russian target identity and hand-over cleared entered facts/verdicts. |
| `b16-marsh-he-position`, revision 4 | Board 7 [P6] was loaded and inspected as Marsh with no bridge, adjacent to the DC carrier in [O6]. DC returned factor 0.5; hypothetical unknown bridge position returned not-evaluated; bridge structure and owner-entered on-bridge position returned factor 1. Private on-bridge input was hidden without its owner anchor. |
| `terrain-fire-factory-position`, revision 3, existing `b23-factory` card | The existing card visibly declares `factory:X3`. The new contract selected through All contracts delegated internal direct fire to +1. Russian owner-entered building-hexside Residual entry also returned +1 without a firer anchor. The board inspector still shows underlying stone terrain; this is a hand-entered Rules demonstration under the SSR, not completed Factory production wiring. |

The final strict Studio build passed with zero warnings/errors after the review fixes, and the corrected new card was checked in the UI. Studio was stopped after verification. No rule contract rolled dice or wrote an event. Wood, bridge, mudflat, cold, flooding, Deluxe and special-profile variants remain explicitly hypothetical.

Focused validation covered 52 distinct Rules cases, including the existing marsh-ordnance case and contract-registry validation, plus 30 bundle checks (28 new verdict checks and two structural methods). The first Rules run passed 49 of 51; two test-data binding errors were corrected from integer `1` to nullable-double `1d`. The affected bridge method plus registry check then passed all 11 cases. There was no rule-result failure. `verify.ps1` records the complete named-method filter and checks the result files as well as process exit status.

Evidence is retained under `E:/Archive/GitHub/dlandi/asl-gate-work-codex`: `terrain-fire-build.log`, `terrain-fire-rules-first.trx`, `test-classes/Rules.log`, `test-classes/Rules.trx`, `test-classes/Play.log`, `test-classes/Play.trx`, and `terrain-fire-rule-sites.txt`. Source reading is under `batch2-reading`. Inventory, coverage, backlog and generated status/minigame/count summaries record delivery. CI and PR status are recorded on the PR; this document does not claim merge approval or a new batch gate.
