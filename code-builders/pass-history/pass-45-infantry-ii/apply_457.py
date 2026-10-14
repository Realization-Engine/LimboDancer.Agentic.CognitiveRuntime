"""Pass 45, task 45.7: the X# of a lowered B# (A.11, A9.3, A9.7); no Sustained Fire by a lone SMC's MG (A9.3); no repair of a captured SW (A9.72);
dismantling for every SW A9.8 names; a German dm MMG or HMG fired as a LMG (A9.8, A4.41). Every anchor is checked before anything is written."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs": [
        ("""public sealed record FireWeaponEffect(string EquipmentId, int BreakdownNumber, bool Malfunctioned, bool RateOfFireRetained, bool SustainedFire,
    string? FireCounter, int? SelectionDr);
""",
         """public sealed record FireWeaponEffect(string EquipmentId, int BreakdownNumber, bool Malfunctioned, bool RateOfFireRetained, bool SustainedFire,
    string? FireCounter, int? SelectionDr)
{
    /// <summary>
    /// Whether the weapon is removed for good (A.11, p. 43; A9.7, p. 65; pass 45): a fire that lowers the B# "also transforms the weapon's Original
    /// B# to an X# during that use", and a DR at least the X# removes it. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Removed
    {
        get; init;
    }
}
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs": [
        ("""                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? "final-fire" : operatorUnit is null ? counter : OwnCounter(operatorUnit), selection.TryGetValue(id, out var dr) ? dr : null);
""",
         """                // A.11 (p. 43; pass 45): "it also transforms the weapon's Original B# to an X# during that use": a weapon whose B# this fire lowered is
                // removed on a DR at least its printed B# (the page's LMG in Sustained Fire: B10, X12). A FT's own removal is ruling R15.1's.
                var definition = reference.Definitions[weapon.DefinitionId!];
                var printed = definition.Breakdown ?? 12;
                var removed = attack.CoweringMarksAndWeaponResidual == true && !definition.IsFt && malfunctioned.Contains(id) && breakdown[id] < printed && original >= printed;
                return new FireWeaponEffect(id, breakdown[id], malfunctioned.Contains(id), retained, sustained,
                    retained ? null : sustained ? "final-fire" : operatorUnit is null ? counter : OwnCounter(operatorUnit), selection.TryGetValue(id, out var dr) ? dr : null)
                {
                    Removed = removed ? true : null,
                };
"""),
        ("""        // A8.4: a First-Fire-marked unit fires again in the DFPh only at an adjacent or same-hex target.
""",
         """        // A9.3 (p. 63; pass 45): "Sustained Fire cannot be used ... by a MG fired by a lone SMC." A leader with no partner fires no MG in Sustained
        // Fire; an attack recorded before the pass stands.
        if (attack.CoweringMarksAndWeaponResidual == true && firers.Zip(firerDefinitions).Any(pair => pair.Second is { IsLeader: true } && pair.First.Partner is null
            && (pair.First.Weapons ?? []).Any(weapon => IsSustained(attack, weapon, reference))))
        {
            outside.Add("asl.a1.fire.weapon-outside");
        }

        // A8.4: a First-Fire-marked unit fires again in the DFPh only at an adjacent or same-hex target.
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs": [
        ("""    /// <summary>A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.</summary>
""",
         """    /// <summary>
    /// The weapons a unit may dismantle and assemble (A9.8, p. 65; pass 45): "Any light mortar of >= four PP, 76-82mm mortar, or non-Russian
    /// HMG/MMG". The catalog holds no 76-82mm mortar as a SW.
    /// </summary>
    public static bool Dismantlable(string? kind, string? nationality, string? size, int? portage) =>
        (kind == "asl:mg" && size is "medium" or "heavy" && nationality is not (null or "russian")) || (kind == "asl:light-mortar" && portage >= 4);

    /// <summary>
    /// Whether a dismantled MG fires as its side's LMG (A9.8, p. 65; pass 45): "A dm SW may not be fired until reassembled [EXC: a German dm
    /// HMG/MMG may be fired as a German LMG]".
    /// </summary>
    public static bool DismantledFiresAsLmg(bool dismantled, string? nationality, string? size) =>
        dismantled && nationality == "german" && size is "medium" or "heavy";

    /// <summary>A9.12 (referee, pass 12): a leader who directed fire this phase gives up leadership by firing a MG, so fires none; he fires one MG a phase.</summary>
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1RallyRules.cs": [
        ("""    /// <summary>A9.72: the SW has a Repair Number in the catalog.</summary>
""",
         """    /// <summary>A9.72 (p. 65; pass 45): "A captured SW/Gun may never be repaired unless recaptured by its original side."</summary>
    public static string? SwRepairCapturedBar(string equipmentId, bool captured) =>
        captured ? $"play.repair-weapon: '{equipmentId}' is a captured SW, which only its original side repairs (A9.72)" : null;

    /// <summary>A9.72: the SW has a Repair Number in the catalog.</summary>
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Rally.cs": [
        ("""        var printed = equipment.Definition is null ? null : FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair;
""",
         """        if (ScenarioA1RallyRules.SwRepairCapturedBar(equipmentId, LiveFire.CapturedBy(equipment.Definition?.Definition, unit) == true) is { } capturedBar)
        {
            return Refused(scope, label, expected, capturedBar);
        }

        var printed = equipment.Definition is null ? null : FireReference.Value.Definitions.GetValueOrDefault(equipment.Definition.Definition)?.Repair;
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs": [
        ("""    /// <summary>The German MMG, the one catalog weapon A9.8 lets its possessor dismantle and assemble (ruling R13.6).</summary>
    private static bool Dismantlable(EquipmentInstance weapon) =>
        weapon.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) is { Nationality: "german" } definition
            && definition.Id.Contains("mmg", StringComparison.Ordinal);
""",
         """    /// <summary>The weapons A9.8 lets their possessor dismantle and assemble; Rules decides from the catalog's values (pass 45).</summary>
    private bool Dismantlable(EquipmentInstance weapon)
    {
        var definition = weapon.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        var printed = weapon.Definition is { } named ? catalogs.FirstOrDefault(item => item.Identity == named.Catalog)?.Definition(named.Definition) : null;
        return ScenarioA1FireEligibility.Dismantlable(weapon.Kind, definition?.Nationality ?? printed?.Nationality, definition?.Size,
            (int?)printed?.Printed("front", "asl:portage")?.Value?.Number);
    }
"""),
        ("""            return Refused(scope, label, expected, $"play.dismantle-weapon: {weapon.Id} is not a weapon the game dismantles; only the German MMG is (A9.8; ruling R13.6)");
""",
         """            return Refused(scope, label, expected, $"play.dismantle-weapon: {weapon.Id} is not dismantled: only a light mortar of four PP or more and a non-Russian MMG or HMG are (A9.8)");
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, equipment.Definition?.Definition, Is(equipment, Conditions.Malfunctioned), Fired(equipment),
                Is(equipment, Conditions.FirstFire))
            : new FireWeapon(id, null, null, null, null);
""",
         """        // A9.8 (pass 45): a German dm MMG or HMG fires as a German LMG, so its facts name that definition.
        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, FiredAs(equipment), Is(equipment, Conditions.Malfunctioned), Fired(equipment),
                Is(equipment, Conditions.FirstFire))
            : new FireWeapon(id, null, null, null, null);
"""),
        ("""                        state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled),
""",
         """                        state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled) && !FiresAsLmg(held),
"""),
        ("""    public const string Catalog = "asl-scenario-a1";
""",
         """    /// <summary>Whether a dismantled MG fires as its side's LMG (A9.8; pass 45).</summary>
    public static bool FiresAsLmg(EquipmentInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        var definition = weapon.Definition is { } reference ? CatalogReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        return ScenarioA1FireEligibility.DismantledFiresAsLmg(Is(weapon, Conditions.Dismantled), definition?.Nationality, definition?.Size);
    }

    /// <summary>The definition a weapon fires as: its own, or its side's LMG for a dismantled German MMG or HMG (A9.8; pass 45).</summary>
    public static string? FiredAs(EquipmentInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);
        var own = weapon.Definition?.Definition;
        if (!FiresAsLmg(weapon) || own is null)
        {
            return own;
        }

        var nationality = CatalogReference.Value.Definitions[own].Nationality;
        return CatalogReference.Value.Definitions.Values.Where(item => item.IsMg && item.Size == "light" && item.Nationality == nationality)
            .OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()?.Id ?? own;
    }

    public const string Catalog = "asl-scenario-a1";
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("""            .Select(equipment => new PossessedWeaponFacts(equipment.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True,
                GameState.Condition(equipment, Conditions.Dismantled) == ConditionState.True, equipment.Kind,
""",
         """            .Select(equipment => new PossessedWeaponFacts(equipment.Id, GameState.Condition(equipment, Conditions.Malfunctioned) == ConditionState.True,
                GameState.Condition(equipment, Conditions.Dismantled) == ConditionState.True && !LiveFire.FiresAsLmg(equipment), equipment.Kind,
"""),
        ("""        // The MGs: a malfunction (A9.7), and the fire counter of a MG that lost its Multiple ROF (A9.2).
        foreach (var weapon in (resolution.WeaponEffects ?? []).Where(item => item.EquipmentId != resolution.FlamethrowerRemoved))
        {
""",
         """        // A.11, A9.7 (pass 45): a weapon whose lowered B# became an X# is removed on that DR.
        foreach (var gone in (resolution.WeaponEffects ?? []).Where(item => item.Removed == true))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(gone.EquipmentId), package, null, [fireId]));
        }

        // The MGs: a malfunction (A9.7), and the fire counter of a MG that lost its Multiple ROF (A9.2).
        foreach (var weapon in (resolution.WeaponEffects ?? []).Where(item => item.EquipmentId != resolution.FlamethrowerRemoved && item.Removed != true))
        {
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.cs": [
        ("""            "asl.game.fire-ordnance" => HeldBehindMandatoryFire(scope, arguments, existing, expected, label, "Gun's shot")
""",
         """            "asl.game.fire-ordnance" => DismantledOrdnance(scope, arguments, existing, expected, label)
                ?? HeldBehindMandatoryFire(scope, arguments, existing, expected, label, "Gun's shot")
"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.MandatoryFire.cs": [
        ("""    /// <summary>
    /// The fire action, with the Mandatory FG's bar before it (A8.312). The bar is on the DEFENDER's proposal alone: a proposal by the phasing side
""",
         """    /// <summary>A9.8 (p. 65; pass 45): "A dm SW may not be fired until reassembled": the refusal of a dismantled mortar's shot, or null.</summary>
    private GamePlan? DismantledOrdnance(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, long expected, string label) =>
        existing.Count > 0 && Text(arguments, "gunId", out var gunId) && Replay(existing).Current?.Find(gunId) is EquipmentInstance weapon && Is(weapon, Conditions.Dismantled)
            ? Refused(scope, label, expected, $"play.ordnance-dismantled: {gunId} is dismantled and is not fired until it is assembled (A9.8)")
            : null;

    /// <summary>
    /// The fire action, with the Mandatory FG's bar before it (A8.312). The bar is on the DEFENDER's proposal alone: a proposal by the phasing side
"""),
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
