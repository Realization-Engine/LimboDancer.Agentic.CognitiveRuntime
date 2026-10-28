# Review R4: UI, the Rules boundary, memory and performance (head db15561b)

The read-only reviewer's findings, condensed in layout only.

Common answers: two small boundary cases (4, 9); every new JSON property is `JsonIgnore(WhenWritingNull)` and each verifier keeps an older record's result, the one unstamped recompute (5d's ordnance `AmmunitionShortage`) safe because the token did not exist before 5c (10); one unknown-unit case (3).

1. MINOR. `DescribeChoice` (GamePlanner.Choices.cs 114-118) and `HardeningChoiceText` (ChargeCalculator 325): the choice names the hex, not the unit; two berserk units in one hex (a berserk leader's Location, A15.41) get identical choices. Fix: name the unit.
2. MINOR. `PlayRecords` (501, 640): the hardening answer writes only the generic "A choice the game waited for was answered"; the record never says which unit took or refused hardening or what it became. Fix: a `ChoiceMade` arm for `berserkHardening:` with `HardeningText`.
3. MINOR. `LiveCloseCombat.FromState` (136-138): `vehicleSides` reads only Concealed, not Hidden or Captured; a HIP vehicle would keep a unit berserk and be named in the record. Fix: the planner's Known predicate (`VehicleCcUnits`' `Known`).
4. MINOR (boundary). `GamePlanner.ChargeEntry` (CloseCombat.cs 1305): Play withholds a bridged hex's terrain because A15.45 bars only unbridged water: the rule's reasoning in Play. Fix: a `Bridged` fact; `RuleBar` decides.
5. MINOR (perf). `BerserkReturnsAfterFire` (Fire.cs 1251): the index is taken for every fire attack before the berserk filter. Fix: test for a berserk firer first.
6. MINOR (perf). `PlayRecords.BerserkClearedBy` (640): a linear scan of the log per berserk-clearing event. Fix: a lazy EventId dictionary.
7. NOTE (perf). GamePlanner.cs 859-866: `onMap` is evaluated for every unit at every phase advance, twice. Fix: pre-filter the MPh and berserk.
8. NOTE (perf). `ScenarioA1UnitSubstitution` token parsers split every SSR with no prefix test; called several times per Play render with the ordnance panel open. Fix: `StartsWith` first.
9. NOTE (boundary). `PlanBerserkHardeningAnswer` (Choices.cs 350-353): `Berserk = False` written by hand; the comment's order is wrong. Fix: `BerserkEndsConditions()`.
10. NOTE. The ordnance `AmmunitionShortage` has no stamp; safe; a comment.
11. NOTE. `heroWeapons` is set conditionally, repeating `HeroDrm`'s scope test. Fix: an unconditional stamp.
12. NOTE (for R1). `BerserkReturnsAfterVehicleKill` (VehicleCloseCombat.cs 761): Passengers and a surviving crew of the destroyed vehicle not counted after.
13. NOTE (UI). Play.razor 1114: the Fire Lane fields are offered under Ammunition Shortage and refused only on the proposal.

The hex rule elsewhere passes.
