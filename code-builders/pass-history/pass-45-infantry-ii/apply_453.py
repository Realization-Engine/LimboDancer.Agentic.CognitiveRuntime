"""Pass 45, task 45.3: a vehicle is attacked by the Residual FP of a Location once, and again only when the FP grew or its DRM fell (A8.22)."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs": [
        ("""    /// <summary>
    /// Whether a weapon's Multiple ROF may be given up for Residual FP now (A8.23, p. 60; pass 45)""",
         """    /// <summary>
    /// Whether the Residual FP of a Location attacks a unit that spends MF or MP there (A8.22, p. 60; pass 45): "A unit can be attacked by Residual FP
    /// only once per Location [EXC: if, since that first Residual FP attack, the Residual FP has increased in strength or the unit is subject to
    /// more-negative-DRM/less-positive-DRM, it will be attacked again by that Residual FP upon further MF/MP expenditure]". <paramref name="earlier"/>
    /// is each Residual FP attack it has taken in that Location this MPh: the counter's FP and the attack's DRM.
    /// </summary>
    public static bool ResidualAttacksAgain(int fp, int drm, IEnumerable<(int Fp, int Drm)> earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        return earlier.All(attack => fp > attack.Fp || drm < attack.Drm);
    }

    /// <summary>
    /// Whether a weapon's Multiple ROF may be given up for Residual FP now (A8.23, p. 60; pass 45)"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Vehicles.cs": [
        ("""                residualFacts = HeatOfBattleFacts(entered, mapFacts);
                var precheck = ScenarioA1FireCalculator.Precheck(residualFacts, FireReference.Value);
                if (precheck.Count != 0)
                {
                    return Refused(scope, label, expected, ["play.move-vehicle-residual: the Fire package does not decide the Residual FP attack this MP expenditure would suffer", .. precheck]);
                }
""",
         """                residualFacts = HeatOfBattleFacts(entered, mapFacts);
                var precheck = ScenarioA1FireCalculator.Precheck(residualFacts, FireReference.Value);
                if (precheck.Count != 0)
                {
                    return Refused(scope, label, expected, ["play.move-vehicle-residual: the Fire package does not decide the Residual FP attack this MP expenditure would suffer", .. precheck]);
                }

                // A8.22 (pass 45): once a Location, and again only when the Residual FP grew or the DRM fell since.
                if (!ScenarioA1FireFollowUps.ResidualAttacksAgain(residual.Fp, ResidualDrm(residualFacts), EarlierResidualAttacks(existing, at, residualFacts)))
                {
                    residualFacts = null;
                }
"""),
        ("""    /// <summary>
    /// The plan of a vehicle's MP expenditure (rulings R6.6, R11.9 to R11.12)""",
         """    /// <summary>The DRM a Residual FP attack would take, read before its roll.</summary>
    private static int ResidualDrm(FireAttack attack) =>
        (int)(ScenarioA1FireCalculator.Preview(attack, FireReference.Value)?.Drm.Sum(item => item.Value) ?? 0m);

    /// <summary>
    /// The Residual FP attacks this MPh, in a Location, on every unit and vehicle the attack now planned would hit (A8.22): each one's counter FP and
    /// DRM, from its record.
    /// </summary>
    private static List<(int Fp, int Drm)> EarlierResidualAttacks(IReadOnlyList<GameEvent> existing, BoardLocation at, FireAttack planned)
    {
        string[] hit = [.. (planned.Targets ?? []).Select(item => item.UnitId!), .. (planned.Vehicles ?? []).Select(item => item.VehicleId!)];
        var earlier = new List<(int, int)>();
        foreach (var record in ThisPhase(existing).Select(item => item.Payload).OfType<FireResolved>().Where(record => record.TargetLocation == at.ToString()))
        {
            if (record.Facts.Deserialize<FireAttack>(LiveFire.Json) is not { FireKind: ScenarioA1FireCalculator.ResidualFire, ResidualFp: { } fp } recorded)
            {
                continue;
            }

            string[] then = [.. (recorded.Targets ?? []).Select(item => item.UnitId!), .. (recorded.Vehicles ?? []).Select(item => item.VehicleId!)];
            if (hit.Length > 0 && hit.All(id => then.Contains(id, StringComparer.Ordinal)))
            {
                var drm = record.Resolution.TryGetProperty("arithmetic", out var arithmetic) && arithmetic.TryGetProperty("drm", out var list) && list.ValueKind == JsonValueKind.Array
                    ? (int)list.EnumerateArray().Sum(item => item.TryGetProperty("value", out var value) ? value.GetDecimal() : 0m) : 0;
                earlier.Add((fp, drm));
            }
        }

        return earlier;
    }

    /// <summary>
    /// The plan of a vehicle's MP expenditure (rulings R6.6, R11.9 to R11.12)"""),
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
