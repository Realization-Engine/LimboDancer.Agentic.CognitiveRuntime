# Increment 3h: Withdrawal completed, CC with a Gun's crew, a Carrier HS (task 70.6)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.26 to R70.28 (design section 6, F13). Frame C's report, Q1 and Q5, names the code.

## Pages
- p. 45: A1.32 (Recombination; "Carrier HS").
- p. 72: A11.1, A11.13 (a Gun or SW in CC, and its loss).
- p. 73 and 74: A11.2, A11.21 (Withdrawal; into a concealed enemy's Location; the p. 74 example with the HIP AT Gun in G4).
- A1.123, A5.5 (a crew's FP; a manning crew as a squad for stacking).
- p. 206: D6.82.

## Build
1. **Withdrawal (R70.26).**
   - A destination is barred only by a Known enemy unit, read from the side's view (today `WithdrawalDestinations`, `GamePlanner.CloseCombat.cs` about :637-640, bars any Location with an uncaptured enemy unit, concealed or hidden ones included, and the page's lists at `Play.razor` about :4371, :4548, :4603 count them too: both leak).
   - Into a concealed (not Dummy) enemy's Location the withdrawer is eliminated, a Random Selection of the enemy units there loses "?", and hidden units are placed (A11.21 and its example). Into a Dummy-only Location the Dummies are removed.
   - A unit above its IPC may withdraw by dropping SW, which stay unpossessed in the Location (today it is refused, `WithdrawalPortageBar`).
   - Other levels stay ground level (pass 135).
2. **A Gun's crew in CC (R70.27).**
   - Lift the five refusals (frame C, Q1: `ScenarioA1AdvanceCalculator.CrewBar`, `ChargeBarred`, `RequiredUnit`, `BerserkOwingVehicleAttack`, `UnitsOutside`'s `asl.a1.cc.unit-outside` for `Manning == true`). The crew fights with its inherent FP (A1.123); the Gun is never used in CC (A11.13).
   - A11.13's loss: each Gun (and SW) of a unit eliminated in CC is eliminated on a colored dr of 1, followed by a dr at or below the black Kill Number of that attack; otherwise it stays unpossessed. A new recorded fact on the CC resolution lists the Guns each unit possesses; absent, the Gun loss is not rolled (an older record resolves as it did). Check whether the SW loss of A11.13 is built today and, if it is, extend it rather than build beside it.
3. **A Carrier HS (R70.28).** A Carrier's leaving crew's `instance-created` records the vehicle it left (an absent-safe fact, `GamePlanner.Ordnance.cs` about :1484 or wherever pass 65's `LeavingCrew` places it); Recombination of a Carrier HS needs no leader and no TC (A1.32); `NeedsNoLeader` gains the input.
4. **The page.** The withdrawal and infiltration choices list only Locations with no Known enemy, from the side's view, and offer a "drop" choice for SW above the IPC; a Gun's crew appears in the CC attack builder hex first.

## Tests (by method)
- **Rules:** a withdrawal destination with a concealed, a hidden, and a Dummy enemy; dropping SW; a Gun's crew in CC; A11.13's Gun loss on a colored dr of 1 and the Kill Number; a Carrier HS recombining.
- **Play:** a withdrawal into a concealed enemy's Location eliminates the withdrawer and reveals; an advance onto a Gun's crew and its CC; an older recorded CC replays as it did.
- **Blast radius:** frame C's Q8 lists the Gun's crew, withdrawal, and Carrier HS methods that assert today's refusals; each changes as intended; run them by method.

## Studio
`games_3h.py`, on board 3: a German 4-6-7 in Melee with a Russian squad in [F9], a concealed Russian HS in [F8] (the withdrawal), and a Russian 45mm AT Gun with its crew in [D10] with a German squad ADJACENT in its APh; a British Bren carrier whose HS left it beside another British HS. Give the dice.
