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
    public void CanisterIsNotYetFired()
    {
        // A later task of the pass opens it: until then the shot is outside, as any ammunition the package does not resolve. C8.4: Canister "is
        // ineffective vs armored targets", so it stays outside the Vehicle Target Type.
        Assert.Contains(AmmunitionOutside, Resolve(Gun("american", 1944, "canister", vehicle: true, toHit: [3, 1])).Reasons);
        Assert.Contains(GunOutside, Resolve(Gun("american", 1944, "canister", toHit: [3, 1])).Reasons);
        Assert.DoesNotContain(ScenarioA1Definitions.AmmunitionChoices("american-at-gun", true, 1944, 6, false, []), item => item.Ammunition == "canister");
        Assert.Empty(ScenarioA1Definitions.AmmunitionChoices("american-at-gun", false, 1944, 6, false, []));

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
}
