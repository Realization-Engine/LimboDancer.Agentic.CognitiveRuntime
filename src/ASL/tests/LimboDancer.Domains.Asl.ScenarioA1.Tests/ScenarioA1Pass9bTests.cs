using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The backlog pass 9b in the Ordnance package (rulings R9.10, R9.11): the manufactured Russian ATR on the black Vehicle row with AP at the Russian
/// ATR TK#, and the manufactured German Panzerschreck on its own To Hit Table with HEAT at TK# 26 and its X#.
/// </summary>
public sealed class ScenarioA1Pass9bTests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";

    private static FireAttack Hit(string phase, string side, int month) =>
        new(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), month, "open-ground", [], 2, null);

    private static OrdnanceShot Shot(string nationality, string weapon, string firer, string target, string ammunition, int range, int year = 1944, int month = 7,
        string phase = "PFPh")
    {
        var side = phase is "DFPh" or "MPh" ? "non-phasing" : "phasing";
        return new OrdnanceShot(phase, side, nationality, new OrdnanceGun("sw", weapon, false, 0, false, false),
            new OrdnanceCrew("firer", firer, false, false, false, false, false), At, range, 0, false, true, 0, Hit(phase, side, month),
            new OrdnanceRolls(null, null, null, null, null))
        {
            VehicleTarget = new OrdnanceVehicleTarget("target", target, "side", "side", false, false, false, false, true),
            Ammunition = ammunition,
            ScenarioYear = year,
        };
    }

    private static OrdnanceResolution Resolve(OrdnanceShot shot, int[] toHit, int[]? toKill = null) =>
        ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = new OrdnanceRolls(toHit, null, null, null, null) { ToKill = toKill, CrewSurvival = [6, 6], CrewCheck = [3, 4], ShockCheck = [3, 4] } }, Reference);

    [Fact]
    public void AnAtrHitsOnTheBlackVehicleRowAndKillsWithTheRussianAtrTk()
    {
        // C13.2, C13.22 (R9.10): black 10 at 3 hexes for the Russian ATR; a hull hit meets AP TK# 6, the Russian ATR entry of p. 701.
        var result = Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "ap", 3), [3, 2], [2, 2]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(("black", 10), (result.ToHit!.Color, result.ToHit.BasicToHit));
        Assert.Equal(6, result.Kill!.BasicTk);
        Assert.Null(result.ToHit.Drm.FirstOrDefault(item => item.Name == "case-l"));

        // AP only, up to 12 hexes; an Original 11 malfunctions it.
        Assert.Contains("asl.a1.ordnance.ammunition-outside", Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "heat", 3), [3, 2]).Reasons);
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "ap", 13), [3, 2]).Reasons);
        Assert.True(Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "ap", 3), [5, 6]).Gun!.Malfunctioned);

        // Against a truck, the unarmored AP value of a weapon of 28mm or less (C7.311).
        Assert.Equal(7, Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-truck", "ap", 3), [3, 2], [6, 6]).Kill!.FinalTk);
    }

    [Fact]
    public void APanzerschreckReadsItsOwnTableAndKillsWithHeatTk26()
    {
        // C13.42, C13.48 (R9.11): 9 at 2 hexes; HEAT TK# 26 on the PSK row; no Multiple ROF.
        var result = Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 2), [3, 2], [2, 2]);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal((9, 9), (result.ToHit!.BasicToHit, result.ToHit.ModifiedToHit));
        Assert.Equal(26, result.Kill!.BasicTk);
        Assert.Equal(0, result.Gun!.RateOfFire);

        // Not before September 1943, not beyond its table, not by a lone SMC; an Original 11 removes it (C13.47).
        Assert.Contains("asl.a1.ordnance.latw-date-outside", Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 2, 1943, 8), [3, 2]).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 2, 1943, 9), [3, 2], [6, 6]).Disposition);
        Assert.Contains("asl.a1.ordnance.out-of-range", Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 5), [3, 2]).Reasons);
        Assert.Contains("asl.a1.ordnance.crew-outside", Resolve(Shot("german", "attacker-psk", "attacker-leader-8-1", "defender-tank", "heat", 2), [3, 2]).Reasons);
        Assert.True(Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 2), [6, 5]).Gun!.Malfunctioned);
    }

    [Fact]
    public void APanzerschreckTakesTheBackblastAndAnAtrDoesNot()
    {
        // C13.8 (R9.11): +2 from a ground-level building for the PSK, none for the ATR; both take +2 in the AFPh (C13.1).
        var psk = Resolve(Shot("german", "attacker-psk", "attacker-squad", "defender-tank", "heat", 2, phase: "AFPh") with { Panzerfaust = new OrdnancePanzerfaust(null, null, true, null) }, [3, 2], [6, 6]);
        Assert.Equal(2, psk.ToHit!.Drm.Count(item => item.Name.StartsWith("case-c3", StringComparison.Ordinal)));
        var atr = Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "ap", 3, phase: "AFPh") with { Panzerfaust = new OrdnancePanzerfaust(null, null, true, null) }, [3, 2], [6, 6]);
        Assert.Single(atr.ToHit!.Drm, item => item.Name.StartsWith("case-c3", StringComparison.Ordinal));
    }

    [Fact]
    public void InexperiencedHandsLowerTheBreakdownAndXNumberByOne()
    {
        // A19.32 (referee, pass 9b): a Conscript squad's ATR malfunctions on 10, a Conscript squad's PSK is removed on 10; a 1st Line squad's do not.
        Assert.True(Resolve(Shot("russian", "defender-atr", "defender-conscript-squad", "attacker-tank", "ap", 3), [4, 6], [6, 6]).Gun!.Malfunctioned);
        Assert.False(Resolve(Shot("russian", "defender-atr", "defender-squad", "attacker-tank", "ap", 3), [4, 6], [6, 6]).Gun!.Malfunctioned);
        Assert.Equal(10, Resolve(Shot("german", "attacker-psk", "attacker-conscript-squad", "defender-tank", "heat", 2), [4, 6], [6, 6]).Gun!.BreakdownNumber);
    }
}
