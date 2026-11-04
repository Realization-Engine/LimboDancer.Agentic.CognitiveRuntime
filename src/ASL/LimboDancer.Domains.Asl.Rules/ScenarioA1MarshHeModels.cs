namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The actual resolved effect, not its source weapon. HE includes normal OBA; Residual from HE is still Residual.</summary>
public enum MarshHeEffect
{
    He, DemolitionCharge, HeEquivalency, NonHe, Residual, UnsupportedVtFfe
}

/// <summary>Terrain before the B16.6-B16.8 HE-applicability consequences (p. 130); swamp is separately ruled.</summary>
public enum MarshHeTerrain
{
    Marsh, Mudflat, Other, Swamp
}

/// <summary>Scenario-defined snow cover, separate from sub-zero temperature (B16.8, p. 130).</summary>
public enum MarshHeSnow
{
    None, Snow, DeepSnow
}

/// <summary>A ground-Location effect is not an attack on the bridge or a blanket answer for units on it.</summary>
public enum MarshHeTarget
{
    Unit, Bridge, GroundLocation
}

/// <summary>Whether the B16.31 multiplier is evaluated or still unknown/unsupported.</summary>
public enum MarshHeDisposition
{
    Evaluated, NotEvaluated
}

/// <summary>
/// Facts for B16.31 (p. 130). Connectivity is measured, not a precomputed rule answer. Null facts remain unknown; actual effects and labelled
/// hypothetical effects supply their public context. OnBridge is private when the unit's position is hidden and irrelevant when no bridge exists.
/// </summary>
public sealed record MarshHeFacts(MarshHeEffect? Effect, MarshHeTerrain? Terrain, MarshHeSnow? SnowCover, bool? SubZero,
    bool? ConnectedToFloodedWater, bool? BridgePresent, MarshHeTarget? Target, bool? OnBridge);

/// <summary>The exact FP multiplier, not final firepower or an IFT column. A missing multiplier must never default to 1.</summary>
public sealed record MarshHeVerdict(MarshHeDisposition Disposition, decimal? Multiplier, string Reason, string Rule, int Page);
