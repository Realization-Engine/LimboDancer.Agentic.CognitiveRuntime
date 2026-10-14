using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Campaign.Refinement;

// Campaign owns formation legality, including its supply exception. The adapter snapshots it;
// this module separately admits tactical locations and validates every placement.
public sealed record DeploymentContext(string CampaignId, string FormationSignature, string[] AlliedParents, string[] GermanParents);
public sealed record PilotUnit(string Id, string ParentId, string Side, string Label);
public sealed record PilotPlacement(string UnitId, string Hex);
public sealed record PilotSetup(string Policy, string ContextHash, string PackageVersion, int Revision, bool Complete, PilotPlacement[] Placements);
public sealed record PilotBundle(string Seed, string PackageVersion, string Board, string Name, string Features, string Manifest, string CatalogSha256, string GridSha256);
public sealed record PilotCell(string Hex, string GlobalKey, string? Parent, double X, double Y, PixelPoint[] Polygon, string Terrain, bool Admitted, string Reason);

/// <summary>Fixed-crop infantry setup policy. This is not an ASL movement or combat implementation.</summary>
public sealed class PilotDeployment
{
    public const string Policy = "goodwood-infantry-setup-1";
    public static IReadOnlyList<PilotUnit> Roster { get; } = Array.AsReadOnly(
        Enumerable.Range(1, 4).Select(i => new PilotUnit("uk-rifle-01/squad-" + i, "uk-rifle-01", "Allied", "British rifle squad " + i))
        .Concat(Enumerable.Range(1, 4).Select(i => new PilotUnit("de-rifle-01/squad-" + i, "de-rifle-01", "German", "German rifle squad " + i))).ToArray());
    private readonly PilotInputs inputs;
    public BoardPackage Package { get; }
    public TerrainGrid Grid { get; }
    public HexFactSet Facts { get; }
    public PilotBundle Bundle { get; }
    public IReadOnlyList<PilotCell> Cells { get; }

    public PilotDeployment(PilotInputs inputs, PilotResult result, string seed)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(result);
        this.inputs = inputs;
        Package = result.Package; Grid = result.Grid; Facts = result.Facts;
        Bundle = new(seed, Package.Version, Package.Board.Value, Package.Name, Encoding.UTF8.GetString(Package.FeaturesBytes()),
            Encoding.UTF8.GetString(result.Manifest), inputs.Profile.CatalogSha256, PilotGenerator.GridHash(Grid));
        CheckIdentity(); Cells = Describe();
    }

    // Load accepted features, never regenerate terrain when opening a campaign save.
    public PilotDeployment(PilotInputs inputs, PilotBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(bundle);
        this.inputs = inputs; Bundle = bundle;
        SeedRandom.ValidateSeed(bundle.Seed);
        if (bundle.Features is null || bundle.Manifest is null || bundle.Features.Length > 1_000_000 || bundle.Manifest.Length > 2_000_000)
            throw new InvalidDataException("Invalid pilot package size.");
        var model = FeatureModelJson.Deserialize(Encoding.UTF8.GetBytes(bundle.Features));
        var expected = inputs.DefaultCrop.Geometry(); var g = model.Geometry;
        if (model.Features.Count > 500 || g.WidthInHexes != expected.WidthInHexes || g.HeightInHexes != expected.HeightInHexes ||
            g.GridWidth != expected.GridWidth || g.GridHeight != expected.GridHeight || g.HexWidth != expected.HexWidth ||
            g.HexHeight != expected.HexHeight || g.A1CenterX != expected.A1CenterX || g.A1CenterY != expected.A1CenterY ||
            g.ColumnLetterOffset != 0 || g.RowNumberOffset != 0 || model.CatalogHash != inputs.CatalogBlob)
            throw new InvalidDataException("Only the pinned Goodwood crop and catalog are admitted.");
        Package = new(BoardRef.Parse(bundle.Board), bundle.Name, model);
        CheckIdentity();
        Grid = FeatureCompiler.Compile(model, inputs.Catalog).Grid;
        if (PilotGenerator.GridHash(Grid) != bundle.GridSha256) throw new InvalidDataException("Compiled terrain hash differs from the accepted save.");
        Facts = VaslCompatibleHexFactDerivation.Derive(Grid, inputs.Catalog, HexsideAnnotations.None);
        if (BoardValidator.Validate(model, Facts, inputs.Catalog).HasErrors) throw new InvalidDataException("Saved terrain failed validation.");
        Cells = Describe();
    }

    private void CheckIdentity()
    {
        using var doc = JsonDocument.Parse(Bundle.Manifest); var m = doc.RootElement;
        if (Package.Board.Value != PilotIdentity.Reference(inputs, Bundle.Seed, inputs.DefaultCrop) ||
            m.GetProperty("WorldIdentity").GetString() != PilotIdentity.World(inputs, Bundle.Seed) ||
            m.GetProperty("SpatialSha256").GetString() != PilotIdentity.SpatialDigest ||
            Package.Version != Bundle.PackageVersion || Bundle.CatalogSha256 != inputs.Profile.CatalogSha256 ||
            m.GetProperty("Seed").GetString() != Bundle.Seed || m.GetProperty("PackageVersion").GetString() != Package.Version ||
            m.GetProperty("PackageReference").GetString() != Package.Board.Value ||
            m.GetProperty("FeaturesSha256").GetString() != BoardPackage.Hash(Package.FeaturesBytes()) ||
            m.GetProperty("GridSha256").GetString() != Bundle.GridSha256 ||
            m.GetProperty("SourceSha256").GetString() != inputs.Profile.SourceSha256 ||
            m.GetProperty("ProfileSha256").GetString() != inputs.ProfileDigest ||
            m.GetProperty("MapSpace").GetString() != PilotInputs.MapSpace ||
            m.GetProperty("CatalogSha256").GetString() != inputs.Profile.CatalogSha256 ||
            m.GetProperty("CatalogBlob").GetString() != inputs.CatalogBlob ||
            m.GetProperty("SerializationVersion").GetString() != "feature-model-1" ||
            m.GetProperty("GeneratorVersion").GetString() != PilotInputs.GeneratorVersion ||
            m.GetProperty("SpatialVersion").GetString() != SpatialFrame.Version ||
            m.GetProperty("CompilerVersion").GetString() != FeatureCompiler.Version ||
            m.GetProperty("DerivationVersion").GetString() != VaslCompatibleHexFactDerivation.Version ||
            m.GetProperty("Crop").Deserialize<Crop>() != inputs.DefaultCrop)
            throw new InvalidDataException("Pilot package identity or supported version does not match the save.");
    }

    private PilotCell[] Describe() => Facts.Hexes.Select(f =>
    {
        var h = f.Index; var g = Facts.Geometry; var crop = inputs.DefaultCrop;
        var world = SpatialFrame.Center(crop.Column + h.Column, crop.Row + h.Row);
        var parent = inputs.Regions.FirstOrDefault(r => MetricGeometry.Contains(r.Polygon, world))?.Key;
        // A two-hex guard avoids clamped edge facts. Overlapping playable crops are not admitted.
        var interior = h.Column >= 2 && h.Column < crop.Width - 2 && h.Row >= 2 && h.Row < crop.Height - 2;
        var code = f.Center.Terrain?.Code;
        var ground = f.BaseLevel == 0 && code is 0 or 40 or 60 or 66;
        // A bridge deck is a distinct location. Version 1 does not place infantry on decks/streams.
        var reason = !interior ? "Crop boundary: inspection only." : parent is null ? "No source formation parent." :
            !ground || f.Bridge is not null ? "This terrain or bridge location is not admitted for pilot setup." : "Infantry ground-level setup admitted.";
        var p = g.CenterDot(h);
        return new PilotCell(f.Hex.ToString(), (crop.Column + h.Column) + "/" + (crop.Row + h.Row), parent,
            p.X, p.Y, g.Vertices(h).ToArray(), f.Center.Terrain?.Name ?? "Unknown", interior && parent is not null && ground && f.Bridge is null, reason);
    }).ToArray();

    public string ContextHash(DeploymentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.CampaignId) || context.CampaignId.Length > 100 ||
            string.IsNullOrWhiteSpace(context.FormationSignature) || context.FormationSignature.Length > 200000 ||
            context.AlliedParents is null || context.GermanParents is null)
            throw new InvalidDataException("Campaign setup context is missing.");
        var known = inputs.Regions.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
        if (context.AlliedParents.Length > known.Count || context.GermanParents.Length > known.Count ||
            context.AlliedParents.Concat(context.GermanParents).Any(p => !known.Contains(p)))
            throw new InvalidDataException("Campaign setup references an unknown formation hex.");
        return BoardPackage.Hash(CanonicalJson.Serialize(new object[] { Policy, context.CampaignId, context.FormationSignature,
            context.AlliedParents.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            context.GermanParents.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() }));
    }

    public PilotSetup Create(DeploymentContext context)
    {
        var state = new PilotSetup(Policy, ContextHash(context), Package.Version, 0, false, []);
        // Capacity is checked against this seed and campaign policy before admitting the workflow.
        _ = AutoDeploy(context, state); return state;
    }

    public void Validate(DeploymentContext context, PilotSetup state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Policy != Policy || state.PackageVersion != Package.Version || state.ContextHash != ContextHash(context))
            throw new InvalidDataException("Formation setup or supply rules changed. Keep the saved pilot; restore its formation context before reopening it.");
        if (state.Revision < 0 || state.Placements is null || state.Placements.Length > Roster.Count || state.Placements.Any(p => p is null) ||
            state.Placements.Select(p => p.UnitId).Distinct(StringComparer.Ordinal).Count() != state.Placements.Length)
            throw new InvalidDataException("Invalid pilot roster or duplicate placement.");
        foreach (var p in state.Placements)
        {
            var reason = Rejection(context, state, p.UnitId, p.Hex);
            if (reason is not null) throw new InvalidDataException(reason);
        }
        if (state.Complete && state.Placements.Length != Roster.Count) throw new InvalidDataException("Deploy all eight pilot squads before completing setup.");
    }

    public string? Rejection(DeploymentContext context, PilotSetup state, string unitId, string hex)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(state);
        var unit = Roster.FirstOrDefault(u => u.Id == unitId); var cell = Cells.FirstOrDefault(c => c.Hex == hex);
        if (unit is null) return "Unknown pilot squad.";
        if (cell is null) return "Choose a hex in the pilot map.";
        if (!cell.Admitted) return cell.Reason;
        var allowed = unit.Side == "Allied" ? context.AlliedParents : context.GermanParents;
        if (!allowed.Contains(cell.Parent, StringComparer.Ordinal)) return unit.Side + " setup is not allowed in formation hex " + cell.Parent + ".";
        var occupants = state.Placements.Where(p => p.UnitId != unitId && p.Hex == hex).ToArray();
        if (occupants.Any(p => Roster.FirstOrDefault(u => u.Id == p.UnitId)?.Side != unit.Side)) return "Opposing squads cannot share a setup hex.";
        return occupants.Length >= 2 ? "Stacking limit: two pilot squads per hex." : null;
    }

    public PilotSetup Move(DeploymentContext context, PilotSetup state, string unitId, string hex)
    {
        Validate(context, state);
        if (state.Complete) throw new InvalidDataException("Setup is complete. Select Review setup before repositioning squads.");
        var reason = Rejection(context, state, unitId, hex);
        if (reason is not null) throw new InvalidDataException(reason);
        return state with { Revision = checked(state.Revision + 1), Placements = [.. state.Placements.Where(p => p.UnitId != unitId), new(unitId, hex)] };
    }

    public PilotSetup AutoDeploy(DeploymentContext context, PilotSetup state)
    {
        Validate(context, state); var next = state;
        foreach (var unit in Roster.Where(u => !state.Placements.Any(p => p.UnitId == u.Id)))
        {
            var cell = Cells.FirstOrDefault(c => !next.Placements.Any(p => p.Hex == c.Hex) && Rejection(context, next, unit.Id, c.Hex) is null)
                ?? Cells.FirstOrDefault(c => Rejection(context, next, unit.Id, c.Hex) is null);
            if (cell is null) throw new InvalidDataException("Insufficient legal setup capacity for " + unit.Side + ". No deployment was changed.");
            next = Move(context, next, unit.Id, cell.Hex);
        }
        return next;
    }

    public PilotSetup SetComplete(DeploymentContext context, PilotSetup state, bool complete)
    {
        Validate(context, state); var next = state with { Complete = complete, Revision = checked(state.Revision + 1) };
        Validate(context, next); return next;
    }
}
