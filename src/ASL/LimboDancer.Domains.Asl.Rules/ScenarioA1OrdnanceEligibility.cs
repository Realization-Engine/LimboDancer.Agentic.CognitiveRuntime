namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Ordnance rules that Play decided over the state before pass 32.d (slice S7): what a live Gun's, mortar's, or PF's shot may be, from facts
/// Play reads. The recorded records of a shot keep their shape (the pass 32 design, D5); the map reads stay in Play.
/// </summary>
public static class ScenarioA1OrdnanceEligibility
{
    /// <summary>Whether a vehicle has a MA the Ordnance package reviews (D1.3; ruling R7.1): a tank of the catalog, its Gun type read only for a vehicle.</summary>
    public static bool IsTank(bool vehicle, Func<string?> gunType)
    {
        ArgumentNullException.ThrowIfNull(gunType);
        return vehicle && gunType() == "vehicle";
    }

    /// <summary>
    /// The PF shots a side may take in a scenario (C13.31; ruling R9.7): its squad equivalents at setup before 1944, one and a half times as many
    /// (FRD) in 1944, and twice as many in 1945, from its half-squads at setup; a scenario with no year counts as before 1944.
    /// </summary>
    public static int PanzerfaustAllowance(int halfSquads, int? scenarioYear) => scenarioYear switch
    {
        >= 1945 => halfSquads,
        1944 => halfSquads * 3 / 4,
        _ => halfSquads / 2,
    };

    /// <summary>
    /// The squad equivalents (FRU) by which a side's Personnel overstack a Location for a shot, a crew or HS counting half (A5.1, A5.12). The
    /// Ordnance copy: no SMC term (the pass 32 design, section 12; it stays its own function).
    /// </summary>
    public static int OverstackExcessForOrdnance(int squads, int halfSquadsAndCrews)
    {
        var equivalents = squads + (halfSquadsAndCrews / 2m);
        return equivalents > 3 ? (int)Math.Ceiling(equivalents - 3) : 0;
    }

    /// <summary>Whether a Gun fires at its Bore Sighted Location with its original crew from its setup Location (C6.43); Locations compare by their own equality.</summary>
    public static bool BoreSighted<TLocation>(IEnumerable<(string Gun, TLocation Location, string Crew, TLocation SetupLocation)> sightings, string gun, string crew, TLocation from, TLocation target)
    {
        ArgumentNullException.ThrowIfNull(sightings);
        var equality = EqualityComparer<TLocation>.Default;
        return sightings.Any(item => item.Gun == gun && equality.Equals(item.Location, target) && item.Crew == crew && equality.Equals(item.SetupLocation, from));
    }

    /// <summary>Whether a Gun is Emplaced (C11.2, C11.3): manned by a crew (the manning unit's kind, null when not manned), never moved or hooked up.</summary>
    public static bool Emplaced(string? mannedByKind, Func<bool> unemplaced)
    {
        ArgumentNullException.ThrowIfNull(unemplaced);
        return mannedByKind == "asl:crew" && !unemplaced();
    }

    /// <summary>A turreted AFV's TCA (D3.12; ruling R7.10): its recorded turret facing, or its VCA; facings as ints.</summary>
    public static int? TurretFacing(int? recorded, int? hull) => recorded ?? hull;

    /// <summary>
    /// A vehicle target's state facts (C6.1 Case J, C.8, D5.5, D5.6): a vehicle that entered a new hex or moved in Motion this Player Turn, or is in
    /// Motion, is moving; a Stunned, Shocked, or Recalled crew adds one to Crew Survival. The Target Facings are the planner's map reads.
    /// </summary>
    public static OrdnanceVehicleTarget VehicleTarget(OrdnanceVehicleTargetStateFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new OrdnanceVehicleTarget(facts.Id, facts.Definition, null, null,
            facts.Motion || facts.MovedThisPlayerTurn, facts.Motion,
            facts.Concealed || facts.Hidden,
            facts.Stunned || facts.Shocked || facts.UnconfirmedKill || facts.Recalled,
            // D5.5: a Stunned or Shocked crew takes no Immobilization TC, nor does an absent crew or one already immobilized.
            !facts.Stunned && !facts.Shocked && !facts.UnconfirmedKill && !facts.Abandoned && !facts.Immobilized)
        {
            Abandoned = facts.Abandoned ? true : null,
            StunRecovery = facts.StunRecovery ? true : null,
        };
    }

    /// <summary>
    /// A light mortar's Spotter and directing leader (C9.3, A7.531; rulings R9.2, R9.4): the Spotter is an active unit of the firer's side, the one
    /// already spotting for the mortar while it is Good Order (<paramref name="keptSpotter"/>), unless it is the firer; the leader is an active leader
    /// in the firer's Location. Adjacency and LOS are the planner's map reads.
    /// </summary>
    public static ((OrdnanceSpotter? Spotter, FireDirector? Director)? Support, string? Reason) MortarSupport(string gun, string? keptSpotter, string? spotter,
        Func<OrdnanceSpotterFacts> spotterFacts, string? director, Func<OrdnanceDirectorFacts> directorFacts, string phase)
    {
        ArgumentNullException.ThrowIfNull(spotterFacts);
        ArgumentNullException.ThrowIfNull(directorFacts);
        OrdnanceSpotter? spotting = null;
        if (spotter is not null)
        {
            var unit = spotterFacts();
            if (!unit.ActiveWithDefinition || !unit.SameSide || unit.Captured || unit.Melee)
            {
                return (null, $"play.ordnance-spotter: '{spotter}' is not an active unit of the firer's side (C9.3)");
            }

            // C9.3 (ruling R9.4): a new Spotter is named only when the original is broken, eliminated, or captured.
            if (keptSpotter is not null && keptSpotter != unit.Id)
            {
                return (null, $"play.ordnance-spotter-kept: {keptSpotter} spots for {gun} while it is Good Order (C9.3)");
            }

            // C9.31 EX, A7.352 (table player, pass 9): a unit spots for one mortar a phase; a HS, crew, or SMC that has fired does not spot.
            if (unit.SpottedOtherMortar || (!unit.Squad && unit.FiredOrFirstFire && !unit.SpottedThisPhase))
            {
                return (null, $"play.ordnance-spotter-used: {unit.Id} has spotted for another mortar or fired this phase (C9.31, A7.352)");
            }

            if (unit.Squad && !unit.SpottedThisPhase && SquadSupportRefusal(unit.Support, phase, false) is { } spotterRefusal)
            {
                return (null, spotterRefusal);
            }

            spotting = new OrdnanceSpotter(unit.Id, unit.Definition, unit.Broken, unit.Pinned);
        }

        FireDirector? directing = null;
        if (director is not null)
        {
            var leader = directorFacts();
            if (!leader.ActiveWithDefinition || !leader.SameSide || leader.At is null || !leader.AtFirer)
            {
                return (null, $"play.ordnance-director: '{director}' is not an active unit of the firer's side in its Location (A7.531)");
            }

            // A7.53 (referee, pass 9): the leader directing a SW this phase goes on directing its further ROF shots.
            directing = new FireDirector(leader.Id, leader.Definition, leader.At, leader.Broken, leader.Pinned, leader.Concealed,
                leader.FiredOrFirstFire && !leader.Continuing, leader.Wounded);
        }

        return ((spotting, directing), null);
    }

    /// <summary>
    /// Why a squad may not use a SW now (A7.351, C13.31; table player, pass 9): it fired in an earlier phase (a Prep or Final Fire counter it did
    /// not get this phase), it fired its inherent FP with a SW this phase, or, for a PF, it is marked First Fire in the MPh (no PF Check in
    /// Subsequent First Fire). Null when it may.
    /// </summary>
    public static string? SquadSupportRefusal(SquadSupportFacts squad, string phase, bool panzerfaust)
    {
        ArgumentNullException.ThrowIfNull(squad);
        if (!squad.Squad)
        {
            return null;
        }

        if (phase == "MPh" ? squad.FirstFire && (panzerfaust || (!squad.FiredHere && !squad.UsedHere))
            : squad.Fired && !squad.FiredHere && !squad.UsedHere)
        {
            return $"play.sw-fired: {squad.Id} fired earlier{(phase == "MPh" ? " this MPh" : string.Empty)} and may not use a SW now (A7.351, C13.31)";
        }

        return squad.FiredHere && squad.FiredHereWeapon is not "0"
            ? $"play.sw-limit: {squad.Id} fired its inherent FP with a SW this phase and uses no other (A7.351)"
            : null;
    }
}
