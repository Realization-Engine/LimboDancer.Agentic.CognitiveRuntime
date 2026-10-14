"""Pass 40 task 40.7, the portage and movement corrections (A4.11, A4.4, A4.41, A4.42, A4.44, A4.5, A4.61, A4.72).

Exact blocks with asserted counts, all checked before any file is written.
"""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PENDING = {}


def apply(path, edits):
    text = PENDING.get(path) or open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:90], text.count(old))
        text = text.replace(old, new)
    PENDING[path] = text


R = "LimboDancer.Domains.Asl.Rules/"
P = "LimboDancer.Domains.Asl.Play/"
U = "LimboDancer.Domains.Asl.Units/State/"
M = "LimboDancer.Domains.Asl.MapStudio/"

# ---- Rules ----------------------------------------------------------------------------------------------------------------------
apply(R + "ScenarioA1MovementCalculator.cs", [
    # A4.42: the leader's IPC as the mover's choice.
    ("    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n"
     "        ArgumentNullException.ThrowIfNull(laden);\n"
     "        var heavy = movers.Select((unit, index) => (Unit: unit, Index: index)).Where(item => LeaderBonus(item.Unit, movers) && laden(item.Index)).Select(item => item.Unit).ToArray();\n"
     "        var leader = heavy.Length != 1 ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded\n"
     "            && item.Nationality == heavy[0].Nationality);\n"
     "        return leader is not null ? (heavy[0].Id, leader.Id) : (null, null);\n"
     "    }\n",
     "    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden, string? chosen = null)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n"
     "        ArgumentNullException.ThrowIfNull(laden);\n"
     "        var heavy = movers.Select((unit, index) => (Unit: unit, Index: index)).Where(item => LeaderBonus(item.Unit, movers) && laden(item.Index)).Select(item => item.Unit).ToArray();\n\n"
     "        // A4.42 (p. 50; pass 40, ruling R40.11): \"one SMC can add its IPC to that of any one Good Order Infantry unit\": the mover names which of\n"
     "        // several laden MMC takes it; with one laden MMC it goes there unasked.\n"
     "        var recipient = chosen is not null ? heavy.FirstOrDefault(unit => unit.Id == chosen) : heavy.Length == 1 ? heavy[0] : null;\n"
     "        var leader = recipient is null ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded\n"
     "            && item.Nationality == recipient.Nationality);\n"
     "        return leader is not null ? (recipient!.Id, leader.Id) : (null, null);\n"
     "    }\n\n"
     "    /// <summary>\n"
     "    /// A4.42 (ruling R40.11): the leader's IPC goes to one unit for the whole move (\"start the phase together and move together as a stack\"). Null\n"
     "    /// when the choice stands: none named, or the unit named is the one the move's earlier steps gave it to and it is a laden MMC with the leader.\n"
     "    /// </summary>\n"
     "    public static string? IpcChoiceBar(string? chosen, string? pending, string? recipient)\n"
     "    {\n"
     "        if (chosen is null)\n"
     "        {\n"
     "            return null;\n"
     "        }\n\n"
     "        return pending is not null && pending != chosen\n"
     "            ? $\"play.move-ipc: the leader's IPC went to {pending} with this move's earlier step and stays with it (A4.42)\"\n"
     "            : recipient != chosen ? $\"play.move-ipc: {chosen} is not a MMC carrying more than its IPC that moves with a leader of its nationality (A4.42)\" : null;\n"
     "    }\n\n"
     "    /// <summary>\n"
     "    /// A4.4 (p. 50; pass 40): \"No item can be portaged more than once per phase\". Null unless a mover holds an item another unit already portaged\n"
     "    /// in this phase.\n"
     "    /// </summary>\n"
     "    public static string? PortagedTwiceBar(IEnumerable<(string Weapon, string Holder, string? PortagedBy)> held)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(held);\n"
     "        return held.Where(item => item.PortagedBy is not null && item.PortagedBy != item.Holder).Select(item =>\n"
     "            $\"play.move-portage: {item.Weapon} was portaged by {item.PortagedBy} in this phase, and no item is portaged more than once a phase (A4.4)\").FirstOrDefault();\n"
     "    }\n\n"
     "    /// <summary>\n"
     "    /// A4.4 (p. 50; pass 40): the items portaged in this phase, after a step: \"even if a unit carries a SW during only one MF expenditure before\n"
     "    /// dropping it, that unit may not recoup the portage cost\". Each item is kept with the unit that first spent MF holding it.\n"
     "    /// </summary>\n"
     "    public static IReadOnlyList<(string Unit, string Weapon)> Portaged(IReadOnlyList<(string Unit, string Weapon)> already, IEnumerable<(string Unit, string Weapon)> heldByMovers)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(already);\n"
     "        ArgumentNullException.ThrowIfNull(heldByMovers);\n"
     "        var all = already.ToList();\n"
     "        foreach (var item in heldByMovers.Where(item => !all.Any(known => known.Weapon == item.Weapon)))\n"
     "        {\n"
     "            all.Add(item);\n"
     "        }\n\n"
     "        return all;\n"
     "    }\n"),
    # A4.61: Assault Movement with MF spent inside a Location beside the one change of Location.
    ("    public static string? MoveTiming(bool currentExists, bool currentAssault, bool assault, bool doubleTime, int? nvr, IReadOnlyList<MoveTimingUnitFacts> movers)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n\n"
     "        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location.\n"
     "        if (currentExists && (currentAssault || assault))\n",
     "    public static string? MoveTiming(bool currentExists, bool currentAssault, bool assault, bool doubleTime, int? nvr, IReadOnlyList<MoveTimingUnitFacts> movers,\n"
     "        bool currentChangedLocation = true, bool stepChangesLocation = true)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n\n"
     "        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location. Its EXC (p. 51; pass 40): MF spent\n"
     "        // within a Location, before or after the one change of Location, do not prevent it; the step's own check keeps it short of all its MF.\n"
     "        if (currentExists && (currentAssault || assault) && (!currentAssault || !assault || (currentChangedLocation && stepChangesLocation)))\n"),
])
apply(R + "ScenarioA1FireReference.cs", [
    ("    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>\n",
     "    /// <summary>A MG's size as its counter has it (A9.1): light, medium, or heavy; null for other definitions.</summary>\n"
     "    public string? Size\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>\n"),
    ("            AssaultFire = Trait(\"front\", \"asl:assault-fire\"),\n", "            AssaultFire = Trait(\"front\", \"asl:assault-fire\"),\n            Size = Text(\"front\", \"size\"),\n"),
])
apply(R + "ScenarioA1FireEligibility.cs", [
    ("    public static string? FirerLimitBar(FirerLimitFacts facts)\n",
     "    /// <summary>\n"
     "    /// A4.41 (p. 50; pass 40): \"No MMG, HMG, mortar ... which moved [other than being Recovered] during the MPh may fire during the ensuing AFPh\".\n"
     "    /// Null unless a weapon named is a MMG or HMG carried into a new Location this Player Turn. The light mortar's bar is the ordnance's.\n"
     "    /// </summary>\n"
     "    public static string? MovedWeaponBar(string? phase, IEnumerable<(string Id, bool Moved, string? Size)> weapons)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(weapons);\n"
     "        return phase != \"afph\" ? null\n"
     "            : weapons.Where(weapon => weapon.Moved && weapon.Size is \"medium\" or \"heavy\").Select(weapon =>\n"
     "                $\"play.fire-weapon-moved: {weapon.Id} is a MMG or HMG that moved in the MPh, so it does not fire in the AFPh (A4.41)\").FirstOrDefault();\n"
     "    }\n\n"
     "    /// <summary>\n"
     "    /// A4.61 (p. 51; pass 40): a unit using Assault Movement that breaks or becomes berserk \"is no longer considered using Assault Movement and is\n"
     "    /// subject to the -1 FFNAM DRM for the remainder of its MPh\". The stack's fire reads one status, so one such mover ends it for the attack.\n"
     "    /// </summary>\n"
     "    public static bool AssaultHolds(bool declared, IEnumerable<(bool Broken, bool Berserk)> movers)\n"
     "    {\n"
     "        ArgumentNullException.ThrowIfNull(movers);\n"
     "        return declared && movers.All(unit => !unit.Broken && !unit.Berserk);\n"
     "    }\n\n"
     "    public static string? FirerLimitBar(FirerLimitFacts facts)\n"),
])
apply(R + "ScenarioA1SupportWeaponRules.cs", [
    ("    /// <summary>A4.44: a Recovery in the MPh costs one MF, refused when it would pass the unit's allotment.</summary>\n",
     "    /// <summary>A4.44 (p. 50; pass 40): \"a Recovery attempt is allowed only by an unpinned, Good Order non-Bypassing unit\".</summary>\n"
     "    public static string? RecoveryBypassBar(string unitId, bool bypassing) =>\n"
     "        bypassing ? $\"play.recover-bypass: {unitId} is in Bypass, and a Bypassing unit Recovers nothing (A4.44)\" : null;\n\n"
     "    /// <summary>A4.44: a Recovery in the MPh costs one MF, refused when it would pass the unit's allotment.</summary>\n"),
])
apply(R + "ScenarioA1PassengerCalculator.cs", [
    ("    /// <summary>D6.4: the half MF a unit has left after the one MF boarding costs; negative when it cannot board.</summary>\n",
     "    /// <summary>\n"
     "    /// A4.11 (p. 48; pass 40): a unit that mounts a conveyance has four MF that Player Turn: a SMC four in place of six, and a wounded SMC or\n"
     "    /// Inexperienced Infantry four in place of three.\n"
     "    /// </summary>\n"
     "    public const int ConveyanceAllowance = 4;\n\n"
     "    /// <summary>A4.5 (p. 51; pass 40): Double Time \"may not be used by a unit that will mount, ride, or dismount any form of conveyance during that Player Turn\".</summary>\n"
     "    public static string? BoardingDoubleTimeBar(string unitId, int doubleTimeMf) =>\n"
     "        doubleTimeMf > 0 ? $\"play.load: {unitId} has Double Timed this MPh, and a unit that Double Times does not board (A4.5)\" : null;\n\n"
     "    /// <summary>D6.4: the half MF a unit has left after the one MF boarding costs; negative when it cannot board.</summary>\n"),
])

# ---- Units ----------------------------------------------------------------------------------------------------------------------
apply(U + "LiveRecords.cs", [
    ("    public BoardLocation? Dash\n    {\n        get; init;\n    }\n\n    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;",
     "    public BoardLocation? Dash\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>The laden MMC the stack's leader lends his IPC to for this move, when the mover named it (A4.42; pass 40, ruling R40.11); null otherwise.</summary>\n"
     "    public string? IpcTo\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>\n    /// Whether the step is a Minimum Move (A4.134; ruling R10.9): the movers' only hex this MPh;"),
    ("    /// <summary>The Location the stack Dashes to from the road Location it is in (A4.63; pass 40); null when it is not Dashing.</summary>\n"
     "    public BoardLocation? Dash\n    {\n        get; init;\n    }\n",
     "    /// <summary>The Location the stack Dashes to from the road Location it is in (A4.63; pass 40); null when it is not Dashing.</summary>\n"
     "    public BoardLocation? Dash\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>The laden MMC the leader's IPC went to with an earlier step of this move, when the mover named it (A4.42; pass 40).</summary>\n"
     "    public string? IpcTo\n    {\n        get; init;\n    }\n\n"
     "    /// <summary>Whether a step of this move has changed the stack's Location: Assault Movement allows one such step (A4.61; pass 40).</summary>\n"
     "    public bool ChangedLocation\n    {\n        get; init;\n    }\n"),
])
apply(U + "GameEventWriter.cs", [
    ("                if (moving.Dash is { } dashTo)\n                {\n                    writer.WriteString(\"dash\", dashTo.ToString());\n                }\n",
     "                if (moving.Dash is { } dashTo)\n                {\n                    writer.WriteString(\"dash\", dashTo.ToString());\n                }\n\n"
     "                if (moving.IpcTo is { } ipcTo)\n                {\n                    writer.WriteString(\"ipcTo\", ipcTo);\n                }\n"),
])
apply(U + "GameEventReader.cs", [
    ("                        Dash = dash,\n", "                        Dash = dash,\n                        IpcTo = fields.OptionalString(payload, \"ipcTo\", path),\n"),
])
apply(U + "GameState.cs", [
    ("    public IReadOnlyList<string> HazardousMovers { get; init; } = [];\n",
     "    public IReadOnlyList<string> HazardousMovers { get; init; } = [];\n\n"
     "    /// <summary>\n"
     "    /// The items portaged in this phase, each with the unit that first spent MF holding it (A4.4; pass 40): the unit does not recoup their PP by\n"
     "    /// dropping them, and no other unit portages them. Emptied at every phase change.\n"
     "    /// </summary>\n"
     "    public IReadOnlyList<(string Unit, string Weapon)> PortagedThisPhase { get; init; } = [];\n"),
])
apply(U + "GameProjector.cs", [
    ("                    SmokeAttempts = [],\n", "                    SmokeAttempts = [],\n                    PortagedThisPhase = [],\n"),
    ("                    RoadRate = moving.RoadRate,\n",
     "                    RoadRate = moving.RoadRate,\n"
     "                    IpcTo = moving.IpcTo ?? current?.IpcTo,\n"
     "                    ChangedLocation = current?.ChangedLocation == true || (movers[0]!.Position is MapPosition stoodAt && stoodAt.Location != moving.To),\n"),
    ("                HazardousMovers = Rules.ScenarioA1MovementCalculator.HazardousMovers(next.HazardousMovers, moving.PushedGun is not null, moving.Movers),\n",
     "                HazardousMovers = Rules.ScenarioA1MovementCalculator.HazardousMovers(next.HazardousMovers, moving.PushedGun is not null, moving.Movers),\n"
     "                PortagedThisPhase = Rules.ScenarioA1MovementCalculator.Portaged(next.PortagedThisPhase,\n"
     "                    next.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } carrier && moving.Movers.Contains(carrier.Holder, StringComparer.Ordinal))\n"
     "                        .Select(item => (item.Holding!.Holder, item.Id))),\n"),
    (".Select(item => new Rules.CarriedWeaponFacts(item.Id, vocabulary.IsA(item.Kind, \"asl:light-mortar\"), next.MovedWeapons.Contains(item.Id, StringComparer.Ordinal)))],",
     ".Select(item => new Rules.CarriedWeaponFacts(item.Id, vocabulary.IsA(item.Kind, \"asl:light-mortar\") || vocabulary.IsA(item.Kind, \"asl:mg\"), next.MovedWeapons.Contains(item.Id, StringComparer.Ordinal)))],"),
])
apply(R + "ScenarioA1MovementModels.cs", [
    ("public sealed record CarriedWeaponFacts(string Id, bool LightMortar, bool AlreadyMoved);",
     "/// <summary>A SW a mover carries: its id, whether it is one A4.41 names (a light mortar, or a MG, whose size the fire reads), and whether it is already marked moved.</summary>\n"
     "public sealed record CarriedWeaponFacts(string Id, bool LightMortar, bool AlreadyMoved);"),
])

# ---- Play -----------------------------------------------------------------------------------------------------------------------
apply(P + "GamePlanner.Movement.cs", [
    ("    private int? MfAllotment(GameState state, UnitInstance unit, int doubleTimeMf, bool cx, int bonusMf = 0, int ipcBonus = 0) =>\n",
     "    private int? MfAllotment(GameState state, UnitInstance unit, int doubleTimeMf, bool cx, int bonusMf = 0, int ipcBonus = 0, int? allowance = null) =>\n"),
    ("            doubleTimeMf, cx, bonusMf, ipcBonus, () => Experience.MoveAllowance(state, unit, catalogs, vocabulary), () => Portage(state, unit));",
     "            doubleTimeMf, cx, bonusMf, ipcBonus, () => allowance ?? Experience.MoveAllowance(state, unit, catalogs, vocabulary), () => Portage(state, unit));"),
    ("        var values = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)\n"
     "            .Select(PortageOf).ToArray();\n",
     "        // A4.4 (pass 40): with what it holds, what it portaged earlier in this phase and dropped: that portage cost is not recouped.\n"
     "        EquipmentInstance[] held = [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)];\n"
     "        var values = held.Concat(state.PortagedThisPhase.Where(item => item.Unit == unit.Id && !held.Any(now => now.Id == item.Weapon))\n"
     "            .Select(item => state.Find(item.Weapon)).OfType<EquipmentInstance>()).Select(PortageOf).ToArray();\n"),
    ("        if (ScenarioA1MovementCalculator.MoveTiming(current is not null, current?.Assault == true, assault, doubleTime, state.Nvr,\n",
     "        // A4.4 (pass 40): no item is portaged more than once a phase.\n"
     "        if (ScenarioA1MovementCalculator.PortagedTwiceBar(state.Equipment.Where(item => item.Status == InstanceStatus.Active\n"
     "                && item.Holding is { Role: HoldingRole.Possessed } carrier && movers.Any(unit => unit.Id == carrier.Holder))\n"
     "            .Select(item => (item.Id, item.Holding!.Holder, state.PortagedThisPhase.Where(known => known.Weapon == item.Id).Select(known => known.Unit).FirstOrDefault()))) is { } twice)\n"
     "        {\n"
     "            return Refused(scope, label, expected, twice);\n"
     "        }\n\n"
     "        if (ScenarioA1MovementCalculator.MoveTiming(current is not null, current?.Assault == true, assault, doubleTime, state.Nvr,\n"),
    ("                vocabulary.IsA(unit.Kind, \"asl:smc\"), Portage(state, unit)?.Sum()))]) is { } badTiming)",
     "                vocabulary.IsA(unit.Kind, \"asl:smc\"), Portage(state, unit)?.Sum()))], current?.ChangedLocation == true, to != from) is { } badTiming)"),
    ("        var leaderIpcTo = LeaderIpcRecipient(state, movers);\n",
     "        // A4.42 (pass 40, ruling R40.11): the laden MMC the mover names for the leader's IPC, kept for the whole move.\n"
     "        var ipcChosen = Text(arguments, \"ipcTo\", out var ipcText) ? ipcText : null;\n"
     "        var leaderIpcTo = LeaderIpcRecipient(state, movers, ipcChosen ?? current?.IpcTo);\n"
     "        if (ScenarioA1MovementCalculator.IpcChoiceBar(ipcChosen, current?.IpcTo, leaderIpcTo.Recipient) is { } ipcBar)\n"
     "        {\n"
     "            return Refused(scope, label, expected, ipcBar);\n"
     "        }\n\n"),
    ("            Dash = forcedBack ? null : dashTo,\n", "            Dash = forcedBack ? null : dashTo,\n            IpcTo = ipcChosen,\n"),
])
apply(P + "GamePlanner.Terrain.cs", [
    ("    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, UnitInstance[] movers) =>\n"
     "        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]));",
     "    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, UnitInstance[] movers, string? chosen = null) =>\n"
     "        ScenarioA1MovementCalculator.LeaderIpcRecipient([.. movers.Select(MovingUnit)], index => Laden(state, movers[index]), chosen);"),
    ("    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to, bool terrainRate = false)\n",
     "    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to, bool terrainRate = false, bool withoutSmoke = false)\n"),
    ("        return ScenarioA1TerrainCosts.InfantryStep(facts, () => BlazeEntryHalfMf(state, to), (terrain, road, rise) => InfantryWeatherHalfMf(state, crossed!, terrain, road, rise));",
     "        return ScenarioA1TerrainCosts.InfantryStep(facts, () => withoutSmoke ? 0 : BlazeEntryHalfMf(state, to), (terrain, road, rise) => InfantryWeatherHalfMf(state, crossed!, terrain, road, rise));"),
])
apply(P + "GamePlanner.CloseCombat.cs", [
    ("        var halfMf = ScenarioA1AdvanceCalculator.AdvanceHalfMf(entry.HalfMf, units.Any(unit => state.Encircled(unit!)), excess);\n",
     "        var halfMf = ScenarioA1AdvanceCalculator.AdvanceHalfMf(entry.HalfMf, units.Any(unit => state.Encircled(unit!)), excess);\n\n"
     "        // A4.72 (p. 52; pass 40): Difficult Terrain is judged by the hex's MF cost \"excluding SMOKE\".\n"
     "        var drySmoke = entering is null && BlazeEntryHalfMf(state, to) > 0 ? InfantryStep(state, from, to, false, true).Entry : null;\n"
     "        var difficultHalfMf = drySmoke is null ? halfMf : ScenarioA1AdvanceCalculator.AdvanceHalfMf(drySmoke.HalfMf, units.Any(unit => state.Encircled(unit!)), excess);\n"),
    ("            if (DifficultAdvance(state, unit!, halfMf, Aided(unit!), unit!.Id == ipcTo) is not { } difficult)",
     "            if (DifficultAdvance(state, unit!, difficultHalfMf, Aided(unit!), unit!.Id == ipcTo) is not { } difficult)"),
])
apply(P + "LiveFire.cs", [
    ("kind, state.Movement?.Assault ?? false, () => state.Side(side)?.Elr, friendlyTargets,", "kind, AssaultOf(state), () => state.Side(side)?.Elr, friendlyTargets,"),
    ("            target.ToString(), here, movement?.Assault ?? false,\n", "            target.ToString(), here, AssaultOf(state),\n"),
    ("        return ScenarioA1FireEligibility.Residual(new ResidualFacts(here, movement?.Assault ?? false,", "        return ScenarioA1FireEligibility.Residual(new ResidualFacts(here, AssaultOf(state),"),
    ("                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];\n",
     "                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];\n\n"
     "                // A4.41 (pass 40): a MMG or HMG carried into a new Location in the MPh does not fire in the AFPh.\n"
     "                if (ScenarioA1FireEligibility.MovedWeaponBar(state.Phase, used.Select(weapon => (weapon.EquipmentId ?? string.Empty,\n"
     "                    weapon.EquipmentId is { } carried && state.MovedWeapons.Contains(carried, StringComparer.Ordinal),\n"
     "                    weapon.DefinitionId is { } definition ? CatalogReference.Value.Definitions.GetValueOrDefault(definition)?.Size : null))) is { } movedBar)\n"
     "                {\n"
     "                    return (null, movedBar);\n"
     "                }\n"),
    ("    private static readonly Lazy<ScenarioA1FireReference> CatalogReference = new(() => new ScenarioA1FirePackage().Reference);\n",
     "    private static readonly Lazy<ScenarioA1FireReference> CatalogReference = new(() => new ScenarioA1FirePackage().Reference);\n\n"
     "    /// <summary>The moving stack's Assault Movement as fire reads it (A4.61; pass 40): declared, and no mover broken or berserk since.</summary>\n"
     "    private static bool AssaultOf(GameState state) => state.Movement is { } moving && ScenarioA1FireEligibility.AssaultHolds(moving.Assault,\n"
     "        moving.Movers.Select(id => state.Unit(id)).OfType<UnitInstance>().Select(unit => (Is(unit, Conditions.Broken), Is(unit, Conditions.Berserk))));\n"),
])
apply(P + "GamePlanner.Passengers.cs", [
    ("            if (MfAllotment(state, unit, 0, Is(unit, Conditions.Cx)) is not { } allotment)\n",
     "            // A4.5, A4.11 (pass 40): no boarding after Double Time, and four MF for a unit that mounts.\n"
     "            if (ScenarioA1PassengerCalculator.BoardingDoubleTimeBar(unitId, unit.DoubleTimeMf) is { } tiredBar)\n"
     "            {\n"
     "                return Refused(scope, label, expected, tiredBar);\n"
     "            }\n\n"
     "            if (MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), 0, 0, ScenarioA1PassengerCalculator.ConveyanceAllowance) is not { } allotment)\n"),
])
apply(P + "GamePlanner.SupportWeapons.cs", [
    ("        // A20.5 (ruling R14.5; table player, pass 14): an Unarmed unit uses no SW, so it Recovers none.\n        if (Is(unit, Conditions.Unarmed))\n        {\n            return Refused(scope, label, expected, $\"play.recover-unarmed:",
     "        // A4.44 (pass 40): never by a Bypassing unit.\n"
     "        if (ScenarioA1SupportWeaponRules.RecoveryBypassBar(unit.Id, state.Movement is { Bypass.Count: > 0 } lane && lane.Members.Contains(unit.Id, StringComparer.Ordinal)) is { } bypassBar)\n"
     "        {\n"
     "            return Refused(scope, label, expected, bypassBar);\n"
     "        }\n\n"
     "        // A20.5 (ruling R14.5; table player, pass 14): an Unarmed unit uses no SW, so it Recovers none.\n        if (Is(unit, Conditions.Unarmed))\n        {\n            return Refused(scope, label, expected, $\"play.recover-unarmed:"),
])
apply(P + "GameActions.cs", [
    ("\"dash\": { \"type\": \"string\" },", "\"dash\": { \"type\": \"string\" }, \"ipcTo\": { \"type\": \"string\" },"),
])

# ---- The page -------------------------------------------------------------------------------------------------------------------
apply(M + "Components/Play/InfantryMovementAction.razor", [
    ('<label><input id="move-assault" type="checkbox"',
     '@if (Selected.Count > 1)\n{\n'
     '    <label title="A4.42: one leader adds his IPC to one laden MMC that starts the phase with him and moves with him. With one laden MMC it goes there by itself; with several, name the one. The choice holds for the whole move.">Leader\'s IPC to\n'
     '        <select id="move-ipc" value="@Draft.IpcTo" @onchange="@(args => Changed(Draft with { IpcTo = args.Value as string ?? string.Empty }))">\n'
     '            <option value="">the only laden MMC</option>\n'
     '            @foreach (var row in Movers.Where(row => Selected.Contains(row.Id)))\n'
     '            {\n'
     '                <option value="@row.Id">@row.Label</option>\n'
     '            }\n'
     '        </select>\n'
     '    </label>\n}\n'
     '<label><input id="move-assault" type="checkbox"'),
    ("bool TerrainRate = false, string Dash = \"\");", "bool TerrainRate = false, string Dash = \"\", string IpcTo = \"\");"),
])
apply(M + "Components/Pages/Play.razor", [
    ("    private string moveDash = string.Empty;\n", "    private string moveDash = string.Empty;\n    private string moveIpcTo = string.Empty;\n"),
    ("moveTerrainRate, moveDash);", "moveTerrainRate, moveDash, moveIpcTo);"),
    ("        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate, draft.Dash);",
     "        (moveTo, moveMinimum, moveBypass, moveAssault, moveDoubleTime, movePush, moveTerrainRate, moveDash, moveIpcTo) =\n"
     "            (draft.To, draft.Minimum, draft.Bypass, draft.Assault, draft.DoubleTime, draft.Push, draft.TerrainRate, draft.Dash, draft.IpcTo);"),
    ("        if (moveDash.Trim().Length > 0)\n        {\n            arguments[\"dash\"] = moveDash.Trim();\n        }\n",
     "        if (moveDash.Trim().Length > 0)\n        {\n            arguments[\"dash\"] = moveDash.Trim();\n        }\n\n"
     "        // A4.42 (pass 40): the laden MMC named for the leader's IPC.\n"
     "        if (moveIpcTo.Length > 0 && moveUnits.Contains(moveIpcTo))\n        {\n            arguments[\"ipcTo\"] = moveIpcTo;\n        }\n"),
])

for path, text in PENDING.items():
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)
print("ok", len(PENDING))
