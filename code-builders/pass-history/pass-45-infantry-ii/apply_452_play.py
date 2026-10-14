"""Pass 45, task 45.2: the Cowering marks in Play, the cower selection roll, and the action that gives a weapon's ROF up (A8.23)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""        private string[] Marked() => attack.SprayShare == true ? [] : [.. (attack.Firers ?? [])
            .Where(item => !(item.UsesInherentFp == false && item.Weapons is [_] && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Kind == "asl:squad"))
""",
         """        private string[] Marked() => attack.SprayShare == true ? [] : [.. (attack.Firers ?? [])
            // A7.9 (pass 45): a unit that cowers is marked "regardless of whether it was using its inherent FP".
            .Where(item => cowering.Contains(item.UnitId!)
                || !(item.UsesInherentFp == false && item.Weapons is [_] && reference.Definitions.GetValueOrDefault(item.DefinitionId!)?.Kind == "asl:squad"))
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs": [
        ("""    /// <summary>A8.2, A8.21: Residual FP is placed, at the attack's value, unless a counter at least as large is already in the Location (<paramref name="counterAtLeast"/> is read for the value).</summary>
""",
         """    /// <summary>
    /// A7.9 (p. 58; pass 45): a unit that cowers is marked "as well as all of its SW regardless of whether it was using its inherent FP". The
    /// conditions of a SW it possesses that the attack did not fire: the unit's counter, a Final Fire counter replacing a First Fire one.
    /// </summary>
    public static IReadOnlyList<(UnitCondition Condition, bool Value)> CoweredWeaponConditions(string counter, bool firstFireMarked)
    {
        var conditions = new List<(UnitCondition, bool)> { (Marker(counter), true) };
        if (counter == "final-fire" && firstFireMarked)
        {
            conditions.Add((UnitCondition.FirstFire, false));
        }

        return conditions;
    }

    /// <summary>
    /// Whether a weapon's Multiple ROF may be given up for Residual FP now (A8.23, p. 60; pass 45): in the MPh, "immediately after resolving its
    /// attack" (nothing has happened since but what the attack caused), when giving it up would leave more Residual FP, and a weapon still keeps it.
    /// </summary>
    public static bool RofForfeitOpen(bool mph, bool nothingSince, int? forfeitedFp, int weaponsStillKeeping) =>
        mph && nothingSince && forfeitedFp is > 0 && weaponsStillKeeping > 0;

    /// <summary>Why the ROF may not be given up.</summary>
    public static string RofForfeitClosedText() =>
        "play.forfeit-rof: a weapon's Multiple ROF is given up for Residual FP only at once after its Defensive First Fire attack, when that leaves more Residual FP (A8.23)";

    /// <summary>The choice in words.</summary>
    public static string RofForfeitText(IReadOnlyList<string> weapons, int fp, string location)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        return $"play.forfeit-rof: {string.Join(", ", weapons)} gives up its Multiple ROF and is marked First Fire; {fp} Residual FP stands in {location} (A8.23)";
    }

    /// <summary>A8.2, A8.21: Residual FP is placed, at the attack's value, unless a counter at least as large is already in the Location (<paramref name="counterAtLeast"/> is read for the value).</summary>
"""),
        ("""    public static RecordRefusal? VerifyResidualFp(string? phase, bool recordKnown, bool sameTarget, int? recordedFp, int fp, string fireId, int? existingFp)
    {
        if (phase != "mph" || !recordKnown || !sameTarget || recordedFp != fp)
""",
         """    public static RecordRefusal? VerifyResidualFp(string? phase, bool recordKnown, bool sameTarget, int? recordedFp, int fp, string fireId, int? existingFp, int? forfeitedFp = null)
    {
        // A8.23 (pass 45): or the value the record gives for its weapons' ROF given up.
        if (phase != "mph" || !recordKnown || !sameTarget || (recordedFp != fp && forfeitedFp != fp))
"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        ("""            if (Rules.ScenarioA1FireFollowUps.VerifyResidualFp(state.Phase, recorded is not null, recorded?.TargetLocation == residual.Location.ToString(), recordedFp, residual.Fp,
                residual.Fire, state.ResidualFire.FirstOrDefault(item => item.Location == residual.Location)?.Fp) is { } refused)
""",
         """            var forfeitedFp = recorded is not null && recorded.Resolution.TryGetProperty("arithmetic", out var withRof) && withRof.TryGetProperty("residualFpIfForfeited", out var forfeit)
                ? forfeit.GetInt32() : (int?)null;
            if (Rules.ScenarioA1FireFollowUps.VerifyResidualFp(state.Phase, recorded is not null, recorded?.TargetLocation == residual.Location.ToString(), recordedFp, residual.Fp,
                residual.Fire, state.ResidualFire.FirstOrDefault(item => item.Location == residual.Location)?.Fp, forfeitedFp) is { } refused)
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs": [
        ("""            "firerSelection" => (unit.Split(',').Length, "fire-firer-selection"),
""",
         """            "firerSelection" => (unit.Split(',').Length, "fire-firer-selection"),
            "cowerSelection" => (unit.Split(',').Length, "fire-cower-selection"),
"""),
        ("""            "firerSelection" => rolls with { FirerSelection = Selection(rolls.FirerSelection) },
""",
         """            "firerSelection" => rolls with { FirerSelection = Selection(rolls.FirerSelection) },
            "cowerSelection" => rolls with { CowerSelection = Selection(rolls.CowerSelection) },
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""        Dictionary<string, int>? firerSelection = null;
""",
         """        Dictionary<string, int>? firerSelection = null;
        Dictionary<string, int>? cowerSelection = null;
"""),
        ("""                case "firerSelection":
                    if (!Select(ref firerSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
""",
         """                case "firerSelection":
                    if (!Select(ref firerSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
                case "cowerSelection":
                    if (!Select(ref cowerSelection, unit, roll))
                    {
                        return null;
                    }

                    break;
"""),
        ("""            FirerSelection = firerSelection,
""",
         """            FirerSelection = firerSelection,
            CowerSelection = cowerSelection,
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""        // A8.2, A8.21: Residual FP, unless a counter at least as large is already there.
""",
         """        // A7.9 (p. 58; pass 45): a unit that cowers is marked "as well as all of its SW regardless of whether it was using its inherent FP": the
        // SW it possesses that the attack did not fire take its counter too.
        foreach (var id in resolution.CoweredUnitIds ?? [])
        {
            if ((resolution.FirerCounters?.GetValueOrDefault(id) ?? resolution.FireCounter) is not { } cowerCounter)
            {
                continue;
            }

            foreach (var held in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == id
                && !(resolution.WeaponEffects ?? []).Any(effect => effect.EquipmentId == item.Id)))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(held.Id,
                    ConditionChanges(ScenarioA1FireFollowUps.CoweredWeaponConditions(cowerCounter, GameState.Condition(held, Conditions.FirstFire) == ConditionState.True))),
                    package, null, [fireId]));
            }
        }

        // A8.2, A8.21: Residual FP, unless a counter at least as large is already there.
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.cs": [
        ("""            "asl.game.pass-fire" => PlanPassFire(scope, existing, attemptId, expected, label),
""",
         """            "asl.game.pass-fire" => PlanPassFire(scope, existing, attemptId, expected, label),
            "asl.game.forfeit-rof" => PlanForfeitRof(scope, existing, attemptId, expected, label),
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GameActions.cs": [
        ("""    public static readonly ActionDescriptor InfantryOverrunNtc = Descriptor(""",
         """    public static readonly ActionDescriptor ForfeitRof = Descriptor("asl.game.forfeit-rof", "Give up a weapon's ROF for Residual FP",
        "The DEFENDER, at once after a Defensive First Fire attack in which a weapon kept its Multiple ROF, gives the ROF up so that the weapon's FP counts toward the Residual FP left (A8.23).",
        PlayPermission, "asl.game.sequence-v1", \"\"\"
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 }
          }
        }
        \"\"\");

    public static readonly ActionDescriptor InfantryOverrunNtc = Descriptor("""),
        ("""            InfantryOverrunNtc, OverrunEvade];
""",
         """            InfantryOverrunNtc, OverrunEvade, ForfeitRof];
"""),
    ],
    "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor": [
        ("""                            <button type="button" id="dff-show" @onclick="@(() => LocateHex(movingAt))">Show it on the map</button>
""",
         """                            <button type="button" id="dff-show" @onclick="@(() => LocateHex(movingAt))">Show it on the map</button>
                            @if (Live.Planner.RateOfFireForfeit(state, history!.Events) is { } forfeit)
                            {
                                <button id="propose-forfeit-rof" title="A8.23: a weapon that kept its Multiple ROF leaves no Residual FP of its own. Give the ROF up now, before anything else, and its FP counts toward the Residual FP; the weapon is marked First Fire."
                                    @onclick='@(() => ProposeSimple(GameActions.ForfeitRof, "forfeit-rof"))'>
                                    Propose: give up the ROF of @string.Join(", ", forfeit.Weapons.Select(id => Words(id))) to leave @forfeit.Fp Residual FP in @PlaceOf(forfeit.At)
                                </button>
                            }
"""),
    ],
    "tests/LimboDancer.Domains.Asl.Play.Tests/PlayTests.cs": [
        (""""asl.game.mop-up", "asl.game.infantry-overrun-ntc", "asl.game.overrun-evade"],""",
         """"asl.game.mop-up", "asl.game.infantry-overrun-ntc", "asl.game.overrun-evade", "asl.game.forfeit-rof"],"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
