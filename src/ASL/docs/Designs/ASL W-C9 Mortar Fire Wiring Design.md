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

Keep the serialized legacy `OrdnanceShot` shape and legacy answers. Use an explicit new shot-reading version and optional participation evidence outside that frozen record. Reuse wire-fire-1's provenance plumbing where possible. New records identify the participants and the reading used; old records never acquire today's semantics by default.

`OrdnanceRecordVerifier` currently lives in `LiveOrdnance.cs` and receives the payload. Pass the version into verification explicitly if it lives in the event envelope; merely putting it on `GameEvent` cannot change a verifier which never sees it. Reader, writer, planner and projector must agree on the version and reject unsupported versions. Verification reconstructs facts from the recorded revision and compares them, then resolution asks Rules. Forged participants or missing evidence refuse.

New behavior: refuse a named To Kill vehicle target and an unknown mortar minimum; allow the 81mm Gun with its applicable CA and crew rules; allow lone-SMC use through 82mm as light with no Multiple ROF; allow two qualified participating SMC to retain ROF where Rules permits; allow the larger heroic case only with all A15.23 limits enforced. Existing games retain their recorded reading.

## 5. Viewer and command boundaries

Proposals are read-only. Confirmation rechecks revision, side, possession and the Rules verdict; cancellation, stale confirmation and refusal consume no dice and append no event. The opponent must not learn hidden caliber, minimum, participants or refusal details through choices, evidence or history. Contract facts concern `firerId`; a concealed enemy subject remains unknown. Use PR 64's `units` and `sealed` manifest conventions for opposing-view checks.

## 6. Verification and acceptance

Targeted tests cover light mortar, 81mm Gun and lone 81/82mm SMC; two SMC versus one or a director; above-82mm nonhero refusal and every heroic limit; exact minimum, below minimum and unknown minimum; Area hex with AFV versus a named vehicle; CA, captured/non-qualified use, heroic DRM, Ammunition Shortage and ROF interaction. Test every new refusal through the command and projector and exercise the corresponding UI filter. Verify private facts with owner, opponent and stale-view fixtures.

Prove cancellation/staleness writes nothing, forged evidence refuses, and old serialized records round-trip and replay unchanged. Run the claim check, focused tests and wire-fire-2's own merge gate, then CI. Name every intended sweep difference against W-C9's behavior list. No unexplained replay difference is accepted. Exercise the governed shot in Codex Studio on port 6680 with the actual catalog mortar and both participant cases. Only after every site and dependency is complete may W-C9 be marked wired. The user authorizes the code merge separately.

## 7. Memory, performance and outputs

Gather equipment and participant facts once per revision-bound proposal; reuse existing map/state indexes. Keep fact records small and avoid whole-state clones, repeated JSON round trips and per-candidate full-history scans. Read heroic-use history once for the current Game Turn. Avoid unbounded caches; any derived cache is keyed by game revision and viewer and invalidated on either change. New optional evidence grows only with an actual shot, with at most two participant identities for this case. Measure allocations and elapsed time on a representative repeated-shot fixture before and after; investigate regressions rather than adding speculative caching.

Set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` for every code-builders tool. Register external build, gate and Studio outputs as created, retain proof artifacts through review, and clean disposable outputs after successful CI. No implementation tests or CI are needed for this proposed documentation alone, per the user's instruction.
