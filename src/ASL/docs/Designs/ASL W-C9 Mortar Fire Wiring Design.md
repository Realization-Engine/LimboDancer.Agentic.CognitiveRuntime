# W-C9 mortar fire wiring design

**Status:** Approved by the user on 2026-10-29. Claim `wire-fire-2`, Codex, reviewer Claude, branch `codex/wire-fire-2`. W-C9 was claimed on main in `db4e821f`. Implementation is authorized after wire-fire-1 and the Rules prerequisites below. The inventory sections 7 and 8 remain the ownership ledger.

## 1. Scope and order

Move every W-C9 site onto `ScenarioA1MortarCalculator`, following [the C9 rulings](<ASL C9 Mortars Claim Design.md>). C9.1, C9.2, C9.4 and C9.5 are on physical rulebook PDF p. 179. Related limits are A15.23, p. 83, and A21.13, p. 88. The calculator's C9 answer is one part of shot authorization, not permission to bypass other fire rules.

**wire-fire-1 goes first.** Implement and merge its approved design, including its gate and CI, before editing shared ordnance paths for wire-fire-2. Then merge main into this branch, never rebase. Both branches are published early. Run the overlap check again before work and before the PR. The shared files include LiveOrdnance.cs, GamePlanner.Ordnance.cs, GameProjector.cs and the Play page. Build on wire-fire-1's event reading and spotting marks rather than creating a competing path.

## 2. Site map

| Site | Facts and calculator answer |
|---|---|
| `LiveOrdnance.FromState` | Read equipment kind, gun type, caliber and actual participants. Ask `IsMortar`, `IsLight`, `FiresAsLight`, `CrewNeeded`, and `TargetTypeRefusal`. Recognize the catalog's 81mm `mtr` Gun. Preserve the difference between a physical light SW and a Gun used as light by a lone SMC. |
| `GamePlanner.OrdnanceShotFacts` | Ask `VehicleTargetRefusal` and `MinimumRangeRefusal` before shot validation; gather range from map geometry and minimum from equipment. An Area target hex containing a vehicle remains a hex target, not a To Kill vehicle selection. |
| `GameProjector.Ordnance` | Reconstruct participants independently; ask the same crew and firing-mode answers. Use `HasCoveredArc` for mortar facing. The existing pooled possessed-SW branch also handles MG/LATW and must retain their own rules. |
| Play ammunition, targets, directors and facing | Filter with the same refusal and crew/CA members as the command. Clear stale target, director and second-firer picks on revision or viewer changes. Show unknown facts as unavailable, not permission. |
| `ScenarioA1OrdnanceCalculator.Outside` | W-C9 explicitly requires admitting mortar gun types through `IsMortar`, then separating identity, physical SW size and effective firing mode. CA, crew, non-qualified use, captured use, heroic DRM and Ammunition Shortage must keep their respective Rules decisions. |
| `ScenarioA1OrdnanceMapRules.GunTargetStatus` | Admit the 81mm mortar through the same identity decision and apply its recorded minimum. Preserve all other terrain, LOS and range restrictions. |
| `ScenarioA1AreaCalculator.Run` | Obtain actual lone-SMC status from validated participants for `MultipleRofKept`, rather than treating every primary leader/hero as alone. Keep spotting and other ROF reductions cumulative. |
| `GamePlanner.RuleContracts.cs` | Gather `c9-mortar-shot` facts only for the authorized firer's viewer; use the same equipment, participant and range reads as the command. The contract does not replace complete shot validation. |

Critical Hit and Indirect Fire forwards already built in PR 62 remain in place. Do not double full HE FP twice or equate every future Area shot with Indirect Fire. The present `IndirectFireOfHit` compatibility assumption remains valid only while non-mortar Area fire stays outside the supported path.

## 3. Participants and Rules prerequisites

One SMC and two SMC are different firing facts. Add an optional second participant to the governed request and versioned evidence, not a fabricated MMC or a repurposed target list. Gather both identities, sides, Locations, status, possession and prior use. Rules must validate their combined use; projection must apply use and fire effects to both. A leader directing a shot is not automatically its second firer. Invalid, duplicate, enemy or unavailable participants refuse without dice or events.

The current shot model has one primary crew, and Area derives lone status from it. Before implementing the dual-SMC route, obtain the scoped Rules adapter/participation contract needed to carry the real fact into resolution. No caller may decide combined-unit eligibility or silently restore ROF after resolution.

The C9 verdict allows a hero over 82mm subject to A15.23. Its once-per-Game-Turn, MPh/DFPh, captured-use, heroic-DRM and NT CA limits need a complete Rules authorization path. If the existing Rules cannot express these facts, secure a follow-up Rules claim and approved contract before enabling this case. Do not implement phase or caliber thresholds in Play. This is a dependency of delivering W-C9 in full, not a reason to call the row wired with the case omitted.

W-C9 names changes to Rules adapters, but its removal column names no obsolete members. Before editing any Rules file, resolve that scope through the ledger and a scoped Rules claim with Claude as reviewer, preserving the old entry points' answers. The current caller claim does not grant an implicit whole-file hold. Record these dependencies in the backlog until resolved.

## 4. Recording and compatibility

Keep the serialized legacy `OrdnanceShot` shape and legacy answers. Under the user's amendment of Rules convention 6 on 2026-10-30, add a nullable `Participation` property, omitted when null; null preserves the earlier single-crew reading. Its version 1 value names the actual additional firer, or explicitly records that there is none. Preserve wire-fire-4's `UseMultiple` branch and pass the actual SMC count to its existing `User` calculator. Old records never acquire today's semantics by default.

`OrdnanceRecordVerifier` currently lives in `LiveOrdnance.cs` and receives the payload. Reader, writer, planner and projector must agree on the participation version and reject unsupported versions. Verification reconstructs both participants' facts from the recorded revision and compares them, then resolution asks Rules. Forged participants or missing evidence refuse.

The prerequisite is now claimed as C9.2 on `codex/rules-c9-participation`, main claim commit `505d6ec0`, with Claude as reviewer. `ScenarioA1OrdnanceParticipationCalculator` owns second-firer admission and counting, and forwards the count to the existing A21.13 and C9 calculators. The additional firer's facts retain its real crew identity and definition, active status, side and complete-Location relationships, conditions and prior use. The contract's two Unit facts each concern themselves; the secondary facts concern the second Unit, so neither side can enter the enemy's concealed identity to obtain a verdict.

The existing site map remains the map for this prerequisite: Area's primary-kind-only lone-SMC test decides the old reading; `LiveOrdnance` gathers the primary crew but currently supplies a literal one to `User`; the record verifier reconstructs facts; the projector records use and effects; and the page gathers candidates. The scoped Rules claim adds opt-in adapters only, with the callers changed by wire-fire-2. C9.2 and A21.13 were reread on PDF pages 179 and 88; A9.12, A7.81 and C5.4 on pages 62, 58 and 172 establish separate use, pinned firing, its To Hit penalty and loss of ROF. Pinned participation is allowed, with those consequences; it is not treated as an unpinned primary crew. No new process cache or whole-state copy is needed for two participants.

New behavior: refuse a named To Kill vehicle target and an unknown mortar minimum; allow the 81mm Gun with its applicable CA and crew rules; allow lone-SMC use through 82mm as light with no Multiple ROF; allow two qualified participating SMC to retain ROF where Rules permits; allow the larger heroic case only with all A15.23 limits enforced. Existing games retain their recorded reading.

## 5. Viewer and command boundaries

Proposals are read-only. Confirmation rechecks revision, side, possession and the Rules verdict; cancellation, stale confirmation and refusal consume no dice and append no event. The opponent must not learn hidden caliber, minimum, participants or refusal details through choices, evidence or history. Contract facts concern `firerId`; a concealed enemy subject remains unknown. Use PR 64's `units` and `sealed` manifest conventions for opposing-view checks.

## 6. Verification and acceptance

Targeted tests cover light mortar, 81mm Gun and lone 81/82mm SMC; two SMC versus one or a director; above-82mm nonhero refusal and every heroic limit; exact minimum, below minimum and unknown minimum; Area hex with AFV versus a named vehicle; CA, captured/non-qualified use, heroic DRM, Ammunition Shortage and ROF interaction. Test every new refusal through the command and projector and exercise the corresponding UI filter. Verify private facts with owner, opponent and stale-view fixtures.

Prove cancellation/staleness writes nothing, forged evidence refuses, and old serialized records round-trip and replay unchanged. Run the claim check, focused tests and wire-fire-2's own merge gate, then CI. Name every intended sweep difference against W-C9's behavior list. No unexplained replay difference is accepted. Exercise the governed shot in Codex Studio on port 6680 with the actual catalog mortar and both participant cases. Only after every site and dependency is complete may W-C9 be marked wired. The user authorizes the code merge separately.

## 7. Memory, performance and outputs

Gather equipment and participant facts once per revision-bound proposal; reuse existing map/state indexes. Keep fact records small and avoid whole-state clones, repeated JSON round trips and per-candidate full-history scans. Read heroic-use history once for the current Game Turn. Avoid unbounded caches; any derived cache is keyed by game revision and viewer and invalidated on either change. New optional evidence grows only with an actual shot, with at most two participant identities for this case. Measure allocations and elapsed time on a representative repeated-shot fixture before and after; investigate regressions rather than adding speculative caching.

Set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` for every code-builders tool. Register external build, gate and Studio outputs as created, retain proof artifacts through review, and clean disposable outputs after successful CI.

## 8. C9.2 prerequisite implementation evidence

The scoped Rules prerequisite adds nullable `OrdnanceShot.Participation`, omitted when null. Version 1 carries the real second SMC and its conditions; null keeps every adapter on its earlier reading. `CandidateRefusal` admits two distinct, available, same-side SMC in the same complete Location and rejects an overlapping director, Spotter or other weapon use. Pin, CX and Encircled conditions from either firer reach the existing resolution arithmetic. Area, Armor and Canister use the same condition adapters; the two extra shared holds were recorded on main in `b3ccdb40` before editing them.

The actual catalog 81mm mortar has a 360-degree mount. C2.3 (PDF p. 168) treats that mount as T for Case A. C2.5 on that page also exempts NT 76-82mm mortars from conditional ROF loss. These are separate facts: the actual catalog case and a synthetic NT boundary case are both tested. C5.11 (p. 172) exempts mortars from the fixed-CA restriction after earlier fire from woods. A15.23 (p. 83) supplies the heroic NT M# boundary; the existing A21.13 `Use` calculator remains responsible for phase and prior Game-Turn use. Heroic SW DRM is included for the actual participating hero, and excluded when the mortar fires as a Gun.

The new `GunTargetStatus` overload asks the mortar calculator for minimum range and effective light mode, and the existing elevation calculator for the signed height difference (C2.6, p. 168). Its original overload is unchanged. Play must pass the actual participant count and signed map read.

Validation on 2026-10-30: 18 focused Rules checks passed with warnings treated as errors, including the existing solo/director and spotted-fire readings. The shared bundle harness passed all 16 selected checks: the C9 mortar examples, six new participation examples, and bundle validity. The new examples include duplicate identities, direction versus participation, a different level, unknown prior use, and both concealed participant identities in the opponent's view. Logs are `C9-Participation-18.log` and `C9-Minigame-16.log` under the Codex gate folder. A read-only review found no blocking issue in the participation calculator, its adapters or its owner-only contract.

Follow-up integration checks: 19 focused checks passed after adding the second participant's Opportunity Fire and concealment facts. The read-only follow-up review found that Vehicle Target Type still read only the primary crew's concealment. Armor now uses the same helper as Infantry, Area and Canister; the targeted Vehicle Target Type test passed, including the unseen firer's retained concealment. A7.25 (p. 55) bars declared Opportunity Firers during PFPh and requires the participating units to qualify for the AFPh benefit. A12.14 (pp. 77-78) applies the firing action's concealment loss to both actual participants. Logs are `C9-Participation-19.log` and `C9-Vehicle-Participant-Concealment.log`.

This evidence proves the prerequisite, not completed W-C9 wiring. Participant selection, independently reconstructed records, effects on both firers, live Workbench inspection, and wire-fire-2's replay gate remain required.

## 9. PR 84 review corrections, 2026-10-31

These rulings apply to the participation reading. A null `OrdnanceShot.Participation` keeps the earlier answers on every path.

| Ruling | Reading and evidence |
|---|---|
| RC9P.1 | A15.23, PDF p. 83, gives the heroic SW To Hit benefit. A15.24's cumulative hero DRM is stated for IFT and CC attacks, not this To Hit DR. Two heroic participants in one light-mortar shot therefore supply one -1 To Hit modifier. A heroic second participant supplies it when the primary is not heroic. Neither supplies that modifier when the mortar fires as a Gun. |
| RC9P.2 | A21.13, PDF p. 88, requires the actual SMC count. `ReadingRefusal` derives `User` from the primary kind and recorded second participant, calls `ScenarioA1NonQualifiedUseCalculator.Use`, and compares its multiple. New nullable `Participation.UseHistory` records playing-side capture and prior Game Turn use. Nationality cannot establish capture. Null means the earlier evidence boundary: the state verifier checks these two facts, while the standalone calculator requires a possible Use verdict for the actual participants. It can no longer accept a two-SMC Gun shot with the lone SMC's zero multiple. The verifier must omit newly reconstructed history when the older record omitted it, but independently reconstruct and compare it when present. |
| RC9P.3 | C6.42, PDF p. 174, Bore Sights a Location, and only the ground level of a building. The existing `TargetLocationId` identifies the Location for which Play gathered `BoreSighted`; no second identity is needed. Area resolution matches each target's Location against it before applying Case M. Other levels may use their own Area Acquisition, but never borrow the selected Location's Bore Sighting. |
| RC9P.4 | C2.3 and C2.5, PDF p. 168: a 360-degree mount is T; only a turning non-vehicular NT Gun loses one ROF, with 76-82mm mortars exempt. If no ROF remains to lower, it receives an Intensive Fire counter. One `ConditionalRof` decision now serves Area, direct fire and Canister. Null Participation retains each path's former lowering and counter behavior. |
| RC9P.5 | A19.32 and A19.131, PDF p. 86, lower SW B#/X#, not every mortar's. Both adjustments use the effective light-mortar mode under Participation. A21.11, PDF p. 88, exempts captured weapons from Ammunition Shortage. New history supplies playing-side capture; older evidence keeps its recorded nationality-based shortage reading. Gun circled-B# behavior remains with D3.7. |
| RC9P.6 | A7.25, PDF p. 55: the second participant's Opportunity Fire declaration is the common fact for the PFPh refusal and AFPh benefit. Null declaration retains the older crew fact. The contract now asks for the current public phase and the owner's declaration. Primary prior weapon use remains gathered and checked at `LiveOrdnance.FromState` through `ParticipantUse` and `PrimaryUseRefusal`, including when the record verifier calls FromState. The participation use citation is A7.35, PDF p. 56, and C9.3, p. 179. |

The minigame card lists two German leaders and the catalog 81mm mortar, with g-leader and g-second stacked in [B10] and the target in [B6]. Its contract examples compare the pair's Gun reading with the lone SMC's light-mortar reading. Workbench inspection and the recorded proof belong to PR 85. Firing's aggregate concealment verdict must be applied to each concealed participant separately, never just the primary.

Memory ownership remains per shot: the new history is two booleans, retained only with that shot's immutable evidence. No new global cache or event-history collection is introduced. PR 85 measures Gun/mortar fire, Canister and target advice against main containing PR 68, with thresholds declared before measurement and two alternating pairs.

Rules validation, 2026-10-31: all 21 selected cases passed across the focused runs. The 16 c9-mortar bundle examples passed, followed by the card-validity check after correcting the manual-setup card format. The card remains a minimal card with explicit placement instructions; PR 85 supplies the actual recorded board position. `claim_check.py --base origin/main --branch codex/rules-c9-participation` reports zero faults. Its two overlap warnings are for the later rules-wiring003-adapters branch, behind PR 85 in the user's ordered chain. Changed files pass whitespace and LF/UTF-8 checks. Claude's Rules review and the PR 85 gate are still required.
