using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public static class PilotIdentity
{
    public static string SpatialDigest { get; } = BoardPackage.Hash(CanonicalJson.Serialize(
        new[] { SpatialFrame.Version, "250000", "40000", "34641.016151377546", "0", "0" }));
    public static string[] Defining(PilotInputs inputs, string seed)
    {
        ArgumentNullException.ThrowIfNull(inputs); SeedRandom.ValidateSeed(seed);
        return [SeedRandom.Version, seed, PilotInputs.MapSpace, inputs.Profile.SourceSha256,
            SpatialDigest, inputs.ProfileDigest, inputs.Version];
    }
    public static string World(PilotInputs inputs, string seed) => BoardPackage.Hash(CanonicalJson.Serialize(Defining(inputs, seed)));
    public static string Reference(PilotInputs inputs, string seed, Crop crop)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(crop);
        var key = string.Create(CultureInfo.InvariantCulture, $"{crop.Column}/{crop.Row}/{crop.Width}/{crop.Height}");
        return "ab-goodwood-" + BoardPackage.Hash(CanonicalJson.Serialize(new[] { World(inputs, seed), key,
            inputs.CatalogBlob, FeatureCompiler.Version, VaslCompatibleHexFactDerivation.Version, "feature-model-1" }));
    }
}
