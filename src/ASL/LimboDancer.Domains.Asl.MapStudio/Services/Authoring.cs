using System.Collections.Concurrent;
using System.Diagnostics;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>An authored board in the library: its reference, name, and whether it is a draft.</summary>
public sealed record AuthoredListing(BoardRef Ref, string Name, bool IsDraft);

/// <summary>
/// Authored boards (Model Design sections 9 and 10): packages under <c>BoardsRoot</c>, and drafts under
/// <c>{CacheRoot}/drafts</c> for boards derived from VASL data, which may not be committed (ASL-MAP-074). Builds are
/// cached by (board, version), so render URLs for recent edit versions keep working.
/// </summary>
public sealed class AuthoredBoardService
{
    private const int MaxCachedVersions = 64;
    private readonly ICatalogSource catalogSource;
    private readonly ConcurrentDictionary<(string Board, string Version), StudioBoard> versions = new();
    private readonly ConcurrentQueue<(string Board, string Version)> order = new();

    public AuthoredBoardService(StudioOptions options, ICatalogSource catalogSource)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.catalogSource = catalogSource;
        Boards = new BoardPackageStore(options.ResolveBoardsRoot());
        Drafts = new BoardPackageStore(Path.Combine(options.ResolveCacheRoot(), "drafts"));
    }

    public BoardPackageStore Boards
    {
        get;
    }

    public BoardPackageStore Drafts
    {
        get;
    }

    /// <summary>The catalog authored boards use, or null when no VASL checkout is configured.</summary>
    public (TerrainCatalog Catalog, string Hash)? Catalog => catalogSource.Catalog();

    public IReadOnlyList<AuthoredListing> List()
    {
        var saved = Boards.List().Select(board => new AuthoredListing(board, Boards.Load(board).Package?.Name ?? board.Value, false));
        var drafts = Drafts.List().Select(board => new AuthoredListing(board, Drafts.Load(board).Package?.Name ?? board.Value, true));
        return [.. saved.Concat(drafts).OrderBy(listing => listing.Ref.Value, StringComparer.Ordinal)];
    }

    public bool Exists(BoardRef board) => Directory.Exists(Boards.DirectoryOf(board)) || Directory.Exists(Drafts.DirectoryOf(board));

    public BoardLoadResult Load(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (Catalog is not { } catalog)
        {
            return new BoardLoadResult(null, [new MapDiagnostic("STUDIO-002", MapDiagnosticSeverity.Error, "Authored boards need the terrain catalog from a configured VASL checkout.")]);
        }

        var draft = !Directory.Exists(Boards.DirectoryOf(board));
        var load = (draft ? Drafts : Boards).Load(board);
        return load.Package is { } package
            ? new BoardLoadResult(Build(package, catalog.Catalog, draft), load.Diagnostics)
            : new BoardLoadResult(null, load.Diagnostics);
    }

    /// <summary>The saved package for a board and whether it is a draft, for opening it in the editor.</summary>
    public (BoardPackage? Package, bool Draft, IReadOnlyList<MapDiagnostic> Diagnostics) LoadPackage(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var draft = !Directory.Exists(Boards.DirectoryOf(board));
        var load = (draft ? Drafts : Boards).Load(board);
        return (load.Package, draft, load.Diagnostics);
    }

    public StudioBoard? LoadVersion(BoardRef board, string version) =>
        versions.TryGetValue((board.Value, version), out var built) ? built : Load(board).Board is { } loaded && loaded.Version == version ? loaded : null;

    /// <summary>Compiles, derives, and validates a package, and caches the result by version.</summary>
    public StudioBoard Build(BoardPackage package, TerrainCatalog catalog, bool draft)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(catalog);
        var version = package.Version;
        var key = (package.Board.Value, version);
        if (versions.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var compiled = FeatureCompiler.Compile(package.Model, catalog).Grid;
        var facts = VaslCompatibleHexFactDerivation.Derive(compiled, catalog, package.Model.Annotations.Hexsides);
        var validation = BoardValidator.Validate(package.Model, facts, catalog);

        var render = BoardRenderInput.Create(package.Board, package.Name, compiled, catalog, facts, new StyledSource(package.Model, compiled, facts));
        var board = new StudioBoard(package.Board, version, package.Name, validation.HasErrors ? BoardStatus.Authored : BoardStatus.AuthoredValid,
            render, catalog, null, null, null, [])
        {
            Validation = validation,
            IsDraft = draft,
        };
        versions[key] = board;
        order.Enqueue(key);
        while (order.Count > MaxCachedVersions && order.TryDequeue(out var old))
        {
            versions.TryRemove(old, out _);
        }

        return board;
    }

    /// <summary>Saves a package to the boards root, or to drafts for boards derived from VASL data.</summary>
    public void Save(BoardPackage package, bool draft)
    {
        ArgumentNullException.ThrowIfNull(package);
        (draft ? Drafts : Boards).Save(package);
    }
}

/// <summary>What an edit changed, for the render patch: features to upsert and features removed, by layer.</summary>
public sealed record EditOutcome(string? Error, IReadOnlyList<Feature> Upserted, IReadOnlyList<(string Layer, string Id)> Removed, double Milliseconds)
{
    public bool Succeeded => Error is null;
}

/// <summary>
/// One board being edited (Architecture and Rendering Design, section 6.3): the current model, undo and redo stacks,
/// and the built board for the current version. It belongs to one circuit.
/// </summary>
public sealed class AuthoringSession
{
    private readonly AuthoredBoardService service;
    private readonly TerrainCatalog catalog;
    private readonly Stack<(AuthoringCommand Inverse, string Description)> undo = new();
    private readonly Stack<(AuthoringCommand Command, string Description)> redo = new();

    public AuthoringSession(AuthoredBoardService service, TerrainCatalog catalog, BoardPackage package, bool draft)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(package);
        this.service = service;
        this.catalog = catalog;
        Board = package.Board;
        Name = package.Name;
        IsDraft = draft;
        Current = service.Build(package, catalog, draft);
    }

    public BoardRef Board
    {
        get;
    }

    public string Name
    {
        get; set;
    }

    /// <summary>A board derived from VASL data, saved only as a draft (ASL-MAP-074).</summary>
    public bool IsDraft
    {
        get;
    }

    public StudioBoard Current
    {
        get; private set;
    }

    public FeatureModel Model => Current.Render.Styled!.Model;

    public TerrainCatalog Catalog => catalog;

    public bool Dirty
    {
        get; private set;
    }

    public string? UndoDescription => undo.Count > 0 ? undo.Peek().Description : null;

    public string? RedoDescription => redo.Count > 0 ? redo.Peek().Description : null;

    public EditOutcome Execute(AuthoringCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var outcome = Apply(command, out var inverse);
        if (outcome.Succeeded)
        {
            undo.Push((inverse!, command.Description));
            redo.Clear();
        }

        return outcome;
    }

    public EditOutcome? Undo()
    {
        if (undo.Count == 0)
        {
            return null;
        }

        var (inverse, description) = undo.Pop();
        var outcome = Apply(inverse, out var again);
        if (outcome.Succeeded)
        {
            redo.Push((again!, description));
        }

        return outcome;
    }

    public EditOutcome? Redo()
    {
        if (redo.Count == 0)
        {
            return null;
        }

        var (command, description) = redo.Pop();
        var outcome = Apply(command, out var inverse);
        if (outcome.Succeeded)
        {
            undo.Push((inverse!, description));
        }

        return outcome;
    }

    public void Save()
    {
        service.Save(Package(), IsDraft);
        Dirty = false;
    }

    public BoardPackage Package() => new(Board, Name, Model, Current.Validation?.Summary);

    private EditOutcome Apply(AuthoringCommand command, out AuthoringCommand? inverse)
    {
        var watch = Stopwatch.StartNew();
        var before = Model;
        var result = AuthoringCommands.Apply(before, command, catalog);
        inverse = result.Inverse;
        if (!result.Succeeded)
        {
            return new EditOutcome(result.Error, [], [], watch.Elapsed.TotalMilliseconds);
        }

        Current = service.Build(new BoardPackage(Board, Name, result.Model!), catalog, IsDraft);
        Dirty = true;
        var previous = before.Features.ToDictionary(feature => feature.Id, StringComparer.Ordinal);
        var upserted = Model.Features.Where(feature => !previous.TryGetValue(feature.Id, out var old) || !ReferenceEquals(old, feature)).ToArray();
        var current = Model.Features.Select(feature => feature.Id).ToHashSet(StringComparer.Ordinal);
        var removed = before.Features.Where(feature => !current.Contains(feature.Id)).Select(feature => (BoardRenderer.StyledLayerOf(feature), "f-" + feature.Id)).ToArray();
        return new EditOutcome(null, upserted, removed, watch.Elapsed.TotalMilliseconds);
    }
}

/// <summary>Builds features from editor gestures (Architecture and Rendering Design, section 6.2), snapping on the server.</summary>
public static class EditorGestures
{
    public static FixedVector ToFixed(double x, double y) => new((int)Math.Round(x * FixedPoint.UnitsPerPixel), (int)Math.Round(y * FixedPoint.UnitsPerPixel));

    /// <summary>Snapped, de-duplicated points of a gesture.</summary>
    public static IReadOnlyList<FixedVector> Snap(FeatureModel model, IEnumerable<(double X, double Y)> points, bool snap)
    {
        ArgumentNullException.ThrowIfNull(model);
        var result = new List<FixedVector>();
        foreach (var (x, y) in points)
        {
            var point = ToFixed(x, y);
            var snapped = snap ? AuthoringGeometry.Snap(model.Geometry, model, point).Point : new FixedVector(FixedGeometry.RoundDiv(point.X, 64) * 64, FixedGeometry.RoundDiv(point.Y, 64) * 64);
            if (result.Count == 0 || result[^1] != snapped)
            {
                result.Add(snapped);
            }
        }

        if (result.Count > 1 && result[^1] == result[0])
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    /// <summary>
    /// A centerline through the points: straight segments, or with <paramref name="curved"/> a Catmull-Rom spline
    /// through them as cubic Bézier segments.
    /// </summary>
    public static CenterlinePath Centerline(IReadOnlyList<FixedVector> points, bool curved)
    {
        ArgumentNullException.ThrowIfNull(points);
        var segments = new List<PathSegment>();
        for (var index = 1; index < points.Count; index++)
        {
            if (!curved || points.Count < 3)
            {
                segments.Add(new PathSegment(points[index]));
                continue;
            }

            var p0 = points[Math.Max(0, index - 2)];
            var p1 = points[index - 1];
            var p2 = points[index];
            var p3 = points[Math.Min(points.Count - 1, index + 1)];
            segments.Add(new PathSegment(p2,
                new FixedVector(p1.X + FixedGeometry.RoundDiv(p2.X - p0.X, 6), p1.Y + FixedGeometry.RoundDiv(p2.Y - p0.Y, 6)),
                new FixedVector(p2.X - FixedGeometry.RoundDiv(p3.X - p1.X, 6), p2.Y - FixedGeometry.RoundDiv(p3.Y - p1.Y, 6))));
        }

        return new CenterlinePath(points[0], segments);
    }

    /// <summary>A feature moved by whole pixels.</summary>
    public static Feature Translate(Feature feature, int dxPixels, int dyPixels, BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(feature);
        var dx = dxPixels * FixedPoint.UnitsPerPixel;
        var dy = dyPixels * FixedPoint.UnitsPerPixel;
        FixedVector Move(FixedVector point) => new(point.X + dx, point.Y + dy);
        FeatureShape MoveShape(FeatureShape shape) => new(shape.Rings.Select(ring => (IReadOnlyList<FixedVector>)ring.Select(Move).ToArray()).ToArray());
        return feature switch
        {
            ElevationRegion region => region with { Shape = MoveShape(region.Shape) },
            AreaTerrainFeature area => area with { Shape = MoveShape(area.Shape) },
            BridgeFeature bridge => bridge with { Shape = MoveShape(bridge.Shape) },
            FidelityPin pin => pin with { Shape = MoveShape(pin.Shape) },
            BuildingFeature building => building with { Footprints = building.Footprints.Select(MoveShape).ToArray() },
            LinearTerrainFeature linear => linear with
            {
                Outline = linear.Outline is { } outline ? MoveShape(outline) : null,
                Centerline = linear.Centerline is { } path
                    ? new CenterlinePath(Move(path.Start), path.Segments.Select(segment => new PathSegment(Move(segment.End),
                        segment.Control1 is { } c1 ? Move(c1) : null, segment.Control2 is { } c2 ? Move(c2) : null)).ToArray())
                    : null,
            },
            HexsideTerrainFeature => feature,
            _ => feature,
        };
    }

    /// <summary>A hexside feature with one more span, or a new one when no feature of that code exists.</summary>
    public static AuthoringCommand AddHexside(FeatureModel model, byte code, HexsideRef side)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Features.OfType<HexsideTerrainFeature>().Where(feature => feature.Code == code).OrderBy(feature => feature.Id, StringComparer.Ordinal).FirstOrDefault() is { } existing)
        {
            // Clicking a hexside that already has this terrain removes it; the last span removes the feature.
            if (existing.Spans.Any(span => span.Side == side))
            {
                var remaining = existing.Spans.Where(span => span.Side != side).ToArray();
                return remaining.Length == 0 ? new RemoveFeature(existing.Id) : new ReplaceFeature(existing with
                {
                    Spans = remaining
                });
            }

            return new ReplaceFeature(existing with
            {
                Spans = [.. existing.Spans, new HexsideSpan(side)]
            });
        }

        return new AddFeature(new HexsideTerrainFeature(FeatureIds.NewUlid(), 0, code, [new HexsideSpan(side)]));
    }
}
