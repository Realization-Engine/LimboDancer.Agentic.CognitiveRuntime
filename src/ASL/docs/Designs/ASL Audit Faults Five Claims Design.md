# ASL Audit Faults Five Claims Design

**Codex rule review, 2026-10-30:** A21.13's calculator and owner-scoped contract have no blocking finding against A21.11-.13 (PDF p. 88), A.11 (p. 43), C5.8 (p. 173), and A15.23 (p. 83). Capture and non-qualified use compose once through the penalty multiple; the lone-SMC mortar exception and the larger heroic Gun limits are explicit. Actual participant validation and the NT CA limit remain the W-C9 authorization dependency, not an authorization supplied by this contract. Its dependent private facts name the owner-selected firer; no opponent disclosure shortcut was found. The ledger retains the already-wired state, and this review does not change Claude's W-A21.13 row. The other four claims' adapter findings remain tracked in the Wiring 003 supplement and implementation PRs.

**Status:** Built, 2026-10-30 (section 9), after a sub-agent's review of the design (section 8). The user's word (2026-10-29): "Pick five claims and implement them in one pass, then prepare the wiring prompt pass for Codex", and "Fix all newly found faults in this pass. Do not defer them." Five rule claims (Claude), each reviewed by a sub-agent, on one branch, `claim/audit-five`. They take the first five sections of the audit-fault queue (the coverage document's section 8.3a; the memory note's order): A21.13, A10.531, A8.41, A11.17, and A11.5. Rules only: no caller changes, so no recorded game replays differently. Each fault the research found, beyond the survey's, is fixed in its claim's calculator and listed in its wiring row; none goes to the backlog. Rulings are numbered by claim: RA21.n, RA10.n, RA8.n, RA11S.n (Stealth), RA11C.n (the CCV).

## 0. How the five claims fix faults that are wrong in play

A rule claim changes no caller (AGENTS.md, step 2), and an old Rules member keeps its answer unless the new one is equal on every input (the Rules conventions, section 2, step 5). So in each claim:

- The new calculator gives the rule's answer, every fault included, and its tests prove it.
- Each old member that gives the same answer on every input forwards to it.
- Each old member whose answer is wrong keeps it, and its summary names the calculator member that replaces it and the difference.
- The wiring row `W-<claim>` lists every site that must move, in Play, the projector, the Studio, and the Rules sites that kept their answers, with the behavior change each brings. Codex's wiring claims move them, with older recorded games kept on their recorded reading, and run the merge gate.
- No recorded fact record gains a field (the conventions, section 1, rule 6: `FireAttack`, `RallyAttempt`, `CloseCombatFacts`, `VehicleCloseCombatFacts`, `AmbushFacts`, `OrdnanceShot`). Where a resolving member must take a new fact, the claim adds an overload with a context argument, and the old overload forwards to it with null, the same answer. Records that are not recorded (`ConcealmentCandidateFacts`, `SearcherFacts`, `MopUpDefenderFacts`) may gain a nullable init property that is null by default.

So a fault is fixed in Rules by this pass, and fixed in play when Codex wires it. The wiring prompt (section 7) carries each one.

**How the wiring keeps older games on their reading** (the review's finding 2). The code's practice since pass 45 is a nullable version property on the recorded record, omitted from the JSON when null, null meaning the reading before: `FireAttack.CoweringMarksAndWeaponResidual` (pass 45), `OrdnanceShot.SpotterUseVersion` (Codex's W-C9.3 wiring, 2026-10-27), and pass 70's and 72's fields on `AmbushFacts` and `OrdnanceShot`. A record written before such a field serializes exactly as it did, so the verifiers' text comparison still holds. Rule 6's text (pass 32, 2026-10-05) forbade any added field; these claims add none, and each wiring row names this mechanism as the guard. The user approved rule 6's new wording, which states this practice, on 2026-10-30 (section 8).

## 1. Claim A21.13: Non-Qualified Use (rows A21.11, A21.12, A21.13)

### 1.1 The rule (p. 88; A.11 p. 43; C5.8 p. 173 and the C5 chart; C2.1 p. 167; D5.1 p. 203; A15.23 p. 83; A21.22 pp. 88 and 89; C9.2)

- A21.13: an ordnance weapon that needs a crew may be fired by a non-qualified friendly MMC, or a qualified enemy MMC, with the A21.11 and A21.12 penalties; by a non-qualified enemy MMC with those penalties doubled. Two or more SMC fire it as a non-qualified MMC. A lone SMC may fire it only if heroic (A15.23), except a mortar of 82mm or less, which he fires as a light mortar (C9.2).
- The penalties of a multiple m: B# (and X#) 2m lower (A21.11); Multiple ROF m lower (A21.12, the C3 chart's "Lowered ROF"); Case H +2m (C5.8; "+4 if both apply"); red To Hit Numbers for any m of 1 or more (A21.12); and the Original B# becomes an X# for that use (A.11, which names "Captured Weapons, unqualified crews [21.13]").
- Qualified: a crew counter, an infantry crew or a vehicle crew (D5.1), of a side to which the weapon is friendly.
- A15.23: a hero fires a Gun of 82mm or less "as if it were captured and being fired by an enemy crew" (the single penalty); a Gun over 82mm only once per Game Turn, in the MPh or DFPh.
- A21.11's Ammunition Shortage exemption names captured weapons only.
- What each side knows: the Gun and its manning unit may be hidden or concealed; who mans it, the Gun's caliber, and whether it is captured are the owner's until it fires. The shot itself is public.

### 1.2 What decides it today

Inside Rules:
- `ScenarioA1OrdnanceEligibility.NonQualified` (`:394`) **decides** who is non-qualified: equipment manned by a non-crew, not a SW. Right.
- `ScenarioA1OrdnanceCalculator.Outside`, its crew check (`:498` to `:510`, and `:474`) **decides** who may man a Gun: refuses a captured Gun, an ally's crew, two SMC, and a lone hero (ruling R8.8, simplifications).
- `PassEightFirerDrm` (`:1601` to `:1615`) **decides** Case H: +2 when `NonQualified`, +2 per unit of `CapturedPenalty`. Right on its inputs.
- **The survey's fault:** the ROF (`:1214`), `CapturedColor` (`:1500`), and `Breakdown` (`:1507` to `:1520`) read `CapturedPenalty` only, so a squad or HS on its own side's Gun keeps its full ROF, its normal To Hit color, and its full B#.
- **Found by the research, the same fault:** `ScenarioA1ArmorCalculator.cs:601`, the AP ROF.
- **Found by the research, a second fault:** `ScenarioA1CanisterCalculator.cs:279` and `ScenarioA1AreaCalculator.cs:235` lower the ROF for no penalty at all.
- **Found by the research, a third fault:** no Gun shot turns the Original B# into an X# under any penalty (A.11); a failed shot only malfunctions.
- `ScenarioA1FireCalculator.cs:3147`, `:3176`, `:3193`, and `:4393`, `:4425`, `:4468`: a Temporary crew's vehicle MG and a captured SW, B# and ROF lowered. Right.
- `ScenarioA1CapturedVehicleRules.WeaponPenalty` (`:155`): A21.22's manning, right for every case `Manning` gives (A21.22's row). `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers` (D3.7, held whole) agrees. `ScenarioA1MortarCalculator.FiresAsLight` (C9) is right and is asked, not changed.

Outside Rules: `Play/LiveOrdnance.cs:339`, `:344` and `Play/LiveFire.cs:626`, `:630` gather `NonQualified` and `CapturedPenalty` (by kind and nationality, never "captured from an enemy side"); `Play/RefusalReasons.cs:103` the crew refusal's text.

### 1.3 Rulings

- **RA21.1 One calculator for the penalties.** `ScenarioA1NonQualifiedUseCalculator` decides who may fire a crew-served weapon, the multiple, and the arithmetic of a multiple. The claim takes A21.11 and A21.12 with A21.13, because A21.13 only multiplies their penalties and the arithmetic is decided today in three calculators.
- **RA21.2 Who may fire and the multiple.** `Qualified(kind)`: `asl:crew` or `asl:vehicle-crew` (C2.1, D5.1). `User(kind, smcFiring)`: crew, MMC, SMC group (two or more), lone SMC. `PenaltyMultiple(user, captured, hero)`: crew 0 or 1 if captured; MMC or SMC group 1 or 2 if captured; a lone hero 1. `UseRefusal(facts)`: a lone SMC not a hero is refused unless a mortar of 82mm or less, which fires as light (asks `ScenarioA1MortarCalculator.FiresAsLight`); unknown facts refuse. `Use(facts)`: the refusal, the multiple, fires-as-light, and A15.23's once-per-Game-Turn limit for a hero with a Gun over 82mm.
- **RA21.3 A hero with a captured Gun** takes the single penalty, by A15.23's wording ("as if ... captured and being fired by an enemy crew") and A21.22's SMC case.
- **RA21.4 Captured means possessed from an enemy side** (A21.1) for this calculator: a fact the caller gives. An ally's crew is qualified on an ally's Gun; an ally's squad is a non-qualified friendly MMC. The SW reading of ruling R13.7 is not this claim's.
- **RA21.5 The arithmetic.** `BreakdownLowering(m)` = 2m; `XNumber(originalB, m)` = the Original B# when m > 0, else none (A.11); `RofLowering(m)` = m; `MultipleRof(printed, m)` = max(0, printed - m); `ToHitColor(color, m)` = red when m > 0; `CaseHDrm(m)` = 2m.
- **RA21.6 Ammunition Shortage.** A non-qualified friendly user keeps its side's Ammunition Shortage; only a captured weapon is exempt (A21.11). No change to `DepletionShortage`.
- **RA21.7 Old members.** Forwarding, equal: `NonQualified`; `PassEightFirerDrm`'s two Case H values (`CaseHDrm(1)`, `CaseHDrm(CapturedPenalty)`); `CapturedColor` (`ToHitColor` of `CapturedPenalty`); `Breakdown`'s penalty term (`BreakdownLowering` of `CapturedPenalty`); the ROF at `:1214` and the armor `:601` (`RateOfFire - RofLowering(CapturedPenalty)`, the subtraction kept, no floor, as today); the fire calculator's vehicle MG and captured SW sites (`RofLowering`, `BreakdownLowering`). Keeping their answers, named: `Outside`'s crew check (refuses what `UseRefusal` admits); the Canister and Area ROF (no lowering); every site that reads `CapturedPenalty` alone (a `NonQualified` shot is not lowered there, and no shot turns its B# into an X#).
- **RA21.8 Crew-served SW** (RCL, MOL-P): none is in the catalog, and `NonQualified` refuses every SW; the calculator takes no SW case. A note on the row.
- **RA21.9 A15.23 in full.** A hero firing a crew-served Gun takes no heroic DRM on that shot, and a Gun over 82mm only once per Game Turn and only in the MPh or DFPh: `Use` returns both limits (`HeroDrmApplies` false; the phases allowed).
- **RA21.10 The other copies of the arithmetic.** The ordnance calculator's D3.7 adapter check (`:63`, `:64`) and the fire calculator's `:94` forward to `BreakdownLowering` and `RofLowering`, equal. `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers` (`:42`) is D3.7's, held whole by that built claim: it agrees, and it stays; W-A21.13 names it for the D3.7 owner to forward once the hold ends.

### 1.4 Contract `a21.13-crew-served-use`

"May this unit fire this crew-served ordnance weapon, and under what penalty?" (A21.11, A21.12, A21.13 p. 88; A.11 p. 43; C5.8 p. 173; A15.23 p. 83.) Facts: `firerId` (Unit, Owner, concerning itself); `user` (Choice crew, mmc, smc-group, lone-smc), `hero`, `captured`, `mortar` (Flags), `caliber` (Number): Owner, concerning `firerId`. Outcomes: `no-penalty`, `penalized`, `penalized-doubled`, `fires-as-light`, `refused`, `not-evaluated`. Place: every ordnance shot, from the PFPh (2.22A) on, the MPh's and DFPh's included.

### 1.5 Wiring row W-A21.13 (fire)

- `Play/LiveOrdnance.cs:339`, `:344`: gather the user kind, the SMC count, the hero, and captured by side; ask `Use`; refused shots carry `UseRefusal`'s text (`Play/RefusalReasons.cs:103`).
- Rules sites that keep their answers, moved behind a recorded-reading guard: the ordnance ROF `:1214`, `CapturedColor` `:1500`, `Breakdown` `:1507` with A.11's X# on a failed shot, `PassEightFirerDrm` (one Case H of `CaseHDrm(m)`), the armor ROF `:601`, the Canister ROF `:279`, the Area ROF `:235`, `Outside`'s crew check `:498` to `:510` and its mortar bar `:474` (ask `UseRefusal`). The multiple reaches them through a version property on `OrdnanceShot` (section 0).
- `ScenarioA1VehicleBreakdownResolutionCalculator.Numbers` (`:42`, D3.7's): forward to `BreakdownLowering` once D3.7's hold ends.
- `Play/LiveFire.cs:626`, `:630`: a vehicle keeps A21.22's `WeaponPenalty`.
- A captured Gun is reachable only once Gun Recovery exists (backlog, R8.6, R8.8).
- Behavior changes: a squad or HS on its own side's Gun fires with its B# two lower and the Original B# as its X#, its Multiple ROF one lower, and the red To Hit Numbers; an enemy crew on a captured Gun takes the single penalty, an enemy squad or HS the doubled one (Case H +4); two SMC fire a Gun as a non-qualified MMC; a lone hero fires a Gun under A15.23; a Temporary crew's Canister and Area ROF is lowered; an ally's crew mans an ally's Gun.

### 1.6 Tests and minigame

`ScenarioA1NonQualifiedUseCalculatorTests`: `Qualified` by kind; every cell of the user and captured table (multiple 0, 1, 2); a lone leader refused; a lone SMC with an 81mm mortar fires as light; a lone hero over 82mm marked once per Game Turn; the arithmetic at m = 0, 1, 2 (B# 0, 2, 4 lower; X#; ROF floor at 0; red; Case H 0, 2, 4); each forward equal to its old answer over `CapturedPenalty` null, 1, 2 with `NonQualified` both ways. The contract's outcomes and the opponent's not-evaluated. Bundle `a21-non-qualified` on bd03 (c9-mortar's board): a German Gun manned by its crew, by a squad, a captured Russian Gun manned by a German crew and by a HS, two leaders, a lone leader, a hero, and a Russian-viewer check.

## 2. Claim A10.531: Open Ground (row A10.531)

### 2.1 The rule (p. 67; A4.6 p. 51; A4.132 p. 48; A4.63 p. 51; A12.14 pp. 77 and 78 and Concealment Table Case B; B1.1 to B1.17, B10.31, B12.3, B14.3, B15.3, B15.6, B16.3)

- A10.531: for rout determination, Dash, concealment, and Interdiction, a hex is Open Ground when **the particular enemy unit(s)** could apply the -1 FFMO DRM in a hypothetical Defensive First Fire. Its example: grain, brush, or orchard is not Open Ground; a shellhole, entrenchment, or pillbox is not unless entered at the minimum MF; SMOKE or a LOS Hindrance in the hex or between it and the enemy, a hexside TEM, Height Advantage (with the Crest Line exception), or a wreck or AFV in the hex make it not Open Ground to that enemy.
- FFMO (A4.6) does not apply with another protective TEM or a LOS Hindrance (SMOKE, an AFV, a wreck) between target and firer or in the target Location; Low Visibility never cancels it. A road-rate mover in a hex of other terrain takes FFMO when the firer's LOS reaches it without crossing that terrain (A4.132; B13.31).
- Out-of-season grain (outside June to September) is Open Ground (B15.6).
- A12.14 and Case B read "Open Ground hex (as defined in 10.531)", for a concealed unit in LOS of a Good Order enemy ground unit within 16 hexes.
- What each side knows: the Location, its terrain, SMOKE, a wreck, and the month are public; whether it is Open Ground to a particular enemy depends on that unit's Location and LOS, which are its owner's while it is hidden or concealed.

### 2.2 What decides it today

- `ScenarioA1RoutCalculator.OpenGround` (`:124`) and `CouldApplyFfmo` (`:141` to `:155`) **decide** the terrain part and the per-enemy cover. Right (R35.1).
- `ScenarioA1Dash.OpenGroundTo` (`:109` to `:113`) **decides** the per-firer cover a second time, the same answer.
- **The survey's fault:** `ScenarioA1MovementCalculator.Unmasked` (`:850`) and `ScenarioA1AdvanceCalculator.LosesConcealment` (`:144` to `:148`) test `terrain == "open-ground"` for any Good Order seer within 16: out-of-season grain keeps the "?", and SMOKE in the hex loses it.
- **Found by the research, wider:** a wreck or AFV in the hex should keep the "?" (A4.6: in the target Location), and a road-rate mover in a woods-road (or orchard-road, B14.6) hex reads `woods` and keeps its "?" where A4.132 gives FFMO to a seer along the road. (A Hindrance, a hexside TEM, or Height Advantage along the LOS does not keep it: RA10.2.)
- **Found by the review:** `ScenarioA1FireMapRules.GrainTerrain` (`:30` to `:44`) decides the grain season a second time (it forwards to `OpenGroundTerrain`, equal); `Play/GamePlanner.InfantryOverrun.cs:154` reads out-of-season grain as grain for A4.151's Open Ground (the wiring row).
- **Found by the research:** Dash's terrain part in Play (`Play/GamePlanner.Fire.cs:1969` to `:1975`) tests `== "open-ground"`, so out-of-season grain is not Open Ground there.
- **Found by the research:** the proving slice's contract `a12.14-advance-concealment` (`ScenarioA1RuleContracts.cs:340` to `:376`) asks the old member with a `terrain` fact, so its verdict carries the fault.
- The fire calculator's FFMO (`:3770`, `:3924`) is A4.6's own predicate and is right; it stays (RA10.4).

### 2.3 Rulings

- **RA10.1 One calculator.** `ScenarioA1OpenGroundCalculator`: `OpenGroundTerrain(terrainKey, month)` (true for open ground and roads, grain outside June to September, null for grain with no month, false otherwise); `NoFfmoCover(cover, losHindrance)`; `OpenGroundTo(location, view)` per enemy unit, with the location's SMOKE, a protected entry (a fact, false everywhere today), and the view's LOS, Hindrance, hexside TEM, Height Advantage, in-hex cover, and the terrain as that unit sees it for a road-rate mover; `OpenGroundToAny(views)`: true if any is, null if none is and one is unknown.
- **RA10.2 A12.14's Hindrance sentence (p. 78), revised after the review.** "The presence of a LOS Hindrance or beneficial defensive DRM (such as TEM and CA change DRM) along the LOS to a concealed unit does not prevent loss of its concealment." A12.14 is the specific rule for concealment, so for its Open Ground cause the hex is judged by what is in it: its terrain and season, SMOKE there, a wreck or AFV there, a protected entry, and, for a road-rate mover in a hex of other terrain, whether the seer's LOS reaches it along the road (A4.132, B13.31, B14.6). A Hindrance, a hexside TEM, or Height Advantage between the seer and the hex does not keep the "?" (all three are along the LOS or a beneficial defensive DRM). The rout, Dash, and Interdiction keep A10.531's full per-seer test. `OpenGroundTo` takes the purpose (`OpenGroundPurpose.Concealment` or the others). A ruling open to the user's veto; the reviewer would count Height Advantage for concealment too.
- **RA10.3 The seer's fire.** The definition judges the view only (terrain, LOS, cover); each reader keeps its own unit filter (A12.14 a Good Order ground unit within 16; A10.5 a Known enemy in Normal Range; A10.53 an unbroken unit with FP in Normal Range, no Hindrance of any kind).
- **RA10.4 FFMO itself stays A4.6's.** The fire calculator's FFMO predicate is the same cover test in A4.6's shape; its comment names `NoFfmoCover`, and an A4.6 claim decides whether it forwards.
- **RA10.5 Overloads for A12.14's members.** `Unmasked` and `LosesConcealment` gain overloads that take A10.531's answer (`OpenGroundToAny` over the seers) as a fact, their other logic the old members' word for word. The old members keep their answers, their summaries naming the overloads and the differences.
- **RA10.6 The proving slice's contract is fixed.** `a12.14-advance-concealment` keeps its `terrain` fact and its seer flag, and gains `month`, `smokeInLocation`, and `inLocationCover` (All), so the owner still settles a woods advance alone; it asks the new `LosesConcealment` overload with A10.531's concealment reading. The bundle `a12-advance-concealment` gains checks for out-of-season grain and SMOKE. The A12.14 row stays the proving slice's (Claude).
- **RA10.7 Grain with no month** is not-evaluated in the contract; the rout keeps "not Open Ground" (its old member reads `== true`); the wiring of A12.14 and Dash treats it as unknown, as fire refuses. Refined after the review of PR 77 (item 22): in the a12.14 contract it is not-evaluated unless the seer flag settles it; with `enemyGoodOrderInLosWithin16` false no Good Order enemy sees the Location (A12.14 Case B, p. 77), the season cannot matter, and the verdict is keeps-concealment. For A4.151's move away (p. 48) grain is Accessible in every month, so no month never bars a destination; only its Open Ground fact is unknown, and `EvadeDestinationsBySeason` refuses only when that fact changes the choice (B15.6, p. 129).
- **RA10.8 Protected entries and other terrain.** Shellholes, entrenchments, pillboxes, Emplaced Guns, frozen marsh, gully, stream, and valley do not exist on the maps; `ProtectedEntry` is a fact (false), and the terrains are their own rows.
- **RA10.9 Old members.** Forwarding, equal: `RoutCalculator.OpenGround`, `CouldApplyFfmo`'s cover test, `ScenarioA1Dash.OpenGroundTo`, `ScenarioA1FireMapRules.GrainTerrain`'s season test. Keeping their answers, named: `Unmasked`, `LosesConcealment`.

### 2.4 Contract `a10.531-open-ground`

"Is this Location Open Ground to this enemy unit?" (A10.531 p. 67; A4.6 p. 51; A12.14 p. 78; B15.6 p. 129.) Facts: `purpose` (Choice rout, dash, interdiction, concealment), `at` (Hex), `terrain`, `month` (may be unknown), `smokeInLocation`, `protectedEntry`, `inLocationCover`, `roadRate`: All. `viewerId` (Unit, Owner, concerning itself); `losClear`, `losHindrance`, `hexsideTem`, `heightAdvantage`, `seesAlongRoad` (a road-rate mover: the LOS reaches it without crossing the hex's other terrain): Owner, concerning `viewerId`. Outcomes: `open-ground`, `not-open-ground`, `not-evaluated`. Public facts settle "not Open Ground" alone for woods, brush, orchard, SMOKE, a wreck, or a protected entry.

### 2.5 Wiring row W-A10.531 (movement)

- `Play/GamePlanner.Movement.cs:550` (`Unmask`): for each Good Order enemy ground unit within 16 with a clear LOS, gather its view (cover by `InterdictionCover`, terrain by `RoadRateTargetTerrain` for a road-rate mover) and call the new `Unmasked` overload with `OpenGroundToAny`.
- `Play/GamePlanner.CloseCombat.cs:260` (`PlanAdvanceUnits`): the same, the new `LosesConcealment` overload.
- `Play/GamePlanner.Fire.cs:1969` to `:1975` (Dash): ask `OpenGroundTo` with `OpenGroundTerrain`.
- `Play/GamePlanner.Rout.cs` `:49`, `:433`, `:440`, `:1020`: no change needed.
- A12.14's other readers of the definition: withdrawal from Melee (`GamePlanner.Withdrawal.cs`) costs no "?" today; MF expended in Open Ground leaving a foxhole (no foxholes).
- Old members to remove once callers move: `Unmasked(..., string terrain, ...)`, `LosesConcealment(bool, string, Func<bool>)`, `ScenarioA1Dash.OpenGroundTo`.
- `Play/GamePlanner.InfantryOverrun.cs:154`: A4.151's "Open Ground hex" reads out-of-season grain as grain; ask `OpenGroundTerrain`.
- Behavior changes: a concealed mover or advancer loses "?" entering out-of-season grain; keeps it entering Open Ground with SMOKE or a wreck or AFV there; loses it as a road-rate mover in a woods-road or orchard-road hex seen along the road; Dash no longer qualifies against a firer to whom out-of-season grain is the Location left or reached; an Infantry Overrun's evader treats out-of-season grain as Open Ground.

### 2.6 Tests and minigame

`ScenarioA1OpenGroundCalculatorTests`: `OpenGroundTerrain` over the terrain keys and months 5, 6, 9, 10, none; `OpenGroundTo` with SMOKE, a protected entry, no LOS, a Hindrance, a wall TEM, Height Advantage, a wreck, a road-rate mover's terrain; `OpenGroundToAny`; the forwards equal over every terrain key, every month, SMOKE both, and a grid of cover facts; the overloads equal to the old members when the Open Ground fact equals the old test. The contract's outcomes. Bundle `a10-open-ground` on bd04: open ground, with SMOKE, grain in October and July and with no month, an orchard, a hex across a hedge and beside it, a hidden viewer. The `a12-advance-concealment` bundle's checks updated to the new fact.

## 3. Claim A8.41: Final Fire by a marked weapon (rows A8.4, A8.41, A9.3, C2.241)

### 3.1 The rule (A8.4, A8.41 p. 61; A9.2 p. 62; A9.3 p. 63; C2.24, C2.241 pp. 167 and 168; C2.29; C5.6 p. 172; D3.5 p. 200; A.11 p. 43)

- A8.4: in the DFPh a unit or weapon not marked First, Final, Intensive, or No Fire may fire; one marked First Fire may fire again, flipping to Final Fire, only as Area Fire and only at an ADJACENT or same-hex target; one marked Final Fire may not.
- A8.41: a Multiple-ROF weapon without such a counter (even if it fired in First Fire and kept its ROF) keeps its attacks in Final Fire at any target; a weapon marked First Fire that can use Intensive Fire (C5.6) or Sustained Fire (A9.3) may use it for one more attack, only at an ADJACENT or same-hex target. **The limit follows each weapon's own counter, and each unit's own counter for its inherent FP.**
- A9.3: Sustained Fire (Subsequent First Fire, FPF, or the DFPh while marked First Fire): B# two lower and the Original B# as an X# (A.11), Area Fire, a Final Fire counter. Not for a vehicular MG [EXC: the MA], an MG firing as ordnance, or an MG fired by a lone SMC; such an MG marked First Fire has no Final Fire.
- C2.241: a Gun marked First Fire has one more attack, as Intensive Fire; A8.41 limits it to range 0 or 1. An IFE Gun takes Sustained Fire penalties (C2.29). An ordnance SW marked First Fire has no Final Fire.
- D3.5 excepts an MA with Multiple ROF from one attack per Player Turn; with A9.3's exception, an MA MG marked First Fire may sustain once at an adjacent target.
- What each side knows: fire counters, range, and the phase are public; a concealed firer's weapon and whether it is a lone SMC are its owner's until it fires.

### 3.2 What decides it today

- `ScenarioA1FireCalculator.cs:279` `IsFinalFireAgain` reads the unit's marker, and `:883` (`final-fire-outside`) **decides** the range by it. **The survey's fault:** a squad that fired its MG alone is left unmarked (`ScenarioA1FireFollowUps.cs:322`), so in the DFPh it fires the First-Fire-marked MG as Sustained Fire at any range (`:1309` lets it through; `IsSustained` `:282` is true).
- **Found by the research, the reverse fault:** a First-Fire-marked squad firing alone an MG that kept its ROF or never fired is refused beyond range 1 (`:883`) and halved as Area Fire (`:4222`), where A8.41 lets it fire at any range at full FP.
- **Found by the research:** `ScenarioA1OrdnanceCalculator.cs:525` to `:537` lets a First-Fire-marked Gun Intensive Fire in the DFPh at any range; the test `ScenarioA1Pass8Tests.AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire` asserts it at range 5 (the old answer, kept).
- **Found by the research:** `VehicleFireOutside` (`:1097`) refuses an MA MG marked First Fire its Sustained Final Fire at an adjacent target (ruling R50.2 read D3.5 alone).
- Right, and forwarded: `IsSustained` (`:282`); `WeaponEffects`' B# lowering and counter (`:4391`, `:4424`, `:4459`); `FirerMayFire`'s and `WeaponMayFire`'s DFPh arms (`:1300`, `:1309`); `ScenarioA1FireEligibility.FireSpent`'s DFPh clause (`:225`); the lone-SMC Sustained bar (`:873` to `:879`, gated on the record's version).
- Outside Rules: `MapStudio/Components/Pages/Play.razor:6105` decides the unit row's range by the unit's marker, and its weapon rows (`:6125` to `:6132`) apply no Final Fire limit; texts at `Play.razor:9721`, `FireArithmeticPreview.razor:18`, `Play/RefusalReasons.cs:41`.

### 3.3 Rulings

- **RA8.1 One calculator.** `ScenarioA1FinalFireCalculator` decides A8.4, A8.41, A9.3's sustain conditions, and C2.241's range: `MaySustain`, `Sustained`, `InReach` (range 0 or 1; unknown is not in reach), `Use` (Normal, OnceMore, None), `FinalFireRefusal`, `Verdict` (the use, Area Fire, Sustained, Intensive, the B# reduction), and the constants `SustainedBreakdownReduction` and `SustainedCounter`.
- **RA8.2 The marker that decides** is the weapon's own for a weapon and the unit's own for its inherent FP. Both faults are fixed by it.
- **RA8.3 A vehicle's MA MG** may sustain once in Final Fire at an adjacent target (A9.3's exception, D3.5's); `MaySustain` answers true. `VehicleFireOutside` keeps its answer.
- **RA8.4 The No Fire counter** is a marker value; no caller gives it yet (C5.64's row).
- **RA8.5 Fire markers are public** (All).
- **RA8.6 Old members.** Forwarding, equal: `IsSustained`, `WeaponEffects`' B# reduction and counter, the DFPh arms of `FirerMayFire` and `WeaponMayFire`, `FireSpent`'s DFPh clause, `Multipliers`' Area Fire for the unit's own FP. Keeping their answers, named: `:883`, `:4222`, the ordnance Intensive Fire site (`:528` to `:530`), `VehicleFireOutside`.
- **RA8.8 Other weapons.** `WeaponMayFire`'s DFPh arm forwards to `WeaponMayFinalFire(marker)`, which reads the marker only (not Prep or Final Fire), as the old arm does; the range is `FinalFireRefusal`'s. A FT, DC, or MOL follows A8.4 by its own counter, and its own once-per-Player-Turn rule (A22.3, A23.3, A23.6) is asked by its caller, not here: the builder checks those pages and records the reading.
- **RA8.7 The coverage document's stale lines** (the Sustained Fire X# and the lone-SMC bar, both built since pass 45) are corrected.

### 3.4 Contract `a8.41-final-fire`

"May this unit or weapon fire in Final Fire at this target, and how?" (A8.4, A8.41 p. 61; A9.3 p. 63; C2.241 p. 168.) Facts: `firerId` (Unit, Owner, concerning itself: a concealed firer is not named to the opponent); `marker` (Choice none, first-fire, final-fire, intensive-fire, no-fire), `range` (may be unknown), `phase`: All; `weapon` (Choice inherent, mg, gun, ordnance-sw, ife), `loneSmc`, `vehicular`, `mainArmament`, `asOrdnance`, `noIntensiveFire`, `crewFit`: Owner, concerning `firerId`. Outcomes: `may-fire`, `may-fire-once-more`, `refused`, `not-final-fire`, `not-evaluated` (a refusal the known facts already make is `refused`). Place: DFPh (4.2).

### 3.5 Wiring row W-A8.41 (fire)

- `Play.razor:6105` (the unit's row) and `:6125` to `:6132` (weapon rows): ask `FinalFireRefusal` per row with its own marker. `Play/GamePlanner.Fire.cs`: ask it for each firer's FP and each weapon before the package. Texts at `Play.razor:9721`, `FireArithmeticPreview.razor:18`, `RefusalReasons.cs:41`: "a unit or weapon marked First Fire".
- Rules sites that keep their answers, moved behind a recorded-reading guard: `ScenarioA1FireCalculator.cs:883`, `:4222`; `ScenarioA1OrdnanceCalculator.cs:528` to `:530`; `VehicleFireOutside` `:1097`.
- Behavior changes: an MG marked First Fire Final Fires only at range 0 or 1, whoever holds it; an unmarked MG fired alone by a First-Fire-marked unit fires at any range at full FP; a First-Fire-marked Gun's Intensive Fire in the DFPh only at range 0 or 1; a vehicle's MA MG marked First Fire may sustain once at an adjacent target.

### 3.6 Tests and minigame

`ScenarioA1FinalFireCalculatorTests`: every `Use` case (inherent, MG, Gun, ordnance SW, IFE, by marker); `MaySustain` for a lone SMC, a non-MA vehicular MG, the MA, as ordnance; `InReach` at 0, 1, 2, unknown; the survey's case at ranges 4 and 1; the reverse case; a First-Fire-marked Gun at ranges 1 and 5, "No IF", already Intensive Fired; the forwards equal over enumerated inputs. The contract. Bundle `a8-final-fire` on bd02, after the A9.2 example (manufactured under R0.3): a German squad with an LMG marked First Fire, Russian squads at ranges 1 and 4, a lone leader, a Gun; the Russian viewer's not-evaluated.

## 4. Claim A11.17: Stealth and Lax (rows A11.17, A11.18, A25.64)

### 4.1 The rule (A11.17, A11.18 p. 73; A15.21, A15.24 p. 83; A19.2, A19.3, A19.36 p. 86; A25.7 p. 97; A25.24 p. 95; A25.43, A25.44 p. 96; A25.64 p. 97; H1.24 p. 329; G17.111 p. 319; G1.6 p. 280; A1.123 p. 44; A11.4 p. 73; A12.122 p. 77; A12.152, A12.154 p. 79)

- A11.17: Good Order ANZAC, Gurkhas, Elite and 1st Line Finns, Heroes, Commandos, and Partisans are always Stealthy unless Lax; an SSR may make troops Stealthy; G1.6 covers the Japanese (Good Order Elite and 1st Line Infantry, crews included). -1 to the Ambush dr, the Concealment Growth dr, the Searching dr, and the Search Casualties dr.
- A11.18: Lax by SSR, every Inexperienced unit (A19.36); +1 to the same drs. "Stealthy unless Lax": Lax wins.
- Green and Conscript troops never use Stealth (A19.2), even a Green squad with a leader. A heroic leader is Stealthy as a hero (A15.21, A15.24). An Infantry crew is Elite (A1.123); a vehicle crew is not.
- Lax also: berserk in CC (A15.432); non-elite Italians always (A25.64); Japanese Conscripts and Banzai (G1.6); Human Wave (A25.23).
- Counting: the Ambush dr once per side (A11.4; R14.13, R29.9); the other three per unit.
- What each side knows: nationality and class are revealed before the Ambush (A11.19); in the other drs a concealed unit's class is its owner's; an SSR is on the card.

### 4.2 What decides it today

- **The survey's fault:** `ScenarioA1CloseCombatCalculator.cs:150` (the Ambush dr) and `ScenarioA1FireRuleModels.cs:457` (the Concealment dr) make only a Good Order hero or heroic leader Stealthy.
- **Found by the research:** the same hero-only test in `ScenarioA1Searching.Drm` (`:93`) and `ScenarioA1MoppingUp.CasualtyDrm` (`:129`); and none of the four applies "Stealthy unless Lax".
- **Found by the research:** non-elite Italians are never Lax (A25.64), though the catalog has them.
- **Found by the research:** Play's Search and Mopping Up decide `Lax = Inexperienced` themselves (`Play/GamePlanner.Searching.cs:69` to `:77`, `:154` to `:157`; `Play/GamePlanner.MoppingUp.cs:174` to `:176`), and miss Unarmed units (A19.3).
- Right: the Lax lines of the Ambush (`:139`, `:167`), the Concealment dr's Lax (`:454`), the drm's use (`ScenarioA1FireFollowUps.cs:120`).
- The catalog carries each definition's nationality and class; the Finns are there; no ANZAC, Gurkha, Commando, Partisan, or Raider identity exists, and no SSR token for Stealth or Lax.

### 4.3 Rulings

- **RA11S.1 One calculator.** `ScenarioA1StealthCalculator`: `StealthBarredByClass`, `StealthyByNature`, `Lax`, `Status` (Lax wins), `Drm`, `AmbushDrm` (once per side, names and order as today), `PerUnitStealthy`, `PerUnitLax`, `ConcealmentDrm`. The claim takes A11.18 with A11.17, since the cancel couples them, and A25.64's Lax, a second fault in the same predicate.
- **RA11S.2 Finnish leaders** are not Stealthy unless heroic: A11.17 and A25.7 qualify Finns by class, and a leader has none (A1.25). ANZAC (A25.44), Gurkha (A25.43), and Partisan (A25.24) forces are not class-qualified, so their leaders are Stealthy when the designation is given. Commandos and Raiders are squads: H1.24 (p. 329) designates "A squad designated as a Commando", and G17.111 (p. 319) says "Raider MMC are Stealthy" of the Raider squads/HS, so a Commando or Raider designation makes only its squads and HS Stealthy, never its leaders (heroes and heroic leaders aside, A15.24, A15.21) or an Infantry crew (amended 2026-10-31, the review of PR 79, finding 1).
- **RA11S.3 The Japanese** (G1.6): Elite and 1st Line Japanese Infantry Stealthy, Japanese Conscripts and Banzai Lax, in the same predicates, cited to G1.6; no Japanese unit is in the main catalog.
- **RA11S.4 Designations and SSR** are facts the caller gives. Their source, a card or OB field (backlog rows at `ASL Unit Backlog.md:393` and `:535`), is wiring work, carried in W-A11.17; until it exists only the Finns and the Italians change in play.
- **RA11S.5 Good Order.** A11.17 says "Good Order troops"; the training manual's example (p. 626) that counts a broken squad's Stealth is not followed.
- **RA11S.6 Vehicle crews** are not Stealthy (non-elite, A1.123, D5.1).
- **RA11S.9 A25.64's units.** "Non-elite Italian units" are Italian MMC whose class is not Elite: leaders and heroes have no class and are not Lax by it, and an Italian Infantry crew is Elite (A1.123), so not Lax. RA11S.2's Finnish-leader reading, likewise from the rule text, is open to the user's veto (no Q&A was read).
- **RA11S.10 Inexperience** is asked of the Rules predicate `ScenarioA1Experience.Inexperienced` (A19.2, A19.3), with Unarmed always Inexperienced; Play gathers its facts and decides nothing (W-A11.17).
- **RA11S.11 Group SSRs** (2026-10-31, the review of PR 79, finding 2). A11.17 and A11.18 (p. 73) let an SSR name "Good Order troops" or "certain units", not only a side. The tokens are `stealthy:<side>`, `lax:<side>`, `stealthy:<side>:<group>`, and `lax:<side>:<group>`, the group an OB group of that side, as the Fanatic token names them (ruling R70.15). `ScenarioA1StealthCalculator.SsrFlags(rules, side, group)` folds them into a unit's `SsrStealthy` and `SsrLax`; every caller asks it. A selection finer than an OB group (one named unit, a unit type) is not built (backlog).
- **RA11S.7 Old members.** Forwarding, equal: the Ambush dr's Lax and Stealthy lines, with only the facts they read today. Keeping their answers, named, each with a nullable init property `Stealth` (`StealthStatus?`, null by default, read in place of the old test when set): `ConcealmentCandidateFacts` (`Stealthy`, `Lax`), `SearcherFacts` (`ScenarioA1Searching.Drm`), `MopUpDefenderFacts` (`ScenarioA1MoppingUp.CasualtyDrm`). They are not recorded records (rule 6).
- **RA11S.8 The Ambush's new reading.** `AmbushFacts` gains no field (rule 6). `ResolveAmbush` gains an overload with a `StealthContext` (each unit's designation and the card's SSR facts, and the reading: the calculator's or the old hero-only one); the old overload forwards with null, the same answer. The wiring decides, by the game's recorded version, which reading a recorded Ambush is verified under.

### 4.4 Contract `a11.17-stealth`

"Is this unit Stealthy, Lax, or Normal for this dr, and what drm does it give?" (A11.17, A11.18 p. 73; A19.2 p. 86; A25.7 p. 97; A25.64 p. 97.) Facts: `unitId` (Unit, Owner, concerning itself); `purpose` (Choice ambush, concealment, search, search-casualty), `goodOrder`, `berserk`, `ssrStealthy`, `ssrLax`: All; `kind`, `heroic`, `nationality`, `class`, `designation`, `inexperienced`: Owner, concerning `unitId`. For the Ambush the Strength Factors are revealed before the dr (A11.19, the p. 74 example), so the contract treats an Owner fact given for the purpose `ambush` as known to both sides: the verdict is evaluated for either viewer once the facts are given. Outcomes: `stealthy`, `lax`, `normal`, `not-evaluated`.

### 4.5 Wiring row W-A11.17 (close combat; setup and work for the three drs)

- `Play/GamePlanner.CloseCombat.cs` `:331`, `:345`, `:839` and `ScenarioA1CloseCombatFactBuilders.Unit` (`:27`): the new `ResolveAmbush` overload with each unit's designation and the card's SSR facts. `Play/LiveCloseCombat.cs:438` (`VerifyAmbush`): verify older records on the old reading.
- `Play/GamePlanner.FireExtensions.cs:219` to `:250` (`Candidate`): set `Stealth`.
- `Play/GamePlanner.Searching.cs:69` to `:77`, `:154` to `:157`; `Play/GamePlanner.MoppingUp.cs:174` to `:176`: set `Stealth`; stop deciding `Lax = Inexperienced`, and count Unarmed as Inexperienced (A19.3).
- The designation and SSR source: a card field for a force's designation (Gambit's New Zealanders are ANZAC) and SSR Stealth or Lax tokens.
- Old members to remove once callers move: `ConcealmentCandidateFacts.Stealthy` and `.Lax`, the hero tests in `Searching.Drm` and `MoppingUp.CasualtyDrm`.
- Behavior changes: Elite and 1st Line Finns get -1 in the four drs; non-elite Italians get +1; a Lax unit that would be Stealthy gets +1 and no -1; with the card field, ANZAC (Gambit), Gurkhas, Commandos, Partisans, Raiders, and SSR Stealth and Lax.

### 4.6 Tests and minigame

`ScenarioA1StealthCalculatorTests`: heroes (Good Order, wounded, berserk), a heroic leader, a plain leader; Finnish elite, 1st Line, square, 2nd Line, Green, Conscript, a Green squad with a leader, an Infantry crew, a vehicle crew; each designation, Green ANZAC; SSR Stealthy, SSR Lax on a Finnish elite (Lax wins); a non-elite Italian; Japanese Elite and Conscript; unknown Inexperience; the Ambush once per side; the A11.4 Partisan example (p. 73) and the p. 74 Hero example; the A12.122 Hero example (p. 77); the Ambush forward equal over a grid; the kept members unchanged with `Stealth` null and taking it when set. The contract. Bundle `a11-stealth` on bd04: Finns against Russians, Italians, the Russian viewer's not-evaluated.

## 5. Claim A11.5: CC against a vehicle (rows A11.5, A11.51, A11.62)

### 5.1 The rule (A11.5 p. 74; A11.51 p. 74; A11.62 p. 76; A25.01, A25.12 p. 93; H1.22 p. 329; A7.8 p. 58; A11.19 p. 73; A12.13 p. 77; A20.5, A20.52 p. 87; D7.213 p. 208; A18.12; A19.34 p. 86; A15.42; A11.141; A25.934 p. 98; G1.4231 p. 279; G1.64 p. 281; E6.2 p. 235; the KW TACP p. 666)

- A11.5: only Infantry or Cavalry attack a vehicle; the Kill Number is the attackers' CCV: squad 5, crew 4, HS 3, SMC 2; +1 Assault Engineer, -1 Inexperienced, +1 a combining SMC; -1 for each halving FP penalty (pinned; a concealed target, A11.19; an outnumbered Guard, A20.52; D7.213's marked Reaction Fire). Below the CCV eliminates (at most half: a burning wreck), equal immobilizes. At most two units, one a SMC. The PRC are eliminated when the vehicle is destroyed.
- Assault Engineers: DC-symbol counters and their HS (A25.01), lost on Replacement; DYO or SSR designation (H1.22, A25.12); the AVRE crew.
- National CCV: Ethiopian MMC one lower (A25.934); a Japanese T-H Hero 5 (G1.4231) and a Reduced-Strength Japanese unit its Full-Strength CCV (G1.64); a KW TACP 3; swimmers none (E6.2); an Unarmed unit's FP only against Personnel (A20.5).
- A11.51: a leader's DRM applies only to a unit that can use leadership; a berserk unit cannot (A15.42, A11.141).
- A11.62: a vehicle's attack on Infantry uses the defender's CCV, never below 1, and a condition that would take it below 1 becomes a CC DRM instead.
- What each side knows: in CC every concealed unit reveals its Strength Factor (A11.19); a designated Assault Engineer is a Roster entry, the owner's.

### 5.2 What decides it today (all in `ScenarioA1VehicleCloseCombat.cs`)

- `BaseCcv` (`:352`), the CCV block of `AgainstVehicle` (`:427` to `:455`), the result block (`:542` to `:548`), `Precheck` (`:271` to `:345`) **decide** A11.5. **The survey's fault:** no Assault Engineer +1 (the trait is in the catalogs, never read; every entry carries it `present: false` today).
- **Found by the research:** no -1 for a concealed vehicle (A11.19) or an outnumbered Guard (A20.52); an Unarmed unit is not refused (A20.5); the PRC of a vehicle destroyed in CC stay in the state (backlog 75.2); no national CCV (Ethiopian, Japanese T-H Hero, KW TACP, swimmers; Cavalry, which A11.5 admits, is not built).
- **Found by the research (A11.51):** `AgainstVehicle` adds a combining leader's DRM when the attacker with him is berserk.
- **Found by the research (A11.62):** `DefenseCcv` (`:364` to `:370`) lacks the Assault Engineer +1 and floors each MMC at 1 without turning the excess into a DRM.
- Outside Rules: `Play/GamePlanner.VehicleCloseCombat.cs` (`:589` to `:640`, `:808` to `:826`), `Play/LiveCloseCombat.cs` (`:272`, the verifier `:500` to `:519`), `VehicleCloseCombatPanel.razor`, `Play.razor` (`ProposeVehicleCcAt`) gather, record, and show.

### 5.3 Rulings

- **RA11C.1 One calculator.** `ScenarioA1CloseCombatValueCalculator`: `BaseCcv` (by kind, with the national cases), `AttackCcv` (the modifiers in today's order, the new ones placed so today's lists are unchanged when they are absent), `AssaultEngineer(printedTrait, designated, replaced)`, `VehicleResult`, `CombinationRefusal`, `AttackerRefusal` (Infantry or Cavalry, an enemy, not broken, captured, Unarmed, or swimming), `PrcEliminated`, `LeaderDrmApplies(attackerBerserk)`, `DefenseCcv` and `DefenseCcvDrm` (A11.62's floor and its excess as a DRM).
- **RA11C.2 Each halving penalty** counts once per kind that applies to the attack (pinned, Reaction Fire marked, a concealed vehicle, an outnumbered Guard), as today for pinned; Pin effects are not cumulative (A7.8). No floor on the attacker's CCV.
- **RA11C.3 The Assault Engineer +1** applies to the defender's CCV too (A11.62 uses "the defender's CCV", A11.5 defines it).
- **RA11C.4 The national CCV** cases are facts the caller gives (nationality, a T-H Hero, a TACP, swimming, Cavalry); none has a counter in the catalogs today.
- **RA11C.5 The new facts reach `Resolve` by an overload** with a `CcvContext` (each unit's Assault Engineer status, a concealed vehicle, an outnumbered Guard, the national cases); the old overload forwards with null, the same answer (rule 6). `FireDefinition` gains a nullable `AssaultEngineer` read from the front face's `asl:assault-engineer` trait (`ScenarioA1FireReference.cs`, a field on a definition, not a recorded record).
- **RA11C.6 Old members.** Forwarding, equal: `BaseCcv`, the CCV block (with the new facts absent). Keeping their answers, named: `Resolve` (no context), `Precheck` (no Unarmed bar), `DefenseCcv` (its floor and no Assault Engineer), the leader DRM line of `AgainstVehicle`, and the result block (the review's finding 7: it tests burning before elimination, so a Final DR equal to a CCV of 0 or less gives a burning wreck; `VehicleResult` burns only a DR below the CCV and at most half of it, and immobilizes a DR equal to it).
- **RA11C.7 A11.51's and A11.62's other missing clauses** (a turreted MA of 15mm or less as MG armament, the "armed" test on escorts; RMG, IFE, Passengers and Riders, separate attacks, a CMG in the VCA) stay as their rows record them; this claim fixes the faults the research found in those rows. ATMM (C13.7) is a rule not built, not a fault of these rows.

### 5.4 Contract `a11.5-ccv-vs-vehicle`

"What does this Final DR do to the vehicle, with these attackers?" (A11.5 p. 74; D7.213 p. 208.) Facts: `attackerId` (Unit, Owner, concerning itself); `attackerKind`, `inexperienced`, `smcCombining`, `pinned`, `reactionFireMarked`, `vehicleConcealed`, `guardOutnumbered`, `csRed`, `finalDr`, `unarmed`, `berserk`, `swimming`: All; `assaultEngineer`, `nationalCcv` (Choice none, ethiopian, t-h-hero, tacp): Owner, concerning `attackerId`. Outcomes: `burning-wreck`, `eliminated`, `immobilized`, `no-effect`, `refused`, `not-evaluated`.

### 5.5 Wiring row W-A11.5 (close combat)

- `Play/GamePlanner.VehicleCloseCombat.cs` `:589` to `:640` and `:808` to `:826`: gather each attacker's Assault Engineer status, a concealed vehicle, an outnumbered Guard, the national facts; call the new `Resolve` overload; ask `AttackerRefusal` and `CombinationRefusal` before `Precheck`; drop a combining leader's DRM for a berserk attacker (`LeaderDrmApplies`).
- `Play/LiveCloseCombat.cs` `VehicleFromState` (`:272`) and the verifier (`:510`): the same facts at verify time, older records on their recorded reading.
- `VehicleCloseCombatPanel.razor`, `Play.razor` `ProposeVehicleCcAt`: offer only the attackers `AttackerRefusal` admits.
- The PRC of a vehicle destroyed in CC leave the state by `PrcEliminated` (backlog 75.2 closes with it).
- The vehicle's attack on Infantry (A11.62): the defender's CCV by `DefenseCcv` and `DefenseCcvDrm`.
- An Assault Engineer HS of an Assault Engineer squad keeps the status through Deployment, and loses it on Replacement (A25.01): the state's lineage.
- Behavior changes: an Assault Engineer's CCV +1 (none in play until a definition or a card designates one); no Unarmed unit attacks a vehicle; a concealed vehicle and an outnumbered Guard each lower the CCV by one; a berserk attacker takes no leader's DRM; the PRC of a destroyed vehicle are eliminated; a defender's CCV below 1 becomes a DRM.

### 5.6 Tests and minigame

`ScenarioA1CloseCombatValueTests`: base CCV by kind; Assault Engineer squad, HS, with a leader, an AVRE crew, a Replaced one; Inexperienced; pinned once; pinned and Reaction Fire marked (p. 208's 3); a concealed vehicle; an outnumbered Guard; the national cases; `VehicleResult` with p. 76's and p. 74's examples and a red CS#; the refusals; `PrcEliminated`; `LeaderDrmApplies`; `DefenseCcv` and its DRM; the forwards equal over the existing vehicle CC cases. The contract. Bundle `a11-ccv-vehicle` on bd02: a designated Assault Engineer, a 4-6-7 with a 9-2, a pinned 5-4-8, a T-34, the Russian viewer's not-evaluated, an Unarmed squad refused.

## 6. The claims together

- **Files held** (claims table, section 6):
  - A21.13: `ScenarioA1NonQualifiedUseCalculator.cs`, `ScenarioA1NonQualifiedUseModels.cs`, `ScenarioA1NonQualifiedUseContract.cs`, `ScenarioA1OrdnanceCalculator.cs (shared)`, `ScenarioA1OrdnanceEligibility.cs (shared)`, `ScenarioA1ArmorCalculator.cs (shared)`, `ScenarioA1CanisterCalculator.cs (shared)`, `ScenarioA1AreaCalculator.cs (shared)`.
  - A10.531: `ScenarioA1OpenGroundCalculator.cs`, `ScenarioA1OpenGroundModels.cs`, `ScenarioA1OpenGroundContract.cs`, `ScenarioA1RoutCalculator.cs (shared)`, `ScenarioA1Dash.cs (shared)`, `ScenarioA1MovementCalculator.cs (shared)`, `ScenarioA1AdvanceCalculator.cs (shared)`, `ScenarioA1RuleContracts.cs (shared)`, `ScenarioA1FireMapRules.cs (shared)` (added after the review).
  - A8.41: `ScenarioA1FinalFireCalculator.cs`, `ScenarioA1FinalFireModels.cs`, `ScenarioA1FinalFireContract.cs`, `ScenarioA1FireCalculator.cs (shared)`, `ScenarioA1FireEligibility.cs (shared)`, `ScenarioA1OrdnanceCalculator.cs (shared)`.
  - A11.17: `ScenarioA1StealthCalculator.cs`, `ScenarioA1StealthModels.cs`, `ScenarioA1StealthContract.cs`, `ScenarioA1CloseCombatCalculator.cs (shared)`, `ScenarioA1FireRuleModels.cs (shared)`, `ScenarioA1FireFollowUps.cs (shared)`, `ScenarioA1Searching.cs (shared)`, `ScenarioA1MoppingUp.cs (shared)`.
  - A11.5: `ScenarioA1CloseCombatValueCalculator.cs`, `ScenarioA1CloseCombatValueModels.cs`, `ScenarioA1CloseCombatValueContract.cs`, `ScenarioA1VehicleCloseCombat.cs (shared)`, `ScenarioA1FireReference.cs (shared)`.
  - A21.13 also changes `ScenarioA1FireCalculator.cs` at its captured-SW edges, held shared by A8.41 on the same branch; the A21.13 row lists it shared too.
  - No holding claim holds any of these whole (the fire calculator and the eligibility are held shared by A23.3 and D3.7, the ordnance and armor calculators and the follow-ups by D3.7).
- **ASOP rows:** none; the steps that cite these rules cite other sections' rules too (4.21D, 8.21B).
- **The batch counter:** one branch, one merge: four of five since `580aadf6`. The user asked for the wiring prompt after this pass, so the batch gate runs on main after the merge (section 7).
- **Memory and performance:** pure static checks over a few facts; no state, cache, or event; the overloads allocate nothing new on the old paths.
- **Build outputs outside the repository:** the worktree `asl-gate-work/wt-five` (registered), its filtered test builds, and a Studio check of the five bundles on the Rule Workbench in Claude's Studio (6670), stopped when done.

## 7. After the merge: the batch gate and Wiring Prompt 003

On main after the merge, the batch gate: `gate.sh rules-batch 580aadf6 baseline`, `build`, `proofs`, `carry`. No game may differ. Then main's proved commit is recorded as the last batch gate, and `wiring_prompt.py` writes `Wiring Prompt 003.md` from the board: the open wiring rows by capability (W-D3.7, W-B25.2, W-A23.3, and the five new rows, marked new), the wiring claims in progress, and a note that every fault of section 8.3a these claims took is fixed in Rules and waits for its wiring. The record and the prompt go to main in one commit.

## 8. Review of the design

A sub-agent read the draft against the PDF pages and the code, read-only (2026-10-29): thirteen findings, two blocking. All are taken.

1. **Blocking, A12.14's Hindrance sentence:** RA10.2 rewritten. For concealment, what is in the hex decides, and a Hindrance, a hexside TEM, or Height Advantage along the LOS does not keep the "?". The rout, Dash, and Interdiction keep the full per-seer test. The reviewer would count Height Advantage for concealment; the design does not, as a beneficial defensive DRM. Open to the user's veto.
2. **Blocking, older games' reading:** section 0 names the code's practice, a nullable version property omitted when null (passes 45 to 72, `SpotterUseVersion`). These claims add no field. **For the user:** rule 6's text forbids any added field, while the practice adds nullable, omitted-when-null ones. The proposal is to amend rule 6's wording to state the practice: "No field is removed, renamed, or reordered; a new field is a nullable property omitted from the JSON when null, and null is the reading before it." Approved by the user, 2026-10-30; CONVENTIONS section 1 carries it.
3. The a12.14 contract keeps `terrain` and the seer flag as public facts (RA10.6).
4. A road-rate mover's per-viewer fact `seesAlongRoad`, and B14.6's orchard-road (section 2.2, 2.4).
5. Missing sites: `ScenarioA1FireMapRules.GrainTerrain` (forwards; the file is added to A10.531's holds), `GamePlanner.InfantryOverrun.cs:154` (W-A10.531), the ordnance `:63` and `:64` and fire `:94` copies (forward, RA21.10), D3.7's `Numbers` (named), and `:474` (W-A21.13).
6. A15.23 in full (RA21.9); the contract's Place widened.
7. The A11.5 result block keeps its answer (RA11C.6).
8. The a11.5 contract gains `unarmed`, `berserk`, `swimming`, and `nationalCcv`.
9. `WeaponMayFire`'s forward named, and other weapons' Final Fire (RA8.8).
10. Hidden information: a8.41's `firerId` is the owner's; the a11.17 contract evaluates the Ambush for either viewer (A11.19).
11. A25.64's units (RA11S.9); the Finnish-leader reading is open to veto.
12. Inexperience is asked of the Rules predicate (RA11S.10).
13. Wording: A12.14 cited at p. 78; the Canister and Area ROF sites "lower the ROF for no penalty at all" means they ignore every penalty, the Temporary crew's included.

## 9. As built (2026-10-30)

Five builders, one per claim, each in its own worktree, merged into `claim/audit-five`.

- **A21.13:** `ScenarioA1NonQualifiedUseCalculator` (`Qualified`, `User`, `Multiple`, `PenaltyMultiple`, `Use`, `UseRefusal`, and the arithmetic), its models and the contract `a21.13-crew-served-use`; the bundle `a21-non-qualified`. Forwarding, equal: `NonQualified`, `CapturedColor`, `Breakdown`'s captured term, the ordnance and armor ROF, `PassEightFirerDrm`'s Case H values, the D3.7 adapter checks (through `Multiple`, the multiple they compare), the fire calculator's vehicle MG and captured SW sites. Keeping their answers: `Outside`'s crew check and mortar bar, the Canister and Area ROF, every site reading `CapturedPenalty` alone. Rulings beyond the design: a light mortar (60mm or less) needs no crew (C9.2), and a lone SMC firing a captured mortar as light keeps the capture's penalty; A15.23's phases and once per Game Turn are facts of the contract.
- **A10.531:** `ScenarioA1OpenGroundCalculator` (`OpenGroundTerrain`, `NoFfmoCover`, `OpenGroundTo` by purpose, `OpenGroundToAny`), its models and the contract `a10.531-open-ground`; the bundle `a10-open-ground`; `a12-advance-concealment` gains four checks. Forwarding, equal: the rout's `OpenGround` and `CouldApplyFfmo`'s cover test, `ScenarioA1Dash.OpenGroundTo`, `ScenarioA1FireMapRules.GrainTerrain`'s season. Keeping their answers: `Unmasked` and `LosesConcealment`, each with a new overload taking A10.531's answer. Ruling beyond the design: the a12.14 contract's `smokeInLocation` and `inLocationCover` read as none when absent, since Play does not gather them yet (W-A10.531).
- **A8.41:** `ScenarioA1FinalFireCalculator` (`FiresAgain`, `UnitMayFinalFire`, `WeaponMayFinalFire`, `MaySustain`, `Sustained`, `InReach`, `MayIntensiveFire`, `Use`, `FinalFireRefusal`, `Verdict`), its models and the contract `a8.41-final-fire`; the bundle `a8-final-fire`. Forwarding, equal: `IsFinalFireAgain`, `IsSustained`, the DFPh arms of `FirerMayFire` and `WeaponMayFire`, `WeaponEffects`' sustained B# and counter, `FireSpent`'s DFPh clause. Keeping their answers: `final-fire-outside` (`:883`), the weapon row's Area Fire (`:4232`), the ordnance Intensive Fire site, `VehicleFireOutside`. Rulings beyond the design: a FT or DC (`OtherSw`) marked First Fire has no Final Fire (A8.41 gives the extra attack only to a weapon that can use Intensive or Sustained Fire; A22.3, A23.63); a MOL rides on the unit's inherent attack (A22.6); a vehicle's Gun may Intensive Fire (C5.6), the ordnance site keeping ruling R8.2's refusal.
- **A11.17:** `ScenarioA1StealthCalculator` (`StealthBarredByClass`, `StealthyByNature`, `Lax`, `Status`, `Drm`, `Inexperienced`, `ConcealmentDrm`, `PerUnitStealthy`, `PerUnitLax`, `AmbushDrm`), its models (`StealthFacts` a record struct, `StealthContext`, `StealthReading`) and two contracts: `a11.17-stealth` (concealment, search, search-casualty; the unit facts the owner's) and `a11.17-stealth-ambush` (the unit facts known to both, A11.19); the bundle `a11-stealth`. Forwarding, equal: `ResolveAmbush` to its new overload with a null context, its Lax and Stealthy lines with today's ids and order. Keeping their answers, each with a nullable `Stealth` init property: `ConcealmentCandidateFacts`, `ScenarioA1Searching.Drm`, `ScenarioA1MoppingUp.CasualtyDrm`. Rulings beyond the design: an Italian vehicle crew is Lax (non-elite, A1.123); a `banzaiOrHumanWave` fact (G1.6, A25.23).
- **A11.5:** `ScenarioA1CloseCombatValueCalculator` (`BaseCcv` with the national cases, `AttackCcv`, `AssaultEngineer`, `VehicleResult`, `CombinationRefusal`, `AttackerRefusal`, `PrcEliminated`, `LeaderDrmApplies`, `DefenderCcv`, `DefenseCcv`, `DefenseCcvDrm`), its models (`CcvContext`) and the contract `a11.5-ccv-vs-vehicle`; `FireDefinition.AssaultEngineer`; the bundle `a11-ccv-vehicle`. Forwarding, equal: `Resolve` to its new overload with a null context, the CCV block, the defender's CCV without the new facts. Keeping their answers without a context: `Precheck`, the leader DRM line, the result block, `ByVehicle`'s defender floor. Rulings beyond the design: a Reduced-Strength unit's Full-Strength CCV (G1.64, KW 7.97) is the caller's kind; a T-H Hero is not Unarmed against a vehicle (G1.4231); the defender's floor is per MMC, its excess on that defender's DRM; the pinned -1 on defense follows the p. 75 example.
- After the merge, the fire calculator's three captured SW lines (`:4408`, `:4440`, `:4483`) forward to `BreakdownLowering` and `RofLowering`, equal.
- The wiring rows W-A21.13, W-A10.531, W-A8.41, W-A11.17, and W-A11.5 are in the inventory's section 7.

## 10. Review of the build

A sub-agent read the merged branch read-only against main and the PDF (2026-10-30). All 26 forwarded sites give the old answer on every input Play can reach. Every kept member keeps its body and names its replacement. It agreed with every ruling beyond the design: C5.6 does not bar a vehicle's Gun from Intensive Fire. No recorded record gained a field. Nine findings, none against a replay; all taken:

1. A berserk unit of any kind is Lax in the Ambush, as the old line read it: `Lax` tests berserk before Personnel; a tank joins the forward's test grid.
2. `StealthyByNature` answers false for a vehicle, a Dummy, or a vehicle crew, whatever the SSR or designation (RA11S.6).
3. A25.66's Eritreans are not Lax: none is in the catalog; a note on the A25.64 row.
4. The documents (built states, wiring rows, coverage, Minigames column, status lines) were written before the review reached them (5d47ccfe).
5. `Precheck`'s summary reworded: its own check of the number of attackers gives `CombinationRefusal`'s answer, with its own texts.
6. `AttackCcv`'s concealed-vehicle modifier is `vehicle-concealed`, not the `vs-concealed` of `VehicleFirepower`.
7. **RA11C.8:** the a11.5 contract's `attackerId` is disclosed to All: in CC every concealed unit reveals its Strength Factor before attacks are declared (A11.19).
8. `CcvAttackFacts` and `CcvDefenderFacts` are `readonly record struct`s; `CcvVerdict` keeps its modifier list, which the old block built as well.
9. `IsSustained` reads the definition only when a case applies, as before the claim.
