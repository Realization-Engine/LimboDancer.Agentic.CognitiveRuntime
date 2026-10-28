namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The kinds of option a resolution puts to a side (ruling R5.8; A15.3, A18.11, A7.309, A12.41, C6.51); the caller maps each to its own record name.</summary>
public enum ChoiceKind
{
    BattleHardening,
    LeaderCreation,
    UnlikelyKill,
    Paatc,
    Acquisition,
}

/// <summary>Who owns a proposal's action (ruling R31.6): the side that may propose it, or null when any view may, and what that side does, for the refusal.</summary>
public sealed record ProposalOwner(string? Side, string What);

/// <summary>A Massacre by berserk units at the start of their side's fire phase (A20.4; ruling R5.7): the units and the prisoners, in id order, and the Location's text.</summary>
public sealed record BerserkMassacreFacts(IReadOnlyList<string> Units, IReadOnlyList<string> Prisoners, string Location);

/// <summary>
/// An attack a berserk unit may return to normal by (A15.46, p. 84; pass 72, ruling R72.7): the phase; whether every firer was in the target Location (its
/// TPBF, A7.21, or its FT, which fires there unmultiplied, A22.1); whether it was a CC attack that destroyed an enemy vehicle there; and the attacking group's
/// berserk units that survive it, in id order.
/// </summary>
public sealed record BerserkAttackFacts(string? Phase, bool OwnLocation, bool VehicleDestroyedInCc, IReadOnlyList<string> BerserkAttackers);

/// <summary>The Known enemy units in the attacking group's Location before the attack and those left after it (A15.46).</summary>
public readonly record struct KnownEnemyCount(int Before, int After);
