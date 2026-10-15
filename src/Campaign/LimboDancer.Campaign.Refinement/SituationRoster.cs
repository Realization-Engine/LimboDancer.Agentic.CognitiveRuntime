using System.Text.Json;

namespace LimboDancer.Campaign.Refinement;

// Aircraft are retained as off-map reference records, never invented ASL ground instances.
public sealed record SituationAircraft(string Id, string SourceType, string Side, string Group,
    string Role, int AvailableFromTurn);
public sealed record SituationAirSupport(string Status, string Instructions, int MaxConcurrentAircraft,
    SituationAircraft[] Aircraft);
public sealed record SituationFormation(string Id, string SourceType, string Policy, PilotUnit[] Units);
public sealed record SituationRosterSnapshot(string Version, string SourceSha256,
    SituationFormation[] Ground, SituationAirSupport AirSupport);

/// <summary>Complete source accounting, independent of terrain generation and deployment positions.</summary>
public static class SituationRoster
{
    public const string Version = "goodwood-situation-roster-1";

    public static SituationRosterSnapshot Create(PilotInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var air = inputs.AirSupport;
        var aircraftIds = air.Aircraft.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);
        if (aircraftIds.Count != air.Aircraft.Length || air.MaxConcurrentAircraft < 1 ||
            air.Aircraft.Any(a => a.AvailableFromTurn < 1 ||
                !inputs.FormationTypes.TryGetValue(a.Id, out var type) || type != a.SourceType || type != "uk-typhoon-rockets"))
            throw new InvalidDataException("Invalid Goodwood off-map aircraft accounting.");
        var ground = inputs.FormationTypes.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Where(p => !aircraftIds.Contains(p.Key)).Select(p =>
            {
                // Composition does not require a placement anchor. Unsupported source types fail closed.
                var units = FormationDeployment.Units(inputs, new(p.Key, string.Empty)).ToArray();
                return new SituationFormation(p.Key, p.Value, FormationDeployment.PolicyFor(units), units);
            }).ToArray();
        var children = ground.SelectMany(f => f.Units).ToArray();
        if (ground.Length + aircraftIds.Count != inputs.FormationTypes.Count ||
            children.Select(u => u.Id).Distinct(StringComparer.Ordinal).Count() != children.Length ||
            ground.Any(f => f.Units.Length == 0 || f.Units.Any(u => u.ParentId != f.Id || u.Definition is null)))
            throw new InvalidDataException("Incomplete Goodwood ground formation accounting.");
        return new(Version, inputs.Profile.SourceSha256, ground, air);
    }

    // A catalog or composition change requires an explicit migration, never silent replacement on reload.
    public static void Validate(PilotInputs inputs, SituationRosterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!JsonSerializer.SerializeToUtf8Bytes(Create(inputs)).AsSpan()
            .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(snapshot)))
            throw new InvalidDataException("Situation roster differs from the supported source, catalog or composition policy.");
    }
}
