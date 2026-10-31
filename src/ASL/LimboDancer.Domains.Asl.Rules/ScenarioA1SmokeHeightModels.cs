namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Resolved map facts at one LOS end, before A24.4 decides whether that end is within SMOKE.</summary>
public sealed record SmokeLosEnd(decimal Elevation, decimal RooftopAdjustment, bool Cellar, bool Hillock, bool Upslope,
    decimal EmbankmentAdjustment);

/// <summary>
/// A24.4/A6.4 (PDF pp. 92, 53): one SMOKE source's absolute base and height, the LOS ends, and independent hex distances.
/// Crossed is the planar inherent-terrain footprint. The source and target hexes are named separately from intermediate crossed hexes.
/// </summary>
public sealed record SmokeLosFacts(decimal SmokeBase, decimal GroundLevel, int Height, SmokeLosEnd Source, SmokeLosEnd Target,
    int RangeFromSource, int RangeToTarget, bool Crossed, bool SourceHex, bool TargetHex);

/// <summary>Intersection and whether either end is actually within SMOKE. Passing down through one's own hex does not imply firing out of SMOKE.</summary>
public sealed record SmokeLosVerdict(bool Intersects, bool SourceWithin, bool TargetWithin);
