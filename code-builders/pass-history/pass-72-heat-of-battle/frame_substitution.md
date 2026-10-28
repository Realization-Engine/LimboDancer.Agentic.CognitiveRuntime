# Frame C: 72.2 Unit Substitution (A19.131, A19.132, A19.31, A19.32; p. 86)

The read-only frame agent's report, 2026-10-27, condensed only in layout. Scratch scripts c_elr5.py, c_defs.py, c_inexp.py, c_twelve.py in the session's scratch folder.

## 1. Card SSR facts
An SSR is a `ScenarioCardRule` with `Tokens` (ScenarioCards.cs:67); `ScenarioCards.Start` writes them into `start.specialRules` (:297); the state keeps `GameState.SpecialRules` (GameState.cs:39, GameProjector.cs:510). SAN and ELR are set by side and group (ScenarioCards.cs:53-61, 259-286); `GameState.ElrOf` (:48) feeds `FireTarget.Elr`, `TargetSideElr`, `FiringSideElr`. Pattern: pass 70's `fanatic:<side>[:<group>]`, parsed in Rules (`ScenarioA1GameStart.FanaticToken`, :41-68), checked on the card (ScenarioCards.cs:420-422, `ScenarioA1SetupCalculator.RuleDiagnostics`) and for a new game (`ScenarioA1NightAndWeather.RulesBar`, :57-61). The card editor has a free-text tokens field (CardRulesFields.razor:26).
Proposal: `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>` (n 0 to 4); null facts when absent; no built-in card holds a token and no proof game a special rule.

## 2. A19.131
Cross-references: A.11 (p. 43, a lowered B# becomes an X#); A21.11 (p. 88, not for captured Guns and SW); A8.221 (p. 60, pass 73); A9.22 note (p. 110, a MG under Ammunition Shortage is not Good Order for a Fire Lane); p. 391 (OBA Scarce Ammo).

| Clause | Code today | Proposal |
|---|---|---|
| Original 12 IFT DR with Inherent FP: Unit Replacement after the attack, Random Selection among the FG, incapable broken unless berserk | `ScenarioA1FireReference.ReplacementOf` (:571), `Replace` (FireCalculator :4721), `FireResolution.FirerEffects` (FireModels :1706, FireCalculator :4283), `FireRolls.RandomSelection`, `UsesInherentFp` | in: an attack fact `AmmunitionShortage`; Random Selection drs (A.9, p. 43) under a new roll key; effects public; `LiveFire.Rolls` key check |
| the Replaced firer broken? | none | ruling: it keeps its status; only the "incapable" are broken; an underscored squad becomes its two HS |
| SW B#/X# one lower (not DC) | `attack.BreakdownReduction` (Extreme Winter) on MG, ATR, FT, vehicle MGs (FireCalculator :4214-4220, :3068, :3088), ordnance SW (OrdnanceCalculator :1422); taken as recorded by the verifier (LiveFire.cs:1251) | in: the fact, checkable; skip captured; A.11's `lowered` (:4268); ordnance SW through an `OrdnanceShot` fact |
| Depletion Numbers one lower | `ScenarioA1ArmorCalculator.Depletion`/`ShotDepletion` (:97-180), +1 Elite | in, -1 |
| vehicles and Guns circled B#, Low Ammo (D3.71) | not built; pass 105 (105.7) | out: pass 105 |
| no effect on CC, Sniper, Residual | `ResidualFire` own kind | in, a skip |
| no Fire Lanes | `FireLaneDeclarationBar` (:1043) | in, one fact |
| OBA Scarce Ammo | OBA not built | out: pass 150 |

## 3. A19.132
R15.9 in `ElrOf` (FireCalculator :4695-4706) gives an underscored MMC 5; `Replace` (:4731-4751) sends it down the two-HS or Disrupted branch. Proposal: target fact `UnderscoredElr` (int?), on FPF firers too (:4290); `ElrOf` returns `UnderscoredElr ?? 5`; when set, the normal `ReplacementOf`. Catalog underscored MMC (`asl:elr-5`): German 8-3-8 and 3-3-8, `attacker-elite-half-squad` (2-4-8), NKVD 6-2-8 and 3-2-8, Finnish 8-3-8 and 3-3-8. 2-4-8 to 2-4-7 and NKVD to Conscript exist; 3-3-8 to 2-3-7 via `CasualtyHalfSquadOf`; the German 8-3-8 has no entry. Ruling needed: 8-3-8 to the 4-3-6 Conscript (the only lower-Class German MMC with no factor above it); Finnish elite stays as R15.13.

## 4. A19.31
Built: three MF, leader bonus, eight berserk (Experience.cs:46-61); the conveyance exception (`ConveyanceAllowance = 4`, GamePlanner.Passengers.cs:338-339; `ConveyedAllowance`, Movement.cs:396). Pass 71's carry is no conveyance and a SMC is never Inexperienced (A19.2). Human Wave: pass 180. Verify and record only. Side note: `Experience.Inexperienced` runs `state.At` per unit.

## 5. A19.32 and R9.10
A MG's IFT B# in `WeaponEffects` (FireCalculator :4214-4220); only `AtrInexperience` takes -1, by class only (misses A19.3's Green-with-leader exemption); ordnance (OrdnanceCalculator :1427-1429) class only. A9.7's inherent B# is a SW's B12, not a squad's. The verifier recomputes from recorded facts and dice and refuses a differing record (LiveFire.cs:1048-1335). The store's one Inexperienced MG fire: `dl-tractor-works-01` revision 1267, `defender-conscript-squad`, Russian LMG, B11, Original DR 10; lifting R9.10 unguarded would make it B10, a malfunction, and the record refused. Proposal: lift behind a pass-72 attack stamp (like pass 45's `CoweringMarksAndWeaponResidual`); Inexperienced from the `Inexperienced` fact or Conscript class; the same stamp fixes the ATR's leader exemption; the ordnance class-only read to the backlog. No proof fire record has an Original 12.

## 6. Hidden information
Both tokens are public; the Random Selection dr and the Replacement are public events, never only inside a record withheld from the firing side.

## Estimates
Tokens 0:35; Original 12 Replacement 1:30; B#/X# 0:30; Depletion 0:15; Fire Lane 0:10; A19.132 0:35; A19.31 0:05; A19.32 0:35; Studio games 0:30. Total 4:45 (plan 0:40). Moved out: circled B# and Low Ammo to 105.7; OBA Scarce Ammo to 150; the ordnance class-only read to the backlog; A8.221 to 73.

## Not read
AddFireEvents past its header; the projector's `FirerEffects`; `LiveFire.Rolls`; the ordnance verifier; the dice prompts; A25.7.
