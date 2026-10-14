
    /// <summary>A unit of a Location as the CC-required read sees it (A15.43, A11.15): berserk with a Known enemy there, or reinforcing a Melee this APh.</summary>
    public sealed record RequiredCcUnitFacts(string Id, string Side, bool Berserk, bool Melee, bool Broken, bool Known, bool AdvancedHere, bool Crew, bool Vehicle);

    /// <summary>A15.43, A11.31 (ruling R27.2): a berserk unit owing an attack on a Known enemy vehicle attacks it in the Location's sequential CC first.</summary>
    public static string CcRequiredVehicleText(string unitId, string locationText) =>
        $"play.cc-required: {unitId} is berserk with a Known enemy vehicle in {locationText}, so it attacks the vehicle in CC before the CCPh ends (A15.43, A11.31)";

    /// <summary>
    /// A15.43, A11.15: the unit whose CC the Location still requires: a berserk unit with a Known enemy there, else, where a Melee is held, an unbroken unit
    /// that advanced in this APh; null when a crew or a vehicle is there (CC with them is not reviewed or is sequential) or none qualifies. The flag says
    /// which rule.
    /// </summary>
    public static (string Id, bool Berserk)? RequiredUnit(IReadOnlyList<RequiredCcUnitFacts> here)
    {
        ArgumentNullException.ThrowIfNull(here);
        if (here.Any(unit => unit.Crew || unit.Vehicle))
        {
            return null;
        }

        var berserk = here.FirstOrDefault(unit => unit.Berserk && here.Any(other => other.Side != unit.Side && other.Known));
        var reinforcing = here.Any(unit => unit.Melee) ? here.FirstOrDefault(unit => unit.AdvancedHere && !unit.Broken) : null;
        return berserk is not null ? (berserk.Id, true) : reinforcing is not null ? (reinforcing.Id, false) : null;
    }

    /// <summary>A15.43, A11.15: why the CCPh may not end.</summary>
    public static string CcRequiredText(string unitId, string locationText, bool berserk) => berserk
        ? $"play.cc-required: {unitId} is berserk with a Known enemy unit in {locationText}, so it attacks in CC before the CCPh ends (A15.43)"
        : $"play.cc-required: {unitId} advanced into the Melee in {locationText}, so it attacks in CC before the CCPh ends (A11.15)";

    /// <summary>
    /// Ruling R14.14 (referee, pass 14): the units of a trial attack by one unit alone: the unit, every unit of the other side, every prisoner, and the
    /// Guards of the prisoners kept.
    /// </summary>
    public static IReadOnlyList<CloseCombatUnit> TrialUnits(IReadOnlyList<CloseCombatUnit> units, string unitId, string side)
    {
        ArgumentNullException.ThrowIfNull(units);
        var kept = units.Where(item => item.UnitId == unitId || item.Side != side || item.Captured == true).ToList();
        kept.AddRange(units.Where(item => item.Side == side && item.UnitId != unitId && kept.Any(prisoner => prisoner.GuardId == item.UnitId)));
        return kept;
    }

    /// <summary>
    /// A11.4, A11.3, A11.32, A11.11: the sides that may declare CC attacks now, phasing side first: after an Ambush, the ambusher until its first round is
    /// resolved or when it attacks again, otherwise the ambushed side; both in a simultaneous round.
    /// </summary>
    public static IReadOnlyList<string> DeclaringSides(IReadOnlyList<string> sidesPhasingFirst, string? ambusher, int rounds, bool ambusherAgain)
    {
        ArgumentNullException.ThrowIfNull(sidesPhasingFirst);
        return ambusher is { } side ? rounds == 0 || ambusherAgain ? [side] : [.. sidesPhasingFirst.Where(item => item != side)] : sidesPhasingFirst;
    }

    /// <summary>A11.4 (ruling R11.16): Ambush drs may be due in the CCPh where no CC has begun and no vehicle is present; the package then decides.</summary>
    public static bool AmbushDueCandidate(string? phase, bool ccBegun, bool vehicleThere) => phase == "ccph" && !ccBegun && !vehicleThere;

    /// <summary>Why the Close Combat package would refuse a Location's Ambush drs, each reason as its sentence (pass 31d, design D9).</summary>
    public static string AmbushUndecidedText(IReadOnlyList<string> reasons, Func<string, string> explain)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(explain);
        return string.Join("; ", reasons.Select(reason => explain(reason).Split(": ", 2) is [_, var sentence] ? sentence : reason));
    }

    /// <summary>Pass 31d (design D9; ruling R31c.5): a Location with an Ambush and a round whose ambushed side has no unit left holds the phase no longer.</summary>
    public static bool NothingLeft(bool ambushed, int rounds, bool ambushedUnitLeft) => ambushed && rounds > 0 && !ambushedUnitLeft;

    /// <summary>What an open CC Location still owes (A11.31, A11.12, A11.3, A11.32).</summary>
    public static string DueOpenLine(string locationText, string? next, string? ambusher) =>
        next is { } side ? $"{locationText}: the {side} side attacks or passes (A11.31)"
            : ambusher is null ? $"{locationText}: its round after the Ambush drs (A11.12)"
            : $"{locationText}: more attacks by the {ambusher} side, then the ambushed side's round, which may declare no attacks (A11.3, A11.32)";

    /// <summary>A11.4 (pass 31d, design D9): the Ambush drs a Location owes, or why the package does not decide it.</summary>
    public static string DueAmbushLine(string locationText, string? undecided) =>
        undecided is { } why ? $"{locationText}: the Close Combat package does not decide this Location, since {why}" : $"{locationText}: the Ambush drs (A11.4)";

    /// <summary>A11.16: a broken unit in Melee that must attempt to withdraw.</summary>
    public static string DueWithdrawLine(string locationText, string unitId) => $"{locationText}: {unitId} is broken in Melee and must attempt to withdraw (A11.16)";

    /// <summary>
    /// A15.431, A15.432 (rulings R10.15, R24.3, R30.5; table player, pass 27): why a berserk unit may not step into a Location: it holds a Gun's crew, or an
    /// enemy vehicle together with enemy Infantry; null when the step is allowed.
    /// </summary>
    public static string? ChargeBarred(bool crewThere, bool enemyVehicleThere, bool enemyInfantryThere, string toText) =>
        crewThere ? $"play.berserk-crew: a charge into {toText}, which holds a Gun's crew, is CC with a crew, which is not reviewed (C11, R24.3); the charge ends in place (ruling R30.5)"
            : enemyVehicleThere && enemyInfantryThere
                ? $"play.berserk-vehicle: a charge into {toText}, which holds an enemy vehicle and enemy Infantry, needs CC between Infantry beside a vehicle, which is not built (R11.16); the charge ends in place (ruling R30.5)"
                : null;
}
