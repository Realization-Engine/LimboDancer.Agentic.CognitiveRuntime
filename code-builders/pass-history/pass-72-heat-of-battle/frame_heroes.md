# Frame B: A15.23 hero weapons (p. 83), with A15.24 where it applies

The read-only frame agent's report, 2026-10-27, kept as given (condensed only in layout). Pages read: 83, 88, 179, 182 to 185, 195, 201, 203, 221; every page grepped for hero, heroic, SMC, "lone SMC", A21.1x.

## 1. What is built
- R15.11 (Passes Plan:574) in `ScenarioA1FireCalculator.cs`: a hero fires one MG or FT, an ATR refused (`definition.IsHero && mg.IsAtr`, 750-757); `hero-mg:<id>` +1 (3690-3695); `heroic:<id>` -1 within his weapon's Normal Range, never for a FT (3747-3754); inherent FP forfeited (880-882, 4036-4038).
- Tests: `ScenarioA1Pass15Tests.AHeroFiresAMgWithItsTwoManDrmAndHisHeroicDrm`; `BacklogPass15Tests.AHeroFiresAMgAtFullFpWithTheTwoManDrmNegated`; `BacklogPass15TablePlayerTests.AHalfSquadAndAHeroFireFlamethrowers`.
- Backlog row 347 ("A hero firing a LATW, a light mortar, or a Gun: Refused") does not match the code: the ordnance calculator takes any Personnel firer, hero included (`ScenarioA1OrdnanceCalculator.cs:15, 439-447`; `LiveOrdnance.cs:133-158`). Today a hero fires a light mortar (lone SMC ROF loss built, `ScenarioA1AreaCalculator.cs:213-220`), a PF (+2 SMC on the PF Check, `OrdnanceCalculator.cs:107-132`), an ATR at a vehicle; none takes A15.23's -1. Refused: a PSK for leader and hero (`OrdnanceCalculator.cs:442`, R9.11), an ATR as Small Arms (FireCalculator:753). Guns: only a crew or squad/HS mans one (`OrdnanceCalculator.cs:441`).
- A heroic leader: `FireFirer` (`ScenarioA1FireModels.cs:918`) has no Heroic fact; both hero rules test `definition.IsHero`, so a heroic leader (A15.21) fires a MG as a leader and takes no heroic -1. Proposed: backlog row to pass 73 (A15.21's reach). No proof game holds one.

## 2. A15.23's clauses
Two-man SW: BAZ and PSK (C13.45, C13.48, p. 184), RCL (C12.21), MOL-Projector (C13.51). One-man: PIAT (C13.62), ATR (C13.21), PF, light mortar (C9.2). C13.25 (p. 183): hero modifiers apply to an ATR's To Hit, not its To Kill. Catalog: PSK, ATR, PF, light mortars; PIAT synthetic only; no bazooka.

| Clause | Package today | Hero firer? | Needs | Proposal |
|---|---|---|---|---|
| Non-MG SW -1: light mortar | Ordnance | yes | -1 `hero:<id>` on To Hit | in, 0:20 |
| Non-MG SW -1: PF, ATR at vehicle, PIAT | Ordnance (To Hit DRM near 832-843) | yes | same -1, To Hit only | in, shared |
| ATR as Small Arms | Fire (753 refuses) | no | lift the bar; A15.24's -1 applies; no `hero-mg`; `RefusalReasons.cs:31` | in, 0:20 |
| Bazooka, MOL-Projector | none | n/a | counter | out: pass 110 (110.7, 110.8) |
| PSK by a lone hero (+1 negated) | Ordnance, 442 refuses | no | allow lone hero; +1 and -1 | in, 0:30 |
| RCL | none | n/a | counter | out: pass 110 (110.9) |
| Gun <= 82mm as captured, no heroic DRM | Ordnance; manning only at setup or unhook (`GamePlanner.Guns.cs:166-173`) | no | manning an unmanned Gun (backlog 200) and `CapturedPenalty = 1` | out: pass 110 (110.6) |
| Gun > 82mm once a Game Turn, MPh/DFPh | needs the above | no | per-turn record | out: pass 110 |
| No CA change of NT unless M# >= 9 (C2.27) | needs the above | no | CA bar | out: pass 110 |
| AFV AAMG as a Rider (D1.83, D5.3, D5.34, D3.7) | vehicle weapons fire by its crew (`LiveFire.cs:95-128`) | no | a Rider-firer path | out: pass 105 (105.1), backlog 166 |

Totals: in 1:30 plus a Studio game 0:20, 1:50 of building. Proposed ruling: an ordnance SW takes the hero's -1 on the To Hit DR only, not again on the HE IFT after a hit (A15.23 "To Hit or IFT DR"; C13.25); a SW fired as Small Arms takes A15.24's -1 and no second one.

## 3. Captured weapons
Built: a captured MG or ATR, B# two lower and ROF one lower (R13.7; `FireCalculator.cs:4217-4268`); ordnance `OrdnanceShot.CapturedPenalty` (R65.22) set only for a vehicle's Temporary crew. Not built: captured Guns, mortars, LATW (backlog 304, pass 110.6). A21.11 (p. 88): Ammunition Shortage penalties do not apply to captured Gun/SW; the swept game's hero fires a captured Russian MMG, so 72.2's B# must skip `weapon.Captured`.

## 4. Recorded-ness
Heroes only in the Tractor Works games: `g-hero-1` (card hero, German) fires a captured MMG (`r-mmg-2`) twice from cut r1202 on, with `hero-mg` +1 and `heroic` -1; `choose-8d3f461e087c-r2-squad-3-hero` (Russian, Heat of Battle). No hero as an ordnance crew, none holding a mortar, LATW, or PF, no heroic leader, none in p71 games. The MG path is unchanged: any differing line for `g-hero-1` would be a regression.

## 5. Hidden information
`hero:<id>` shown only where the firing side's records are, through `GameView` like `hero-mg`; `DisplayText.cs:392` needs a label. Refusals read the acting side's view and name a hex-tagged hero. A hero created concealed loses "?" when he fires by the existing rule.

## Not read
The ordnance To Hit DRM lines in full; whether a hero firing a light mortar records his inherent FP as forfeited; `GamePlanner.CardSetup.cs`; pass 65's Rider fire code; the PDF images.
