using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a firing Location's night sight gives the attack (E1.1, E1.101; ruling R16.2): whether the target is beyond NVR but seen as a Gunflash, or the refusal.</summary>
public sealed record NightSightVerdict(bool BeyondNvr, string? Reason);

/// <summary>A firing Location as the night and weather facts read it (rulings R16.2, R16.3, R16.11): its range to the target, the Hindrance DRM of its LOS, and its height.</summary>
public sealed record FiringLocationFacts(int Range, int? HindranceDrm, int Height);

/// <summary>The Wind Change DR's outcome (B25.65, E1.12, E3.4, E3.51, E3.71; rulings R16.1, R16.10): the Base NVR after it, the precipitation, whether a Gust blows, and the notes in order.</summary>
public sealed record WindChangeVerdict(int? Nvr, string? Precipitation, bool Gust, IReadOnlyList<string> Notes);

/// <summary>
/// Night and weather (backlog pass 16, rulings R16.1 to R16.14; pass 32.h, S9): the SSRs that declare them, what a unit sees at night (its NVR, Illumination,
/// and Gunflashes), the Low Visibility DRM and the other facts they give an attack, the weather's MF and MP, and the Wind Change DR. Play reads the state and
/// the map, hands the facts over, and writes the events.
/// </summary>
public static class ScenarioA1NightAndWeather
{
    // The weather, sky, and Axis lists (E3, E1.11, E3.741), named as the planner named them so the refusal texts keep their holes.
    private static readonly IReadOnlyList<string> WeatherKinds = ScenarioA1Definitions.WeatherKinds;
    private static readonly IReadOnlyList<string> Clouds = ScenarioA1Definitions.Clouds;
    private static readonly IReadOnlyList<string> Moons = ScenarioA1Definitions.Moons;
    private static readonly IReadOnlyList<string> Axis = ScenarioA1Definitions.AxisNationalities;

    /// <summary>The markers that make a Gunflash (E1.8; ruling R16.2): Prep, First, Final, Bounding, or Intensive Fire, or a Melee.</summary>
    public static IReadOnlyList<UnitCondition> GunflashMarkers
    {
        get;
    } =
        [UnitCondition.PrepFire, UnitCondition.FirstFire, UnitCondition.FinalFire, UnitCondition.BoundingFire, UnitCondition.IntensiveFire, UnitCondition.Melee];

    /// <summary>The Base NVR an SSR <c>night:n</c> names, or null (E1.1; ruling R16.1).</summary>
    public static int? NightRule(IReadOnlyList<string> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.Select(rule => rule.StartsWith("night:", StringComparison.Ordinal)
            && int.TryParse(rule.AsSpan(6), NumberStyles.None, CultureInfo.InvariantCulture, out var nvr) ? nvr : (int?)null)
            .FirstOrDefault(nvr => nvr is not null);
    }

    /// <summary>
    /// Why the SSRs of a new game are refused (rulings R16.1, R16.9, and R23.5 for HIP), or null. <paramref name="hipAllowanceKnown"/> reads the setup's HIP
    /// allowance of a rule and a side (S10), asked for a well-formed <c>hip:</c> rule.
    /// </summary>
    public static string? RulesBar(IReadOnlyList<string> rules, Func<string, string, bool> hipAllowanceKnown, bool hasMonth, bool hasYear)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(hipAllowanceKnown);
        if (rules.FirstOrDefault(rule => rule.StartsWith("hip:", StringComparison.Ordinal)
            && (rule.Split(':') is not [_, { Length: > 0 } side, _] || !hipAllowanceKnown(rule, side))) is { } hip)
        {
            return $"play.hip-rule: '{hip}' is not hip:<side>:<n>, n the squad-equivalents of the side that may set up hidden, more than 0 (A12.3; ruling R23.5)";
        }

        var weather = rules.Where(rule => rule.StartsWith("weather:", StringComparison.Ordinal)).Select(rule => rule["weather:".Length..]).ToArray();
        if (weather.FirstOrDefault(kind => !WeatherKinds.Contains(kind, StringComparer.Ordinal)) is { } unknown)
        {
            return $"play.weather-rule: 'weather:{unknown}' is not built; the weather SSRs are {string.Join(", ", WeatherKinds.Select(kind => "weather:" + kind))} (Fog, Drifts, "
                + "Winter Camouflage, and the Weather Charts of Chapters F, G, and H are not built; E3; ruling R16.9)";
        }

        var snow = weather.Any(kind => kind is "ground-snow" or "deep-snow");
        if ((weather.Contains("mud") && snow) || (weather.Contains("ground-snow") && weather.Contains("deep-snow")) || (weather.Contains("rain") && weather.Contains("heavy-rain"))
            || (weather.Any(kind => kind is "rain" or "heavy-rain") && weather.Contains("falling-snow")))
        {
            return "play.weather-rule: Mud and snow, Ground and Deep Snow, rain and heavy rain, and rain and Falling Snow do not combine (E3.6, E3.7; ruling R16.9)";
        }

        if (weather.Contains("extreme-winter") && !weather.Any(kind => kind is "ground-snow" or "deep-snow" or "falling-snow"))
        {
            return "play.weather-rule: Extreme Winter comes with a condition of the Snow Chart: Falling, Ground, or Deep Snow (E3.74; ruling R16.14)";
        }

        if (weather.Contains("extreme-winter") && (!hasMonth || !hasYear))
        {
            return "play.weather-rule: Extreme Winter needs the scenario's month and year, which decide whose weapons and Fate it affects (E3.741, E3.742; ruling R16.14)";
        }

        var nights = rules.Where(rule => rule.StartsWith("night:", StringComparison.Ordinal)).ToArray();
        if (nights.Length > 1 || (nights.Length == 1 && (NightRule(nights) is not { } named || named < (snow ? 2 : 0) || named > (snow ? 9 : 6))))
        {
            return "play.night-rule: one SSR 'night:n' names the Base NVR, 0 to 6 (2 to 9 with Ground or Deep Snow) (E1.1, E1.15; ruling R16.1)";
        }

        if (rules.FirstOrDefault(rule => rule.StartsWith("night-clouds:", StringComparison.Ordinal) && !Clouds.Contains(rule["night-clouds:".Length..], StringComparer.Ordinal)
            || rule.StartsWith("night-moon:", StringComparison.Ordinal) && !Moons.Contains(rule["night-moon:".Length..], StringComparer.Ordinal)) is { } sky)
        {
            return $"play.night-rule: '{sky}' is not a sky of the NVR Table: night-clouds:none, scattered, or overcast; night-moon:none, half, or full (E1.11)";
        }

        return rules.Any(rule => rule.StartsWith("night-", StringComparison.Ordinal)) && nights.Length == 0
            ? "play.night-rule: a sky SSR needs 'night:n' too (E1.1)" : null;
    }

    /// <summary>Whether rain has fallen, as the SSRs name it or the Wind Change DR started it (E3.54; ruling R16.12).</summary>
    public static bool Rain(bool rained, string? precipitation) => rained || precipitation is "rain" or "heavy-rain";

    /// <summary>
    /// An Infantry step's night and weather MF, in half MF, and whether it keeps the road rate (rulings R16.5, R16.12, R16.13): at night one MF more
    /// into Concealment Terrain but across a road hexside (E1.51); in or after rain, and in Ground or Deep Snow, one MF more per level changed but on a
    /// paved (rain) or plowed (snow) road (E3.54, E3.723); in Mud half an MF more into Open Ground but by a paved road, whose rate an unpaved road loses
    /// (E3.6, E3.64); in Ground or Deep Snow the road rate only on a plowed road (E3.723), and in Deep Snow half an MF more per hexside but into woods,
    /// a building, or rubble, or across a plowed road hexside (E3.733). <paramref name="pavedRoadCrossed"/> says the hexside's terrain is a Paved Road.
    /// </summary>
    public static (int HalfMf, bool RoadRate) InfantryWeatherHalfMf(bool night, bool rain, bool mud, bool groundSnow, bool deepSnow, bool plowedRoads, int? month,
        bool pavedRoadCrossed, string terrain, bool road, int rise)
    {
        var paved = road && pavedRoadCrossed;
        var plowed = road && plowedRoads;
        var snow = groundSnow || deepSnow;
        var extra = 0;
        var roadRate = road;
        if (night && !road && ScenarioA1Definitions.IsConcealmentTerrain(terrain, month))
        {
            extra += 2;
        }

        if (rise != 0 && ((rain && !paved) || (snow && !plowed)))
        {
            extra += 2 * Math.Abs(rise);
        }

        if (mud && !paved)
        {
            roadRate = false;
            extra += terrain == "open-ground" ? 1 : 0;
        }

        if (snow && !plowed)
        {
            roadRate = false;
        }

        if (deepSnow && !plowed && terrain is not ("woods" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"))
        {
            extra += 1;
        }

        return (extra, roadRate);
    }

    /// <summary>
    /// A vehicle's night and weather MP, in half MP (rulings R16.5, R16.12, R16.13): one MP more per hexside at night (E1.52); in or after rain one MP
    /// more per level changed but on a paved road (E3.54); in Mud one MP more into Open Ground (E3.64); in Ground Snow one MP more per hexside for a
    /// vehicle that is not tracked (E3.724); in Deep Snow, off plowed roads, one MP more per hexside if tracked, two if not, along them one more if not
    /// tracked (E3.7331). The road entry of at least one MP in snow is the entry's own cost.
    /// </summary>
    public static int VehicleWeatherHalfMp(bool night, bool rain, bool mud, bool groundSnow, bool deepSnow, string type, string terrain, bool paved, bool plowed, int rise)
    {
        var tracked = type is "fully-tracked" or "half-tracked";
        var extra = night ? 2 : 0;
        if (rise != 0 && rain && !paved)
        {
            extra += 2 * Math.Abs(rise);
        }

        if (mud && !paved && terrain == "open-ground")
        {
            extra += 2;
        }

        if (groundSnow && !tracked)
        {
            extra += 2;
        }

        if (deepSnow)
        {
            extra += plowed ? (tracked ? 0 : 2) : (tracked ? 2 : 4);
        }

        return extra;
    }

    /// <summary>The Base NVR's limits (E1.12, E1.15; ruling R16.1): 0 to 6, or 2 to 9 with Ground or Deep Snow.</summary>
    public static (int Min, int Max) NvrLimits(bool snow) => snow ? (2, 9) : (0, 6);

    /// <summary>A viewer's NVR at night (E1.1, E1.14; ruling R16.2): the Base NVR, halved (FRD) from a BU AFV; null by day.</summary>
    public static int? NvrOf(int? nvr, bool buttonedUpVehicle) =>
        nvr is not { } value ? null
        : buttonedUpVehicle ? value / 2
        : value;

    /// <summary>
    /// Whether a Location is Illuminated at night (E1.9, E1.923, E1.94; ruling R16.8): within three hexes of a Starshell, or within two hexes of a Blaze
    /// (a burning wreck), over the distances to each, read as they are asked for. Shadows (E1.941) are not built.
    /// </summary>
    public static bool Illuminated(bool night, IEnumerable<int?> starshellDistances, IEnumerable<int?> blazeDistances)
    {
        ArgumentNullException.ThrowIfNull(starshellDistances);
        ArgumentNullException.ThrowIfNull(blazeDistances);
        if (!night)
        {
            return false;
        }

        return starshellDistances.Any(distance => distance <= 3) || blazeDistances.Any(distance => distance <= 2);
    }

    /// <summary>
    /// Whether a Location is marked by a Gunflash (E1.8; ruling R16.2): one of its active occupants holds a Prep, First, Final, Bounding, or Intensive
    /// Fire counter, or a Melee; each occupant answers whether it has a marker.
    /// </summary>
    public static bool Gunflash(IEnumerable<Func<UnitCondition, bool>> activeOccupants)
    {
        ArgumentNullException.ThrowIfNull(activeOccupants);
        return activeOccupants.Any(has => GunflashMarkers.Any(has));
    }

    /// <summary>E1.14 (referee, pass 16): how far a vehicle in Motion is within NVR: 1.5 times it (FRU), twice for a tracked one; with an NVR of 0, 1 hex if wheeled or 2 if tracked.</summary>
    public static int MotionVehicleSightRange(string movementType, int nvr) =>
        movementType is "fully-tracked" or "half-tracked" ? (nvr == 0 ? 2 : nvr * 2) : nvr == 0 ? 1 : (nvr * 3 + 1) / 2;

    /// <summary>
    /// What a viewer at <paramref name="from"/> sees at night of a Location <paramref name="target"/>, <paramref name="range"/> hexes away (E1.101, E1.13,
    /// E1.14, E1.81; ruling R16.2): within NVR or Illuminated, it sees it; beyond NVR, a moving vehicle within 1.5 times the NVR (twice if tracked) is within
    /// it, and a Gunflash is seen as a concealed target; an Illuminated viewer sees only Illuminated Locations and Gunflashes. By day it sees what its LOS
    /// allows. The Illumination and Gunflash reads are made in the old order, and the Motion vehicles' movement types are read as they are asked for.
    /// </summary>
    public static NightSightVerdict NightSight(int? nvr, string from, string target, int range, Func<bool> targetIlluminated, Func<bool> targetGunflash, Func<bool> fromIlluminated,
        IEnumerable<string?> motionVehicleTypes)
    {
        ArgumentNullException.ThrowIfNull(targetIlluminated);
        ArgumentNullException.ThrowIfNull(targetGunflash);
        ArgumentNullException.ThrowIfNull(fromIlluminated);
        ArgumentNullException.ThrowIfNull(motionVehicleTypes);
        if (nvr is not { } sight || range == 0)
        {
            return new NightSightVerdict(false, null);
        }

        var lit = targetIlluminated();
        var flash = targetGunflash();
        if (fromIlluminated() && !lit && !flash)
        {
            return new NightSightVerdict(false, $"play.night-illuminated: {from} is Illuminated, and an Illuminated unit sees only Illuminated Locations and Gunflashes (E1.9; ruling R16.2)");
        }

        if (lit || range <= sight)
        {
            return new NightSightVerdict(false, null);
        }

        // E1.14 (referee, pass 16): a vehicle in Motion is within NVR at up to 1.5 times it (FRU), twice for a tracked one; with an NVR of 0, at 1 hex if
        // wheeled or 2 if tracked.
        if (motionVehicleTypes.Any(type => type is { } movementType && range <= MotionVehicleSightRange(movementType, sight)))
        {
            return new NightSightVerdict(false, null);
        }

        return flash ? new NightSightVerdict(true, null)
            : new NightSightVerdict(false, $"play.night-nvr: {target} is {range} hexes away, beyond the NVR of {sight}, and neither Illuminated nor marked by a Gunflash (E1.1, E1.101; ruling R16.2)");
    }

    /// <summary>
    /// The Extreme Winter reduction of a side's B# and X# (E3.741; ruling R16.14): 1 for Russians before April 1941, 2 for Axis but Finns before
    /// April 1942; null otherwise, and null when the side's nationality is not known.
    /// </summary>
    public static int? ExtremeWinterReduction(bool extremeWinter, int? year, int? month, string? nationality)
    {
        if (!extremeWinter || year is not { } scenarioYear || month is not { } scenarioMonth || nationality is null)
        {
            return null;
        }

        var date = scenarioYear * 100 + scenarioMonth;
        return nationality == "russian" && date < 194104 ? 1
            : nationality != "finnish" && Axis.Contains(nationality, StringComparer.Ordinal) && date < 194204 ? 2
            : null;
    }

    /// <summary>
    /// The night and weather facts of an attack (rulings R16.2, R16.3, R16.11 to R16.14): refusal beyond NVR, fire at a Gunflash as at a concealed target,
    /// no multi-Location fire group at night, the Low Visibility DRM, the Mud and Deep Snow cushion, and the Extreme Winter B#. The firing Locations come
    /// in the planner's order, and <paramref name="nightSight"/> reads each one's sight by its index.
    /// </summary>
    public static (FireAttack? Facts, string? Reason) NightAndWeatherFacts(FireAttack facts, int? breakdownReduction, bool targetsInMelee, bool mud, bool deepSnow, bool night,
        bool mist, string? precipitation, int targetTop, IReadOnlyList<FiringLocationFacts> perLocation, Func<int, NightSightVerdict> nightSight)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(perLocation);
        ArgumentNullException.ThrowIfNull(nightSight);
        var residual = facts.FireKind == ScenarioA1FireCalculator.ResidualFire;
        facts = facts with
        {
            BreakdownReduction = breakdownReduction,

            // Pass 31 (ruling R31.3; A7.81): every attack planned from the pass on fires a pinned unit's MG as Area Fire.
            PinnedMgAreaFire = true,

            // Referee, pass 31 (A11.141): units in Melee take and cause no Leader Loss check.
            TargetsInMelee = targetsInMelee ? true : null,
            CushionedOpenGround = facts.OrdnanceHit is not null && (mud || deepSnow) ? true : null,
        };
        if (residual)
        {
            return (facts, null);
        }

        var beyond = false;
        if (night)
        {
            // E1.75 (ruling R16.3): no fire group spans Locations at night.
            if (perLocation.Count > 1)
            {
                return (null, "play.night-fire-group: a fire group spanning more than one Location is not allowed at night (E1.75; ruling R16.3)");
            }

            for (var index = 0; index < perLocation.Count; index++)
            {
                var (far, reason) = nightSight(index);
                if (reason is not null)
                {
                    return (null, reason);
                }

                beyond |= far;
            }
        }

        // E1.7 (ruling R16.3): +1 at night, but in the firer's own hex, with Height Advantage, or where the target hex's terrain tops out a full level above
        // every firer; E3.32, E3.51, E3.71 (ruling R16.11): Mist, rain, and Falling Snow +1 per six hexes beyond six, heavy precipitation +1 more.
        var range = perLocation.Max(item => item.Range);
        var nightDrm = night && range > 0 && facts.HeightAdvantage != true && facts.FireLane != true
            && !perLocation.All(item => targetTop >= item.Height + 1) ? 1 : 0;
        var heavy = precipitation is "heavy-rain" or "heavy-snow";
        var weatherDrm = range == 0 || facts.FireLane == true ? 0 : (mist && range > 6 ? (range - 1) / 6 : 0) + (heavy ? 1 : 0);
        // A6.2, E3.1 (referee, pass 16): Low Visibility and other Hindrances of 6 or more block the LOS.
        var lowVisibility = nightDrm + weatherDrm;
        if (lowVisibility > 0 && lowVisibility + perLocation.Max(item => item.HindranceDrm ?? 0) >= 6)
        {
            return (null, $"play.night-los: the Low Visibility DRM of {lowVisibility} with the other Hindrances reaches 6, which blocks the LOS (A6.2, E3.1)");
        }

        return (facts with
        {
            LowVisibilityDrm = lowVisibility > 0 ? lowVisibility : null,
            BeyondNvr = beyond ? true : null,
        }, null);
    }

    /// <summary>
    /// The Wind Change DR at the start of a RPh (B25.65; rulings R16.1, R16.10): made when night, Overcast, rain, Falling Snow, or Gusty weather is in
    /// effect, after the opening Player Turn.
    /// </summary>
    public static bool WindChangeDue(string phase, int turn, string phasing, string? firstSide, bool night, bool overcast, bool gusty, string? precipitation, bool rain, bool heavyRain,
        bool fallingSnow) =>
        phase == "rph" && !(turn == 1 && phasing == firstSide)
        && (night || overcast || gusty || precipitation is not null || rain || heavyRain || fallingSnow);

    /// <summary>
    /// The Wind Change DR's outcome (B25.65, E1.12, E3.4, E3.51, E3.71; rulings R16.1, R16.10): a colored 6 changes the Base NVR by one hex, with Scattered
    /// clouds and a Half or Full Moon by a further dr / 3 or / 2 (FRU), drawn through <paramref name="nvrChangeDr"/> only then; with Overcast (rain) or
    /// Falling Snow, 10 or more starts or intensifies the precipitation, 3 or less ends it; a Gust blows on 10 or more in Gusty weather.
    /// </summary>
    public static WindChangeVerdict WindChange(int colored, int white, int? nvr, bool starshellUsed, bool moonHalf, bool moonFull, bool scatteredClouds, bool snow,
        bool overcast, bool rain, bool heavyRain, bool fallingSnow, bool gusty, string? precipitation, Func<int> nvrChangeDr)
    {
        ArgumentNullException.ThrowIfNull(nvrChangeDr);
        var dr = colored + white;
        var notes = new List<string>();

        // E1.12: a colored 6 changes the Base NVR by one hex; with Scattered clouds and a Half or Full Moon, by a further dr / 3 or / 2 (FRU).
        var changedNvr = nvr;
        if (nvr is { } before && colored == 6)
        {
            var direction = white <= 3 ? -1 : white >= 5 || !starshellUsed ? 1 : 0;
            var step = 1;
            var moon = moonHalf ? 3 : moonFull ? 2 : 0;
            if (direction != 0 && moon != 0 && scatteredClouds)
            {
                step = (nvrChangeDr() + moon - 1) / moon;
            }

            var (min, max) = NvrLimits(snow);
            changedNvr = Math.Clamp(before + direction * step, Math.Min(min, before), Math.Max(max, before));
            notes.Add(changedNvr == before ? "the Base NVR stays " + before.ToString(CultureInfo.InvariantCulture)
                : $"the Base NVR goes from {before} to {changedNvr}");
        }

        // E3.51, E3.71: with Overcast (rain) or Falling Snow, 10 or more starts or intensifies the precipitation, 3 or less ends it.
        var changedPrecipitation = precipitation;
        var raining = overcast || rain || heavyRain;
        var snowing = fallingSnow;
        if (raining || snowing)
        {
            var (light, heavy) = snowing ? ("snow", "heavy-snow") : ("rain", "heavy-rain");
            changedPrecipitation = dr >= 10 ? (changedPrecipitation is null ? light : heavy) : dr <= 3 ? null : changedPrecipitation;
            if (changedPrecipitation != precipitation)
            {
                notes.Add(changedPrecipitation is null ? $"the {(snowing ? "snowfall" : "rain")} stops" : $"{changedPrecipitation.Replace('-', ' ')} falls");
            }
        }

        // E3.4: a Gust on 10 or more in Gusty weather.
        var gust = gusty && dr >= 10;
        if (gust)
        {
            notes.Add("a Gust blows");
        }

        return new WindChangeVerdict(changedNvr, changedPrecipitation, gust, notes);
    }

    /// <summary>The Wind Change DR in words (B25.65, E1.12, E3.51).</summary>
    public static string WindChangeText(int colored, int white, int dr, IReadOnlyList<string> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        return $"play.wind-change: the Wind Change DR is {colored} + {white} = {dr}: {(notes.Count == 0 ? "no change" : string.Join("; ", notes))} (B25.65, E1.12, E3.51)";
    }
}
