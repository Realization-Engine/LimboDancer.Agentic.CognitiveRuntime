"""Pass 45, task 45.8: every act of the DEFENDER waits behind the attack owed under A8.312 (ruling R45.2, replacing R40.19)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Play/GamePlanner.cs": [
        ("""            "asl.game.fire-ordnance" => PlanFireOrdnance(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
""",
         """            "asl.game.fire-ordnance" => HeldBehindMandatoryFire(scope, arguments, existing, expected, label, "Gun's shot")
                ?? PlanFireOrdnance(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
"""),
        ("""            "asl.game.throw-dc" => PlanThrowDc(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.fire-starshell" => PlanStarshell(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
""",
         """            "asl.game.throw-dc" => HeldBehindMandatoryFire(scope, arguments, existing, expected, label, "thrown DC")
                ?? PlanThrowDc(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
            "asl.game.fire-starshell" => HeldBehindMandatoryFire(scope, arguments, existing, expected, label, "Starshell")
                ?? PlanStarshell(scope, arguments, existing, attemptId, expected, label, actor ?? "unknown"),
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.MandatoryFire.cs": [
        ("""    /// <summary>
    /// The fire action, with the Mandatory FG's bar before it (A8.312). The bar is on the DEFENDER's proposal alone: a proposal by the phasing side
""",
         """    /// <summary>
    /// The refusal of another act of the DEFENDER while the Mandatory FG's attack is owed (A8.312; pass 45, ruling R45.2), or null. Like the fire
    /// action's bar it is on the DEFENDER's proposal alone: one that names a unit or a weapon of the side that is not moving.
    /// </summary>
    private GamePlan? HeldBehindMandatoryFire(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, long expected, string label, string act)
    {
        if (existing.Count == 0 || arguments.ValueKind != JsonValueKind.Object || Replay(existing).Current is not { Phase: "mph" } state)
        {
            return null;
        }

        bool Defenders(string id) => state.Unit(id) is { } unit ? unit.Side != state.PhasingSide
            : state.Find(id) is EquipmentInstance { Holding.Holder: { } holder } && state.Unit(holder) is { } possessor && possessor.Side != state.PhasingSide;
        var named = arguments.EnumerateObject().SelectMany(property => property.Value.ValueKind switch
        {
            JsonValueKind.String => [property.Value.GetString()!],
            JsonValueKind.Array => property.Value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!),
            _ => Enumerable.Empty<string>(),
        });
        return named.Any(Defenders) && MandatoryFire(state, existing) is { } owed
            ? Refused(scope, label, expected, ScenarioA1MandatoryFire.OtherActBar(owed.Firers, owed.At.ToString(), act))
            : null;
    }

    /// <summary>
    /// The fire action, with the Mandatory FG's bar before it (A8.312). The bar is on the DEFENDER's proposal alone: a proposal by the phasing side
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1MandatoryFire.cs": [
        ("""    /// <summary>The page's note for the DEFENDER; where the fire rules form no one group of the units, it says so and cites the ruling.</summary>
""",
         """    /// <summary>
    /// Why another act of the DEFENDER waits while the attack is owed (A8.312, p. 61; pass 45, ruling R45.2): the DEFENDER "must ... then immediately
    /// attack", so a Gun's shot, a thrown DC, and a Starshell come after it.
    /// </summary>
    public static string OtherActBar(IReadOnlyList<string> owed, string location, string act)
    {
        ArgumentNullException.ThrowIfNull(owed);
        return $"play.act-mandatory: {string.Join(", ", owed)} must first attack the units that entered {location}; the {act} waits for that attack (A8.312)";
    }

    /// <summary>The page's note for the DEFENDER; where the fire rules form no one group of the units, it says so and cites the ruling.</summary>
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
