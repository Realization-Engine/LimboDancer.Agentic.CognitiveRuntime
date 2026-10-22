namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// One CC round as a table (the user, 2026-10-21: the record's text was "completely unreadable by a human player"): its Location in words, the
/// round, its attacks, and the round's other effects (SW lost, withdrawals, captures) as notes. Every name is the view's, as of the round.
/// </summary>
public sealed record CloseCombatTable(string Location, string Round, IReadOnlyList<CloseCombatTableAttack> Attacks, IReadOnlyList<string> Notes);

/// <summary>
/// One attack of the round: the attacking side, the units on each side, their FP, the odds and Kill Number, the dice, the attack's DRM and the
/// modified DR, the DRM every defender shares (said once), a row per defender, the outcome in words, and any Leader Creation.
/// </summary>
public sealed record CloseCombatTableAttack(
    string Side,
    IReadOnlyList<string> Attackers,
    IReadOnlyList<string> Defenders,
    string AttackFirepower,
    string DefenseFirepower,
    string Odds,
    int KillNumber,
    string Dice,
    IReadOnlyList<string> Drm,
    decimal ModifiedDr,
    IReadOnlyList<string> SharedDrm,
    IReadOnlyList<CloseCombatTableRow> Rows,
    string Outcome,
    string? LeaderCreation);

/// <summary>A defender's line: its name, its own DRM (those not shared by every defender), its Final DR, and its result.</summary>
public sealed record CloseCombatTableRow(string Unit, IReadOnlyList<string> Drm, int FinalDr, string Result, bool Eliminated);
