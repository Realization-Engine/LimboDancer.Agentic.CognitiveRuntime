using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Games;

/// <summary>How the Game states page and its components word phases, events, and a case read.</summary>
public static class GameText
{
    public static string PhaseLabel(string phase) => phase switch
    {
        "rph" => "Rally Phase",
        "pfph" => "Prep Fire Phase",
        "mph" => "Movement Phase",
        "dfph" => "Defensive Fire Phase",
        "afph" => "Advancing Fire Phase",
        "rtph" => "Rout Phase",
        "aph" => "Advance Phase",
        "ccph" => "Close Combat Phase",
        _ => phase,
    };

    private static string Plain(string id) => id.Replace("asl:", string.Empty, StringComparison.Ordinal);

    public static string EventText(GameEvent item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Payload switch
        {
            GameStarted started => $": {string.Join(" v ", started.Sides.Select(side => side.Id))} on {started.Map.Reference}",
            PhaseChanged phase => $": turn {phase.Turn}, {PhaseLabel(phase.Phase)}",
            InstanceCreated created => $": {created.Instance.Id} ({created.Instance.Kind})",
            InstanceMoved moved => $": {moved.Id} to {moved.Position}",
            ConditionsChanged changed => $": {changed.Id} {string.Join(", ", changed.Conditions.Select(pair => $"{Plain(pair.Key)} {Conditions.Name(pair.Value)}"))}",
            LineageRecorded lineage => $": {lineage.Action.ToString().ToLowerInvariant()} {string.Join(", ", lineage.Consumed)} into {string.Join(", ", lineage.Produced.Select(produced => produced.Id))}",
            InstanceCaptured captured => $": {captured.Id} by {captured.Custodian}",
            InstanceEliminated eliminated => $": {eliminated.Id}",
            EquipmentTransferred transferred => $": {transferred.Id}",
            _ => string.Empty,
        };
    }

    public static string LocationReadText(LocationRead read)
    {
        ArgumentNullException.ThrowIfNull(read);
        return $"{read.Location}: {read.Level.Terrain?.Name ?? "no terrain"}, level {read.Level.Level}; board version {read.BoardVersion[..Math.Min(12, read.BoardVersion.Length)]}, {read.Status}";
    }

    public static string SoleText(CaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var (unit, reason) = snapshot.Occupancy.SoleEnemy(snapshot.Attacker.Unit.Side);
        return unit is not null ? $"{unit.Id}, definitively" : $"not definitive: {reason}";
    }

    public static string OccupantsText(CaseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return string.Join(", ", snapshot.Occupancy.Units.Select(unit => $"{unit.Id} ({unit.Side} {Plain(unit.Kind)})")
            .Concat(snapshot.Occupancy.Sealed.Select(presence => $"{presence.PlacementId} (concealed {presence.Side})"))
            .Concat(snapshot.Occupancy.Entities.Select(entity => $"{entity.Id} ({Plain(entity.Kind)})")).DefaultIfEmpty("none seen"));
    }
}
