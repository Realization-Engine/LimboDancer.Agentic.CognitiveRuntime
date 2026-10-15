using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationPlacementIssue(string UnitId, string Reason);
public sealed record SituationDeploymentDraft(string Policy, string WorldIdentity, string Catalog,
    FormationSelection[] Formations, SituationUnitPosition[] Positions, SituationPlacementIssue[] Review)
{
    public bool Ready => Review.Length == 0;
}

/// <summary>Campaign setup assembly. Existing ASL setup validation is required before handoff.</summary>
public sealed class SituationDeployment
{
    public const string Policy = "goodwood-situation-deployment-1";
    private readonly SituationMap map;
    private readonly SituationAddresses addresses;
    private readonly SituationRosterSnapshot roster;
    private readonly Dictionary<string, PilotUnit> units;
    private readonly Dictionary<string, SituationAddress[]> parents;

    public SituationDeployment(PilotInputs inputs, SituationMap map)
    {
        this.map = map;
        addresses = new(inputs, map.Manifest.Seed);
        roster = SituationRoster.Create(inputs);
        units = roster.Ground.SelectMany(f => f.Units).ToDictionary(u => u.Id, StringComparer.Ordinal);
        parents = addresses.Cells.GroupBy(c => c.Parent).ToDictionary(g => g.Key,
            g => g.OrderBy(c => c.Column).ThenBy(c => c.Row).ToArray(), StringComparer.Ordinal);
    }

    public SituationDeploymentDraft Assemble(IEnumerable<FormationSelection> formations,
        IEnumerable<SituationUnitPosition>? preserved = null, CancellationToken cancellationToken = default)
        => Build(formations, preserved ?? [], true, cancellationToken);

    // Recheck a saved draft without filling gaps or relocating an accepted counter.
    public SituationDeploymentDraft Review(SituationDeploymentDraft draft, CancellationToken cancellationToken = default)
    {
        if (draft.Policy != Policy || draft.WorldIdentity != map.Manifest.WorldIdentity || draft.Catalog != PilotCatalog.Identity)
            throw new InvalidDataException("Situation deployment policy, world or catalog changed. Retain the saved deployment for review.");
        return Build(draft.Formations, draft.Positions, false, cancellationToken);
    }

    private SituationDeploymentDraft Build(IEnumerable<FormationSelection> formations,
        IEnumerable<SituationUnitPosition> preserved, bool fill, CancellationToken cancellationToken)
    {
        var anchors = new Dictionary<string, string>(StringComparer.Ordinal);
        var sourceIds = roster.Ground.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var formation in formations)
            if (formation is null || !sourceIds.Contains(formation.UnitId) || !parents.ContainsKey(formation.Anchor) ||
                !anchors.TryAdd(formation.UnitId, formation.Anchor))
                throw new InvalidDataException("Unknown, duplicate or off-map ground formation anchor.");
        var positions = new Dictionary<string, SituationUnitPosition>(StringComparer.Ordinal);
        foreach (var position in preserved)
        {
            if (position is null || !units.ContainsKey(position.UnitId) || !positions.TryAdd(position.UnitId, position))
                throw new InvalidDataException("Unknown or duplicate preserved squad identity.");
            _ = addresses.At(position.Column, position.Row);
        }
        // Preserved coordinates, even those needing review, reserve their cells before any new assignment.
        foreach (var formation in roster.Ground)
        foreach (var unit in formation.Units.Where(u => u.Holding is null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var group = formation.Units.Where(u => u.Id == unit.Id || u.Holding?.Holder == unit.Id).ToArray();
            if (!fill || !anchors.TryGetValue(unit.ParentId, out var anchor)) continue;
            var retained = group.Where(u => positions.ContainsKey(u.Id)).Select(u => positions[u.Id]).ToArray();
            if (retained.Length == group.Length) continue;
            if (retained.Length > 0)
            {
                // A partial crew/weapon group can only be completed at its accepted position.
                // Conflicting or invalid accepted positions remain review findings, never relocation.
                var at = addresses.At(retained[0].Column, retained[0].Row);
                if (retained.All(p => p.Column == at.Column && p.Row == at.Row) &&
                    group.All(u => Rejection(u, at, anchors, positions, cancellationToken) is null))
                    foreach (var member in group.Where(u => !positions.ContainsKey(u.Id)))
                        positions.Add(member.Id, new(member.Id, at.Column, at.Row));
                continue;
            }

            var candidates = parents[anchor];
            var target = candidates.FirstOrDefault(c => !positions.Values.Any(p => p.Column == c.Column && p.Row == c.Row) &&
                    group.All(u => Rejection(u, c, anchors, positions, cancellationToken) is null))
                ?? candidates.FirstOrDefault(c => group.All(u => Rejection(u, c, anchors, positions, cancellationToken) is null));
            if (target is null) continue;
            foreach (var member in group) positions.Add(member.Id, new(member.Id, target.Column, target.Row));
        }
        var issues = new List<SituationPlacementIssue>();
        foreach (var unit in units.Values.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!positions.TryGetValue(unit.Id, out var position))
            {
                issues.Add(new(unit.Id, anchors.ContainsKey(unit.ParentId)
                    ? "No admitted squad position. Review terrain or available capacity inside the formation hex."
                    : "Place the parent formation on the campaign map."));
                continue;
            }
            var rejection = Rejection(unit, addresses.At(position.Column, position.Row), anchors, positions, cancellationToken);
            if (rejection is not null) issues.Add(new(unit.Id, rejection));
            if (unit.Holding is { } link && (!positions.TryGetValue(link.Holder, out var crew) ||
                crew.Column != position.Column || crew.Row != position.Row))
                issues.Add(new(unit.Id, "Weapon and assigned crew must share a position."));
            if (unit.Holding is null && units.Values.Any(u => u.Holding?.Holder == unit.Id && !positions.ContainsKey(u.Id)))
                issues.Add(new(unit.Id, "Assigned weapon has no position. Preserve the crew and review the missing weapon."));
        }
        return new(Policy, map.Manifest.WorldIdentity, PilotCatalog.Identity,
            anchors.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new FormationSelection(p.Key, p.Value)).ToArray(),
            positions.Values.OrderBy(p => p.UnitId, StringComparer.Ordinal).ToArray(), issues.ToArray());
    }

    private string? Rejection(PilotUnit unit, SituationAddress at, Dictionary<string, string> anchors,
        Dictionary<string, SituationUnitPosition> positions, CancellationToken cancellationToken)
    {
        if (!anchors.TryGetValue(unit.ParentId, out var anchor) || at.Parent != anchor)
            return "Squad position differs from its parent formation hex. The saved position was retained.";
        var facts = map.Tile(at.Tile, cancellationToken).Facts[HexName.Parse(at.Hex)];
        if (TerrainRejection(unit, facts) is { } terrain) return terrain;
        var others = positions.Values.Where(p => p.UnitId != unit.Id && p.Column == at.Column && p.Row == at.Row)
            .Select(p => units[p.UnitId]).ToArray();
        if (others.Any(u => u.Side != unit.Side)) return "Opposing counters share a setup hex.";
        if (others.Length > 0 && (unit.Kind == "vehicle" || others.Any(u => u.Kind == "vehicle")))
            return "Vehicle setup requires an unoccupied hex.";
        var people = others.Count(u => u.Holding is null) + (unit.Holding is null ? 1 : 0);
        return people > 2 ? "Campaign setup permits two personnel counters per hex; manned weapons remain with their crew." : null;
    }

    private static string? TerrainRejection(PilotUnit unit, HexFacts facts)
    {
        if (facts.BaseLevel is < 0 or > 2 || facts.Bridge is not null || facts.Center.Terrain?.Code is not (0 or 40 or 60 or 66))
            return "This terrain or bridge location needs setup review.";
        if (unit.Kind is "vehicle" or "gun" or "crew" or "mortar" && facts.Center.Terrain?.Name is not ("Open Ground" or "Paved Road" or "Woods"))
            return "Vehicle and weapon detachments currently require open ground, paved road or woods for campaign setup.";
        return null;
    }
}
