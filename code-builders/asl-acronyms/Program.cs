using AslAcronyms;

if (args.Length != 4 || args[0] is not ("generate" or "verify"))
{
    Console.Error.WriteLine("Usage: AslAcronyms generate|verify <scan.md> <ledger.csv> <catalog.json>");
    return 2;
}
try
{
    var json = CatalogBuilder.Generate(File.ReadAllText(args[1]), File.ReadAllText(args[2]));
    if (args[0] == "verify")
    {
        if (!File.Exists(args[3]) || CatalogBuilder.Normalize(File.ReadAllText(args[3])) != json)
            throw new InvalidDataException("Catalog is missing or stale. Run generate, then rebuild the app.");
    }
    else
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        File.WriteAllText(args[3], json, new System.Text.UTF8Encoding(false));
    }
    Console.WriteLine($"Catalog {args[0]} succeeded: {args[3]}");
    return 0;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
