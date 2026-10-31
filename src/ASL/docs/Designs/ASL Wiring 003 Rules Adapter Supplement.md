# Wiring 003 Rules adapter supplement

Status: built on `codex/rules-wiring003-adapters`, pending review (Claude's review findings 14 to 17 and 46 to 49 settled 2026-10-31; see "Review follow-up" below). The user approved the designs and autonomous implementation on 2026-10-30. The inventory records the scoped Rules holds; the caller claims remain separate.

## Review and scope

Codex's read-only reviews checked the rulebook PDF and disclosure contracts for B25.2, A23.3, A21.13, A10.531, A8.41, A11.17, and A11.5. They found two calculator faults and two movement adapter requirements. This supplement changes Rules and focused tests only. It does not mark any wiring claim complete.

- A8.312 (PDF p. 61), A9.3 (p. 63), C2.29 (p. 168): a lone SMC cannot use Sustained Fire with IFE. Separate IFE from inherent FP in `Use`; require the owner's `loneSmc` fact for the IFE contract. Unknown owner facts must not grant an attack.
- A11.5 (PDF p. 74): the new vehicle CC resolver must read the attacker's actual definition before `AttackerRefusal`. Keep the null-context resolver's recorded answer. The supported kind predicate is shared with callers so list filtering and commands use the same rule.
- A23.3 (PDF p. 90), A8.1 (p. 59): expose the settlement boundary and permanent failure predicates to the projector. A new Cave-aware Placement refusal overload refuses the unsupported G11.8331 target. The old entry point keeps its prior answer. A23.4's deadline adapter forwards through the detonation window and preserves overdue obligations.
- A10.531 (PDF p. 67), A12.14 (p. 78): the advance contract must require smoke and cover facts when the answer depends on them. That change must be coordinated with the viewer-safe gatherer in wire-movement-1; it is not yet implemented by this supplement.

The fire review also found a caller requirement: a burning wreck created after Fire Lane placement must affect the later residual attack. Wire-fire-5 must gather the current hindrance, avoid double counting stored hindrance, and preserve old recorded attacks. The B25.2 contract's A9.222 citation says p. 60 rather than p. 63; its correction remains with the claim's review follow-up.

## Replay and merge order

The close-combat wiring review found one further correction in A20.52 (PDF p. 87): every Guard halves its attack FP against non-prisoners in CC. The separate US# restriction is not a condition of that halving. The CCV calculator represents that penalty by one -1 CCV modifier. Additive `CcvAttackFacts.Guard` and nullable, omitted-when-null `CcvContext.Guards` supply it without changing the old constructor or the old outnumbered-only inputs. Either fact applies the penalty once. The contract accepts the old optional `guardOutnumbered` fact and the new optional `guard` fact. It evaluates when `guard` is known, or when `guardOutnumbered` is true (the penalty applies either way); `guardOutnumbered: false` with `guard` unknown is not evaluated, since "not outnumbered" does not show that the attacker is no Guard (review item 15). New minigame checks use `guard`. Both a main attacker and a combining SMC may supply the Guard fact. The callers still wait for wire-close-combat-1.

No recorded record changes in this supplement. Existing rule entry points keep their answers except the two unwired new calculator paths above. The DC deadline forward is algebraically equal to its previous body.

Wire-fire-4 merges before wire-fire-2 and wire-fire-3. Each merges main afterward and preserves the nullable `OrdnanceShot.UseMultiple` branch. W-C9 alone adds the second SMC participant and passes the count to `ScenarioA1NonQualifiedUseCalculator.User`. The user settled rule 6: new recorded properties are nullable, omitted when null, and null retains the old reading.

## Verification

Focused methods cover the lone-SMC IFE verdict and unknown owner fact, the new-context resolver refusing a vehicle as an Infantry attacker, DC settlement and failure boundaries, Cave refusal, and the unchanged overdue deadline. On 2026-10-30 the initial focused run passed all 24 cases, including permitted IFE and empty-context CC regression checks. A follow-up build with warnings as errors and the IFE and existing placed-DC deadline methods passed all 8 cases.

The Studio build passed with zero warnings and errors. Before unit tests, the Rule Workbench at port 6680 showed First Fire IFE at range 1 in DFPh refused for a lone SMC, permitted once more when not a lone SMC, and not evaluated when that fact was unknown. No caller behavior has changed, so this Rules-only supplement uses its focused tests and PR CI; each later wiring claim runs its own replay and sweep gate.

The Guard follow-up also built Studio with zero warnings and errors. The Workbench minigame `wire-guard-review-2` showed CCV 4 and immobilization on Final DR 4 with `guard = true`, CCV 5 and elimination with `guard = false`, and not-evaluated when both Guard inputs were unknown. Facts were entered by hand on the manufactured A11.5 position. The Studio was stopped afterward. Five focused Rules methods passed with warnings as errors, including the existing empty-context comparison, old contract input, new Guard input, a combining SMC, one penalty when both inputs apply, and omission of null Guards. All eight vehicle CCV bundle checks passed. The independent read-only review found no further correctness or compatibility issue.

## Review follow-up (2026-10-31)

Claude's reviews of this branch (items 14 to 17 of the combined prompt, 46 to 49 of the addendum) are settled here. None changes a recorded game's answer: every changed path is a Rules member no Play or projector caller asks yet, or one behind a nullable recorded fact.

- Item 14, A25.01 (p. 93): `AssaultEngineerLineage` takes each parent's printed trait (`parentPrintedTraits`, optional). A parent with no recorded status is AE when its printed trait is, so its lower-quality Replacement is recorded as lost "even if it subsequently Battle Hardens". Without the list the answer is the earlier one.
- Item 15, A20.52 (p. 87): the A11.5 contract evaluates only when `guard` is known or `guardOutnumbered` is true. `guardOutnumbered: false` with `guard` unknown is not evaluated.
- Item 16: A25.01 and A20.52 join the A11.5 claim in the inventory's section 6; their work is in the claim's held files, so no file was added. Both rows read Owner "A11.5 (Codex)" and state "built".
- Item 17: backlog section 89 (DC Placement into a Cave, G11.8331; mixed Assault Engineer recombination); two IFE checks in the `a8-final-fire` bundle (a lone SMC refused, a crew once more); the status line above and the `CcvAttackFacts` summary (`Guard` is the full reading).
- Item 46, D3.5 (p. 200): `ScenarioA1FinalFireCalculator.VehicleFacts` takes the vehicle's definition, and an MA MG sustains only with a printed Multiple ROF. With none it is a vehicular MG that is not the MA: no Final Fire, and `FireSpent` reads it as spent.
- Item 47, A6.7 (p. 54), D9.4 (p. 210): `FireLaneWreckFacts` has a required `TargetRange`, the range to the attacked Location. A wreck at range 0, at that range, or beyond it is refused. The record is new on this branch, so no recorded shape changes.
- Item 48: tests for `vehicle-breakdown-facts-missing`, `final-fire-marker-unknown`, `VehicleMarker`'s fallback, a lone leader or hero's First-Fire MG, a First-Fire light mortar in the DFPh, and the omission of `FinalFireVersion` and `FinalFireMarker` when null. A vehicle Gun's Intensive Fire with the version null was already asserted by `CurrentVehicleGunMayIntensiveFinalFireWithItsOwnBreakdownFacts`.
- Item 49: under version 1 an MA that fired this Player Turn, kept no ROF, and has no counter is refused (`final-fire-marker-contradicts`); W-A8.41 lists the two sites that still read the unit's counter; a comment says the constant `WreckBlazeLos` call is there to cite B25.2.

## Memory and performance

The A25.01 lineage adapter distinguishes lower-quality Replacement from Battle Hardening and an underscored squad becoming its own HS (PDF p. 93). Fire's ordered consequence events retain the needed distinction, including a casualty-plus-ELR result after earlier Battle Hardening. The projected status is true for inherited AE, false for permanent loss, and null for the printed reading. Ordinary non-AE replacement does not create an AE-loss history. A nullable recorded lineage marker leaves older records on their former reading; the new-event factory supplies false for ordinary new lineage, and the specific Rules verdict supplies true for lower-quality replacement. Mixed AE/non-AE recombination is explicitly not evaluated pending a ruling; A1.32 (PDF p. 45) does not settle that mixture. Nine focused cases passed with warnings as errors. Independent review identified the ordinary non-AE case, which is corrected and covered, and confirmed the event distinctions and source-trait gathering requirement.

The added decisions read a few scalar facts. There is no state scan, cache, retention, new project, or rendering change. Existing state ownership, disclosure, map versions, and incremental counter rendering are unchanged. Tests reuse the existing static reference package and add no repeated expensive manifest generation. No performance improvement is claimed.

## Build outputs outside the repository

Codex's Studio build is `E:/Archive/GitHub/dlandi/asl-gate-work-codex/studio-6680`, registered before the build. Filtered test outputs use the registered `test-classes-bin` folder under the same Codex root. Studio uses port 6680. Outputs are removed after their final use and CI acceptance; the last implementation task checks the registry and removes only this work's outputs.

## Remaining work

The separate A11.17 adapter branch adds explicit `stealthy:<side>` and `lax:<side>` SSR token parsing and validation in Rules, including an optional check against the scenario's actual sides. Since the review of PR 79 (2026-10-31, ruling RA11S.11) a token may also name an OB group, `stealthy:<side>:<group>` and `lax:<side>:<group>`, and `SsrFlags(rules, side, group)` is the one fold of the tokens into a unit's two flags; `SsrToken` still reads only the side-wide form. A Commando or Raider designation makes only its squads and HS Stealthy (H1.24, G17.111; ruling RA11S.2 amended), and the contract's designation choices are the calculator's `DesignationTokens`. Explicit OB designations use `anzac`, `gurkha`, `commando`, `partisan`, or `raider`; absence is None, and an unknown token is not silently treated as ordinary troops. Five focused cases passed with warnings as errors: malformed setup tokens, unknown sides, Lax taking precedence, and an explicit ANZAC designation versus unsupported prose. Existing Stealth verdicts and contracts are unchanged.

The strict concealment adapter refuses unknown Stealth only when an eligible unit needs a concealment dr. Automatic and night gains keep their answers. The old concealment entry point and NightAndWeather.RulesBar retain their previous reading; new scenario setup calls the new SSR validator explicitly. FactsRefusal supplies a generic refusal for unknown contributing search or casualty facts without disclosing unit identities. Eight focused cases passed with warnings as errors, including the old concealment comparison and the strict adapter's automatic-gain boundaries. Independent read-only review found no further issue in the strict adapter.

The three Wiring 003 caller claims and their played minigames, gates, reviews, and PR CI remain pending. The contract fact requirement above remains pending with its gatherer. Rule claims with findings are not marked reviewed until those findings are corrected and verified.
