using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Night and weather (backlog pass 16, rulings R16.1 to R16.14): the SSRs that declare them, what a unit sees at night (its NVR, Illumination, and
/// Gunflashes), the Low Visibility DRM and the other facts they give an attack, the Wind Change DR at the start of each RPh, and Starshells.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The weather an SSR may name (E3; ruling R16.9).</summary>
    private static readonly string[] WeatherKinds =
        ["overcast", "gusty", "mist", "rain", "heavy-rain", "mud", "falling-snow", "ground-snow", "deep-snow", "extreme-winter"];

    private static readonly string[] Clouds = ["none", "scattered", "overcast"];
    private static readonly string[] Moons = ["none", "half", "full"];

    /// <summary>The Axis nationalities of the catalog, for Extreme Winter (E3.741, E3.742): the Finns are Axis too, but excepted there.</summary>
    private static readonly string[] Axis = ["german", "italian", "finnish", "japanese"];

    /// <summary>Why the SSRs of a new game are refused (rulings R16.1, R16.9, and R23.5 for HIP), or null.</summary>
    internal static string? NightAndWeatherRulesBar(JsonElement start)
    {
        string[] rules = start.TryGetProperty("specialRules", out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : [];
        if (rules.FirstOrDefault(rule => rule.StartsWith("hip:", StringComparison.Ordinal)
            && (rule.Split(':') is not [_, { Length: > 0 } side, _] || ScenarioSetup.HipAllowance([rule], side) is null)) is { } hip)
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

        if (weather.Contains("extreme-winter") && (!start.TryGetProperty("scenarioMonth", out var month) || month.ValueKind != JsonValueKind.Number
            || !start.TryGetProperty("scenarioYear", out var year) || year.ValueKind != JsonValueKind.Number))
        {
            return "play.weather-rule: Extreme Winter needs the scenario's month and year, which decide whose weapons and Fate it affects (E3.741, E3.742; ruling R16.14)";
        }

        var nights = rules.Where(rule => rule.StartsWith("night:", StringComparison.Ordinal)).ToArray();
        if (nights.Length > 1 || (nights.Length == 1 && (GameState.NightRule(nights) is not { } named || named < (snow ? 2 : 0) || named > (snow ? 9 : 6))))
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

    /// <summary>Whether a terrain is Concealment Terrain (A12.12, as the concealment gain reads it): grain only June to September.</summary>
    private static bool ConcealmentTerrain(string terrain, int? month) =>
        terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
        || (terrain == "grain" && month is >= 6 and <= 9);

    /// <summary>Whether rain has fallen, as the SSRs name it or the Wind Change DR started it (E3.54; ruling R16.12).</summary>
    private static bool Rain(GameState state) =>
        state.Rained || state.Precipitation is "rain" or "heavy-rain";

    /// <summary>
    /// An Infantry step's night and weather MF, in half MF, and whether it keeps the road rate (rulings R16.5, R16.12, R16.13): at night one MF more
    /// into Concealment Terrain but across a road hexside (E1.51); in or after rain, and in Ground or Deep Snow, one MF more per level changed but on a
    /// paved (rain) or plowed (snow) road (E3.54, E3.723); in Mud half an MF more into Open Ground but by a paved road, whose rate an unpaved road loses
    /// (E3.6, E3.64); in Ground or Deep Snow the road rate only on a plowed road (E3.723), and in Deep Snow half an MF more per hexside but into woods,
    /// a building, or rubble, or across a plowed road hexside (E3.733).
    /// </summary>
    private static (int HalfMf, bool RoadRate) InfantryWeatherHalfMf(GameState state, Maps.Derivation.HexsideFacts crossed, string terrain, bool road, int rise)
    {
        var paved = road && crossed.Terrain?.Name == "Paved Road";
        var plowed = road && state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal);
        var snow = state.Weather("ground-snow") || state.Weather("deep-snow");
        var extra = 0;
        var roadRate = road;
        if (state.Night && !road && ConcealmentTerrain(terrain, state.ScenarioMonth))
        {
            extra += 2;
        }

        if (rise != 0 && ((Rain(state) && !paved) || (snow && !plowed)))
        {
            extra += 2 * Math.Abs(rise);
        }

        if (state.Weather("mud") && !paved)
        {
            roadRate = false;
            extra += terrain == "open-ground" ? 1 : 0;
        }

        if (snow && !plowed)
        {
            roadRate = false;
        }

        if (state.Weather("deep-snow") && !plowed && terrain is not ("woods" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"))
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
    private static int VehicleWeatherHalfMp(GameState state, string type, string terrain, bool road, bool paved, bool plowed, int rise)
    {
        var tracked = type is "fully-tracked" or "half-tracked";
        var extra = state.Night ? 2 : 0;
        if (rise != 0 && Rain(state) && !paved)
        {
            extra += 2 * Math.Abs(rise);
        }

        if (state.Weather("mud") && !paved && terrain == "open-ground")
        {
            extra += 2;
        }

        if (state.Weather("ground-snow") && !tracked)
        {
            extra += 2;
        }

        if (state.Weather("deep-snow"))
        {
            extra += plowed ? (tracked ? 0 : 2) : (tracked ? 2 : 4);
        }

        return extra;
    }

    /// <summary>The Base NVR's limits (E1.12, E1.15; ruling R16.1): 0 to 6, or 2 to 9 with Ground or Deep Snow.</summary>
    private static (int Min, int Max) NvrLimits(GameState state) =>
        state.Weather("ground-snow") || state.Weather("deep-snow") ? (2, 9) : (0, 6);

    /// <summary>A viewer's NVR at night (E1.1, E1.14; ruling R16.2): the Base NVR, halved (FRD) from a BU AFV; null by day.</summary>
    private static int? NvrOf(GameState state, UnitInstance? viewer) =>
        state.Nvr is not { } nvr ? null
        : viewer is not null && LiveFire.IsVehicle(viewer) && Is(viewer, Conditions.ButtonedUp) ? nvr / 2
        : nvr;

    /// <summary>
    /// Whether a Location is Illuminated at night (E1.9, E1.923, E1.94; ruling R16.8): within three hexes of a Starshell, or within two hexes of a Blaze
    /// (a burning wreck). Shadows (E1.941) are not built.
    /// </summary>
    private bool Illuminated(GameState state, BoardLocation location)
    {
        if (!state.Night)
        {
            return false;
        }

        var starshells = state.Entities.Where(entity => entity.Status == InstanceStatus.Active && entity.Kind == "asl:starshell" && entity.Position is MapPosition)
            .Select(entity => ((MapPosition)entity.Position).Location);
        var blazes = state.Units.Where(unit => unit.Status == InstanceStatus.Wrecked && IsBurning(state, unit) && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location);
        return starshells.Any(at => HexDistance(state, at, location) <= 3) || blazes.Any(at => HexDistance(state, at, location) <= 2);
    }

    /// <summary>
    /// Whether a Location is marked by a Gunflash (E1.8; ruling R16.2): it holds a unit or weapon with a Prep, First, Final, Bounding, or Intensive Fire
    /// counter, or a Melee.
    /// </summary>
    private static bool Gunflash(GameState state, BoardLocation location) =>
        state.At(location).Any(item => item.Status is InstanceStatus.Active && new[] { Conditions.PrepFire, Conditions.FirstFire, Conditions.FinalFire,
            Conditions.BoundingFire, Conditions.IntensiveFire, Conditions.Melee }.Any(name => GameState.Condition(item, name) == ConditionState.True));

    /// <summary>
    /// What a viewer at <paramref name="from"/> sees at night of a Location <paramref name="range"/> hexes away (E1.101, E1.13, E1.14, E1.81; ruling
    /// R16.2): within NVR or Illuminated, it sees it; beyond NVR, a moving vehicle within 1.5 times the NVR (twice if tracked) is within it, and a Gunflash
    /// is seen as a concealed target; an Illuminated viewer sees only Illuminated Locations and Gunflashes. By day it sees what its LOS allows.
    /// </summary>
    private (bool BeyondNvr, string? Reason) NightSight(GameState state, UnitInstance? viewer, BoardLocation from, BoardLocation target, int range,
        IEnumerable<UnitInstance> targets)
    {
        if (NvrOf(state, viewer) is not { } nvr || range == 0)
        {
            return (false, null);
        }

        var lit = Illuminated(state, target);
        var flash = Gunflash(state, target);
        if (Illuminated(state, from) && !lit && !flash)
        {
            return (false, $"play.night-illuminated: {from} is Illuminated, and an Illuminated unit sees only Illuminated Locations and Gunflashes (E1.9; ruling R16.2)");
        }

        if (lit || range <= nvr)
        {
            return (false, null);
        }

        // E1.14 (referee, pass 16): a vehicle in Motion is within NVR at up to 1.5 times it (FRU), twice for a tracked one; with an NVR of 0, at 1 hex if
        // wheeled or 2 if tracked.
        if (targets.Any(unit => LiveFire.IsVehicle(unit) && Is(unit, Conditions.Motion) && VehicleDefinition(unit) is { } definition
            && range <= (definition.MovementType is "fully-tracked" or "half-tracked" ? (nvr == 0 ? 2 : nvr * 2) : nvr == 0 ? 1 : (nvr * 3 + 1) / 2)))
        {
            return (false, null);
        }

        return flash ? (true, null)
            : (false, $"play.night-nvr: {target} is {range} hexes away, beyond the NVR of {nvr}, and neither Illuminated nor marked by a Gunflash (E1.1, E1.101; ruling R16.2)");
    }

    /// <summary>
    /// The Extreme Winter reduction of a side's B# and X# (E3.741; ruling R16.14): 1 for Russians before April 1941, 2 for Axis but Finns before
    /// April 1942; null otherwise.
    /// </summary>
    internal static int? ExtremeWinterReduction(GameState state, string? side)
    {
        if (!state.Weather("extreme-winter") || state.ScenarioYear is not { } year || state.ScenarioMonth is not { } month
            || state.Sides.FirstOrDefault(item => item.Id == side)?.Nationality is not { } nationality)
        {
            return null;
        }

        var date = year * 100 + month;
        return nationality == "russian" && date < 194104 ? 1
            : nationality != "finnish" && Axis.Contains(nationality, StringComparer.Ordinal) && date < 194204 ? 2
            : null;
    }

    /// <summary>
    /// The night and weather facts of an attack (rulings R16.2, R16.3, R16.11 to R16.14): refusal beyond NVR, fire at a Gunflash as at a concealed target,
    /// no multi-Location fire group at night, the Low Visibility DRM, the Mud and Deep Snow cushion, and the Extreme Winter B#.
    /// </summary>
    private (FireAttack? Facts, string? Reason) NightAndWeatherFacts(GameState state, FireAttack facts, BoardLocation target, int targetTop,
        IReadOnlyDictionary<string, (int Range, bool SameLevel, FireLos Los, int Height)> perLocation)
    {
        var residual = facts.FireKind == ScenarioA1FireCalculator.ResidualFire;
        // E1.14 (referee, pass 16): a vehicle firing, its MA's shot included, sees with its own NVR.
        var viewer = facts.VehicleFire is { } byVehicle ? state.Unit(byVehicle.VehicleId!)
            : facts.Firers is { Count: > 0 } shooting && state.Unit(shooting[0].UnitId ?? string.Empty) is { } shooter && LiveFire.IsVehicle(shooter) ? shooter : null;
        var side = viewer?.Side ?? state.Unit((facts.Firers is { Count: > 0 } firing ? firing[0].UnitId : null) ?? string.Empty)?.Side;
        facts = facts with
        {
            BreakdownReduction = ExtremeWinterReduction(state, side),
            CushionedOpenGround = facts.OrdnanceHit is not null && (state.Weather("mud") || state.Weather("deep-snow")) ? true : null,
        };
        if (residual)
        {
            return (facts, null);
        }

        var night = state.Night;
        var beyond = false;
        if (night)
        {
            // E1.75 (ruling R16.3): no fire group spans Locations at night.
            if (perLocation.Count > 1)
            {
                return (null, "play.night-fire-group: a fire group spanning more than one Location is not allowed at night (E1.75; ruling R16.3)");
            }

            // E1.14 (table player, pass 16): the target Location's vehicles are read with its other targets.
            var targets = (facts.Targets ?? []).Select(item => state.Unit(item.UnitId ?? string.Empty))
                .Concat((facts.Vehicles ?? []).Select(item => state.Unit(item.VehicleId ?? string.Empty))).OfType<UnitInstance>().ToArray();
            foreach (var (location, value) in perLocation)
            {
                var (far, reason) = NightSight(state, viewer, BoardLocation.Parse(location), target, value.Range, targets);
                if (reason is not null)
                {
                    return (null, reason);
                }

                beyond |= far;
            }
        }

        // E1.7 (ruling R16.3): +1 at night, but in the firer's own hex, with Height Advantage, or where the target hex's terrain tops out a full level above
        // every firer; E3.32, E3.51, E3.71 (ruling R16.11): Mist, rain, and Falling Snow +1 per six hexes beyond six, heavy precipitation +1 more.
        var range = perLocation.Values.Max(item => item.Range);
        var nightDrm = night && range > 0 && facts.HeightAdvantage != true && facts.FireLane != true
            && !perLocation.Values.All(item => targetTop >= item.Height + 1) ? 1 : 0;
        var mist = state.Weather("mist") || state.Precipitation is not null;
        var heavy = state.Precipitation is "heavy-rain" or "heavy-snow";
        var weatherDrm = range == 0 || facts.FireLane == true ? 0 : (mist && range > 6 ? (range - 1) / 6 : 0) + (heavy ? 1 : 0);
        // A6.2, E3.1 (referee, pass 16): Low Visibility and other Hindrances of 6 or more block the LOS.
        var lowVisibility = nightDrm + weatherDrm;
        if (lowVisibility > 0 && lowVisibility + perLocation.Values.Max(item => item.Los.HindranceDrm ?? 0) >= 6)
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
    /// effect, after the opening Player Turn. Null when none is made.
    /// </summary>
    private static bool WindChangeDue(GameState state, string phase, int turn, string phasing) =>
        phase == "rph" && !(turn == 1 && phasing == state.FirstSide)
        && (state.Night || state.Weather("overcast") || state.Weather("gusty") || state.Precipitation is not null
            || state.Weather("rain") || state.Weather("heavy-rain") || state.Weather("falling-snow"));

    /// <summary>The Wind Change DR's events after the phase change (B25.65, E1.12, E3.4, E3.51, E3.71; rulings R16.1, R16.10).</summary>
    private void AddWindChange(GameScope scope, string attemptId, long expected, string actor, GameState state, List<GameEvent> events, List<string> reasons,
        Func<RollRequest, RollResult> draw, string changed)
    {
        string Roll(int count, string purpose)
        {
            var drawn = draw(new RollRequest(count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), null, null, [changed]));
            return rollId;
        }

        var rollId = Roll(2, "wind-change");
        var dice = ((DiceRolled)events[^1].Payload).Values;
        var (colored, white, dr) = (dice[0], dice[1], dice[0] + dice[1]);
        var notes = new List<string>();

        // E1.12: a colored 6 changes the Base NVR by one hex; with Scattered clouds and a Half or Full Moon, by a further dr / 3 or / 2 (FRU).
        int? nvr = state.Nvr;
        string? nvrRoll = null;
        if (state.Nvr is { } before && colored == 6)
        {
            var direction = white <= 3 ? -1 : white >= 5 || !state.StarshellUsed ? 1 : 0;
            var step = 1;
            var moon = state.SpecialRules.Contains("night-moon:half", StringComparer.Ordinal) ? 3
                : state.SpecialRules.Contains("night-moon:full", StringComparer.Ordinal) ? 2 : 0;
            if (direction != 0 && moon != 0 && state.SpecialRules.Contains("night-clouds:scattered", StringComparer.Ordinal))
            {
                nvrRoll = Roll(1, "nvr-change");
                step = (((DiceRolled)events[^1].Payload).Values[0] + moon - 1) / moon;
            }

            var (min, max) = NvrLimits(state);
            nvr = Math.Clamp(before + direction * step, Math.Min(min, before), Math.Max(max, before));
            notes.Add(nvr == before ? "the Base NVR stays " + before.ToString(CultureInfo.InvariantCulture)
                : $"the Base NVR goes from {before} to {nvr}");
        }

        // E3.51, E3.71: with Overcast (rain) or Falling Snow, 10 or more starts or intensifies the precipitation, 3 or less ends it.
        var precipitation = state.Precipitation;
        var raining = state.Weather("overcast") || state.Weather("rain") || state.Weather("heavy-rain");
        var snowing = state.Weather("falling-snow");
        if (raining || snowing)
        {
            var (light, heavy) = snowing ? ("snow", "heavy-snow") : ("rain", "heavy-rain");
            precipitation = dr >= 10 ? (precipitation is null ? light : heavy) : dr <= 3 ? null : precipitation;
            if (precipitation != state.Precipitation)
            {
                notes.Add(precipitation is null ? $"the {(snowing ? "snowfall" : "rain")} stops" : $"{precipitation.Replace('-', ' ')} falls");
            }
        }

        // E3.4: a Gust on 10 or more in Gusty weather.
        var gust = state.Weather("gusty") && dr >= 10;
        if (gust)
        {
            notes.Add("a Gust blows");
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "wind-changed", new WindChanged(rollId, nvr, precipitation, gust, nvrRoll), null, null, [changed]));
        reasons.Add($"play.wind-change: the Wind Change DR is {colored} + {white} = {dr}: {(notes.Count == 0 ? "no change" : string.Join("; ", notes))} (B25.65, E1.12, E3.51)");
    }
}
