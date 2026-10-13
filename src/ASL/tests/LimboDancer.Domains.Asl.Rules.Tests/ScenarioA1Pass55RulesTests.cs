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
    public void ApdsCanisterAndTheSixPoundersHeAreNotYetFired()
    {
        // A later task of the pass opens them: until then the shot is outside, as any ammunition the package does not resolve.
        Assert.Contains(AmmunitionOutside, Resolve(Gun("british", 1944, "apds", vehicle: true, toHit: [3, 1])).Reasons);
        Assert.Contains(AmmunitionOutside, Resolve(Gun("american", 1944, "canister", vehicle: true, toHit: [3, 1])).Reasons);
        Assert.Contains(AmmunitionOutside, Resolve(Gun("british", 1944, "he", vehicle: true, toHit: [3, 1])).Reasons);
        Assert.Contains(GunOutside, Resolve(Gun("american", 1944, "canister", toHit: [3, 1])).Reasons);

        // C2.21: the underscored 6-Pounder fires no HE at Infantry, named or not.
        Assert.Contains(GunOutside, Resolve(Gun("british", 1944, toHit: [3, 1])).Reasons);
        Assert.Contains(GunOutside, Resolve(Gun("british", 1944, "he", toHit: [3, 1])).Reasons);

        // The American 37mm fires its AP at a tank (C7.31: 37LL is 11) and its HE at Infantry.
        var ap = Resolve(Gun("american", 1944, "ap", vehicle: true, toHit: [5, 6]));
        Assert.Equal(OrdnanceResolution.Resolved, ap.Disposition);
        Assert.Equal(OrdnanceResolution.Resolved, Resolve(Gun("american", 1944, toHit: [5, 6])).Disposition);
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
