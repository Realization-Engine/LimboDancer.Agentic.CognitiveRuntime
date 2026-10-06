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

    // The state's part of a fire group's attack (LiveFire.FromState of Play, unit steps 18 to 23; pass 32.c): one function a block, called in the
    // old order. The caller parses the request, reads the state and the catalog, and hands the facts over.

    /// <summary>The name of a phase as the Fire package reads it: PFPh, DFPh, AFPh, MPh; any other phase as it stands.</summary>
    public static string? PhaseName(string? phase) => phase switch
    {
        "pfph" => "PFPh",
        "dfph" => "DFPh",
        "afph" => "AFPh",
        "mph" => "MPh",
        _ => phase,
    };

    /// <summary>D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group.</summary>
    public static string? VehicleFiresAlone(int vehicleFirers, int firers, int directors, bool weaponsNamed) =>
        vehicleFirers > 0 && (firers > 1 || directors > 0 || weaponsNamed)
            ? "play.fire-vehicle-group: a vehicle's MG fires alone, with no leader or Infantry in its fire group (D3.4)"
            : null;

    /// <summary>A firer is an active catalog unit on the map, and a TI unit does not fire (A4.8, C10.3; table player, pass 8).</summary>
    public static string? FirerBar(FirerStateFacts firer)
    {
        ArgumentNullException.ThrowIfNull(firer);
        if (!firer.ActiveWithDefinition || !firer.OnMap)
        {
            return $"play.fire-firers: '{firer.Id}' is not an active unit from the catalog on the map";
        }

        return firer.Ti ? $"play.fire-ti: '{firer.Id}' is TI and does not fire this Player Turn (A4.8, C10.3)" : null;
    }

    /// <summary>A directing leader is an active catalog unit in a Location of the fire group.</summary>
    public static string? DirectorBar(DirectorStateFacts director)
    {
        ArgumentNullException.ThrowIfNull(director);
        return !director.ActiveWithDefinition || director.InGroupLocation != true
            ? $"play.fire-director: '{director.Id}' is not an active unit in a Location of the fire group"
            : null;
    }

    /// <summary>
    /// The kind of fire in the MPh (D3.3, A8.1, A8.3, A8.31; rulings R6.9, R12.3): the DEFENDER's vehicle fires as Defensive First Fire, the
    /// moving vehicle as Bounding First Fire once the DEFENDER has passed; Infantry by their markers, where FPF firers may group with other
    /// Defensive fire but First Fire and Subsequent First Fire firers may not. Null kind outside the MPh.
    /// </summary>
    public static (string? Refusal, string? Kind) MphFireKind(string? phase, FireKindFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (phase == "mph" && facts.VehicleFires)
        {
            if (!facts.VehicleIsPhasing)
            {
                return (null, ScenarioA1FireCalculator.FirstFire);
            }

            return facts.MayBoundingFire() ? (null, ScenarioA1FireCalculator.BoundingFirstFire)
                : ("play.fire-vehicle-phase: in its own MPh a vehicle fires only while it moves, as Bounding First Fire, after the DEFENDER has passed on its last MP expenditure (D3.3)", null);
        }

        if (phase != "mph")
        {
            return (null, null);
        }

        var marks = facts.Marks().Select(item => item.FinalFire ? 2 : item.FirstFire ? 1 : 0).Distinct().ToArray();
        if (marks.Length != 1 && !(marks.Contains(2) && !marks.Contains(0)) && facts.NoneFiresAlone)
        {
            return ("play.fire-kind: a group mixes firers marked for different kinds of Defensive fire (A8.3, A8.31)", null);
        }

        return (null, marks.Max() switch
        {
            0 => ScenarioA1FireCalculator.FirstFire,
            1 => facts.AnyFiresAlone ? ScenarioA1FireCalculator.FirstFire : ScenarioA1FireCalculator.SubsequentFirstFire,
            _ => ScenarioA1FireCalculator.FinalProtectiveFire,
        });
    }

    /// <summary>
    /// Who the attack hits (A8.1, A7.308, A15.41; ruling R21.1): every active unit in the target Location in id order, in the MPh the moving stack
    /// only; vehicles apart from Infantry; the target side's units not attacked (not moving) as a berserk leader's companions; a target outside the
    /// catalog refuses the attack.
    /// </summary>
    public static TargetSelection SelectTargets(IReadOnlyList<TargetUnitStateFacts> units, bool moversKnown)
    {
        ArgumentNullException.ThrowIfNull(units);
        var attacked = units.Where(unit => unit.Active && (!moversKnown || unit.Mover == true)).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var targets = attacked.Where(unit => !unit.Vehicle).ToArray();
        var vehicles = attacked.Where(unit => unit.Vehicle).ToArray();
        var companions = !moversKnown || targets.Length == 0 ? [] : units
            .Where(unit => unit.Active && unit.Mover != true && unit.Side == targets[0].Side && unit.HasDefinition && !unit.Vehicle)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var refusal = targets.Any(unit => !unit.HasDefinition && !unit.Dummy) ? "play.fire-target: the target Location holds a unit outside the catalog" : null;
        return new TargetSelection([.. targets.Select(unit => unit.Id)], [.. vehicles.Select(unit => unit.Id)], [.. companions.Select(unit => unit.Id)], refusal);
    }

    /// <summary>
    /// A firer's named weapons (A7.35, A9.8, A22.3, A7.351; rulings R13.6, R12.4): each possessed and not dismantled; one FT or DC a Player Turn;
    /// with inherent FP, no second SW after one fired or was used.
    /// </summary>
    public static string? NamedWeaponsBar(NamedWeaponsFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Named.Any(weapon => !weapon.PossessedByFirer))
        {
            return $"play.fire-weapon: every weapon '{facts.FirerId}' fires must be one it possesses (A7.35)";
        }

        if (facts.Named.FirstOrDefault(weapon => weapon.Dismantled) is { } dismantled)
        {
            return $"play.fire-weapon: {dismantled.Id} is dismantled and is not fired until it is assembled (A9.8)";
        }

        if (facts.Named.Any(weapon => weapon.Ft) && facts.AssaultWeaponUser)
        {
            return $"play.fire-ft-once: {facts.FirerId} has used a FT or DC this Player Turn and uses no other (A22.3)";
        }

        if (!facts.FiresAlone && facts.OtherPossessedFired())
        {
            return $"play.fire-sw-limit: {facts.FirerId} fired one MG this phase; with its inherent FP it fires no second SW (A7.351)";
        }

        return facts.SupportWeaponUsed() ? $"play.fire-sw-limit: {facts.FirerId} has used a SW this phase; with its inherent FP it fires no other (A7.351)" : null;
    }

    /// <summary>
    /// A unit whose SW fired alone in the PFPh has Prep Fired and does not fire in the AFPh (A3.3, A7.1); a squad fires no more than two SW in a
    /// phase, and no inherent FP with two (A7.351).
    /// </summary>
    public static string? FirerLimitBar(FirerLimitFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase == "afph" && !facts.BoundingFire && facts.PrepFiredWeaponHeld())
        {
            return $"play.fire-prep-fired: {facts.Id} fired a SW in the PFPh, so it has Prep Fired and does not fire in the AFPh (A3.3, A7.1)";
        }

        var otherFired = facts.OtherFired();
        return facts.Squad && ((!facts.FiresAlone && otherFired >= 2) || (facts.FiresAlone && facts.Fired && otherFired >= 1 && facts.NamesWeapons))
            ? $"play.fire-sw-limit: {facts.Id} fires no more than two SW in a phase, and no inherent FP with two (A7.351)"
            : null;
    }

    /// <summary>A leader's MG partner (A9.12; ruling R12.4): a Good Order SMC stacked with him that has not fired and is neither a firer nor a director.</summary>
    public static string? PartnerBar(string leaderId, PartnerFacts partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        return !partner.Active || !partner.LeaderOrHero || !partner.SameSide || !partner.SameLocation || partner.FirerOrDirector || partner.Fired || partner.Broken
            ? $"play.fire-partner: '{partner.Id}' is not a Good Order SMC stacked with {leaderId} that has not fired (A9.12)"
            : null;
    }

    /// <summary>
    /// An Infantry firer's record (A7.351, A7.352, A7.25, A22.611, A19.2, A19.32; rulings R12.1, R15.4, R15.10): fired unless its only fire is one
    /// SW use; First Fire marked the same; Final Fire marked in the MPh; a crew, HS, or SMC that fired a Gun loses its inherent FP, a squad does not;
    /// no inherent FP for a leader or a MG firing alone; an Opportunity Firer fires in the AFPh.
    /// </summary>
    public static FireFirer Firer(FirerRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new FireFirer(facts.Id, facts.Definition, facts.At, facts.Broken, facts.Pinned, facts.Concealed, facts.Fired && !facts.SwOnly, facts.Weapons is not null)
        {
            Elr = facts.Elr,
            FirstFireMarked = facts.FirstFire && !facts.SwOnly ? true : null,
            FinalFireMarked = facts.Phase == "mph" && facts.FinalFire ? true : null,
            GunFired = facts.GunCrewFired && !facts.Squad ? true : null,
            Weapons = facts.Weapons,
            UsesInherentFp = facts.FiresAlone || facts.Leader ? false : null,
            Fanatic = facts.Fanatic ? true : null,
            Wounded = facts.Wounded ? true : null,
            Cx = facts.Cx ? true : null,
            OpportunityFire = facts.Phase == "afph" && facts.BoundingFire ? true : null,
            Encircled = facts.Encircled ? true : null,
            Partner = facts.Partner,
            Mol = facts.Mol ? true : null,
            Inexperienced = facts.Inexperienced,
        };
    }

    /// <summary>A directing leader's record (A7.531): he has fired or directed when marked Prep, Final, or First Fire, or when he directed a SW use this phase.</summary>
    public static FireDirector Director(DirectorRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new FireDirector(facts.Id, facts.Definition, facts.At, facts.Broken, facts.Pinned, facts.Concealed,
            facts.Fired || facts.FirstFire || facts.DirectedSupportWeapon, facts.Wounded)
        {
            Cx = facts.Cx ? true : null,
        };
    }

    /// <summary>A target with the firing side's relation (rulings R12.8, R12.9, R12.11): the firing side's own unit as Friendly, a prisoner's Guard, and Encirclement.</summary>
    public static FireTarget AttackTarget(AttackTargetFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.Target with
        {
            Friendly = facts.SameSide ? true : null,
            GuardId = facts.Captured ? facts.Custodian : null,
            Encircled = facts.Encircled ? true : null,
        };
    }

    /// <summary>The target side of a fire group's attack: the first target of another side, else the first vehicle's, else the other side of the game.</summary>
    public static string? TargetSide(string? firingSide, IEnumerable<string?> targetSides, string? firstVehicleSide, string? otherSide)
    {
        ArgumentNullException.ThrowIfNull(targetSides);
        return targetSides.FirstOrDefault(side => side != firingSide) ?? firstVehicleSide ?? otherSide;
    }

    /// <summary>
    /// The attack of a fire group assembled (unit steps 18 to 23): the firing side's ELR and No Quarter go with FPF or its own units among the
    /// targets (A8.31, A10.7); the moving stack's Assault Movement goes with Defensive fire other than Bounding First Fire (A4.6).
    /// </summary>
    public static FireAttack Assemble(AttackAssemblyFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var ownSide = facts.Kind == ScenarioA1FireCalculator.FinalProtectiveFire || facts.FriendlyTargets;
        return new FireAttack(
            PhaseName(facts.Phase),
            facts.Phasing ? "phasing" : "non-phasing",
            true,
            facts.FirerLocation,
            facts.Target,
            facts.Firers,
            facts.Directors.Count == 0 ? null : facts.Directors[0],
            null,
            null,
            null,
            facts.ScenarioMonth,
            null,
            facts.Targets,
            facts.TargetSideElr,
            null)
        {
            FireKind = facts.Kind,
            TargetMovement = facts.Kind is null or ScenarioA1FireCalculator.BoundingFirstFire ? null : new FireMovement(facts.Assault),
            FiringSideElr = ownSide ? facts.FiringSideElr() : null,
            OtherDirectors = facts.Directors.Count > 1 ? [.. facts.Directors.Skip(1)] : null,
            Vehicles = facts.Vehicles.Count > 0 ? facts.Vehicles : null,
            VehicleFire = facts.VehicleFire,
            TargetSideNoQuarter = facts.TargetSideNoQuarter ? true : null,
            FiringSideNoQuarter = ownSide && facts.FiringSideNoQuarter ? true : null,
            FiringNationalities = facts.FiringNationalities,
            Companions = facts.Companions.Count > 0 ? facts.Companions : null,
        };
    }

    /// <summary>
    /// The state's part of a Residual FP attack on the moving stack as it enters a Location (A8.2, A8.22, A8.222; ruling R6.6): no firers, the
    /// counter's FP, the Infantry movers as targets, moving vehicles on the Vehicle line, the non-movers of the phasing side as companions.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) Residual(ResidualFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.MovementAtTarget)
        {
            return (null, "play.fire-residual: Residual FP attacks the moving stack in its Location (A8.2)");
        }

        var targets = facts.Units.Where(unit => unit.Active && unit.Mover && !unit.Vehicle).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var vehicles = facts.Units.Where(unit => unit.Active && unit.Mover && unit.Vehicle).ToArray();
        var companions = facts.Units.Where(unit => unit.Active && !unit.Mover && unit.Side == facts.PhasingSide && unit.HasDefinition && !unit.Vehicle)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        return (new FireAttack("MPh", "non-phasing", null, null, facts.Target, null, null, null, null, null, facts.ScenarioMonth, null,
            [.. targets.Select(unit => facts.TargetOf(unit.Id))],
            facts.PhasingSideElr, null)
        {
            FireKind = ScenarioA1FireCalculator.ResidualFire,
            TargetMovement = new FireMovement(facts.Assault),
            ResidualFp = facts.Fp,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => facts.VehicleOf(unit.Id))] : null,
            Companions = companions.Length > 0 ? [.. companions.Select(unit => facts.TargetOf(unit.Id))] : null,
            TargetSideNoQuarter = facts.PhasingSideNoQuarter ? true : null,
        }, null);
    }

    /// <summary>
    /// A vehicle's OVR of its Location (D7.1, D7.11; ruling R11.11): by an active or wrecked vehicle on the map (a vehicle destroyed before its
    /// declared OVR resolves still makes it, at half FP); every non-captured enemy unit there is attacked, Infantry as targets and vehicles on the
    /// Vehicle line; the vehicle is Immobile when Immobilized, Bogged, or Wrecked.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) Overrun(OverrunFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.VehicleFound || !facts.IsVehicle || !facts.OnMap || !facts.HasDefinition)
        {
            return (null, "play.overrun: an OVR is made by a vehicle on the map (D7.1)");
        }

        var enemies = facts.Units.Where(unit => unit.Active && unit.Side != facts.Side && !unit.Captured).OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var immobile = facts.Immobilized || facts.Bogged || facts.Wrecked;
        var vehicles = enemies.Where(unit => unit.Vehicle).ToArray();
        return (new FireAttack("MPh", "phasing", null, facts.At, facts.At, null, null, 0, true, null, facts.ScenarioMonth, null,
            [.. enemies.Where(unit => !unit.Vehicle).Select(unit => facts.TargetOf(unit.Id))],
            facts.TargetSideElr, null)
        {
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            Overrun = new FireOverrun(facts.VehicleId, facts.Definition!, facts.At, facts.CrewExposed, immobile,
                facts.MainMalfunctioned || facts.Disabled, facts.BmgMalfunctioned, facts.CmgMalfunctioned),
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => facts.VehicleOf(unit.Id))] : null,
            TargetSideNoQuarter = facts.TargetSideNoQuarter ? true : null,
        }, null);
    }

    /// <summary>
    /// The state's part of a DC's attack (A23; rulings R15.2, R15.3): through the unit that Placed it (an operable placement) or the unit that
    /// possesses it; every unit of the target Location, in the MPh only the movers and the user's side; First Fire in the MPh unless the thrower's
    /// Location is attacked; CX and concealment from the placement when Placed, from the user now when Thrown; captured use; Opportunity use in the AFPh.
    /// </summary>
    public static (FireAttack? Attack, string? Reason) DemolitionCharge(DemolitionChargeFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.ChargeActiveWithDefinition)
        {
            return (null, $"play.dc: '{facts.ChargeId}' is not a DC in play");
        }

        var placed = facts.Mode == FireDemolitionCharge.Placed;
        var placement = placed && facts.PlacementOperable;
        var userId = placement ? facts.PlacementUnit : facts.PossessorId;
        if (userId is null || !facts.UserFoundWithDefinition || (placed && !placement))
        {
            return (null, $"play.dc: '{facts.ChargeId}' has no unit to Place or Throw it");
        }

        var attacked = facts.Units.Where(unit => unit.Active && (!facts.MovementAtTarget || unit.Mover || unit.Side == facts.UserSide))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        var targets = attacked.Where(unit => !unit.Vehicle).ToArray();
        var vehicles = attacked.Where(unit => unit.Vehicle).ToArray();
        var friendlyTargets = targets.Any(unit => unit.Side == facts.UserSide);
        var kind = facts.Phase == "mph" && facts.Mode != FireDemolitionCharge.Thrower ? ScenarioA1FireCalculator.FirstFire : null;
        return (new FireAttack(PhaseName(facts.Phase), facts.Phasing ? "phasing" : "non-phasing", null, facts.UserLocation ?? facts.Target,
            facts.Target, null, null, null, null, null, facts.ScenarioMonth, null,
            [.. targets.Select(unit => facts.TargetOf(unit.Id))],
            facts.TargetSideElr, null)
        {
            FireKind = kind,
            TargetMovement = kind is null ? null : new FireMovement(facts.Assault),
            FiringSideElr = friendlyTargets ? facts.UserSideElr : null,
            Vehicles = vehicles.Length > 0 ? [.. vehicles.Select(unit => facts.VehicleOf(unit.Id))] : null,
            TargetSideNoQuarter = facts.TargetSide is not null && facts.TargetSideNoQuarter ? true : null,
            FiringSideNoQuarter = friendlyTargets && facts.UserSideNoQuarter ? true : null,
            FiringNationalities = facts.FiringNationalities,
            DemolitionCharge = new FireDemolitionCharge(facts.ChargeId, facts.ChargeDefinition, facts.Mode, userId, facts.UserDefinition,
                placement ? facts.PlacementCx ?? facts.UserCx : facts.UserCx, facts.Captured == true,
                placed ? facts.PlacementTargetsConcealed : null,
                !placed && facts.Phase == "afph" && facts.UserBoundingFire ? true : null)
            {
                Inexperienced = facts.UserInexperienced,
            },
        }, null);
    }
}
