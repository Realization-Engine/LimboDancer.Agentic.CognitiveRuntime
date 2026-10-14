"""Pass 50, task 50.6: the Target Facing of a vehicle fired on from inside its own hex (D3.2, C5.5)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
RULES = "LimboDancer.Domains.Asl.Rules/"
C = RULES + "ScenarioA1OrdnanceCalculator.cs"
A = RULES + "ScenarioA1ArmorCalculator.cs"
G = "LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs"

e.edit(C, """        // C5.51's Defensive First Fire in the Gun's own hex, turning its CA with Case A, is not built (ruling R8.8).
        if ((sameHex ? shot.Range != 0 || shot.VehicleTarget is not null || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw : shot.Range < 1)""",
       """        // C5.51's Defensive First Fire in the Gun's own hex, turning its CA with Case A, is not built (ruling R8.8).
        // D3.2, C5.5 (pass 50): a vehicle in the firer's own Location is fired on with Case E, its Target Facing by the colored dr.
        if ((sameHex ? shot.Range != 0 || shot.HexspinesToTurn != 0 || shot.FireKind is not null || mortar || latw : shot.Range < 1)""")

e.edit(A, """        need(target.DefinitionId, "vehicleTarget.definitionId");
        need(target.HullFacing, "vehicleTarget.hullFacing");
""", """        need(target.DefinitionId, "vehicleTarget.definitionId");

        // D3.2 (pass 50): from inside the target's hex the colored dr of the To Hit DR gives the Target Facing, so none is read from the map.
        if (shot.SameHex != true)
        {
            need(target.HullFacing, "vehicleTarget.hullFacing");
        }

""")
e.edit(A, """        if (!reference.Armor.Vehicles.TryGetValue(target.DefinitionId!, out var armor) || armor.Nationality == gun.Nationality
            || target.HullFacing is not ("front" or "side" or "rear") || (armor.Turreted && target.TurretFacing is not ("front" or "side" or "rear"))
            || shot.Hit!.Targets is { Count: > 0 })
""", """        // D3.2 (pass 50): a shot from inside the target's hex names no Target Facing, and one from outside it names the hull's, and the turret's too.
        if (!reference.Armor.Vehicles.TryGetValue(target.DefinitionId!, out var armor) || armor.Nationality == gun.Nationality
            || (shot.SameHex == true ? target.HullFacing is not null || target.TurretFacing is not null
                : target.HullFacing is not ("front" or "side" or "rear") || (armor.Turreted && target.TurretFacing is not ("front" or "side" or "rear")))
            || shot.Hit!.Targets is { Count: > 0 })
""")
e.edit(A, """        if (target.Concealed == true)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }
""", """        // C5.5 (p. 172; pass 50): "any Gun firing at a target within its own hex must add the +2 DRM of Case E", doubled in woods, a building, or
        // rubble; "Cases J3, J4, L, and M are not applicable".
        var sameHex = shot.SameHex == true;
        if (sameHex)
        {
            drm.Add(new FireModifier("case-e", woods ? 4 : 2, "C5.5"));
        }

        if (target.Concealed == true)
        {
            drm.Add(new FireModifier("case-k", 2, "C6.2"));
        }
""")
e.edit(A, """        if ((!latw || gun.LatwType == "atr") && range <= 2 && target.NonStopped != true && shot.Vehicle?.InMotion != true)
""", """        if (!sameHex && (!latw || gun.LatwType == "atr") && range <= 2 && target.NonStopped != true && shot.Vehicle?.InMotion != true)
""")
e.edit(A, """        if (!latw && shot.BoreSighted == true)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
""", """        if (!latw && shot.BoreSighted == true && !sameHex)
        {
            drm.Add(new FireModifier("case-m", -2, "C6.4"));
        }
""")
e.edit(A, """        var location = armor.Turreted && (improbable is { } subsequent ? subsequent == 2 : dice[0] < dice[1]) ? "turret" : "hull";
        var facing = location == "turret" ? target.TurretFacing ?? target.HullFacing! : target.HullFacing!;
""", """        var location = armor.Turreted && (improbable is { } subsequent ? subsequent == 2 : dice[0] < dice[1]) ? "turret" : "hull";

        // D3.2 (p. 199; pass 50): "if the fire originates from within the target hex (Case E To Hit DRM; C5.5) the colored dr of the To Hit DR
        // determines the Target Facing: 1-2 Rear, 3-4 Side, 5-6 Front", for the hull and the turret alike.
        var facing = shot.SameHex == true ? SameHexFacing(dice[0]) : location == "turret" ? target.TurretFacing ?? target.HullFacing! : target.HullFacing!;
""")
e.edit(A, """    /// <summary>The facts a Vehicle Target Type shot needs beyond an Infantry Target Type shot's.</summary>
""", """    /// <summary>D3.2 (p. 199; pass 50): the Target Facing of a vehicle fired on from inside its own hex, by the colored dr of the To Hit DR: 1-2 rear, 3-4 side, 5-6 front.</summary>
    public static string SameHexFacing(int coloredDr) => coloredDr <= 2 ? "rear" : coloredDr <= 4 ? "side" : "front";

    /// <summary>The facts a Vehicle Target Type shot needs beyond an Infantry Target Type shot's.</summary>
""")

e.edit(G, """            if (firer is not EquipmentInstance || shot.VehicleTarget is not null || ReadLocation(state, target) is not { } ownRead || TerrainKey(ownRead) is not { } ownTerrain)
            {
                return (null, "play.ordnance-own-hex: only a Gun fires within its own Location, at Infantry (C5.5)");
            }

            // C5.5: a wreck, SMOKE, or an AFV in the hex adds its Hindrance (Case R); the firer is a Gun and the target Infantry, so every one counts.
            var ownHindrance = ScenarioA1VehicleSightRules.OwnHexHindrance(
                SmokeSources(state).Count(place => place.Board == target.Board && place.Hex == target.Hex),
                state.Units.Any(unit => (unit.Status == InstanceStatus.Active ? IsAfv(unit) : unit.Status == InstanceStatus.Wrecked && !IsBurning(state, unit))
                    && state.Location(unit.Id)?.Location == target && Standing(state, unit)));
""", """            // D3.2, C5.5 (pass 50): a Gun, or a tank's MA, also fires at a vehicle in its own Location; Infantry there is still a Gun's target alone.
            if ((firer is not EquipmentInstance && shot.VehicleTarget is null) || ReadLocation(state, target) is not { } ownRead || TerrainKey(ownRead) is not { } ownTerrain)
            {
                return (null, "play.ordnance-own-hex: within its own Location a Gun fires at Infantry or a vehicle, and a tank's MA at a vehicle (C5.5, D3.2)");
            }

            // C5.5: a wreck, SMOKE, or an AFV in the hex adds its Hindrance (Case R), "other than the target or firer".
            var ownHindrance = ScenarioA1VehicleSightRules.OwnHexHindrance(
                SmokeSources(state).Count(place => place.Board == target.Board && place.Hex == target.Hex),
                state.Units.Any(unit => unit.Id != shot.VehicleTarget?.VehicleId && unit.Id != firer.Id
                    && (unit.Status == InstanceStatus.Active ? IsAfv(unit) : unit.Status == InstanceStatus.Wrecked && !IsBurning(state, unit))
                    && state.Location(unit.Id)?.Location == target && Standing(state, unit)));
""")
e.apply()
