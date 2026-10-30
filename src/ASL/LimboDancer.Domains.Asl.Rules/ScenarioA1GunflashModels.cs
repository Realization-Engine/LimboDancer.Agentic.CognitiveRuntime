namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What an act at night is, as E1.8 to E1.89 (p. 226) read it for a Gunflash (the E1.8 claim's design, RE1.1). E1.83 to E1.88 add their causes when
/// their rows are claimed: a PIAT's shot (E1.88, which makes no Gunflash) and a HE FFE's Blast Area (E1.87) are known and left to them.
/// </summary>
public enum GunflashCause
{
    /// <summary>An attack: a unit fires (E1.8).</summary>
    Attack,

    /// <summary>Close Combat, while its Melee counter stays (E1.82).</summary>
    Melee,

    /// <summary>An Opportunity Fire declaration (A7.25), which places a Bounding Fire counter on a unit that has not fired (RE1.4).</summary>
    OpportunityFireDeclared,

    /// <summary>Dismantling or assembling a SW (E1.89).</summary>
    DismantleOrAssemble,

    /// <summary>Spotting (C9.3; E1.89).</summary>
    Spotting,

    /// <summary>The use of a radio or phone (E1.89).</summary>
    RadioOrPhone,

    /// <summary>An Ammo Vehicle's B# benefit (E10.2; E1.89).</summary>
    AmmoVehicleBenefit,

    /// <summary>Firing a Starshell (E1.921).</summary>
    Starshell,
}

/// <summary>
/// A Gunflash marked at night (E1.8, p. 226; RE1.2): the Location the act came from, the side that made it, its cause, and whether it was an attack vs a
/// unit enemy to its attacker, a Dummy stack included (E1.91; RE1.3 as corrected 2026-10-30, by the Index's "Unit"). It belongs to the Location until the end of the AFPh (ASOP 5.33B, p. 680), whoever is there.
/// </summary>
public sealed record GunflashMark(string Location, string Side, GunflashCause Cause, bool VsEnemyUnit);

/// <summary>
/// Who tries a Starshell, as E1.92 and E1.921 (p. 226) name the firers (RE1.6): a leader; a CE AFV with an Armor Leader, which fires as a leader; a CE AFV
/// with none; a MMC; any other unit, a BU AFV among them, which fires none.
/// </summary>
public enum StarshellFirer
{
    /// <summary>A leader.</summary>
    Leader,

    /// <summary>A CE AFV with an Armor Leader: "a leader (including a CE Armor Leader)".</summary>
    CeAfvWithArmorLeader,

    /// <summary>A CE AFV with no Armor Leader.</summary>
    CeAfv,

    /// <summary>A MMC.</summary>
    Mmc,

    /// <summary>Any other unit, a BU AFV among them.</summary>
    Other,
}
