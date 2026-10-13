using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 55, Armored combat II. Task 55.1's first part: the notation of a Depletion Number (C8.1 to C8.91, pp. 177 to 179; ruling R55.4), and task
/// 55.6: the British OQF 6-Pounder and the American M3A1 37mm AT Gun with a crew of each (ruling R55.3), and the To Hit color of the two
/// nationalities (C3.3, p. 169; the National Capabilities Chart, p. 695).
/// </summary>
public sealed class ScenarioA1Pass55RulesTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private static readonly string[] Types = ["ap", "apcr", "apds", "heat", "he", "canister"];
    private static readonly int?[] Months = [null, 1, 4, 5, 6, 12];

    /// <summary>The definitions that came with pass 55: the pin below is of the counters that were there before it.</summary>
    private static readonly string[] Pass55 = ["british-at-gun", "american-at-gun"];

    // The Depletion Numbers of the catalog's counters as they stood before pass 55, by year from 1939 to 1945 (the month matters to HEAT alone).
    private static readonly Dictionary<(string Definition, string Ammunition), int?[]> Pinned = new()
    {
        [("defender-at-gun", "apcr")] = [null, null, null, 4, 5, 6, 7],
        [("attacker-tank", "apcr")] = [null, null, 4, 5, 5, 5, 5],
        [("defender-tank", "apcr")] = [null, null, null, 4, 5, 6, 6],
    };

    private const string At = "bd01:G5:0";
    private const string AmmunitionOutside = "asl.a1.ordnance.ammunition-outside";
    private const string GunOutside = "asl.a1.ordnance.gun-outside";

    /// <summary>A made-up Gun that lists the given special ammunition; no counter of the catalog prints AP# or an undated HE#.</summary>
    private static GunDefinition MadeUp(params string[] ammunition) => new("made-up", "british", "at", 57, "l", 3, 12, 225, false, false)
    {
        SpecialAmmo = ammunition,
    };

    private static int? Number(GunDefinition gun, string ammunition, int? year, int? month = null, bool elite = false) =>
        ScenarioA1Definitions.DepletionNumber(gun, ammunition, year, month, elite);

    /// <summary>A Gun of the nationality, manned by its crew, firing in the PFPh at range 12: at German Infantry, or at the PzKpfw IIIH when an ammunition is named.</summary>
    private static OrdnanceShot Gun(string nationality, int? year, string? ammunition = null, bool vehicle = false, int[]? toHit = null)
    {
        var hit = new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
            vehicle ? [] : [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 3, null);
        return new OrdnanceShot("PFPh", "phasing", nationality, new OrdnanceGun("gun", nationality + "-at-gun", false, 0, false, false),
            new OrdnanceCrew("crew", nationality + "-crew", false, false, false, false, false), At, 12, 0, false, true, 0, hit,
            new OrdnanceRolls(toHit, null, null, null, null))
        {
            VehicleTarget = vehicle ? new OrdnanceVehicleTarget("de-tank", "attacker-tank", "front", "front", false, false, false, false, true) : null,
            Ammunition = ammunition,
            ScenarioYear = year,
        };
    }

    private static OrdnanceResolution Resolve(OrdnanceShot shot) => ScenarioA1OrdnanceCalculator.Resolve(shot, Ordnance);

    [Fact]
    public void EachTypeOfAmmunitionHasItsDepletionNumber()
    {
        // C8.11, C8.3, C8.8, C8.4 (ruling R55.4): A is APCR, D APDS, H HEAT, HE and AP the limited stowage, C Canister.
        var gun = MadeUp("A4^2", "D6^4", "H7", "HE8", "AP5", "C7^2");
        Assert.Equal((4, 6, 7, 8, 5, 7), (Number(gun, "apcr", 1944), Number(gun, "apds", 1944), Number(gun, "heat", 1944), Number(gun, "he", 1944),
            Number(gun, "ap", 1944), Number(gun, "canister", 1944)));

        // The two-letter types are not the one-letter ones: HE7 is no HEAT, AP5 no APCR.
        var stowage = MadeUp("HE7", "AP5");
        Assert.Null(Number(stowage, "heat", 1944));
        Assert.Null(Number(stowage, "apcr", 1944));
        Assert.Equal((7, 5), (Number(stowage, "he", 1944), Number(stowage, "ap", 1944)));
        var special = MadeUp("H7", "A5^3");
        Assert.Null(Number(special, "he", 1944));
        Assert.Null(Number(special, "ap", 1944));
        Assert.Null(Number(special, "smoke", 1944));

        // C8.8: an AP# or HE# with no year holds whatever the date, and in a game that names none; C8.3's HEAT and a dated number need the year.
        Assert.Equal((5, 5, 5), (Number(stowage, "ap", 1939), Number(stowage, "ap", 1945), Number(stowage, "ap", null)));
        Assert.Null(Number(gun, "heat", null));
        Assert.Null(Number(gun, "apcr", null));
    }

    [Fact]
    public void ANumberHoldsFromItsYearAndItsMonth()
    {
        // British Ordnance Note 6 (p. 426): HE7 from February 1943, HE8 for 1944 and 1945; APDS D6 from June 1944, D7 in 1945.
        var sixPounder = Ordnance.Guns["british-at-gun"];
        Assert.Equal(["HE7^3m2", "HE8^4+", "D6^4m6E", "D7^5"], sixPounder.SpecialAmmo);
        Assert.Null(Number(sixPounder, "he", 1942, 12));
        Assert.Null(Number(sixPounder, "he", 1943, 1));
        Assert.Null(Number(sixPounder, "he", 1943));
        Assert.Equal((7, 7, 8, 8), (Number(sixPounder, "he", 1943, 2), Number(sixPounder, "he", 1943, 12), Number(sixPounder, "he", 1944, 1), Number(sixPounder, "he", 1945)));
        Assert.Null(Number(sixPounder, "apds", 1943, 12));
        Assert.Null(Number(sixPounder, "apds", 1944, 5));
        Assert.Equal((6, 6, 7), (Number(sixPounder, "apds", 1944, 6), Number(sixPounder, "apds", 1944, 12), Number(sixPounder, "apds", 1945, 1)));
        Assert.Null(Number(sixPounder, "apcr", 1944, 6));
        Assert.Null(Number(sixPounder, "ap", 1944, 6));

        // U.S. Ordnance Note C (p. 395): 37mm canister "August 1942 on".
        var m3a1 = Ordnance.Guns["american-at-gun"];
        Assert.Equal(["C7^2m8+"], m3a1.SpecialAmmo);
        Assert.Null(Number(m3a1, "canister", 1941, 12));
        Assert.Null(Number(m3a1, "canister", 1942, 7));
        Assert.Equal((7, 7, 7), (Number(m3a1, "canister", 1942, 8), Number(m3a1, "canister", 1943), Number(m3a1, "canister", 1945, 5)));

        // The "+" changes nothing the last listed number did not have already (C8.91 as the game read it before the pass), and the "E" is not read.
        Assert.Equal(7, Number(MadeUp("C7^2m8"), "canister", 1945));
        Assert.Equal(6, Number(MadeUp("D6^4m6"), "apds", 1944, 6));

        // C8.91: the exponent is the last digit of the year, so a 9 is 1939.
        Assert.Equal((null, 4, 4), (Number(MadeUp("A4^9"), "apcr", 1938), Number(MadeUp("A4^9"), "apcr", 1939), Number(MadeUp("A4^9"), "apcr", 1941)));
    }

    [Fact]
    public void EliteForcesHaveOneMore()
    {
        // C8.2 (p. 178): "Increase the Depletion Number of an armed-vehicle/weapon by one for Elite forces."
        var sixPounder = Ordnance.Guns["british-at-gun"];
        Assert.Equal((8, 9, 7), (Number(sixPounder, "he", 1943, 2, elite: true), Number(sixPounder, "he", 1944, elite: true), Number(sixPounder, "apds", 1944, 6, elite: true)));
        Assert.Equal(6, Number(MadeUp("AP5"), "ap", null, elite: true));
        Assert.Equal(5, Number(Ordnance.Guns["defender-at-gun"], "apcr", 1942, elite: true));
        Assert.Null(Number(sixPounder, "he", 1942, elite: true));
    }

    [Fact]
    public void WhatIsNoDepletionNumberIsPassedOver()
    {
        // The British 2-in. mortar's s7, and items the notation does not write, give no number and no error.
        var gun = MadeUp("s7", "WP7", "IR", "A", "A4^", "HE7^3m13", "D6^4x", "C", "H7x");
        foreach (var type in Types)
        {
            Assert.Null(Number(gun, type, 1944, 6));
        }

        Assert.Empty(ScenarioA1Definitions.Ammunitions("british-crew"));
    }

    [Fact]
    public void ADefinitionListsTheAmmunitionItMayCarry()
    {
        // C8.1; C8.8 (p. 179): the 6-Pounder's caliber is underscored, no HE, yet it lists HE by a Depletion Number.
        Assert.Equal(["ap", "apds", "he"], ScenarioA1Definitions.Ammunitions("british-at-gun"));
        Assert.Equal(["ap", "he", "canister"], ScenarioA1Definitions.Ammunitions("american-at-gun"));

        // The earlier counters list what they did.
        Assert.Equal(["ap", "apcr", "he"], ScenarioA1Definitions.Ammunitions("attacker-tank"));
        Assert.Equal(["ap", "apcr", "he"], ScenarioA1Definitions.Ammunitions("defender-at-gun"));
        Assert.Equal(["heat", "he"], ScenarioA1Definitions.Ammunitions("attacker-inf-gun"));
        Assert.Equal(["he"], ScenarioA1Definitions.Ammunitions("british-light-mortar"));
    }

    [Fact]
    public void TheTwoAtGunsAndTheirCrewsAreInTheCatalog()
    {
        // The British Ordnance Listing (p. 426): OQF 6-Pounder 7-cwt, AT, 57L underscored, ROF 3, no B#, range 225, M10, +1, NT.
        var sixPounder = Ordnance.Guns["british-at-gun"];
        Assert.Equal(("british", "at", 57, "l", 3, 12, 225), (sixPounder.Nationality, sixPounder.GunType, sixPounder.Caliber, sixPounder.Suffix, sixPounder.RateOfFire,
            sixPounder.Breakdown, sixPounder.RangeMaximum));
        Assert.Equal((10, "small", true, false, false), (sixPounder.Manhandling, sixPounder.TargetSize, sixPounder.NoHe, sixPounder.NoAp, sixPounder.Mount360));

        // The U.S. Ordnance Listing (p. 392): M3A1 37mm AT Gun, AT, 37LL, ROF 2, no B#, range 292, M12, +1, NT.
        var m3a1 = Ordnance.Guns["american-at-gun"];
        Assert.Equal(("american", "at", 37, "ll", 2, 12, 292), (m3a1.Nationality, m3a1.GunType, m3a1.Caliber, m3a1.Suffix, m3a1.RateOfFire, m3a1.Breakdown, m3a1.RangeMaximum));
        Assert.Equal((12, "small", false, false, false), (m3a1.Manhandling, m3a1.TargetSize, m3a1.NoHe, m3a1.NoAp, m3a1.Mount360));

        // The National Capabilities Chart (p. 695): the British crew 2-2-8, the American Army crew 2-2-7 with a broken Morale Level of 8.
        var british = Ordnance.Fire.Definitions["british-crew"];
        Assert.Equal(("asl:crew", "british", 2, 2, 8, 8), (british.Kind, british.Nationality, british.Firepower, british.Range, british.Morale, british.BrokenMorale));
        var american = Ordnance.Fire.Definitions["american-crew"];
        Assert.Equal(("asl:crew", "american", 2, 2, 7, 8), (american.Kind, american.Nationality, american.Firepower, american.Range, american.Morale, american.BrokenMorale));
    }

    [Fact]
    public void TheToHitColorIsTheNationalitysAndTheYears()
    {
        // The National Capabilities Chart (p. 695): German black, Russian red, British black, American red before 1944 and black from 1944.
        Assert.Equal("black", ScenarioA1OrdnanceReference.Color(Ordnance.Guns["british-at-gun"], 1942));
        Assert.Equal("black", ScenarioA1OrdnanceReference.Color(Ordnance.Guns["british-at-gun"], null));
        Assert.Equal(("red", "red", "black", "black"), (ScenarioA1OrdnanceReference.Color(Ordnance.Guns["american-at-gun"], null),
            ScenarioA1OrdnanceReference.Color(Ordnance.Guns["american-at-gun"], 1943), ScenarioA1OrdnanceReference.Color(Ordnance.Guns["american-at-gun"], 1944),
            ScenarioA1OrdnanceReference.Color(Ordnance.Guns["american-at-gun"], 1945)));

        // Every earlier counter keeps the color it had, whatever the year: the German black, every other red, the British 2-in. mortar among them.
        foreach (var gun in Ordnance.Guns.Values.Where(gun => !Pass55.Contains(gun.Id)))
        {
            foreach (int? year in new int?[] { null, 1939, 1943, 1944, 1945 })
            {
                Assert.Equal(gun.Nationality == "german" ? "black" : "red", ScenarioA1OrdnanceReference.Color(gun, year));
                Assert.Equal(ScenarioA1OrdnanceReference.Color(gun.Nationality), ScenarioA1OrdnanceReference.Color(gun, year));
            }
        }

        // C3.3: the shot reads the row of its color. The 37mm at Infantry at 12 hexes in 1943 and in 1944; the 6-Pounder's AP at a tank.
        var modifications = (int)Ordnance.Modifications(Ordnance.Guns["american-at-gun"], 12).Sum(item => item.Value);
        Assert.NotEqual(Ordnance.BasicToHit("red", 12), Ordnance.BasicToHit("black", 12));
        Assert.NotEqual(Ordnance.Armor.BasicToHit("red", Ordnance.Column(12)), Ordnance.Armor.BasicToHit("black", Ordnance.Column(12)));
        Assert.Equal(Ordnance.BasicToHit("red", 12) + modifications, Resolve(Gun("american", 1943, toHit: [5, 6])).ToHit!.ModifiedToHit);
        Assert.Equal(Ordnance.BasicToHit("black", 12) + modifications, Resolve(Gun("american", 1944, toHit: [5, 6])).ToHit!.ModifiedToHit);
        var british = Resolve(Gun("british", 1944, "ap", vehicle: true, toHit: [5, 6]));
        Assert.Equal(OrdnanceResolution.Resolved, british.Disposition);
        Assert.Equal(Ordnance.Armor.BasicToHit("black", Ordnance.Column(12)) + (int)Ordnance.Modifications(Ordnance.Guns["british-at-gun"], 12).Sum(item => item.Value),
            british.ToHit!.ModifiedToHit);

        // A shot records its year when it is at a vehicle or names its ammunition, as before, and when its color turns with the year.
        Assert.True(ScenarioA1OrdnanceEligibility.RecordsYear(false, false, "american"));
        Assert.False(ScenarioA1OrdnanceEligibility.RecordsYear(false, false, "british"));
        Assert.False(ScenarioA1OrdnanceEligibility.RecordsYear(false, false, "german"));
        Assert.True(ScenarioA1OrdnanceEligibility.RecordsYear(true, false, "german"));
        Assert.True(ScenarioA1OrdnanceEligibility.RecordsYear(false, true, "russian"));
    }

    [Fact]
    public void CanisterStaysOutsideTheVehicleTargetType()
    {
        // C8.4: Canister "is ineffective vs armored targets", so it stays outside the Vehicle Target Type; task 55.2 fires it at an aim (below).
        // A shot that names it with a target Location's facts and no aim is missing its Canister facts.
        Assert.Contains(AmmunitionOutside, Resolve(Gun("american", 1944, "canister", vehicle: true, toHit: [3, 1])).Reasons);
        Assert.Contains("asl.a1.ordnance.fact-missing:canister", Resolve(Gun("american", 1944, "canister", toHit: [3, 1])).Reasons);
        Assert.DoesNotContain(ScenarioA1Definitions.AmmunitionChoices("american-at-gun", true, 1944, 6, false, []), item => item.Ammunition == "canister");
        Assert.Empty(ScenarioA1Definitions.AmmunitionChoices("attacker-inf-gun", false, 1944, 6, false, []));

        // The American 37mm fires its AP at a tank (C7.31: 37LL is 11) and its HE at Infantry.
        var ap = Resolve(Gun("american", 1944, "ap", vehicle: true, toHit: [5, 6]));
        Assert.Equal(OrdnanceResolution.Resolved, ap.Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Gun("american", 1944, toHit: [5, 6])).Disposition);
    }

    // Task 55.4 (increment 2b): ordnance and a vehicle's MGs at another level (C2.6, p. 168), with Height Advantage (B10.31, p. 125; D4.22, p. 202).

    private static readonly ScenarioA1FireReference Fire = new ScenarioA1FirePackage().Reference;
    private const string TankAt = "bd01:F5:0";
    private const string OutOfRange = "asl.a1.ordnance.out-of-range";
    private const string ElevationOutside = "asl.a1.fire.elevation-outside";

    private static decimal? Drm(OrdnanceResolution result, string name) => result.ToHit!.Drm.Where(item => item.Name == name).Select(item => (decimal?)item.Value).SingleOrDefault();

    /// <summary>The American 37mm at German Infantry uphill at 12 hexes, or with AP at the PzKpfw IIIH there.</summary>
    private static OrdnanceShot Uphill(bool vehicle = false, string terrain = "open-ground", bool heightAdvantage = true)
    {
        var shot = Gun("american", 1944, vehicle ? "ap" : null, vehicle, [5, 6]);
        return shot with
        {
            HeightAdvantage = heightAdvantage ? true : null,
            Hit = shot.Hit! with
            {
                SameLevel = false,
                TargetTerrain = terrain,
            },
        };
    }

    /// <summary>The same shot as Defensive First Fire at Infantry moving in the open without Assault Movement.</summary>
    private static OrdnanceShot AtAMover(bool heightAdvantage)
    {
        var shot = Uphill(heightAdvantage: heightAdvantage);
        return shot with
        {
            Phase = "MPh",
            FiringSide = "non-phasing",
            FireKind = "first-fire",
            Movement = new OrdnanceMovement(null, true, true, 1, 0),
            Hit = shot.Hit! with
            {
                Phase = "MPh",
                FiringSide = "non-phasing",
            },
        };
    }

    /// <summary>The PzKpfw IIIH's CMG (5 FP) in the PFPh at a Russian squad, by the range and the levels the target is above the tank.</summary>
    private static FireAttack Cmg(int range, int? levelAbove, string terrain = "open-ground", bool heightAdvantage = false, string weapon = FireVehicleWeapon.CoaxialMg) =>
        new FireAttack("PFPh", "phasing", true, TankAt, At, [], null, range, levelAbove is null or 0, new FireLos(false, 0, true, false), null, terrain,
            [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false)], 3, new FireRolls([5, 6], null, null, null))
        {
            VehicleFire = new FireVehicleFire("de-tank", "attacker-tank", TankAt, false, false, false, false, false, false, false, false)
            {
                Weapons = [new FireVehicleWeapon(weapon, false, false) { Turn = 0 }],
            },
            TargetLevelAbove = levelAbove is null or 0 ? null : levelAbove,
            HeightAdvantage = heightAdvantage ? true : null,
        };

    private static FireResolution Shoot(FireAttack attack) => ScenarioA1FireCalculator.Resolve(attack, Fire);

    [Fact]
    public void AGunFiresAtAnotherLevelWhenTheRangeIsAtLeastTheElevationDifference()
    {
        // C2.6: "a Gun may fire-at/affect a different-level target only if the range is >= the elevation difference between them", up or down.
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(0, 0, "at"));
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(1, 1, "at"));
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(-2, 2, "vehicle"));
        Assert.False(ScenarioA1OrdnanceMapRules.ElevationAllowed(2, 1, "at"));
        Assert.False(ScenarioA1OrdnanceMapRules.ElevationAllowed(-2, 1, "inf"));
        Assert.False(ScenarioA1OrdnanceMapRules.ElevationAllowed(1, 0, "at"));
        Assert.False(ScenarioA1OrdnanceMapRules.ElevationAllowed(3, 2));

        // "Only mortars ... may fire-at/affect a higher-level target if the range to that target is < the elevation difference"; firing down a
        // mortar is held to the limit ("they do apply to MTR/INF/RCL ordnance SW"), and a LATW is free of it ("but not to SW").
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(3, 2, "mortar"));
        Assert.False(ScenarioA1OrdnanceMapRules.ElevationAllowed(-3, 2, "mortar"));
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(-3, 3, "mortar"));
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(-3, 1, "latw"));
        Assert.True(ScenarioA1OrdnanceMapRules.ElevationAllowed(3, 1, "latw"));

        // The shot at another level resolves on both Target Types; one the range does not allow is refused.
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Uphill(heightAdvantage: false)).Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Uphill(vehicle: true, heightAdvantage: false)).Disposition);
        Assert.Contains(OutOfRange, Resolve(Uphill() with { ElevationAllowed = false }).Reasons);
        Assert.Contains(OutOfRange, Resolve(Uphill(vehicle: true) with { ElevationAllowed = false }).Reasons);

        // "A Gun in a building hex may not fire at a lower-level target in its own hex", nor at a higher one unless it is an AA Gun.
        Assert.Contains("C2.6", ScenarioA1OrdnanceMapRules.OwnHexLevelRefusal("de-gun", true, false), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceMapRules.OwnHexLevelRefusal("de-gun", true, true));
        Assert.Null(ScenarioA1OrdnanceMapRules.OwnHexLevelRefusal("de-gun", false, false));
        Assert.Contains("C2.6", ScenarioA1OrdnanceMapRules.GunTargetStatus(1, 100, () => 2, () => 0, 0), StringComparison.Ordinal);
        Assert.Equal("range 2, in its CA", ScenarioA1OrdnanceMapRules.GunTargetStatus(2, 100, () => 2, () => 0, 0));
    }

    [Fact]
    public void NoOrdnanceFiresThroughACliffHexsideToAnAdjacentLocation()
    {
        // B11.31: down, "[EXC: LATW other than PIAT or MTR with minimum range of 1 hex]"; B11.32: up, "Only an AA weapon, MG counter, SW ATR, PIAT,
        // MTR with minimum range of 1 hex".
        Assert.Contains("B11.31", ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, -1, "at", null, null), StringComparison.Ordinal);
        Assert.Contains("B11.32", ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 1, "vehicle", null, null), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, -1, "latw", "psk", null));
        Assert.NotNull(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, -1, "latw", "piat", null));
        Assert.NotNull(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 1, "latw", "psk", null));
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 1, "latw", "atr", null));
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 1, "mortar", null, 1));
        Assert.NotNull(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 1, "mortar", null, 2));

        // Only to an adjacent Location at another level, and only through a cliff.
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 2, -1, "at", null, null));
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(true, 1, 0, "at", null, null));
        Assert.Null(ScenarioA1OrdnanceMapRules.CliffRefusal(false, 1, -1, "at", null, null));
    }

    [Fact]
    public void HeightAdvantageIsCaseQOnTheInfantryAndVehicleTargetTypes()
    {
        // B10.31, C6.8: "Any unit in a hex receiving Direct Fire from a lower elevation is entitled to a +1 TEM".
        Assert.Equal(1m, Drm(Resolve(Uphill()), "case-q:height-advantage"));
        Assert.Null(Drm(Resolve(Uphill(heightAdvantage: false)), "case-q:height-advantage"));
        Assert.Equal("Case Q (Height Advantage, B10.31)", ScenarioA1OrdnanceEligibility.CaseWords("case-q:height-advantage"));

        // "provided that unit is not eligible to receive any other positive TEM": woods give their own and no more.
        var woods = Resolve(Uphill(terrain: "woods"));
        Assert.Equal((1m, null), (Drm(woods, "case-q:woods"), Drm(woods, "case-q:height-advantage")));

        // D4.22: "also applies to vehicles"; a vehicle in Bypass takes no TEM of its woods (D2.38), so it takes this one.
        Assert.Equal(1m, Drm(Resolve(Uphill(vehicle: true)), "case-q:height-advantage"));
        var tankInWoods = Resolve(Uphill(vehicle: true, terrain: "woods"));
        Assert.Equal((1m, null), (Drm(tankInWoods, "case-q:woods"), Drm(tankInWoods, "case-q:height-advantage")));
        var bypassing = Uphill(vehicle: true, terrain: "woods");
        var inBypass = Resolve(bypassing with { VehicleTarget = bypassing.VehicleTarget! with { Bypass = true } });
        Assert.Equal((null, 1m), (Drm(inBypass, "case-q:woods"), Drm(inBypass, "case-q:height-advantage")));

        // The final DR is one more for it.
        Assert.Equal(Resolve(Uphill(heightAdvantage: false)).ToHit!.FinalDr + 1, Resolve(Uphill()).ToHit!.FinalDr);

        // The map read: none behind a wall or hedge the LOS crosses, and none on the Area Target Type (C6.8; a mortar's fire is Indirect, C.1).
        Assert.True(ScenarioA1OrdnanceMapRules.HeightAdvantage(true, null));
        Assert.True(ScenarioA1OrdnanceMapRules.HeightAdvantage(true, 0));
        Assert.Null(ScenarioA1OrdnanceMapRules.HeightAdvantage(true, 2));
        Assert.Null(ScenarioA1OrdnanceMapRules.HeightAdvantage(true, null, areaTargetType: true));
        Assert.Null(ScenarioA1OrdnanceMapRules.HeightAdvantage(null, null));

        // A record made before the pass carries no such fact, and reads none from the recorded map facts.
        Assert.Null(ScenarioA1OrdnanceEligibility.WithRecordedMapFacts(Uphill(), Uphill(heightAdvantage: false)).HeightAdvantage);
        Assert.True(ScenarioA1OrdnanceEligibility.WithRecordedMapFacts(Uphill(heightAdvantage: false), Uphill()).HeightAdvantage);
    }

    [Fact]
    public void HeightAdvantageBarsCaseJ4()
    {
        // B10.31: "A unit eligible for the +1 TEM for Height Advantage is not subject to Interdiction/FFMO by an attack to which that +1 TEM applies."
        var level = Resolve(AtAMover(heightAdvantage: false));
        Assert.Equal((-1m, -1m, null), (Drm(level, "case-j3"), Drm(level, "case-j4"), Drm(level, "case-q:height-advantage")));
        var uphill = Resolve(AtAMover(heightAdvantage: true));
        Assert.Equal((-1m, null, 1m), (Drm(uphill, "case-j3"), Drm(uphill, "case-j4"), Drm(uphill, "case-q:height-advantage")));
    }

    [Fact]
    public void AVehiclesMgFiresAtAnotherLevelByTheSameLimit()
    {
        // C2.6: "These restrictions also apply to non-AA vehicular MG": range at least the elevation difference, up and down.
        Assert.Equal(FireResolution.Resolved, Shoot(Cmg(2, 1)).Disposition);
        Assert.Equal(FireResolution.Resolved, Shoot(Cmg(2, -2)).Disposition);
        Assert.Equal(FireResolution.Resolved, Shoot(Cmg(3, 2, weapon: FireVehicleWeapon.BowMg)).Disposition);
        Assert.Contains(ElevationOutside, Shoot(Cmg(2, 3)).Reasons);
        Assert.Contains(ElevationOutside, Shoot(Cmg(2, -3)).Reasons);
        Assert.Contains(ElevationOutside, Shoot(Cmg(1, 2, weapon: FireVehicleWeapon.BowMg)).Reasons);

        // The Range tab says so before any proposal.
        Assert.Contains("C2.6", FireRange.VehicleElevationWords(2, -3), StringComparison.Ordinal);
        Assert.Null(FireRange.VehicleElevationWords(3, 3));
        Assert.Null(FireRange.VehicleElevationWords(0, 1));

        // The levels are a fact the attack needs once it is not at the firer's level.
        Assert.Contains("asl.a1.fire.fact-missing:targetLevelAbove", Shoot(Cmg(2, 1) with { TargetLevelAbove = null }).Reasons);

        // Another level of the vehicle's own hex is never fired at.
        Assert.Contains("C2.6", ScenarioA1FireMapRules.VehicleOwnHexLevelBar(true, true, false, TankAt, "bd01:F5:1"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1FireMapRules.VehicleOwnHexLevelBar(false, true, false, TankAt, "bd01:F5:1"));
        Assert.Null(ScenarioA1FireMapRules.VehicleOwnHexLevelBar(true, false, false, TankAt, At));

        // A7.21: PBF at an adjacent target one level above or below, as for Infantry.
        static bool PointBlank(FireResolution result) => result.Arithmetic!.Firers.Single().Multipliers.Any(item => item.Name == "point-blank-fire");
        Assert.True(PointBlank(Shoot(Cmg(1, 0))));
        Assert.True(PointBlank(Shoot(Cmg(1, 1))));
        Assert.True(PointBlank(Shoot(Cmg(1, -1))));
        Assert.False(PointBlank(Shoot(Cmg(2, 1))));
    }

    [Fact]
    public void AVehiclesMgGivesItsTargetHeightAdvantage()
    {
        // B10.31: a vehicle's MG fire is Direct Fire; +1 for a target above it with no other positive TEM.
        static decimal? Tem(FireResolution result, string name) => result.Arithmetic!.Drm.Where(item => item.Name == name).Select(item => (decimal?)item.Value).SingleOrDefault();
        var uphill = Shoot(Cmg(2, 1, heightAdvantage: true));
        Assert.Equal(FireResolution.Resolved, uphill.Disposition);
        Assert.Equal(1m, Tem(uphill, "height-advantage"));
        var woods = Shoot(Cmg(2, 1, terrain: "woods", heightAdvantage: true));
        Assert.Equal((1m, null), (Tem(woods, "tem:woods"), Tem(woods, "height-advantage")));
        Assert.Null(Tem(Shoot(Cmg(2, 1)), "height-advantage"));

        // The map reads it for Infantry fire and for a vehicle's MGs, not for an ordnance hit or Residual FP.
        Assert.True(ScenarioA1FireMapRules.ReadsHeightAdvantage(Cmg(2, 1)));
        Assert.False(ScenarioA1FireMapRules.ReadsHeightAdvantage(Cmg(2, 1) with { OrdnanceHit = new FireOrdnanceHit("gun", 6, false) }));
        Assert.False(ScenarioA1FireMapRules.ReadsHeightAdvantage(Cmg(2, 1) with { VehicleFire = null, FireKind = ScenarioA1FireCalculator.ResidualFire }));

        // An attack at the firer's level, as every record before the pass was, is what it was.
        Assert.Equal(5m, Shoot(Cmg(2, 0)).Arithmetic!.TotalFirepower);
        Assert.Empty(Shoot(Cmg(2, 0)).Arithmetic!.Drm);
    }

    // Task 55.1, the rest (increment 2c): APDS (C4.3, C7.32, C8.11), C8.9's three outcomes for every ammunition with a Depletion Number on the
    // Infantry and Vehicle Target Types (ruling R55.7), HE that must be named (C2.21, C8.8), Elite (C8.2; ruling R55.6), and the Supply Chart as
    // a test's data (C8.12; ruling R55.5).

    private const string HeNotNamed = "asl.a1.ordnance.he-not-named";
    private const string HeUnavailable = "asl.a1.ordnance.he-unavailable";
    private const string Unavailable = "asl.a1.ordnance.ammunition-unavailable";

    /// <summary>The shot in another month of its year.</summary>
    private static OrdnanceShot InMonth(OrdnanceShot shot, int month) => shot with
    {
        Hit = shot.Hit! with
        {
            ScenarioMonth = month
        }
    };

    /// <summary>The 6-Pounder's APDS at the PzKpfw IIIH's front in July 1944 (D6), a To Kill DR ready that is a dud.</summary>
    private static OrdnanceShot Apds(int[] toHit, int range = 12, int year = 1944)
    {
        var shot = Gun("british", year, "apds", vehicle: true, toHit) with
        {
            Range = range,
        };
        return shot with
        {
            Rolls = shot.Rolls! with
            {
                ToKill = [6, 6]
            }
        };
    }

    /// <summary>The 6-Pounder's HE, named, at the German squad in July 1944 (HE8), an IFT DR ready that does nothing.</summary>
    private static OrdnanceShot He(int[] toHit, int year = 1944, string? ammunition = "he")
    {
        var shot = Gun("british", year, ammunition, toHit: toHit);
        return shot with
        {
            Rolls = shot.Rolls! with
            {
                Hit = new FireRolls([6, 6], null, null, null),
                CriticalHit = new FireRolls([6, 6], null, null, null),
            }
        };
    }

    [Fact]
    public void ApdsLessensTheBasicToHitNumberBeyondTwelveHexes()
    {
        // C4.3 (p. 171): "the Basic TH# of any shot using APCR/APDS is lessened by one for every 12 hexes (or fraction thereof) beyond 12 hex range";
        // the chart's APDS/APCR row (p. 189): 0 to 12 hexes, -1 at 13 to 24, -2 at 25 to 36. A DR above the Depletion Number shows the To Hit line.
        var gun = Ordnance.Guns["british-at-gun"];
        decimal? Modification(int range) => Resolve(Apds([5, 6], range)).ToHit!.Modifications.Where(item => item.Name == "apds").Select(item => (decimal?)item.Value).SingleOrDefault();
        Assert.Equal((null, -1m, -1m, -2m), (Modification(12), Modification(13), Modification(24), Modification(25)));
        Assert.Contains(Resolve(Apds([5, 6], 13)).ToHit!.Modifications, item => item is { Name: "apds", Rule: "C4.3" });
        foreach (var range in new[] { 12, 13, 25 })
        {
            Assert.Equal(Ordnance.Armor.BasicToHit("black", Ordnance.Column(range)) + (int)Ordnance.Modifications(gun, range).Sum(item => item.Value) + (int)(Modification(range) ?? 0),
                Resolve(Apds([5, 6], range)).ToHit!.ModifiedToHit);
        }

        // AP at the same ranges takes no such modification.
        Assert.DoesNotContain(Resolve(Gun("british", 1944, "ap", vehicle: true, toHit: [5, 6]) with { Range = 25 }).ToHit!.Modifications, item => item.Name is "apds" or "apcr");
    }

    [Fact]
    public void ApdsReadsItsOwnListingsOfTheToKillTable()
    {
        // C7.32 (the table of p. 190): D57L 18, D76LL 25; the APDS (D) Case D row: +1 at 0 to 1 hexes, 0 from 2 to 24, -1 at 25 to 30, -2 at 31
        // to 36, -3 at 37 to 42, -4 at 43 to 48, NA from 49.
        var gun = Ordnance.Guns["british-at-gun"];
        Assert.Equal(18, Ordnance.Armor.BasicTk("apds", gun));
        Assert.Equal(25, Ordnance.Armor.BasicTk("apds", gun with { Caliber = 76, Suffix = "ll" }));
        Assert.Equal((1, 0, 0, 0, -1, -2, -3, -4, null), (Ordnance.Armor.CaseD("apds", gun, 1), Ordnance.Armor.CaseD("apds", gun, 2), Ordnance.Armor.CaseD("apds", gun, 12),
            Ordnance.Armor.CaseD("apds", gun, 24), Ordnance.Armor.CaseD("apds", gun, 25), Ordnance.Armor.CaseD("apds", gun, 31), Ordnance.Armor.CaseD("apds", gun, 37),
            Ordnance.Armor.CaseD("apds", gun, 48), Ordnance.Armor.CaseD("apds", gun, 49)));

        // The 6-Pounder has no APCR listing, and a Gun with no D listing has no APDS: the 57LL's 18 is APCR's.
        Assert.Null(Ordnance.Armor.BasicTk("apcr", gun));
        Assert.Null(Ordnance.Armor.BasicTk("apds", gun with { Suffix = "ll" }));
        Assert.Null(Ordnance.Armor.BasicTk("apds", Ordnance.Guns["defender-at-gun"]));

        // C7.321: "The Final TK# for an unarmored vehicle hit by APCR/APDS is equal to that listed on the AP To Kill Table": 8 for 37 to 57mm.
        Assert.Equal((8, 8), (Ordnance.Armor.UnarmoredTk("apds", gun), Ordnance.Armor.UnarmoredTk("ap", gun)));

        // A hull hit on the PzKpfw IIIH's front (AF 6) at 12 hexes: 18, no Case D, Final TK# 12; at one hex Case D +1: 19, Final TK# 13.
        var far = Resolve(Apds([2, 1])).Kill!;
        Assert.Equal(("apds", "hull", 18, 18, 6, 12), (far.Ammunition, far.HitLocation, far.BasicTk, far.ModifiedTk, far.ArmorFactor, far.FinalTk));
        Assert.DoesNotContain(far.Modifications, item => item.Name == "case-d");
        var near = Resolve(Apds([2, 1], 1)).Kill!;
        Assert.Equal((18, 19, 13), (near.BasicTk, near.ModifiedTk, near.FinalTk));
        Assert.Contains(near.Modifications, item => item is { Name: "case-d", Value: 1 });

        // C7.35, C7.7: an Original 12 is a dud; an 11 is below the Final TK# of 13 and above half of it: eliminated.
        var struck = Apds([2, 1], 1);
        Assert.Equal(OrdnanceKill.Dud, Resolve(struck).Kill!.Result);
        Assert.Equal(OrdnanceKill.Eliminated, Resolve(struck with { Rolls = struck.Rolls! with { ToKill = [5, 6], CrewSurvival = [6, 6] } }).Kill!.Result);
    }

    [Fact]
    public void ApdsFollowsItsDepletionNumber()
    {
        // C8.9 (p. 179), the 6-Pounder's D6 from June 1944: an Original DR below 6 uses it; a 6 uses it and it runs out; above 6 "the firer had no
        // such ammunition and is considered not to have fired yet for any purpose unless Gun Malfunction ... occurs".
        var used = Resolve(Apds([2, 1]));
        Assert.Equal(("used", true), (used.AmmunitionUse, used.ToHit!.Hit));
        Assert.NotNull(used.Kill);
        var last = Resolve(Apds([4, 2]));
        Assert.Equal(("depleted", true), (last.AmmunitionUse, last.ToHit!.Hit));
        Assert.NotNull(last.Kill);
        var none = Resolve(Apds([4, 3]) with { Acquisition = -1 });
        Assert.Equal(OrdnanceResolution.Resolved, none.Disposition);
        Assert.Equal(("none", false, true, null, false, -1, null), (none.AmmunitionUse, none.ToHit!.Hit, none.Gun!.RateOfFireKept, none.Gun.FireCounter, none.Gun.Malfunctioned,
            none.Gun.Acquisition, none.Gun.AcquiredLocationId));
        Assert.Null(none.Kill);

        // The 6-Pounder's B# is 12: an Original 12 found no APDS, and the Gun malfunctioned, so it has fired.
        var broke = Resolve(Apds([6, 6]));
        Assert.Equal(("none", true, false, "prep-fire", false), (broke.AmmunitionUse, broke.Gun!.Malfunctioned, broke.Gun.RateOfFireKept, broke.Gun.FireCounter, broke.ToHit!.Hit));
        Assert.Null(broke.Kill);

        // C8.92: "Intensive Fire does not change Depletion Numbers": a 6 is still its last round. A Gun that has fired Intensive Fires.
        var intensive = Apds([4, 2]);
        Assert.Equal("depleted", Resolve(intensive with { IntensiveFire = true, Gun = intensive.Gun! with { ShotsThisPhase = 1, FiredThisPlayerTurn = true } }).AmmunitionUse);

        // C8.11, the listing's "(J4+E)": none in May 1944, D6 from June, D7 in 1945; none once it has run out.
        Assert.Contains(Unavailable, Resolve(InMonth(Apds([2, 1]), 5)).Reasons);
        Assert.Equal("used", Resolve(InMonth(Apds([2, 1]), 6)).AmmunitionUse);
        Assert.Contains(Unavailable, Resolve(Apds([2, 1], year: 1943)).Reasons);
        Assert.Equal("used", Resolve(InMonth(Apds([4, 2], year: 1945), 1)).AmmunitionUse);
        Assert.Contains("asl.a1.ordnance.ammunition-depleted", Resolve(Apds([2, 1]).WithDepleted("apds")).Reasons);

        // No other Gun of the catalog lists APDS.
        Assert.Contains(AmmunitionOutside, Resolve(Gun("american", 1944, "apds", vehicle: true, toHit: [2, 1])).Reasons);
    }

    [Fact]
    public void HeWithADepletionNumberFollowsItOnTheInfantryTargetType()
    {
        // C8.8, C8.9: the 6-Pounder's HE8 in 1944. Below 8 the round is used; an 8 is its last.
        var used = Resolve(He([3, 3]));
        Assert.True(used.Disposition == OrdnanceResolution.Resolved, string.Join("; ", used.Reasons));
        Assert.Equal("used", used.AmmunitionUse);
        Assert.True(used.ToHit!.Hit);
        Assert.NotNull(used.Hit ?? used.CriticalHit);
        Assert.Equal("depleted", Resolve(He([4, 4])).AmmunitionUse);

        // Above 8 there was no round: at one hex the 9 would hit, yet no IFT DR is asked for, the ROF is kept, no fire counter is placed, and
        // the Acquisition stands.
        var shot = Gun("british", 1944, "he", toHit: [5, 4]) with { Range = 1, Acquisition = -1 };
        var none = Resolve(shot);
        Assert.Equal(OrdnanceResolution.Resolved, none.Disposition);
        Assert.Equal(("none", false, true, null, -1, null), (none.AmmunitionUse, none.ToHit!.Hit, none.Gun!.RateOfFireKept, none.Gun.FireCounter, none.Gun.Acquisition,
            none.Gun.AcquiredLocationId));
        Assert.Null(none.Hit);
        Assert.Null(none.CrewConcealmentLost);

        // The control: the same DR under an Elite force's HE9 is the last round, it hits, and the IFT DR is asked for.
        Assert.StartsWith("asl.a1.ordnance.roll-missing:hit:", Assert.Single(Resolve(shot with { Elite = true }).Reasons), StringComparison.Ordinal);

        // A malfunction on that DR still counts: the Gun has fired, and hit nothing.
        var broke = Resolve(Gun("british", 1944, "he", toHit: [6, 6]) with { Range = 1 });
        Assert.Equal(("none", true, false, "prep-fire", false), (broke.AmmunitionUse, broke.Gun!.Malfunctioned, broke.Gun.RateOfFireKept, broke.Gun.FireCounter, broke.ToHit!.Hit));
        Assert.Null(broke.Hit);

        // British Ordnance Note 6 (p. 426): HE7 from February 1943.
        Assert.Contains(HeUnavailable, Resolve(InMonth(He([2, 1], 1943), 1)).Reasons);
        Assert.Equal("used", Resolve(InMonth(He([2, 1], 1943), 2)).AmmunitionUse);
        Assert.Equal("depleted", Resolve(InMonth(He([4, 3], 1943), 2)).AmmunitionUse);
        Assert.Equal("none", Resolve(InMonth(He([4, 4], 1943), 2)).AmmunitionUse);
    }

    [Fact]
    public void AGunWhoseHeHasADepletionNumberNeverFiresItUndeclared()
    {
        // C2.21 (p. 167): a Gun "is otherwise always assumed to be firing HE vs unarmored targets ... provided that ammunition type is available,
        // is not Special Ammunition (i.e., has no Depletion #; 8.8)".
        Assert.Equal([HeNotNamed], Resolve(He([2, 1], ammunition: null)).Reasons);
        Assert.Equal([HeNotNamed], Resolve(He([2, 1], ammunition: "ap")).Reasons);
        Assert.Equal([HeNotNamed], Resolve(He([2, 1], ammunition: "apds")).Reasons);

        // Named, it needs the number's date (none in 1942, none with no year) and a round left.
        Assert.Equal([HeUnavailable], Resolve(He([2, 1], 1942)).Reasons);
        Assert.Equal([HeUnavailable], Resolve(He([2, 1]) with { ScenarioYear = null }).Reasons);
        Assert.Equal([HeUnavailable], Resolve(He([2, 1]).WithDepleted("he")).Reasons);
        Assert.Equal(OrdnanceResolution.Abstained, Resolve(He([2, 1], 1942)).Disposition);

        // Every other Gun fires HE at Infantry as it did, named or not, with no Depletion Number in play.
        var plain = Resolve(Gun("american", 1944, toHit: [5, 6]));
        Assert.Equal((OrdnanceResolution.Resolved, null), (plain.Disposition, plain.AmmunitionUse));
        Assert.Null(Resolve(Gun("american", 1944, "he", toHit: [5, 6])).AmmunitionUse);

        // What the player chooses from: at Infantry the 6-Pounder's HE alone, with its number; nothing for a Gun whose HE is assumed.
        var choice = Assert.Single(ScenarioA1Definitions.AmmunitionChoices("british-at-gun", false, 1944, 6, false, []));
        Assert.Equal(("he", "HE (Depletion Number 8)", 8, null), (choice.Ammunition, choice.Label, choice.DepletionNumber, choice.Unavailable));
        Assert.NotNull(Assert.Single(ScenarioA1Definitions.AmmunitionChoices("british-at-gun", false, 1942, 6, false, [])).Unavailable);
        Assert.Contains("C8.9", Assert.Single(ScenarioA1Definitions.AmmunitionChoices("british-at-gun", false, 1944, 6, false, ["he"])).Unavailable, StringComparison.Ordinal);
        Assert.Empty(ScenarioA1Definitions.AmmunitionChoices("attacker-inf-gun", false, 1944, 6, false, []));

        // At a vehicle: AP, APDS from June 1944, and HE by its number.
        Assert.Equal(["AP", "APDS (Depletion Number 6)", "HE (Depletion Number 8)"],
            ScenarioA1Definitions.AmmunitionChoices("british-at-gun", true, 1944, 6, false, []).Select(item => item.Label));
        Assert.Equal([null, "none at the scenario's date (C8.9)", null],
            ScenarioA1Definitions.AmmunitionChoices("british-at-gun", true, 1944, 5, false, []).Select(item => item.Unavailable));
        Assert.Equal(["AP", "APCR (Depletion Number 5)", "HE"], ScenarioA1Definitions.AmmunitionChoices("attacker-tank", true, 1942, 7, false, []).Select(item => item.Label));
    }

    [Fact]
    public void HeWithADepletionNumberFollowsItAtAVehicle()
    {
        // C8.8, C8.9: the 6-Pounder's HE at the PzKpfw IIIH, which its underscored caliber alone would deny (C2.21).
        OrdnanceShot AtTank(int[] toHit, int year = 1944)
        {
            var shot = Gun("british", year, "he", vehicle: true, toHit);
            return shot with { Rolls = shot.Rolls! with { ToKill = [6, 6] } };
        }

        Assert.Equal("used", Resolve(AtTank([4, 3])).AmmunitionUse);
        Assert.Equal("depleted", Resolve(AtTank([4, 4])).AmmunitionUse);
        var none = Resolve(AtTank([5, 4]));
        Assert.Equal(("none", false, true), (none.AmmunitionUse, none.ToHit!.Hit, none.Gun!.RateOfFireKept));
        Assert.Contains(Unavailable, Resolve(AtTank([4, 3], 1942)).Reasons);

        // AP has no number on this counter, and a Gun with no HE listing fires its HE at a vehicle with none.
        Assert.Null(Resolve(Gun("british", 1944, "ap", vehicle: true, toHit: [5, 6])).AmmunitionUse);
        Assert.Null(Resolve(Gun("american", 1944, "he", vehicle: true, toHit: [5, 6])).AmmunitionUse);
    }

    [Fact]
    public void AnEliteForceHasOneMoreOnEveryDepletionNumber()
    {
        // C8.2 (p. 178; ruling R55.6): an Original 7 is above the 6-Pounder's D6, and is the last round of an Elite force's D7.
        Assert.Equal("none", Resolve(Apds([4, 3])).AmmunitionUse);
        Assert.Equal("depleted", Resolve(Apds([4, 3]) with { Elite = true }).AmmunitionUse);
        Assert.Equal("used", Resolve(Apds([4, 2]) with { Elite = true }).AmmunitionUse);
        Assert.Equal("none", Resolve(Apds([4, 4]) with { Elite = true }).AmmunitionUse);

        // HE8 is HE9, on the Infantry Target Type; APCR's number too.
        Assert.Equal(("depleted", "none"), (Resolve(He([5, 4]) with { Elite = true }).AmmunitionUse, Resolve(He([5, 4])).AmmunitionUse));

        // The game's special rule names the side.
        Assert.True(ScenarioA1Definitions.EliteAmmunition(["night:2", "elite-ammunition:british"], "british"));
        Assert.False(ScenarioA1Definitions.EliteAmmunition(["elite-ammunition:british"], "german"));
        Assert.False(ScenarioA1Definitions.EliteAmmunition([], "british"));

        // The words of the proposal and the record, and of the list a player chooses from.
        Assert.Equal("Depletion Number 6", ScenarioA1Definitions.DepletionWords(Apds([4, 3])));
        Assert.Equal("Depletion Number 7 (6, and 1 for Elite, C8.2)", ScenarioA1Definitions.DepletionWords(Apds([4, 3]) with { Elite = true }));
        Assert.Null(ScenarioA1Definitions.DepletionWords(Gun("british", 1944, "ap", vehicle: true)));
        Assert.Null(ScenarioA1Definitions.DepletionWords(Gun("american", 1944)));
        Assert.Equal("APDS (Depletion Number 7: 6, and 1 for Elite, C8.2)",
            ScenarioA1Definitions.AmmunitionChoices("british-at-gun", true, 1944, 6, true, []).Single(item => item.Ammunition == "apds").Label);
    }

    [Fact]
    public void ApWithADepletionNumberFollowsIt()
    {
        // C8.8: "Such AFV types have a Depletion Number (in the form "AP#" or "HE#")". No counter of the catalog prints AP#, so a made-up 57L
        // Gun lists AP5, with no year: it holds in a game that names none.
        var reference = Ordnance.WithGun(MadeUp("AP5"));
        OrdnanceResolution Ap(int[] toHit, int? year = 1944)
        {
            var shot = Gun("british", year, "ap", vehicle: true, toHit);
            return ScenarioA1OrdnanceCalculator.Resolve(shot with { Gun = shot.Gun! with { DefinitionId = "made-up" }, Rolls = shot.Rolls! with { ToKill = [6, 6] } }, reference);
        }

        Assert.Equal(("used", "depleted", "none"), (Ap([2, 2]).AmmunitionUse, Ap([3, 2]).AmmunitionUse, Ap([3, 3]).AmmunitionUse));
        Assert.True(Ap([3, 3]).Gun!.RateOfFireKept);
        Assert.Null(Ap([3, 3]).Kill);
        Assert.Equal("depleted", Ap([3, 2], year: null).AmmunitionUse);

        // The catalog's own Guns are what they were in the package's reference.
        Assert.False(Ordnance.Guns.ContainsKey("made-up"));
        Assert.Same(Ordnance.Guns["british-at-gun"], reference.Guns["british-at-gun"]);
    }

    // C8.12 (pp. 177 and 178), the Ammunition Supply Chart for APCR/APDS, by year from 1941 to 1945; a parenthesized month is "the start of
    // availability in that year". Ruling R55.5: the game reads the counter, never this chart.
    private static readonly (string Nationality, string[] Sizes, string Type, (int Number, int? Month)?[] Years)[] SupplyChart =
    [
        ("german", ["50L"], "apcr", [(5, null), (6, null), (5, null), (4, null), null]),
        ("german", ["37L", "47L", "50", "88L"], "apcr", [(4, null), (5, null), (4, null), (3, null), null]),
        ("german", ["75L", "76L"], "apcr", [null, (5, null), (4, null), (3, null), null]),
        ("american", ["76L"], "apcr", [null, null, null, (4, 8), (5, null)]),
        ("american", ["90L"], "apcr", [null, null, null, null, (5, null)]),
        ("russian", ["45L", "45LL", "76L", "76LL"], "apcr", [null, (4, null), (5, null), (6, null), (7, null)]),
        ("russian", ["57LL", "85L"], "apcr", [null, null, (4, null), (5, null), (6, null)]),
        ("british", ["57L"], "apds", [null, null, null, (6, 6), (7, null)]),
        ("british", ["76LL"], "apds", [null, null, null, (5, 9), (6, null)]),
    ];

    // C8.12: "Some Guns/AFV have a different Depletion Number printed on their counter, which applies to that Gun/AFV." The catalog's counters
    // that differ from the chart, and the years they differ in.
    private static readonly Dictionary<string, int[]> DiffersFromTheSupplyChart = new()
    {
        // The PzKpfw IIIH's counter prints A4 for 1941 and A5 for 1942 and nothing later, so its A5 holds on (C8.91); the chart's 50 falls to
        // A4 in 1943 and A3 in 1944, and has none in 1945.
        ["attacker-tank"] = [1943, 1944, 1945],

        // The T-34 M41's counter ends with A6 for 1944, which holds in 1945; the chart's Russian 76L has A7 then.
        ["defender-tank"] = [1945],
    };

    [Fact]
    public void TheCatalogsCountersAgreeWithTheSupplyChartOrAreNamed()
    {
        var listed = Ordnance.Guns.Values.Where(gun => gun.SpecialAmmo.Any(item => (item.StartsWith('A') && !item.StartsWith("AP", StringComparison.Ordinal)) || item.StartsWith('D')))
            .OrderBy(gun => gun.Id, StringComparer.Ordinal).ToArray();
        Assert.Equal(["attacker-tank", "british-at-gun", "defender-at-gun", "defender-tank"], listed.Select(gun => gun.Id));
        foreach (var gun in listed)
        {
            var size = ScenarioA1ArmorReference.GunSize(gun);
            var row = Assert.Single(SupplyChart, item => item.Nationality == gun.Nationality && item.Sizes.Contains(size));
            var differs = new List<int>();
            for (var year = 1941; year <= 1945; year++)
            {
                var chart = row.Years[year - 1941];
                var agrees = chart?.Number == Number(gun, row.Type, year, 12)
                    && (chart?.Month is not { } month || (Number(gun, row.Type, year, month - 1) is null && Number(gun, row.Type, year, month) == chart.Value.Number));
                if (!agrees)
                {
                    differs.Add(year);
                }
            }

            Assert.True(differs.SequenceEqual(DiffersFromTheSupplyChart.GetValueOrDefault(gun.Id) ?? []), $"{gun.Id} ({size}) differs from the chart in {string.Join(", ", differs)}");
        }

        // The 6-Pounder's "(J4+E)" is the chart's "D6(Jun)", and its D7 the chart's 1945; the Russian 45L AT Gun is the chart's row year by year.
        Assert.DoesNotContain("british-at-gun", DiffersFromTheSupplyChart.Keys);
        Assert.DoesNotContain("defender-at-gun", DiffersFromTheSupplyChart.Keys);
    }

    [Fact]
    public void TheDepletionNumbersOfTheEarlierCountersDoNotMove()
    {
        // C8.91: a number holds from its year on, and the last listed one after its year, as the game had it before the notation grew.
        var guns = Ordnance.Guns.Values.Where(gun => !Pass55.Contains(gun.Id)).OrderBy(gun => gun.Id, StringComparer.Ordinal).ToArray();
        Assert.Contains(guns, gun => gun.Id == "attacker-inf-gun");
        foreach (var gun in guns)
        {
            foreach (var type in Types)
            {
                foreach (var month in Months)
                {
                    for (var year = 1939; year <= 1945; year++)
                    {
                        // C8.3: the German 7.5cm leIG 18's H7 from May 1942.
                        int? expected = (gun.Id, type) == ("attacker-inf-gun", "heat")
                            ? (year > 1942 || (year == 1942 && month >= 5) ? 7 : null)
                            : Pinned.TryGetValue((gun.Id, type), out var byYear) ? byYear[year - 1939] : null;
                        Assert.True(expected == ScenarioA1Definitions.DepletionNumber(gun, type, year, month), $"{gun.Id} {type} {year}-{month}");
                    }

                    Assert.Null(ScenarioA1Definitions.DepletionNumber(gun, type, null, month));
                }
            }
        }
    }

    // Task 55.2 (increment 2d): Canister (C8.4 to C8.42, p. 178; rulings R55.8 to R55.10).

    private const string GunAt = "bd01:F5:0";
    private const string CanisterOutside = "asl.a1.ordnance.canister-outside";

    private static readonly string[] Cluster = ["bd01:D4:0", "bd01:D5:0", "bd01:C5:0"];

    /// <summary>
    /// The American 37mm (C7 from August 1942, ROF 2, B12, NT) firing Canister in the PFPh of July 1943 at aim 4 of its CA: three hexes of the
    /// cluster and the adjacent hex on the LOS, unless other Locations are given.
    /// </summary>
    private static OrdnanceShot Canister(int[]? dice, int aim = 4, int turn = 0, int year = 1943, bool woods = false, bool intensive = false, bool pinned = false,
        OrdnanceCanisterLocation[]? locations = null)
    {
        var hit = new FireAttack("PFPh", "phasing", null, null, Cluster[0], [], null, null, null, null, 7, null, [], 3, null);
        return new OrdnanceShot("PFPh", "phasing", "american", new OrdnanceGun("gun", "american-at-gun", false, intensive ? 1 : 0, false, intensive),
            new OrdnanceCrew("crew", "american-crew", false, pinned, false, false, false), Cluster[0], 2, turn, woods, true, 0, hit,
            new OrdnanceRolls(null, null, null, null, null) { Canister = dice })
        {
            Ammunition = "canister",
            ScenarioYear = year,
            IntensiveFire = intensive ? true : null,
            Canister = new OrdnanceCanister(aim, "west", locations ?? [.. Cluster.Select(at => new OrdnanceCanisterLocation(at)), new OrdnanceCanisterLocation("bd01:E5:0") { AdjacentHexes = 1 }]),
        };
    }

    /// <summary>A Canister attack on a German squad in a Location at range 2, with the IFT DR.</summary>
    private static FireAttack CanisterAttack(FireCanister canister, int[] dice, string terrain = "open-ground", int hindrance = 0, bool concealed = false,
        IReadOnlyList<FireVehicle>? vehicles = null, bool empty = false) =>
        new FireAttack("PFPh", "phasing", null, GunAt, At, [], null, 2, true, new FireLos(false, hindrance, true, false), 7, terrain,
            empty ? [] : [new FireTarget("de-s", "attacker-squad", At, false, false, concealed, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 3,
            new FireRolls(dice, null, null, null))
        {
            Canister = canister,
            Vehicles = vehicles,
        };

    // The test's own hex grid, as the map has it: flat-topped hexes in columns, odd columns half a hex higher; a hexside is numbered from north clockwise.
    private static (int Column, int Row) Neighbor((int Column, int Row) hex, int side)
    {
        var even = hex.Column % 2 == 0;
        return side switch
        {
            0 => (hex.Column, hex.Row - 1),
            1 => (hex.Column + 1, hex.Row + (even ? 0 : -1)),
            2 => (hex.Column + 1, hex.Row + (even ? 1 : 0)),
            3 => (hex.Column, hex.Row + 1),
            4 => (hex.Column - 1, hex.Row + (even ? 1 : 0)),
            _ => (hex.Column - 1, hex.Row + (even ? 0 : -1)),
        };
    }

    private static (int Column, int Row) Walk((int Column, int Row) firer, int facing, CanisterOffset offset)
    {
        var (left, right) = ScenarioA1CanisterCalculator.ArcSides(facing);
        var hex = firer;
        for (var step = 0; step < offset.Left; step++)
        {
            hex = Neighbor(hex, left);
        }

        for (var step = 0; step < offset.Right; step++)
        {
            hex = Neighbor(hex, right);
        }

        return hex;
    }

    private static int Distance((int Column, int Row) one, (int Column, int Row) two)
    {
        static (int Q, int R) Axial((int Column, int Row) hex) => (hex.Column, hex.Row - ((hex.Column + (hex.Column & 1)) / 2));
        var (a, b) = (Axial(one), Axial(two));
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.Q + a.R - b.Q - b.R)) / 2;
    }

    /// <summary>A hex as the boards name it: column A is 0; an odd column's rows are numbered from 0, an even column's from 1.</summary>
    private static string Name((int Column, int Row) hex)
    {
        var letter = (char)('A' + (hex.Column % 26));
        return new string(letter, (hex.Column / 26) + 1) + (hex.Row + (hex.Column % 2 == 0 ? 1 : 0)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void ACoveredArcHasFiveCanisterAims()
    {
        // The EX of C8.4 and its figure (p. 178): "only five possible vertex aiming points (and corresponding three-hex target clusters) exist in
        // each CA as illustrated": a firer in Z6 whose CA holds Z5 and AA6, and the clusters Z3/Z4/AA4, Z4/AA4/AA5, AA4/AA5/BB4, AA5/BB4/BB5,
        // BB4/BB5/CC5. Z is column 25; that CA is the hexspine to the north-east.
        Assert.Equal(5, ScenarioA1CanisterCalculator.Aims.Count);
        var z6 = (Column: 25, Row: 6);
        Assert.Equal("Z6", Name(z6));
        Assert.Equal((0, 1), ScenarioA1CanisterCalculator.ArcSides(1));
        string[][] figure = [["Z3", "Z4", "AA4"], ["Z4", "AA4", "AA5"], ["AA4", "AA5", "BB4"], ["AA5", "BB4", "BB5"], ["BB4", "BB5", "CC5"]];
        string[][] adjacent = [["Z5"], ["Z5"], ["Z5", "AA6"], ["AA6"], ["AA6"]];
        foreach (var aim in ScenarioA1CanisterCalculator.Aims)
        {
            Assert.Equal(figure[aim.Aim - 1].Order(StringComparer.Ordinal), aim.Cluster.Select(offset => Name(Walk(z6, 1, offset))).Order(StringComparer.Ordinal));
            Assert.Equal(adjacent[aim.Aim - 1], aim.Adjacent.Select(offset => Name(Walk(z6, 1, offset))));
            Assert.Same(aim, ScenarioA1CanisterCalculator.Aim(aim.Aim));
        }

        Assert.Null(ScenarioA1CanisterCalculator.Aim(0));
        Assert.Null(ScenarioA1CanisterCalculator.Aim(6));

        // Ruling R55.8: for every facing and both column parities, each cluster is three hexes that share a vertex (each adjacent to the other
        // two) at range 2 or 3, the nearest at 2, and the adjacent hexes are the two of the CA. East faces the north-east and south-east hexes.
        Assert.Equal((1, 2), ScenarioA1CanisterCalculator.ArcSides(0));
        Assert.Equal((4, 5), ScenarioA1CanisterCalculator.ArcSides(3));
        foreach (var firer in new[] { (Column: 10, Row: 10), (Column: 11, Row: 10) })
        {
            var all = new HashSet<string>(StringComparer.Ordinal);
            for (var facing = 0; facing < 6; facing++)
            {
                var (left, right) = ScenarioA1CanisterCalculator.ArcSides(facing);
                Assert.Equal((left + 1) % 6, right);
                foreach (var aim in ScenarioA1CanisterCalculator.Aims)
                {
                    var hexes = aim.Cluster.Select(offset => Walk(firer, facing, offset)).ToArray();
                    Assert.Equal(3, hexes.Distinct().Count());
                    Assert.All(hexes, one => Assert.All(hexes.Where(other => other != one), other => Assert.Equal(1, Distance(one, other))));
                    int[] ranges = [.. hexes.Select(hex => Distance(firer, hex)).Order()];
                    Assert.True(ranges is [2, 3, 3] or [2, 2, 3], $"{firer} facing {facing} aim {aim.Aim}: {string.Join(",", ranges)}");
                    Assert.All(aim.Adjacent, offset => Assert.Equal(1, Distance(firer, Walk(firer, facing, offset))));
                    Assert.True(all.Add(string.Join("/", hexes.Select(Name).Order(StringComparer.Ordinal))));
                }
            }

            Assert.Equal(30, all.Count);
        }
    }

    [Fact]
    public void CanisterHasTheFirepowerOfItsGunSizeAndItsHalvings()
    {
        // The Canister FP IFT Equivalency Table (C8.4).
        Assert.Equal((12, 20, 24), (ScenarioA1CanisterCalculator.Firepower(37), ScenarioA1CanisterCalculator.Firepower(75), ScenarioA1CanisterCalculator.Firepower(105)));
        Assert.Null(ScenarioA1CanisterCalculator.Firepower(57));
        Assert.Null(ScenarioA1CanisterCalculator.Firepower(76));

        static decimal Factor(int? adjacent = null, bool bounding = false, bool motion = false, bool intensive = false, bool pinned = false) =>
            ScenarioA1CanisterCalculator.Multipliers(adjacent, bounding, motion, intensive, pinned).Aggregate(1m, (value, item) => value * item.Value);

        // C8.4, C8.41, D3.31, D2.42, A7.81: each halves, and they multiply; "quartered if both Motion and Bounding Fire penalties apply"; the two
        // adjacent hexes of a LOS along their hexside are "quartered instead of halved". Ruling R55.10: nothing for the AFPh.
        Assert.Equal(1m, Factor());
        Assert.Equal(0.5m, Factor(adjacent: 1));
        Assert.Equal(0.25m, Factor(adjacent: 2));
        Assert.Equal(0.5m, Factor(bounding: true));
        Assert.Equal(0.5m, Factor(motion: true));
        Assert.Equal(0.25m, Factor(bounding: true, motion: true));
        Assert.Equal(0.5m, Factor(intensive: true));
        Assert.Equal(0.5m, Factor(pinned: true));
        Assert.Equal(0.125m, Factor(adjacent: 1, intensive: true, pinned: true));

        // On the IFT: 12 FP, 6 in the adjacent hex, 3 with Bounding First Fire and Motion (the 2 column), and half again against a concealed unit.
        var full = Shoot(CanisterAttack(new FireCanister("gun", 12), [5, 6])).Arithmetic!;
        Assert.Equal((12m, 12), (full.TotalFirepower, full.ColumnFp));
        Assert.Empty(full.Drm);
        var adjacentHex = Shoot(CanisterAttack(new FireCanister("gun", 12) { AdjacentHexes = 1 }, [5, 6])).Arithmetic!;
        Assert.Equal((6m, 6), (adjacentHex.TotalFirepower, adjacentHex.ColumnFp));
        Assert.Equal("C8.41", Assert.Single(Assert.Single(adjacentHex.Firers).Multipliers).Rule);
        var quartered = Shoot(CanisterAttack(new FireCanister("gun", 12) { BoundingFirstFire = true, Motion = true }, [5, 6])).Arithmetic!;
        Assert.Equal((3m, 2), (quartered.TotalFirepower, quartered.ColumnFp));
        Assert.Equal(["D3.31", "D2.42"], Assert.Single(quartered.Firers).Multipliers.Select(item => item.Rule));
        var hexside = Shoot(CanisterAttack(new FireCanister("gun", 20) { AdjacentHexes = 2 }, [5, 6])).Arithmetic!;
        Assert.Equal((5m, 4), (hexside.TotalFirepower, hexside.ColumnFp));
        var concealed = Shoot(CanisterAttack(new FireCanister("gun", 12) { IntensiveFire = true }, [5, 6], concealed: true)).Arithmetic!;
        Assert.Equal(3m, concealed.TotalFirepower);
        Assert.Equal(["C8.4", "C8.4"], Assert.Single(concealed.Firers).Multipliers.Select(item => item.Rule));

        // A Location the firing side sees nothing in is attacked as a hidden unit's would be: on the halved FP.
        Assert.Equal(6m, Shoot(CanisterAttack(new FireCanister("gun", 12), [5, 6], empty: true)).Arithmetic!.TotalFirepower);
    }

    [Fact]
    public void ACanisterAttackTakesItsLocationsTemAndHindranceAndItsCaseA()
    {
        // C8.4: "resolved on the IFT using its FP and adding any applicable DRM for TEM, LOS Hindrance, and/or hexspine changes in the CA";
        // "LOS Hindrances and the individual TEM of each Location may vary the results in each hex".
        var woods = Shoot(CanisterAttack(new FireCanister("gun", 12), [5, 6], "woods", hindrance: 1)).Arithmetic!;
        Assert.Equal([("tem:woods", 1m), ("los-hindrance", 1m)], woods.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal(13, woods.FinalDr);
        var building = Shoot(CanisterAttack(new FireCanister("gun", 12) { CaseA = 3 }, [5, 6], "stone-building")).Arithmetic!;
        Assert.Equal([("tem:stone-building", 3m), ("case-a:gun", 3m)], building.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal("D3.52", building.Drm[^1].Rule);

        // C3.21, D3.52, C5.1, C5.11: the NT Gun's Case A, 3 for the first hexspine and 1 for each more, doubled in woods or a building.
        Assert.Equal((3, 4, 5), (ScenarioA1CanisterCalculator.CaseA(null, 1, false), ScenarioA1CanisterCalculator.CaseA(null, 2, false), ScenarioA1CanisterCalculator.CaseA(null, 3, false)));
        Assert.Equal(6, ScenarioA1CanisterCalculator.CaseA(null, 1, true));
        Assert.Equal(1, ScenarioA1CanisterCalculator.CaseA("t", 1, false));
        Assert.Equal(0, ScenarioA1CanisterCalculator.CaseA(null, 0, true));
        var gun = Ordnance.Guns["american-at-gun"];
        var fact = ScenarioA1CanisterCalculator.AttackFact(Canister(null, turn: 2, woods: true, intensive: true), gun, new OrdnanceCanisterLocation(At) { AdjacentHexes = 1 });
        Assert.Equal(new FireCanister("gun", 12) { AdjacentHexes = 1, IntensiveFire = true, CaseA = 8 }, fact);
        Assert.Equal(new FireCanister("gun", 12), ScenarioA1CanisterCalculator.AttackFact(Canister(null), gun, new OrdnanceCanisterLocation(At)));
        Assert.Equal(new FireCanister("gun", 12) { PinnedCrew = true }, ScenarioA1CanisterCalculator.AttackFact(Canister(null, pinned: true), gun, new OrdnanceCanisterLocation(At)));

        // A7.9: "Cowering affects all fire except that from ... Canister": doubles shift no column. No fire counter and no firer comes of the
        // attack: the Gun's are the shot's own.
        var doubles = Shoot(CanisterAttack(new FireCanister("gun", 12), [6, 6]));
        Assert.Equal(FireResolution.Resolved, doubles.Disposition);
        Assert.False(doubles.Arithmetic!.Cowered);
        Assert.Equal((0, 12), (doubles.Arithmetic.ColumnShift, doubles.Arithmetic.ColumnFp));
        Assert.Null(doubles.FireCounter);
        Assert.Empty(doubles.FireCounterUnitIds);
        Assert.Null(doubles.Arithmetic.ResidualFp);

        // C8.4: "ineffective vs armored targets (but not their Vulnerable PRC)": a BU AFV in the Location is unharmed.
        var armored = Shoot(CanisterAttack(new FireCanister("gun", 12), [5, 6], vehicles: [new FireVehicle("de-tank", "attacker-tank", At, false, false, false, false)]));
        Assert.Equal(FireResolution.Resolved, armored.Disposition);
        var afv = Assert.Single(armored.VehicleEffects!);
        Assert.Equal((FireVehicleEffect.None, FireVehicleEffect.NotVulnerable), (afv.Result, afv.CrewResult));

        // A7.51, A7.4, A8.15: no leader, no firers, no unit of its own side, no Snap Shot, and a Location at the firer's own elevation.
        const string outside = "asl.a1.fire.canister-outside";
        var plain = CanisterAttack(new FireCanister("gun", 12), [5, 6]);
        Assert.Contains(outside, Shoot(plain with { SameLevel = false, TargetLevelAbove = 1 }).Reasons);
        Assert.Contains(outside, Shoot(plain with { SnapShot = true }).Reasons);
        Assert.Contains(outside, Shoot(plain with { Los = new FireLos(true, 0, true, false) }).Reasons);
        Assert.Contains(outside, Shoot(plain with { Canister = new FireCanister("gun", 16) }).Reasons);
        Assert.Contains(outside, Shoot(plain with { Canister = new FireCanister("gun", 12) { AdjacentHexes = 3 } }).Reasons);
        Assert.Contains("asl.a1.fire.target-outside", Shoot(plain with { Targets = [plain.Targets![0] with { Friendly = true }] }).Reasons);
        Assert.Contains("asl.a1.fire.phase-outside", Shoot(plain with { Phase = "MPh", FiringSide = "non-phasing" }).Reasons);
    }

    [Fact]
    public void TheCanisterDrIsTheDepletionMalfunctionAndRofCheck()
    {
        // C8.4: "A To Hit DR is not required for Canister ... The IFT DR also serves as the Ammo Depletion, armament breakdown, and ROF checks".
        Assert.Equal(["asl.a1.ordnance.roll-missing:canister"], Resolve(Canister(null)).Reasons);
        Assert.Empty(ScenarioA1OrdnanceCalculator.Precheck(Canister(null), Ordnance));

        // C8.9: below the Depletion Number 7 the round is used; the colored dr of 2 keeps the ROF of 2 (C2.24).
        var used = Resolve(Canister([2, 4]));
        Assert.Equal(OrdnanceResolution.Resolved, used.Disposition);
        Assert.Null(used.ToHit);
        Assert.Equal("used", used.AmmunitionUse);
        Assert.Equal(new OrdnanceGunEffect(12, false, 2, true, null, 0, null), used.Gun);
        var block = used.Canister!;
        Assert.Equal((4, "west", 12, 6, 0, 7), (block.Aim, block.Facing, block.Firepower, block.OriginalDr, block.CaseA, block.DepletionNumber));
        Assert.Equal([2, 4], block.Dice);
        Assert.Equal([(Cluster[0], 12m), (Cluster[1], 12m), (Cluster[2], 12m), ("bd01:E5:0", 6m)], block.Attacks.Select(item => (item.LocationId, item.Firepower)));

        // At the number it is the last round; a colored dr of 3 loses the ROF and leaves the Prep Fire counter.
        var last = Resolve(Canister([3, 4]));
        Assert.Equal("depleted", last.AmmunitionUse);
        Assert.Equal(new OrdnanceGunEffect(12, false, 2, false, "prep-fire", 0, null), last.Gun);
        Assert.Equal(4, last.Canister!.Attacks.Count);

        // Above it "the firer had no such ammunition and is considered not to have fired yet for any purpose": nothing is attacked, no counter,
        // the ROF kept whatever the colored dr.
        var none = Resolve(Canister([6, 2]));
        Assert.Equal(OrdnanceResolution.Resolved, none.Disposition);
        Assert.Equal("none", none.AmmunitionUse);
        Assert.Empty(none.Canister!.Attacks);
        Assert.Equal(new OrdnanceGunEffect(12, false, 2, true, null, 0, null), none.Gun);

        // "unless Gun Malfunction ... occurs": an Original 12 malfunctions the Gun (C2.28), which then has fired.
        var broken = Resolve(Canister([6, 6]));
        Assert.Equal("none", broken.AmmunitionUse);
        Assert.Empty(broken.Canister!.Attacks);
        Assert.Equal(new OrdnanceGunEffect(12, true, 2, false, "prep-fire", 0, null), broken.Gun);

        // C8.2: an Elite force's number is 8. C5.62, C8.92: Intensive Fire lowers the B# to 10 and not the Depletion Number; its FP is halved.
        Assert.Equal("depleted", Resolve(Canister([4, 4]) with { Elite = true }).AmmunitionUse);
        var intensive = Resolve(Canister([1, 3], intensive: true));
        Assert.Equal(new OrdnanceGunEffect(10, false, 0, false, "intensive-fire", 0, null), intensive.Gun);
        Assert.Equal([6m, 6m, 6m, 3m], intensive.Canister!.Attacks.Select(item => item.Firepower));
        Assert.Equal(["C8.41", "C8.4"], intensive.Canister.Attacks[^1].Multipliers.Select(item => item.Rule));
        Assert.True(Resolve(Canister([5, 5], intensive: true) with { Elite = true }).Gun!.Malfunctioned);

        // C2.5: an NT Gun's ROF is one lower after a CA change; D3.52, C5.11: Case A on the IFT DR, doubled in woods.
        var turned = Resolve(Canister([2, 1], turn: 1));
        Assert.Equal((false, 3), (turned.Gun!.RateOfFireKept, turned.Canister!.CaseA));
        Assert.True(Resolve(Canister([1, 2], turn: 1)).Gun!.RateOfFireKept);
        Assert.Equal(8, Resolve(Canister([1, 2], turn: 2, woods: true)).Canister!.CaseA);

        // A7.81: a pinned crew fires it as Area Fire, keeps no ROF, and changes no CA; C5.2: no ROF in the AFPh, and no halving there (R55.10).
        var pinned = Resolve(Canister([1, 2], pinned: true));
        Assert.Equal((false, 6m), (pinned.Gun!.RateOfFireKept, pinned.Canister!.Attacks[0].Firepower));
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed", Resolve(Canister([1, 2], turn: 1, pinned: true)).Reasons);
        var afph = Canister([1, 2]);
        var advancing = Resolve(afph with { Phase = "AFPh", Hit = afph.Hit! with { Phase = "AFPh" } });
        Assert.Equal((false, 12m), (advancing.Gun!.RateOfFireKept, advancing.Canister!.Attacks[0].Firepower));

        // C6.5: the Gun loses its Acquisition and gains none.
        Assert.Equal(0, used.Gun!.Acquisition);
        Assert.Null(used.Gun.AcquiredLocationId);
    }

    [Fact]
    public void ACanisterShotIsOutsideWithoutItsRoundItsGunOrItsAim()
    {
        // U.S. Ordnance Note C: Canister from August 1942; C8.9: none once it has run out.
        Assert.Contains("asl.a1.ordnance.ammunition-unavailable", Resolve(InMonth(Canister([1, 2], year: 1942), 7)).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(InMonth(Canister([1, 2], year: 1942), 8)).Disposition);
        var spent = Canister([1, 2]);
        Assert.Contains("asl.a1.ordnance.ammunition-depleted", Resolve(spent with { Gun = spent.Gun! with { Depleted = ["canister"] } }).Reasons);

        // C8.4's table gives no FP to a 57mm Gun, and the 6-Pounder's counter lists no Canister.
        var british = Canister([1, 2]) with { FiringNationality = "british", Gun = new OrdnanceGun("gun", "british-at-gun", false, 0, false, false),
            Crew = new OrdnanceCrew("crew", "british-crew", false, false, false, false, false) };
        Assert.Contains("asl.a1.ordnance.canister-gun-outside", Resolve(british).Reasons);

        // Ruling R55.8: an aim is 1 to 5; aim 3 alone has two adjacent hexes; a Location is attacked once.
        Assert.Contains(CanisterOutside, Resolve(Canister([1, 2], aim: 6)).Reasons);
        Assert.Contains(CanisterOutside, Resolve(Canister([1, 2], aim: 3)).Reasons);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Canister([1, 2], aim: 3, locations: [new OrdnanceCanisterLocation(Cluster[0]),
            new OrdnanceCanisterLocation("bd01:E5:0") { AdjacentHexes = 2 }, new OrdnanceCanisterLocation("bd01:E6:0") { AdjacentHexes = 2 }])).Disposition);
        Assert.Contains(CanisterOutside, Resolve(Canister([1, 2], locations: [new OrdnanceCanisterLocation(Cluster[0]), new OrdnanceCanisterLocation(Cluster[0])])).Reasons);
        Assert.Contains("asl.a1.ordnance.roll-malformed", Resolve(Canister([1, 7])).Reasons);

        // The Canister facts belong to a Canister shot; the MPh is not built.
        Assert.Contains(CanisterOutside, Resolve(Gun("american", 1944, toHit: [5, 6]) with { Canister = Canister(null).Canister }).Reasons);
        var first = Canister([1, 2]);
        Assert.Contains("asl.a1.ordnance.canister-movement-phase", Resolve(first with { Phase = "MPh", FiringSide = "non-phasing", FireKind = "first-fire",
            Movement = new OrdnanceMovement(null, true, true, 1, 0), Hit = first.Hit! with { Phase = "MPh", FiringSide = "non-phasing" } }).Reasons);

        // A concealed crew's shot is decided with the planner's read of who sees it fire (A12.14).
        var hidden = Canister([1, 2]) with { Crew = new OrdnanceCrew("crew", "american-crew", false, false, false, true, false) };
        Assert.Contains("asl.a1.ordnance.concealment-unreviewed:crew", Resolve(hidden).Reasons);
        Assert.True(Resolve(hidden with { CrewSeen = true }).CrewConcealmentLost);
        Assert.Null(Resolve(hidden with { CrewSeen = false }).CrewConcealmentLost);
    }

    [Fact]
    public void AGunThatListsCanisterOffersItBesideItsHe()
    {
        // C8.4, C2.21 (pass 55): at Infantry the 37mm chooses between the HE it fires and its Canister, with the number at the game's date.
        Assert.Equal([("he", "HE", null), ("canister", "Canister (Depletion Number 7)", 7)],
            ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1944, 6, false, []).Select(item => (item.Ammunition, item.Label, item.DepletionNumber)));
        Assert.All(ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1944, 6, false, []), item => Assert.Null(item.Unavailable));
        Assert.Equal("Canister (Depletion Number 8: 7, and 1 for Elite, C8.2)",
            ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1944, 6, true, []).Single(item => item.Ammunition == "canister").Label);
        Assert.Equal("none at the scenario's date (C8.9)",
            ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1942, 7, false, []).Single(item => item.Ammunition == "canister").Unavailable);
        Assert.Equal("it has run out (C8.9)",
            ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1944, 6, false, ["canister"]).Single(item => item.Ammunition == "canister").Unavailable);
        Assert.Equal("Depletion Number 7", ScenarioA1Definitions.DepletionWords(Canister(null)));
        Assert.Equal("aim 4 of the CA facing west", ScenarioA1CanisterCalculator.AimWords(4, "west"));
        Assert.True(ScenarioA1OrdnanceCalculator.IsCanister(Canister(null)));
        Assert.False(ScenarioA1OrdnanceCalculator.IsCanister(Gun("american", 1944, "canister", vehicle: true)));
        Assert.True(ScenarioA1OrdnanceEligibility.CanisterElevation(1, 1));
        Assert.False(ScenarioA1OrdnanceEligibility.CanisterElevation(0, 1));
        Assert.Null(ScenarioA1OrdnanceEligibility.CanisterFacingRefusal("west", "west"));
        Assert.NotNull(ScenarioA1OrdnanceEligibility.CanisterFacingRefusal("west", "east"));
    }

    // Task 55.5 (increment 2e): Armor Leaders and Inexperienced Crews (D3.4 to D3.45, p. 200; rulings R55.15 to R55.18).

    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;

    /// <summary>The four cases of the modifier: none, an 8-1 or 9-1, a 9-2 or 10-2, and an Inexperienced Crew.</summary>
    private static readonly (int? Leadership, string? Name, string? Rule)[] Leaderships =
        [(null, null, null), (-1, "armor-leader", "D3.44"), (-2, "armor-leader", "D3.44"), (1, "inexperienced-crew", "D3.45")];

    private static void AssertLeadership(IEnumerable<FireModifier> drm, (int? Leadership, string? Name, string? Rule) given)
    {
        if (given.Name is null)
        {
            Assert.DoesNotContain(drm, item => ScenarioA1ArmorLeaders.IsModifier(item.Name));
        }
        else
        {
            Assert.Contains(drm, item => item.Name == given.Name && item.Value == given.Leadership && item.Rule == given.Rule);
        }
    }

    /// <summary>The T-34 M41 firing AP at the PzKpfw IIIH at 3 hexes in 1942, as pass 7's tests have it, with the firer's modifier and the target crew's morale.</summary>
    private static OrdnanceShot TankShot(int? leadership = null, int? crewMorale = null, bool buttonedUp = true, int[]? toHit = null, int[]? toKill = null,
        int[]? shock = null, int[]? crew = null) =>
        new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("defender-tank-1", "defender-tank", false, 0, false, false),
            new OrdnanceCrew("defender-tank-1", "defender-tank", false, false, false, false, false), At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls(toHit ?? [2, 4], null, null, null, null) { ToKill = toKill, ShockCheck = shock, CrewCheck = crew })
        {
            Vehicle = new OrdnanceVehicleFirer(buttonedUp, false, false, false, false) { ArmorLeadership = leadership },
            VehicleTarget = new OrdnanceVehicleTarget("attacker-tank-1", "attacker-tank", "front", "front", false, false, false, false, true) { CrewMorale = crewMorale },
            Ammunition = "ap",
            ScenarioYear = 1942,
        };

    private static FireResolution Finish(FireAttack attack)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with { Rolls = rolls }, Fire);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:checks:", StringComparison.Ordinal))
            {
                return result;
            }

            rolls = rolls with { Checks = reason["asl.a1.fire.roll-missing:checks:".Length..].Split(',').ToDictionary(id => id, _ => (IReadOnlyList<int>)[2, 2]) };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void AnArmorLeaderIsOfOneOfFourGrades()
    {
        // D3.4, H1.43: "a Strength Factor ranging from 8-1 to 10-2"; the Availability Table lists 8-1, 9-1, 9-2, and 10-2.
        Assert.All(new[] { (8, -1), (9, -1), (9, -2), (10, -2) }, grade => Assert.True(ScenarioA1ArmorLeaders.IsGrade(grade.Item1, grade.Item2)));
        Assert.All(new[] { (8, 0), (8, -2), (10, -1), (10, -3), (7, -1), (6, 1), (9, 1) }, grade => Assert.False(ScenarioA1ArmorLeaders.IsGrade(grade.Item1, grade.Item2)));
        Assert.Null(ScenarioA1ArmorLeaders.AssignmentRefusal("t1", true, 9, -1, false));
        Assert.Contains("8-1, 9-1, 9-2, and 10-2", ScenarioA1ArmorLeaders.AssignmentRefusal("t1", true, 10, -1, false)!.Text, StringComparison.Ordinal);
        Assert.Contains("not a vehicle", ScenarioA1ArmorLeaders.AssignmentRefusal("s1", false, 9, -1, false)!.Text, StringComparison.Ordinal);

        // D3.45: the Inexperienced Crew's 6+1 is its leader, so it takes no other.
        Assert.Contains("D3.45", ScenarioA1ArmorLeaders.AssignmentRefusal("t1", true, 9, -1, true)!.Text, StringComparison.Ordinal);

        // D3.41, D3.45: either belongs to an armed AFV; a truck and a squad are refused at setup.
        Assert.True(ScenarioA1ArmorLeaders.ArmedAfv(true, null, 50));
        Assert.True(ScenarioA1ArmorLeaders.ArmedAfv(true, "aamg", null));
        Assert.False(ScenarioA1ArmorLeaders.ArmedAfv(false, "aamg", 50));
        Assert.False(ScenarioA1ArmorLeaders.ArmedAfv(true, null, null));
        Assert.Null(ScenarioA1ArmorLeaders.SetupBar("t1", true, false, true));
        Assert.Null(ScenarioA1ArmorLeaders.SetupBar("s1", false, false, false));
        Assert.StartsWith("play.setup-armor-leader", ScenarioA1ArmorLeaders.SetupBar("truck", true, false, false), StringComparison.Ordinal);
        Assert.StartsWith("play.setup-inexperienced-crew", ScenarioA1ArmorLeaders.SetupBar("truck", false, true, false), StringComparison.Ordinal);

        // The facts a vehicle's crew gives: the Inexperienced Crew's 6+1 before anything else.
        Assert.Equal((-2, 10), (ScenarioA1ArmorLeaders.Leadership(-2, false), ScenarioA1ArmorLeaders.CrewMorale(10, false)));
        Assert.Equal((1, 6), (ScenarioA1ArmorLeaders.Leadership(null, true), ScenarioA1ArmorLeaders.CrewMorale(null, true)));
        Assert.Equal(((int?)null, (int?)null), (ScenarioA1ArmorLeaders.Leadership(null, false), ScenarioA1ArmorLeaders.CrewMorale(null, false)));
        Assert.Equal(("Armor Leader -1 (D3.44)", "Inexperienced Crew +1 (D3.45)", string.Empty),
            (ScenarioA1ArmorLeaders.ModifierText(-1), ScenarioA1ArmorLeaders.ModifierText(1), ScenarioA1ArmorLeaders.ModifierText(null)));
        Assert.Equal(("crew morale 9, its Armor Leader's (D3.42)", "crew morale 6, an Inexperienced Crew's (D3.45)", string.Empty),
            (ScenarioA1ArmorLeaders.MoraleText("armor-leader", 9), ScenarioA1ArmorLeaders.MoraleText("inexperienced-crew", 6), ScenarioA1ArmorLeaders.MoraleText(null, 8)));
        Assert.Equal("9-1", ScenarioA1ArmorLeaders.Grade(9, -1));
    }

    [Fact]
    public void AnArmorLeaderModifiesHisVehiclesMaToHitDr()
    {
        // D3.44: "can be used only to modify a vehicle's MA DR (To Hit if ordnance; IFT otherwise)"; D3.45: an Inexperienced Crew adds one.
        var plain = Resolve(TankShot(toKill: [6, 6])).ToHit!;
        foreach (var given in Leaderships)
        {
            // The Vehicle Target Type; "being BU does not void an Armor Leader's effects, although BU penalties (Case I To Hit DRM) still apply".
            var atVehicle = Resolve(TankShot(given.Leadership, toKill: [6, 6])).ToHit!;
            Assert.Equal(plain.FinalDr + (given.Leadership ?? 0), atVehicle.FinalDr);
            AssertLeadership(atVehicle.Drm, given);
            Assert.Contains(atVehicle.Drm, item => item.Name == "case-i" && item.Value == 1);

            // The Infantry Target Type: the same tank's HE at a German squad; an Original 12 misses and asks for no IFT DR.
            var shot = TankShot(given.Leadership, toHit: [6, 6]);
            var atInfantry = Resolve(shot with
            {
                VehicleTarget = null,
                Ammunition = null,
                Hit = shot.Hit! with
                {
                    Targets = [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }],
                },
            });
            Assert.Equal(OrdnanceResolution.Resolved, atInfantry.Disposition);
            AssertLeadership(atInfantry.ToHit!.Drm, given);
            Assert.Contains(atInfantry.ToHit.Drm, item => item.Name == "case-i");
        }

        // The state's fact reaches the shot through the firer's facts.
        Assert.Equal(-2, ScenarioA1OrdnanceEligibility.VehicleFirer(false, false, false, false, false, false, false, false, armorLeadership: -2).ArmorLeadership);
        Assert.Null(ScenarioA1OrdnanceEligibility.VehicleFirer(false, false, false, false, false, false, false, false).ArmorLeadership);
    }

    [Fact]
    public void AnArmorLeaderModifiesTheIftDrOfAMaThatIsAMgAndNoOtherMg()
    {
        // D3.44: "IFT otherwise": the SPW 251/1's MA is its AAMG. "An Armor Leader cannot modify non-OVR MG/FT (unless MA) attacks."
        foreach (var given in Leaderships)
        {
            var halftrack = new FireAttack("PFPh", "phasing", true, TankAt, At, [], null, 3, true, new FireLos(false, 0, true, false), null, "open-ground",
                [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false)], 3, new FireRolls([6, 5], null, null, null))
            {
                VehicleFire = new FireVehicleFire("de-ht", "attacker-halftrack", TankAt, true, false, false, false, false, false, false, false) { ArmorLeadership = given.Leadership },
            };
            var result = Shoot(halftrack);
            Assert.Equal(FireResolution.Resolved, result.Disposition);
            AssertLeadership(result.Arithmetic!.Drm, given);

            // A tank's BMG or CMG takes none, whatever the fact says.
            foreach (var weapon in new[] { FireVehicleWeapon.BowMg, FireVehicleWeapon.CoaxialMg })
            {
                var mg = Cmg(2, null, weapon: weapon);
                var fired = Shoot(mg with { VehicleFire = mg.VehicleFire! with { ArmorLeadership = given.Leadership } });
                Assert.Equal(FireResolution.Resolved, fired.Disposition);
                Assert.DoesNotContain(fired.Arithmetic!.Drm, item => ScenarioA1ArmorLeaders.IsModifier(item.Name));
            }
        }

        // The state's fact reaches a MA MG's attack and is dropped from a BMG's or CMG's.
        var firer = ScenarioA1FireEligibility.VehicleFirer(new VehicleFirerFacts("de-ht", "attacker-halftrack", TankAt, true, false, false, false, false, false, false, false, false, false,
            false, false, false, false)
        {
            ArmorLeadership = -1,
        });
        Assert.Equal(-1, firer.ArmorLeadership);
        Assert.Null(ScenarioA1FireEligibility.WithVehicleWeapons(firer, new VehicleWeaponsFacts([FireVehicleWeapon.CoaxialMg], false, false, "pfph", [], "t")).ArmorLeadership);
    }

    [Fact]
    public void AnArmorLeaderModifiesHisVehiclesOvrDr()
    {
        // D7.16: "Leadership modification of an OVR is limited to an Armor Leader"; the PzKpfw IIIH's OVR of a Russian squad in Open Ground.
        FireAttack Overrun(int? leadership) => new("MPh", "phasing", null, At, At, null, null, 0, true, new FireLos(false, 0, true, false), 7, "open-ground",
            [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false)], 3, new FireRolls([6, 5], null, null, null))
        {
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            Overrun = new FireOverrun("de-v", "attacker-tank", At, false, false, false, false, false) { ArmorLeadership = leadership },
        };
        var plain = Finish(Overrun(null));
        Assert.Equal(FireResolution.Resolved, plain.Disposition);
        foreach (var given in Leaderships)
        {
            var result = Finish(Overrun(given.Leadership));
            Assert.Equal(FireResolution.Resolved, result.Disposition);
            AssertLeadership(result.Arithmetic!.Drm, given);
            Assert.Equal(plain.Arithmetic!.FinalDr + (given.Leadership ?? 0), result.Arithmetic.FinalDr);
        }
    }

    [Fact]
    public void AnArmorLeaderModifiesHisVehiclesOwnCcAttack()
    {
        // D3.44, A11.62: "CC DR": the tank's CMG against a Russian squad; an attack made on the vehicle carries no such fact.
        var units = new[] { new CloseCombatUnit("ru-s", "defender-squad", "russian", false, false, false, false, false, false, false, false, false, false, false) { Inexperienced = false } };
        VehicleCloseCombatResolution ByTank(int? leadership) => ScenarioA1VehicleCloseCombat.Resolve(new VehicleCloseCombatFacts("CCPh", At,
            new VehicleCloseCombatVehicle("de-v", "attacker-tank", "german", false, false, false, false, false, false, false, false, false) { ArmorLeadership = leadership },
            units, [], ["ru-s"], true, false, new VehicleCloseCombatRolls([6, 5])), CloseCombat);
        var plain = Assert.Single(ByTank(null).Defending!);
        foreach (var given in Leaderships)
        {
            var defender = Assert.Single(ByTank(given.Leadership).Defending!);
            AssertLeadership(defender.Drm, given);
            Assert.Equal(plain.FinalDr + (given.Leadership ?? 0), defender.FinalDr);
        }

        Assert.Equal(-1, ScenarioA1ArmorLeaders.CloseCombatLeadership(-1, byVehicle: true));
        Assert.Null(ScenarioA1ArmorLeaders.CloseCombatLeadership(-1, byVehicle: false));
        Assert.Equal(1, ScenarioA1CloseCombatFactBuilders.Vehicle("de-v", "attacker-tank", "german", false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, armorLeadership: 1).ArmorLeadership);
    }

    [Fact]
    public void AnArmorLeaderModifiesTheColoredDrOfABogRemoval()
    {
        // D8.3: "One armor leader in either AFV also modifies the colored dr"; D8.31: +1 when Mired. The MP cost is the Original colored dr's.
        Assert.Equal((0, 1, -1, -2, 0, 2), (ScenarioA1ArmorLeaders.BogRemovalDrm(false, null), ScenarioA1ArmorLeaders.BogRemovalDrm(true, null),
            ScenarioA1ArmorLeaders.BogRemovalDrm(false, -1), ScenarioA1ArmorLeaders.BogRemovalDrm(false, -2), ScenarioA1ArmorLeaders.BogRemovalDrm(true, -1),
            ScenarioA1ArmorLeaders.BogRemovalDrm(true, 1)));

        // A colored 5 is Mired; with a 9-1 it is a Final 4 and frees the vehicle; an Inexperienced Crew's colored 4 is a Final 5.
        Assert.Equal(("mired", "freed", "mired"), (ScenarioA1ResultTables.VehicleCheck("bog-removal", ScenarioA1VehicleProjection.CheckFinal(true, 5, 3, 0)),
            ScenarioA1ResultTables.VehicleCheck("bog-removal", ScenarioA1VehicleProjection.CheckFinal(true, 5, 3, ScenarioA1ArmorLeaders.BogRemovalDrm(false, -1))),
            ScenarioA1ResultTables.VehicleCheck("bog-removal", ScenarioA1VehicleProjection.CheckFinal(true, 4, 3, ScenarioA1ArmorLeaders.BogRemovalDrm(false, 1)))));
        Assert.Equal(30, ScenarioA1VehicleMovementCalculator.BogRemovalHalfMp(5, 3, false));

        // The record names the modifier its vehicle has, on a Bog Removal alone.
        Assert.Null(ScenarioA1ArmorLeaders.CheckRefusal(true, -1, -1));
        Assert.Null(ScenarioA1ArmorLeaders.CheckRefusal(true, null, null));
        Assert.Null(ScenarioA1ArmorLeaders.CheckRefusal(false, null, -1));
        Assert.NotNull(ScenarioA1ArmorLeaders.CheckRefusal(true, null, -1));
        Assert.NotNull(ScenarioA1ArmorLeaders.CheckRefusal(true, -2, -1));
        Assert.NotNull(ScenarioA1ArmorLeaders.CheckRefusal(false, -1, -1));
    }

    [Fact]
    public void TheCrewChecksAtItsArmorLeadersMoraleLevel()
    {
        // D3.42: "The inherent crew of a vehicle always has the same Morale Level as any Armor Leader in that vehicle." C7.41: a TK DR one above
        // the Final TK# of 9 asks the German crew's NTC, at 8 by its nationality (D5.1), at 10 with a 10-2, at 6 when Inexperienced (D3.45).
        var national = Resolve(TankShot(toKill: [5, 5], shock: [5, 4])).Kill!;
        Assert.Equal((8, false, (bool?)true, (string?)null), (national.ShockCheck!.MoraleLevel, national.ShockCheck.Passed, national.Shocked, national.ShockCheck.MoraleSource));
        var led = Resolve(TankShot(crewMorale: 10, toKill: [5, 5], shock: [5, 4])).Kill!;
        Assert.Equal((10, true, (bool?)null, "armor-leader"), (led.ShockCheck!.MoraleLevel, led.ShockCheck.Passed, led.Shocked, led.ShockCheck.MoraleSource));

        // His leadership modifier is not added to the check: he takes it himself.
        Assert.Equal((9, 9), (led.ShockCheck.OriginalDr, led.ShockCheck.FinalDr));
        Assert.Empty(led.ShockCheck.Drm);
        var green = Resolve(TankShot(crewMorale: 6, toKill: [5, 5], shock: [4, 3])).Kill!;
        Assert.Equal((6, false, "inexperienced-crew"), (green.ShockCheck!.MoraleLevel, green.ShockCheck.Passed, green.ShockCheck.MoraleSource));

        // D5.5: the Immobilization TC of a hull hit equal to the Final TK# of 7.
        var kept = Resolve(TankShot(crewMorale: 9, toHit: [4, 2], toKill: [3, 4], crew: [5, 4])).Kill!;
        Assert.Equal((OrdnanceKill.Immobilized, 9, true, "armor-leader"), (kept.Result, kept.CrewCheck!.MoraleLevel, kept.CrewCheck.Passed, kept.CrewCheck.MoraleSource));
        var left = Resolve(TankShot(toHit: [4, 2], toKill: [3, 4], crew: [5, 4])).Kill!;
        Assert.Equal((8, false, (bool?)true), (left.CrewCheck!.MoraleLevel, left.CrewCheck.Passed, left.Abandoned));

        // D5.34: the CE crew's MC under IFT fire. Two Russian squads fire at a CE halftrack; the first DR that asks the crew a check is taken.
        FireResolution Fired(int[] dice, int? morale) => ScenarioA1FireCalculator.Resolve(new FireAttack("PFPh", "phasing", true, TankAt, At,
            [new FireFirer("ru-1", "defender-squad", TankAt, false, false, false, false, false), new FireFirer("ru-2", "defender-squad", TankAt, false, false, false, false, false)],
            null, 1, true, new FireLos(false, 0, true, false), null, "open-ground", [], 3,
            new FireRolls(dice, null, null, null) { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-ht"] = [5, 4] } })
        {
            Vehicles = [new FireVehicle("de-ht", "attacker-halftrack", At, true, false, false, false) { CrewMorale = morale }],
        }, Fire);
        var asking = Enumerable.Range(1, 6).SelectMany(one => Enumerable.Range(1, 6).Select(two => new[] { one, two }))
            .First(dice => Fired(dice, null) is { Disposition: FireResolution.Resolved, VehicleEffects: [{ CrewCheck: not null }] });
        var plain = Assert.Single(Fired(asking, null).VehicleEffects!).CrewCheck!;
        Assert.Equal((8, (string?)null), (plain.MoraleLevel, plain.MoraleSource));
        var withLeader = Assert.Single(Fired(asking, 10).VehicleEffects!).CrewCheck!;
        Assert.Equal((10, "armor-leader", plain.FinalDr), (withLeader.MoraleLevel, withLeader.MoraleSource, withLeader.FinalDr));
        Assert.Equal(withLeader.FinalDr <= 10, withLeader.Passed);
        var inexperienced = Assert.Single(Fired(asking, 6).VehicleEffects!).CrewCheck!;
        Assert.Equal((6, "inexperienced-crew", false), (inexperienced.MoraleLevel, inexperienced.MoraleSource, inexperienced.Passed));
    }

    [Fact]
    public void ARecordKeepsACrewsMoraleOnlyWhenTheCrewChecked()
    {
        // D3.41: the Armor Leader "remains out of play until the owner needs to use his morale/leadership factor": a shot that asks no check
        // of the target's crew records no morale, and one that asks it records the morale it was made at.
        var miss = TankShot(crewMorale: 10, toKill: [6, 6]);
        Assert.Null(ScenarioA1ArmorLeaders.Recorded(miss, Resolve(miss)).VehicleTarget!.CrewMorale);
        var asked = TankShot(crewMorale: 10, toKill: [5, 5], shock: [5, 4]);
        Assert.Equal(10, ScenarioA1ArmorLeaders.Recorded(asked, Resolve(asked)).VehicleTarget!.CrewMorale);
        var plain = TankShot(toKill: [6, 6]);
        Assert.Same(plain, ScenarioA1ArmorLeaders.Recorded(plain, Resolve(plain)));

        // The record's check: a morale it leaves out is not compared; a check made without the state's morale is refused.
        Assert.Null(ScenarioA1ArmorLeaders.AsRecorded(miss.VehicleTarget, plain.VehicleTarget)!.CrewMorale);
        Assert.Equal(10, ScenarioA1ArmorLeaders.AsRecorded(miss.VehicleTarget, asked.VehicleTarget)!.CrewMorale);
        Assert.Null(ScenarioA1ArmorLeaders.RecordRefusal(miss, plain, Resolve(plain)));
        var dodged = TankShot(toKill: [5, 5], shock: [5, 4]);
        Assert.Contains("D3.42", ScenarioA1ArmorLeaders.RecordRefusal(asked, dodged, Resolve(dodged)), StringComparison.Ordinal);
        Assert.Null(ScenarioA1ArmorLeaders.RecordRefusal(asked, asked, Resolve(asked)));
        Assert.Null(ScenarioA1ArmorLeaders.RecordRefusal(dodged, dodged, Resolve(dodged)));

        // The vehicles of an IFT attack, by the crews that checked.
        FireVehicle[] vehicles = [new FireVehicle("a", "attacker-halftrack", At, true, false, false, false) { CrewMorale = 9 }, new FireVehicle("b", "attacker-halftrack", At, true, false, false, false)];
        Assert.Equal(new int?[] { null, null }, ScenarioA1ArmorLeaders.Recorded(vehicles, [])!.Select(item => item.CrewMorale));
        Assert.Equal(new int?[] { 9, null }, ScenarioA1ArmorLeaders.Recorded(vehicles, ["a"])!.Select(item => item.CrewMorale));
        Assert.Contains("D3.42", ScenarioA1ArmorLeaders.RecordRefusal(vehicles, ScenarioA1ArmorLeaders.Recorded(vehicles, []), ["a"]), StringComparison.Ordinal);
        Assert.Null(ScenarioA1ArmorLeaders.RecordRefusal(vehicles, ScenarioA1ArmorLeaders.Recorded(vehicles, []), ["b"]));
        Assert.Null(ScenarioA1ArmorLeaders.RecordRefusal(vehicles, vehicles, ["a", "b"]));
    }

    [Fact]
    public void AnArmorLeaderIsLostAndShownByRule()
    {
        // D5.341: "Recall eliminates any Armor Leader present in that AFV"; D3.43: he "ceases to exist once the crew takes counter form".
        Assert.False(ScenarioA1ArmorLeaders.Lost(active: true, recalled: false, abandoned: false));
        Assert.True(ScenarioA1ArmorLeaders.Lost(active: true, recalled: true, abandoned: false));
        Assert.True(ScenarioA1ArmorLeaders.Lost(active: true, recalled: false, abandoned: true));
        Assert.True(ScenarioA1ArmorLeaders.Lost(active: false, recalled: false, abandoned: false));

        // D3.41: his owner always reads him; the enemy once a record has used him.
        Assert.True(ScenarioA1ArmorLeaders.Shown(enemy: false, revealed: false));
        Assert.False(ScenarioA1ArmorLeaders.Shown(enemy: true, revealed: false));
        Assert.True(ScenarioA1ArmorLeaders.Shown(enemy: true, revealed: true));
    }

    // Task 55.3 (increment 2f): Motion attempts (D2.401, p. 198; rulings R55.11 to R55.13) and the DEFENDER's Motion firer (C5.35; ruling R55.14).

    private static readonly MotionAttemptVehicle Mobile = new("r1", true, true);

    private static string? MotionBar(MotionAttemptVehicle? vehicle, string phase = "mph", bool windowOpen = true) => ScenarioA1MotionAttempts.Bar("r1", phase, windowOpen, vehicle);

    [Fact]
    public void AMobileDefendingVehicleAttemptsMotionOnceInTheEnemyMphsWindow()
    {
        // D2.401: "declared during the MPh of an enemy ground unit by any DEFENDING Mobile vehicle".
        Assert.Null(MotionBar(Mobile));
        Assert.Contains("enemy MPh", MotionBar(Mobile, phase: "dfph"), StringComparison.Ordinal);
        Assert.Contains("enemy MPh", MotionBar(Mobile, windowOpen: false), StringComparison.Ordinal);
        Assert.Contains("not a vehicle of the DEFENDER", MotionBar(null), StringComparison.Ordinal);
        Assert.Contains("not a vehicle of the DEFENDER", MotionBar(new MotionAttemptVehicle("r1", true, false)), StringComparison.Ordinal);
        Assert.Contains("not a vehicle of the DEFENDER", MotionBar(new MotionAttemptVehicle("r1", false, true)), StringComparison.Ordinal);

        // Mobile: not immobilized, bogged, or Mired (D8.2: a bogged vehicle "is Immobile").
        Assert.Contains("immobilized", MotionBar(Mobile with { Immobilized = true }), StringComparison.Ordinal);
        Assert.Contains("bogged", MotionBar(Mobile with { Bogged = true }), StringComparison.Ordinal);
        Assert.Contains("bogged", MotionBar(Mobile with { Mired = true }), StringComparison.Ordinal);

        // D5.34: a stunned crew may not "move (inclusive of CA changes)"; the "+1" counter says it "is no longer Stunned". D5.341: a Recall
        // "is treated the same as a Stun" until the Player Turn ends, and then the AFV leaves "using Motion status". C7.42, D5.41.
        Assert.Contains("D5.34", MotionBar(Mobile with { Stunned = true }), StringComparison.Ordinal);
        Assert.Null(MotionBar(Mobile with { StunRecovery = true }));
        Assert.Contains("D5.341", MotionBar(Mobile with { Recalled = true }), StringComparison.Ordinal);
        Assert.Null(MotionBar(Mobile with { Recalled = true, StunRecovery = true }));
        Assert.Contains("C7.42", MotionBar(Mobile with { Shocked = true }), StringComparison.Ordinal);
        Assert.Contains("C7.42", MotionBar(Mobile with { UnconfirmedKill = true }), StringComparison.Ordinal);
        Assert.Contains("D5.41", MotionBar(Mobile with { Abandoned = true }), StringComparison.Ordinal);

        // "may not attempt it at all if already marked with a First Fire (or Final/Intensive Fire) counter"; "only once per enemy MPh".
        Assert.All(new[] { "First Fire", "Final Fire", "Intensive Fire" }, counter => Assert.Contains($"a {counter} counter", MotionBar(Mobile with { FireCounter = counter }), StringComparison.Ordinal));
        Assert.Contains("only once", MotionBar(Mobile with { Attempted = true }), StringComparison.Ordinal);
    }

    [Fact]
    public void TheNumberIsTheSumOfWhatTheMoverSpentInLos()
    {
        // Ruling R55.11: every expenditure made in a Location in the vehicle's LOS counts, with a gap out of LOS between them: 1 + 2 MP, not
        // the 2 MP of the unbroken LOS that Cases C1 and J1 count.
        (bool, int)[] steps = [(false, 2), (true, 2), (false, 4), (true, 4)];
        Assert.Equal(6, ScenarioA1MotionAttempts.HalfPointsInLos(steps));
        Assert.Equal(2, ScenarioA1OrdnanceMapRules.MpInLos([.. steps.Select(step => (step.Item2, (Func<bool>)(() => step.Item1)))], () => true));
        Assert.Equal(0, ScenarioA1MotionAttempts.HalfPointsInLos([(false, 2), (false, 6)]));

        // A dr is a whole number: 1½ MF or MP is passed by a dr of 1 alone; a die shows no more than 6.
        Assert.Equal([0, 0, 1, 1, 2, 6, 6], new[] { 0, 1, 2, 3, 4, 12, 40 }.Select(ScenarioA1MotionAttempts.Number));
        Assert.True(ScenarioA1MotionAttempts.Succeeds(1, 3));
        Assert.False(ScenarioA1MotionAttempts.Succeeds(2, 3));

        // "a Motion attempt dr <= the number of MF/MP expended in its LOS": at, below, and above 3 MP.
        Assert.True(ScenarioA1MotionAttempts.Succeeds(3, 6));
        Assert.True(ScenarioA1MotionAttempts.Succeeds(2, 6));
        Assert.False(ScenarioA1MotionAttempts.Succeeds(4, 6));
        Assert.Equal("3 MP in its LOS: succeeds on a dr of 3 or less", ScenarioA1MotionAttempts.Chance(6, true));
        Assert.Equal("1½ MF in its LOS: succeeds on a dr of 1 or less", ScenarioA1MotionAttempts.Chance(3, false));
    }

    [Fact]
    public void TheMoverIsOneThatBeganItsMoveOutOfTheVehiclesLos()
    {
        // Ruling R55.12: the LOS to the Location the unit began its move in decides "had not been in the vehicle's LOS during that Player Turn";
        // a unit seen there is refused before any roll, and costs no attempt.
        var unseen = new MotionAttemptMover("g1", false, [(false, 2), (true, 4)]);
        var seen = new MotionAttemptMover("g2", true, [(true, 2), (true, 8)]);
        var (mover, halfPoints, refusal) = ScenarioA1MotionAttempts.Mover("r1", true, [seen, unseen]);
        Assert.Equal(("g1", 4, null), (mover!.Id, halfPoints, refusal));

        var refused = ScenarioA1MotionAttempts.Mover("r1", true, [seen]);
        Assert.Null(refused.Mover);
        Assert.Contains("play.motion-attempt-seen", refused.Refusal, StringComparison.Ordinal);
        Assert.Contains("no attempt is spent", refused.Refusal, StringComparison.Ordinal);

        // Less than 1 MF or MP in LOS: no dr could pass, and none is rolled.
        var early = ScenarioA1MotionAttempts.Mover("r1", false, [new MotionAttemptMover("g1", false, [(false, 2), (true, 1)])]);
        Assert.Null(early.Mover);
        Assert.Contains("½ MF", early.Refusal, StringComparison.Ordinal);
        Assert.Contains("no enemy unit is moving", ScenarioA1MotionAttempts.Mover("r1", false, []).Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void APassedAttemptFreelyChangesTheVcaAndTheTcaUnlessADrUndoesIt()
    {
        // "allowed to freely change its VCA/TCA": any hexspine; D8.2: "one Bog DR per hexspine", counted the shorter way round.
        Assert.Equal([0, 1, 2, 3, 2, 1], Enumerable.Range(0, 6).Select(next => ScenarioA1MotionAttempts.Hexspines(0, next)));
        Assert.Equal(2, ScenarioA1MotionAttempts.Hexspines(5, 1));
        Assert.Null(ScenarioA1MotionAttempts.ChangeBar("r1", true, true, true, true, turreted: true, bypass: false));
        Assert.Null(ScenarioA1MotionAttempts.ChangeBar("r1", false, false, false, false, turreted: false, bypass: true));
        Assert.Contains("no turret", ScenarioA1MotionAttempts.ChangeBar("r1", false, false, true, true, turreted: false, bypass: false), StringComparison.Ordinal);
        Assert.Contains("name the hexspine", ScenarioA1MotionAttempts.ChangeBar("r1", true, false, false, false, turreted: true, bypass: false), StringComparison.Ordinal);
        Assert.Contains("Bypass", ScenarioA1MotionAttempts.ChangeBar("r1", true, true, false, false, turreted: true, bypass: true), StringComparison.Ordinal);

        // D2.401, D2.51: "neither Motion nor VCA change if any mechanical reliability DR results in Immobilization"; ruling R55.13 for a bog.
        Assert.Equal(ScenarioA1MotionAttempts.Failed, ScenarioA1MotionAttempts.Result(false, false, false));
        Assert.Equal(ScenarioA1MotionAttempts.Passed, ScenarioA1MotionAttempts.Result(true, false, false));
        Assert.Equal(ScenarioA1MotionAttempts.Immobilized, ScenarioA1MotionAttempts.Result(true, true, false));
        Assert.Equal(ScenarioA1MotionAttempts.Bogged, ScenarioA1MotionAttempts.Result(true, false, true));

        var summary = ScenarioA1MotionAttempts.Summary("r1", "g1", 6, true, "north-east", null, unreliable: true, bogDrm: 1, hexspines: 2);
        Assert.Contains("3 MP spent in its LOS: it gains Motion on a dr of 3 or less, and turns its VCA to north-east (D2.401)", summary, StringComparison.Ordinal);
        Assert.Contains("D2.51", summary, StringComparison.Ordinal);
        Assert.Contains("a Bog DR at +1 for each of its 2 hexspines", summary, StringComparison.Ordinal);
        Assert.Equal("it gains Motion and turns its VCA to west and its TCA to north-west", ScenarioA1MotionAttempts.ResultText(ScenarioA1MotionAttempts.Passed, "west", "north-west"));
    }

    [Fact]
    public void ARecordedMotionAttemptIsHeldToItsDrAndItsChecks()
    {
        static RecordRefusal? Record(string result, int? dr = 2, int halfPoints = 6, bool attempted = false, bool immobilized = false, bool bogged = false, bool changesCa = false,
            string phase = "mph", bool windowOpen = true, bool defending = true) =>
            ScenarioA1MotionAttempts.RecordRefusal(phase, windowOpen, defending, attempted, dr, halfPoints, result, immobilized, bogged, changesCa);

        Assert.Null(Record(ScenarioA1MotionAttempts.Passed, changesCa: true));
        Assert.Null(Record(ScenarioA1MotionAttempts.Failed, dr: 4));
        Assert.Null(Record(ScenarioA1MotionAttempts.Immobilized, immobilized: true));
        Assert.Null(Record(ScenarioA1MotionAttempts.Bogged, bogged: true));
        Assert.All(new[]
        {
            Record(ScenarioA1MotionAttempts.Passed, dr: 4), Record(ScenarioA1MotionAttempts.Failed), Record(ScenarioA1MotionAttempts.Passed, immobilized: true),
            Record(ScenarioA1MotionAttempts.Immobilized), Record(ScenarioA1MotionAttempts.Failed, dr: 4, changesCa: true), Record(ScenarioA1MotionAttempts.Passed, dr: null),
            Record(ScenarioA1MotionAttempts.Passed, attempted: true), Record(ScenarioA1MotionAttempts.Passed, phase: "dfph"), Record(ScenarioA1MotionAttempts.Passed, windowOpen: false),
            Record(ScenarioA1MotionAttempts.Passed, defending: false),
        }, refusal => Assert.Equal("UNIT-STATE-048", refusal!.Code));

        // The checks a DEFENDER's vehicle makes in the enemy MPh: the Mechanical Reliability DR and a Bog Check, before its attempt is marked.
        Assert.True(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.MechanicalCheck, true, false));
        Assert.True(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.BogCheck, true, false));
        Assert.False(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.EsbCheck, true, false));
        Assert.False(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.BogRemovalCheck, true, false));
        Assert.False(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.BogCheck, false, false));
        Assert.False(ScenarioA1MotionAttempts.CheckInEnemyMph(ScenarioA1ResultTables.BogCheck, true, true));
    }

    /// <summary>The T-34 M41 (a RST turret) of the side that is not phasing, in Motion, firing AP at the PzKpfw IIIH at 3 hexes.</summary>
    private static OrdnanceShot DefenderInMotion(string phase, int[] toHit, bool inMotion = true, int acquisition = 0, int range = 3)
    {
        var shot = TankShot(toHit: toHit, toKill: [6, 6]);
        return shot with
        {
            Phase = phase,
            FiringSide = "non-phasing",
            Range = range,
            Acquisition = acquisition,
            Hit = shot.Hit! with { Phase = phase, FiringSide = "non-phasing" },
            Vehicle = new OrdnanceVehicleFirer(true, inMotion, false, false, false),
            FireKind = phase == "MPh" ? "first-fire" : null,
            Movement = phase == "MPh" ? new OrdnanceMovement(5, null, null, 1, 0) : null,
        };
    }

    [Fact]
    public void ADefendersVehicleInMotionFiresItsMaWithCaseCAndTheLowerDrDoubled()
    {
        // D2.401: "may still use Motion Fire (Case C4)"; C5.35: "must use the Case C, or C1, or C2 DRM as applicable and must double the lower
        // dr of its TH DR". Ruling R55.14: Case C alone, +4 for a turret (the +2 of Case B and the mount's 2), in the enemy MPh and in the DFPh.
        foreach (var phase in new[] { "MPh", "DFPh" })
        {
            var result = Resolve(DefenderInMotion(phase, [2, 4]));
            Assert.DoesNotContain("asl.a1.ordnance.vehicle-fire-outside", result.Reasons);
            Assert.Equal(4m, Drm(result, "case-c"));
            Assert.Equal(2m, Drm(result, "case-c4:lower-dr"));
            Assert.Null(Drm(result, "case-c1"));
            Assert.Null(Drm(result, "case-c2"));
            Assert.Null(Drm(result, "case-b"));
            Assert.Equal(6 + (int)result.ToHit!.Drm.Sum(item => item.Value), result.ToHit.FinalDr);
            Assert.Equal(phase == "MPh" ? "first-fire" : "final-fire", result.Gun!.FireCounter);

            // C6.55 (ruling R50.13): no Acquisition DRM, and none carried on; C6.3: no Case L for a firer in Motion.
            var acquired = Resolve(DefenderInMotion(phase, [2, 4], acquisition: -1, range: 1));
            Assert.Null(Drm(acquired, "case-n"));
            Assert.Null(Drm(acquired, "case-l"));
            Assert.Equal(-1, acquired.Gun!.Acquisition);

            // The same vehicle not in Motion fires as it did: no Case C, its Acquisition, and Case L.
            var still = Resolve(DefenderInMotion(phase, [2, 4], inMotion: false, acquisition: -1, range: 1));
            Assert.Null(Drm(still, "case-c"));
            Assert.Null(Drm(still, "case-c4:lower-dr"));
            Assert.Equal(-1m, Drm(still, "case-n"));
            Assert.Equal(-2m, Drm(still, "case-l"));
        }

        // D2.4: a vehicle in Motion still fires nothing in its own PFPh.
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Resolve(TankShot() with { Vehicle = new OrdnanceVehicleFirer(true, true, false, false, false) }).Reasons);
    }

    [Fact]
    public void ADefendersVehicleInMotionFiresItsMgsHalved()
    {
        // D2.42: "All other types of FP from such a vehicle ... are halved as Motion Fire". The PzKpfw IIIH's CMG (5 FP) in the DFPh.
        var attack = Cmg(2, null) with { Phase = "DFPh", FiringSide = "non-phasing" };
        var moving = attack with { VehicleFire = attack.VehicleFire! with { InMotion = true } };
        Assert.Equal(5m, Shoot(attack).Arithmetic!.TotalFirepower);
        var halved = Shoot(moving);
        Assert.Equal(FireResolution.Resolved, halved.Disposition);
        Assert.Equal(2.5m, halved.Arithmetic!.TotalFirepower);
        Assert.Contains(halved.Arithmetic.Firers.SelectMany(item => item.Multipliers), item => item.Name == "motion-fire" && item.Rule == "D2.42");
    }

    // Task 55.7 (increment 2g): Gun Duels (C2.2401, pp. 167 and 168; rulings R55.19, R55.20).

    /// <summary>
    /// Every To Hit DRM name the Ordnance calculators give, with its class on the C5 and C6 charts (p. 189; C5.9's closing note, p. 173). A name
    /// with a colon stands for every name of that head.
    /// </summary>
    private static readonly (string Name, ToHitDrmClass Class)[] ToHitDrmNames =
    [
        ("case-a:", ToHitDrmClass.FirerBased), ("case-a:maintained", ToHitDrmClass.FirerBased), ("case-b", ToHitDrmClass.FirerBased), ("case-c", ToHitDrmClass.FirerBased),
        ("case-c1", ToHitDrmClass.FirerBased), ("case-c2", ToHitDrmClass.FirerBased), ("case-c3:afph", ToHitDrmClass.FirerBased),
        ("case-c3:backblast", ToHitDrmClass.FirerBased), ("case-c4:lower-dr", ToHitDrmClass.LowerDrDoubled), ("case-d", ToHitDrmClass.FirerBased),
        ("case-e", ToHitDrmClass.FirerBased), ("case-f", ToHitDrmClass.FirerBased), ("case-h", ToHitDrmClass.FirerBased), ("case-i", ToHitDrmClass.FirerBased),
        ("cx", ToHitDrmClass.FirerBased), ("stun", ToHitDrmClass.FirerBased), ("spotted", ToHitDrmClass.FirerBased), ("overstack-firer", ToHitDrmClass.FirerBased),
        ("leadership:", ToHitDrmClass.FirerBased), (ScenarioA1ArmorLeaders.LeaderModifier, ToHitDrmClass.FirerBased),
        (ScenarioA1ArmorLeaders.InexperiencedModifier, ToHitDrmClass.FirerBased), ("case-n", ToHitDrmClass.Acquisition), ("case-j", ToHitDrmClass.TargetBased),
        ("case-j1", ToHitDrmClass.TargetBased), ("case-j2", ToHitDrmClass.TargetBased), ("case-j3", ToHitDrmClass.TargetBased), ("case-j4", ToHitDrmClass.TargetBased),
        ("case-k", ToHitDrmClass.TargetBased), ("case-l", ToHitDrmClass.TargetBased), ("case-m", ToHitDrmClass.TargetBased), ("case-o", ToHitDrmClass.TargetBased),
        ("case-p:", ToHitDrmClass.TargetBased), ("case-q:", ToHitDrmClass.TargetBased), ("case-q:emplacement", ToHitDrmClass.TargetBased),
        ("case-q:weather-cushion", ToHitDrmClass.TargetBased), ("case-q:afv-cover:", ToHitDrmClass.TargetBased), ("case-q:height-advantage", ToHitDrmClass.TargetBased),
        ("case-r", ToHitDrmClass.TargetBased), ("case-r:lv", ToHitDrmClass.TargetBased), ("overstack-target", ToHitDrmClass.TargetBased),
    ];

    /// <summary>
    /// The names the same calculators give to what is no To Hit DRM: the PF Check's drm (C13.31), a PF's range on its TH# (C13.33), and the To
    /// Kill number's Case A for a rear hit (C7.21); its Cases C and D share their names with To Hit cases.
    /// </summary>
    private static readonly string[] NotToHitDrm = ["1945", "hs-or-crew", "smc", "pf-range", "case-a"];

    /// <summary>The names a calculator's source gives its modifiers as literals: the text after <c>new FireModifier("</c>.</summary>
    private static IEnumerable<string> ModifierNames(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LimboDancer.Domains.Asl.Rules", file)))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"The Rules source '{file}' was not found above {AppContext.BaseDirectory}.");
        const string Opening = "new FireModifier(\"";
        var text = File.ReadAllText(Path.Combine(directory!.FullName, "LimboDancer.Domains.Asl.Rules", file));
        for (var at = text.IndexOf(Opening, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Opening, at + 1, StringComparison.Ordinal))
        {
            var start = at + Opening.Length;
            yield return text[start..text.IndexOf('"', start)];
        }
    }

    [Fact]
    public void EveryToHitDrmNameIsFirerBasedAcquisitionOrTargetBased()
    {
        // C2.2401: "its total Firer-Based [5.] and Acquisition [6.5] TH DRM"; the doubled lower dr of Case C4 is left out by name.
        foreach (var (name, expected) in ToHitDrmNames)
        {
            Assert.True(ScenarioA1GunDuels.Classify(name) == expected, name);
            Assert.True(ScenarioA1GunDuels.Classify(name.EndsWith(':') ? name + "anything" : name) == expected, name);
        }

        Assert.Null(ScenarioA1GunDuels.Classify("a-new-drm"));

        // A name a calculator gains later fails here until it is classified above (or named as no To Hit DRM).
        var listed = ToHitDrmNames.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        var given = new[] { "ScenarioA1OrdnanceCalculator.cs", "ScenarioA1ArmorCalculator.cs", "ScenarioA1AreaCalculator.cs" }.SelectMany(ModifierNames)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Assert.True(given.Length >= 30, string.Join(", ", given));
        Assert.Empty(given.Where(name => !listed.Contains(name) && !NotToHitDrm.Contains(name)));
        Assert.All(given.Except(NotToHitDrm), name => Assert.NotNull(ScenarioA1GunDuels.Classify(name)));
    }

    /// <summary>The Russian 45L AT Gun's Defensive First Fire shot, AP at the moving PzKpfw IIIH at 4 hexes: the declared shot.</summary>
    private static OrdnanceShot DeclaredShot(int turn = 1, bool woods = true, bool concealed = false, int acquisition = 0) =>
        new OrdnanceShot("MPh", "non-phasing", "russian", new OrdnanceGun("ru-gun", "defender-at-gun", false, 0, false, false),
            new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, concealed, false), At, 4, turn, woods, true, acquisition,
            new FireAttack("MPh", "non-phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 3, null),
            new OrdnanceRolls(null, null, null, null, null))
        {
            VehicleTarget = new OrdnanceVehicleTarget("de-tank", "attacker-tank", "front", "front", false, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
            FireKind = "first-fire",
            Movement = new OrdnanceMovement(5, null, null, 1, 0),
        };

    /// <summary>The PzKpfw IIIH's potential answer: its MA's Bounding First Fire, HE at the Gun's crew in woods at 4 hexes.</summary>
    private static OrdnanceShot AnswerShot(int mpInLos = 99, int turn = 0, bool buttonedUp = true, bool inMotion = true, int? leadership = null, string? ammunition = null) =>
        new OrdnanceShot("MPh", "phasing", "german", new OrdnanceGun("de-tank", "attacker-tank", false, 0, false, false),
            new OrdnanceCrew("de-tank", "attacker-tank", false, false, false, false, false), "bd01:H3:0", 4, turn, false, true, 0,
            new FireAttack("MPh", "phasing", null, null, "bd01:H3:0", [], null, null, true, new FireLos(false, 0, true, false), 7, "woods",
                [new FireTarget("ru-crew", "defender-crew", "bd01:H3:0", false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls(null, null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(buttonedUp, inMotion, false, false, false) { MpInLos = mpInLos, ArmorLeadership = leadership },
            FireKind = ScenarioA1FireCalculator.BoundingFirstFire,
            Ammunition = ammunition,
            GunDuel = true,
        };

    private static readonly GunDuelFacts Duelable = new(true, false, false, false, false);

    private static GunDuelVerdict Duel(GunDuelFacts? facts = null, OrdnanceShot? declared = null, OrdnanceShot? answer = null, bool noAnswer = false) =>
        ScenarioA1GunDuels.Verdict(facts ?? Duelable, declared ?? DeclaredShot(), () => noAnswer ? null : answer ?? AnswerShot(), Ordnance);

    [Fact]
    public void TheGunDuelDrmIsTheFirerBasedAndAcquisitionDrmOfTheShotItself()
    {
        // The declared shot is accepted and resolves with Case A +6 and Case J +2: the firer's line alone is its Gun Duel DRM.
        var declared = DeclaredShot();
        Assert.Equal(["asl.a1.ordnance.roll-missing:toHit"], Resolve(declared).Reasons);
        var rolled = Resolve(declared with { Rolls = new OrdnanceRolls([6, 6], null, null, null, null) }).ToHit!;
        Assert.Equal(["case-a:1", "case-j"], rolled.Drm.Select(item => item.Name));
        var gun = ScenarioA1GunDuels.Drm(declared, Ordnance)!;
        Assert.Equal((6, "case-a:1"), (gun.Total, Assert.Single(gun.Lines).Name));

        // C6.5: the Acquisition counts; nothing of the target does.
        var acquired = ScenarioA1GunDuels.Drm(DeclaredShot(turn: 0, acquisition: -2), Ordnance)!;
        Assert.Equal((-2, "case-n"), (acquired.Total, Assert.Single(acquired.Lines).Name));
        Assert.Equal(0, ScenarioA1GunDuels.Drm(DeclaredShot(turn: 0), Ordnance)!.Total);

        // C2.2401: "nor the doubling of the lower dr ... in TH Case C4 (5.35) is included": the Motion firer's shot resolves with it, and its
        // Gun Duel DRM is Case C and Case I alone.
        var answer = AnswerShot();
        Assert.Equal(["asl.a1.ordnance.roll-missing:toHit"], Resolve(answer).Reasons);
        Assert.Contains(Resolve(answer with { Rolls = new OrdnanceRolls([6, 6], null, null, null, null) }).ToHit!.Drm, item => item.Name == "case-c4:lower-dr" && item.Value == 6);
        var tank = ScenarioA1GunDuels.Drm(answer, Ordnance)!;
        Assert.Equal(5, tank.Total);
        Assert.Equal(["case-c", "case-i"], tank.Lines.Select(item => item.Name));
        Assert.Equal(tank.Total, ScenarioA1GunDuels.Drm(AnswerShot(inMotion: false), Ordnance)!.Total);

        // D3.44: an Armor Leader's modifier is the firer's; C5.31, C5.32: so are Case C1 and Case C2.
        Assert.Equal(4, ScenarioA1GunDuels.Drm(AnswerShot(leadership: -1), Ordnance)!.Total);
        Assert.Equal(6, ScenarioA1GunDuels.Drm(AnswerShot(leadership: 1), Ordnance)!.Total);
        Assert.Equal(6, ScenarioA1GunDuels.Drm(AnswerShot(mpInLos: 3), Ordnance)!.Total);
        Assert.Equal(7, ScenarioA1GunDuels.Drm(AnswerShot(mpInLos: 1), Ordnance)!.Total);
        Assert.Equal("+5 (Case C +4, Case I +1)", ScenarioA1GunDuels.DrmText(tank));
        Assert.Equal("+4 (Case C +4, Case I +1, Armor Leader -1)", ScenarioA1GunDuels.DrmText(ScenarioA1GunDuels.Drm(AnswerShot(leadership: -1), Ordnance)!));
        Assert.Equal("+0", ScenarioA1GunDuels.DrmText(new GunDuelDrm(0, [])));
    }

    [Fact]
    public void TheRulesOwnGunDuelIsEqualAndIsNotOfferedInTheSlice()
    {
        // The C2.2401 EX (p. 168): the CE PSW 234/2 has spent 1/2 MP in the AT Gun's LOS and "must use TH Case C4 (+6) for a total Firer-Based
        // TH DRM of +6" (Case C2 for its turret, the doubled lower dr not counted); the AT Gun "must use Case A for a +6 Firer-Based TH DRM
        // (+3 [NT Gun] x 2 [firer in woods] = +6)". Equal: ruling R55.20 leaves that case out, so no duel is offered and both totals are told.
        var equal = Duel(answer: AnswerShot(mpInLos: 0, buttonedUp: false));
        Assert.Equal((ScenarioA1GunDuels.NotLower, 6, 6, false), (equal.Bar, equal.Attacker!.Total, equal.Defender!.Total, equal.Offered));
        Assert.Equal("case-c2", Assert.Single(equal.Attacker.Lines).Name);

        // A Gun that turns two hexspines in its woods pays (3 + 1) x 2 = +8: the PSW's +6 is lower, and it may fire first.
        var offered = Duel(declared: DeclaredShot(turn: 2), answer: AnswerShot(mpInLos: 0, buttonedUp: false));
        Assert.Equal((null, 6, 8, true), (offered.Bar, offered.Attacker!.Total, offered.Defender!.Total, offered.Offered));

        // A vehicle with the higher total is offered none.
        Assert.Equal(ScenarioA1GunDuels.NotLower, Duel(answer: AnswerShot(mpInLos: 1)).Bar);
    }

    [Fact]
    public void AGunDuelIsBarredByEachConditionOfTheRule()
    {
        var offered = Duel();
        Assert.Equal((null, 5, 6), (offered.Bar, offered.Attacker!.Total, offered.Defender!.Total));

        // "Vs a non-concealed ... DEFENDER's declared Defensive First Fire attack on it": the Gun, its crew, or the shot's kind and target.
        Assert.Equal(ScenarioA1GunDuels.Concealed, Duel(Duelable with { FirerConcealed = true }).Bar);
        Assert.Equal(ScenarioA1GunDuels.Concealed, Duel(declared: DeclaredShot(concealed: true)).Bar);
        Assert.Equal(ScenarioA1GunDuels.NotDefensiveFirstFire, Duel(Duelable with { AtTheMovingVehicle = false }).Bar);
        Assert.Equal(ScenarioA1GunDuels.NotDefensiveFirstFire, Duel(declared: DeclaredShot() with { FireKind = null, Phase = "DFPh" }).Bar);
        Assert.Equal(ScenarioA1GunDuels.NotDefensiveFirstFire, Duel(declared: DeclaredShot() with { VehicleTarget = null }).Bar);

        // "is not conducting an OVR (D7.1) ... and the DEFENDER's attack is not Reaction Fire (D7.2)".
        Assert.Equal(ScenarioA1GunDuels.Overrunning, Duel(Duelable with { Overrun = true }).Bar);
        Assert.Equal(ScenarioA1GunDuels.Reaction, Duel(Duelable with { ReactionFire = true }).Bar);

        // Ruling R55.20: ordnance that is a Gun's or a vehicle's MA; no second duel at an MP expenditure (C5.3).
        Assert.Equal(ScenarioA1GunDuels.NotOrdnance, Duel(declared: DeclaredShot() with { Gun = DeclaredShot().Gun! with { DefinitionId = "british-atr" } }).Bar);
        Assert.Equal(ScenarioA1GunDuels.NotOrdnance, Duel(declared: DeclaredShot() with { Gun = DeclaredShot().Gun! with { DefinitionId = OrdnanceTargetTypes.Panzerfaust } }).Bar);
        Assert.Equal(ScenarioA1GunDuels.AlreadyFired, Duel(Duelable with { Dueled = true }).Bar);

        // "may attempt to Bounding First Fire (D3.3) its MA ... provided the vehicle need not change CA": a MA that cannot fire, a shot that is
        // no Bounding First Fire, a turret to turn, and (ruling R55.20) an ammunition other than the default.
        Assert.Equal(ScenarioA1GunDuels.NoShot, Duel(noAnswer: true).Bar);
        Assert.Equal(ScenarioA1GunDuels.NoShot, Duel(answer: AnswerShot() with { FireKind = null }).Bar);
        Assert.Equal(ScenarioA1GunDuels.NoShot, Duel(answer: AnswerShot() with { Vehicle = null }).Bar);
        Assert.Equal(ScenarioA1GunDuels.CaChange, Duel(answer: AnswerShot(turn: 1)).Bar);
        Assert.Equal(ScenarioA1GunDuels.SpecialAmmunition, Duel(answer: AnswerShot(ammunition: "he")).Bar);

        // D3.41, ruling R55.29: an Armor Leader no record has revealed is not counted at the declaration, and his line is in neither total; a
        // revealed one counts, and so does an Inexperienced Crew's +1, which is public. With 3 MP in LOS the tank pays Case C1 +5 and Case I +1.
        var led = AnswerShot(mpInLos: 3, leadership: -2);
        var revealed = Duel(answer: led);
        Assert.Equal((null, 4, 6), (revealed.Bar, revealed.Attacker!.Total, revealed.Defender!.Total));
        Assert.Contains(revealed.Attacker.Lines, line => line.Name == ScenarioA1ArmorLeaders.LeaderModifier);
        var hidden = Duel(Duelable with { UnrevealedArmorLeader = true }, answer: led);
        Assert.Equal((ScenarioA1GunDuels.NotLower, 6, 6), (hidden.Bar, hidden.Attacker!.Total, hidden.Defender!.Total));
        Assert.DoesNotContain(hidden.Attacker.Lines, line => ScenarioA1ArmorLeaders.IsModifier(line.Name));
        Assert.DoesNotContain("Armor Leader", ScenarioA1GunDuels.DrmText(hidden.Attacker), StringComparison.Ordinal);
        var inexperienced = Duel(Duelable with { UnrevealedArmorLeader = true }, declared: DeclaredShot(turn: 3), answer: AnswerShot(leadership: 1));
        Assert.Equal((null, 6, 10), (inexperienced.Bar, inexperienced.Attacker!.Total, inexperienced.Defender!.Total));
        Assert.Contains(inexperienced.Attacker.Lines, line => line.Name == ScenarioA1ArmorLeaders.InexperiencedModifier);

        // The vehicle's potential shot is read only for a shot that can be dueled.
        var asked = 0;
        Assert.Equal(ScenarioA1GunDuels.Concealed, ScenarioA1GunDuels.Verdict(Duelable with { FirerConcealed = true }, DeclaredShot(), () =>
        {
            asked++;
            return AnswerShot();
        }, Ordnance).Bar);
        Assert.Equal(0, asked);
    }

    [Fact]
    public void AGunDuelIsWordedAndItsRecordsAreHeldToTheChoice()
    {
        var verdict = Duel();
        Assert.Equal("r1 in bd03:H10:0 has declared a shot at g1. Gun Duel DRM: g1 +5 (Case C +4, Case I +1); r1 +6 (Case A +6). g1 may fire its MA first (C2.2401)",
            ScenarioA1GunDuels.ChoiceText("r1", "bd03:H10:0", "g1", verdict.Attacker!, verdict.Defender!));
        Assert.Equal("play.gun-duel: declaring this shot lets g1 answer with a Gun Duel (C2.2401): its Gun Duel DRM +5 (Case C +4, Case I +1) is lower than r1's +6 (Case A +6); "
            + "no dice are rolled until it answers", ScenarioA1GunDuels.ProposalText("g1", "r1", verdict.Attacker!, verdict.Defender!));
        Assert.Contains("fires its MA first at r1", ScenarioA1GunDuels.AnswerText(ScenarioA1GunDuels.Duel, "g1", "r1"), StringComparison.Ordinal);
        Assert.Contains("declines the Gun Duel", ScenarioA1GunDuels.AnswerText(ScenarioA1GunDuels.Decline, "g1", "r1"), StringComparison.Ordinal);
        Assert.Equal("r1 in bd03:H10:0 declares a shot at g1, which may fire its MA first in a Gun Duel (C2.2401)", ScenarioA1GunDuels.DeclarationText("r1", " in bd03:H10:0", "g1"));

        // "voids the opponent's return shot by eliminating, breaking, stunning or shocking it", in that order of telling.
        Assert.Equal(ScenarioA1GunDuels.Eliminated, ScenarioA1GunDuels.VoidCause(true, true, false, false, true));
        Assert.Equal(ScenarioA1GunDuels.Broken, ScenarioA1GunDuels.VoidCause(false, true, false, false, false));
        Assert.Equal(ScenarioA1GunDuels.Stunned, ScenarioA1GunDuels.VoidCause(false, false, true, true, false));
        Assert.Equal(ScenarioA1GunDuels.Shocked, ScenarioA1GunDuels.VoidCause(false, false, false, true, false));
        Assert.Equal(ScenarioA1GunDuels.GunOut, ScenarioA1GunDuels.VoidCause(false, false, false, false, true));
        Assert.Equal(ScenarioA1GunDuels.Other, ScenarioA1GunDuels.VoidCause(false, false, false, false, false));
        Assert.Equal("the return shot of r1 is void: its crew is broken (C2.2401)", ScenarioA1GunDuels.VoidText("r1", ScenarioA1GunDuels.Broken));

        // A record that names itself a duel's first shot answers the choice; a record made before the pass names none and is never refused.
        Assert.Null(ScenarioA1GunDuels.ShotRecordRefusal(false, false));
        Assert.Null(ScenarioA1GunDuels.ShotRecordRefusal(false, true));
        Assert.Null(ScenarioA1GunDuels.ShotRecordRefusal(true, true));
        Assert.Equal("UNIT-STATE-049", ScenarioA1GunDuels.ShotRecordRefusal(true, false)!.Code);
        Assert.Null(ScenarioA1GunDuels.VoidRecordRefusal(true, true));
        Assert.NotNull(ScenarioA1GunDuels.VoidRecordRefusal(false, true));
        Assert.NotNull(ScenarioA1GunDuels.VoidRecordRefusal(true, false));

        // D3.3, C2.2401: the duel's shot alone is made while the DEFENDER's window is open.
        var open = new BoundingFireFacts("mph", true, true, true, true, true, false);
        Assert.False(ScenarioA1FireEligibility.MayBoundingFire(open));
        Assert.True(ScenarioA1FireEligibility.MayBoundingFire(open with { GunDuel = true }));
        Assert.False(ScenarioA1FireEligibility.MayBoundingFire(open with { GunDuel = true, AmongMovers = false }));
        Assert.False(ScenarioA1FireEligibility.MayBoundingFire(open with { GunDuel = true, Phase = "afph" }));
    }
}
