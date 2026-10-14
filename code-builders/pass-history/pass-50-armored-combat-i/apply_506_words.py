"""Pass 50, task 50.6: the proposal's and the record's words for a shot from inside the target's hex, and the tests."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
e.edit("LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs",
       """                var aim = shot?.VehicleTarget is { } aimed ? $"{(shot.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {fired.Target} (hull {aimed.HullFacing}"
                    + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : "") + " toward the firer)\"""",
       """                // D3.2 (pass 50): from inside the target's hex the colored dr gives the Target Facing.
                var aim = shot?.VehicleTarget is { } aimed ? $"{(shot.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {fired.Target} ("
                    + (shot.SameHex == true ? "in its own hex: the Target Facing by the colored dr, D3.2)"
                        : $"hull {aimed.HullFacing}" + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : "") + " toward the firer)")""")
e.edit("LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs",
       """            ? $"{(facts.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {facts.TargetLocationId} (Vehicle Target Type; hull {aimed.HullFacing}"
                + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : string.Empty) + " facing the firer)\"""",
       """            ? $"{(facts.Ammunition ?? "ap").ToUpperInvariant()} at {aimed.VehicleId} in {facts.TargetLocationId} (Vehicle Target Type; "
                + (facts.SameHex == true ? "in its own hex, so the colored dr gives the Target Facing: 1-2 rear, 3-4 side, 5-6 front, D3.2)"
                    : $"hull {aimed.HullFacing}" + (aimed.TurretFacing is { } turretFacing ? $", turret {turretFacing}" : string.Empty) + " facing the firer)")""")

R = "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass50RulesTests.cs"
e.edit(R, """        Assert.Contains("doubles", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, true, true), StringComparison.Ordinal);
    }
}
""", """        Assert.Contains("doubles", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, true, true), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "rear")]
    [InlineData(2, "rear")]
    [InlineData(3, "side")]
    [InlineData(4, "side")]
    [InlineData(5, "front")]
    [InlineData(6, "front")]
    public void FromInsideTheTargetsHexTheColoredDrGivesTheTargetFacing(int colored, string facing)
    {
        // D3.2: "the colored dr of the To Hit DR determines the Target Facing: 1-2 Rear, 3-4 Side, 5-6 Front".
        Assert.Equal(facing, ScenarioA1ArmorCalculator.SameHexFacing(colored));
        var shot = MaShot(phase: "PFPh", kind: null) with
        {
            Range = 0,
            SameHex = true,
            VehicleTarget = new OrdnanceVehicleTarget("target-1", "defender-tank", null, null, false, false, false, false, true),
            Rolls = new OrdnanceRolls([colored, 1], null, null, null, null),
        };
        var result = Shoot(shot);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(facing, result.Kill!.TargetFacing);

        // C5.5: Case E, +2, doubled in woods, a building, or rubble; Case L does not apply to it.
        Assert.Equal(2m, Assert.Single(result.ToHit!.Drm, item => item.Name == "case-e").Value);
        Assert.Equal(4m, Assert.Single(Shoot(shot with { FirerInWoodsOrBuilding = true }).ToHit!.Drm, item => item.Name == "case-e").Value);
        Assert.DoesNotContain(result.ToHit.Drm, item => item.Name == "case-l");

        // A shot from inside the hex names no facing of its own, and one from outside it must.
        Assert.Contains("asl.a1.ordnance.vehicle-target-outside", Shoot(shot with { VehicleTarget = shot.VehicleTarget! with { HullFacing = "front" } }).Reasons);
        Assert.NotEqual(OrdnanceResolution.Resolved, Shoot(shot with { SameHex = null, Range = 1 }).Disposition);
    }
}
""")
e.apply()
