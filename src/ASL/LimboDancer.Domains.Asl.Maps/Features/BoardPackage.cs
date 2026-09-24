using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>A validation summary recorded in a package manifest. It is informational and not part of the version.</summary>
public sealed record ValidationSummary(int Errors, int Warnings, int Infos);

/// <summary>
/// An authored board package (Model Design section 9): a manifest (<c>board.json</c>) and the Feature Model
/// (<c>features.json</c>). The <see cref="Version"/> is SHA-256 over the sorted (entry name, entry hash) list of the
/// defining entries only: the manifest's defining fields and the features. Caches never change identity.
/// </summary>
public sealed record BoardPackage(BoardRef Board, string Name, FeatureModel Model, ValidationSummary? Validation = null)
{
    public const string ManifestEntry = "board.json";
    public const string FeaturesEntry = "features.json";
    public const int FormatVersion = 1;

    /// <summary>The <c>BoardVersion</c>: lowercase SHA-256 hex.</summary>
    public string Version => ComputeVersion(DefiningManifestBytes(), FeatureModelJson.Serialize(Model));

    public byte[] FeaturesBytes() => FeatureModelJson.Serialize(Model);

    /// <summary>The manifest with its defining fields, the features entry hash, and the validation summary.</summary>
    public byte[] ManifestBytes()
    {
        var manifest = DefiningFields();
        manifest["entries"] = new Dictionary<string, object?> { [FeaturesEntry] = Hash(FeaturesBytes()) };
        if (Validation is { } validation)
        {
            manifest["validation"] = new Dictionary<string, object?>
            {
                ["errors"] = validation.Errors,
                ["warnings"] = validation.Warnings,
                ["infos"] = validation.Infos,
            };
        }

        return CanonicalJson.Serialize(manifest);
    }

    public static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>SHA-256 over the canonical sorted list of (entry name, entry hash) pairs.</summary>
    public static string ComputeVersion(byte[] definingManifest, byte[] features)
    {
        var entries = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            [ManifestEntry] = Hash(definingManifest),
            [FeaturesEntry] = Hash(features),
        };
        return Hash(CanonicalJson.Serialize(entries.Select(pair => (object?)new List<object?> { pair.Key, pair.Value }).ToList()));
    }

    private byte[] DefiningManifestBytes() => CanonicalJson.Serialize(DefiningFields());

    private Dictionary<string, object?> DefiningFields() => new()
    {
        ["formatVersion"] = FormatVersion,
        ["boardRef"] = Board.Value,
        ["name"] = Name,
        ["geometry"] = FeatureModelJson.GeometryJson(Model.Geometry),
        ["catalogHash"] = Model.CatalogHash,
        ["provenance"] = FeatureModelJson.Provenance(Model.Provenance),
    };
}

/// <summary>A package load outcome with its diagnostics.</summary>
public sealed record BoardPackageLoad(BoardPackage? Package, IReadOnlyList<MapDiagnostic> Diagnostics);

/// <summary>
/// Reads and writes packages as directories, one per board under a root, named by the board's slug (Model Design
/// section 9.4). The features hash in the manifest is checked on load.
/// </summary>
public sealed class BoardPackageStore(string root)
{
    public string Root { get; } = root;

    public string DirectoryOf(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return Path.Combine(Root, Slug(board));
    }

    public IReadOnlyList<BoardRef> List()
    {
        if (!Directory.Exists(Root))
        {
            return [];
        }

        return Directory.GetDirectories(Root)
            .Where(directory => File.Exists(Path.Combine(directory, BoardPackage.ManifestEntry)))
            .Select(directory => BoardRef.TryParse("ab-" + Path.GetFileName(directory), out var board) ? board : null)
            .OfType<BoardRef>()
            .OrderBy(board => board.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public void Save(BoardPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var directory = DirectoryOf(package.Board);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, BoardPackage.FeaturesEntry), package.FeaturesBytes());
        File.WriteAllBytes(Path.Combine(directory, BoardPackage.ManifestEntry), package.ManifestBytes());
    }

    public BoardPackageLoad Load(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var directory = DirectoryOf(board);
        var manifestPath = Path.Combine(directory, BoardPackage.ManifestEntry);
        var featuresPath = Path.Combine(directory, BoardPackage.FeaturesEntry);
        if (!File.Exists(manifestPath) || !File.Exists(featuresPath))
        {
            return Failed("MAP-PKG-001", $"{board} has no package at {directory}.");
        }

        try
        {
            var features = File.ReadAllBytes(featuresPath);
            using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
            var root = manifest.RootElement;
            var expected = root.GetProperty("entries").GetProperty(BoardPackage.FeaturesEntry).GetString();
            if (expected != BoardPackage.Hash(features))
            {
                return Failed("MAP-PKG-002", $"{board}: {BoardPackage.FeaturesEntry} does not match the hash recorded in {BoardPackage.ManifestEntry}.");
            }

            if (root.GetProperty("boardRef").GetString() != board.Value)
            {
                return Failed("MAP-PKG-003", $"{board}: the manifest names {root.GetProperty("boardRef").GetString()}.");
            }

            var model = FeatureModelJson.Deserialize(features);
            return new BoardPackageLoad(new BoardPackage(board, root.GetProperty("name").GetString() ?? board.Value, model), []);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            return Failed("MAP-PKG-004", $"{board}: the package cannot be read: {exception.Message}");
        }
    }

    /// <summary>The directory name for an authored board: its reference without the <c>ab-</c> prefix.</summary>
    public static string Slug(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Kind != BoardRefKind.Authored)
        {
            throw new ArgumentException($"{board} is not an authored board.", nameof(board));
        }

        return board.Value[3..];
    }

    private static BoardPackageLoad Failed(string code, string message) => new(null, [new MapDiagnostic(code, MapDiagnosticSeverity.Error, message)]);
}
