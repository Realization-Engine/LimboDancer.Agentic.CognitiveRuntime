using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Reads LOS for a fire attack. The planner's own reader builds the LOS map from the boards' LOS data; a host or a test
/// may supply another, such as one over a map it has already built.
/// </summary>
public interface IFireLosReader
{
    /// <summary>The LOS result from one Location to another on the game's map, or null when it cannot be read.</summary>
    public LosResult? Read(GameState state, BoardLocation from, BoardLocation target);

    /// <summary>
    /// The LOS to a hexside Location's auxiliary vertex (the second end of the hexside, for a Snap Shot, A8.15); by default the reader's LOS to the
    /// hexside Location itself.
    /// </summary>
    public LosResult? ReadAuxiliary(GameState state, BoardLocation from, BoardLocation hexside) => Read(state, from, hexside);
}

/// <summary>
/// A proposed fire attack (Fire in Live Play, part 5): the facts read from the game and the map, and the reasons the firing
/// side may see. When the target Location holds a unit the firing side cannot see, or nothing it can see, a refusal tells
/// that side only that the Fire package does not decide the attack, since the package's reasons can name the unit or its
/// printed values; the target side and the adjudicator see the package's reasons.
/// </summary>
public sealed record FireProposal(string FiringSide, string TargetSide, FireAttack Attack, IReadOnlyList<string> FiringSideReasons)
{
    public const string Undisclosed = ScenarioA1FireEligibility.FireProposalUndisclosed;

    /// <summary>The reasons a perspective may see, given the reasons of the plan or of its result.</summary>
    public IReadOnlyList<string> ReasonsFor(IReadOnlyList<string> reasons, Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(perspective);
        return perspective.IsAdjudicator || perspective.Name != FiringSide || FiringSideReasons.Count == 0 ? reasons : FiringSideReasons;
    }
}

/// <summary>
/// Fire in live play (unit steps 18 to 23): PFPh, AFPh, DFPh, and MPh attacks by a fire group in one Location or across
/// ADJACENT Locations, with MGs, resolved by the reviewed Fire package. Every fact comes from the game and the map read;
/// the attack is refused before any roll unless every outcome the dice can reach is decided
/// (<see cref="ScenarioA1FireCalculator.Precheck"/>), and its rolls are drawn one at a time as the package asks for them.
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1FireReference> FireReference = new(() => new ScenarioA1FirePackage().Reference);

    // The VASL terrain names the Fire package's TEM admits (Terrain Chart p. 698; B1.1, B12, B13, B14, B15, B23), moved to Rules (pass 32.a).
    private static readonly IReadOnlyDictionary<string, string> FireTerrain = ScenarioA1Definitions.FireTerrain;

    /// <summary>
    /// Why a unit may not fire or direct fire now, or null: a berserk unit never fires in its PFPh and directs no fire (A15.432, A15.42; ruling
    /// R12.10); an Opportunity Firer fires in the AFPh (A7.25; ruling R12.1); a unit held in Melee fires only in CC (A11.15); a prisoner does not
    /// fire (A20.5); and a Guard whose US# is less than its prisoners' fires only at them (A20.52; ruling R12.9).
    /// </summary>
    public static string? FireBar(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        // Rules decides it (pass 32.c): the unit's conditions and kind, the phase, and the kinds of the prisoners in its custody.
        return ScenarioA1FireEligibility.FireBar(FireBarFacts(state, unit));
    }

    /// <summary>The facts the fire bar reads: the unit's conditions and kind, the phase, and the kinds of the prisoners in its custody.</summary>
    private static FireBarFacts FireBarFacts(GameState state, UnitInstance unit) =>
        new(unit.Kind, unit.Side, Is(unit, Conditions.Berserk), Is(unit, Conditions.BoundingFire),
            Is(unit, Conditions.Melee), Is(unit, Conditions.Captured), Is(unit, Conditions.Unarmed), LiveFire.IsVehicle(unit), state.Phase, state.PhasingSide,
            [.. state.Units.Where(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id).Select(prisoner => prisoner.Kind)]);

    private GamePlan PlanFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!arguments.TryGetProperty("firers", out var firerList) || firerList.ValueKind != JsonValueKind.Array
            || firerList.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)
            || !Text(arguments, "target", out var targetText) || !BoardLocation.TryParse(targetText, out var target))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: fire names its firers and a target Location");
        }

        var directors = new List<string>();
        if (Text(arguments, "director", out var named))
        {
            directors.Add(named);
        }

        directors.AddRange(Strings(arguments, "directors").Where(id => !directors.Contains(id)));
        var weapons = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (arguments.TryGetProperty("weapons", out var weaponMap) && weaponMap.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in weaponMap.EnumerateObject())
            {
                weapons[entry.Name] = entry.Value.ValueKind == JsonValueKind.Array
                    ? [.. entry.Value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
                    : [];
            }
        }

        // A22.3 (table player, pass 15): a FT fires apart from its user's inherent FP, so a firer naming one fires without it (Rules decides it, pass 32.c).
        var alone = ScenarioA1FireEligibility.FiresWithoutInherent(Strings(arguments, "withoutInherent"),
            weapons.Select(entry => new FirerFtFacts(entry.Key, entry.Value.Any(id => state.Find(id) is EquipmentInstance { Kind: "asl:ft" }))));
        string[] firerIds = [.. firerList.EnumerateArray().Select(item => item.GetString()!)];

        // A22.6, A22.611 (backlog pass 15, ruling R15.4): a MOL Check by one firer, where an SSR gives its side MOL; Rules decides it (pass 32.c), the
        // SSR and the record scan read when asked.
        var mol = Text(arguments, "mol", out var molText) ? molText : null;
        if (mol is not null)
        {
            var molUser = state.Unit(mol);
            if (ScenarioA1FireEligibility.MolBar(new MolUserFacts(mol, firerIds.Contains(mol), molUser is not null, molUser?.Side,
                () => state.SpecialRules.Contains("mol:" + molUser!.Side, StringComparer.Ordinal), state.Phase, () => MolCheckedInFirstFire(existing, mol),
                molUser is not null && Is(molUser, Conditions.Broken), molUser is not null && Is(molUser, Conditions.Captured),
                molUser is not null && Is(molUser, Conditions.Melee))) is { } molBar)
            {
                return Refused(scope, label, expected, molBar);
            }
        }

        // A9.12 (ruling R12.4): the SMC who fires a leader's MG with him, by leader.
        var partners = new Dictionary<string, string>(StringComparer.Ordinal);
        if (arguments.TryGetProperty("partners", out var partnerMap) && partnerMap.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in partnerMap.EnumerateObject().Where(item => item.Value.ValueKind == JsonValueKind.String))
            {
                partners[entry.Name] = entry.Value.GetString()!;
            }
        }

        // D3.3 (ruling R6.9): the moving vehicle's Bounding First Fire comes once the DEFENDER has passed on its MP expenditure; A8.1, A8.11: in the
        // MPh, Defensive fire answers the moving stack's MF expenditure, in its Location. Rules decides both (pass 32.c).
        var (bounding, windowRefusal) = ScenarioA1FireEligibility.MphWindow(state.Phase, firerIds.Length, () => LiveFire.MayBoundingFire(state, firerIds[0]),
            state.Movement is { WindowOpen: true } window && window.Location == target);
        if (windowRefusal is not null)
        {
            return Refused(scope, label, expected, windowRefusal);
        }

        var (attack, reason) = LiveFire.FromState(state, firerIds, directors, target, weapons.Count > 0 ? weapons : null, alone.Length > 0 ? alone : null,
            partners.Count > 0 ? partners : null, mol);
        if (attack is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        attack = WithSeen(state, attack);

        // A22.611 (ruling R15.4): no MOL through a woods or orchard hexside, one both of whose hexes are woods, or both orchard; Rules decides it from the
        // two terrains (pass 32.c).
        if (mol is not null && state.Location(mol)?.Location is { } molAt && molAt != target
            && ReadLocation(state, molAt) is { } molRead && ReadLocation(state, target) is { } targetRead
            && ScenarioA1FireEligibility.MolHexsideBar(TerrainKey(molRead), TerrainKey(targetRead)) is { } molHexside)
        {
            return Refused(scope, label, expected, molHexside);
        }

        // A9.22, A9.223 (referee, pass 12): a MG with a Fire Lane does not fire again this MPh, nor does its manning Infantry use Subsequent First Fire
        // or FPF; the TPBF and CC Reaction Fire cancellation is not built. Rules decides it (pass 32.c).
        if (ScenarioA1FireEligibility.FireLaneInUseBar(state.FireLanes.Select(lane => (lane.Weapon, lane.Operator)), attack) is { } laneInUse)
        {
            return Refused(scope, label, expected, laneInUse);
        }

        // A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.
        // Rules decides it (pass 32.c); the record scan and the two state scans are read when asked.
        if (ScenarioA1FireEligibility.LeaderMgBar(attack.Firers!.Select(leader => new LeaderMgFacts(leader.UnitId!, state.Unit(leader.UnitId!)?.Kind == "asl:leader",
            () => ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>().Any(record => record.Director == leader.UnitId
                || record.Facts.TryGetProperty("otherDirectors", out var others) && others.ValueKind == JsonValueKind.Array
                    && others.EnumerateArray().Any(item => item.TryGetProperty("unitId", out var id) && id.GetString() == leader.UnitId)),
            () => state.SupportWeaponDirectors.Any(item => item.Leader == leader.UnitId),
            () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == leader.UnitId
                && !(leader.Weapons?.Select(weapon => weapon.EquipmentId).ToArray() ?? []).Contains(item.Id) && (LiveFire.Fired(item) || Is(item, Conditions.FirstFire)))))) is { } leaderBar)
        {
            return Refused(scope, label, expected, leaderBar);
        }

        // A15.42 (ruling R12.10): a berserk leader gives no leadership, so directs no fire (Rules decides it, pass 32.c).
        if (ScenarioA1FireEligibility.BerserkDirectorBar(directors.Select(state.Unit).OfType<UnitInstance>().Select(unit => (unit.Id, Is(unit, Conditions.Berserk)))) is { } berserkLeader)
        {
            return Refused(scope, label, expected, berserkLeader);
        }

        // D2.4: a vehicle under a Motion counter may not Prep Fire (Rules decides it, pass 32.c).
        if (ScenarioA1FireEligibility.MotionPrepFireBar(attack.VehicleFire, state.Phase) is { } inMotion)
        {
            return Refused(scope, label, expected, inMotion);
        }

        // A15.432: berserk fire (TPBF in the AFPh, and the DFPh) is not reviewed; A11.15: units held in Melee fire only in CC; A20.52:
        // a Guard's fire is not reviewed; A20.54, A11.15: fire at a Location holding prisoners or units in Melee is not reviewed. Rules decides it (pass 32.c).
        if (ScenarioA1FireEligibility.GroupFireBar(firerIds.Concat(directors).Select(state.Unit).OfType<UnitInstance>().Select(unit => (unit.Id, FireBarFacts(state, unit)))) is { } barred)
        {
            return Refused(scope, label, expected, barred);
        }

        // A11.15, A20.54 (rulings R12.8, R12.9): a Location holding units in Melee or prisoners is fired at from outside it, and every unit there is attacked.
        if (ScenarioA1FireEligibility.MeleeLocationBar(
            state.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))),
            attack.Firers!.Any(item => item.LocationId == target.ToString())) is { } melee)
        {
            return Refused(scope, label, expected, melee);
        }

        // A8.3, A8.31: Subsequent First Fire and FPF use every usable MG the firer possesses (Rules decides it, pass 32.c).
        if (ScenarioA1FireEligibility.EveryMgBar(attack, unitId => Possessed(state, unitId)) is { } everyMg)
        {
            return Refused(scope, label, expected, everyMg);
        }

        var firingSide = state.Unit(attack.VehicleFire?.VehicleId ?? attack.Firers![0].UnitId!)!.Side;
        var targetSide = ScenarioA1FireEligibility.ProposalTargetSide(attack.Targets!.Count > 0, attack.Targets.Count > 0 ? state.Unit(attack.Targets[0].UnitId!)!.Side : null,
            attack.Vehicles is [{ }, ..], attack.Vehicles is [{ } vehicleTarget, ..] ? state.Unit(vehicleTarget.VehicleId!)!.Side : null,
            () => state.Sides.First(item => item.Id != firingSide).Id);

        // The firing side sees nothing, or not everything, at the target: its refusals say only that the attack is undecided. A concealed
        // vehicle is unseen too (ruling R6.7). Rules decides it (pass 32.c).
        var seen = attack.Targets.Count(item => VisibleTo(state.Unit(item.UnitId!)!, firingSide));
        var unseen = ScenarioA1FireEligibility.Unseen(seen, attack.Targets.Count, attack.Vehicles is { Count: > 0 }, (attack.Vehicles ?? []).Any(item => item.Concealed == true));
        FireProposal Proposal(FireAttack facts, bool undecided = false) =>
            new(firingSide, targetSide, facts, ScenarioA1FireEligibility.FiringSideReasons(undecided, unseen));

        // A7.55: the units of a Location that fire at a target in a phase (in the MPh, at one MF expenditure) form one fire
        // group, so it fires once; a MG firing again alone on its Multiple ROF is not a new group. Rules decides it (pass 32.c).
        var step = ScenarioA1FireEligibility.MphStep(state.Phase, bounding, state.Movement?.Step);
        var fromLocations = attack.Firers!.Select(item => item.LocationId!).Distinct(StringComparer.Ordinal).ToArray();
        if (ScenarioA1FireEligibility.FireGroupBar(alone.Length > 0,
            state.FiresThisPhase.Select(record => new PhaseFireFacts(record.FirerLocation, record.TargetLocation, record.Step, record.Vehicle)), attack, fromLocations, step) is { } group)
        {
            return Refused(scope, label, expected, group) with
            {
                Fire = Proposal(attack)
            };
        }

        // A8.3, A9.2: a unit or MG fires at a moving stack in a Location no more often than the MF the stack spent entering it
        // (FRD, at least once); each step enters a new Location, so the attacks there answer the current step. Rules counts them (pass 32.c).
        if (state.Movement is { } moving)
        {
            // Pass 31 (play test P-05; ruling R31.1): the count is of this stack's move alone. Step numbers start again at 1 for each stack, so only the
            // attacks made since this move's first step are read; a unit and each weapon it fires are counted apart ("the same unit/weapon", A8.3).
            var moveStart = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is MovementStepped { Step: 1 } or VehicleStepped { Step: 1 }).index;
            var recordedFirers = existing.Skip(moveStart).Select(item => item.Payload).OfType<FireResolved>().Where(record => record.MovementStep == moving.Step)
                .SelectMany(record => record.Facts.Deserialize<FireAttack>(LiveFire.Json)?.Firers ?? []);
            if (ScenarioA1FireEligibility.MfLimitBar(moving.HalfMfInLocation, moving.Location.ToString(), recordedFirers, attack) is { } spent)
            {
                return Refused(scope, label, expected, spent) with
                {
                    Fire = Proposal(attack)
                };
            }
        }

        // A8.15 (ruling R10.13): a Snap Shot at the hexside the moving stack crossed.
        if (arguments.TryGetProperty("snapShot", out var snap) && snap.ValueKind == JsonValueKind.True)
        {
            attack = attack with
            {
                SnapShot = true
            };
        }

        var (map, mapReason) = FireMapFacts(state, attack, target, existing);
        if (map is null)
        {
            return Refused(scope, label, expected, mapReason!) with
            {
                Fire = Proposal(attack)
            };
        }

        attack = HeatOfBattleFacts(state, map);

        // C11 (ruling R8.3): a Gun's crew alone in the target Location takes its gunshield, facing every firer's Location, or its Emplacement; Rules says
        // which fire reads it (pass 32.c).
        if (ScenarioA1FireEligibility.InfantryFireAtGun(attack) && attack.Firers is { } firing
            && GunAt(state, target, state.Unit(firing[0].UnitId!)!.Side, [.. firing.Select(item => BoardLocation.Parse(item.LocationId!)).Distinct()]) is { } gunTarget)
        {
            attack = attack with
            {
                GunTarget = gunTarget
            };
        }

        // A6.11, A7.52 (ruling R12.2): in a group spanning Locations, the firers whose LOS is blocked make their DR first and drop out; the others
        // attack as a smaller group. Rules names the blocked Locations and each part's units (pass 32.c).
        FireAttack? blockedFirst = null;
        if (ScenarioA1FireEligibility.BlockedLocations(attack) is { } blockedLocations)
        {
            var everyFirer = attack.Firers!;
            (FireAttack? Part, string? Why) Subgroup(bool blockedPart)
            {
                var ids = ScenarioA1FireEligibility.PartFirers(everyFirer, blockedLocations, blockedPart);
                var leaders = ScenarioA1FireEligibility.PartDirectors(directors.Select(id => (id, state.Location(id)?.Location.ToString())), blockedLocations, blockedPart);
                var (part, why) = LiveFire.FromState(state, ids, leaders, target, weapons.Where(item => ids.Contains(item.Key)).ToDictionary(item => item.Key, item => item.Value),
                    alone.Where(ids.Contains).ToArray(), partners.Where(item => ids.Contains(item.Key)).ToDictionary(item => item.Key, item => item.Value),
                    mol is not null && ids.Contains(mol) ? mol : null);
                if (part is null)
                {
                    return (null, why);
                }

                part = WithSeen(state, part);
                var (read, readWhy) = FireMapFacts(state, part with
                {
                    SnapShot = attack.SnapShot
                }, target, existing);
                return read is null ? (null, readWhy) : (HeatOfBattleFacts(state, read), null);
            }

            var (blockedPart, blockedWhy) = Subgroup(true);
            var (openPart, openWhy) = Subgroup(false);
            if (blockedPart is null || openPart is null)
            {
                return Refused(scope, label, expected, blockedWhy ?? openWhy!);
            }

            blockedFirst = blockedPart;
            attack = openPart with
            {
                GunTarget = attack.GunTarget
            };
        }

        // A9.5 (ruling R12.6): Spraying Fire at a second Location sharing a hexside with the first; Rules decides each check (pass 32.c).
        FireAttack? spray = null;
        if (Text(arguments, "sprayTarget", out var sprayText))
        {
            var parsed = BoardLocation.TryParse(sprayText, out var second);
            if (ScenarioA1FireEligibility.SprayTargetBar(parsed, parsed && second! == target, parsed && second!.Level == target.Level,
                parsed && SideToward(state, target, second!) is not null) is { } sprayTarget)
            {
                return Refused(scope, label, expected, sprayTarget);
            }

            if (ScenarioA1FireEligibility.SprayPhaseBar(state.Phase, blockedFirst is not null) is { } sprayPhase)
            {
                return Refused(scope, label, expected, sprayPhase);
            }

            if (ScenarioA1FireEligibility.SprayMolBar(mol is not null) is { } sprayMol)
            {
                return Refused(scope, label, expected, sprayMol);
            }

            var (sprayAttack, sprayReason) = LiveFire.FromState(state, firerIds, directors, second!, weapons.Count > 0 ? weapons : null, alone.Length > 0 ? alone : null,
                partners.Count > 0 ? partners : null);
            if (sprayAttack is null)
            {
                return Refused(scope, label, expected, sprayReason!);
            }

            sprayAttack = WithSeen(state, sprayAttack);

            var (sprayMap, sprayMapReason) = FireMapFacts(state, sprayAttack, second!, existing);
            if (sprayMap is null)
            {
                return Refused(scope, label, expected, sprayMapReason!);
            }

            if (ScenarioA1FireEligibility.SprayGroupBar(state.FiresThisPhase.Select(record => new PhaseFireFacts(record.FirerLocation, record.TargetLocation, record.Step, record.Vehicle)),
                fromLocations, second!.ToString()) is { } sprayGroup)
            {
                return Refused(scope, label, expected, sprayGroup);
            }

            spray = HeatOfBattleFacts(state, sprayMap) with
            {
                SprayingFire = true,
                SprayShare = true
            };
            attack = attack with
            {
                SprayingFire = true
            };

            // A9.52: a First-Fire-marked unit sprays in Final Fire only at two ADJACENT Locations (Rules decides it, pass 32.c).
            if (ScenarioA1FireEligibility.SprayAdjacentBar(state.Phase, attack, spray) is { } sprayAdjacent)
            {
                return Refused(scope, label, expected, sprayAdjacent);
            }
        }

        // A7.7 (ruling R12.11): an attack that completes an Encirclement lowers the Encircled units' Morale Level against it.
        var firingSideId = state.Unit(attack.VehicleFire?.VehicleId ?? attack.Firers![0].UnitId!)!.Side;
        var encircles = blockedFirst is null ? EncirclementSeal(state, existing, attack, firingSideId) : null;
        if (encircles is not null)
        {
            // Rules marks the targets (pass 32.c).
            attack = ScenarioA1FireEligibility.MarkEncircled(attack, encircles, id => state.Unit(id) is { } unit
                ? new EncircledUnitFacts(true, unit.Kind, unit.Side, Is(unit, Conditions.Melee)) : new EncircledUnitFacts(false, null, null, false));
        }

        // A9.22 (ruling R12.7): a Fire Lane declared with a MG's Defensive First Fire.
        (string Weapon, string Operator, IReadOnlyList<FireLaneEntry> Entries)? lane = null;
        if (arguments.TryGetProperty("fireLane", out var laneArguments) && laneArguments.ValueKind == JsonValueKind.Object)
        {
            if (!Text(laneArguments, "weapon", out var laneWeapon) || !Text(laneArguments, "to", out var laneText) || !BoardLocation.TryParse(laneText, out var laneTo))
            {
                return Refused(scope, label, expected, "play.invalid-arguments: a Fire Lane names its MG and the Location of its counter");
            }

            var manning = attack.Firers?.FirstOrDefault(item => item.Weapons?.Any(weapon => weapon.EquipmentId == laneWeapon) == true);
            var mg = manning?.Weapons!.First(weapon => weapon.EquipmentId == laneWeapon);
            var mgDefinition = mg is null ? null : FireReference.Value.Definitions.GetValueOrDefault(mg.DefinitionId ?? string.Empty);
            BoardLocation? mgAt = null;
            var mgAtRead = manning is not null && BoardLocation.TryParse(manning.LocationId, out mgAt);

            // Rules decides the declaration's conditions (pass 32.c).
            if (ScenarioA1FireEligibility.FireLaneDeclarationBar(new FireLaneDeclarationFacts(state.Phase, attack, manning, mg, mgDefinition?.IsMg == true,
                mgDefinition?.Range, mgDefinition?.Firepower, mgAtRead)) is { } laneBar)
            {
                return Refused(scope, label, expected, laneBar);
            }

            var (entries, laneReason) = FireLaneEntries(state, mgAt!, target, laneTo, mgDefinition!.Range!.Value, mgDefinition!.Firepower!.Value);
            if (entries is null)
            {
                return Refused(scope, label, expected, laneReason!);
            }

            lane = (laneWeapon, manning!.UnitId!, entries);
        }

        foreach (var part in new[] { blockedFirst, attack, spray }.OfType<FireAttack>())
        {
            var partCheck = ScenarioA1FireCalculator.Precheck(part, FireReference.Value);
            if (partCheck.Count != 0)
            {
                // Pass 31c (design section 14): a refusal for range names the firer or weapon, its range, and how far it fires. Those lines rest on
                // the proposer's own units and the map alone, so the firing side reads them even where the target's units are not its to see; it
                // is then told nothing else, whatever else the package found.
                var refusal = RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", partCheck);
                var ranged = RangeNamed(part, refusal);
                var proposal = Proposal(part, undecided: true);

                // Pass 31d (design D8; ruling R31d.5): the same holds for every reason that rests on the proposer's own group and the map alone (a
                // FT fired with other units, a firer that may not fire, a weapon that has fired). Where there is one, the firing side reads those
                // reasons and nothing else; where there is none, it reads the one sentence. What it reads then depends only on its own group.
                string[] ownGroup = [.. ranged.Named, .. ranged.Reasons.Where(reason => OwnGroupCodes.Any(code => reason.StartsWith(code, StringComparison.Ordinal)))];
                return Refused(scope, label, expected, ranged.Reasons) with
                {
                    Fire = ownGroup.Length > 0 && proposal.FiringSideReasons.Count > 0
                        ? proposal with
                        {
                            FiringSideReasons = ["play.fire-refused: the Fire package refuses this attack as proposed", .. ownGroup.Distinct(StringComparer.Ordinal)]
                        }
                        : proposal
                };
            }
        }

        var facts = attack;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            if (blockedFirst is not null)
            {
                AddFireEvents(scope, attemptId, expected, actor, state, blockedFirst, targetSide, step, events, draw);
                var after = Replay([.. existing, .. events]).Current!;
                var reread = LiveFire.FromState(after, [.. facts.Firers!.Select(item => item.UnitId!)], [.. new[] { facts.Director?.UnitId }.Concat(facts.OtherDirectors?.Select(item => item.UnitId) ?? []).OfType<string>()],
                    target, facts.Firers!.Where(item => item.Weapons is { Count: > 0 }).ToDictionary(item => item.UnitId!, item => (IReadOnlyList<string>)[.. item.Weapons!.Select(weapon => weapon.EquipmentId!)]),
                    [.. facts.Firers!.Where(item => item.UsesInherentFp == false).Select(item => item.UnitId!)],
                    facts.Firers!.Where(item => item.Partner is not null).ToDictionary(item => item.UnitId!, item => item.Partner!),
                    facts.Firers!.FirstOrDefault(item => item.Mol == true)?.UnitId).Attack;
                if (reread is not null && FireMapFacts(after, WithSeen(after, reread), target, [.. existing, .. events]).Facts is { } rereadMap)
                {
                    AddFireEvents(scope, attemptId, expected, actor, after, HeatOfBattleFacts(after, rereadMap) with
                    {
                        GunTarget = facts.GunTarget
                    }, targetSide, step, events, draw);
                }

                AddSniperAttacks(scope, attemptId, expected, actor, existing, events, draw);
                return events;
            }

            var followUps = new FireFollowUps(target.ToString())
            {
                Spray = spray,
                Encircles = encircles,
                LaneWeapon = lane?.Weapon,
                LaneOperator = lane?.Operator,
                LaneEntries = lane is { } declaredLane ? [.. declaredLane.Entries.Select(item => new FireFollowUps.LaneEntry(item.Location.ToString(), item.Fp, item.HindranceDrm))] : null,
            };
            AddFireEvents(scope, attemptId, expected, actor, state, facts, targetSide, step, events, draw, followUps: followUps);
            AddFireFollowUps(scope, attemptId, expected, actor, existing, state, targetSide, step, events, draw, followUps);
            AddSniperAttacks(scope, attemptId, expected, actor, existing, events, draw);
            return events;
        }

        var directing = new[] { facts.Director?.UnitId }.Concat(facts.OtherDirectors?.Select(item => item.UnitId) ?? []).OfType<string>().ToArray();
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.fire: {string.Join(", ", facts.VehicleFire is { } byVehicle ? [byVehicle.VehicleId] : facts.Firers!.Select(item => item.UnitId))} fire at {facts.TargetLocationId} in the {facts.Phase}"
                + (facts.FireKind is { } kind ? $" ({kind})" : string.Empty)
                + (directing.Length > 0 ? $", directed by {string.Join(", ", directing)}" : string.Empty)
                + $"; range {facts.Range}, {facts.TargetTerrain}, Hindrance {facts.Los!.HindranceDrm}"
                + (spray is not null ? $"; Spraying Fire at {spray.TargetLocationId} too, on the same DR (A9.5)" : string.Empty)
                + (lane is { } declared ? $"; a Fire Lane of {declared.Weapon} to {declared.Entries[^1].Location} (A9.22)" : string.Empty)
                + (blockedFirst is not null ? "; the firers whose LOS is blocked fire first and drop out (A6.11, A7.52)" : string.Empty)
                + (encircles is not null ? $"; this attack Encircles the {encircles} units at {facts.TargetLocationId} (A7.7)" : string.Empty)
                + (mol is not null ? $"; {mol} makes a MOL Check first: a dr of 3 or less after its drm adds four FP (A22.611)" : string.Empty),

                // Pass 31 (play test P-12, P-13): a shot with no LOS, or one that also hits the firing side's own units, is said before it is confirmed.
                .. FireWarnings(facts)])
        {
            Roll = new PlannedRoll("fire", Build),
            FirstEventId = EventId(attemptId, 1),
            Fire = Proposal(facts),
        };
    }

    /// <summary>
    /// The map reads Heat of Battle needs before any roll (unit step 30): for each target, and each FPF firer, whether a Known enemy
    /// unit is in its LOS (A15.44) and the ADJACENT units it may surrender to (A15.5).
    /// </summary>
    private FireAttack HeatOfBattleFacts(GameState state, FireAttack attack)
    {
        // A12.14: a concealed firer or director loses "?" by this attack when every firer is within 16 hexes and a target is Good Order,
        // as the Fire package decides it; one that does is Known when a target's Heat of Battle result is read (A15.44, A15.5).
        // Pass 31d (ruling R31d.2): with the planner's read for every concealed unit of the group, the units a Good Order enemy unit sees; without
        // it, as the package decided before. Rules decides which (pass 32.c); the state's concealment is read when asked.
        var revealed = ScenarioA1FireEligibility.RevealedByAttack(attack, id => state.Unit(id) is { } unit && Is(unit, Conditions.Concealed));
        FireTarget Read(FireTarget target)
        {
            if (target.Dummy == true || state.Unit(target.UnitId!) is not { } unit || state.Location(unit.Id) is not { } at)
            {
                return target;
            }

            return target with
            {
                KnownEnemyInLos = ScenarioA1FireEligibility.KnownEnemyInLosAfterReveal(revealed.Length > 0, () => KnownEnemyInLos(state, unit.Side, at.Location)),
                Captors = Captors(state, unit, revealed),
            };
        }

        return attack with
        {
            Targets = [.. attack.Targets!.Select(Read)],
            Firers = !ScenarioA1FireEligibility.FirersTakeHeatOfBattleReads(attack.FireKind) ? attack.Firers
                : [.. attack.Firers!.Select(firer => state.Unit(firer.UnitId!) is { } unit && state.Location(unit.Id) is { } at
                    ? firer with { KnownEnemyInLos = KnownEnemyInLos(state, unit.Side, at.Location), Captors = Captors(state, unit) }
                    : firer)],
        };
    }

    /// <summary>Whether a unit made a MOL Check in Defensive First Fire this Player Turn (A22.611; ruling R15.4).</summary>
    private static bool MolCheckedInFirstFire(IReadOnlyList<GameEvent> existing, string unitId) =>
        // Rules scans the record from its end (pass 32.c); each event is read when asked.
        ScenarioA1FireEligibility.MolCheckedInFirstFire(existing.Count, index => existing[index].Payload switch
        {
            PhaseChanged { Phase: "rph" } => new MolEventFacts(true, false),
            FireResolved fire => new MolEventFacts(false, fire.MovementStep is not null && fire.Facts.TryGetProperty("firers", out var firers) && firers.ValueKind == JsonValueKind.Array
                && firers.EnumerateArray().Any(item => item.TryGetProperty("unitId", out var id) && id.GetString() == unitId
                    && item.TryGetProperty("mol", out var used) && used.ValueKind == JsonValueKind.True)),
            _ => new MolEventFacts(false, false),
        });

    private static IEnumerable<string> Strings(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)
            : [];

    /// <summary>The usable MGs and ATR a unit possesses, in id order (table player, pass 9b: a mortar or PSK takes no part in its fire groups).</summary>
    private static string[] Possessed(GameState state, string unitId) =>
        ScenarioA1FireEligibility.UsableMgs(state.Equipment.Where(equipment => equipment.Status == InstanceStatus.Active
                && equipment.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unitId)
            .Select(equipment => new PossessedWeaponFacts(equipment.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True,
                GameState.Condition(equipment, Conditions.Dismantled) == ConditionState.True, equipment.Kind,
                () => equipment.Definition is { } weapon ? LiveOrdnance.LatwType(weapon.Definition) : null)));

    /// <summary>
    /// What follows a fire attack once its owners' options are answered (ruling R27.4): Spraying Fire's second Location on the same Original DR (A9.5),
    /// the Encirclement it seals (A7.7), the Fire Lane it lays (A9.22), and a DC's attack on its thrower's Location and its removal (A23.6, A23.4).
    /// A choice pending in the first attack carries it in its resume.
    /// </summary>
    internal sealed record FireFollowUps(string Target)
    {
        public FireAttack? Spray
        {
            get; init;
        }

        public string? Encircles
        {
            get; init;
        }

        public string? LaneWeapon
        {
            get; init;
        }

        public string? LaneOperator
        {
            get; init;
        }

        public IReadOnlyList<LaneEntry>? LaneEntries
        {
            get; init;
        }

        /// <summary>The DC removed after its attacks; null for other fire.</summary>
        public string? DcCharge
        {
            get; init;
        }

        /// <summary>The thrower's Location a Thrown DC attacks next (A23.6); null once that attack is made, and for a Placed DC.</summary>
        public string? DcThrower
        {
            get; init;
        }

        public sealed record LaneEntry(string Location, int Fp, int HindranceDrm);
    }

    /// <summary>The follow-ups of an attack whose record is the last in <paramref name="events"/> (ruling R27.4); nothing while a choice is pending. Rules decides each (pass 32.c).</summary>
    private void AddFireFollowUps(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, GameState state, string targetSide,
        int? step, List<GameEvent> events, Func<RollRequest, RollResult> draw, FireFollowUps followUps)
    {
        if (events.Any(item => item.Payload is ChoicePending))
        {
            return;
        }

        var target = BoardLocation.Parse(followUps.Target);
        var record = events.LastOrDefault(item => item.Payload is FireResolved);
        if (followUps.Spray is { } spray && record?.Payload is FireResolved first && first.Rolls.TryGetValue("attack", out var attackRoll)
            && existing.Concat(events).Select(item => item.Payload).OfType<DiceRolled>().FirstOrDefault(item => item.Roll == attackRoll) is { } dice)
        {
            // A9.5: the second Location takes the same Original DR.
            var sprayState = Replay([.. existing, .. events]).Current!;
            var sprayTargetSide = ScenarioA1FireFollowUps.SprayTargetSide(spray, id => state.Unit(id)!.Side, targetSide);
            AddFireEvents(scope, attemptId, expected, actor, sprayState, spray, sprayTargetSide, step, events, draw,
                new ResumedRolls(new Dictionary<string, string>(StringComparer.Ordinal) { ["attack"] = attackRoll }, [("attack", dice.Values)]), followUps with
                {
                    Spray = null
                });
            if (events.Any(item => item.Payload is ChoicePending))
            {
                return;
            }
        }

        if (followUps.Encircles is { } encircles && ScenarioA1FireFollowUps.PlacesEncirclement(record is not null,
            record is not null && Replay([.. existing, .. events]).Current is { } sealedState
                && sealedState.At(target).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == encircles)))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "encirclement-placed", new EncirclementPlaced(target, encircles, record!.EventId), null, null,
                [record.EventId]));
        }

        // A9.22: no Fire Lane when the manning Infantry Cowered or the MG malfunctioned; the MG is marked First Fire.
        if (followUps is { LaneWeapon: { } laneWeapon, LaneOperator: { } laneOperator, LaneEntries: { } laneEntries })
        {
            var cowered = record?.Payload is FireResolved laneRecord && laneRecord.Resolution.TryGetProperty("arithmetic", out var laneArithmetic)
                && laneArithmetic.TryGetProperty("cowered", out var coweredFlag) && coweredFlag.GetBoolean();
            var weapon = record is null || cowered ? null : Replay([.. existing, .. events]).Current?.Find(laneWeapon) as EquipmentInstance;
            var (placed, markFirstFire) = ScenarioA1FireFollowUps.PlacesFireLane(record is not null, cowered, weapon is not null,
                weapon is not null && Is(weapon, Conditions.Malfunctioned), weapon is not null && Is(weapon, Conditions.FirstFire));
            if (placed)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-lane-placed", new FireLanePlaced(record!.EventId, laneWeapon, laneOperator,
                    [.. laneEntries.Select(item => new FireLaneEntry(BoardLocation.Parse(item.Location), item.Fp, item.HindranceDrm))]), null, null, [record.EventId]));
                if (markFirstFire)
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(laneWeapon, new Dictionary<string, ConditionState> { [Conditions.FirstFire] = ConditionState.True }), null, null, [record.EventId]));
                }
            }
        }

        if (followUps.DcCharge is not { } chargeId)
        {
            return;
        }

        // A23.6 (ruling R15.3): a Thrown DC that did not malfunction attacks its thrower's Location next.
        var malfunctioned = record?.Payload is FireResolved dcRecord && dcRecord.Resolution.TryGetProperty("demolitionChargeMalfunctioned", out var flag)
            && flag.ValueKind == JsonValueKind.True;
        if (followUps.DcThrower is { } throwerText && !malfunctioned && Replay([.. existing, .. events]).Current is { } after
            && BoardLocation.Parse(throwerText) is var own && LiveFire.DemolitionChargeFromState(after, chargeId, FireDemolitionCharge.Thrower, own).Attack is { } back
            && back.DemolitionCharge?.UserId is { } user && after.Unit(user) is { } thrower)
        {
            AddFireEvents(scope, attemptId, expected, actor, after, HeatOfBattleFacts(after, ScenarioA1FireFollowUps.ThrowerAttack(back,
                ReadLocation(after, own) is { } ownRead ? TerrainKey(ownRead) : null)), thrower.Side, null, events, draw, followUps: followUps with
                {
                    DcThrower = null
                });
            if (events.Any(item => item.Payload is ChoicePending))
            {
                return;
            }
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(chargeId), ScenarioA1FirePackage.Identity.ToString(), null));
        AddSniperAttacks(scope, attemptId, expected, actor, existing, events, draw);
    }

    /// <summary>
    /// Draws the rolls the package asks for, one at a time, and adds the attack's events: the dice, the fire record (withheld
    /// from the firing side when it would identify unseen targets the attack leaves unaffected), its public report, the
    /// effects on the targets, the FPF firers' NMC, the MGs' malfunctions and markers, the fire markers, and the Residual FP
    /// it leaves. Rules decides each block (pass 32.c); the dice, the records, and the events are written here.
    /// </summary>
    private void AddFireEvents(GameScope scope, string attemptId, long expected, string actor, GameState state, FireAttack facts, string targetSide,
        int? step, List<GameEvent> events, Func<RollRequest, RollResult> draw, ResumedRolls? resumed = null, FireFollowUps? followUps = null)
    {
        var reference = FireReference.Value;
        var package = ScenarioA1FirePackage.Identity.ToString();

        // Ruling R5.8: the owners' options are asked for as the attack reaches them; a resumed attack starts from the rolls it drew. A DC's attack
        // asks them too (ruling R27.4): its thrower's Location and its removal follow once they are answered.
        facts = facts with
        {
            Choices = facts.Choices ?? new Dictionary<string, string>(StringComparer.Ordinal)
        };
        var rollIds = new Dictionary<string, string>(resumed?.RollIds ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var rolls = new FireRolls(null, null, null, null, null);
        foreach (var (key, values) in resumed?.Values ?? [])
        {
            rolls = ApplyFireRoll(rolls, key, values);
        }

        FireResolution resolution;
        while (true)
        {
            resolution = ScenarioA1FireCalculator.Resolve(facts with
            {
                Rolls = rolls
            }, reference);
            var next = ScenarioA1FireFollowUps.NextFireStep(resolution);
            if (next.Resolved)
            {
                break;
            }

            // An option the attack reaches stops it until its owner answers (ruling R5.8).
            if (next.ChoiceKey is { } choiceKey)
            {
                var resume = new JsonObject
                {
                    ["record"] = "fire",
                    ["facts"] = JsonNode.Parse(JsonSerializer.Serialize(facts, LiveFire.Json)),
                    ["rolls"] = RollNode(rollIds),
                    ["targetSide"] = targetSide,
                };
                if (step is { } movementStep)
                {
                    resume["step"] = movementStep;
                }

                // Ruling R27.4: what follows the attack (Spraying Fire's second Location, Encirclement, a Fire Lane, a DC's thrower and removal) resumes too.
                if (followUps is not null)
                {
                    resume["followUps"] = JsonNode.Parse(JsonSerializer.Serialize(followUps, LiveFire.Json));
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "choice-pending", Pending(state, choiceKey, resume), package, null));
                return;
            }

            if (next.Undecided is { } undecided)
            {
                throw new InvalidOperationException("The Fire package left an attack it had accepted undecided: " + undecided);
            }

            var drawn = draw(new RollRequest(next.Count, 6));
            var rollId = $"{attemptId}-roll-{(events.Count(item => item.Payload is DiceRolled) + 1).ToString(CultureInfo.InvariantCulture)}";
            rollIds[next.RollKey!] = rollId;
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                new DiceRolled(rollId, next.Purpose!, next.Count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            rolls = ApplyFireRoll(rolls, next.RollKey!, drawn.Values);
        }

        // A12.13: concealed, hidden, and Dummy targets have their own column when known targets share the Location. A
        // result that leaves unseen targets, or nothing, unaffected is not identified to the firing side (A12.14; R21.1).
        var arithmetic = resolution.Arithmetic!;
        var hidesIdentity = ScenarioA1FireFollowUps.HidesIdentity(arithmetic, facts.Targets!);
        var fireId = EventId(attemptId, events.Count + 1);
        var firerLocation = facts.FirerLocationId ?? facts.TargetLocationId!;
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-resolved",
            new FireResolved(facts.VehicleFire is { } byVehicle ? [byVehicle.VehicleId!] : facts.Overrun is { } overrunning ? [overrunning.VehicleId!]
                    : [.. (facts.Firers ?? []).Select(item => item.UnitId!)],
                facts.Director?.UnitId, firerLocation, facts.TargetLocationId!, rollIds,
                JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json))
            {
                MovementStep = step,
            }, package, hidesIdentity ? [targetSide] : null));
        if (hidesIdentity)
        {
            // The firing side still learns the arithmetic, which names no target unit.
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "fire-reported",
                new FireReported(fireId, firerLocation, facts.TargetLocationId!, JsonSerializer.SerializeToElement(arithmetic, LiveFire.Json)), package, null,
                [fireId]));
        }

        // A10.62: a broken unit attacked by FP that could inflict at least a NMC, allowing for Cowering, is under DM.
        var desperate = CouldCauseNmc(facts, arithmetic);
        foreach (var effect in resolution.Effects)
        {
            var target = facts.Targets!.First(item => item.UnitId == effect.UnitId);
            var attackedBroken = ScenarioA1FireFollowUps.AttackedWhileBroken(target, desperate);
            foreach (var payload in EffectEvents(state, effect, attemptId, attackedBroken))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }
        }

        // A7.308, A7.309, D.8B: the Vehicle line's result and the crew's (ruling R25.5, R25.6).
        foreach (var vehicle in resolution.VehicleEffects ?? [])
        {
            foreach (var payload in VehicleEffectEvents(state, vehicle, facts))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }

            // D5.341, D5.41 (ruling R5.18): a Recalled AFV on its way off the map that is immobilized is Abandoned by its crew.
            if (state.Unit(vehicle.VehicleId) is { } stuck && ScenarioA1FireFollowUps.AbandonsWhenImmobilized(vehicle, MustLeave(stuck)))
            {
                foreach (var (type, abandon) in AbandonEvents(stuck, attemptId))
                {
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, type, abandon, package, null, [fireId]));
                }
            }
        }

        foreach (var effect in resolution.FirerEffects ?? [])
        {
            foreach (var payload in EffectEvents(state, effect, attemptId, false))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, payload.Type, payload.Payload, package, null, [fireId]));
            }
        }

        // A15.41: the companions a berserk leader took with him.
        foreach (var effect in resolution.CompanionEffects ?? [])
        {
            if (EffectEvent(state, effect, attemptId, false) is { } companion)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, companion.Type, companion.Payload, package, null, [fireId]));
            }
        }

        // A22.6111 (ruling R15.4): a colored dr of 6 breaks the MOL's user, under DM.
        if (resolution.MolCheck is { } molCheck && state.Unit(molCheck.UnitId) is { } molUser
            && ScenarioA1FireFollowUps.MolUserBreaks(molCheck, molUser.Status == InstanceStatus.Active, Is(molUser, Conditions.Broken)) is { } molConditions)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(molUser.Id, ConditionChanges(molConditions)), package, null, [fireId]));
        }

        // A22.5 (ruling R15.1): a FT run out of fuel is removed after the attack.
        if (resolution.FlamethrowerRemoved is { } outOfFuel)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(outOfFuel), package, null, [fireId]));
        }

        // The MGs: a malfunction (A9.7), and the fire counter of a MG that lost its Multiple ROF (A9.2).
        foreach (var weapon in (resolution.WeaponEffects ?? []).Where(item => item.EquipmentId != resolution.FlamethrowerRemoved))
        {
            var conditions = ConditionChanges(ScenarioA1FireFollowUps.WeaponEffectConditions(weapon,
                () => state.Find(weapon.EquipmentId) is { } equipment && GameState.Condition(equipment, Conditions.FirstFire) == ConditionState.True));
            if (conditions.Count > 0)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(weapon.EquipmentId, conditions),
                    package, null, [fireId]));
            }
        }

        // D7.17 (ruling R11.11): an OVR's Original 12 malfunctions a weapon that added FP, or immobilizes a vehicle with none; a wreck keeps no weapons.
        if (resolution.OverrunEffect is { } overrunEffect && state.Unit(overrunEffect.VehicleId) is { Status: InstanceStatus.Active })
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(overrunEffect.VehicleId, ConditionChanges(ScenarioA1FireFollowUps.OverrunEffectConditions(overrunEffect))), package, null, [fireId]));
        }

        // The fire markers (A3.2, A3.4, A3.5, A8.1, A8.3, A8.4); a MG firing again alone on its Multiple ROF leaves its unit as it was.
        foreach (var id in ScenarioA1FireFollowUps.FireMarkerUnits(facts, resolution, id => state.Unit(id) is { Status: not InstanceStatus.Active }))
        {
            var marked = state.Unit(id);
            var conditions = ConditionChanges(ScenarioA1FireFollowUps.FireMarkerConditions(id, facts, resolution,
                marked is not null && GameState.Condition(marked, Conditions.FirstFire) == ConditionState.True,
                marked is not null && (Is(marked, Conditions.Concealed) || Is(marked, Conditions.Hidden))));
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(id, conditions), package, null, [fireId]));
        }

        // A8.2, A8.21: Residual FP, unless a counter at least as large is already there.
        var targetKnown = BoardLocation.TryParse(facts.TargetLocationId!, out var location);
        if (ScenarioA1FireFollowUps.ResidualFpPlaced(arithmetic, targetKnown, residual => state.ResidualFire.Any(item => item.Location == location && item.Fp >= residual)) is { } residualFp)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "residual-fp-placed", new ResidualFirePlaced(fireId, location!, residualFp), package, null,
                [fireId]));
        }

        // A15.5: a unit that surrendered to ADJACENT captors waits for the captor's choice, last, since nothing else happens until then.
        foreach (var (id, captors) in ScenarioA1FireFollowUps.SurrenderPendings(resolution, attemptId))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(id, captors), package, null, [fireId]));
        }
    }

    /// <summary>
    /// The events that record a vehicle's effect (rulings R25.5, R25.6, R6.5, R6.7), as Rules decides it (pass 32.c): a destroyed vehicle becomes a
    /// wreck, with a Blaze when it burns; else its conditions change, and a concealed vehicle may lose its "?".
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> VehicleEffectEvents(GameState state, FireVehicleEffect effect, FireAttack facts)
    {
        var vehicle = state.Unit(effect.VehicleId);
        var verdict = ScenarioA1FireFollowUps.VehicleEffect(effect, facts.FireKind, vehicle is not null && (Is(vehicle, Conditions.Concealed) || Is(vehicle, Conditions.Hidden)));
        if (verdict.Wrecked)
        {
            yield return ("vehicle-wrecked", new VehicleWrecked(effect.VehicleId, verdict.Burning));
            if (verdict.Burning && vehicle is not null && state.Location(vehicle.Id) is { } at)
            {
                yield return ("instance-created", new InstanceCreated(new NewInstance(BlazeId(vehicle.Id), "asl:fire", null, null, new MapPosition(at.Location), null,
                    new Dictionary<string, ConditionState>())));
            }

            yield break;
        }

        if (verdict.Changes)
        {
            var changed = VehicleConditions(effect)!;
            if (verdict.LosesConcealment)
            {
                changed[Conditions.Concealed] = ConditionState.False;
                changed[Conditions.Hidden] = ConditionState.False;
            }

            yield return ("conditions-changed", new ConditionsChanged(effect.VehicleId, changed));
        }
        else if (verdict.LosesConcealment)
        {
            yield return ("conditions-changed", new ConditionsChanged(effect.VehicleId,
                new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }));
        }
    }

    /// <summary>The conditions a vehicle result sets, as Rules decides them (pass 32.a), under Units' condition names; null when it sets none.</summary>
    private static Dictionary<string, ConditionState>? VehicleConditions(FireVehicleEffect effect)
    {
        var changes = ScenarioA1ResultTables.VehicleConditions(effect);
        if (changes.Count == 0)
        {
            return null;
        }

        // In the order Rules gives them: the record writes the conditions in the order they were set.
        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        foreach (var (condition, value) in changes)
        {
            conditions[condition switch
            {
                VehicleCondition.Immobilized => Conditions.Immobilized,
                VehicleCondition.Motion => Conditions.Motion,
                VehicleCondition.Stunned => Conditions.Stunned,
                VehicleCondition.StunRecovery => Conditions.StunRecovery,
                VehicleCondition.Recalled => Conditions.Recalled,
                VehicleCondition.ButtonedUp => Conditions.ButtonedUp,
                _ => Conditions.Pinned,
            }] = value ? ConditionState.True : ConditionState.False;
        }

        return conditions;
    }

    /// <summary>A verdict's condition changes as the record writes them (pass 32.c): each condition under its name, in the order given, a later value replacing an earlier one in its place.</summary>
    private static Dictionary<string, ConditionState> ConditionChanges(IReadOnlyList<(UnitCondition Condition, bool Value)> changes)
    {
        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        foreach (var (condition, value) in changes)
        {
            conditions[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
        }

        return conditions;
    }

    /// <summary>A condition of the Rules verdicts under Units' name.</summary>
    private static string ConditionName(UnitCondition condition) => condition switch
    {
        UnitCondition.Broken => Conditions.Broken,
        UnitCondition.Pinned => Conditions.Pinned,
        UnitCondition.Wounded => Conditions.Wounded,
        UnitCondition.Disrupted => Conditions.Disrupted,
        UnitCondition.DesperationMorale => Conditions.DesperationMorale,
        UnitCondition.Concealed => Conditions.Concealed,
        UnitCondition.Hidden => Conditions.Hidden,
        UnitCondition.Fanatic => Conditions.Fanatic,
        UnitCondition.Berserk => Conditions.Berserk,
        UnitCondition.Heroic => Conditions.Heroic,
        UnitCondition.PrepFire => Conditions.PrepFire,
        UnitCondition.FirstFire => Conditions.FirstFire,
        UnitCondition.FinalFire => Conditions.FinalFire,
        UnitCondition.BoundingFire => Conditions.BoundingFire,
        UnitCondition.Cx => Conditions.Cx,
        UnitCondition.Malfunctioned => Conditions.Malfunctioned,
        UnitCondition.Immobilized => Conditions.Immobilized,
        UnitCondition.Motion => Conditions.Motion,
        UnitCondition.BmgMalfunctioned => Conditions.BmgMalfunctioned,
        UnitCondition.IntensiveFire => Conditions.IntensiveFire,
        UnitCondition.Shocked => Conditions.Shocked,
        UnitCondition.UnconfirmedKill => Conditions.UnconfirmedKill,
        UnitCondition.ButtonedUp => Conditions.ButtonedUp,
        UnitCondition.Abandoned => Conditions.Abandoned,
        UnitCondition.Disabled => Conditions.Disabled,
        UnitCondition.Captured => Conditions.Captured,
        UnitCondition.Melee => Conditions.Melee,
        UnitCondition.Unarmed => Conditions.Unarmed,
        _ => Conditions.CmgMalfunctioned,
    };

    /// <summary>
    /// Whether the attack could inflict at least a NMC on a target of a group (A10.62): over every DR, with the Cowering a
    /// doubles DR brings when no leader directs, on the group's column and with the attack's DRM.
    /// </summary>
    private static Func<bool, bool> CouldCauseNmc(FireAttack facts, FireArithmetic arithmetic) => ScenarioA1ResultTables.CouldCauseNmc(facts, arithmetic);

    private static Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, string id, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[id] = value;
        return next;
    }

    /// <summary>
    /// The event that records one unit's effect: an elimination, a Reduction or Replacement, or new conditions. A unit that
    /// breaks, or that is attacked while broken by enough FP, is under DM (A10.62). The heroes Heat of Battle creates follow (A15.21; rulings R5.10,
    /// R5.11), as Rules names them (pass 32.c).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> EffectEvents(GameState state, FireUnitEffect effect, string attemptId,
        bool attackedWhileBroken)
    {
        if (EffectEvent(state, effect, attemptId, attackedWhileBroken) is { } own)
        {
            yield return own;
        }

        if (state.Unit(effect.UnitId) is { } creator)
        {
            foreach (var hero in ScenarioA1FireFollowUps.HeroCreations(effect, attemptId))
            {
                yield return ("instance-created", new InstanceCreated(HeroOf(creator, hero.DefinitionId, attemptId, hero.Suffix, hero.Concealed)) { Creator = hero.CreatorId });
            }
        }
    }

    /// <summary>A hero a unit creates (A15.21), with the conditions Rules gives it (pass 32.c), in its creator's Location.</summary>
    private static NewInstance HeroOf(UnitInstance creator, string hero, string attemptId, string suffix = "hero", bool concealed = false) =>
        new(ScenarioA1FireFollowUps.HeroId(attemptId, creator.Id, suffix), "asl:hero", hero, creator.Side, creator.Position, null,
            ConditionChanges(ScenarioA1FireFollowUps.HeroConditions(concealed, condition => GameState.Condition(creator, ConditionName(condition)) == ConditionState.True)));

    private static (string Type, EventPayload Payload)? EffectEvent(GameState state, FireUnitEffect effect, string attemptId, bool attackedWhileBroken)
    {
        var unit = state.Unit(effect.UnitId)!;
        // Rules decides the conditions and the lineage (pass 32.c); the dictionaries are built here in the order given.
        var verdict = ScenarioA1FireFollowUps.Effect(effect, attackedWhileBroken, condition => GameState.Condition(unit, ConditionName(condition)) == ConditionState.True,
            unit.Kind, () => FireReference.Value.Definitions[effect.FinalDefinitionId].Kind);
        if (verdict.Eliminated)
        {
            return ("instance-eliminated", new InstanceEliminated(unit.Id));
        }

        var conditions = ConditionChanges(verdict.Conditions);
        if (verdict.Lineage is { } lineage)
        {
            var reference = FireReference.Value.Definitions[effect.FinalDefinitionId];
            var produced = unit.Conditions.Where(item => item.Key != Conditions.Concealed && item.Key != Conditions.Hidden)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var (name, value) in conditions)
            {
                produced[name] = value;
            }

            produced[Conditions.Concealed] = verdict.KeepsConcealment ? GameState.Condition(unit, Conditions.Concealed) : ConditionState.False;
            produced[Conditions.Hidden] = ConditionState.False;
            return lineage == EffectVerdict.Deployed
                ? ("lineage", new LineageRecorded(LineageAction.Deployed, [unit.Id],
                    [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced),
                        new NewInstance($"{attemptId}-{unit.Id}-2", reference.Kind, reference.Id, unit.Side, unit.Position, null, new Dictionary<string, ConditionState>(produced, StringComparer.Ordinal))]))
                : ("lineage", new LineageRecorded(lineage == EffectVerdict.Reduced ? LineageAction.Reduced : LineageAction.Replaced, [unit.Id],
                    [new NewInstance($"{attemptId}-{unit.Id}", reference.Kind, reference.Id, unit.Side, unit.Position, null, produced)]));
        }

        return conditions.Count == 0 ? null : ("conditions-changed", new ConditionsChanged(unit.Id, conditions));
    }

    /// <summary>
    /// The Fire package's refusals that rest on the firing side's own group and the map alone (pass 31d, design D8; each read against the package's
    /// pre-check): a FT that does not fire alone (A22.31), a firer that may not fire or uses no weapon, one that has fired, a weapon that may not
    /// fire, firers of two sides, a director who may not direct, and a fire kind its phase does not have. None reads a unit of the target Location.
    /// </summary>
    private static readonly string[] OwnGroupCodes =
    [
        "asl.a1.fire.flamethrower-outside", "asl.a1.fire.firer-outside", "asl.a1.fire.firer-already-fired", "asl.a1.fire.weapon-outside",
        "asl.a1.fire.firers-of-two-sides", "asl.a1.fire.director-outside", "asl.a1.fire.phase-outside",
    ];

    /// <summary>
    /// A refusal for range says what was out of range (pass 31c, design section 14; A7.21, A7.22): the package's one sentence for
    /// <c>out-of-range</c> gives way to a line for each firer and weapon, with its range, its Normal Range, and how far it fires. The code stays.
    /// A refusal records nothing, so no recorded game reads this.
    /// </summary>
    private static (string[] Reasons, IReadOnlyList<string> Named) RangeNamed(FireAttack part, string[] reasons) => ScenarioA1ResultTables.RangeNamed(part, reasons);

    /// <summary>
    /// The map's facts of an attack: for each firer's Location, its range, level, and LOS and its attributed Hindrance; the
    /// target's terrain; for a group across Locations, whether each Location is ADJACENT to another (A7.5); and for
    /// Subsequent First Fire, whether no target is farther than the closest Known enemy unit (A8.3). Backlog pass 10 adds the
    /// levels between firers and target, the wall or hedge TEM, Height Advantage, a Bypassing target's terrain, Snap Shots,
    /// TPBF, and Hazardous Movement (rulings R10.4 to R10.8, R10.13, R10.14).
    /// </summary>
    private (FireAttack? Facts, string? Reason) FireMapFacts(GameState state, FireAttack attack, BoardLocation target, IReadOnlyList<GameEvent>? history = null)
    {
        // Rules decides each block (pass 32.c); the map and the state are read here and handed over, a read made where the old body made it.
        var targetRead = ReadLocation(state, target);
        var terrainKey = targetRead is null ? null : TerrainKey(targetRead);
        if (ScenarioA1FireMapRules.TargetTerrainBar(targetRead is not null, terrainKey, (targetRead?.Level.Terrain ?? targetRead?.Hex.Center.Terrain)?.Name) is { } targetBar)
        {
            return (null, targetBar);
        }

        var terrain = terrainKey!;

        // Rulings R10.5, R10.6: walls and hedges are reviewed for Infantry fire; ordnance, a vehicle's fire, and Residual FP keep refusing them.
        var direct = ScenarioA1FireMapRules.IsDirectFire(attack);
        if (ScenarioA1FireMapRules.HexsideTerrainBar(direct, attack.FireKind, targetRead!.Hex.Hexsides.Any(side => side.HexsideTerrain is not null || side.Cliff)) is { } hexsideBar)
        {
            return (null, hexsideBar);
        }

        // B15.6 (ruling R5.19): grain is Open Ground outside June to September.
        var (grainBar, grainTerrain) = ScenarioA1FireMapRules.GrainTerrain(terrain, state.ScenarioMonth, state.Phase);
        if (grainBar is not null)
        {
            return (null, grainBar);
        }

        terrain = grainTerrain;

        // A4.3, A4.34, B23.31 (ruling R10.7): a stack in Bypass is in the other terrain of the hexsides it moves along, not in the obstacle.
        var bypassed = state.Phase == "mph" && state.Movement is { Bypass: { Count: > 0 } sides } bypassing && bypassing.Location == target ? sides : null;
        var lane = ScenarioA1FireMapRules.UsesBypassLane(direct, attack.FireKind, state.Phase, bypassed is not null) ? bypassed : null;
        if (lane is not null)
        {
            var (bypassBar, bypassTerrain) = ScenarioA1FireMapRules.BypassTerrain(
                [.. lane.Select(side => HexsideAt(state, target, side)).Select(side => new BypassHexsideFacts(side?.Terrain?.Name, side?.Terrain?.IsRoad == true))],
                direct, (attack.Firers ?? []).Any(item => item.LocationId == target.ToString()), attack.SnapShot == true,
                targetRead.Hex.Hexsides.Any(side => WallOn(side) is not null));
            if (bypassBar is not null)
            {
                return (null, bypassBar);
            }

            terrain = bypassTerrain!;
        }

        // D9.3, D10.3 (ruling R6.1): the wreck or AFV whose +1 TEM the target Location's Infantry may claim.
        var infantrySide = ScenarioA1FireMapRules.InfantrySideOf(attack.Targets!.Select(item => state.Unit(item.UnitId!)?.Side));
        attack = attack with
        {
            AfvCover = CoverAt(state, target, infantrySide)
        };
        if (attack.FireKind == ScenarioA1FireCalculator.ResidualFire)
        {
            // A8.2 (rulings R6.6, R9.6): Residual FP has no LOS Hindrance, but the SMOKE of its Location applies.
            return (ScenarioA1FireMapRules.ResidualMapFacts(attack, terrain, SmokeSources(state).Count(place => place == target)), null);
        }

        var firingSide = attack.VehicleFire is { } vehicleFire ? state.Unit(vehicleFire.VehicleId!)?.Side : state.Unit(attack.Firers![0].UnitId!)?.Side;
        var targetHeight = targetRead.Hex.BaseLevel + target.Level;

        // A8.15 (ruling R10.13): a Snap Shot is traced to both ends of the hexside the stack crossed into the target hex.
        BoardLocation? snapHexside = null;
        BoardLocation? left = null;
        if (attack.SnapShot == true)
        {
            var came = state.Movement is { From: { } from } snapped && snapped.Location == target ? from : null;
            var crossedSide = came is null ? null : SideToward(state, target, came);
            if (ScenarioA1FireMapRules.SnapShotBar(direct, state.Phase, came is not null, crossedSide is not null,
                // A8.15, B9.42 (referee, pass 10): a wall, hedge, SMOKE, or rubble of either hex can modify a Snap Shot; that is not built.
                () => new[] { target, came! }.Any(hex => ReadLocation(state, hex) is not { } hexRead || hexRead.Hex.Hexsides.Any(side => WallOn(side) is not null)
                    || IsRubbleTerrain(TerrainKey(hexRead)) || HasSmoke(state, hex))) is { } snapBar)
            {
                return (null, snapBar);
            }

            snapHexside = new BoardLocation(target.Board, target.Hex, 0, crossedSide!.Value);
            left = came;
        }

        LosReadFacts? Facts(LosResult? los) => los is null ? null
            : new LosReadFacts(los.Status is LosStatus.Clear or LosStatus.Blocked, los.Status.ToString(), los.Reason, los.IsBlocked == true, los.Range,
                [.. los.Hindrances.Select(entry => new LosHindranceFacts(entry.Range, entry.Value, entry.Terrains))]);

        var perLocation = new Dictionary<string, (int Range, bool SameLevel, FireLos Los, int Height)>(StringComparer.Ordinal);
        foreach (var location in (attack.Firers ?? []).Select(item => item.LocationId!).Append(attack.FirerLocationId!).Distinct(StringComparer.Ordinal))
        {
            var from = BoardLocation.Parse(location);
            var firerRead = ReadLocation(state, from);

            // B16.32, A7.212, A7.21, D7.22 (rulings R10.1, R10.14): the firer's Location; the two state scans are read when asked.
            var verdict = ScenarioA1FireMapRules.FirerLocation(new FirerLocationFacts(from.ToString(), firerRead is not null, firerRead is null ? null : TerrainKey(firerRead),
                direct, from == target, attack.SnapShot == true, state.Phase,
                () => state.At(from).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != firingSide && KnownEnemy(unit)
                    && !(LiveFire.IsVehicle(unit) && !HasVehicleMg(unit) && unit.Definition is { } vehicleDefinition
                        && FireReference.Value.Definitions.GetValueOrDefault(vehicleDefinition.Definition)?.Unarmored == true)),
                () => state.Movement is { Vehicle: true, Reaction: false } moving && moving.Movers.Any(id => state.Location(id)?.Location == from)));
            if (verdict.Refusal is { } locationBar)
            {
                return (null, locationBar);
            }

            var height = firerRead!.Hex.BaseLevel + from.Level;
            if (verdict.OwnLocation)
            {
                perLocation[location] = (0, true, ScenarioA1FireMapRules.OwnLocationLos, height);
                continue;
            }

            LosResult los;
            int range;
            if (snapHexside is { } hexside)
            {
                var (first, second) = LosToHexside(state, from, hexside);
                var (snapLosBar, takeFirst, snapRange) = ScenarioA1FireMapRules.SnapShotLos(Facts(first), Facts(second), HexDistance(state, from, target), HexDistance(state, from, left!));
                if (snapLosBar is not null)
                {
                    return (null, snapLosBar);
                }

                los = takeFirst ? first! : second!;
                range = snapRange;
            }
            else
            {
                var read = Los(state, from, target);
                if (ScenarioA1FireMapRules.LosBar(Facts(read)) is { } losBar)
                {
                    return (null, losBar);
                }

                los = read!;
                range = los.Range;
            }

            // A4.34 (ruling R10.7): LOS to a Bypassing stack's hex center must cross a hexside it Bypasses; the vertex LOS is not built.
            if (ScenarioA1FireMapRules.BypassLosBar(lane is not null, lane is null ? null : LosEntrySides(state, target, from)?.Select(side => (int)side).ToArray(),
                [.. (lane ?? []).Select(side => (int)side)]) is { } bypassLosBar)
            {
                return (null, bypassLosBar);
            }

            // A6.7: the Hindrances by range; an AFV or wreck, and a burning wreck's smoke, read for the ranges with a map Hindrance (D9.4, B25.2; rulings R6.2, R6.3).
            var sameLevel = height == targetHeight;
            var (hindranceBar, fireLos) = ScenarioA1FireMapRules.LocationLos(Facts(los)!, state.ScenarioMonth, sameLevel,
                mapRanges => VehicleHindrance(state, from, target, los, sameLevel, mapRanges));
            if (hindranceBar is not null)
            {
                return (null, hindranceBar);
            }

            perLocation[location] = (range, sameLevel, fireLos!, height);
        }

        // A7.5: the group's facts from its Locations, each ADJACENT to another of them when the group spans Locations.
        var facts = ScenarioA1FireMapRules.GroupMapFacts(attack, terrain, targetHeight, perLocation,
            (one, two) => IsAdjacent(state, BoardLocation.Parse(one), BoardLocation.Parse(two)));

        if (direct)
        {
            var sources = perLocation.Where(pair => pair.Value.Range > 0).Select(pair => (From: BoardLocation.Parse(pair.Key), pair.Value.Range, pair.Value.Height)).ToArray();

            // B9.3 to B9.41 (rulings R10.5, R10.6): the wall or hedge TEM; a Snap Shot, TPBF, and a Bypassing stack take none.
            if (ScenarioA1FireMapRules.WantsWallTem(direct, attack.SnapShot == true, lane is not null, sources.Length, perLocation.Count))
            {
                var (wall, wallReason) = HexsideTemAt(state, target, [.. sources.Select(item => (item.From, item.Range))], firingSide!, history);
                if (wallReason is not null)
                {
                    return (null, wallReason);
                }

                facts = facts with
                {
                    HexsideTem = wall
                };
            }

            // B10.31 (ruling R10.4): Height Advantage over every firer; A4.62 (ruling R10.8): a crew pushing its Gun is under Hazardous Movement.
            var pushedGun = state.Phase == "mph" && state.Movement is { PushedGun: { } pushed } pushing && pushing.Location == target ? pushed : null;
            facts = facts with
            {
                HeightAdvantage = ScenarioA1FireMapRules.HeightAdvantage(sources.Length, perLocation.Count,
                    () => HeightAdvantageAt(state, target, targetRead, [.. sources.Select(item => (item.From, item.Height))], attack.SnapShot == true)),
                HazardousMovement = ScenarioA1FireMapRules.HazardousMovement(state.Phase, pushedGun is not null,
                    pushedGun is not null && state.Find(pushedGun) is EquipmentInstance { Holding: { } manning } ? manning.Holder : null, attack),
            };
        }

        if (ScenarioA1FireMapRules.ReadsSubsequentFirstFireRange(attack))
        {
            // A8.3: no farther than the closest armed, Known enemy unit, from each firer's Location: a search, with the fact reader of the design's D4.
            var side = state.Unit(attack.Firers![0].UnitId!)!.Side;
            var table = new LosTable(this, state);
            int[] enemies = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && unit.Kind != UnitKinds.Dummy
                    && VisibleTo(unit, side) && GameState.Condition(unit, Conditions.Captured) != ConditionState.True
                    && (!LiveFire.IsVehicle(unit) || HasVehicleMg(unit)))
                .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().Select(table.Index)];
            facts = facts with
            {
                WithinSubsequentFirstFireRange = ScenarioA1FireMapRules.WithinSubsequentFirstFireRange(
                    [.. perLocation.Select(pair => (table.Index(BoardLocation.Parse(pair.Key)), pair.Value.Range))], enemies, table),
            };
        }

        // Backlog pass 16 (rulings R16.2, R16.3, R16.11 to R16.14): night and weather.
        return NightAndWeatherFacts(state, facts, target, targetRead.Hex.BaseLevel + (targetRead.Hex.Center.Terrain?.Height ?? 0), perLocation);
    }

    /// <summary>A table of Locations by index, with the LOS between two of them as Rules asks for it (the pass 32 design, D4; pass 32.c).</summary>
    private sealed class LosTable(GamePlanner planner, GameState state) : ILosFactReader
    {
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        public LosFacts? Los(int fromLocation, int toLocation) =>
            planner.Los(state, locations[fromLocation], locations[toLocation]) is { } result ? new LosFacts(result.Status == LosStatus.Clear, result.Range) : null;
    }
}
