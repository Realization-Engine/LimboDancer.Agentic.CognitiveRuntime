"""Pass 32.g commit 8: LiveCloseCombat's builders call Rules (ScenarioA1CloseCombatFactBuilders)."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
LC = ROOT / "LimboDancer.Domains.Asl.Play/LiveCloseCombat.cs"
C = "ScenarioA1CloseCombatFactBuilders"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:90])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(LC, [
    (
        '        if (state.Catalog.Catalog != LiveFire.Catalog || state.Catalog.Version != LiveFire.CatalogVersion)\n'
        '        {\n'
        '            return (null, $"play.cc-catalog: the Close Combat package reads {LiveFire.Catalog}@{LiveFire.CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");\n'
        '        }\n'
        '\n'
        '        // A11.19 (ruling R14.2): Dummies are removed before any attack is declared, so they are never CC units.\n'
        '        UnitInstance[] units = [.. state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Kind != UnitKinds.Dummy)\n'
        '            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];\n'
        '        if (units.Any(unit => unit.Definition is null))\n'
        '        {\n'
        '            return (null, "play.cc-units: the Location holds a unit outside the catalog (A11.19)");\n'
        '        }\n'
        '\n'
        '        return ([.. units.Select(unit => new CloseCombatUnit(unit.Id, unit.Definition!.Definition, unit.Side, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),\n'
        '            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Berserk), Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic),\n'
        '            Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured),\n'
        '            state.Advances.Any(item => item.Unit == unit.Id && item.To == location), Is(unit, Conditions.Melee))\n'
        '        {\n'
        '            StackedWith = stacking?.GetValueOrDefault(unit.Id),\n'
        '            WithdrawingTo = withdrawals?.GetValueOrDefault(unit.Id),\n'
        '            Cx = Is(unit, Conditions.Cx) ? true : null,\n'
        '\n'
        '            // Rulings R14.3, R14.5, R14.8: TI, Unarmed units and prisoners\' Guards, and the declared Infiltrations.\n'
        '            Ti = Is(unit, "asl:ti") ? true : null,\n'
        '            Unarmed = Is(unit, Conditions.Unarmed) ? true : null,\n'
        '            GuardId = Is(unit, Conditions.Captured) ? unit.Custodian : null,\n'
        '            InfiltrateTo = infiltrations?.GetValueOrDefault(unit.Id),\n'
        '            Weapons = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)\n'
        '                .Select(item => item.Id).Order(StringComparer.Ordinal).ToArray() is { Length: > 0 } weapons ? weapons : null,\n'
        '        })], null);\n',
        f'        if ({C}.CatalogBar(state.Catalog.Catalog, state.Catalog.Version, LiveFire.Catalog, LiveFire.CatalogVersion) is {{ }} catalogBar)\n'
        '        {\n'
        '            return (null, catalogBar);\n'
        '        }\n'
        '\n'
        '        // A11.19 (ruling R14.2): Dummies are removed before any attack is declared, so they are never CC units.\n'
        f'        UnitInstance[] units = [.. state.At(location).OfType<UnitInstance>().Where(unit => {C}.IsCcUnit(unit.Status == InstanceStatus.Active, unit.Kind == UnitKinds.Dummy))\n'
        '            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];\n'
        '        if (units.Any(unit => unit.Definition is null))\n'
        '        {\n'
        f'            return (null, {C}.OutsideCatalogText());\n'
        '        }\n'
        '\n'
        '        // Rulings R14.3, R14.5, R14.8: TI, Unarmed units and prisoners\' Guards, and the declared Infiltrations; Rules builds each unit (pass 32.g).\n'
        f'        return ([.. units.Select(unit => {C}.Unit(unit.Id, unit.Definition!.Definition, unit.Side, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned),\n'
        '            Is(unit, Conditions.Wounded), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Berserk), Is(unit, Conditions.Fanatic), Is(unit, Conditions.Heroic),\n'
        '            Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured), state.Advances.Any(item => item.Unit == unit.Id && item.To == location),\n'
        '            Is(unit, Conditions.Melee), stacking?.GetValueOrDefault(unit.Id), withdrawals?.GetValueOrDefault(unit.Id), Is(unit, Conditions.Cx), Is(unit, "asl:ti"),\n'
        '            Is(unit, Conditions.Unarmed), unit.Custodian, infiltrations?.GetValueOrDefault(unit.Id),\n'
        '            [.. state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding && holding.Holder == unit.Id)\n'
        '                .Select(item => item.Id).Order(StringComparer.Ordinal)]))], null);\n',
    ),
    (
        '        return units is null ? (null, reason) : (new AmbushFacts(state.Phase == "ccph" ? "CCPh" : state.Phase, location.ToString(), terrain, state.PhasingSide, units, null)\n'
        '        {\n'
        '            HiddenPlaced = units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal)) ? true : null,\n'
        '        }, null);\n',
        f'        return units is null ? (null, reason) : ({C}.Ambush(state.Phase, location.ToString(), terrain, state.PhasingSide, units,\n'
        '            units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))), null);\n',
    ),
    (
        '        if (overrun)\n'
        '        {\n'
        '            return (new CloseCombatFacts("MPh", location.ToString(), terrain, state.PhasingSide, CloseCombatFacts.Simultaneous, null, units, [], [], attacks, null)\n'
        '            {\n'
        '                InfantryOverrun = true,\n'
        '            }, null);\n'
        '        }\n'
        '\n'
        '        if (entry is null && ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units, units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))))\n'
        '        {\n'
        '            return (null, $"play.cc-ambush-first: Infantry advanced into CC in {location}, so the Ambush drs come first (A11.4)");\n'
        '        }\n'
        '\n'
        '        // A11.33, A11.34 (ruling R14.6): the prisoners\' escape round, when asked for, comes before any other; the rounds after it follow as before.\n'
        '        var rounds = entry?.Rounds.Where(item => item != CloseCombatFacts.PrisonersRound).Count() ?? 0;\n'
        '        var ambushers = entry?.Ambusher is { } ambusher && units.Any(unit => unit.Side == ambusher && unit.Captured != true);\n'
        '        var round = requested == CloseCombatFacts.PrisonersRound && (entry is null || entry.Rounds.Count == 0) ? CloseCombatFacts.PrisonersRound\n'
        '            : entry?.Ambusher is null ? CloseCombatFacts.Simultaneous\n'
        '            : ambushers && (rounds == 0 || requested == CloseCombatFacts.AmbusherRound) ? CloseCombatFacts.AmbusherRound\n'
        '            : CloseCombatFacts.AmbushedRound;\n'
        '\n'
        '        // J2.31 (ruling R14.1): the Location\'s first round other than the prisoners\' declares Hand-to-Hand for the CCPh.\n'
        '        var hand = rounds > 0 ? entry!.HandToHand : round != CloseCombatFacts.PrisonersRound && handToHand;\n'
        '        return (new CloseCombatFacts(state.Phase == "ccph" ? "CCPh" : state.Phase, location.ToString(), terrain, state.PhasingSide, round, entry?.Ambusher, units,\n'
        '            entry?.Attacked ?? [], entry?.Attacking ?? [], attacks, null)\n'
        '        {\n'
        '            HandToHand = hand ? true : null,\n'
        '        }, null);\n',
        '        if (overrun)\n'
        '        {\n'
        f'            return ({C}.Overrun(location.ToString(), terrain, state.PhasingSide, units, attacks), null);\n'
        '        }\n'
        '\n'
        f'        if ({C}.AmbushFirstBar(entry is not null, ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units, units.Any(unit => state.HiddenPlaced.Contains(unit.UnitId!, StringComparer.Ordinal))),\n'
        '            location.ToString()) is { } ambushFirst)\n'
        '        {\n'
        '            return (null, ambushFirst);\n'
        '        }\n'
        '\n'
        '        // A11.33, A11.34 (ruling R14.6): the prisoners\' escape round, when asked for, comes before any other; the rounds after it follow as before. J2.31\n'
        '        // (ruling R14.1): the Location\'s first round other than the prisoners\' declares Hand-to-Hand for the CCPh.\n'
        f'        var (round, rounds) = {C}.NextRound(requested, entry?.Rounds ?? [], entry?.Ambusher, entry?.Ambusher is {{ }} ambusher && units.Any(unit => unit.Side == ambusher && unit.Captured != true));\n'
        f'        var hand = {C}.HandToHand(rounds, entry?.HandToHand == true, round, handToHand);\n'
        f'        return ({C}.Round(state.Phase, location.ToString(), terrain, state.PhasingSide, round, entry?.Ambusher, units, entry?.Attacked ?? [], entry?.Attacking ?? [], attacks, hand), null);\n',
    ),
    (
        '            return (null, $"play.cc-vehicle: {vehicleId} is not an active vehicle in {location}");\n',
        f'            return (null, {C}.VehicleNotActiveText(vehicleId, location.ToString()));\n',
    ),
    (
        '        var moving = reaction && state.Movement is { Vehicle: true, Started: true, Stopped: false } movement && movement.Members.Contains(vehicle.Id, StringComparer.Ordinal);\n'
        '        var facts = new VehicleCloseCombatFacts(state.Phase == "ccph" ? "CCPh" : state.Phase == "mph" ? "MPh" : state.Phase, location.ToString(),\n'
        '            new VehicleCloseCombatVehicle(vehicle.Id, definition.Definition, vehicle.Side, LiveFire.CrewExposed(vehicle), Is(vehicle, Conditions.Motion) || moving,\n'
        '                Is(vehicle, Conditions.Immobilized) || Is(vehicle, Conditions.Bogged), Is(vehicle, Conditions.Stunned) || Is(vehicle, Conditions.Recalled),\n'
        '                Is(vehicle, Conditions.Shocked) || Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Abandoned),\n'
        '                Is(vehicle, Conditions.Malfunctioned) || Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned)),\n'
        '            [.. units.Where(unit => state.Unit(unit.UnitId!) is { } found && !LiveFire.IsVehicle(found))],\n'
        '            byVehicle ? [] : attackers, byVehicle ? defenders : [], byVehicle, reaction, null)\n'
        '        {\n'
        '            FireMarked = reaction && attackers.Where(id => state.Unit(id) is { } unit && (Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire))).ToArray() is { Length: > 0 } marked\n'
        '                ? marked : null,\n'
        '        };\n'
        '        return (facts, null);\n',
        f'        var moving = {C}.VehicleMoving(reaction, state.Movement is {{ Vehicle: true, Started: true, Stopped: false }},\n'
        '            state.Movement?.Members.Contains(vehicle.Id, StringComparer.Ordinal) == true);\n'
        f'        var facts = {C}.VehicleAttack(state.Phase, location.ToString(),\n'
        f'            {C}.Vehicle(vehicle.Id, definition.Definition, vehicle.Side, LiveFire.CrewExposed(vehicle), Is(vehicle, Conditions.Motion), moving,\n'
        '                Is(vehicle, Conditions.Immobilized), Is(vehicle, Conditions.Bogged), Is(vehicle, Conditions.Stunned), Is(vehicle, Conditions.Recalled),\n'
        '                Is(vehicle, Conditions.Shocked), Is(vehicle, Conditions.UnconfirmedKill), Is(vehicle, Conditions.Abandoned),\n'
        '                Is(vehicle, Conditions.Malfunctioned), Is(vehicle, Conditions.Disabled), Is(vehicle, Conditions.BmgMalfunctioned), Is(vehicle, Conditions.CmgMalfunctioned)),\n'
        '            [.. units.Where(unit => state.Unit(unit.UnitId!) is { } found && !LiveFire.IsVehicle(found))], attackers, defenders, byVehicle, reaction,\n'
        f'            {C}.FireMarked(reaction, [.. attackers.Where(id => state.Unit(id) is {{ }} unit && (Is(unit, Conditions.FirstFire) || Is(unit, Conditions.FinalFire)))]));\n'
        '        return (facts, null);\n',
    ),
])
