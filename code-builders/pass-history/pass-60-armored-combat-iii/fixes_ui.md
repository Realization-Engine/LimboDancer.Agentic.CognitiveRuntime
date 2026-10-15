# Fixes from the UI and Blazor review (R4), as judged by the session

Fix:
1. MAJOR. `Play.razor` `EnemyVehicleOnMap` (about line 4452; used by `OrdnanceTanks` and `OrdnanceGuns`) reads the full state: a side's MGs and MA-MG vehicles appear in the Gun panel once any enemy vehicle exists, seen or not, and for a truck too. Read the firing side's view (as `FireTargets` does, `GameView.Of(...)` / `SeenOnly` for a non-adjudicator) and test for an armored vehicle (`GamePlanner.IsArmoredVehicle` or the Rules predicate).
2. `ThrowDemolitionChargeAction.razor` and `PlacedChargeDetonationActions.razor` show `vehicle.Label` raw ("rt (defender-tank)"): build the label from the side's own words as the other panels do (`Say`), and say the detonate button's charge and Location the same way.
3. `throwDcVehicle` is never cleared: clear it in `ClearFire`, after a ThrowDc commit, and when the throw Location changes.
4. `fireVehicle` is not pruned: clear it in `PruneFire` when it is no longer among `NamedVehicleChoices(side, FireTarget)` or no FT or MOL is in the attack.
5. The MG To Kill panel is ready with no AFV named: require the named vehicle for a MG in `Ready`, with the note "name the AFV (A9.61)".
6. The MG is offered where Rules always refuses it (the AFPh, Bounding First Fire, a pinned possessor, an SMC alone, a BU or moving vehicle's MA MG): leave it out of the list or label it with Rules' reason, as a malfunctioned Gun's label carries its reason. Take the reason from `ScenarioA1MachineGunKill`, do not decide it in the page.
7. `OrdnanceTargets` builds `GameView.Of(...)` twice a render: build it once.
8. Page tests (MapStudio tests): `#fire-vehicle` offers the AFV with a said label and the commit carries `vehicle`; `#throw-dc-vehicle`'s said label; `.detonate-dc-vehicle`; the MG appears in `#ordnance-gun` only when the side sees an enemy AFV (guards item 1); `.fire-named-vehicle` and `.fire-weapon-destruction` render. Unit tests for `FireText.NamedVehicleLines` and `WeaponDestructionLines`.
9. The failing `PlayPageVehicleTests.ATankIsOfferedInTheGunPanelWithTheEnemyVehicleAndItsAmmunition` (line 422): at Infantry the list now offers HE first, then AP (and APCR) by HE Equivalency. Assert the first option is `he`, that `ap` is among them, and the note holds "AP: 2 FP by HE Equivalency, C8.31"; keep the vehicle list's asserts; correct the comment.
10. `@key="(placed.Charge, vehicle.Id)"` on the detonate-at-AFV buttons.

Not fixed now (backlog rows): the pass 55 page test of HE refused after it ran out was replaced (the Gun offers AP instead), so that refusal has no page test; three rule conditions decided in the page (an AAMG MA fires only CE, a plain MG MA's 8-hex Normal Range, when HE is left unnamed), extensions of patterns from passes 50 and 55, to move to Rules; `#propose-throw-dc` disabled with no title (older than the pass).
