using System.Diagnostics;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
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
public sealed record DeploymentContext(string CampaignId, string FormationSignature, string[] AlliedParents, string[] GermanParents, FormationSelection? Formation = null);
public sealed record PilotUnit(string Id, string ParentId, string Side, string Label, string Kind = "rifle", string Symbol = "R", DefinitionReference? Definition = null, string? CatalogStatus = null, Holding? Holding = null, string? AllotmentStatus = null);
public sealed record PilotEquipmentLink(string EquipmentId, Holding Holding);
public sealed record PilotPlacement(string UnitId, string Hex);
public sealed record PilotSetup(string Policy, string ContextHash, string PackageVersion, int Revision, bool Complete, PilotPlacement[] Placements, PilotEquipmentLink[]? Equipment = null, string? UnitCatalog = null);
public sealed record PilotBundle(string Seed, string PackageVersion, string Board, string Name, string Features, string Manifest, string CatalogSha256, string GridSha256);
public sealed record PilotCell(string Hex, string GlobalKey, string? Parent, double X, double Y, PixelPoint[] Polygon, string Terrain, bool Admitted, string Reason, int BaseLevel);

/// <summary>Fixed-crop infantry setup policy. This is not an ASL movement or combat implementation.</summary>
public sealed class PilotDeployment
{
    public const string Policy = "goodwood-infantry-setup-1";
    public static IReadOnlyList<PilotUnit> Roster { get; } = Array.AsReadOnly(
        Enumerable.Range(1, 4).Select(i => PilotCatalog.Infantry("uk-rifle-01/squad-" + i, "uk-rifle-01", "Allied", "British rifle squad " + i))
        .Concat(Enumerable.Range(1, 4).Select(i => PilotCatalog.Infantry("de-rifle-01/squad-" + i, "de-rifle-01", "German", "German rifle squad " + i))).ToArray());
    private readonly PilotInputs inputs;
    private readonly FormationSelection? formation;
    public Crop Crop { get; }
    public IReadOnlyList<PilotUnit> Units { get; }
    public PilotEquipmentLink[] Equipment => Units.Where(u => u.Holding is not null).Select(u => new PilotEquipmentLink(u.Id, u.Holding!)).ToArray();
    public string ActivePolicy => formation is null ? Policy : FormationDeployment.PolicyFor(Units);
    public BoardPackage Package { get; }
    public TerrainGrid Grid { get; }
    public HexFactSet Facts { get; }
    public PilotBundle Bundle { get; }
    public IReadOnlyList<PilotCell> Cells { get; }

    public PilotDeployment(PilotInputs inputs, PilotResult result, string seed, FormationSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(result);
        this.inputs = inputs;
        formation = selection;
        Crop = selection is null ? inputs.DefaultCrop : FormationDeployment.CropFor(inputs, selection);
        Units = selection is null ? Roster : FormationDeployment.Units(inputs, selection);
        Package = result.Package; Grid = result.Grid; Facts = result.Facts;
        Bundle = new(seed, Package.Version, Package.Board.Value, Package.Name, Encoding.UTF8.GetString(Package.FeaturesBytes()),
            Encoding.UTF8.GetString(result.Manifest), inputs.Profile.CatalogSha256, PilotGenerator.GridHash(Grid));
        CheckIdentity(); Cells = Describe();
    }

    // Load accepted features, never regenerate terrain when opening a campaign save.
    public PilotDeployment(PilotInputs inputs, PilotBundle bundle, IDictionary<string, double>? timings = null, FormationSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(bundle);
        using var versionDocument = JsonDocument.Parse(bundle.Manifest);
        inputs = inputs.ForVersion(versionDocument.RootElement.GetProperty("GeneratorVersion").GetString());
        this.inputs = inputs;
        formation = selection;
        Crop = selection is null ? inputs.DefaultCrop : FormationDeployment.CropFor(inputs, selection);
        Units = selection is null ? Roster : FormationDeployment.Units(inputs, selection); Bundle = bundle;
        var watch = Stopwatch.StartNew();
        void Stage(string name) { if (timings is not null) timings[name] = watch.Elapsed.TotalMilliseconds; watch.Restart(); }
        SeedRandom.ValidateSeed(bundle.Seed);
        if (bundle.Features is null || bundle.Manifest is null || bundle.Features.Length > 1_000_000 || bundle.Manifest.Length > 2_000_000)
            throw new InvalidDataException("Invalid pilot package size.");
        var model = FeatureModelJson.Deserialize(Encoding.UTF8.GetBytes(bundle.Features));
        var expected = Crop.Geometry(); var g = model.Geometry;
        if (model.Features.Count > 500 || g.WidthInHexes != expected.WidthInHexes || g.HeightInHexes != expected.HeightInHexes ||
            g.GridWidth != expected.GridWidth || g.GridHeight != expected.GridHeight || g.HexWidth != expected.HexWidth ||
            g.HexHeight != expected.HexHeight || g.A1CenterX != expected.A1CenterX || g.A1CenterY != expected.A1CenterY ||
            g.ColumnLetterOffset != 0 || g.RowNumberOffset != 0 || model.CatalogHash != inputs.CatalogBlob)
            throw new InvalidDataException("Only the selected Goodwood crop dimensions and pinned catalog are admitted.");
        Package = new(BoardRef.Parse(bundle.Board), bundle.Name, model);
        CheckIdentity();
        Stage("decodeAndIdentity");
        Grid = FeatureCompiler.Compile(model, inputs.Catalog).Grid;
        Stage("compile");
        if (PilotGenerator.GridHash(Grid) != bundle.GridSha256) throw new InvalidDataException("Compiled terrain hash differs from the accepted save.");
        Stage("gridHash");
        Facts = VaslCompatibleHexFactDerivation.Derive(Grid, inputs.Catalog, HexsideAnnotations.None);
        Stage("deriveFacts");
        if (BoardValidator.Validate(model, Facts, inputs.Catalog).HasErrors) throw new InvalidDataException("Saved terrain failed validation.");
        Stage("validateBoard");
        Cells = Describe();
        Stage("describeCells");
    }

    private void CheckIdentity()
    {
        using var doc = JsonDocument.Parse(Bundle.Manifest); var m = doc.RootElement;
        if (Package.Board.Value != PilotIdentity.Reference(inputs, Bundle.Seed, Crop) ||
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
            m.GetProperty("GeneratorVersion").GetString() != inputs.Version ||
            m.GetProperty("SpatialVersion").GetString() != SpatialFrame.Version ||
            m.GetProperty("CompilerVersion").GetString() != FeatureCompiler.Version ||
            m.GetProperty("DerivationVersion").GetString() != VaslCompatibleHexFactDerivation.Version ||
            m.GetProperty(nameof(Crop)).Deserialize<Crop>() != Crop)
            throw new InvalidDataException("Pilot package identity or supported version does not match the save.");
    }

    private PilotCell[] Describe()
    {
        // Preserve ordinal source order and exact boundary tests. Most source polygons cannot
        // contain a given center, so reject those using precomputed inclusive bounds first.
        var regions = inputs.Regions.Select(r => (Region: r,
            MinX: r.Polygon.Min(p => p.X), MaxX: r.Polygon.Max(p => p.X),
            MinY: r.Polygon.Min(p => p.Y), MaxY: r.Polygon.Max(p => p.Y))).ToArray();
        return Facts.Hexes.Select(f =>
    {
        var h = f.Index; var g = Facts.Geometry; var crop = Crop;
        var world = SpatialFrame.Center(crop.Column + h.Column, crop.Row + h.Row);
        var parent = regions.FirstOrDefault(r => world.X >= r.MinX && world.X <= r.MaxX &&
            world.Y >= r.MinY && world.Y <= r.MaxY && MetricGeometry.Contains(r.Region.Polygon, world)).Region?.Key;
        // A two-hex guard avoids clamped edge facts. Overlapping playable crops are not admitted.
        var interior = h.Column >= 2 && h.Column < crop.Width - 2 && h.Row >= 2 && h.Row < crop.Height - 2;
        var code = f.Center.Terrain?.Code;
        var ground = (inputs.SupportsElevation ? f.BaseLevel is >= 0 and <= 2 : f.BaseLevel == 0) && code is 0 or 40 or 60 or 66;
        // A bridge deck is a distinct location. Version 1 does not place infantry on decks/streams.
        var reason = !interior ? "Crop boundary: inspection only." : parent is null ? "No source formation parent." :
            !ground || f.Bridge is not null ? "This terrain or bridge location is not admitted for pilot setup." : "Infantry ground-level setup admitted.";
        var p = g.CenterDot(h);
        return new PilotCell(f.Hex.ToString(), (crop.Column + h.Column) + "/" + (crop.Row + h.Row), parent,
            p.X, p.Y, g.Vertices(h).ToArray(), f.Center.Terrain?.Name ?? "Unknown", interior && parent is not null && ground && f.Bridge is null, reason, f.BaseLevel);
    }).ToArray();
    }

    public string ContextHash(DeploymentContext context) => ContextHashFor(context, ActivePolicy);

    private string ContextHashFor(DeploymentContext context, string policy)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.CampaignId) || context.CampaignId.Length > 100 ||
            string.IsNullOrWhiteSpace(context.FormationSignature) || context.FormationSignature.Length > 200000 ||
            context.AlliedParents is null || context.GermanParents is null)
            throw new InvalidDataException("Campaign setup context is missing.");
        if (context.Formation != formation) throw new InvalidDataException("Selected formation or anchor differs from the accepted map.");
        if (formation is not null)
        {
            var allowed = Units[0].Side == "Allied" ? context.AlliedParents : context.GermanParents;
            if (!allowed.Contains(formation.Anchor, StringComparer.Ordinal))
                throw new InvalidDataException("Selected formation anchor is outside its setup permissions.");
        }
        var known = inputs.Regions.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
        if (context.AlliedParents.Length > known.Count || context.GermanParents.Length > known.Count ||
            context.AlliedParents.Concat(context.GermanParents).Any(p => !known.Contains(p)))
            throw new InvalidDataException("Campaign setup references an unknown formation hex.");
        if (formation is not null)
            return BoardPackage.Hash(CanonicalJson.Serialize(new object[] {
                policy, "selected-anchor-binding-2", context.CampaignId, formation.UnitId, formation.Anchor, Units[0].Side }));
        return LegacyContextHash(context);
    }

    private string LegacyContextHash(DeploymentContext context)
    {
        var binding = new object[] { ActivePolicy, context.CampaignId, context.FormationSignature,
            context.AlliedParents.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            context.GermanParents.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() };
        return BoardPackage.Hash(CanonicalJson.Serialize(formation is null ? binding : binding.Concat(new object[] { formation.UnitId, formation.Anchor }).ToArray()));
    }

    // Validate old snapshots before upgrading their binding. Terrain and placements are retained.
    public PilotSetup Rebind(DeploymentContext previous, DeploymentContext current, PilotSetup state)
    {
        state = UpgradeMortar(previous, state);
        var previousHash = ContextHash(previous);
        if (formation is not null && state.ContextHash == LegacyContextHash(previous))
            state = state with { ContextHash = previousHash };
        Validate(previous, state);
        if (previous.CampaignId != current.CampaignId || previous.Formation != current.Formation)
            throw new InvalidDataException("Selected formation moved. Rebuild its local map to use the new anchor.");
        if (state.UnitCatalog is null) state = state with { UnitCatalog = PilotCatalog.Identity };
        if (formation is null) { Validate(current, state); return state; }
        var next = state with { ContextHash = ContextHash(current) };
        Validate(current, next);
        return next;
    }

    public PilotSetup Create(DeploymentContext context)
    {
        var state = new PilotSetup(ActivePolicy, ContextHash(context), Package.Version, 0, false, [], Equipment.Length == 0 ? null : Equipment, PilotCatalog.Identity);
        // Capacity is checked against this seed and campaign policy before admitting the workflow.
        _ = AutoDeploy(context, state); return state;
    }

    public void Validate(DeploymentContext context, PilotSetup state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Policy != ActivePolicy || state.PackageVersion != Package.Version || state.ContextHash != ContextHash(context))
            throw new InvalidDataException("Formation setup or supply rules changed. Keep the saved pilot; restore its formation context before reopening it.");
        if (state.Revision < 0 || state.Placements is null || state.Placements.Length > Units.Count || state.Placements.Any(p => p is null) ||
            state.Placements.Select(p => p.UnitId).Distinct(StringComparer.Ordinal).Count() != state.Placements.Length)
            throw new InvalidDataException("Invalid pilot roster or duplicate placement.");
        if ((Equipment.Length > 0 || state.UnitCatalog is not null) && !PilotCatalog.Recognizes(state.UnitCatalog))
            throw new InvalidDataException("Setup requires a recognized ASL catalog identity.");
        var links = state.Equipment ?? [];
        if (links.Length != Equipment.Length || links.Any(l => l is null) ||
            !links.OrderBy(l => l.EquipmentId, StringComparer.Ordinal).SequenceEqual(Equipment.OrderBy(l => l.EquipmentId, StringComparer.Ordinal)))
            throw new InvalidDataException("Equipment operators differ from the accepted composition.");
        foreach (var link in links)
        {
            var weapon = state.Placements.SingleOrDefault(p => p.UnitId == link.EquipmentId);
            var crew = state.Placements.SingleOrDefault(p => p.UnitId == link.Holding.Holder);
            if ((weapon is null) != (crew is null) || weapon?.Hex != crew?.Hex)
                throw new InvalidDataException("A weapon and its assigned crew must set up together.");
        }
        foreach (var p in state.Placements)
        {
            var reason = Rejection(context, state, p.UnitId, p.Hex);
            if (reason is not null) throw new InvalidDataException(reason);
        }
        if (state.Complete && state.Placements.Length != Units.Count) throw new InvalidDataException("Deploy all units before completing setup.");
    }

    public string? Rejection(DeploymentContext context, PilotSetup state, string unitId, string hex)
    {
        ArgumentNullException.ThrowIfNull(context); ArgumentNullException.ThrowIfNull(state);
        var unit = Units.FirstOrDefault(u => u.Id == unitId); var cell = Cells.FirstOrDefault(c => c.Hex == hex);
        if (unit is null) return "Unknown pilot unit.";
        if (cell is null) return "Choose a hex in the pilot map.";
        if (!cell.Admitted) return cell.Reason;
        if ((unit.Kind == "gun" || Units.Any(u => u.Kind == "gun" && u.Holding?.Holder == unit.Id)) &&
            cell.Terrain is not ("Open Ground" or "Paved Road"))
            return "Gun detachments currently require open ground or paved road; other terrain is inspection-only.";
        // Bounded setup admission, not a complete ASL Gun emplacement rule.
        if ((unit.Kind is "mortar" or "crew") && cell.Terrain is not ("Open Ground" or "Paved Road"))
            return "Mortar detachments currently require open ground or paved road; other terrain is inspection-only.";
        if (unit.Kind == "vehicle" && cell.Terrain is not ("Open Ground" or "Paved Road"))
            return "Vehicle setup currently requires open ground or paved road; no building or woods entry is inferred.";
        if (unit.Kind == "vehicle" && state.Placements.Any(p => p.UnitId != unitId && p.Hex == hex))
            return "Vehicle setup requires an unoccupied hex.";
        if (state.Placements.Any(p => p.UnitId != unitId && p.Hex == hex && Units.Any(u => u.Id == p.UnitId && u.Kind == "vehicle")))
            return "This hex already contains a vehicle.";
        if (formation is not null && cell.Parent != formation.Anchor) return "Deploy within the selected formation hex " + formation.Anchor + ".";
        var allowed = unit.Side == "Allied" ? context.AlliedParents : context.GermanParents;
        if (!allowed.Contains(cell.Parent, StringComparer.Ordinal)) return unit.Side + " setup is not allowed in formation hex " + cell.Parent + ".";
        var group = LinkedUnits(unitId);
        var occupants = state.Placements.Where(p => !group.Contains(p.UnitId) && p.Hex == hex && Units.Any(u => u.Id == p.UnitId && u.Holding is null)).ToArray();
        if (occupants.Any(p => Units.FirstOrDefault(u => u.Id == p.UnitId)?.Side != unit.Side)) return "Opposing units cannot share a setup hex.";
        return occupants.Length >= 2 ? "Stacking limit: two pilot units per hex." : null;
    }

    public PilotSetup Move(DeploymentContext context, PilotSetup state, string unitId, string hex)
    {
        Validate(context, state);
        if (state.Complete) throw new InvalidDataException("Setup is complete. Select Review setup before repositioning units.");
        var reason = Rejection(context, state, unitId, hex);
        if (reason is not null) throw new InvalidDataException(reason);
        return state with { Revision = checked(state.Revision + 1), Placements = [.. state.Placements.Where(p => !LinkedUnits(unitId).Contains(p.UnitId)), .. LinkedUnits(unitId).Select(id => new PilotPlacement(id, hex))] };
    }

    public PilotSetup AutoDeploy(DeploymentContext context, PilotSetup state)
    {
        Validate(context, state); var next = state;
        foreach (var unit in Units.Where(u => !state.Placements.Any(p => p.UnitId == u.Id)))
        {
            if (next.Placements.Any(p => p.UnitId == unit.Id)) continue;
            var cell = Cells.FirstOrDefault(c => !next.Placements.Any(p => p.Hex == c.Hex) && Rejection(context, next, unit.Id, c.Hex) is null)
                ?? Cells.FirstOrDefault(c => Rejection(context, next, unit.Id, c.Hex) is null);
            if (cell is null) throw new InvalidDataException("Insufficient legal setup capacity for " + unit.Side + ". No deployment was changed.");
            next = Move(context, next, unit.Id, cell.Hex);
        }
        return next;
    }

    private string[] LinkedUnits(string id)
    {
        var link = Equipment.FirstOrDefault(l => l.EquipmentId == id || l.Holding.Holder == id);
        return link is null ? [id] : [link.Holding.Holder, link.EquipmentId];
    }

    // Expand only the exact old six-detachment policy after checking its original binding.
    // No terrain regeneration, no best-effort repair of malformed saves.
    private PilotSetup UpgradeMortar(DeploymentContext context, PilotSetup state)
    {
        const string oldPolicy = "goodwood-selected-mortar-1";
        if (state.Policy != oldPolicy || ActivePolicy != "goodwood-selected-mortar-2") return state;
        if (state.PackageVersion != Package.Version || state.ContextHash != ContextHashFor(context, oldPolicy) ||
            state.Revision < 0 || state.Placements is null || state.Placements.Length > 6 ||
            state.Equipment is { Length: > 0 } || (state.Complete && state.Placements.Length != 6))
            throw new InvalidDataException("Invalid legacy mortar setup; saved data retained.");
        var placements = new List<PilotPlacement>();
        foreach (var p in state.Placements)
        {
            var index = Enumerable.Range(1, 6).SingleOrDefault(i => p?.UnitId == formation!.UnitId + "/detachment-" + i);
            if (index == 0) throw new InvalidDataException("Unknown legacy mortar detachment.");
            placements.Add(new(formation!.UnitId + "/crew-" + index, p!.Hex));
            placements.Add(new(formation.UnitId + "/mortar-" + index, p.Hex));
        }
        var upgraded = state with { Policy = ActivePolicy, ContextHash = ContextHash(context),
            Revision = checked(state.Revision + 1), Placements = placements.ToArray(), Equipment = Equipment, UnitCatalog = PilotCatalog.Identity };
        Validate(context, upgraded);
        return upgraded;
    }

    public PilotSetup SetComplete(DeploymentContext context, PilotSetup state, bool complete)
    {
        Validate(context, state); var next = state with { Complete = complete, Revision = checked(state.Revision + 1) };
        Validate(context, next); return next;
    }
}
