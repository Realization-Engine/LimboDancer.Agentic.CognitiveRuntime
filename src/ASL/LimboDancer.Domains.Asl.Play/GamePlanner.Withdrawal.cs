using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Withdrawal completed (pass 70, task 70.6; ruling R70.26): the SW a unit above its IPC drops to withdraw, and what a withdrawal into a concealed or hidden
/// enemy's Location meets there. The Locations are read here, through the state's index, and <see cref="ScenarioA1Withdrawal"/> decides; the reveals are
/// public <c>concealment-lost</c> events, as every reveal is, and a Dummy's loss its removal.
/// </summary>
public sealed partial class GamePlanner
{
    public static bool MeleeWithdrawalEligible(GameState state, UnitInstance unit, BoardLocation location) =>
        ScenarioA1WithdrawalEquipmentRules.MayPrepareMelee(state.Phase, state.CloseCombats.Any(item => item.Location == location),
            unit.Status == InstanceStatus.Active, Is(unit, Conditions.Melee), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk),
            Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured));

    public static bool AmbushWithdrawalEligible(GameState state, UnitInstance unit, BoardLocation location)
    {
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        return ScenarioA1PrisonerCalculator.AmbushWithdrawalPhaseBar(state.Phase, entry?.Ambusher is not null,
                entry is { } begun && begun.Rounds.Count > 0 && !begun.Closed) is null
            && ScenarioA1PrisonerCalculator.AmbushWithdrawer(unit.Status == InstanceStatus.Active, unit.Side == entry?.Ambusher,
                state.Location(unit.Id)?.Location == location, Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk),
                Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured), LiveFire.IsVehicle(unit));
    }

    public static IReadOnlyList<string> WithdrawalEquipmentChoices(GameState state, UnitInstance unit) =>
        [.. state.Equipment.Where(weapon => ScenarioA1WithdrawalEquipmentRules.MayChooseDrop(state.Phase, true,
            unit.Status == InstanceStatus.Active, weapon.Status == InstanceStatus.Active,
            weapon.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)).Select(weapon => weapon.Id)];

    public WithdrawalEquipmentLoad WithdrawalEquipmentLoad(GameState state, UnitInstance unit, IReadOnlyCollection<string> dropped) =>
        ScenarioA1WithdrawalEquipmentRules.Load(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded), Is(unit, Conditions.Cx), Portage(state, unit, dropped));

    /// <summary>
    /// The SW named in the arguments' <c>drops</c>, by the withdrawing or infiltrating unit that possesses each (A11.21, A4.43; ruling R70.26), or why the
    /// list is refused: a SW that no such unit possesses.
    /// </summary>
    private static (Dictionary<string, IReadOnlyList<string>> Drops, string? Bar) WithdrawalDrops(GameState state, JsonElement arguments, IEnumerable<string> movers)
    {
        var moving = movers.ToHashSet(StringComparer.Ordinal);
        string[] named = [.. Strings(arguments, "drops").Distinct(StringComparer.Ordinal)];
        string? Holder(string weapon) => state.Find(weapon) is EquipmentInstance { Holding: { Role: HoldingRole.Possessed } holding } item
            && ScenarioA1WithdrawalEquipmentRules.MayChooseDrop(state.Phase, moving.Contains(holding.Holder),
                state.Unit(holding.Holder)?.Status == InstanceStatus.Active, item.Status == InstanceStatus.Active, true) ? holding.Holder : null;
        if (ScenarioA1Withdrawal.ForeignDrop(named, weapon => Holder(weapon) is { } holder && moving.Contains(holder)) is { } foreign)
        {
            return (new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal), ScenarioA1CloseCombatRules.DropBarText(foreign));
        }

        return (named.GroupBy(weapon => Holder(weapon)!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)[.. group.Order(StringComparer.Ordinal)], StringComparer.Ordinal), null);
    }

    /// <summary>
    /// What each Location withdrawn into meets (A11.21; ruling R70.26), by Location, read once a Location from the state's index before the withdrawal's
    /// own events: <paramref name="moving"/> gives each unit and where it goes (null when it stays).
    /// </summary>
    private Dictionary<string, WithdrawalEntryVerdict> WithdrawalEntries(GameState state, IEnumerable<(string Unit, string? To)> moving)
    {
        var verdicts = new Dictionary<string, WithdrawalEntryVerdict>(StringComparer.Ordinal);
        var index = MemoOf(state).Objects(state);
        foreach (var (unitId, to) in moving)
        {
            if (to is null || verdicts.ContainsKey(to) || index.Unit(unitId) is not { } unit || !BoardLocation.TryParse(to, out var at))
            {
                continue;
            }

            WithdrawalLocationFacts[] there = [.. index.At(at).Where(item => item is UnitInstance or EquipmentInstance)
                .Select(item => new WithdrawalLocationFacts(item.Id, item.Side, Is(item, Conditions.Captured), Is(item, Conditions.Concealed), Is(item, Conditions.Hidden),
                    item is UnitInstance { Kind: UnitKinds.Dummy }, item is EquipmentInstance))];
            verdicts[to] = ScenarioA1Withdrawal.Entry(there, unit.Side);
        }

        return verdicts;
    }

    /// <summary>
    /// The reveals of each Location whose withdrawers were eliminated (A11.21; ruling R70.26), appended in order: the Random Selection dr among several
    /// concealed units, then each unit that loses its "?" and each hidden unit and Gun placed on board unconcealed.
    /// </summary>
    private void AddWithdrawalReveals(GameScope scope, string attemptId, long expected, string actor, List<GameEvent> events,
        IReadOnlyDictionary<string, WithdrawalEntryVerdict> entries, Func<RollRequest, RollResult> draw, string? package)
    {
        var rolled = 0;
        foreach (var (_, verdict) in entries.Where(item => item.Value.Eliminated).OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            IReadOnlyList<int>? drs = null;
            if (ScenarioA1Withdrawal.SelectionRolled(verdict.Selection.Count))
            {
                var selection = draw(new RollRequest(verdict.Selection.Count, 6));
                rolled++;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled", new DiceRolled($"{attemptId}-withdrawal-{rolled.ToString(CultureInfo.InvariantCulture)}",
                    ScenarioA1Withdrawal.SelectionPurpose, verdict.Selection.Count, 6, selection.Values, DiceRolled.SystemSource, actor), package, null));
                drs = selection.Values;
            }

            events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, [.. ScenarioA1Withdrawal.Revealed(verdict.Selection, drs), .. verdict.Placed]));
        }
    }
}
