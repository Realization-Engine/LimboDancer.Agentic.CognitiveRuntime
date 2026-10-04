namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A roll as a view reads it (pass 28b; moved here from the Play page's roll list in pass 31c, so the records service uses no type of a
/// component): its id, what it was for with its dice and units, such as "Rally DR: 1, 2 for 4-4-7 squad R1", and how it was drawn.
/// </summary>
public sealed record RollRow(string Roll, string Text, string Drawn);
