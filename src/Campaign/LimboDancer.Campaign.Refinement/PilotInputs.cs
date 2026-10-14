using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Campaign.Refinement;

public sealed record PilotProfile(string Version, string SourceSha256, string CatalogSha256,
    int CropColumn, int CropRow, int Width, int Height, int RoadWidthMm, int StreamWidthMm,
    int ParcelSpacingMm, int BuildingMinMm, int BuildingMaxMm, int BuildingPercent,
    int RouteClearanceMm, int BoundaryInsetMm,
    string Anchor, Dictionary<string, byte> TerrainCodes);
public sealed record SourceRegion(string Key, string Kind, int Elevation, MetricPoint[] Polygon);
public sealed record SourceRoute(string Key, string Kind, MetricPoint[] Points);
public sealed record SourceBridge(string Key, MetricPoint[] Polygon);

public sealed class PilotInputs
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public PilotProfile Profile { get; }
    public string ProfileDigest { get; }
    public TerrainCatalog Catalog { get; }
    public byte[] CatalogBytes { get; }
    public string CatalogBlob { get; }
    public Crop DefaultCrop => new(Profile.CropColumn, Profile.CropRow, Profile.Width, Profile.Height);
    public IReadOnlyList<SourceRegion> Regions { get; }
    public IReadOnlyList<SourceRoute> Routes { get; }
    public IReadOnlyList<SourceBridge> Bridges { get; }
    public const string MapSpace = "panzer-leader-05-joined-b-a-c";
    public const string GeneratorVersion = "goodwood-pilot-1";

    public PilotInputs(string sourceJson)
    {
        ArgumentNullException.ThrowIfNull(sourceJson);
        var profile = Resource("goodwood.json");
        ProfileDigest = BoardPackage.Hash(profile);
        Profile = JsonSerializer.Deserialize<PilotProfile>(profile, JsonOptions)!;
        if (BoardPackage.Hash(Encoding.UTF8.GetBytes(sourceJson.Replace("\r\n", "\n", StringComparison.Ordinal))) != Profile.SourceSha256)
            throw new InvalidDataException("Goodwood source differs from the pinned fixture. Review the adapter before generating a new version.");
        CatalogBytes = Resource("SharedBoardMetadata.xml");
        if (BoardPackage.Hash(CatalogBytes) != Profile.CatalogSha256) throw new InvalidDataException("Pilot catalog hash mismatch.");
        using var stream = new MemoryStream(CatalogBytes);
        Catalog = SharedBoardMetadataParser.Parse(stream).Catalog ?? throw new InvalidDataException("Pilot catalog cannot be loaded.");
        // ASL authored-board loading uses the Git blob identity of SharedBoardMetadata.xml.
        CatalogBlob = GitBlob.Sha(CatalogBytes);
        using var doc = JsonDocument.Parse(sourceJson);
        var root = doc.RootElement; var illustration = root.GetProperty("illustration");
        var layouts = new Dictionary<string, (int Turn, double X, double Y)>(StringComparer.Ordinal);
        double ox = 0, oy = 0;
        foreach (var item in illustration.GetProperty("joinedLayout").EnumerateArray())
        {
            var turn = item.GetProperty("clockwiseDegrees").GetInt32();
            layouts.Add(item.GetProperty("boardId").GetString()!, (turn, ox, oy));
            if (illustration.GetProperty("layoutAxis").GetString() == "vertical") oy += turn % 180 == 0 ? 3138 : 1130;
            else ox += turn % 180 == 0 ? 1130 : 3138;
        }
        MetricPoint Project(string board, double u, double v)
        {
            var l = layouts[board]; return SpatialFrame.Project(u, v, l.Turn, l.X, l.Y);
        }
        MetricPoint[] Pairs(string board, JsonElement array) => array.EnumerateArray()
            .Select(v => Project(board, v[0].GetDouble(), v[1].GetDouble())).ToArray();
        Regions = root.GetProperty("boards").EnumerateArray().SelectMany(b =>
        {
            var board = b.GetProperty("id").GetString()!;
            return b.GetProperty("hexes").EnumerateArray().Select(h => new SourceRegion(
                board + "/" + h.GetProperty("id").GetString(), h.GetProperty("terrain").GetProperty("base").GetString()!,
                h.GetProperty("terrain").GetProperty("elevationLevel").GetInt32(),
                h.GetProperty("imagePolygon").EnumerateArray().Select(v => Project(board, v.GetProperty("u").GetDouble(), v.GetProperty("v").GetDouble())).Distinct().ToArray()));
        }).OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
        Routes = root.GetProperty("mapModel").GetProperty("routes").EnumerateArray().Select(r =>
        {
            var geometry = r.GetProperty("geometry");
            return new SourceRoute(r.GetProperty("id").GetString()!, r.GetProperty("kind").GetString()!,
                Pairs(geometry.GetProperty("boardId").GetString()!, geometry.GetProperty("coordinates")));
        }).OrderBy(r => r.Key, StringComparer.Ordinal).ToArray();
        Bridges = illustration.GetProperty("features").EnumerateArray().Where(f => f.GetProperty("kind").GetString() == "bridge")
            .Select(f => new SourceBridge(f.GetProperty("id").GetString()!, Pairs(f.GetProperty("boardId").GetString()!, f.GetProperty("polygon"))))
            .OrderBy(b => b.Key, StringComparer.Ordinal).ToArray();
    }
    private static byte[] Resource(string name)
    {
        using var stream = typeof(PilotInputs).Assembly.GetManifestResourceStream("Fixtures." + name)
            ?? throw new InvalidDataException("Missing pilot fixture " + name);
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
}
