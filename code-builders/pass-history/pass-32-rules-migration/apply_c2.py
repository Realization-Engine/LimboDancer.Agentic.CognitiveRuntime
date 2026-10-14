"""Commit 2 of pass 32.c: FromState's blocks, ResidualFromState, OverrunFromState, DemolitionChargeFromState move to Rules."""
import sys

S = r"C:\Users\dland\AppData\Local\Temp\claude\E--Archive-GitHub-dlandi-LimboDancer-MCP\f1a7628b-868f-42e5-a3e2-1a2cb327ccb3\scratchpad"
R = r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL"
LIVE = R + r"\LimboDancer.Domains.Asl.Play\LiveFire.cs"
ELIG = R + r"\LimboDancer.Domains.Asl.Rules\ScenarioA1FireEligibility.cs"
MODELS = R + r"\LimboDancer.Domains.Asl.Rules\ScenarioA1FireRuleModels.cs"


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        return f.read()


def write(path, text):
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def nl(text):
    return "\r\n" if "\r\n" in text else "\n"


def append_in_class(path, draft):
    text = read(path)
    eol = nl(text)
    draft = read(draft).replace("\r\n", "\n").replace("\n", eol)
    marker = eol + "}" + eol
    assert text.endswith(marker), path
    write(path, text[: -len(marker)] + eol + draft.rstrip(eol) + eol + "}" + eol)


def append_file(path, draft):
    text = read(path)
    eol = nl(text)
    draft = read(draft).replace("\r\n", "\n").replace("\n", eol)
    write(path, text.rstrip(eol) + eol + draft.rstrip(eol) + eol)


def replace_to_end(text, start, new):
    """Replace from the unique start line to the next '        }, null);\\n    }\\n' with new."""
    eol = nl(text)
    start = start.replace("\n", eol)
    new = new.replace("\n", eol)
    assert text.count(start) == 1, start[:60]
    begin = text.index(start)
    ender = eol + "        }, null);" + eol + "    }" + eol
    end = text.index(ender, begin) + len(ender)
    return text[:begin] + new.rstrip(eol) + eol + text[end:]


append_file(MODELS, S + r"\draft-c2-models.cs")
append_in_class(ELIG, S + r"\draft-c2-eligibility.cs")

live = read(LIVE)

FROM_START = "        // D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group.\n"
FROM_NEW = '''        // D1.83, D3.4 (ruling R25.7): a vehicle's MG fires alone, with no leader and no Infantry in its group. Rules decides each block (pass 32.c).
        var vehicleFirers = firerIds.Select(state.Unit).OfType<UnitInstance>().Where(IsVehicle).ToArray();
        if (ScenarioA1FireEligibility.VehicleFiresAlone(vehicleFirers.Length, firerIds.Count, directorIds.Count, weapons is { Count: > 0 }) is { } grouped)
        {
            return (null, grouped);
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in firerIds)
        {
            var unit = state.Unit(id);
            BoardLocation? at = unit is null ? null : state.Location(unit.Id)?.Location;
            if (ScenarioA1FireEligibility.FirerBar(new FirerStateFacts(id, unit is { Status: InstanceStatus.Active, Definition: not null }, at is not null,
                unit is not null && Is(unit, "asl:ti"))) is { } barred)
            {
                return (null, barred);
            }

            firers.Add((unit!, at!.Value));
        }

        var locations = firers.Select(item => item.At).Distinct().ToArray();
        var directors = new List<(UnitInstance Unit, BoardLocation At)>();
        foreach (var id in directorIds)
        {
            var leader = state.Unit(id);
            BoardLocation? at = leader is null ? null : state.Location(leader.Id)?.Location;
            if (ScenarioA1FireEligibility.DirectorBar(new DirectorStateFacts(id, leader is { Status: InstanceStatus.Active, Definition: not null },
                at is { } here ? locations.Contains(here) : null)) is { } barred)
            {
                return (null, barred);
            }

            directors.Add((leader!, at!.Value));
        }

        var side = firers[0].Unit.Side;

        // D3.3, A8.1, A8.3, A8.31 (rulings R6.9, R12.3): the kind of MPh fire; the Bounding Fire read and the firers' marks are read when asked.
        var (kindRefusal, kind) = ScenarioA1FireEligibility.MphFireKind(state.Phase, new FireKindFacts(vehicleFirers.Length > 0,
            vehicleFirers.Length > 0 && vehicleFirers[0].Side == state.PhasingSide, () => MayBoundingFire(state, vehicleFirers[0].Id),
            () => [.. firers.Select(item => (Is(item.Unit, Conditions.FinalFire), Is(item.Unit, Conditions.FirstFire)))],
            firers.All(item => withoutInherent?.Contains(item.Unit.Id) != true), withoutInherent is { Count: > 0 }));
        if (kindRefusal is not null)
        {
            return (null, kindRefusal);
        }

        // A8.1, A7.308, A15.41: who the attack hits, as Rules sorts the target Location's units.
        var movers = state.Phase == "mph" && state.Movement is { } movement && movement.Location == target ? movement.Movers : null;
        UnitInstance[] atTarget = [.. state.At(target).OfType<UnitInstance>()];
        UnitInstance UnitAt(string id) => atTarget.First(unit => unit.Id == id);
        var selection = ScenarioA1FireEligibility.SelectTargets([.. atTarget.Select(unit => new TargetUnitStateFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side,
            IsVehicle(unit), unit.Kind == UnitKinds.Dummy, unit.Definition is not null, movers?.Contains(unit.Id)))], movers is not null);
        if (selection.Refusal is { } outside)
        {
            return (null, outside);
        }

        var targetSide = ScenarioA1FireEligibility.TargetSide(side, selection.Targets.Select(id => UnitAt(id).Side),
            selection.Vehicles.Count > 0 ? UnitAt(selection.Vehicles[0]).Side : null, state.Sides.FirstOrDefault(item => item.Id != side)?.Id);
        var friendlyTargets = selection.Targets.Any(id => UnitAt(id).Side == side);

        FireWeapon Weapon(string id) => state.Find(id) is EquipmentInstance equipment
            ? new FireWeapon(equipment.Id, equipment.Definition?.Definition, Is(equipment, Conditions.Malfunctioned), Fired(equipment),
                Is(equipment, Conditions.FirstFire))
            : new FireWeapon(id, null, null, null, null);

        var firerFacts = new List<FireFirer>();
        foreach (var (unit, at) in firers.Where(item => !IsVehicle(item.Unit)))
        {
            IReadOnlyList<FireWeapon>? used = null;
            if (weapons?.TryGetValue(unit.Id, out var named) == true && named.Count > 0)
            {
                // A7.35, A9.8, A22.3, A7.351 (rulings R13.6, R12.4): the limits on the named weapons; the two state scans are read when asked.
                if (ScenarioA1FireEligibility.NamedWeaponsBar(new NamedWeaponsFacts(unit.Id,
                    [.. named.Select(id => new NamedWeaponFacts(id,
                        state.Find(id) is EquipmentInstance { Status: InstanceStatus.Active, Holding: { Role: HoldingRole.Possessed } holding } && holding.Holder == unit.Id,
                        state.Find(id) is EquipmentInstance held && Is(held, Conditions.Dismantled),
                        state.Find(id) is EquipmentInstance { Kind: "asl:ft" }))],
                    state.AssaultWeaponUsers.Contains(unit.Id, StringComparer.Ordinal), withoutInherent?.Contains(unit.Id) == true,
                    () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } other && other.Holder == unit.Id
                        && !named.Contains(item.Id, StringComparer.Ordinal) && (Fired(item) || Is(item, Conditions.FirstFire))),
                    () => state.SupportWeaponUses.Any(item => item.Unit == unit.Id))) is { } weaponBar)
                {
                    return (null, weaponBar);
                }

                used = [.. named.Select(id => Weapon(id) is var weapon && CapturedBy(weapon.DefinitionId, unit) is { } captured ? weapon with { Captured = captured } : weapon)];
            }

            // A3.3, A7.1, A7.351: the phase's limits on a firer; the Prep Fire scan and the count of its other fired weapons are read when asked.
            if (ScenarioA1FireEligibility.FirerLimitBar(new FirerLimitFacts(unit.Id, state.Phase, Is(unit, Conditions.BoundingFire), unit.Kind == "asl:squad",
                withoutInherent?.Contains(unit.Id) == true, Fired(unit), used is { Count: > 0 },
                () => state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } prepped && prepped.Holder == unit.Id
                    && Is(item, Conditions.PrepFire)),
                () => state.Equipment.Count(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holder && holder.Holder == unit.Id
                    && !(used ?? []).Any(weapon => weapon.EquipmentId == item.Id) && (Fired(item) || Is(item, Conditions.FirstFire))))) is { } limit)
            {
                return (null, limit);
            }

            // A7.351 (rulings R9.2, R9.7): a squad whose only fire this phase is one SW use still fires its inherent FP.
            var swOnly = state.SupportWeaponUses.Any(item => item.Unit == unit.Id);

            // A9.12 (ruling R12.4): a leader fires a MG with no inherent FP of his own, alone or with a stacked SMC as his partner.
            var leader = unit.Kind == "asl:leader";
            string? partner = null;
            if (leader && partners?.TryGetValue(unit.Id, out var named2) == true)
            {
                var helper = state.Unit(named2);
                if (ScenarioA1FireEligibility.PartnerBar(unit.Id, new PartnerFacts(named2, helper is { Status: InstanceStatus.Active }, helper?.Kind is "asl:leader" or "asl:hero",
                    helper?.Side == unit.Side, helper is not null && state.Location(helper.Id)?.Location == at, firerIds.Contains(named2) || directorIds.Contains(named2),
                    helper is not null && Fired(helper), helper is not null && Is(helper, Conditions.Broken))) is { } partnerBar)
                {
                    return (null, partnerBar);
                }

                partner = helper!.Id;
            }

            firerFacts.Add(ScenarioA1FireEligibility.Firer(new FirerRecordFacts(unit.Id, unit.Definition!.Definition, at.ToString(), Is(unit, Conditions.Broken),
                Is(unit, Conditions.Pinned), Is(unit, Conditions.Concealed), Fired(unit), used is not null,
                unit.Group is not null ? state.ElrOf(unit) : null, Is(unit, Conditions.FirstFire), Is(unit, Conditions.FinalFire), state.Phase, swOnly,
                state.GunCrewsFired.Contains(unit.Id, StringComparer.Ordinal), unit.Kind == "asl:squad", leader, withoutInherent?.Contains(unit.Id) == true, used,
                Is(unit, Conditions.Fanatic), Is(unit, Conditions.Wounded), Is(unit, Conditions.Cx), Is(unit, Conditions.BoundingFire), state.Encircled(unit), partner,
                molUser == unit.Id, GreenInexperienced(state, unit))));
        }

        FireDirector Director((UnitInstance Unit, BoardLocation At) item) =>
            ScenarioA1FireEligibility.Director(new DirectorRecordFacts(item.Unit.Id, item.Unit.Definition!.Definition, item.At.ToString(), Is(item.Unit, Conditions.Broken),
                Is(item.Unit, Conditions.Pinned), Is(item.Unit, Conditions.Concealed), Fired(item.Unit), Is(item.Unit, Conditions.FirstFire),
                state.SupportWeaponDirectors.Any(directed => directed.Leader == item.Unit.Id), Is(item.Unit, Conditions.Wounded), Is(item.Unit, Conditions.Cx)));

        // Rulings R12.8, R12.9, R12.11: the firing side's units in a Melee or as prisoners, a prisoner's Guard, and Encirclement; Rules assembles the attack.
        return (ScenarioA1FireEligibility.Assemble(new AttackAssemblyFacts(state.Phase, side == state.PhasingSide, firers[0].At.ToString(), target.ToString(),
            firerFacts, [.. directors.Select(Director)], state.ScenarioMonth,
            [.. selection.Targets.Select(UnitAt).Select(unit => ScenarioA1FireEligibility.AttackTarget(new AttackTargetFacts(Target(state, unit, target), unit.Side == side,
                Is(unit, Conditions.Captured), unit.Custodian, state.Encircled(unit))))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, kind, state.Movement?.Assault ?? false, () => state.Side(side)?.Elr, friendlyTargets,
            [.. selection.Vehicles.Select(id => Vehicle(UnitAt(id), target))], vehicleFirers.Length > 0 ? VehicleFire(state, vehicleFirers[0], firers[0].At) : null,
            targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.NoQuarter.Contains(side, StringComparer.Ordinal),
            Allies(state, side), [.. selection.Companions.Select(id => Target(state, UnitAt(id), target))])), null);
    }
'''

DC_START = "        if (state.Find(chargeId) is not EquipmentInstance { Status: InstanceStatus.Active, Definition: { } dcReference } charge)\n"
DC_NEW = '''        // Rules decides it (pass 32.c): the charge, its placement, its user, and the units of the target Location are read and handed over.
        var charge = state.Find(chargeId) as EquipmentInstance;
        var chargeOk = charge is { Status: InstanceStatus.Active, Definition: not null };
        var placement = chargeOk && mode == FireDemolitionCharge.Placed ? state.PlacedCharges.FirstOrDefault(item => item.Charge == chargeId && item.Operable) : null;
        var possessor = charge?.Holding is { Role: HoldingRole.Possessed } holding ? holding.Holder : null;
        var userId = placement?.Unit ?? possessor;
        var user = chargeOk && userId is not null ? state.Unit(userId) : null;
        var ok = user is { Definition: not null } && (mode != FireDemolitionCharge.Placed || placement is not null);
        var side = user?.Side;
        var movement = state.Movement;
        var here = state.Phase == "mph" && movement is not null && movement.Location == target;
        UnitInstance[] units = ok ? [.. state.At(target).OfType<UnitInstance>()] : [];
        var targetSide = ok ? state.Sides.FirstOrDefault(item => item.Id != side)?.Id : null;
        return ScenarioA1FireEligibility.DemolitionCharge(new DemolitionChargeFacts(chargeId, chargeOk, charge?.Definition?.Definition, mode, placement is not null,
            placement?.Unit, placement?.Cx, placement?.TargetsConcealed, possessor, user is { Definition: not null }, user?.Definition?.Definition, side,
            user is not null && Is(user, Conditions.Cx), user is not null && Is(user, Conditions.BoundingFire), ok ? GreenInexperienced(state, user!) : null,
            ok ? CapturedBy(charge!.Definition!.Definition, user!) : null, ok ? state.Location(userId!)?.Location.ToString() : null, side == state.PhasingSide, state.Phase,
            target.ToString(), here, movement?.Assault ?? false,
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), here && movement!.Movers.Contains(unit.Id)))],
            targetSide, targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal),
            ok ? state.Side(side!)?.Elr : null, ok && state.NoQuarter.Contains(side, StringComparer.Ordinal), ok ? Allies(state, side!) : null, state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target)));
    }
'''

RES_START = "        if (state.Movement is not { } movement || movement.Location != target)\n        {\n            return (null, \"play.fire-residual: Residual FP attacks the moving stack in its Location (A8.2)\");\n"
RES_NEW = '''        // Rules decides it (pass 32.c): the moving stack and the units of its Location; a target's and a vehicle's facts are read for the units the attack takes.
        var movement = state.Movement;
        var here = movement is not null && movement.Location == target;
        UnitInstance[] units = here ? [.. state.At(target).OfType<UnitInstance>()] : [];
        return ScenarioA1FireEligibility.Residual(new ResidualFacts(here, movement?.Assault ?? false,
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), movement!.Movers.Contains(unit.Id)))],
            state.PhasingSide, here ? state.Side(state.PhasingSide)?.Elr : null, here && state.NoQuarter.Contains(state.PhasingSide, StringComparer.Ordinal),
            state.ScenarioMonth, fp, target.ToString(),
            id => Target(state, units.First(unit => unit.Id == id), target), id => Vehicle(units.First(unit => unit.Id == id), target)));
    }
'''

OVR_START = "        // D7.11 (referee, pass 11): a vehicle destroyed before its declared OVR resolves still makes it, at half FP.\n"
OVR_NEW = '''        // D7.11 (referee, pass 11): a vehicle destroyed before its declared OVR resolves still makes it, at half FP. Rules decides it (pass 32.c).
        var vehicle = state.Unit(vehicleId);
        var found = vehicle is { Status: InstanceStatus.Active or InstanceStatus.Wrecked };
        var isVehicle = found && IsVehicle(vehicle!);
        BoardLocation? at = isVehicle ? state.Location(vehicleId)?.Location : null;
        var ok = at is not null && vehicle!.Definition is not null;
        UnitInstance[] units = ok ? [.. state.At(at!.Value).OfType<UnitInstance>()] : [];
        var targetSide = ok ? state.Sides.FirstOrDefault(side => side.Id != vehicle!.Side)?.Id : null;
        return ScenarioA1FireEligibility.Overrun(new OverrunFacts(found, isVehicle, at is not null, vehicle?.Definition is not null, vehicleId, vehicle?.Definition?.Definition,
            at?.ToString() ?? string.Empty, vehicle?.Side, ok && CrewExposed(vehicle!), ok && Is(vehicle!, Conditions.Immobilized), ok && Is(vehicle!, Conditions.Bogged),
            vehicle?.Status == InstanceStatus.Wrecked, ok && Is(vehicle!, Conditions.Malfunctioned), ok && Is(vehicle!, Conditions.Disabled),
            ok && Is(vehicle!, Conditions.BmgMalfunctioned), ok && Is(vehicle!, Conditions.CmgMalfunctioned),
            [.. units.Select(unit => new LocationUnitFacts(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, IsVehicle(unit), unit.Definition is not null,
                Is(unit, Conditions.Captured), false))],
            targetSide is null ? null : state.Side(targetSide)?.Elr, targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal), state.ScenarioMonth,
            id => Target(state, units.First(unit => unit.Id == id), at!.Value), id => Vehicle(units.First(unit => unit.Id == id), at!.Value)));
    }
'''

live = replace_to_end(live, FROM_START, FROM_NEW)
live = replace_to_end(live, DC_START, DC_NEW)
live = replace_to_end(live, RES_START, RES_NEW)
live = replace_to_end(live, OVR_START, OVR_NEW)
write(LIVE, live)
print("ok")
