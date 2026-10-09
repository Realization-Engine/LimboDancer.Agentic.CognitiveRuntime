using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Night and weather (backlog pass 16, rulings R16.1 to R16.14): the SSRs that declare them, what a unit sees at night (its NVR, Illumination, and
/// Gunflashes), the Low Visibility DRM and the other facts they give an attack, the Wind Change DR at the start of each RPh, and Starshells. The rules
/// are <see cref="ScenarioA1NightAndWeather"/>'s (pass 32.h); this file reads the state and the map and writes the events.
/// </summary>
public sealed partial class GamePlanner
{
    // The weather, sky, and Axis lists (E3, E1.11, E3.741), moved to Rules (pass 32.a).
    private static readonly IReadOnlyList<string> WeatherKinds = ScenarioA1Definitions.WeatherKinds;
    private static readonly IReadOnlyList<string> Clouds = ScenarioA1Definitions.Clouds;
    private static readonly IReadOnlyList<string> Moons = ScenarioA1Definitions.Moons;
    private static readonly IReadOnlyList<string> Axis = ScenarioA1Definitions.AxisNationalities;

    /// <summary>Why the SSRs of a new game are refused (rulings R16.1, R16.9, and R23.5 for HIP), or null; the request's parsing here, the rules in Rules.</summary>
    internal static string? NightAndWeatherRulesBar(JsonElement start)
    {
        string[] rules = start.TryGetProperty("specialRules", out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : [];
        return ScenarioA1NightAndWeather.RulesBar(rules, (rule, side) => ScenarioSetup.HipAllowance([rule], side) is not null,
            start.TryGetProperty("scenarioMonth", out var month) && month.ValueKind == JsonValueKind.Number,
            start.TryGetProperty("scenarioYear", out var year) && year.ValueKind == JsonValueKind.Number);
    }

    /// <summary>Whether a terrain is Concealment Terrain (A12.12, as the concealment gain reads it): grain only June to September.</summary>
    private static bool ConcealmentTerrain(string terrain, int? month) => ScenarioA1Definitions.IsConcealmentTerrain(terrain, month);

    /// <summary>Whether rain has fallen, as the SSRs name it or the Wind Change DR started it (E3.54; ruling R16.12).</summary>
    private static bool Rain(GameState state) => ScenarioA1NightAndWeather.Rain(state.Rained, state.Precipitation);

    /// <summary>An Infantry step's night and weather MF, in half MF, and whether it keeps the road rate (rulings R16.5, R16.12, R16.13); Rules decides (pass 32.h).</summary>
    private static (int HalfMf, bool RoadRate) InfantryWeatherHalfMf(GameState state, Maps.Derivation.HexsideFacts crossed, string terrain, bool road, int rise) =>
        ScenarioA1NightAndWeather.InfantryWeatherHalfMf(state.Night, Rain(state), state.Weather("mud"), state.Weather("ground-snow"), state.Weather("deep-snow"),
            state.SpecialRules.Contains("plowed-roads", StringComparer.Ordinal), state.ScenarioMonth, crossed.Terrain?.Name == "Paved Road", terrain, road, rise);

    /// <summary>A vehicle's night and weather MP, in half MP (rulings R16.5, R16.12, R16.13); Rules decides (pass 32.h).</summary>
    private static int VehicleWeatherHalfMp(GameState state, string type, string terrain, bool road, bool paved, bool plowed, int rise) =>
        ScenarioA1NightAndWeather.VehicleWeatherHalfMp(state.Night, Rain(state), state.Weather("mud"), state.Weather("ground-snow"), state.Weather("deep-snow"), type, terrain,
            paved, plowed, rise);

    /// <summary>The Base NVR's limits (E1.12, E1.15; ruling R16.1): 0 to 6, or 2 to 9 with Ground or Deep Snow.</summary>
    private static (int Min, int Max) NvrLimits(GameState state) =>
        ScenarioA1NightAndWeather.NvrLimits(state.Weather("ground-snow") || state.Weather("deep-snow"));

    /// <summary>A viewer's NVR at night (E1.1, E1.14; ruling R16.2): the Base NVR, halved (FRD) from a BU AFV; null by day.</summary>
    private static int? NvrOf(GameState state, UnitInstance? viewer) =>
        ScenarioA1NightAndWeather.NvrOf(state.Nvr, viewer is not null && LiveFire.IsVehicle(viewer) && Is(viewer, Conditions.ButtonedUp));

    /// <summary>
    /// Whether a Location is Illuminated at night (E1.9, E1.923, E1.94; ruling R16.8): within three hexes of a Starshell, or within two hexes of a Blaze
    /// (a burning wreck); the distances are read as Rules asks for them.
    /// </summary>
    private bool Illuminated(GameState state, BoardLocation location)
    {
        var starshells = state.Entities.Where(entity => entity.Status == InstanceStatus.Active && entity.Kind == "asl:starshell" && entity.Position is MapPosition)
            .Select(entity => ((MapPosition)entity.Position).Location);
        var blazes = state.Units.Where(unit => unit.Status == InstanceStatus.Wrecked && IsBurning(state, unit) && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location);
        return ScenarioA1NightAndWeather.Illuminated(state.Night, starshells.Select(at => HexDistance(state, at, location)), blazes.Select(at => HexDistance(state, at, location)));
    }

    /// <summary>Whether a Location is marked by a Gunflash (E1.8; ruling R16.2): a fire counter of any kind or a Melee among its active occupants.</summary>
    private static bool Gunflash(GameState state, BoardLocation location) =>
        ScenarioA1NightAndWeather.Gunflash(state.At(location).Where(item => item.Status is InstanceStatus.Active)
            .Select(item => (Func<UnitCondition, bool>)(marker => GameState.Condition(item, ConditionName(marker)) == ConditionState.True)));

    /// <summary>
    /// What a viewer at <paramref name="from"/> sees at night of a Location <paramref name="range"/> hexes away (E1.101, E1.13, E1.14, E1.81; ruling
    /// R16.2); Rules decides over the Illumination, Gunflash, and Motion vehicle reads (pass 32.h).
    /// </summary>
    private (bool BeyondNvr, string? Reason) NightSight(GameState state, UnitInstance? viewer, BoardLocation from, BoardLocation target, int range,
        IEnumerable<UnitInstance> targets)
    {
        var verdict = ScenarioA1NightAndWeather.NightSight(NvrOf(state, viewer), from.ToString(), target.ToString(), range, () => Illuminated(state, target), () => Gunflash(state, target),
            () => Illuminated(state, from),
            targets.Where(unit => LiveFire.IsVehicle(unit) && Is(unit, Conditions.Motion))
                .Select(unit => VehicleDefinition(unit) is { } definition ? definition.MovementType ?? string.Empty : null));
        return (verdict.BeyondNvr, verdict.Reason);
    }

    /// <summary>
    /// The Extreme Winter reduction of a side's B# and X# (E3.741; ruling R16.14): 1 for Russians before April 1941, 2 for Axis but Finns before
    /// April 1942; null otherwise.
    /// </summary>
    internal static int? ExtremeWinterReduction(GameState state, string? side) =>
        ScenarioA1NightAndWeather.ExtremeWinterReduction(state.Weather("extreme-winter"), state.ScenarioYear, state.ScenarioMonth,
            state.Sides.FirstOrDefault(item => item.Id == side)?.Nationality);

    /// <summary>
    /// The night and weather facts of an attack (rulings R16.2, R16.3, R16.11 to R16.14): the viewer and its side are read here, each firing Location's
    /// sight when Rules asks, and Rules decides (pass 32.h).
    /// </summary>
    private (FireAttack? Facts, string? Reason) NightAndWeatherFacts(GameState state, FireAttack facts, BoardLocation target, int targetTop,
        IReadOnlyDictionary<string, (int Range, bool SameLevel, FireLos Los, int Height)> perLocation)
    {
        // E1.14 (referee, pass 16): a vehicle firing, its MA's shot included, sees with its own NVR.
        var viewer = facts.VehicleFire is { } byVehicle ? state.Unit(byVehicle.VehicleId!)
            : facts.Firers is { Count: > 0 } shooting && state.Unit(shooting[0].UnitId ?? string.Empty) is { } shooter && LiveFire.IsVehicle(shooter) ? shooter : null;
        var side = viewer?.Side ?? state.Unit((facts.Firers is { Count: > 0 } firing ? firing[0].UnitId : null) ?? string.Empty)?.Side;
        var locations = perLocation.ToArray();

        // E1.14 (table player, pass 16): the target Location's vehicles are read with its other targets.
        UnitInstance[]? targets = null;
        UnitInstance[] Targets() => targets ??= [.. (facts.Targets ?? []).Select(item => state.Unit(item.UnitId ?? string.Empty))
            .Concat((facts.Vehicles ?? []).Select(item => state.Unit(item.VehicleId ?? string.Empty))).OfType<UnitInstance>()];
        return ScenarioA1NightAndWeather.NightAndWeatherFacts(facts, ExtremeWinterReduction(state, side),
            state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Melee)),
            state.Weather("mud"), state.Weather("deep-snow"), state.Night, state.Weather("mist"), state.Precipitation, targetTop,
            [.. locations.Select(pair => new FiringLocationFacts(pair.Value.Range, pair.Value.Los.HindranceDrm, pair.Value.Height))],
            index =>
            {
                var (far, reason) = NightSight(state, viewer, BoardLocation.Parse(locations[index].Key), target, locations[index].Value.Range, Targets());
                return new NightSightVerdict(far, reason);
            });
    }

    /// <summary>The Wind Change DR at the start of a RPh (B25.65; rulings R16.1, R16.10); Rules decides (pass 32.h).</summary>
    private static bool WindChangeDue(GameState state, string phase, int turn, string phasing) =>
        ScenarioA1NightAndWeather.WindChangeDue(phase, turn, phasing, state.FirstSide, state.Night, state.Weather("overcast"), state.Weather("gusty"), state.Precipitation,
            state.Weather("rain"), state.Weather("heavy-rain"), state.Weather("falling-snow"));

    /// <summary>The Wind Change DR's events after the phase change (B25.65, E1.12, E3.4, E3.51, E3.71; rulings R16.1, R16.10); Rules decides the change (pass 32.h).</summary>
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
        string? nvrRoll = null;
        var verdict = ScenarioA1NightAndWeather.WindChange(colored, white, state.Nvr, state.StarshellUsed,
            state.SpecialRules.Contains("night-moon:half", StringComparer.Ordinal), state.SpecialRules.Contains("night-moon:full", StringComparer.Ordinal),
            state.SpecialRules.Contains("night-clouds:scattered", StringComparer.Ordinal), state.Weather("ground-snow") || state.Weather("deep-snow"),
            state.Weather("overcast"), state.Weather("rain"), state.Weather("heavy-rain"), state.Weather("falling-snow"), state.Weather("gusty"), state.Precipitation,
            () =>
            {
                nvrRoll = Roll(1, "nvr-change");
                return ((DiceRolled)events[^1].Payload).Values[0];
            });
        var notes = verdict.Notes;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "wind-changed", new WindChanged(rollId, verdict.Nvr, verdict.Precipitation, verdict.Gust, nvrRoll), null, null, [changed]));
        reasons.Add(ScenarioA1NightAndWeather.WindChangeText(colored, white, dr, notes));
    }
}
