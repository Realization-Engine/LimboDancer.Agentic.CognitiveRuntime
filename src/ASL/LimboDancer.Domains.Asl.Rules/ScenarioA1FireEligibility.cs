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

    // The planner's checks of a proposed fire attack (GamePlanner.PlanFire of Play, blocks 2 to 23; pass 32.c): one function a block, called in the
    // old order. The parsing of the request and the reads of the state, the map, and the record stay in Play.

    /// <summary>A22.3 (table player, pass 15): a FT fires apart from its user's inherent FP, so a firer naming one fires without it, as does a firer the request names.</summary>
    public static string[] FiresWithoutInherent(IEnumerable<string> withoutInherent, IEnumerable<FirerFtFacts> weapons)
    {
        ArgumentNullException.ThrowIfNull(withoutInherent);
        ArgumentNullException.ThrowIfNull(weapons);
        return [.. withoutInherent.Concat(weapons.Where(entry => entry.NamesFt).Select(entry => entry.Firer)).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>A22.6, A22.611 (backlog pass 15, ruling R15.4): a MOL Check by one firer, where an SSR gives its side MOL, none after a Defensive First Fire check, by a unit that is Good Order or berserk.</summary>
    public static string? MolBar(MolUserFacts user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!user.IsFirer || !user.Found)
        {
            return "play.fire-mol: the MOL user is one of the firers (A22.611)";
        }

        if (!user.SsrGivesMol())
        {
            return $"play.fire-mol: no SSR gives the {user.Side} side MOL (A22.6; an SSR mol:{user.Side})";
        }

        if (user.Phase == "dfph" && user.CheckedInFirstFire())
        {
            return $"play.fire-mol: {user.Id} made a MOL Check in Defensive First Fire and makes none in Final Fire (A22.611)";
        }

        return user.Broken || user.Captured || user.Melee ? $"play.fire-mol: {user.Id} is not Good Order or berserk, and uses no MOL (A22.61)" : null;
    }

    /// <summary>Whether a unit made a MOL Check in Defensive First Fire this Player Turn (A22.611; ruling R15.4): the record read from its end, each event when asked, to the last RPh.</summary>
    public static bool MolCheckedInFirstFire(int events, Func<int, MolEventFacts> eventAt)
    {
        ArgumentNullException.ThrowIfNull(eventAt);
        for (var index = events - 1; index >= 0; index--)
        {
            var facts = eventAt(index);
            if (facts.RallyPhase)
            {
                return false;
            }

            if (facts.MolCheckByUnit)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>D3.3 (ruling R6.9): the moving vehicle's Bounding First Fire; A8.1, A8.11: otherwise in the MPh, Defensive fire answers the moving stack's MF expenditure, in its Location.</summary>
    public static (bool Bounding, string? Refusal) MphWindow(string? phase, int firers, Func<bool> mayBoundingFire, bool windowOpenAtTarget)
    {
        ArgumentNullException.ThrowIfNull(mayBoundingFire);
        var bounding = phase == "mph" && firers == 1 && mayBoundingFire();
        return (bounding, phase == "mph" && !bounding && !windowOpenAtTarget
            ? "play.fire-window: Defensive First Fire attacks the moving stack in its Location, while the DEFENDER's window on its MF expenditure is open (A8.1, A8.11)"
            : null);
    }

    /// <summary>A22.611 (ruling R15.4): no MOL through a woods or orchard hexside, one both of whose hexes are woods, or both orchard. The terrains are null when the MOL user is in the target Location or a Location cannot be read.</summary>
    public static string? MolHexsideBar(string? molTerrain, string? targetTerrain) =>
        molTerrain is "woods" or "orchard" && molTerrain == targetTerrain ? $"play.fire-mol: a MOL is not thrown through a {molTerrain} hexside (A22.611)" : null;

    /// <summary>A9.22, A9.223 (referee, pass 12): a MG with a Fire Lane does not fire again this MPh, nor does its manning Infantry use Subsequent First Fire or FPF.</summary>
    public static string? FireLaneInUseBar(IEnumerable<(string Weapon, string Operator)> lanes, FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(lanes);
        ArgumentNullException.ThrowIfNull(attack);
        return lanes.FirstOrDefault(lane => attack.Firers!.Any(item => item.Weapons?.Any(weapon => weapon.EquipmentId == lane.Weapon) == true
            || (item.UnitId == lane.Operator && attack.FireKind is ScenarioA1FireCalculator.SubsequentFirstFire or ScenarioA1FireCalculator.FinalProtectiveFire))) is { Weapon: { } inUse }
            ? $"play.fire-lane-mg: {inUse} has a Fire Lane and fires again only in the DFPh (A9.22, A9.223)"
            : null;
    }

    /// <summary>A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.</summary>
    public static string? LeaderMgBar(IEnumerable<LeaderMgFacts> firers)
    {
        ArgumentNullException.ThrowIfNull(firers);
        foreach (var leader in firers.Where(item => item.Leader))
        {
            if (leader.DirectedThisPhase() || leader.DirectedSupportWeapon() || leader.FiredAnotherMg())
            {
                return $"play.fire-smc: {leader.Id} directed fire or fired another MG this phase, and fires one MG only without leading (A9.12)";
            }
        }

        return null;
    }

    /// <summary>A15.42 (ruling R12.10): a berserk leader gives no leadership, so directs no fire.</summary>
    public static string? BerserkDirectorBar(IEnumerable<(string Id, bool Berserk)> directors)
    {
        ArgumentNullException.ThrowIfNull(directors);
        return directors.FirstOrDefault(item => item.Berserk) is { Id: { } berserkLeader } ? $"play.fire-barred: {berserkLeader} is berserk and gives no leadership (A15.42)" : null;
    }

    /// <summary>D2.4: a vehicle under a Motion counter may not Prep Fire.</summary>
    public static string? MotionPrepFireBar(FireVehicleFire? vehicleFire, string? phase) =>
        vehicleFire is { InMotion: true } inMotion && phase == "pfph" ? $"play.fire-vehicle-motion: {inMotion.VehicleId} is in Motion and may not Prep Fire (D2.4)" : null;

    /// <summary>A15.432, A11.15, A20.52, A20.54: the first firer or director that may not fire refuses the attack (<see cref="FireBar"/>); the facts are read as the units are tried.</summary>
    public static string? GroupFireBar(IEnumerable<(string Id, FireBarFacts Facts)> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return units.Select(item => (item.Id, Cause: FireBar(item.Facts))).FirstOrDefault(item => item.Cause is not null) is ({ } barred, { } cause)
            ? $"play.fire-barred: {barred} {cause}"
            : null;
    }

    /// <summary>A11.15, A20.54 (rulings R12.8, R12.9): a Location holding units in Melee or prisoners is fired at from outside it, and every unit there is attacked.</summary>
    public static string? MeleeLocationBar(bool meleeOrPrisonersAtTarget, bool firerInTarget) =>
        meleeOrPrisonersAtTarget && firerInTarget ? "play.fire-melee: units fire into a Melee or prisoners' Location only from outside it (A11.15, A20.54)" : null;

    /// <summary>A8.3, A8.31: Subsequent First Fire and FPF use every usable MG the firer possesses; <paramref name="possessed"/> reads a firer's usable MGs in id order.</summary>
    public static string? EveryMgBar(FireAttack attack, Func<string, string[]> possessed)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(possessed);
        return attack.FireKind is ScenarioA1FireCalculator.SubsequentFirstFire or ScenarioA1FireCalculator.FinalProtectiveFire
            && attack.Firers!.Any(item => !(item.Weapons?.Select(weapon => weapon.EquipmentId!).Order(StringComparer.Ordinal).ToArray() ?? [])
                .SequenceEqual(possessed(item.UnitId!)))
            ? "play.fire-weapons: Subsequent First Fire and FPF use every MG the firer possesses (A8.3, A8.31)"
            : null;
    }

    /// <summary>The target side of a proposal: the first target's side, else the first vehicle target's, else the other side of the game (read only then).</summary>
    public static string ProposalTargetSide(bool hasTargets, string? firstTargetSide, bool hasVehicles, string? firstVehicleSide, Func<string> otherSide)
    {
        ArgumentNullException.ThrowIfNull(otherSide);
        return hasTargets ? firstTargetSide! : hasVehicles ? firstVehicleSide! : otherSide();
    }

    /// <summary>Whether the firing side sees nothing, or not everything, at the target, so its refusals say only that the attack is undecided; a concealed vehicle is unseen too (ruling R6.7).</summary>
    public static bool Unseen(int seen, int targets, bool anyVehicles, bool concealedVehicle) => seen < targets || (seen == 0 && !anyVehicles) || concealedVehicle;

    /// <summary>The reasons the firing side reads for a proposal: the one undisclosed sentence when the attack is undecided and the target is unseen, else none.</summary>
    public static IReadOnlyList<string> FiringSideReasons(bool undecided, bool unseen) => undecided && unseen ? [FireProposalUndisclosed] : [];

    /// <summary>The sentence the firing side reads when the Fire package refuses an attack for reasons about units it cannot see.</summary>
    public const string FireProposalUndisclosed =
        "play.fire-refused: the Fire package does not decide every outcome of an attack on this Location, for reasons about units the firing side cannot see";

    /// <summary>A7.55: the step a MPh attack answers; null in a fire phase and for Bounding First Fire.</summary>
    public static int? MphStep(string? phase, bool bounding, int? movementStep) => phase == "mph" && !bounding ? movementStep : null;

    /// <summary>
    /// A7.55: the units of a Location that fire at a target in a phase (in the MPh, at one MF expenditure) form one fire group, so it fires once; a MG
    /// firing again alone on its Multiple ROF is not a new group.
    /// </summary>
    public static string? FireGroupBar(bool firesAlone, IEnumerable<PhaseFireFacts> fires, FireAttack attack, IReadOnlyList<string> fromLocations, int? step)
    {
        ArgumentNullException.ThrowIfNull(fires);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(fromLocations);
        return !firesAlone && fires.Any(record => !(attack.VehicleFire is not null && record.Vehicle)
            && (fromLocations.Contains(record.FirerLocation) || record.FirerLocation == attack.FirerLocationId)
            && record.TargetLocation == attack.TargetLocationId && record.Step == step)
            ? $"play.fire-group: a Location of the group has already fired at {attack.TargetLocationId}{(step is null ? " this phase" : " at this MF expenditure")}, and its units fire as one fire group (A7.55, p. 57)"
            : null;
    }

    /// <summary>
    /// A8.3, A9.2: a unit or MG fires at a moving stack in a Location no more often than the MF the stack spent entering it (FRD, at least once); the
    /// count is of this stack's move alone, a unit and each weapon it fires counted apart (pass 31, play test P-05; ruling R31.1). <paramref name="recordedFirers"/>
    /// are the firers of the attacks made at this step since the move's first step.
    /// </summary>
    public static string? MfLimitBar(int halfMfInLocation, string location, IEnumerable<FireFirer> recordedFirers, FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(recordedFirers);
        ArgumentNullException.ThrowIfNull(attack);
        var limit = Math.Max(1, halfMfInLocation / 2);
        var fired = recordedFirers.SelectMany(ScenarioA1ResultTables.FiringParts).GroupBy(id => id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return attack.Firers!.SelectMany(ScenarioA1ResultTables.FiringParts).FirstOrDefault(id => fired.GetValueOrDefault(id) >= limit) is { } spent
            ? $"play.fire-mf-limit: {spent} has attacked this moving stack in {location} {(limit == 1 ? "once" : $"{limit} times")} already, as often as the MF the stack spent there (A8.3, A9.2)"
            : null;
    }

    /// <summary>C11 (ruling R8.3): Infantry fire, not Residual FP, at a Location takes the gunshield or Emplacement of a Gun's crew alone there, which the caller then reads.</summary>
    public static bool InfantryFireAtGun(FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        return attack.VehicleFire is null && attack.FireKind != ScenarioA1FireCalculator.ResidualFire && attack.Firers is { Count: > 0 };
    }

    /// <summary>
    /// A6.11, A7.52 (ruling R12.2): in a group spanning Locations with some but not all LOS blocked, the firers whose LOS is blocked make their DR
    /// first and drop out; the others attack as a smaller group. The Locations whose firers are blocked, or null when the group is not split.
    /// </summary>
    public static IReadOnlySet<string>? BlockedLocations(FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        if (attack.Firers is { Count: > 1 } everyFirer && everyFirer.Select(item => item.LocationId).Distinct().Count() > 1
            && everyFirer.Count(item => (item.Los ?? attack.Los)?.Blocked == true) is var blockedCount && blockedCount > 0 && blockedCount < everyFirer.Count)
        {
            return everyFirer.Where(item => (item.Los ?? attack.Los)?.Blocked == true).Select(item => item.LocationId!).ToHashSet(StringComparer.Ordinal);
        }

        return null;
    }

    /// <summary>The firers of one part of a split group: those in the blocked Locations, or those outside them.</summary>
    public static string[] PartFirers(IReadOnlyList<FireFirer> firers, IReadOnlySet<string> blockedLocations, bool blockedPart)
    {
        ArgumentNullException.ThrowIfNull(firers);
        ArgumentNullException.ThrowIfNull(blockedLocations);
        return [.. firers.Where(item => blockedLocations.Contains(item.LocationId!) == blockedPart).Select(item => item.UnitId!)];
    }

    /// <summary>The directors of one part of a split group, by their Locations (null for a director with none).</summary>
    public static string[] PartDirectors(IEnumerable<(string Id, string? Location)> directors, IReadOnlySet<string> blockedLocations, bool blockedPart)
    {
        ArgumentNullException.ThrowIfNull(directors);
        ArgumentNullException.ThrowIfNull(blockedLocations);
        return [.. directors.Where(item => item.Location is { } at && blockedLocations.Contains(at) == blockedPart).Select(item => item.Id)];
    }

    /// <summary>A9.5 (ruling R12.6): Spraying Fire attacks two Locations that share a hexside, at the same level.</summary>
    public static string? SprayTargetBar(bool parsed, bool sameAsTarget, bool sameLevel, bool sharesHexside) =>
        !parsed || sameAsTarget || !sameLevel || !sharesHexside ? "play.fire-spray: Spraying Fire attacks two Locations that share a hexside (A9.5)" : null;

    /// <summary>A9.5 (ruling R12.6): Spraying Fire is made in the PFPh, AFPh, or DFPh, by a group that can see both Locations.</summary>
    public static string? SprayPhaseBar(string? phase, bool blockedFirst) =>
        phase is not ("pfph" or "afph" or "dfph") || blockedFirst
            ? "play.fire-spray: Spraying Fire is made in the PFPh, AFPh, or DFPh, by a group that can see both Locations (A9.5; ruling R12.6)"
            : null;

    /// <summary>A22.611: a MOL goes with a PBF or TPBF attack at one Location.</summary>
    public static string? SprayMolBar(bool mol) => mol ? "play.fire-mol: a MOL goes with a PBF or TPBF attack at one Location (A22.611)" : null;

    /// <summary>A7.55, A9.52: a Location of the group that has fired at the second Location this phase sprays no more at it.</summary>
    public static string? SprayGroupBar(IEnumerable<PhaseFireFacts> fires, IReadOnlyList<string> fromLocations, string second)
    {
        ArgumentNullException.ThrowIfNull(fires);
        ArgumentNullException.ThrowIfNull(fromLocations);
        return fires.Any(record => fromLocations.Contains(record.FirerLocation) && record.TargetLocation == second && record.Step is null)
            ? $"play.fire-group: a Location of the group has already fired at {second} this phase (A7.55, A9.52)"
            : null;
    }

    /// <summary>A9.52: a First-Fire-marked unit sprays in Final Fire only at two ADJACENT Locations.</summary>
    public static string? SprayAdjacentBar(string? phase, FireAttack attack, FireAttack spray)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(spray);
        return phase == "dfph" && attack.Firers!.Any(item => item.FirstFireMarked == true)
            && new[] { attack, spray }.Any(one => one.Firers!.Any(item => (item.Range ?? one.Range) > 1))
            ? "play.fire-spray: a First-Fire-marked unit sprays in Final Fire only at two ADJACENT Locations (A9.52)"
            : null;
    }

    /// <summary>A7.7 (ruling R12.11): the targets of the sealed side (and units in Melee), not Dummy, berserk, heroic, a hero, or guarded, are marked Encircled against this attack.</summary>
    public static FireAttack MarkEncircled(FireAttack attack, string encircles, Func<string, EncircledUnitFacts> unitOf)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(unitOf);
        return attack with
        {
            Targets = [.. attack.Targets!.Select(item => unitOf(item.UnitId!) is { Found: true } unit && item.Dummy != true && item.Berserk != true && item.Heroic != true
                && unit.Kind != "asl:hero" && item.GuardId is null && (unit.Side == encircles || unit.Melee) ? item with { Encircled = true } : item)],
        };
    }

    /// <summary>
    /// A9.22 (ruling R12.7): a Fire Lane goes with MPh Defensive First Fire by an unpinned Infantry unit's Good Order MG, within its Normal Range at a
    /// same-level target, not TPBF or a Snap Shot.
    /// </summary>
    public static string? FireLaneDeclarationBar(FireLaneDeclarationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var range = facts.Manning?.Range ?? facts.Attack.Range;
        return facts.Phase != "mph" || facts.Attack.FireKind != ScenarioA1FireCalculator.FirstFire || facts.Manning is null || !facts.MgIsMg
            || facts.MgRange is not { } mgRange || facts.MgFirepower is null
            || facts.Manning.Pinned == true || facts.Mg!.Malfunctioned == true || range is not { } distance || distance < 1 || distance > mgRange
            || (facts.Manning.SameLevel ?? facts.Attack.SameLevel) != true || facts.Attack.SnapShot == true || !facts.MgLocationRead
            ? "play.fire-lane: a Fire Lane goes with Defensive First Fire by an unpinned Infantry unit's Good Order MG, within its Normal Range at a same-level target, not TPBF or a Snap Shot (A9.22)"
            : null;
    }

    /// <summary>
    /// A12.14 (pass 31d, ruling R31d.2): the concealed firers and directors this attack reveals: with the planner's read for every concealed unit of the
    /// group, the units a Good Order enemy unit sees; without it, as the package decided before: every concealed one when every firer is within 16
    /// hexes and a target is Good Order. <paramref name="concealed"/> reads whether a unit is concealed, for the second way only.
    /// </summary>
    public static string[] RevealedByAttack(FireAttack attack, Func<string, bool> concealed)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(concealed);
        var firers = attack.Firers ?? [];
        var directing = new[] { attack.Director }.Concat(attack.OtherDirectors ?? []).OfType<FireDirector>().ToArray();
        var read = firers.Where(item => item.Concealed == true).All(item => item.SeenByGoodOrderEnemy is not null) && directing.Where(item => item.Concealed == true).All(item => item.SeenByGoodOrderEnemy is not null);
        return read
            ? [.. firers.Where(item => item.SeenByGoodOrderEnemy == true).Select(item => item.UnitId).Concat(directing.Where(item => item.SeenByGoodOrderEnemy == true).Select(item => item.UnitId)).OfType<string>()]
            : firers.Count > 0 && firers.All(item => (item.Range ?? attack.Range) <= 16) && attack.Targets!.Any(item => item.Broken == false && item.Dummy != true)
            ? [.. firers.Select(item => item.UnitId).Concat(new[] { attack.Director?.UnitId }).Concat((attack.OtherDirectors ?? []).Select(item => item.UnitId))
                .OfType<string>().Where(concealed)]
            : [];
    }

    /// <summary>A15.44: a target's Known enemy in LOS is settled by this attack's reveal; otherwise the map is read.</summary>
    public static bool? KnownEnemyInLosAfterReveal(bool revealedAny, Func<bool?> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        return revealedAny ? true : read();
    }

    /// <summary>A15.44, A15.5: the Heat of Battle reads are made for every target, and for the firers of FPF alone.</summary>
    public static bool FirersTakeHeatOfBattleReads(string? fireKind) => fireKind == ScenarioA1FireCalculator.FinalProtectiveFire;

    // Opportunity Fire (GamePlanner.PlanOpportunityFire of Play; pass 32.c).

    /// <summary>A7.25 (ruling R12.1): Opportunity Fire is declared in the PFPh.</summary>
    public static string? OpportunityFirePhaseBar(string? phase) => phase != "pfph" ? "play.opportunity-fire-phase: Opportunity Fire is declared in the PFPh (A7.25)" : null;

    /// <summary>A7.25, A15.432 (ruling R12.1): a unit declared for Opportunity Fire is Good Order Infantry of the phasing side on the map, not berserk, TI, in Melee, or a prisoner, that has not fired or directed fire this Player Turn.</summary>
    public static string? OpportunityFirerBar(OpportunityFirerFacts unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!unit.ActiveOnMap || !unit.PhasingSide || unit.Vehicle || !unit.Personnel || unit.Dummy)
        {
            return $"play.opportunity-fire: '{unit.Id}' is not Infantry of the phasing side on the map (A7.25)";
        }

        if (unit.Broken || unit.Berserk || unit.Melee || unit.Captured || unit.Ti)
        {
            return $"play.opportunity-fire: {unit.Id} is not Good Order, or is berserk, TI, in Melee, or a prisoner (A7.25, A15.432)";
        }

        return unit.Fired || unit.BoundingFire || unit.PhaseFirer || unit.SupportWeaponUser || unit.SupportWeaponDirector
            ? $"play.opportunity-fire: {unit.Id} has already fired or directed fire this Player Turn (A7.25)"
            : null;
    }

    /// <summary>A7.25, A12.14 (Case D): a concealed or hidden Opportunity Firer in the LOS of a Good Order enemy ground unit within 16 hexes loses its "?"; <paramref name="seen"/> is read for the concealed units only.</summary>
    public static IReadOnlyList<string> OpportunityFireReveals(IEnumerable<(string Id, bool ConcealedOrHidden, Func<bool> Seen)> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return [.. units.Where(unit => unit.ConcealedOrHidden && unit.Seen()).Select(unit => unit.Id)];
    }

    /// <summary>The sentence of an Opportunity Fire declaration, with the units that lose their "?".</summary>
    public static string OpportunityFireSummary(IReadOnlyList<string> ids, IReadOnlyList<string> revealed)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(revealed);
        return $"play.opportunity-fire: {string.Join(", ", ids)} hold their fire for the AFPh under a Bounding Fire counter, and do not move in the MPh (A7.25)"
            + (revealed.Count > 0 ? $"; {string.Join(", ", revealed)} loses its \"?\" (Case D)" : string.Empty);
    }
}
