# ASL W-A21.13 Non-Qualified Use Wiring Design

Wiring claim `wire-fire-4` (Claude; reviewer Codex; branch `claim/wire-fire-4`), claimed on main in 99d4e444 at the user's word of 2026-10-30 ("pick up claim A21.13 after writing the next codex prompt"). It moves the callers of A21.11, A21.12, and A21.13 onto `ScenarioA1NonQualifiedUseCalculator`, which the A21.13 rule claim built (merged in PR 73, 519977ff; its design is `ASL Audit Faults Five Claims Design.md`, section 1, rulings RA21.1 to RA21.10). Implementation waits for the user's approval of this design.

## 1. The rule, in short

A21.13 (p. 88): a crew-served ordnance weapon may be fired by a non-qualified friendly MMC, or a qualified enemy MMC, with A21.11's and A21.12's penalties; by a non-qualified enemy MMC with them doubled. Two or more SMC fire it as a non-qualified MMC; a lone SMC only if heroic (A15.23), except a mortar of 82mm or less, fired as a light mortar (C9.2). The penalties of a multiple m: B# 2m lower and the Original B# an X# (A21.11, A.11); Multiple ROF m lower and red To Hit Numbers (A21.12); Case H +2m (C5.8). A hero fires a Gun over 82mm only once per Game Turn, in the MPh or DFPh (A15.23). A vehicle's manning keeps A21.22's penalty (`ScenarioA1CapturedVehicleRules.WeaponPenalty`).

## 2. What decides it today, and what each site asks instead

Line numbers are main's at 99d4e444.

| Site | Today | After |
|---|---|---|
| `Play/LiveOrdnance.cs:338` (`FromState`) | `NonQualified` from `ScenarioA1OrdnanceEligibility.NonQualified`: a non-crew on a Gun | gathers `NonQualifiedUseFacts` (section 3) and asks `ScenarioA1NonQualifiedUseCalculator.Use`; a refused use returns `(null, verdict.Refusal)`; otherwise the shot carries `UseMultiple` (section 4) |
| `Play/LiveOrdnance.cs:343` | `CapturedPenalty` for a manned vehicle | kept; under the new reading `UseMultiple` is `WeaponPenalty` for a vehicle |
| `Play/LiveFire.cs:626`, `:630` | A21.22's `WeaponPenalty` | unchanged (the row: a vehicle keeps A21.22) |
| `Play/RefusalReasons.cs:103` | `ordnance.crew-outside`'s one text for every crew refusal | the refusal of `UseRefusal` names its own rule; the old key stays for the PSK, broken, and berserk cases it also covers |
| `ScenarioA1OrdnanceCalculator.cs:1221`, ROF | `RateOfFire - RofLowering(CapturedPenalty)` | `MultipleRof(printed, UseMultiple)` when `UseMultiple` is set |
| `ScenarioA1ArmorCalculator.cs:603`, AP ROF | the same | the same |
| `ScenarioA1CanisterCalculator.cs:281`, `ScenarioA1AreaCalculator.cs:237` | no lowering | `MultipleRof(printed, UseMultiple)` when set |
| `ScenarioA1OrdnanceCalculator.cs:1511`, `CapturedColor` (and its callers, Armor `:320`, the ordnance `:877`) | red on `CapturedPenalty` | `ToHitColor(color, UseMultiple)` when set |
| `ScenarioA1OrdnanceCalculator.cs:1521` to `:1534`, `Breakdown` (callers `:1085`, Armor `:501`, Canister `:260`, Area `:400`) | `BreakdownLowering(CapturedPenalty)`; a failed shot only malfunctions | `BreakdownLowering(UseMultiple)` when set, and on a DR at or above `XNumber(gun.Breakdown, UseMultiple)` the weapon is eliminated, not malfunctioned (A.11) |
| `ScenarioA1OrdnanceCalculator.cs:1615` to `:1631`, `PassEightFirerDrm` (and Armor `:402`) | `case-h` +2 on `NonQualified`, `case-h:captured` +2 per `CapturedPenalty` | one `case-h` of `CaseHDrm(UseMultiple)` when set |
| `ScenarioA1OrdnanceCalculator.cs:474`, the mortar bar, and `:498` to `:515`, the crew check | refuses a captured Gun, an ally's crew, two SMC, a lone hero | when `UseMultiple` is set the use was asked of `Use` at gathering, and `Outside` checks only what is not A21.13's (broken, berserk, the PSK's own rule, R9.11) |
| `ScenarioA1OrdnanceCalculator.cs:60` to `:67`, the D3.7 adapter check | `Multiple(Captured, NonQualified)` against `CapturedPenalty` | against `UseMultiple` when set |
| `ScenarioA1VehicleBreakdownResolutionCalculator.cs:42` | D3.7's, held whole by the D3.7 claim; agrees today | unchanged here; the D3.7 owner forwards it to `BreakdownLowering` once that hold ends (as the row says) |
| `GamePlanner.Ordnance.cs:1069` to `:1076` | emits `InstanceEliminated` for a PSK after its shot | also for a Gun whose effect carries `Eliminated` (section 4); the planner writes the event, the calculator decided it |

Old Rules members whose callers all move are removed: `ScenarioA1OrdnanceEligibility.NonQualified` once no caller is left (the verifier's legacy path keeps one, so it stays, marked for the old reading), and `OrdnanceShot.NonQualified` stays as a recorded field.

Callers that inherit the change through `FromState`: `GamePlanner.Ordnance.cs:88` (the shot), `GamePlanner.Canister.cs:128`, `:208`, and the verifier (`LiveOrdnance.cs:840`). The projector (`GameProjector.cs:1335` to `:1365`) checks only that the recorded crew holds the Gun and decides nothing of A21.13. The Play page's `OrdnanceGuns` lists every manned Gun and filters nothing by crew, so it asks nothing; a refused shot shows the calculator's text.

## 3. The facts, and where Play reads them

- **User**: `ScenarioA1NonQualifiedUseCalculator.User(crew.Kind, smcFiring)`. `smcFiring` is 1: a Gun has one holder today. Two SMC firing together need a second participant in the ordnance request, which W-C9's design (section 3) also introduces for mortars (decision D2).
- **Captured**: possessed from an enemy side (RA21.4): the Gun's `EquipmentInstance.Side` against the firer's side. Not the nationality reading of `ScenarioA1FireEligibility.CapturedBy`, which calls an ally's Gun captured. No path captures a Gun yet (Gun Recovery is backlog, R8.6, R8.8); the fact is gathered so the rule holds when it does, and tests reach it by state.
- **Hero**: the firer's kind `asl:hero` (as `HeroWeapons` reads it, `LiveOrdnance.cs:332`).
- **Mortar** and **Caliber**: the Gun's definition (`Reference.Guns[definition]`); the mortar identity asks `ScenarioA1MortarCalculator.IsMortar` (W-C9 moves `:245` there; this claim reads the same member).
- **Phase**: `ScenarioA1OrdnanceEligibility.FirePhase(state.Phase)` (`:103`), already "MPh" and "DFPh".
- **FiredThisGameTurn** (a hero with a Gun over 82mm): no state holds it; the Player Turn's PrepFire and FinalFire counters do not, and `OrdnanceShots` clears each phase. A new GameState fact, `GunFirersThisGameTurn` (unit ids that fired an equipment Gun this Game Turn), written by the projector on `OrdnanceFired` and cleared when the Game Turn ends. It records what happened and decides nothing; Play hands `Contains(crew.Id)` to `Use`. It adds `Units/State/GameState.cs (shared)` and `Units/State/GameProjector.cs (shared)` to the claim's held files, by a claim commit before the build.

## 4. Older games keep their reading (rule 6, as amended 2026-10-30)

- `OrdnanceShot` gains `UseMultiple` (`int?`, `[JsonIgnore(Condition = WhenWritingNull)]`): null is a shot recorded before this wiring, read as today (`NonQualified`, `CapturedPenalty`); a value is the new reading with that multiple. It is set on every shot of a crew-served Gun (the multiple from `Use`) and every vehicle ordnance shot (`WeaponPenalty`); a SW shot leaves it null (RA21.8: no crew-served SW is in the catalog).
- `FromState` takes `legacyNonQualifiedUse` (false by default) as it takes `legacySpotterUse`. The verifier rejects an unknown value and rebuilds with `legacyNonQualifiedUse: recorded.UseMultiple is null`, so a record without the field rebuilds without it and the text comparison holds.
- `OrdnanceGunEffect` gains `Eliminated` (`bool?`, omitted when null), set only by the new reading's X#, so an older resolution still compares equal.
- Codex's W-C9 and wire-fire-3 designs say "no field on `OrdnanceShot`"; that was rule 6 before 2026-10-30. Under the amended rule this field is allowed, and the note to Codex in decision D1 says so.

## 5. Behavior changes

- A squad or HS on its own side's Gun: B# two lower with the Original B# as its X#, Multiple ROF one lower, red To Hit Numbers, Case H +2 (it already had the +2).
- An enemy crew on a captured Gun: the single penalty; an enemy squad or HS: the doubled one (Case H +4, B# four lower, ROF two lower). Reached only by state until Gun Recovery exists.
- A lone hero fires a Gun: the single penalty, no heroic DRM (the hero DRM already applies only to a light mortar or LATW), over 82mm once per Game Turn in the MPh or DFPh. A lone leader stays refused, with the calculator's text.
- An ally's crew mans an ally's Gun without penalty.
- A Temporary crew's (A21.22) Canister and Area ROF is lowered as its other ROF already is.
- Every shot of a Gun or vehicle recorded after this carries `UseMultiple`.

## 6. Proof and tests

- The proof store holds three games with ordnance fire (p71-sniper-to-hit, a tank's MA; p72-hero-psk and p72-hero-mortar, SWs) and no Gun manned by a non-crew; the swept dl-tractor-works games have no Gun. The gate is expected to differ in nothing but the new properties, which its skip list leaves out. A difference is named or fixed.
- Tests that assert the old reading and change: `BacklogPass8Tests.ASquadMansAGunWithCaseH` (now red, ROF one lower, B# two lower, X#); `ScenarioA1Pass8Tests.CasesEHAndMAndOverstackingChangeTheToHitDr` and `ScenarioA1OrdnanceTests`' `crew-foreign` and `squad-mans` cases keep their answers on shots without `UseMultiple`, and gain cases with it.
- New Play tests, by method: a squad mans its Gun (the four penalties and a replay); a hero with a 75mm Gun (single penalty) and with a 105mm Gun (refused the second time in a Game Turn, refused in the PFPh); a lone leader refused with A15.23's text; an ally's crew on an ally's Gun; an enemy squad on a captured Gun by state (doubled); a Temporary crew's Canister ROF; an old record without `UseMultiple` replays on its reading; an X# elimination emits `InstanceEliminated`.
- Then the Rule Workbench's `a21-non-qualified` bundle against the wired page, and the merge gate (a wiring claim runs its own).

## 7. Memory and performance

One calculator call and a set lookup per ordnance shot planned or verified; no per-hex or per-unit work. `GunFirersThisGameTurn` is bounded by the Guns on the map and cleared each Game Turn. Disclosure is unchanged: who mans a Gun and whether it is captured are known to the owner, and the shot is public once fired. No rendering path changes. The new tests build their states inline; no new test project. Outside outputs: the worktree `wt-wire4` (registered).

## 8. Decisions for the user

- **D1. Order with Codex's claims.** wire-fire-2 (W-C9) and wire-fire-3 (W-D3.7) are claimed with designs and change the same `FromState` lines, `Outside`'s crew check, `Breakdown`, and the Canister and Area ROF; neither has pushed code (both branches are at main). Proposal: wire-fire-4 builds now and merges first, adding only the `UseMultiple` branch at each site; Codex's two claims merge main after it and keep that branch where they rewrite the same lines. A note in their rows says so, and that rule 6 now allows the field.
- **D2. Two SMC firing a Gun together** need a second participant in the ordnance request, which W-C9's design builds for mortars. Proposal: W-C9 builds the participant once, and its row gains the call `User(kind, smcFiring)` with the count; this claim passes one SMC. The alternative is that this claim builds the participant and W-C9 uses it, which grows this claim into W-C9's request model.

**Approved by the user on 2026-10-30, with D1 and D2 as proposed.** The claim commit 1eafe141 added `GameState.cs` and `GameProjector.cs` (shared) and the notes on wire-fire-2 and wire-fire-3.

## 9. As built (2026-10-30)

As designed, with these additions, each found when a test of the new reading was refused by a check that read the Gun's nationality or side as its user's:

- **The Gun's nationality bar** (`ScenarioA1OrdnanceCalculator`, the `gun-outside` check): a shot carrying `UseMultiple` was admitted by `Use`, a captured Gun of another nationality included; a shot recorded before keeps the bar.
- **The friendly nationality** of a shot (`ScenarioA1OrdnanceCalculator.FriendlyNationality`): the firer's for a Gun shot carrying `UseMultiple`, the Gun's otherwise, as then. Read by the Infantry Target Type's friendly-unit check, the mortar's friendly units and vehicles, the Spotter's and the director's nationality, the armor calculator's own-vehicle bar, and the Area Target Type's enemy test.
- **The projector's Acquisition check** reads the side of the Gun's manning unit, which differs from the side that set it up only on a captured Gun.
- **A Gun set up with no side** is not captured: it records no other owner (the calculator would refuse an unknown fact).
- **A vehicle's X#** is not built here: a vehicle weapon's breakdown is D3.7's (`ScenarioA1VehicleBreakdownResolutionCalculator`); W-D3.7's row gains the note that A.11 makes a Temporary crew's Original B# its X#.
- **The `ordnance.crew-outside` text** now says what that code still covers (a broken or berserk crew, a SW's Personnel, the PSK's lone leader); the use itself is refused with the calculator's text under `play.ordnance-use`.
- **The build review** (a sub-agent, read-only) found the legacy reading byte-identical at every site and three things, all fixed:
  - A shot that reached its X# with Special Ammunition the Gun did not have was "never fired" in the projector (C8.9 read only a malfunction) while the planner marked its crew. `ScenarioA1OrdnanceProjection.NeverFired` gains the elimination, and the planner and the projector both ask it.
  - A15.23 reads "If firing a Gun > 82mm, it may fire only once per Game Turn": the hero, and only Guns over 82mm. The state records each Gun shot of the Game Turn by firer and definition (`GameState.GunShotsThisGameTurn`), and the fact handed to `Use` is whether the hero fired a Gun that `ScenarioA1NonQualifiedUseCalculator.HeroGunLimited` (new, also read by `Use`) limits. The model's summary said "the Gun"; it now says the hero.
  - A15.2: a leader that becomes heroic "retains both its leadership ... and heroic qualities", so a leader marked heroic fires a Gun as a hero.
  - Not this claim's, noted for track 3: the Studio's record text calls an eliminated Gun's shot "no more fire this phase" (`MapStudio/Services/PlayRecords.cs`); A19.131's exemption still reads the nationality (SW only, no effect on a Gun).
- **Not tested by a recorded old game of this kind:** no game in the proof store holds a Gun manned by any but its crew; the gate's replay of every older ordnance record (a tank's MA, a hero's PSK and light mortar) proves the legacy reading.

Tests: `BacklogPass8Tests.ASquadOnItsOwnGunFiresUnderTheSinglePenalty`, `APenalizedGunIsEliminatedOnItsOriginalBreakdownNumber`, `AnEnemySquadOnACapturedGunTakesTheDoubledPenalty`, `AnAlliedCrewMansItsSidesGunWithoutPenalty`, `ALoneLeaderIsRefusedAGunAndAHeroFiresItUnderTheSinglePenalty`, `AVehiclesShotCarriesItsTemporaryCrewsMultiple` (new), with the Play and Rules tests of the old reading at these sites (105 in all, passed); the strict Rules and Play builds have no warning.
