namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The target building's material and Factory status (B23.3, B23.31, p. 136).</summary>
public enum BuildingFireKind
{
    None, Wood, Stone, WoodFactory, StoneFactory
}

/// <summary>The supported ordinary interior/ground-Factory Location, or a separately ruled special Location.</summary>
public enum BuildingFireLocation
{
    Ordinary, UnsupportedSpecial
}

/// <summary>Standard ASL or Deluxe ASL, whose Bypass Residual rule is J2.23 (p. 602).</summary>
public enum BuildingFireProfile
{
    Standard, Deluxe
}

/// <summary>Residual includes Fire Lane FP (A9.222, p. 63); indirect and Residual cannot both be selected.</summary>
public enum BuildingFireAttack
{
    Direct, Indirect, Residual
}

/// <summary>Whether a building contribution was evaluated, is inapplicable, or lacks a supported known answer.</summary>
public enum BuildingFireDisposition
{
    Evaluated, NotApplicable, NotEvaluated
}

/// <summary>
/// Facts of the building contribution only (B23.31, p. 136). Null means unknown, never false. Factory material and attack kind have one source;
/// the calculator constructs FactoryTemFacts itself. Firer and entry enums retain their B23.741 meanings. No state or map is read here.
/// </summary>
public sealed record BuildingFireFacts(BuildingFireKind? Building, BuildingFireLocation? Location, BuildingFireProfile? Profile,
    BuildingFireAttack? Attack, bool? Bypassing, bool? Roofless = null, FactoryFirerPlace? Firer = null,
    bool? TracedWithinDepiction = null, bool? OverAdjacentRubbleEnteringAcrossDepiction = null, FactoryResidualEntry? ResidualEntry = null);

/// <summary>A zero TEM removes only the building contribution. A missing TEM must not become zero total DRM.</summary>
public sealed record BuildingFireVerdict(BuildingFireDisposition Disposition, int? Tem, string Reason, string Rule, int Page);
