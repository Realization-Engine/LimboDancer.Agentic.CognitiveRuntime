namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Current possession-operation eligibility: A4.43-.44, PDF p. 50. Legacy phase helpers retain their recorded reading.</summary>
public static class ScenarioA1SupportWeaponPossessionRules
{
    /// <summary>A4.431/.44, PDF p. 50: voluntary transfer, retaining the established R13.4/R14.5 reading.</summary>
    public static SupportWeaponReadiness TransferRefusal(SupportWeaponTransferFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase is not ("rph" or "pfph" or "mph" or "dfph" or "afph" or "rtph" or "aph" or "ccph"))
        {
            return SupportWeaponReadiness.NotEvaluated("The phase is not known.");
        }
        if (facts.KnownWirePresent)
        {
            return SupportWeaponReadiness.NotEvaluated("Transfer across Wire cannot be evaluated: above/below-Wire positions are not recorded.");
        }
        if (!ScenarioA1SupportWeaponRules.TransferPhase(facts.Phase, true))
        {
            return SupportWeaponReadiness.Refused("play.transfer-phase: a SW is transferred in the RPh or at the start of the APh (A4.431)");
        }

        if (facts.GiverGoodOrder is null || facts.RecipientGoodOrder is null)
        {
            return SupportWeaponReadiness.NotEvaluated("A participant's condition cannot be evaluated.");
        }
        if (facts.VehicleInMotion)
        {
            return SupportWeaponReadiness.NotEvaluated("Transfer involving a Motion vehicle requires its own carriage facts (A4.431).");
        }
        if (ScenarioA1PassengerActionRules.TransferBar(facts.Giver, facts.GiverVehicle, facts.Recipient, facts.RecipientVehicle) is { } passengerRefusal)
        {
            return SupportWeaponReadiness.Refused(passengerRefusal);
        }
        if (!facts.DifferentUnits || facts.GiverGoodOrder != true || facts.RecipientGoodOrder != true || facts.GiverPinned || facts.RecipientPinned
            || !facts.SameSide || !facts.SameLocation || !facts.BothInfantry)
        {
            return SupportWeaponReadiness.Refused("play.transfer-unit: a SW passes between Good Order unpinned Infantry of one side in one Location (A4.431)");
        }

        if (facts.RecipientUnarmed)
        {
            return SupportWeaponReadiness.Refused($"play.transfer-unarmed: {facts.Recipient} is Unarmed and uses no SW (A20.5)");
        }

        if (!facts.Possessed)
        {
            return SupportWeaponReadiness.Refused($"play.transfer-weapon: {facts.Giver} does not possess '{facts.Weapon}'");
        }

        if (facts.RecoveredThisPhase)
        {
            return SupportWeaponReadiness.Refused($"play.transfer-weapon: {facts.Weapon} was Recovered this phase and is not transferred in it (A4.44)");
        }

        if (facts.Phase == "rph" && (facts.GiverRallyAction || facts.RecipientRallyAction))
        {
            var acted = facts.GiverRallyAction ? facts.Giver : facts.Recipient;
            return SupportWeaponReadiness.Refused($"play.rph-action: {acted} has already taken a different Rally phase action (A3.1)");
        }

        return !ScenarioA1SupportWeaponRules.TransferPhase(facts.Phase, facts.Phasing) || facts.Phase == "aph" && facts.AdvanceStarted
            ? SupportWeaponReadiness.Refused("play.transfer-phase: transfer before any unit advances (A4.431)") : SupportWeaponReadiness.Allowed;
    }

    /// <summary>A4.43: withdrawal and rout equipment choices belong to their enclosing action, not a standalone drop.</summary>
    public static string? DropRefusal(string? phase, bool phasing, bool active, bool possessed, bool onMap,
        bool broken, bool movementEnded, bool advanced = false)
    {
        if (!active || !possessed || !onMap)
        {
            return "Select a unit's possessed weapon on the map.";
        }

        return phase switch
        {
            "ccph" => "Choose equipment to leave in the withdrawal workflow.",
            "rtph" => "Choose the equipment to carry in the rout workflow.",
            "mph" when ScenarioA1SupportWeaponRules.DropPhase(phase, phasing) && !broken && !movementEnded => null,
            "aph" when ScenarioA1SupportWeaponRules.DropPhase(phase, phasing) && !broken && !advanced => null,
            _ => "Drop a weapon during its unbroken holder's movement or Advance phase.",
        };
    }

    /// <summary>A4.44: recovery from a friendly broken holder is available only to a SMC and has no MF cost.</summary>
    public static bool FromBrokenHolder(bool possessed, bool friendly, bool brokenHolder, bool recoveringSmc) =>
        possessed && friendly && brokenHolder && recoveringSmc;

    /// <summary>A4.44: ordinary MPh recovery costs one MF; the friendly broken-holder exception costs none.</summary>
    public static int RecoveryMf(string? phase, bool fromBrokenHolder) => phase == "mph" && !fromBrokenHolder ? 1 : 0;

    /// <summary>A4.44 (p. 50), A8.1 (p. 59), C6.17 (p. 173): spending MF in place opens fire without resetting shots in that Location.</summary>
    public static SupportWeaponRecoveryMovement RecoveryMovement(string? phase, bool fromBrokenHolder,
        bool sameMoverInLocation, int priorHalfMf, int priorStep, int priorShots)
    {
        var cost = RecoveryMf(phase, fromBrokenHolder);
        return new(cost > 0, cost * 2 + (sameMoverInLocation ? priorHalfMf : 0), priorStep + (cost > 0 ? 1 : 0),
            cost > 0 && ScenarioA1SequenceProjection.ClearsShotsHere(!sameMoverInLocation, priorShots));
    }

    public static bool RecoveryWindowOpen(bool rallyAttempted, bool repaired, bool otherAction, bool transferred) =>
        !rallyAttempted && !repaired && !otherAction && !transferred;

    public static bool RecoveryWeaponApplicable(bool possessed, bool brokenHolder) => !possessed || brokenHolder;

    public static bool RecordsRallyTransfer(string phase) => phase == "rph";

    public static bool RecoveryEnemyBlocks(bool known, bool unarmed, bool vehicle, bool abandoned) =>
        known && !unarmed && ScenarioA1RoutCalculator.Armed(vehicle, abandoned);

    /// <summary>A4.44: phase, participant, possession and repeat-attempt checks for routine recovery.</summary>
    public static SupportWeaponReadiness RecoveryRefusal(SupportWeaponRecoveryFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Phase is not ("rph" or "pfph" or "mph" or "dfph" or "afph" or "rtph" or "aph" or "ccph"))
        {
            return SupportWeaponReadiness.NotEvaluated("The phase is not known.");
        }
        if (facts.KnownWirePresent)
        {
            return SupportWeaponReadiness.NotEvaluated("Recovery across Wire cannot be evaluated: above/below-Wire positions are not recorded.");
        }
        if (!facts.ConditionsKnown)
        {
            return SupportWeaponReadiness.NotEvaluated("The recovering unit's condition cannot be evaluated.");
        }
        if (facts.Terrain is null)
        {
            return SupportWeaponReadiness.NotEvaluated("Recovery cannot be evaluated without this Location's terrain.");
        }
        if (facts.Aboard || facts.Terrain.Contains("wire", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("panji", StringComparison.OrdinalIgnoreCase) || facts.Terrain.Contains("crest", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("jungle", StringComparison.OrdinalIgnoreCase) || facts.Terrain.Contains("kunai", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("bamboo", StringComparison.OrdinalIgnoreCase))
        {
            return SupportWeaponReadiness.NotEvaluated("Recovery in this terrain or vehicle position is not supported.");
        }
        var fromBrokenHolder = FromBrokenHolder(facts.Held, facts.FriendlyHolder, facts.BrokenHolder, facts.RecoveringSmc);
        if (!ScenarioA1SupportWeaponRules.RecoverPhase(facts.Phase, facts.Phasing))
        {
            return SupportWeaponReadiness.Refused("Recover during Rally or the recovering unit's Movement phase.");
        }

        if (facts.Phase == "rph" && !facts.RallyRecoveryWindowOpen)
        {
            return SupportWeaponReadiness.Refused("The Rally recovery step has ended.");
        }

        if (facts.Phase == "mph" && (facts.ReactionPending || facts.AnotherMoverActive))
        {
            return SupportWeaponReadiness.Refused(facts.ReactionPending ? "Resolve defensive fire first." : "Finish the current unit's move first.");
        }

        if (!facts.Active || !facts.GoodOrder || facts.Pinned || !facts.Infantry || facts.Unarmed)
        {
            return SupportWeaponReadiness.Refused("Recovery needs unpinned, armed, Good Order Infantry outside Bypass.");
        }

        if (ScenarioA1SupportWeaponRules.RecoveryBypassBar("The recovering unit", facts.Bypassing) is { } bypass)
        {
            return SupportWeaponReadiness.Refused(bypass);
        }

        if (!facts.WeaponActive || !facts.SameLocation)
        {
            return SupportWeaponReadiness.Refused("Choose a weapon in the recovering unit's Location.");
        }

        if (facts.Held && !fromBrokenHolder)
        {
            return SupportWeaponReadiness.Refused("Only a SMC may recover a possessed weapon from a friendly broken unit.");
        }

        if (facts.ArmedKnownEnemy)
        {
            return SupportWeaponReadiness.Refused("An armed Known enemy in this Location prevents recovery.");
        }

        if (facts.Attempted)
        {
            return SupportWeaponReadiness.Refused("This unit already attempted to recover this weapon this phase.");
        }

        if (facts.Phase == "rph" && facts.RallyActionTaken)
        {
            return SupportWeaponReadiness.Refused("This unit has already taken its Rally phase action.");
        }

        if (facts.Phase == "mph" && facts.MovementEnded)
        {
            return SupportWeaponReadiness.Refused("This unit has finished moving.");
        }

        if (RecoveryMf(facts.Phase, fromBrokenHolder) > 0 && facts.HalfMfRemaining is null)
        {
            return SupportWeaponReadiness.NotEvaluated("The unit's available MF cannot be evaluated.");
        }

        if (RecoveryMf(facts.Phase, fromBrokenHolder) > 0
            && (facts.TemporarilyImmobilized || ScenarioA1SupportWeaponRules.RecoveryMfShort(facts.HalfMfRemaining!.Value / 2, 0, false)))
        {
            return SupportWeaponReadiness.Refused("No MF available for recovery.");
        }

        return SupportWeaponReadiness.Allowed;
    }
}

public sealed record SupportWeaponRecoveryMovement(bool OpensFireWindow, int HalfMfInLocation, int Step, bool ClearsShotsHere);

/// <summary>Observed participants, possession and activity for the current voluntary-transfer reading.</summary>
public sealed record SupportWeaponTransferFacts(string? Phase, string Giver, string Recipient, string Weapon,
    bool DifferentUnits, bool? GiverGoodOrder, bool? RecipientGoodOrder, bool GiverPinned, bool RecipientPinned,
    bool SameSide, bool SameLocation, bool BothInfantry, string? GiverVehicle, string? RecipientVehicle,
    bool RecipientUnarmed, bool Possessed, bool RecoveredThisPhase, bool GiverRallyAction, bool RecipientRallyAction,
    bool Phasing, bool AdvanceStarted, bool VehicleInMotion = false, bool KnownWirePresent = false);

/// <summary>Facts for A4.44 routine recovery; event-triggered recovery requires its own causal opportunity.</summary>
public sealed record SupportWeaponRecoveryFacts(string? Phase, bool Phasing, bool Active, bool GoodOrder,
    bool Pinned, bool Infantry, bool Unarmed, bool Bypassing, bool WeaponActive, bool SameLocation,
    bool Held, bool FriendlyHolder, bool BrokenHolder, bool RecoveringSmc, bool ArmedKnownEnemy, bool Attempted, bool RallyActionTaken,
    bool MovementEnded, bool TemporarilyImmobilized, int? HalfMfRemaining,
    bool RallyRecoveryWindowOpen, bool ReactionPending, bool AnotherMoverActive,
    string? Terrain = null, bool Aboard = false, bool ConditionsKnown = true, bool KnownWirePresent = false);

/// <summary>Missing capability/data is distinct from a rule refusal.</summary>
public enum SupportWeaponReadinessStatus
{
    Allowed,
    Refused,
    NotEvaluated,
}

/// <summary>An operation's applicability and readiness, without generating a proposal or drawing dice.</summary>
public sealed record SupportWeaponReadiness(bool Applicable, SupportWeaponReadinessStatus Status, string? Reason)
{
    public static SupportWeaponReadiness Allowed { get; } = new(true, SupportWeaponReadinessStatus.Allowed, null);

    public static SupportWeaponReadiness Refused(string reason, bool applicable = true) => new(applicable, SupportWeaponReadinessStatus.Refused, reason);

    public static SupportWeaponReadiness NotEvaluated(string reason) => new(true, SupportWeaponReadinessStatus.NotEvaluated, reason);
}
