namespace LimboDancer.Domains.Asl.Rules;

/// <summary>D2.32/D2.321 (p. 197): bounded Bypass geometry and local turret-fire components.</summary>
public static class ScenarioA1BypassTurretCalculator
{
    // Compatibility refusals until callers adopt the new Bypass position model. Keep their recorded text.
    internal static string? LegacyTurretStepRefusal(string id, bool bypass) => bypass
        ? $"play.move-vehicle-turret: {id} is in Bypass, where the TCA has its own four positions; that is not built (D2.32, D2.321)"
        : null;

    internal static string? LegacyMotionChangeRefusal(string id, bool bypass, bool facingNamed, bool turretNamed) =>
        bypass && (facingNamed || turretNamed)
            ? $"play.motion-attempt-bypass: {id} is in Bypass, where a CA has its own positions; that is not built (D2.32, D2.33). Attempt Motion without a change of VCA or TCA"
            : null;

    public const int CoordinateLimit = 1000000;
    public static bool Valid(BypassHex hex) => Math.Abs((long)hex.Q) <= CoordinateLimit && Math.Abs((long)hex.R) <= CoordinateLimit;
    internal static bool Defined<T>(T? value) where T : struct, Enum => value is null || Enum.IsDefined(value.Value);
    internal static bool Valid(BypassFrameFacts? facts)
    {
        if (facts is null)
        {
            return true;
        }
        if (facts.Obstacle is { } a && !Valid(a) || facts.Across is { } b && !Valid(b) || facts.Front is { } f && !Valid(f))
        {
            return false;
        }
        if (facts.Obstacle is { } first && facts.Across is { } second && !Adjacent(first, second)
            || facts.Obstacle is { } obstacle && facts.Front is { } front && !Adjacent(obstacle, front)
            || facts.Across is { } across && facts.Front is { } apex && !Adjacent(across, apex))
        {
            return false;
        }
        return facts.Obstacle is not { } one || facts.Across is not { } two || facts.Front is not { } three
            || SumMinus(one, two, three) is not null;
    }
    internal static BypassHex? SumMinus(BypassHex a, BypassHex b, BypassHex c)
    {
        var q = checked((long)a.Q + b.Q - c.Q);
        var r = checked((long)a.R + b.R - c.R);
        return Math.Abs(q) <= CoordinateLimit && Math.Abs(r) <= CoordinateLimit ? new((int)q, (int)r) : null;
    }
    internal static BypassHex Rotate(BypassHex h) => new(-h.R, h.Q + h.R);
    internal static BypassHex Minus(BypassHex a, BypassHex b) => new(a.Q - b.Q, a.R - b.R);
    internal static bool Adjacent(BypassHex a, BypassHex b)
    {
        long q = (long)a.Q - b.Q, r = (long)a.R - b.R;
        return Math.Max(Math.Max(Math.Abs(q), Math.Abs(r)), Math.Abs(q + r)) == 1;
    }

    public static BypassFrameVerdict Frame(BypassFrameFacts? facts)
    {
        if (!Valid(facts))
        {
            return new(BypassDisposition.NotEvaluated, null, "The supplied frame is malformed or outside the supported bounds.");
        }
        if (facts?.Obstacle is not { } a || facts.Across is not { } b || facts.Front is not { } f)
        {
            return new(BypassDisposition.NotEvaluated, null, "The occupied Bypass frame is unknown.");
        }
        if (!Adjacent(a, b) || !Adjacent(a, f) || !Adjacent(b, f))
        {
            return new(BypassDisposition.NotEvaluated, null, "The three hexes must form one CAFP.");
        }
        var rear = SumMinus(a, b, f)!.Value;
        var (turns, reflected) = Orientation(a, b, f);
        var facing = ((reflected ? 3 : 0) - turns + 6) % 6;
        return new(BypassDisposition.Evaluated, new(a, b, f, rear, facing), "Occupied Bypass frame (D2.32).");
    }

    private static (int Turns, bool Reflected) Orientation(BypassHex a, BypassHex b, BypassHex f)
    {
        var across = Minus(b, a);
        var front = Minus(f, a);
        for (var n = 0; n < 6; n++)
        {
            if (across == new BypassHex(0, 1))
            {
                return (n, front == new BypassHex(-1, 1));
            }
            across = Rotate(across);
            front = Rotate(front);
        }
        throw new InvalidOperationException("Only validated adjacent frames have an orientation.");
    }

    public static BypassSectorVerdict Sector(BypassFrameFacts? facts, BypassHex? target)
    {
        var frame = Frame(facts);
        if (frame.Frame is not { } f)
        {
            return new(BypassDisposition.NotEvaluated, null, frame.Reason);
        }
        if (target is not { } t || !Valid(t))
        {
            return new(BypassDisposition.NotEvaluated, null, "Target hex is unknown or invalid.");
        }
        var (turns, reflected) = Orientation(f.Obstacle, f.Across, f.Front);
        var h = Minus(t, f.Obstacle);
        for (var n = 0; n < turns; n++)
        {
            h = Rotate(h);
        }
        if (reflected)
        {
            h = new(-h.Q, h.Q + h.R);
        }
        BypassTurretPosition sector;
        if (h.Q >= 1 && h.R <= 0 && h.R >= 1 - h.Q)
        {
            sector = BypassTurretPosition.Front;
        }
        else if (h.Q <= -1 && h.R >= 1 && h.R <= -h.Q)
        {
            sector = BypassTurretPosition.Rear;
        }
        else
        {
            sector = h.Q + 2L * h.R > 1 ? BypassTurretPosition.Left : BypassTurretPosition.Right;
        }
        if (reflected && sector is BypassTurretPosition.Left or BypassTurretPosition.Right)
        {
            sector = sector == BypassTurretPosition.Left ? BypassTurretPosition.Right : BypassTurretPosition.Left;
        }
        return new(BypassDisposition.Evaluated, sector, "Occupied-hex sector; LOS is a separate verdict (D2.32).");
    }

    public static BypassTurretChangeVerdict TurretChange(BypassTurretPosition? from, BypassTurretPosition? to, BypassTurnDirection? direction)
    {
        if (!Defined(from) || !Defined(to) || !Defined(direction) || from is null || to is null)
        {
            return new(BypassDisposition.NotEvaluated, null, null, "Turret positions or direction are unknown or invalid.");
        }
        var clockwise = ((int)to.Value - (int)from.Value + 4) % 4;
        if (clockwise == 0)
        {
            return direction is null ? new(BypassDisposition.Evaluated, 0, false, "TCA unchanged.") : new(BypassDisposition.NotEvaluated, null, null, "Full revolutions are unsupported.");
        }
        if (direction is null && clockwise == 2)
        {
            return new(BypassDisposition.NotEvaluated, null, null, "Choose a direction for the two-step turn.");
        }
        var forward = direction == BypassTurnDirection.Clockwise || direction is null && clockwise == 1;
        var steps = forward ? clockwise : 4 - clockwise;
        var side = false;
        for (var step = 1; step <= steps; step++)
        {
            side |= (((int)from.Value + (forward ? step : -step) + 4) % 4) is 1 or 3;
        }
        return new(BypassDisposition.Evaluated, steps, side, "Steps are Target Facings, not hexspines (D2.321).");
    }

    public static BypassTurretVerdict FireAdjustment(BypassTurretFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        BypassTurretVerdict Unknown(string why) => new(BypassDisposition.NotEvaluated, null, null, null, null, why);
        if (!Valid(facts.Frame) || facts.Target is { } bad && !Valid(bad) || !Defined(facts.Current) || !Defined(facts.Proposed)
            || !Defined(facts.Direction) || !Defined(facts.Traverse) || !Defined(facts.Weapon) || !Defined(facts.Mode))
        {
            return Unknown("A supplied fact is invalid.");
        }
        if (facts.InBypass == false)
        {
            return new(BypassDisposition.NotApplicable, null, null, null, null, "Not a Bypass shot.");
        }
        if (facts.InBypass is null)
        {
            return Unknown("Bypass context is unknown.");
        }
        if (facts.Weapon == BypassWeaponKind.Bow || facts.Traverse == BypassTraverse.Nt)
        {
            return new(BypassDisposition.Refused, null, null, null, null, "A bow or non-turreted weapon cannot use Bypass turret positions.");
        }
        if (facts.Weapon is null or BypassWeaponKind.Unsupported || facts.Traverse is null or BypassTraverse.Unsupported || facts.Mode is null)
        {
            return Unknown("A supported weapon, traverse profile and fire mode are required.");
        }
        if (facts.SpecialTerrain != false)
        {
            return Unknown("Ordinary Bypass firing-point terrain must be known; special terrain composition is unsupported.");
        }
        var sector = Sector(facts.Frame, facts.Target);
        if (sector.Sector is not { } target)
        {
            return Unknown(sector.Reason);
        }
        if (facts.Target == facts.Frame?.Obstacle)
        {
            return Unknown("Same-hex firing composition is unsupported.");
        }
        var change = TurretChange(facts.Current, facts.Proposed, facts.Direction);
        if (change.Steps is not { } steps)
        {
            return Unknown(change.Reason);
        }
        if (facts.Proposed != target)
        {
            return new(BypassDisposition.Refused, target, steps, null, null, "The selected TCA does not contain the target hex.");
        }
        if (facts.Mode == BypassFireMode.BoundingFirstFire)
        {
            if (steps > 0)
            {
                return new(BypassDisposition.Refused, target, steps, null, null, "Bounding First Fire cannot turn as part of the shot (C5.13).", "C5.13", 172);
            }
            if (facts.EarlierMpTcaChange != false)
            {
                return Unknown("Earlier MP turret-change history is unknown or requires the separate side-change DRM composition.");
            }
            return new(BypassDisposition.Evaluated, target, 0, 0, 0, "No earlier MP turret change; no local Case A or side-change DRM (C5.13).", "C5.13", 172);
        }
        var chargedSteps = steps;
        if (steps == 0 && target is BypassTurretPosition.Left or BypassTurretPosition.Right)
        {
            bool? exception;
            if (facts.Weapon == BypassWeaponKind.Ordnance)
            {
                exception = facts.ApplicableAcquisition;
            }
            else
            {
                exception = facts.SameKnownTarget == false || facts.SameTargetFacing == false ? false
                    : facts.SameKnownTarget == true && facts.SameTargetFacing == true ? true : (bool?)null;
            }
            if (exception is null)
            {
                return Unknown("The unchanged-side exception is unknown.");
            }
            chargedSteps = exception.Value ? 0 : 1;
        }
        var profile = facts.Traverse switch
        {
            BypassTraverse.T => "t",
            BypassTraverse.St => "st",
            BypassTraverse.Rst => "rst",
            _ => "1mt"
        };
        return new(BypassDisposition.Evaluated, target, steps, ScenarioA1ArmorCalculator.CaseA(profile, chargedSteps),
            change.EntersSide == true ? 1 : 0, "Local Case A and separate To Hit side-change DRM only (D2.321); no maintained-history or IFT composition.");
    }
}
