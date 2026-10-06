namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Who may fire, and what the state says of a firer, a target, and a weapon (A7, A8, A9, A15, A19, A20, A21, A22, D2, D3, D5; pass 32.c, slice S4,
/// from Play's <c>LiveFire</c> and the planner's Fire file). The caller reads the state and the catalog and hands the facts over; a read a function
/// here asks for through a delegate is made where the old body made it.
/// </summary>
public static class ScenarioA1FireEligibility
{
    /// <summary>A21.1 (ruling R13.7): a weapon of another nationality than its possessor's is captured; null when either nationality is not known.</summary>
    public static bool? CapturedBy(string? weaponNationality, string? firerNationality) =>
        weaponNationality is not null && firerNationality is not null && weaponNationality != firerNationality ? true : null;

    /// <summary>
    /// A side's nationalities when it holds Allied Troops (A10.7; backlog pass 15, ruling R15.8): every nationality of its units, each once, in
    /// order; null when it holds one alone. <paramref name="nationalities"/> is the nationality of each of the side's units the catalog knows.
    /// </summary>
    public static IReadOnlyList<string>? Allies(IEnumerable<string> nationalities)
    {
        ArgumentNullException.ThrowIfNull(nationalities);
        string[] distinct = [.. nationalities.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        return distinct.Length > 1 ? distinct : null;
    }

    /// <summary>
    /// Whether a Green or Conscript MMC is Inexperienced as the Fire facts read it (A19.2, A19.3; ruling R15.10): a Conscript always, a Green unit
    /// unless stacked with an unbroken leader of its side; null for any other class. This is the Fire facts' definition, kept beside
    /// <see cref="ScenarioA1Experience.Inexperienced"/> (the state's, with a three-valued answer and a leader's Broken as three values) for pass 45
    /// (the pass 32 design, section 12). <paramref name="stackedWithUnbrokenLeader"/> is read for a Green unit only, as the old body read it.
    /// </summary>
    public static bool? InexperiencedForFire(string? @class, Func<bool> stackedWithUnbrokenLeader)
    {
        ArgumentNullException.ThrowIfNull(stackedWithUnbrokenLeader);
        return @class switch
        {
            "conscript" => true,
            "green" => !stackedWithUnbrokenLeader(),
            _ => null,
        };
    }

    /// <summary>A Green MMC's Inexperience (A19.3; ruling R15.10); null for any other class.</summary>
    public static bool? GreenInexperienced(string? @class, Func<bool> stackedWithUnbrokenLeader) =>
        @class == "green" ? InexperiencedForFire(@class, stackedWithUnbrokenLeader) : null;

    /// <summary>
    /// Whether a phasing vehicle may fire as Bounding First Fire now (D3.3; ruling R6.9): in the MPh, while it moves as the moving vehicle once the
    /// DEFENDER has passed on its last MP expenditure, or at the outset of its MPh while no other move is under way and it has not ended its move.
    /// </summary>
    public static bool MayBoundingFire(BoundingFireFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Phase == "mph" && facts.ActiveVehicleOfPhasingSide
            && (facts.MovementExists && facts.MovementVehicle && !facts.WindowOpen ? facts.AmongMovers : !facts.MovementExists && !facts.MovementEnded);
    }

    /// <summary>
    /// Whether an AFV's crew is Crew Exposed: an OT AFV is CE unless under a BU, Stun, Shock, or Recall marker (D5.3, D5.34, C7.42); a CT AFV only
    /// when its owner has removed its BU counter (D5.2; ruling R7.11); an unarmored vehicle has no crew to expose (D5.1).
    /// </summary>
    public static bool CrewExposed(CrewExposedFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.ButtonedUp != RuleState.True && !facts.Stunned && !facts.Recalled && !facts.Shocked && !facts.UnconfirmedKill
            && (!facts.ClosedTopped || facts.ButtonedUp == RuleState.False);
    }

    /// <summary>A vehicle in the target Location, with its crew's state (A7.307, A7.308, D.8B): Stunned covers Recalled, Concealed covers Hidden.</summary>
    public static FireVehicle VehicleTarget(VehicleTargetFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new FireVehicle(facts.Id, facts.Definition, facts.At, facts.CrewExposed, facts.Stunned || facts.Recalled, facts.StunRecovery, facts.Immobilized)
        {
            Concealed = facts.Concealed || facts.Hidden ? true : null,
        };
    }

    /// <summary>
    /// A vehicle's MA MG attack (ruling R25.7): its crew's state, Motion (D2.42; under a Motion counter or moving unstopped), a pin (A7.82), Stunned
    /// covering Recalled, Shocked, and Unconfirmed Kill, its MG's malfunction (D3.7), whether it fired this Player Turn under any fire counter, and
    /// whether its last shot this phase kept its Multiple ROF (C2.24).
    /// </summary>
    public static FireVehicleFire VehicleFirer(VehicleFirerFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new FireVehicleFire(facts.Id, facts.Definition, facts.At, facts.CrewExposed,
            facts.Motion || facts.MovingUnstopped,
            facts.Pinned,
            facts.Stunned || facts.Recalled || facts.Shocked || facts.UnconfirmedKill,
            facts.StunRecovery, facts.Malfunctioned,
            facts.Fired || facts.FirstFire || facts.BoundingFire,
            facts.RateOfFireKept);
    }

    /// <summary>
    /// A unit in the target Location, with the Fanatic (A10.8) and heroic (A15.21) states the Fire package reads, its Inexperience (A19.2; ruling
    /// R15.10), and the FT it possesses (A22.4; ruling R15.1). A unit of an OB group takes its group's ELR (ruling R18.3).
    /// </summary>
    public static FireTarget Target(TargetUnitFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new FireTarget(facts.Id, facts.Definition, facts.At, facts.Broken, facts.Pinned, facts.Concealed, facts.Hidden, facts.Dummy, facts.Wounded, facts.Disrupted)
        {
            Elr = facts.Elr,
            Fanatic = facts.Fanatic ? true : null,
            Heroic = facts.Heroic ? true : null,
            Berserk = facts.Berserk ? true : null,
            Inexperienced = facts.Inexperienced,
            Flamethrowers = facts.Flamethrowers,
        };
    }

    /// <summary>A7.1: a unit fires in one fire phase per Player Turn; A7.531: a directing leader is marked too. Fired when marked Prep Fire or Final Fire.</summary>
    public static bool Fired(bool prepFire, bool finalFire) => prepFire || finalFire;

    /// <summary>
    /// Why a SW may not be chosen to fire now, in a few words, or null when it may (pass 31d, design D10; A9.2, A9.7): it has malfunctioned, or it
    /// carries a Prep Fire or Final Fire counter. A First Fire counter does not bar it (A8.3, A8.4).
    /// </summary>
    public static string? WeaponBar(bool malfunctioned, bool fired) => malfunctioned ? "has malfunctioned" : fired ? "has fired" : null;

    /// <summary>
    /// Whether a unit has spent its fire for this phase, as the Fire package reads it for the PFPh, AFPh, and DFPh (A7.1, A8.4): marked Prep or Final
    /// Fire, unless in the DFPh it is marked First Fire; a vehicle's MG fires again only on the Multiple ROF its last shot kept (C2.24, D3.5); a
    /// squad whose only fire is one SW use still fires its inherent FP, and an unfired MG, ATR, or FT fires on (A9.2, A7.351, A22.3). The MPh is not
    /// read here.
    /// </summary>
    public static bool FireSpent(FireSpentFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase is not ("pfph" or "afph" or "dfph") || !facts.Fired || (facts.Phase == "dfph" && facts.FirstFire))
        {
            return false;
        }

        if (facts.Vehicle)
        {
            return !facts.RateOfFireKept();
        }

        return !facts.SupportWeaponUsed() && !facts.UnfiredWeaponHeld();
    }

    /// <summary>The US# of a unit for a Guard's fire (A20.52): a squad three, a HS or crew two, any other unit one.</summary>
    public static int GuardSize(string kind) => kind == "asl:squad" ? 3 : kind is "asl:half-squad" or "asl:crew" ? 2 : 1;

    /// <summary>
    /// Why a unit may not fire or direct fire now, or null: a berserk unit never fires in its PFPh and directs no fire (A15.432, A15.42; ruling
    /// R12.10); an Opportunity Firer fires in the AFPh (A7.25; ruling R12.1); a unit held in Melee fires only in CC (A11.15); a prisoner does not
    /// fire (A20.5); and a Guard whose US# is less than its prisoners' fires only at them (A20.52; ruling R12.9).
    /// </summary>
    public static string? FireBar(FireBarFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var prisoners = facts.PrisonerKinds.Sum(GuardSize);
        return facts.Berserk && facts.Phase == "pfph" && facts.Side == facts.PhasingSide ? "is berserk and never fires in its PFPh (A15.432)"
            : facts.BoundingFire && facts.Phase == "pfph" && !facts.Vehicle ? "is an Opportunity Firer and fires in the AFPh (A7.25)"
            : facts.Melee ? "is held in Melee and fires only in CC (A11.15)"
            : facts.Captured ? "is a prisoner and does not fire (A20.5)"
            : facts.Unarmed ? "is Unarmed, and its FP is used only in CC (A20.5)"
            : prisoners > GuardSize(facts.Kind) ? "guards prisoners whose US# exceeds its own, so it attacks only them (A20.52)"
            : null;
    }

    /// <summary>The usable MGs and ATR a unit possesses, in id order (table player, pass 9b: a mortar or PSK takes no part in its fire groups); <paramref name="possessed"/> is every active weapon it possesses.</summary>
    public static string[] UsableMgs(IEnumerable<PossessedWeaponFacts> possessed)
    {
        ArgumentNullException.ThrowIfNull(possessed);
        return [.. possessed.Where(weapon => !weapon.Malfunctioned && !weapon.Dismantled && (weapon.Kind == "asl:mg" || weapon.LatwType() == "atr"))
            .Select(weapon => weapon.Id).Order(StringComparer.Ordinal)];
    }
}
