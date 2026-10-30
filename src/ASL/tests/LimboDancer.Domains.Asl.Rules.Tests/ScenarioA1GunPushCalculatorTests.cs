using System.Text.Json.Nodes;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The C10.3 claim (C10.3 and C10.31 p. 181 and the C10.3 chart; C10.2, C10.25, C10.26 p. 180; E3.61, E3.64 p. 230; E3.733, E3.7332 p. 231; B24.8
/// p. 143; the claim's design, rulings RC10.1 to RC10.12, with the review's findings C1 and C2): the Manhandling DRM term by term, the result and what
/// follows it, Labor, the push bars, the hook-up after a push with its weather floor, the forwards of the old members (each equal to a copy of its old
/// expression), and the two contracts with their hidden-information cases.
/// </summary>
public sealed class ScenarioA1GunPushCalculatorTests
{
    private static readonly bool[] Both = [false, true];

    private static ManhandlingDrmFacts Drm(int mf = 2, int hexTem = 0, int hexsideTem = 0, bool mud = false, bool deepSnow = false, bool road = false,
        bool paved = false, bool plowed = false, int crews = 0, int squads = 0, bool onlySquad = false, bool lowAmmo = false, int labor = 0) =>
        new(mf, hexTem, hexsideTem, mud, deepSnow, road, paved, plowed, crews, squads, onlySquad, lowAmmo, labor);

    private static GunPushFacts Push(bool qsu = true, bool limbered = false, bool nm = false, bool rfnm = false, bool unhooked = false, bool bypass = false,
        bool assault = false, bool minimumMove = false, bool pp = false, bool wire = false, bool deepStream = false, bool bamboo = false, bool tb = false,
        ManhandlingResult? last = null, int entryHalfMf = 4, IReadOnlyList<int?>? pushers = null) =>
        new(qsu, limbered, nm, rfnm, unhooked, bypass, assault, minimumMove, pp, wire, deepStream, bamboo, tb, last, entryHalfMf, pushers ?? [8]);

    private static HookAfterPushFacts Hook(bool pushed = true, ManhandlingResult? last = ManhandlingResult.Enter, IReadOnlyList<int?>? pushers = null,
        bool mud = false, bool deepSnow = false, bool roadExit = false, bool vehicleMp = true) =>
        new(pushed, last, pushers ?? [4], mud, deepSnow, roadExit, vehicleMp);

    // The DRM (C10.3's chart).

    [Fact]
    public void TheMfAndTheTemOfTheHexAndItsHexsideAreAdded()
    {
        // +Y: Open Ground at 1 MF, doubled to 2 MF.
        Assert.Equal(2, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm()));

        // The C10.3 EX: a 37L AT into a stone building from Open Ground, +3 TEM and +4 MF, +7.
        Assert.Equal(7, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(mf: 4, hexTem: 3)));

        // +X of the hexside: a wall +2, a hedge +1 (B9.3).
        Assert.Equal(4, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(hexsideTem: 2)));
        Assert.Equal(3, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(hexsideTem: 1)));

        // Only a positive TEM counts.
        Assert.Equal(2, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(hexTem: -1, hexsideTem: -2)));
    }

    [Fact]
    public void TheRoadMinusTwoFollowsTheWeatherAndThePavedOrPlowedRoad()
    {
        // Clear and Ground Snow (C10.3 names only mud and deep snow): -2.
        Assert.True(ScenarioA1GunPushCalculator.RoadDrmApplies(true, false, false, false, false));
        Assert.Equal(0, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(road: true)));

        // Mud: lost off an unpaved road, kept on a paved one, the +3 added either way (RC10.2).
        Assert.False(ScenarioA1GunPushCalculator.RoadDrmApplies(true, true, false, false, false));
        Assert.True(ScenarioA1GunPushCalculator.RoadDrmApplies(true, true, false, true, false));
        Assert.Equal(5, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(road: true, mud: true)));
        Assert.Equal(3, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(road: true, mud: true, paved: true)));

        // Deep Snow: lost off an unplowed road, kept on a plowed one.
        Assert.False(ScenarioA1GunPushCalculator.RoadDrmApplies(true, false, true, false, false));
        Assert.True(ScenarioA1GunPushCalculator.RoadDrmApplies(true, false, true, false, true));
        Assert.False(ScenarioA1GunPushCalculator.RoadDrmApplies(true, false, true, true, false));
        Assert.Equal(5, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(road: true, deepSnow: true)));
        Assert.Equal(3, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(road: true, deepSnow: true, plowed: true)));

        // No road hexside crossed, no -2.
        Assert.False(ScenarioA1GunPushCalculator.RoadDrmApplies(false, false, false, true, true));
    }

    [Fact]
    public void MudAndDeepSnowAddThree()
    {
        Assert.Equal(0, ScenarioA1GunPushCalculator.WeatherDrm(false, false));
        Assert.Equal(3, ScenarioA1GunPushCalculator.WeatherDrm(true, false));
        Assert.Equal(3, ScenarioA1GunPushCalculator.WeatherDrm(false, true));
        Assert.Equal(3, ScenarioA1GunPushCalculator.WeatherDrm(true, true));
        Assert.Equal(5, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(mud: true)));
        Assert.Equal(5, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(deepSnow: true)));
    }

    [Fact]
    public void ExtraPushersLowAmmoAndLaborSubtract()
    {
        Assert.Equal(0, ScenarioA1GunPushCalculator.ExtraPushersDrm(0, 0, false));
        Assert.Equal(-1, ScenarioA1GunPushCalculator.ExtraPushersDrm(1, 0, false));
        Assert.Equal(-4, ScenarioA1GunPushCalculator.ExtraPushersDrm(0, 2, false));
        Assert.Equal(-4, ScenarioA1GunPushCalculator.ExtraPushersDrm(0, 3, false));
        Assert.Equal(-4, ScenarioA1GunPushCalculator.ExtraPushersDrm(int.MaxValue, int.MaxValue, true));
        Assert.Equal(-1, ScenarioA1GunPushCalculator.ExtraPushersDrm(0, 0, true));
        Assert.Equal(0, ScenarioA1GunPushCalculator.ExtraPushersDrm(-3, -1, false));
        Assert.Equal(1, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(crews: 1)));

        Assert.Equal(0, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(lowAmmo: true)));

        Assert.Equal(0, ScenarioA1GunPushCalculator.LaborDrm(0));
        Assert.Equal(-1, ScenarioA1GunPushCalculator.LaborDrm(1));
        Assert.Equal(-2, ScenarioA1GunPushCalculator.LaborDrm(2));
        Assert.Equal(-2, ScenarioA1GunPushCalculator.LaborDrm(3));
        Assert.Equal(0, ScenarioA1GunPushCalculator.LaborDrm(-1));
        Assert.Equal(0, ScenarioA1GunPushCalculator.ManhandlingDrm(Drm(labor: 2)));
    }

    // The result and what follows it.

    [Fact]
    public void TheResultIsReadAgainstTheMNumber()
    {
        Assert.Equal(ManhandlingResult.Enter, ScenarioA1GunPushCalculator.Result(9, 10));
        Assert.Equal(ManhandlingResult.EnterStop, ScenarioA1GunPushCalculator.Result(10, 10));
        Assert.Equal(ManhandlingResult.Stay, ScenarioA1GunPushCalculator.Result(11, 10));

        Assert.Equal(ManhandlingResult.Enter, ScenarioA1GunPushCalculator.ResultOf("enter"));
        Assert.Equal(ManhandlingResult.EnterStop, ScenarioA1GunPushCalculator.ResultOf("enter-stop"));
        Assert.Equal(ManhandlingResult.Stay, ScenarioA1GunPushCalculator.ResultOf("stay"));
        Assert.Null(ScenarioA1GunPushCalculator.ResultOf(null));
        Assert.Null(ScenarioA1GunPushCalculator.ResultOf("none"));
    }

    [Fact]
    public void EachResultFollowsAsC103Says()
    {
        Assert.Equal(new PushOutcome(true, true, true, false, true), ScenarioA1GunPushCalculator.Follows(ManhandlingResult.Enter));
        Assert.Equal(new PushOutcome(true, false, false, false, true), ScenarioA1GunPushCalculator.Follows(ManhandlingResult.EnterStop));
        Assert.Equal(new PushOutcome(false, false, false, true, true), ScenarioA1GunPushCalculator.Follows(ManhandlingResult.Stay));
    }

    [Fact]
    public void LaborRisesToTwoOnFailureAndClearsOnEntry()
    {
        Assert.Equal(1, ScenarioA1GunPushCalculator.LaborAfter(0, ManhandlingResult.Stay));
        Assert.Equal(2, ScenarioA1GunPushCalculator.LaborAfter(1, ManhandlingResult.Stay));
        Assert.Equal(2, ScenarioA1GunPushCalculator.LaborAfter(2, ManhandlingResult.Stay));
        Assert.Equal(1, ScenarioA1GunPushCalculator.LaborAfter(-1, ManhandlingResult.Stay));
        Assert.Equal(0, ScenarioA1GunPushCalculator.LaborAfter(2, ManhandlingResult.Enter));
        Assert.Equal(0, ScenarioA1GunPushCalculator.LaborAfter(1, ManhandlingResult.EnterStop));
    }

    // The push bars (RC10.1, RC10.5).

    [Fact]
    public void EachBarRefusesAloneAndNoneAdmits()
    {
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push()));
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push(qsu: false, limbered: true)));
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push(last: ManhandlingResult.Enter)));
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push(deepStream: true, tb: true)));
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push(bamboo: true, tb: true)));
        Assert.Null(ScenarioA1GunPushCalculator.PushRefusal(Push(entryHalfMf: 8, pushers: [8, 9])));

        Assert.Contains("NM or RFNM", ScenarioA1GunPushCalculator.PushRefusal(Push(nm: true)), StringComparison.Ordinal);
        Assert.Contains("NM or RFNM", ScenarioA1GunPushCalculator.PushRefusal(Push(rfnm: true, limbered: true)), StringComparison.Ordinal);
        Assert.Contains("only limbered", ScenarioA1GunPushCalculator.PushRefusal(Push(qsu: false)), StringComparison.Ordinal);
        Assert.Contains("unhooked", ScenarioA1GunPushCalculator.PushRefusal(Push(unhooked: true)), StringComparison.Ordinal);
        Assert.Contains("Bypass", ScenarioA1GunPushCalculator.PushRefusal(Push(bypass: true)), StringComparison.Ordinal);
        Assert.Equal("play.move-push: pushing a Gun prevents Assault Movement (C10.3)", ScenarioA1GunPushCalculator.PushRefusal(Push(assault: true)));
        Assert.Contains("Minimum Move", ScenarioA1GunPushCalculator.PushRefusal(Push(minimumMove: true)), StringComparison.Ordinal);
        Assert.Contains("PP", ScenarioA1GunPushCalculator.PushRefusal(Push(pp: true)), StringComparison.Ordinal);
        Assert.Contains("Wire or Panji", ScenarioA1GunPushCalculator.PushRefusal(Push(wire: true)), StringComparison.Ordinal);
        Assert.Contains("deep stream", ScenarioA1GunPushCalculator.PushRefusal(Push(deepStream: true)), StringComparison.Ordinal);
        Assert.Contains("Bamboo", ScenarioA1GunPushCalculator.PushRefusal(Push(bamboo: true)), StringComparison.Ordinal);
        Assert.Contains("equalled the M#", ScenarioA1GunPushCalculator.PushRefusal(Push(last: ManhandlingResult.EnterStop)), StringComparison.Ordinal);
        Assert.Contains("above the M#", ScenarioA1GunPushCalculator.PushRefusal(Push(last: ManhandlingResult.Stay)), StringComparison.Ordinal);

        // C10.3's MF sentence reads every Infantry that has pushed this Player Turn (review C2): one short of the hex's cost refuses, as does one unknown.
        Assert.Contains("MF left to expend", ScenarioA1GunPushCalculator.PushRefusal(Push(entryHalfMf: 6, pushers: [8, 5])), StringComparison.Ordinal);
        Assert.Contains("must be known", ScenarioA1GunPushCalculator.PushRefusal(Push(pushers: [8, null])), StringComparison.Ordinal);
        Assert.Contains("must be known", ScenarioA1GunPushCalculator.PushRefusal(Push(pushers: [])), StringComparison.Ordinal);

        // The order: the Gun's own bars come first.
        Assert.Contains("NM or RFNM", ScenarioA1GunPushCalculator.PushRefusal(Push(nm: true, bypass: true, last: ManhandlingResult.Stay)), StringComparison.Ordinal);
    }

    [Fact]
    public void APushOnIsRefusedAfterAStopOrAFailure()
    {
        Assert.Null(ScenarioA1GunPushCalculator.PushOnRefusal(null));
        Assert.Null(ScenarioA1GunPushCalculator.PushOnRefusal(ManhandlingResult.Enter));
        Assert.NotNull(ScenarioA1GunPushCalculator.PushOnRefusal(ManhandlingResult.EnterStop));
        Assert.NotNull(ScenarioA1GunPushCalculator.PushOnRefusal(ManhandlingResult.Stay));
    }

    // The hook-up after a push (C10.31; RC10.4, review C1 and C2).

    [Fact]
    public void TheFloorFollowsTheWeather()
    {
        Assert.Equal(4, ScenarioA1GunPushCalculator.PushFloorHalfMf(false, false, false));
        Assert.Equal(4, ScenarioA1GunPushCalculator.PushFloorHalfMf(false, false, true));
        Assert.Equal(6, ScenarioA1GunPushCalculator.PushFloorHalfMf(true, false, false));
        Assert.Equal(6, ScenarioA1GunPushCalculator.PushFloorHalfMf(false, true, false));
        Assert.Equal(4, ScenarioA1GunPushCalculator.PushFloorHalfMf(true, false, true));
        Assert.Equal(4, ScenarioA1GunPushCalculator.PushFloorHalfMf(false, true, true));
    }

    [Fact]
    public void AHookUpAfterAPushNeedsAnEntryBelowTheMNumberAndTheMfForAnotherPush()
    {
        // Not pushed this MPh: C10.31 says nothing, whatever the vehicle's MP (C10.11's).
        Assert.Null(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushed: false, last: null, pushers: [], vehicleMp: false)));

        // Clear: 4 half MF left enough, 3 short.
        Assert.Null(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [4])));
        Assert.Contains("could still attempt", ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [3])), StringComparison.Ordinal);

        // Deep Snow: 6 enough, 5 short, 4 enough with a plowed road hexside leaving the hex.
        Assert.Null(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [6], deepSnow: true)));
        Assert.NotNull(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [5], deepSnow: true)));
        Assert.Null(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [4], deepSnow: true, roadExit: true)));

        // Mud the same.
        Assert.NotNull(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [5], mud: true)));
        Assert.Null(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [6], mud: true)));

        // One of two pushers of the Player Turn short; an MF unknown; no pusher named.
        Assert.NotNull(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [8, 3])));
        Assert.NotNull(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [8, null])));
        Assert.NotNull(ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(pushers: [])));

        // A stop or a failure, or no result, refuses.
        Assert.Contains("below the M#", ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(last: ManhandlingResult.EnterStop)), StringComparison.Ordinal);
        Assert.Contains("below the M#", ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(last: ManhandlingResult.Stay)), StringComparison.Ordinal);
        Assert.Contains("below the M#", ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(last: null)), StringComparison.Ordinal);

        // The vehicle short of the hook-up MP (C10.11).
        Assert.Contains("C10.11", ScenarioA1GunPushCalculator.HookAfterPushRefusal(Hook(vehicleMp: false)), StringComparison.Ordinal);
    }

    // The forwards: each old member equal to a copy of its old expression.

    [Fact]
    public void PushOffDrmForwardsEqualToItsOldFormula()
    {
        foreach (var road in Both)
        {
            for (var halfMf = -3; halfMf <= 20; halfMf++)
            {
                Assert.Equal((halfMf / 2) - (road ? 2 : 0), ScenarioA1EntryCalculator.PushOffDrm(halfMf, road));
            }

            Assert.Equal((int.MaxValue / 2) - (road ? 2 : 0), ScenarioA1EntryCalculator.PushOffDrm(int.MaxValue, road));
        }
    }

    [Fact]
    public void TheDoublingsForwardEqualToTheirOldExpressions()
    {
        foreach (var terrain in new[] { "open-ground", "grain", "woods" })
        {
            for (var halfMf = 0; halfMf <= 20; halfMf++)
            {
                var old = terrain is not ("open-ground" or "grain")
                    ? ($"play.move-push-terrain: a Gun is pushed only into Open Ground or grain in the review, and leaving f crosses {terrain} (C10.3; rulings R8.6, R26.4)", halfMf)
                    : ((string?)null, halfMf * 2);
                Assert.Equal(old, ScenarioA1EntryCalculator.PushedExit(terrain, halfMf, "f"));
                Assert.Equal(halfMf * 2, ScenarioA1GunPushCalculator.PushHalfMf(halfMf));
            }
        }
    }

    // The contracts.

    private static JsonObject Manhandling(bool owner = true) =>
        new()
        {
            ["gunId"] = owner ? "g-gun" : null,
            ["at"] = "bd04:Q3:0",
            ["to"] = "bd04:Q2:0",
            ["mud"] = false,
            ["deepSnow"] = false,
            ["roadHexsideCrossed"] = false,
            ["pavedRoad"] = false,
            ["plowedRoad"] = false,
            ["lowAmmoPlaced"] = false,
            ["bypass"] = false,
            ["assault"] = false,
            ["minimumMove"] = false,
            ["carryingPp"] = false,
            ["wire"] = false,
            ["intoDeepStream"] = false,
            ["intoBamboo"] = false,
            ["viaTb"] = false,
            ["hexTem"] = 0,
            ["hexsideTem"] = 0,
            ["mfSpent"] = 2,
            ["labor"] = 0,
            ["leastHalfMfLeft"] = 8,
            ["lastResult"] = "none",
            ["originalDr"] = null,
            ["mNumber"] = owner ? 10 : null,
            ["qsu"] = owner ? true : null,
            ["limbered"] = owner ? false : null,
            ["nm"] = owner ? false : null,
            ["rfnm"] = owner ? false : null,
            ["unhookedThisPlayerTurn"] = owner ? false : null,
            ["extraCrewsOrHalfSquads"] = owner ? 0 : null,
            ["extraSquads"] = owner ? 0 : null,
            ["onlyPusherIsSquad"] = owner ? false : null,
        };

    private static ContractVerdict Evaluate(string id, JsonObject facts) => ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(id)!, facts);

    [Fact]
    public void TheManhandlingContractGivesTheDrmAndTheResult()
    {
        var pushable = Evaluate("c10.3-manhandling", Manhandling());
        Assert.Equal("pushable", pushable.Outcome);
        Assert.Contains(pushable.Reasons, reason => reason.Text.StartsWith("DRM +2:", StringComparison.Ordinal));
        Assert.Equal(8, Assert.Single(pushable.Rolls).Target);

        var facts = Manhandling();
        facts["originalDr"] = 7;
        Assert.Equal("enter", Evaluate("c10.3-manhandling", facts).Outcome);
        facts["originalDr"] = 8;
        Assert.Equal("enter-stop", Evaluate("c10.3-manhandling", facts).Outcome);
        facts["originalDr"] = 9;
        var stay = Evaluate("c10.3-manhandling", facts);
        Assert.Equal("stay", stay.Outcome);
        Assert.Contains(stay.Reasons, reason => reason.Text.Contains("Labor", StringComparison.Ordinal));

        // Mud adds +3, and the road -2 is lost off an unpaved road.
        facts["originalDr"] = 7;
        facts["mud"] = true;
        facts["roadHexsideCrossed"] = true;
        var mud = Evaluate("c10.3-manhandling", facts);
        Assert.Equal("stay", mud.Outcome);
        Assert.Contains(mud.Reasons, reason => reason.Text.StartsWith("DRM +5:", StringComparison.Ordinal));

        // A DR out of range is not evaluated.
        facts["originalDr"] = 13;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("c10.3-manhandling", facts).Outcome);
    }

    [Fact]
    public void TheManhandlingContractRefusesAndLeavesTheGunsFactsToItsOwner()
    {
        // The Gun's own bar, known to its owner.
        var nm = Manhandling();
        nm["nm"] = true;
        Assert.Equal("refused", Evaluate("c10.3-manhandling", nm).Outcome);

        // The opponent's view of a concealed Gun: not evaluated.
        var hidden = Manhandling(owner: false);
        var verdict = Evaluate("c10.3-manhandling", hidden);
        Assert.Equal(ContractVerdict.NotEvaluated, verdict.Outcome);
        Assert.Contains("qsu", verdict.Reasons[0].Text, StringComparison.Ordinal);

        // Public facts alone refuse: Bypass, Assault, a Minimum Move, a deep stream, a push already ended.
        foreach (var bar in new[] { "bypass", "assault", "minimumMove", "intoDeepStream" })
        {
            var refused = Manhandling(owner: false);
            refused[bar] = true;
            Assert.Equal("refused", Evaluate("c10.3-manhandling", refused).Outcome);
        }

        var ended = Manhandling(owner: false);
        ended["lastResult"] = "enter-stop";
        Assert.Equal("refused", Evaluate("c10.3-manhandling", ended).Outcome);

        // A pusher short of the hex's MF refuses; an unknown MF leaves it open.
        var shortMf = Manhandling();
        shortMf["leastHalfMfLeft"] = 3;
        Assert.Equal("refused", Evaluate("c10.3-manhandling", shortMf).Outcome);
        var unknownMf = Manhandling();
        unknownMf["leastHalfMfLeft"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("c10.3-manhandling", unknownMf).Outcome);
    }

    private static JsonObject HookFacts() =>
        new()
        {
            ["gunId"] = "g-gun",
            ["pushedThisMph"] = true,
            ["lastResult"] = "enter",
            ["leastHalfMfLeft"] = 4,
            ["mud"] = false,
            ["deepSnow"] = false,
            ["roadExit"] = false,
            ["vehicleHasHookUpMp"] = true,
        };

    [Fact]
    public void TheHookAfterPushContractAllowsRefusesAndLeavesAnUnknownMfOpen()
    {
        Assert.Equal("allowed", Evaluate("c10.31-hook-after-push", HookFacts()).Outcome);

        var stayed = HookFacts();
        stayed["lastResult"] = "stay";
        Assert.Equal("refused", Evaluate("c10.31-hook-after-push", stayed).Outcome);

        var deepSnow = HookFacts();
        deepSnow["deepSnow"] = true;
        Assert.Equal("refused", Evaluate("c10.31-hook-after-push", deepSnow).Outcome);
        deepSnow["roadExit"] = true;
        Assert.Equal("allowed", Evaluate("c10.31-hook-after-push", deepSnow).Outcome);

        var unknown = HookFacts();
        unknown["leastHalfMfLeft"] = null;
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("c10.31-hook-after-push", unknown).Outcome);

        var notPushed = HookFacts();
        notPushed["pushedThisMph"] = false;
        notPushed["lastResult"] = "none";
        notPushed["leastHalfMfLeft"] = null;
        Assert.Equal("allowed", Evaluate("c10.31-hook-after-push", notPushed).Outcome);
    }
}
