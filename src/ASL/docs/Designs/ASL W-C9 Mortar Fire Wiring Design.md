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

### Caller implementation checkpoint, 2026-10-30

`wire-fire-2` now gathers a real second firer through the shared admission calculator, passes the actual SMC count to A21.13, records nullable participation evidence, and reconstructs it independently during replay. Both participants receive firing-use and concealment effects. The actual 81mm catalog mortar uses its solo or paired mode for CA, minimum range, ROF and target advice. Canister forwards the same participant selection. The Studio exposes the additional firer separately from direction and spotting, clears stale selections, and names both firers only in their owner's or the adjudicator's shot history.

Five focused Play checks passed, followed by three checks of the new owner-only Workbench fact source and the updated solo/paired target advice. The latter deliberately leaves unspecified target and second-firer facts unknown. Logs: `C9-Play-5.log` and `C9-Play-Workbench-3.log`. The Studio build passed with zero warnings and errors (`build-studio-c9.log`), and port 6680 now runs that build from the wire-fire-2 worktree. The saved SMOKE fire game loaded at revision 12 and retained its +2 LOS Hindrance and Final DR 13 result.

The C9 Rules prerequisite at `a4e0da10` has green CI in PR 84, run `41145018504`. A read-only follow-up confirmed the Armor concealment fix and found no remaining blocker in that prerequisite. These checks do not replace the pending live participation exercise, performance evidence or wire-fire-2 replay gate. The branch includes unmerged prerequisites: checks against main consequently report their inherited files, and must be repeated against main after those prerequisite merges; the caller-only scope is checked against the preceding fire wiring branch meanwhile.

Caller review found that visibility was gathered only for a concealed primary firer. The shared map read now gathers it when either participant is concealed, including Canister. Four focused checks passed for the seen/unseen second firer, paired Canister, and retained-ROF follow-up use and direction (`C9-Play-Boundaries-4.log`). `SupportWeaponUses` intentionally records squads only; SMC participation uses `PhaseFirers` and `GunCrewsFired`. Rules prerequisite `329c3af9` adds the shared primary prior-use refusal, so the second firer cannot evade the restriction by becoming the primary user of another weapon. Its two targeted checks passed (`C9-Prior-Use-2.log`). This check is selected only by the new participation reading; older recorded shots keep their prior admission.

### Played C9 acceptance record, 2026-10-30

Studio on port 6680 was rebuilt from `3526719f`, with zero warnings and errors. Its new C9 minigame has the actual game id `village`: two German 8-0 leaders and the catalog 81mm mortar in [B10], firing at the Russian squad in [B6]. The solo controls omitted Gun turning and Intensive Fire; selecting the second leader exposed those controls and excluded that leader from direction and spotting. Cancelling the paired proposal wrote nothing. Confirmation rolled the queued 1 and 6, turned one hexspine, applied Case A +1 and Case H +2, missed with Final DR 10 against TH# 7, retained ROF, and recorded both actual firers. The retained 11-event record is `units/minigames/c9-mortar/games/village.game.json`.

At revision 6, the live Workbench gathered both German leaders and all their participation facts and returned `may-participate`. Switching to the Russian viewer cleared the picks and hid their private facts. Rules prerequisite `329c3af9` passed CI run `41146839775`. The replay gate uses the preceding fire-wiring commit `b1f1595d` as its baseline to isolate W-C9's behavior; prerequisite merges and the final main comparison remain outstanding. Performance measurement and the replay gate remain pending at this checkpoint.

### Replay gate and CI checkpoint

CI at `3526719f` found repeated squad mortar shots incorrectly treated as inherent FP use: new participation records had entered squads in `PhaseFirers`. Commit `5b9bc79d` restricts that entry to the SMC participants and preserves the minimum-range refusal code. All 13 affected mortar checks passed (`C9-CI-Mortar-13.log`); CI run `41149365019` passed all jobs.

The wire-fire-2 gate at `5b9bc79d`, against `b1f1595d`, passed its strict build with zero warnings and errors and its claim check with zero faults. All 56,550 planner sweep lines are unchanged; the early 10,296-line comparison also matches. The only replay difference is `village`: its first seven states match, the baseline cannot read the new `participation` field at revision 8, and the current build replays all 11 events without diagnostics. The two previously known fixture diagnostics, `p71-sniper-residual` and `wire-stealth-ambush`, remain unchanged on both sides. Ten added/changed text lines describe the new participation and mortar behavior. Logs are `gate-fire2-{base,baseline,strict,build,proofs}.log` in Codex's gate folder.

The allocation probe initially measured 10,208 versus 13,080 bytes per read on `p72-hero-mortar`, revision 8, before versus after C9. Investigation found a whole participant record built only to read three prior-use flags. That read now gathers just those flags, without a cache; all nine affected `TwoSmc` checks passed. The current-side gate and stable-JIT measurement must be refreshed for this adjustment. The live opponent Workbench picker also offered no German participant, and the post-shot Play view cleared the selected second firer. Its broader Spotter/director lists still need review for an already-used second SMC.
