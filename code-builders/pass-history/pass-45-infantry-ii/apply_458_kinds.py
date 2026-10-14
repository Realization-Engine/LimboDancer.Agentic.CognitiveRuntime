"""Pass 45, task 45.8: a fire kind for each firer in the fire calculator. Every anchor is checked before anything is written."""
import sys

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/"
EDITS = {
    "ScenarioA1FireCalculator.cs": [
        # IsSustained by the firer's own kind.
        ("""    private static bool IsSustained(FireAttack attack, FireWeapon weapon, ScenarioA1FireReference reference) =>
        (attack.FireKind is SubsequentFirstFire or FinalProtectiveFire || (attack.Phase == "DFPh" && weapon.FirstFireMarked == true))

        // A9.3 (referee, pass 9b): only a MG uses Sustained Fire.
        && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsMg != false;
""",
         """    private static bool IsSustained(FireAttack attack, FireWeapon weapon, ScenarioA1FireReference reference) =>
        ((attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
            && ((attack.Firers ?? []).FirstOrDefault(firer => firer.Weapons?.Contains(weapon) == true) is not { } user || UsesSubsequentFire(attack, user)))
            || (attack.Phase == "DFPh" && weapon.FirstFireMarked == true))

        // A9.3 (referee, pass 9b): only a MG uses Sustained Fire.
        && reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.IsMg != false;

    /// <summary>
    /// A firer's own kind in an attack of Subsequent First Fire or FPF, by its own counter (A8.3, A8.31, pp. 61 to 62; pass 45, ruling R45.1): "A unit
    /// using FPF may form a FG with units not using FPF, but only those units using FPF are affected by its adverse affects". In any other attack
    /// it is the attack's kind.
    /// </summary>
    private static string? OwnKind(FireAttack attack, FireFirer firer) => attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
        ? firer.FinalFireMarked == true ? FinalProtectiveFire : firer.FirstFireMarked == true ? SubsequentFirstFire : FirstFire
        : attack.FireKind;

    /// <summary>
    /// Whether a firer of a Subsequent First Fire or FPF attack uses that fire itself (A8.3): it is marked, or a MG it fires is marked First Fire.
    /// "Whenever a unit uses Subsequent First Fire, it must use all MG/IFE in its possession ... as Subsequent First Fire", and "that unit and all
    /// its SW/Guns are marked with a Final Fire counter"; a unit with no counter beside it fires and is marked as in First Fire.
    /// </summary>
    private static bool UsesSubsequentFire(FireAttack attack, FireFirer firer) => attack.FireKind is SubsequentFirstFire or FinalProtectiveFire
        && (firer.FirstFireMarked == true || firer.FinalFireMarked == true || (firer.Weapons ?? []).Any(weapon => weapon.FirstFireMarked == true));
"""),
        ("""            SubsequentFirstFire => firstFire && !finalFire,
            // A8.31 (ruling R12.3; referee, pass 12): FPF firers with Subsequent First Fire firers in one group, all then marked Final Fire.
            FinalProtectiveFire => finalFire || firstFire,
""",
         """            // A8.31, A8.312 (pass 45, ruling R45.1): a unit with no counter fires First Fire in one group with Subsequent First Fire and FPF firers.
            SubsequentFirstFire => !finalFire && (firstFire || firer.FiredThisPlayerTurn != true),
            FinalProtectiveFire => finalFire || firstFire || firer.FiredThisPlayerTurn != true,
"""),
        ("""        var subsequentMembers = kind == SubsequentFirstFire ? firers.Zip(firerDefinitions).ToArray()
            : kind == FinalProtectiveFire ? firers.Zip(firerDefinitions).Where(pair => pair.First.FinalFireMarked != true).ToArray() : [];
""",
         """        var subsequentMembers = kind is SubsequentFirstFire or FinalProtectiveFire
            ? firers.Zip(firerDefinitions).Where(pair => pair.First.FinalFireMarked != true && UsesSubsequentFire(attack, pair.First)).ToArray() : [];

        // A8.3 (pass 45): an attack of Subsequent First Fire has a firer that uses it.
        if (kind == SubsequentFirstFire && subsequentMembers.Length == 0)
        {
            outside.Add("asl.a1.fire.subsequent-first-fire-outside");
        }

"""),
        ("""            var ownKind = attack.FireKind == FinalProtectiveFire
                ? firer.FinalFireMarked == true ? FinalProtectiveFire : firer.FirstFireMarked == true ? SubsequentFirstFire : FirstFire
                : attack.FireKind;
""",
         """            var ownKind = OwnKind(attack, firer);
"""),
        ("""                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? "final-fire" : counter, selection.TryGetValue(id, out var dr) ? dr : null);
""",
         """                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? "final-fire" : operatorUnit is null ? counter : OwnCounter(operatorUnit), selection.TryGetValue(id, out var dr) ? dr : null);
"""),
        ("""        private string? FireCounter() => attack.FireKind switch
""",
         """        /// <summary>
        /// A firer's own counter (A8.3, A8.31; pass 45, ruling R45.1): in an attack of Subsequent First Fire or FPF a unit that uses neither is marked
        /// First Fire; every other firer takes the attack's counter.
        /// </summary>
        private string? OwnCounter(FireFirer firer) =>
            attack.FireKind is SubsequentFirstFire or FinalProtectiveFire && !UsesSubsequentFire(attack, firer) ? "first-fire" : FireCounter();

        /// <summary>The firers whose counter differs from the attack's, by id; null when none does.</summary>
        private Dictionary<string, string>? FirerCounters()
        {
            var own = (attack.Firers ?? []).Where(firer => OwnCounter(firer) is { } counter && counter != FireCounter())
                .ToDictionary(firer => firer.UnitId!, firer => OwnCounter(firer)!, StringComparer.Ordinal);
            return own.Count == 0 || attack.SprayShare == true ? null : own;
        }

        private string? FireCounter() => attack.FireKind switch
"""),
        ("""                attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), Marked(), FireCounter(), firerConcealment)
            {
                WeaponEffects = weapons,
""",
         """                attack.Targets!.Select(item => state[item.UnitId!].Effect()).ToArray(), Marked(), FireCounter(), firerConcealment)
            {
                FirerCounters = FirerCounters(),
                WeaponEffects = weapons,
"""),
    ],
    "ScenarioA1FireModels.cs": [
        ("""    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
""",
         """    /// <summary>
    /// The firers whose own counter differs from <see cref="FireCounter"/> (A8.31; pass 45, ruling R45.1): a unit with no counter that fires First
    /// Fire in one group with Subsequent First Fire or FPF firers. Null when every firer takes the attack's counter.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? FirerCounters
    {
        get; init;
    }

    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
"""),
    ],
    "ScenarioA1FireFollowUps.cs": [
        ("""        var conditions = new List<(UnitCondition, bool)> { (Marker(resolution.FireCounter!), true) };
        if (resolution.FireCounter == "final-fire" && firstFireMarked)
""",
         """        // A8.31 (pass 45, ruling R45.1): a firer with a counter of its own takes that one.
        var counter = resolution.FirerCounters?.GetValueOrDefault(id) ?? resolution.FireCounter!;
        var conditions = new List<(UnitCondition, bool)> { (Marker(counter), true) };
        if (counter == "final-fire" && firstFireMarked)
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ROOT + name, encoding="utf-8", newline="") as f:
        text = f.read()
    crlf = "\r\n" in text
    if crlf:
        text = text.replace("\r\n", "\n")
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text.replace("\n", "\r\n") if crlf else text
for name, text in texts.items():
    with open(ROOT + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
