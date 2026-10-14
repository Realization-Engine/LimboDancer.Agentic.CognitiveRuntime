"""Pass 35 review fixes 11 to 14 and 16 (the Play page's stale drafts, and the bogged-unload rule out of the page), and the exit a Recalled
vehicle is offered. Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PAGE = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
EDITS = {
    PAGE: [
        # Fix 11: the thrown DC's draft is cleared with the fire drafts (view, phase, game, the fire prune) and by its own commit.
        ("""    private void ClearFire()
    {
        fireAlso.Clear();""",
         """    private void ClearFire()
    {
        throwDc = throwDcAt = string.Empty;
        fireAlso.Clear();"""),
        ("""            else if (proposal.Action == GameActions.Fire)
            {
                ClearFire();
            }""",
         """            else if (proposal.Action == GameActions.Fire)
            {
                ClearFire();
            }
            else if (proposal.Action == GameActions.ThrowDc)
            {
                // The UI review, pass 35: the DC is spent, so its draft goes with it.
                throwDc = throwDcAt = string.Empty;
            }
            else if (proposal.Action == GameActions.FireOrdnance)
            {
                // The UI review, pass 35: a shot may wreck its target or break its Spotter, and Intensive Fire is declared for one shot. The weapon
                // and the Location stay for a weapon that keeps its ROF.
                ordnanceIntensive = false;
                if (state?.Unit(ordnanceVehicle) is not { Status: InstanceStatus.Active })
                {
                    ordnanceVehicle = string.Empty;
                }

                if (state?.Unit(ordnanceSpotter) is not { Status: InstanceStatus.Active } spotter || GameState.Condition(spotter, Conditions.Broken) == ConditionState.True)
                {
                    ordnanceSpotter = string.Empty;
                }

                if (state?.Unit(ordnanceDirector) is not { Status: InstanceStatus.Active } director || GameState.Condition(director, Conditions.Broken) == ConditionState.True)
                {
                    ordnanceDirector = string.Empty;
                }
            }"""),
        # Fix 14: the exit edge and the berserk "keep" text are drafts of the move; a new game clears every draft; a view clears the editor's boxes.
        ("""            else if (proposal.Action == GameActions.EndMove)
            {
                moveTo = string.Empty;""",
         """            else if (proposal.Action == GameActions.EndMove)
            {
                moveTo = moveExit = moveKeep = string.Empty;"""),
        ("""        moveTo = string.Empty;
        moveAssault = false;
        moveDoubleTime = false;
        moveMinimum = false;
        moveBypass = string.Empty;
        vehicleIntended = string.Empty;""",
         """        moveTo = moveExit = moveKeep = string.Empty;
        moveAssault = false;
        moveDoubleTime = false;
        moveMinimum = false;
        moveBypass = string.Empty;
        vehicleIntended = string.Empty;"""),
        ("""        smokeBy = smokeAt = placeDc = placeDcAt = string.Empty;
        ccLocation = string.Empty;
        ClearCloseCombat();""",
         """        smokeBy = smokeAt = placeDc = placeDcAt = string.Empty;
        ccLocation = string.Empty;
        ClearCloseCombat();

        // The UI review, pass 35: another game starts with no draft of the last one (Opportunity Fire boxes, a vehicle's step, an exit edge).
        ResetDrafts(keepCloseCombat: false);"""),
        ("""        placeLocation = placeId = placeBore = placeGroup = string.Empty;
        placeHidden = false;
        placeConcealed = false;
        placeHolder = string.Empty;""",
         """        placeLocation = placeId = placeBore = placeGroup = string.Empty;
        placeHidden = false;
        placeConcealed = false;
        placeBroken = placeOffBoard = false;
        placeHolder = string.Empty;"""),
        # Fix 13: the CC Reaction Fire draft is bound to the units of the hex the vehicle is in now.
        ("""                        <ReactionFireAction Vehicle="@reacting" Attackers="@(reactors.Select(unit => unit.Id).ToArray())"
                            Leaders='@(reactors.Where(unit => unit.Kind is "asl:leader" or "asl:hero").Select(unit => unit.Id).ToArray())'
                            Attacker="@reactionAttacker" Leader="@reactionLeader" OnAttacker="@(id => reactionAttacker = id)" OnLeader="@(id => reactionLeader = id)"
                            OnPropose="@(() => ProposeVehicleCc(reactionAt.ToString(), [reactionAttacker, .. (reactionLeader.Length > 0 ? new[] { reactionLeader } : [])], null, [], false))" />""",
         """                        // The UI review, pass 35: a choice made in the hex the vehicle left names no unit here, so it is not shown and not sent.
                        var reactAs = reactors.Any(unit => unit.Id == reactionAttacker) ? reactionAttacker : string.Empty;
                        var reactWith = reactionLeader != reactAs && reactors.Any(unit => unit.Id == reactionLeader && unit.Kind is "asl:leader" or "asl:hero") ? reactionLeader : string.Empty;
                        <ReactionFireAction Vehicle="@reacting" Attackers="@(reactors.Select(unit => unit.Id).ToArray())"
                            Leaders='@(reactors.Where(unit => unit.Kind is "asl:leader" or "asl:hero").Select(unit => unit.Id).ToArray())'
                            Attacker="@reactAs" Leader="@reactWith" OnAttacker="@(id => reactionAttacker = id)" OnLeader="@(id => reactionLeader = id)"
                            OnPropose="@(() => ProposeVehicleCc(reactionAt.ToString(), [reactAs, .. (reactWith.Length > 0 ? new[] { reactWith } : [])], null, [], false))" />"""),
        # Fix 16: whether a bogged vehicle may unload is Rules' answer (D8.5), not the page's.
        ("""        state is null || !VehicleStopped(vehicle, boggedToo: true) ? [] : [.. state.Passengers(vehicle.Id).Select(unit => unit.Id).Order(StringComparer.Ordinal)];""",
         """        state is null || !VehicleStopped(vehicle, boggedToo: ScenarioA1VehicleProjection.BoggedMaySpend("unload", false)) ? []
            : [.. state.Passengers(vehicle.Id).Select(unit => unit.Id).Order(StringComparer.Ordinal)];"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Recall.cs": [
        ("""    /// <summary>The exits a vehicle may make from its Location (A2.6): an edge direction within its VCA (D2.11) whose exit cost is reviewed.</summary>
    public IReadOnlyList<VehicleMove> VehicleExits(GameState state, UnitInstance vehicle, string? edge = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);""",
         """    /// <summary>
    /// The exits a vehicle may make from its Location (A2.6): an edge direction within its VCA (D2.11) whose exit cost is reviewed. A Recalled AFV
    /// that must leave has only its Friendly Board Edge (D5.341), so the page offers it no other (found in the Studio, pass 35).
    /// </summary>
    public IReadOnlyList<VehicleMove> VehicleExits(GameState state, UnitInstance vehicle, string? edge = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        edge ??= MustLeave(vehicle) ? state.Side(vehicle.Side)?.FriendlyEdge : null;"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
