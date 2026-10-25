# Increment 3c: Voluntary Break and Voluntary Rout (task 70.1, part II)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.11 and R70.12 (design section 6, F5), with R70.3 and R70.6. Increments 3a and 3b are committed: 3b gives `ScenarioA1MoraleCheck` and the `morale-check-resolved` record, which the Voluntary Rout leader's elimination and a leader's Voluntary Break (its LLTC) call.

## Pages
- p. 65: A10.2 ("Should an unbroken leader break ... LLTC").
- p. 66: A10.3 (who never breaks), A10.41 (Voluntary Break), A10.5.
- p. 67: A10.532 (Normal Range), A10.533.
- p. 68: A10.62 (DM after a Voluntary Break).
- p. 69: A10.71, A10.711 (Voluntary Rout), with the example on p. 69 (the pinned HS in P8 that Voluntarily Breaks, and the 9-1 leader who could rout with the squad).
- p. 86 to 87: A20.21 (surrender instead of rout), only as it bears on a unit that breaks voluntarily.

## Build
1. **Voluntary Break (R70.11).**
   - An action (`asl.game.voluntary-break`, unit ids) of the RtPh's start: allowed only before any rout step of the phase, for the owner's units (even pinned) in the LOS and Normal Range (A10.532) of an armed, unbroken Known enemy ground unit, or ADJACENT to any unbroken enemy ground unit. "Known" is read from the side's view, never the full state.
   - Refused when breaking would Reduce or eliminate the unit at once (a broken unit, a unit already DM in a way that would Reduce, whatever A10.41's parenthesis covers), and for heroes, berserk units, and Unarmed units (A10.3). The eligibility is a Rules calculator taking the facts.
   - It writes `voluntary-break` (the units), then the units broken and under DM (A10.62), pin cleared if the rule says so (read it). The projector checks the timing.
   - A leader's Voluntary Break causes a LLTC of the lower-ML unbroken friendly Personnel in his Location (A10.2), through 3b's member.
   - It costs a "?" under R70.17 where 3a's Loss read sees the unit.
2. **Voluntary Rout (R70.12).**
   - A `withLeader` argument on the rout action: a non-berserk, non-pinned, unbroken leader stacked with the broken unit before it routs moves with it on the same route, exempt from one unit at a time (A10.5's EXC).
   - He takes no NMC; he adds his leadership DRM to the unit's Interdiction NMC (recorded in `RoutInterdicted.Drm`, with the leader named in a new optional field); he is eliminated if the unit fails, which causes a LLMC (the surviving HS takes it) through 3b's member.
   - He may portage his own SW; he cannot raise the unit's portage capacity.
   - Both units' steps are recorded; an older rout record, with no leader, replays as it did.
3. **The page.** The Rout panel (`RoutActionPanel.razor` or wherever the Rout panel lives on the Play page) gains a Voluntary Break list (hex first, "[P8]: 2-3-7 half-squad G2", leaders first, through `UnitSelectionList`/`UnitOptions`) and a leader choice for the routing unit (only eligible leaders, hex first). A new button id joins the Icon Lab's coverage file.
4. **Registries:** the action in `GameActions.All`, the planner's dispatch, and `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation`; the event through the reader, writer, projector, `PlayRecords.cs`, and `ReplaySteps.cs`.

## Tests (by method)
- **Rules:** eligibility (in LOS and Normal Range of a Known armed unbroken enemy; ADJACENT; refused for a hero, berserk, Unarmed, or one that would be Reduced; refused after a rout step); the Voluntary Rout leader's DRM on the NMC and his elimination on a failure.
- **Play:** a pinned HS Voluntarily Breaks and routs (the p. 69 example's shape); a leader's Voluntary Break causes a LLTC; a leader routs with a broken squad, adds his DRM, and is eliminated when it fails, with the LLMC; an older recorded rout replays as it did.
- **Blast radius:** frame A's section 9 lists the Interdiction and rout methods; run those your change reaches, by method.

## Studio
`games_3c.py`, on board 3: a German 4-6-7 squad and a 2-3-7 HS pinned in the wooden building [O10], a Russian 4-4-7 in the woods of [P8] (in LOS), in the German RtPh's start, for the Voluntary Break; a broken German 4-6-7 with an unbroken 9-1 leader in [P10] (open, in the Russian LOS), for the Voluntary Rout with an Interdiction (give the dice for a pass and a failure). Check the pairs on `/boards/bd03`'s LOS tab or with the standing rules' board facts, and say which.
