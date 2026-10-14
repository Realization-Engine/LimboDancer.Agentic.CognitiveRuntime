"""Pass 45, task 45.8: a MMG's or HMG's Bore Sighting (C6.41 to C6.44). Every anchor is checked before anything is written."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1GameStart.cs": [
        ("""    /// <summary>The setup in words.</summary>
    public static string SetupText(int eventCount)""",
         """    /// <summary>
    /// C6.41, C6.42 (p. 174; pass 45): "the only SW that can be are MMG, HMG, and light mortars"; the Location "must be outside the weapon's own hex,
    /// and within both the LOS of the weapon ... and its Normal Range (16 hexes maximum)". A possessed MMG or HMG of the Scenario Defender; the LOS
    /// and range are read last. A light mortar's Bore Sighting is a To Hit case and is not built here.
    /// </summary>
    public static string? MgBoreSightBar(string? size, bool scenarioDefenders, bool ownHex, Func<bool> losClearWithinNormalRange, string mg)
    {
        ArgumentNullException.ThrowIfNull(losClearWithinNormalRange);
        return size is not ("medium" or "heavy") || !scenarioDefenders || ownHex || !losClearWithinNormalRange()
            ? $"play.bore-sight: {mg} may Bore Sight only as a MMG or HMG the Scenario Defender possesses, a Location outside its hex, in its LOS, within its Normal Range and 16 hexes (C6.41, C6.42)"
            : null;
    }

    /// <summary>The setup in words.</summary>
    public static string SetupText(int eventCount)"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs": [
        ("""    public static FireAttack Assemble(AttackAssemblyFacts facts)""",
         """    /// C6.44 (p. 174; pass 45): "A MG/IFE non-ordnance attack ... vs a unit in the firing weapon's Bore Sighted Location may deduct two from its IFT
    /// DR", and "may be used by a FG making such an attack only if all elements of that FG have Bore Sighted that same Location". A squad's own FP
    /// has Bore Sighted nothing, so every firer fires its MG alone, and each MG fired has its sighting on the target Location (C6.43: by the unit
    /// that set up with it, from its setup Location).
    /// </summary>
    public static bool MgBoreSighted(IEnumerable<(bool FiresAlone, IReadOnlyList<bool> WeaponsSighted)> firers)
    {
        ArgumentNullException.ThrowIfNull(firers);
        var all = firers.ToArray();
        return all.Length > 0 && all.All(firer => firer.FiresAlone && firer.WeaponsSighted.Count > 0 && firer.WeaponsSighted.All(sighted => sighted));
    }

    /// <summary>
    public static FireAttack Assemble(AttackAssemblyFacts facts)"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs": [
        ("""    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SnapShot
    {
        get; init;
    }
""",
         """    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SnapShot
    {
        get; init;
    }

    /// <summary>
    /// Whether every MG of the attack fires alone at its Bore Sighted Location (C6.44; pass 45): minus 2 on the IFT DR, against Infantry only in
    /// Defensive First Fire, and never with Spraying Fire, a Snap Shot, or Residual FP. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MgBoreSighted
    {
        get; init;
    }
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""            if (attack.HazardousMovement == true && state.Values.Any(unit => !unit.IsDummy && !unit.Pinned))
            {
                drm.Add(new FireModifier("hazardous-movement", -2m, "A4.62"));
            }
""",
         """            // C6.44 (p. 174; pass 45): a MG's attack on its Bore Sighted Location deducts two, "unless using Spraying Fire or taking a Snap Shot
            // [EXC: vs Infantry, only such an attack conducted as Defensive First Fire (A8.1) qualifies]"; never Residual FP of any form.
            if (attack.MgBoreSighted == true && !residual && attack.FireLane != true && attack.SprayingFire != true && attack.SnapShot != true
                && (attack.FireKind == FirstFire || !state.Values.Any(unit => !unit.IsDummy)))
            {
                drm.Add(new FireModifier("bore-sighting", -2m, "C6.44"));
            }

            if (attack.HazardousMovement == true && state.Values.Any(unit => !unit.IsDummy && !unit.Pinned))
            {
                drm.Add(new FireModifier("hazardous-movement", -2m, "A4.62"));
            }
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""        return (ScenarioA1FireEligibility.Assemble(new AttackAssemblyFacts(state.Phase, side == state.PhasingSide, firers[0].At.ToString(), target.ToString(),
""",
         """        // C6.43, C6.44 (pass 45): a MG fired alone by the unit that set up with it, from its setup Location, at its Bore Sighted Location.
        var boreSighted = state.BoreSights.Count > 0 && ScenarioA1FireEligibility.MgBoreSighted(firerFacts.Select(firer => (firer.UsesInherentFp == false,
            (IReadOnlyList<bool>)[.. (firer.Weapons ?? []).Select(weapon => state.BoreSights.Any(sight => sight.Gun == weapon.EquipmentId && sight.Crew == firer.UnitId
                && sight.Location == target && state.Location(firer.UnitId!)?.Location == sight.SetupLocation))])));
        return (ScenarioA1FireEligibility.Assemble(new AttackAssemblyFacts(state.Phase, side == state.PhasingSide, firers[0].At.ToString(), target.ToString(),
"""),
        ("""            Allies(state, side), [.. selection.Companions.Select(id => Target(state, UnitAt(id), target))])), null);
    }
""",
         """            Allies(state, side), [.. selection.Companions.Select(id => Target(state, UnitAt(id), target))])) with
        {
            MgBoreSighted = boreSighted ? true : null,
        }, null);
    }
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.cs": [
        ("""                var found = sightState.Find(boreGun) as EquipmentInstance;
""",
         """                var found = sightState.Find(boreGun) as EquipmentInstance;

                // C6.41 (pass 45): a MMG or HMG the Scenario Defender possesses Bore Sights too; its record names its possessor and where it set up.
                if (found is { Kind: "asl:mg", Holding: { Role: HoldingRole.Possessed } possession } && sightState.Location(possession.Holder)?.Location is { } mgAt)
                {
                    var mg = found.Definition is { } named ? FireReference.Value.Definitions.GetValueOrDefault(named.Definition) : null;
                    if (ScenarioA1GameStart.MgBoreSightBar(mg?.Size, sightState.Unit(possession.Holder)?.Side == sightState.ScenarioDefender,
                        boreAt.Board == mgAt.Board && boreAt.Hex == mgAt.Hex,
                        () => Los(sightState, mgAt, boreAt) is { Status: LosStatus.Clear, Range: var range } && range <= Math.Min(16, mg?.Range ?? 0), boreGun) is { } mgBar)
                    {
                        return Refused(scope, label, expected, mgBar);
                    }

                    sightEvents.Add(Event(scope, attemptId, setUp.Count + sightEvents.Count + 1, expected, "bore-sighted",
                        new BoreSighted(boreGun, boreAt, possession.Holder, mgAt), ScenarioA1OrdnancePackage.Identity.ToString(), [sightState.ScenarioDefender!]));
                    continue;
                }

"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        ("""            var manned = Active(state, sighted.Gun) is EquipmentInstance { Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition at }
                ? (Holder: manning.Holder, At: at.Location) : ((string Holder, BoardLocation At)?)null;
""",
         """            // C6.41 (pass 45): or a MMG or HMG, recorded with its possessor and the Location they set up in; the planner checks its size.
            var manned = Active(state, sighted.Gun) is EquipmentInstance { Holding: { Role: HoldingRole.Manned } manning, Position: MapPosition at }
                ? (Holder: manning.Holder, At: at.Location)
                : Active(state, sighted.Gun) is EquipmentInstance { Kind: "asl:mg", Holding: { Role: HoldingRole.Possessed } possession }
                    && state.Location(possession.Holder)?.Location is { } heldAt ? (Holder: possession.Holder, At: heldAt) : ((string Holder, BoardLocation At)?)null;
"""),
    ],
    "LimboDancer.Domains.Asl.MapStudio/Components/Play/SetupPlacementEditor.razor": [
        ("""    <label><input id="place-concealed" type="checkbox\"""",
         """    @if (Kind == "asl:mg")
    {
        <LocationField Id="place-bore" Label="Bore Sighted" Value="@Draft.Bore" Title="C6.41, C6.42: a MMG or HMG the Scenario Defender possesses may Bore Sight one Location outside its hex, in its LOS, within its Normal Range and 16 hexes."
            OnChange="@(at => Change(Draft with { Bore = at }))" />
    }
    <label><input id="place-concealed" type="checkbox\""""),
    ],
    "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor": [
        ("""definition.Kind == "asl:gun" && placeBore.Trim().Length > 0 ? placeBore.Trim() : null,""",
         """definition.Kind is "asl:gun" or "asl:mg" && placeBore.Trim().Length > 0 ? placeBore.Trim() : null,"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
