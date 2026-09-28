using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package as the backlog pass 6 revises it: an AFV's or wreck's +1 TEM for Infantry, with no FFMO (D9.3, D10.3, A4.6; R6.1);
/// Residual FP against a truck and an AFV's Vulnerable crew (A8.2, A8.222; R6.6); a concealed vehicle on the halved FP's column (A12.2,
/// A12.13; R6.7); and a vehicle's AAMG firing in the MPh as Defensive or Bounding First Fire (D3.3; R6.9).
/// </summary>
public sealed class ScenarioA1Pass6Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string location = From) => new(id, "defender-squad", location, false, false, false, false, false);

    private static FireTarget German(string id) => new(id, "attacker-squad", At, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, string terrain = "open-ground", FireFirer[]? firers = null, FireTarget[]? targets = null) =>
        new("PFPh", "phasing", true, From, At, firers ?? [Firer("ru-1"), Firer("ru-2")], null, 1, true, new FireLos(false, 0, true, false), null,
            terrain, targets ?? [German("de-s")], 3, new FireRolls(dice, null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [6, 6] }, null));

    private static FireAttack Moving(FireAttack attack, string kind) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = kind,
        TargetMovement = new FireMovement(false),
    };

    private static IEnumerable<(string Name, decimal Value)> Drm(FireResolution result) => result.Arithmetic!.Drm.Select(item => (item.Name, item.Value));

    private static FireVehicle Vehicle(string id, string definition, bool crewExposed = true) => new(id, definition, At, crewExposed, false, false, false);

    private static FireAttack Residual(int fp, int[] dice, FireVehicle[] vehicles) => Moving(Attack(dice, targets: []) with
    {
        Firers = null,
        Director = null,
        FireGroupComplete = null,
        FirerLocationId = null,
        Range = null,
        SameLevel = null,
        Los = null,
        ResidualFp = fp,
        Vehicles = vehicles,
        Rolls = new FireRolls(dice, null, null, null),
    }, ScenarioA1FireCalculator.ResidualFire);

    private static FireAttack Halftrack(string phase, string side, string? kind, bool motion) =>
        new FireAttack(phase, side, true, From, At, [], null, 3, true, new FireLos(false, 0, true, false), null, "open-ground", [German("de-s") with { UnitId = "ru-s", DefinitionId = "defender-squad" }],
            3, new FireRolls([6, 5], null, null, null))
        {
            FireKind = kind,
            TargetMovement = side == "non-phasing" && phase == "MPh" ? new FireMovement(false) : null,
            VehicleFire = new FireVehicleFire("de-ht", "attacker-halftrack", From, true, motion, false, false, false, false, false, false),
        };

    [Fact]
    public void AWreckOrFriendlyAfvGivesInfantryOneTemAndCancelsFfmo()
    {
        // Defensive First Fire at a squad moving in Open Ground beside its AFV: -1 FFNAM, and +1 for the AFV instead of -1 FFMO (D9.3, A4.6).
        var covered = ScenarioA1FireCalculator.Resolve(Moving(Attack([6, 5]), ScenarioA1FireCalculator.FirstFire) with { AfvCover = "de-ht" }, Reference);
        Assert.True(covered.Disposition == FireResolution.Resolved, string.Join("; ", covered.Reasons));
        Assert.Equal([("afv-cover:de-ht", 1m), ("ffnam", -1m)], Drm(covered));

        // Woods' +1 TEM is a positive TEM, so the AFV adds nothing; fire from within the Location gets no cover.
        var woods = ScenarioA1FireCalculator.Resolve(Attack([6, 5], terrain: "woods") with { AfvCover = "de-ht", Rolls = new FireRolls([6, 5], null, null, null) }, Reference);
        Assert.Equal([("tem:woods", 1m)], Drm(woods));
        var within = ScenarioA1FireCalculator.Resolve(Attack([6, 5], firers: [Firer("ru-1", At)]) with { FirerLocationId = At, AfvCover = "de-ht", Range = 0, Rolls = new FireRolls([6, 5], null, null, null) }, Reference);
        Assert.DoesNotContain(within.Arithmetic?.Drm ?? [], item => item.Name.StartsWith("afv-cover:", StringComparison.Ordinal));
        Assert.Null(ScenarioA1FireCalculator.Cover(Attack([6, 5], firers: [Firer("ru-1", At)]) with { AfvCover = "de-ht" }));
    }

    [Theory]
    [InlineData(2, 3, FireVehicleEffect.Immobilized)]
    [InlineData(1, 3, FireVehicleEffect.Eliminated)]
    [InlineData(6, 5, FireVehicleEffect.None)]
    public void ResidualFpAttacksATruckOnTheVehicleLine(int colored, int white, string expected)
    {
        // Residual FP 4: the 4 column's Kill Number 5 (A8.2, A7.308; R6.6), never Cowering (A8.224).
        var result = ScenarioA1FireCalculator.Resolve(Residual(4, [colored, white], [Vehicle("de-t", "attacker-truck")]), Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        var effect = Assert.Single(result.VehicleEffects!);
        Assert.Equal((expected, 5), (effect.Result, effect.KillNumber));
    }

    [Fact]
    public void ResidualFpTakesTheSmokeOfTheTargetLocation()
    {
        // A8.2 (referee D2): a burning wreck's +2 smoke in the Location applies to Residual FP, and cancels FFMO (A24.2).
        var result = ScenarioA1FireCalculator.Resolve(Residual(4, [6, 5], [Vehicle("de-t", "attacker-truck")]) with { Los = new FireLos(false, 2, true, false) }, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        Assert.Contains(Drm(result), item => item == ("los-hindrance", 2m));
        Assert.DoesNotContain(Drm(result), item => item.Name == "ffmo");
    }

    [Fact]
    public void ResidualFpAttacksAnAfvsCeCrewCollaterally()
    {
        // A8.222: the halftrack is unharmed; its CE crew takes the Residual FP with +2 CE (D5.31); a BU crew is not Vulnerable.
        var exposed = ScenarioA1FireCalculator.Resolve(Residual(8, [1, 2], [Vehicle("de-ht", "attacker-halftrack")]) with
        {
            Rolls = new FireRolls([1, 2], null, null, null) { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-ht"] = [6, 6] } },
        }, Reference);
        Assert.True(exposed.Disposition == FireResolution.Resolved, string.Join("; ", exposed.Reasons));
        var effect = Assert.Single(exposed.VehicleEffects!);
        Assert.Equal((FireVehicleEffect.None, 5), (effect.Result, effect.FinalDr));
        Assert.Contains(effect.Drm, item => item.Name == "crew-exposed");

        var buttoned = ScenarioA1FireCalculator.Resolve(Residual(8, [1, 2], [Vehicle("de-ht", "attacker-halftrack", crewExposed: false)]), Reference);
        Assert.Equal(FireVehicleEffect.NotVulnerable, Assert.Single(buttoned.VehicleEffects!).CrewResult);
    }

    [Fact]
    public void AConcealedVehicleIsAttackedOnTheHalvedFpsColumn()
    {
        // Two 4-4-7 adjacent: 16 FP at PBF; at a concealed truck alone, 8 FP (A12.13): the 8 column, Kill Number 7 (R6.7).
        var attack = Attack([3, 4], targets: []) with
        {
            Vehicles = [Vehicle("de-t", "attacker-truck") with { Concealed = true }],
            Rolls = new FireRolls([3, 4], null, null, null),
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        Assert.Equal((8, 7), (result.Arithmetic!.ColumnFp, Assert.Single(result.VehicleEffects!).KillNumber));

        var seen = ScenarioA1FireCalculator.Resolve(attack with { Vehicles = [Vehicle("de-t", "attacker-truck")] }, Reference);
        Assert.Equal(16, seen.Arithmetic!.ColumnFp);
    }

    [Fact]
    public void AVehiclesAamgFiresAsDefensiveOrBoundingFirstFire()
    {
        // D3.3, A8.1 (R6.9): as the DEFENDER in the MPh, at a moving squad, with FFNAM and FFMO; moving in its own MPh, halved in Motion (D2.42)
        // and marked with Bounding Fire.
        var defensive = ScenarioA1FireCalculator.Resolve(Halftrack("MPh", "non-phasing", ScenarioA1FireCalculator.FirstFire, false), Reference);
        Assert.True(defensive.Disposition == FireResolution.Resolved, string.Join("; ", defensive.Reasons));
        Assert.Equal("first-fire", defensive.FireCounter);
        Assert.Contains(Drm(defensive), item => item.Name == "ffmo");

        var bounding = ScenarioA1FireCalculator.Resolve(Halftrack("MPh", "phasing", ScenarioA1FireCalculator.BoundingFirstFire, true), Reference);
        Assert.True(bounding.Disposition == FireResolution.Resolved, string.Join("; ", bounding.Reasons));
        Assert.Equal("bounding-fire", bounding.FireCounter);

        // D3.31: Bounding Fire halves the MG, and D2.42 halves it again while the vehicle is Non-Stopped (referee D1).
        var stopped = ScenarioA1FireCalculator.Resolve(Halftrack("MPh", "phasing", ScenarioA1FireCalculator.BoundingFirstFire, false), Reference);
        Assert.Equal((3m, 1.5m, 0.75m), (defensive.Arithmetic!.TotalFirepower, stopped.Arithmetic!.TotalFirepower, bounding.Arithmetic!.TotalFirepower));
        Assert.Contains(stopped.Arithmetic.Firers.Single().Multipliers, item => item is { Name: "bounding-fire", Rule: "D3.31" });

        // Subsequent First Fire and FPF are Infantry's; Bounding First Fire is the phasing side's.
        Assert.Contains("asl.a1.fire.phase-outside", ScenarioA1FireCalculator.Resolve(Halftrack("MPh", "non-phasing", ScenarioA1FireCalculator.SubsequentFirstFire, false), Reference).Reasons);
        Assert.Contains("asl.a1.fire.phase-outside", ScenarioA1FireCalculator.Resolve(Halftrack("MPh", "non-phasing", ScenarioA1FireCalculator.BoundingFirstFire, false), Reference).Reasons);
    }
}
