namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Current possession-operation eligibility: A4.43-.44, PDF p. 50. Legacy phase helpers retain their recorded reading.</summary>
public static class ScenarioA1SupportWeaponPossessionRules
{
    /// <summary>A4.431/.44, PDF p. 50: voluntary transfer, retaining the established R13.4/R14.5 reading.</summary>
    public static string? TransferRefusal(SupportWeaponTransferFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.KnownWirePresent)
        {
            return "Transfer across Wire cannot be evaluated: above/below-Wire positions are not recorded.";
        }
        if (facts.Phase is not ("rph" or "aph"))
        {
            return "play.transfer-phase: a SW is transferred in the RPh or at the start of the APh (A4.431)";
        }

        if (facts.GiverGoodOrder is null || facts.RecipientGoodOrder is null)
        {
            return "A participant's condition cannot be evaluated.";
        }
        if (facts.VehicleInMotion)
        {
            return "A vehicle in Motion prevents this transfer.";
        }
        if (!facts.DifferentUnits || facts.GiverGoodOrder != true || facts.RecipientGoodOrder != true || facts.GiverPinned || facts.RecipientPinned
            || !facts.SameSide || !facts.SameLocation || !facts.BothInfantry)
        {
            return "play.transfer-unit: a SW passes between Good Order unpinned Infantry of one side in one Location (A4.431)";
        }

        if (ScenarioA1PassengerActionRules.TransferBar(facts.Giver, facts.GiverVehicle, facts.Recipient, facts.RecipientVehicle) is { } aboardBar)
        {
            return aboardBar;
        }

        if (facts.RecipientUnarmed)
        {
            return $"play.transfer-unarmed: {facts.Recipient} is Unarmed and uses no SW (A20.5)";
        }

        if (!facts.Possessed)
        {
            return $"play.transfer-weapon: {facts.Giver} does not possess '{facts.Weapon}'";
        }

        if (facts.RecoveredThisPhase)
        {
            return $"play.transfer-weapon: {facts.Weapon} was Recovered this phase and is not transferred in it (A4.44)";
        }

        if (facts.Phase == "rph" && (facts.GiverRallyAction || facts.RecipientRallyAction))
        {
            var acted = facts.GiverRallyAction ? facts.Giver : facts.Recipient;
            return $"play.rph-action: {acted} has already taken a different Rally phase action (A3.1)";
        }

        return facts.Phase == "aph" && (!facts.Phasing || facts.AdvanceStarted)
            ? "play.transfer-phase: transfer before any unit advances (A4.431)" : null;
    }

    /// <summary>A4.43: withdrawal and rout equipment choices belong to their enclosing action, not a standalone drop.</summary>
    public static string? DropRefusal(string? phase, bool phasing, bool active, bool possessed, bool onMap,
        bool broken, bool movementEnded)
    {
        if (!active || !possessed || !onMap)
        {
            return "Select a unit's possessed weapon on the map.";
        }

        return phase switch
        {
            "ccph" => "Choose equipment to leave in the withdrawal workflow.",
            "rtph" => "Choose the equipment to carry in the rout workflow.",
            "mph" when phasing && !broken && !movementEnded => null,
            "aph" when phasing && !broken => null,
            _ => "Drop a weapon during its unbroken holder's movement or Advance phase.",
        };
    }

    /// <summary>A4.44: recovery from a friendly broken holder is available only to a SMC and has no MF cost.</summary>
    public static bool FromBrokenHolder(bool possessed, bool friendly, bool brokenHolder, bool recoveringSmc) =>
        possessed && friendly && brokenHolder && recoveringSmc;

    /// <summary>A4.44: ordinary MPh recovery costs one MF; the friendly broken-holder exception costs none.</summary>
    public static int RecoveryMf(string? phase, bool fromBrokenHolder) => phase == "mph" && !fromBrokenHolder ? 1 : 0;

    public static bool RecoveryWindowOpen(bool rallyAttempted, bool repaired, bool otherAction, bool transferred) =>
        !rallyAttempted && !repaired && !otherAction && !transferred;

    public static bool RecoveryWeaponApplicable(bool possessed, bool brokenHolder) => !possessed || brokenHolder;

    public static bool RecordsRallyTransfer(string phase) => phase == "rph";

    public static bool RecoveryEnemyBlocks(bool known, bool unarmed, bool vehicle, bool abandoned) =>
        known && !unarmed && ScenarioA1RoutCalculator.Armed(vehicle, abandoned);

    /// <summary>A4.44: phase, participant, possession and repeat-attempt checks for routine recovery.</summary>
    public static string? RecoveryRefusal(SupportWeaponRecoveryFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.KnownWirePresent)
        {
            return "Recovery across Wire cannot be evaluated: above/below-Wire positions are not recorded.";
        }
        if (!facts.ConditionsKnown)
        {
            return "The recovering unit's condition cannot be evaluated.";
        }
        if (facts.Terrain is null)
        {
            return "Recovery cannot be evaluated without this Location's terrain.";
        }
        if (facts.Aboard || facts.Terrain.Contains("wire", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("panji", StringComparison.OrdinalIgnoreCase) || facts.Terrain.Contains("crest", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("jungle", StringComparison.OrdinalIgnoreCase) || facts.Terrain.Contains("kunai", StringComparison.OrdinalIgnoreCase)
            || facts.Terrain.Contains("bamboo", StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery in this terrain or vehicle position is not supported.";
        }
        var fromBrokenHolder = FromBrokenHolder(facts.Held, facts.FriendlyHolder, facts.BrokenHolder, facts.RecoveringSmc);
        if (!ScenarioA1SupportWeaponRules.RecoverPhase(facts.Phase, facts.Phasing))
        {
            return "Recover during Rally or the recovering unit's Movement phase.";
        }

        if (facts.Phase == "rph" && !facts.RallyRecoveryWindowOpen)
        {
            return "The Rally recovery step has ended.";
        }

        if (facts.Phase == "mph" && (facts.ReactionPending || facts.AnotherMoverActive))
        {
            return facts.ReactionPending ? "Resolve defensive fire first." : "Finish the current unit's move first.";
        }

        if (!facts.Active || !facts.GoodOrder || facts.Pinned || !facts.Infantry || facts.Unarmed || facts.Bypassing)
        {
            return "Recovery needs unpinned, armed, Good Order Infantry outside Bypass.";
        }

        if (!facts.WeaponActive || !facts.SameLocation)
        {
            return "Choose a weapon in the recovering unit's Location.";
        }

        if (facts.Held && !fromBrokenHolder)
        {
            return "Only a SMC may recover a possessed weapon from a friendly broken unit.";
        }

        if (facts.ArmedKnownEnemy)
        {
            return "An armed Known enemy in this Location prevents recovery.";
        }

        if (facts.Attempted)
        {
            return "This unit already attempted to recover this weapon this phase.";
        }

        if (facts.Phase == "rph" && facts.RallyActionTaken)
        {
            return "This unit has already taken its Rally phase action.";
        }

        if (facts.Phase == "mph" && facts.MovementEnded)
        {
            return "This unit has finished moving.";
        }

        if (RecoveryMf(facts.Phase, fromBrokenHolder) > 0 && facts.HalfMfRemaining is null)
        {
            return "The unit's available MF cannot be evaluated.";
        }

        if (RecoveryMf(facts.Phase, fromBrokenHolder) > 0 && (facts.TemporarilyImmobilized || facts.HalfMfRemaining < 2))
        {
            return "No MF available for recovery.";
        }

        return null;
    }
}

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
