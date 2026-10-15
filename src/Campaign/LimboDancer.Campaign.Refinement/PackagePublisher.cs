using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public static class PackagePublisher
{
    /// <summary>Publish an immutable directory. Staging is never a discoverable board store.</summary>
    public static string Publish(PilotResult result, PilotInputs inputs, string outputRoot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result); ArgumentNullException.ThrowIfNull(inputs);
        var root = Path.GetFullPath(outputRoot); Directory.CreateDirectory(root);
        var destination = Path.Combine(root, result.Package.Version);
        if (Directory.Exists(destination)) { Verify(destination, result, inputs); return destination; }
        var stage = Path.Combine(root, ".stage-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(stage);
            File.WriteAllBytes(Path.Combine(stage, "refinement.json"), result.Manifest);
            File.WriteAllBytes(Path.Combine(stage, "SharedBoardMetadata.xml"), inputs.CatalogBytes);
            new BoardPackageStore(Path.Combine(stage, "boards")).Save(result.Package);
            Verify(stage, result, inputs);
            cancellationToken.ThrowIfCancellationRequested();
            try { Directory.Move(stage, destination); }
            catch (IOException) when (Directory.Exists(destination)) { Verify(destination, result, inputs); }
            return destination;
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
    private static void Verify(string directory, PilotResult result, PilotInputs inputs)
    {
        var loaded = new BoardPackageStore(Path.Combine(directory, "boards")).Load(result.Package.Board);
        if (loaded.Package?.Version != result.Package.Version ||
            !File.ReadAllBytes(Path.Combine(directory, "refinement.json")).AsSpan().SequenceEqual(result.Manifest) ||
            !File.ReadAllBytes(Path.Combine(directory, "SharedBoardMetadata.xml")).AsSpan().SequenceEqual(inputs.CatalogBytes))
            throw new InvalidDataException("Stored pilot package or manifest is corrupt or incompatible.");
        var grid = FeatureCompiler.Compile(loaded.Package.Model, inputs.Catalog).Grid;
        if (PilotGenerator.GridHash(grid) != PilotGenerator.GridHash(result.Grid)) throw new InvalidDataException("Stored terrain does not reproduce the accepted grid.");
    }
}
