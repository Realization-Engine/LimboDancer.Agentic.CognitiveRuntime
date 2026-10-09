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
