namespace LimboDancer.Domains.Asl.Rules;

/// <summary>D2.33 (pp. 197-198): local VCA turns, continuation and retained defensive position.</summary>
public static class ScenarioA1BypassVcaCalculator
{
    private static BypassVcaVerdict Unknown(string reason) => new(BypassDisposition.NotEvaluated, null, null, null, null, null, reason);
    private static BypassVcaVerdict Refused(string reason) => new(BypassDisposition.Refused, null, null, null, null, null, reason);
    private static BypassVcaVerdict? Context(bool? bypass) => bypass == true ? null : bypass == false
        ? new(BypassDisposition.NotApplicable, null, null, null, null, null, "Not a Bypass component.") : Unknown("Bypass context is unknown.");
    internal static bool Valid(BypassEdge? edge) => edge is null
        || (edge.One is not { } a || ScenarioA1BypassTurretCalculator.Valid(a)) && (edge.Two is not { } b || ScenarioA1BypassTurretCalculator.Valid(b))
        && (edge.One is not { } one || edge.Two is not { } two || ScenarioA1BypassTurretCalculator.Adjacent(one, two));
    private static bool SameEdge(BypassEdge edge, BypassHex a, BypassHex b) => edge.One == a && edge.Two == b || edge.One == b && edge.Two == a;
    internal static bool Valid(BypassPositionFacts? position) => position is null
        || ScenarioA1BypassTurretCalculator.Defined(position.Kind) && ScenarioA1BypassTurretCalculator.Valid(position.Frame)
        && (position.Hex is not { } h || ScenarioA1BypassTurretCalculator.Valid(h)) && (position.Facing is null or >= 0 and <= 5)
        && !(position.Kind == BypassPositionKind.Ordinary && position.Frame is not null)
        && !(position.Kind == BypassPositionKind.Bypass && (position.Hex is not null || position.Facing is not null));

    private static BypassFrame? Outgoing(BypassFrame occupied, BypassEdge edge)
    {
        BypassHex other;
        if (SameEdge(edge, occupied.Obstacle, occupied.Front))
        {
            other = occupied.Obstacle;
        }
        else if (SameEdge(edge, occupied.Across, occupied.Front))
        {
            other = occupied.Across;
        }
        else
        {
            return null;
        }
        var behind = other == occupied.Obstacle ? occupied.Across : occupied.Obstacle;
        if (ScenarioA1BypassTurretCalculator.SumMinus(other, occupied.Front, behind) is not { } next)
        {
            return null;
        }
        return ScenarioA1BypassTurretCalculator.Frame(new(other, occupied.Front, next)).Frame;
    }

    public static BypassVcaVerdict TurnAtCafp(BypassVcaTurnFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!ScenarioA1BypassTurretCalculator.Valid(facts.Frame) || !Valid(facts.Edge) || !ScenarioA1BypassTurretCalculator.Defined(facts.Purpose))
        {
            return Unknown("A supplied turn fact is invalid.");
        }
        if (Context(facts.InBypass) is { } context)
        {
            return context;
        }
        if (facts.Purpose == BypassTurnPurpose.FireOnly)
        {
            return Refused("A Bypass VCA cannot pivot merely to fire (D2.33).");
        }
        if (facts.PendingTurn == true)
        {
            return Refused("Complete the pending VCA turn's continuation before another turn (D2.33).");
        }
        if (facts.Purpose is null || facts.PendingTurn is null)
        {
            return Unknown("Turn purpose and pending-turn state must be known.");
        }
        var frame = ScenarioA1BypassTurretCalculator.Frame(facts.Frame);
        if (frame.Frame is not { } occupied)
        {
            return Unknown(frame.Reason);
        }
        if (facts.Edge?.One is null || facts.Edge.Two is null)
        {
            return Unknown("The requested connecting hexside is unknown.");
        }
        if (!SameEdge(facts.Edge, occupied.Obstacle, occupied.Front) && !SameEdge(facts.Edge, occupied.Across, occupied.Front))
        {
            return Refused("Choose one of the two connecting hexsides at the CAFP (D2.33).");
        }
        if (Outgoing(occupied, facts.Edge) is not { } next)
        {
            return Unknown("The connecting lane extends outside supported coordinates.");
        }
        return new(BypassDisposition.Evaluated, 2, next.Facing, true, null, null,
            "The separate turn costs 1 MP; entry and Reverse costs are separate. Continuation is required (D2.33).", Page: 197);
    }

    public static BypassVcaVerdict Continuation(BypassContinuationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!ScenarioA1BypassTurretCalculator.Valid(facts.Frame) || !Valid(facts.Edge) || !Valid(facts.Destination))
        {
            return Unknown("A supplied continuation fact is invalid or contradictory.");
        }
        if (Context(facts.InBypass) is { } context)
        {
            return context;
        }
        var frame = ScenarioA1BypassTurretCalculator.Frame(facts.Frame);
        if (frame.Frame is not { } old)
        {
            return Unknown(frame.Reason);
        }
        if (facts.PendingTurn is null || facts.Reverse is null || facts.Destination?.Kind is null)
        {
            return Unknown("Pending-turn, direction and destination kind must be known.");
        }
        var destination = facts.Destination;
        BypassFrame? outgoing = null;
        if (facts.PendingTurn == true)
        {
            if (facts.Edge?.One is null || facts.Edge.Two is null)
            {
                return Unknown("The pending turn's selected hexside is unknown.");
            }
            outgoing = Outgoing(old, facts.Edge);
            if (outgoing is null)
            {
                return Unknown("The pending turn does not name a connecting lane.");
            }
        }
        var facing = outgoing?.Facing ?? old.Facing;
        if (destination.Kind == BypassPositionKind.Ordinary)
        {
            if (destination.Hex is not { } hex || destination.Facing is null)
            {
                return Unknown("The ordinary destination hex and facing are required.");
            }
            if (hex == old.Obstacle)
            {
                return Refused("The vehicle cannot enter the obstacle it is currently bypassing (D2.33).");
            }
            if (outgoing is not null && facts.Reverse == false)
            {
                return Refused("After the turn, forward movement must reach the next CAFP (D2.33).");
            }
            var expected = facts.Reverse == true ? outgoing?.Rear ?? old.Rear : old.Front;
            if (hex != expected || destination.Facing != facing)
            {
                return Refused("The destination is not the forward/rear exit for this movement orientation (D2.33).");
            }
        }
        else
        {
            var next = ScenarioA1BypassTurretCalculator.Frame(destination.Frame);
            if (next.Frame is not { } entered)
            {
                return Unknown(next.Reason);
            }
            if (facts.Reverse == true)
            {
                return facts.PendingTurn == true
                    ? Refused("This post-turn continuation requires Reverse into a new rear hex, not a Bypass lane (D2.33).")
                    : Unknown("Reverse Bypass without a pending turn requires separate D2.22 geometry.");
            }
            if (outgoing is null)
            {
                return Refused("Select the connecting lane with a VCA turn before continuing along it (D2.33).");
            }
            if (!SameEdge(new(entered.Obstacle, entered.Across), outgoing.Obstacle, outgoing.Across)
                || entered.Front != outgoing.Front || entered.Facing != outgoing.Facing)
            {
                return Refused("The entry must follow the selected lane to its next CAFP (D2.33).");
            }
        }
        return new(BypassDisposition.Evaluated, null, facing, false, destination, null,
            "Local continuation geometry is valid; terrain, Reverse eligibility, Stop/Start and budget remain separate Rules prerequisites.");
    }

    public static BypassVcaVerdict EndMovement(BypassEndFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!ScenarioA1BypassTurretCalculator.Defined(facts.Cause) || facts.PendingTurn == true && facts.EntryCompleted == true)
        {
            return Unknown("End facts are invalid or contradictory.");
        }
        if (Context(facts.InBypass) is { } context)
        {
            return context;
        }
        if (facts.Cause == BypassEndCause.OtherForcedHalt)
        {
            return Unknown("Other forced-halt composition is unsupported.");
        }
        if (facts.Cause == BypassEndCause.Immobilized)
        {
            return facts.Immobilized == true ? new(BypassDisposition.ForcedEnd, null, null, false, null, null,
                "Known immobilization forces the end; ask DefensivePosition for the retained position (D2.33).")
                : Unknown("Immobilization must be known true for an immobilized end.");
        }
        if (facts.Cause is null || facts.PendingTurn is null)
        {
            return Unknown("End cause and pending-turn state are required.");
        }
        if (facts.Immobilized == true)
        {
            return Unknown("Known immobilization is a forced end, not a voluntary end.");
        }
        return facts.PendingTurn == true ? Refused("The pending turn cannot voluntarily end here, even if no continuation is affordable (D2.33).")
            : new(BypassDisposition.Evaluated, null, null, false, null, null, "No pending Bypass-turn obligation; other end-of-move Rules still apply.");
    }

    public static BypassVcaVerdict DefensivePosition(BypassDefensiveFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!ScenarioA1BypassTurretCalculator.Valid(facts.OldFrame) || !Valid(facts.Completed)
            || !ScenarioA1BypassTurretCalculator.Defined(facts.Stage) || !ScenarioA1BypassTurretCalculator.Defined(facts.OriginKind)
            || facts.Origin is { } invalid && !ScenarioA1BypassTurretCalculator.Valid(invalid)
            || facts.Stage is BypassMovementStage.AtOccupiedPosition or BypassMovementStage.TurnPending && facts.Completed is not null)
        {
            return Unknown("Defensive-position facts are invalid or contradictory.");
        }
        BypassPositionFacts position;
        if (facts.Stage == BypassMovementStage.EntryCompleted)
        {
            if (facts.Completed?.Kind is null)
            {
                return Unknown("The completed position is unknown.");
            }
            position = facts.Completed;
            if (position.Kind == BypassPositionKind.Ordinary)
            {
                return position.Hex is not null && position.Facing is not null
                    ? new(BypassDisposition.NotApplicable, null, null, false, position, null, "Completed ordinary exit: use ordinary-facing Rules, retaining the firing-origin kind.")
                    : Unknown("The completed ordinary position is incomplete.");
            }
        }
        else
        {
            if (Context(facts.InBypass) is { } context)
            {
                return context;
            }
            if (facts.Stage is null)
            {
                return Unknown("The movement stage is unknown.");
            }
            position = new(BypassPositionKind.Bypass, facts.OldFrame, null, null);
        }
        var frame = ScenarioA1BypassTurretCalculator.Frame(position.Frame);
        if (frame.Frame is not { } occupied)
        {
            return Unknown(frame.Reason);
        }
        if (facts.OriginKind != BypassOriginKind.HexCenter)
        {
            return Unknown("Incoming CAFP-origin composition is unsupported, or origin kind is unknown.");
        }
        if (facts.Origin == occupied.Obstacle)
        {
            return Unknown("Same-hex defensive fire composition is unsupported.");
        }
        var sector = ScenarioA1BypassTurretCalculator.Sector(position.Frame, facts.Origin);
        if (sector.Sector is not { } value)
        {
            return Unknown(sector.Reason);
        }
        var facing = value == BypassTurretPosition.Front ? "front" : value == BypassTurretPosition.Rear ? "rear" : "side";
        bool? continuation = facts.Stage != BypassMovementStage.TurnPending || facts.Immobilized == true ? false
            : facts.Immobilized == false ? true : null;
        return new(BypassDisposition.Evaluated, null, null, continuation,
            position, facing, facts.Stage == BypassMovementStage.EntryCompleted ? "Use the completed occupied position, including after immobilization (D2.33)."
                : "Use the last occupied CAFP and Target Facing before the pending VCA change, including immobilization (D2.33).");
    }

    // Compatibility readings only. New callers use TurnAtCafp, Continuation and EndMovement.
    internal static bool LegacyMayMoveOn(int left, Func<IEnumerable<(int? HalfMp, bool All)>> entries,
        bool straddling, Func<bool> turnedAtCafp, Func<int> turnHalfMp) =>
        entries().Any(item => item.HalfMp is { } cost && !item.All && cost <= left)
            || (!(straddling && turnedAtCafp()) && turnHalfMp() + 2 <= left);

    internal static string? LegacyTurnRefusal(string id, bool straddling, Func<bool> turnedAtCafp) =>
        straddling && turnedAtCafp() ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)" : null;

    internal static string? LegacyStopRefusal(string id, bool straddling, Func<bool> turnedAtCafp, Func<bool> mayMoveOn) =>
        straddling && turnedAtCafp() && mayMoveOn()
            ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)"
            : null;

    internal static string? LegacyEndMoveRefusal(string id, bool halted, bool straddling, Func<bool> turnedAtCafp, Func<bool> mayMoveOn) =>
        !halted && straddling && turnedAtCafp() && mayMoveOn()
            ? $"play.end-move: {id} changed its VCA at its CAFP and must move on in Bypass before its move ends (D2.33)" : null;

    internal static IReadOnlyList<VehicleStepOption> LegacyMoveOptions(IVehicleStepFactReader reader, int here, int straddling,
        int facing, bool reverse, Func<bool> turnedAtCafp)
    {
        var options = new List<VehicleStepOption>();
        void Outright(int to, int from) => options.Add(new VehicleStepOption(to, from, null));
        void Lane(int one, int two)
        {
            foreach (var (obstacle, other) in new[] { (one, two), (two, one) })
            {
                if (reader.Bypassable(obstacle))
                {
                    options.Add(new VehicleStepOption(obstacle, here, other));
                }
            }
        }
        if (turnedAtCafp())
        {
            // D2.33: after a VCA change at its CAFP the vehicle goes on in Bypass along the hexside it now faces along.
            if (!reverse)
            {
                foreach (var (one, two) in new[] { (here, straddling) }.SelectMany(pair => reader.SharedNeighbors(pair.Item1, pair.Item2)
                    .SelectMany(third => new[] { (pair.Item1, third), (pair.Item2, third) })))
                {
                    if (reader.LaneEnds(one, two, facing) is ({ } _, { } behind) && (behind == here || behind == straddling))
                    {
                        Lane(one, two);
                    }
                }
            }

            return options;
        }

        var (front, rear) = reader.LaneEnds(here, straddling, facing);
        if ((reverse ? rear : front) is { } beyond)
        {
            Outright(beyond, straddling);
        }

        return options;
    }
}
