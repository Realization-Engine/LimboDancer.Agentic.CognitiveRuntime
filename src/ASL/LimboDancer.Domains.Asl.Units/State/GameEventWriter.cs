using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Writes a game record in the form <see cref="GameEventReader"/> reads (State Model Design, section 7): two-space
/// indentation, LF line endings, and fields in a fixed order, so reading and writing a record round-trips its events.
/// </summary>
public static class GameEventWriter
{
    private static readonly JsonWriterOptions Options = new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Write(GameScope scope, GameRecord record)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(record);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Options))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", GameEventReader.SchemaVersion);
            writer.WriteString("tenant", scope.Tenant.ToString("D"));
            writer.WriteString("game", scope.Game);
            writer.WriteString("label", record.Label);
            writer.WriteBoolean("synthetic", record.Synthetic);
            writer.WriteStartArray("events");
            foreach (var item in record.Events)
            {
                WriteEvent(writer, item);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    private static void WriteEvent(Utf8JsonWriter writer, GameEvent item)
    {
        writer.WriteStartObject();
        writer.WriteString("eventId", item.EventId);
        writer.WriteNumber("revision", item.Revision);
        writer.WriteString("time", item.Time.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
        writer.WriteString("source", item.Source);
        writer.WriteString("type", item.Type);
        if (item.RulePackage is not null)
        {
            writer.WriteString("rulePackage", item.RulePackage);
        }

        if (item.Causes.Count > 0)
        {
            Strings(writer, "causes", item.Causes);
        }

        if (item.Visibility is null)
        {
            writer.WriteString("visibility", "all");
        }
        else
        {
            Strings(writer, "visibility", item.Visibility);
        }

        writer.WriteStartObject("payload");
        WritePayload(writer, item.Payload);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WritePayload(Utf8JsonWriter writer, EventPayload payload)
    {
        switch (payload)
        {
            case GameStarted started:
                writer.WriteStartArray("sides");
                foreach (var side in started.Sides)
                {
                    writer.WriteStartObject();
                    writer.WriteString("id", side.Id);
                    writer.WriteString("nationality", side.Nationality);
                    if (side.Elr is { } elr)
                    {
                        writer.WriteNumber("elr", elr);
                    }

                    if (side.San is { } san)
                    {
                        writer.WriteNumber("san", san);
                    }

                    if (side.FriendlyEdge is { } edge)
                    {
                        writer.WriteString("friendlyEdge", edge);
                    }

                    if (side.Nation is { } nation)
                    {
                        writer.WriteString("nation", nation);
                    }

                    if (side.Groups.Count > 0)
                    {
                        writer.WriteStartArray("groups");
                        foreach (var group in side.Groups)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("id", group.Id);
                            writer.WriteString("name", group.Name);
                            if (group.Elr is { } groupElr)
                            {
                                writer.WriteNumber("elr", groupElr);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartObject("map");
                writer.WriteString("reference", started.Map.Reference);
                writer.WriteString("version", started.Map.Version);
                writer.WriteStartArray("boards");
                foreach (var board in started.Map.Boards)
                {
                    writer.WriteStartObject();
                    writer.WriteString("board", board.Board.Value);
                    writer.WriteString("version", board.Version);
                    if (board.Slot is { } slot)
                    {
                        writer.WriteNumber("column", slot.Column);
                        writer.WriteNumber("row", slot.Row);
                        if (slot.Reversed)
                        {
                            writer.WriteBoolean("reversed", true);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteString("catalog", started.Catalog);
                writer.WriteNumber("turn", started.Turn);
                writer.WriteString("phase", started.Phase);
                writer.WriteString("phasingSide", started.PhasingSide);
                writer.WriteBoolean("synthetic", started.Synthetic);
                if (started.SpecialRules.Count > 0)
                {
                    Strings(writer, "specialRules", started.SpecialRules);
                }

                if (started.ScenarioMonth is { } month)
                {
                    writer.WriteNumber("scenarioMonth", month);
                }

                if (started.ScenarioYear is { } year)
                {
                    writer.WriteNumber("scenarioYear", year);
                }

                if (started.ScenarioDefender is { } defender)
                {
                    writer.WriteString("scenarioDefender", defender);
                }

                if (started.Scenario is { } scenario)
                {
                    writer.WriteStartObject("scenario");
                    writer.WriteString("id", scenario.Id);
                    writer.WriteString("sha256", scenario.Sha256);
                    writer.WriteString("title", scenario.Title);
                    if (scenario.Balance is { } balance)
                    {
                        writer.WriteString("balance", balance);
                    }

                    if (scenario.Players.Count > 0)
                    {
                        writer.WriteStartArray("players");
                        foreach (var player in scenario.Players)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("name", player.Name);
                            writer.WriteString("side", player.Side);
                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                break;
            case GameEnded ended:
                writer.WriteNumber("turn", ended.Turn);
                writer.WriteString("reason", ended.Reason);
                if (ended.Result is { } result)
                {
                    writer.WriteStartObject("result");
                    if (result.Winner is { } winner)
                    {
                        writer.WriteString("winner", winner);
                    }

                    writer.WriteString("reason", result.Reason);
                    Strings(writer, "facts", result.Facts);
                    writer.WriteEndObject();
                }

                break;
            case PhaseChanged phase:
                writer.WriteNumber("turn", phase.Turn);
                writer.WriteString("phase", phase.Phase);
                writer.WriteString("phasingSide", phase.PhasingSide);

                // A11.71 (pass 70, ruling R70.31): written on a CCPh's ending record since the pass, even when empty.
                if (phase.PrcHeldInMelee is { } held)
                {
                    Strings(writer, "prcHeldInMelee", held);
                }

                // A11.71 (ruling R70.31; the reviews of pass 70): the projector reads the PRC held from the vehicles, written only when true.
                if (phase.PrcMelee == true)
                {
                    writer.WriteBoolean("prcMelee", true);
                }

                break;
            case InstanceCreated created:
                writer.WritePropertyName("instance");
                WriteNew(writer, created.Instance);
                if (created.Creator is { } creator)
                {
                    writer.WriteString("creator", creator);
                }

                break;
            case InstanceMoved moved:
                writer.WriteString("id", moved.Id);
                WritePosition(writer, "position", moved.Position);
                if (moved.Mf is { } mf)
                {
                    writer.WriteNumber("mf", mf);
                }

                // D5.5, D5.6, D6.9 (pass 65): written only by a placement beneath a vehicle since the pass.
                if (moved.Ended)
                {
                    writer.WriteBoolean("ended", true);
                }

                break;
            case EquipmentTransferred transferred:
                writer.WriteString("id", transferred.Id);
                WriteHolding(writer, transferred.Holding);
                if (transferred.Position is { } left)
                {
                    WritePosition(writer, "position", left);
                }

                break;
            case ConditionsChanged changed:
                writer.WriteString("id", changed.Id);
                WriteConditions(writer, changed.Conditions);
                break;
            case SetupConcealed concealed:
                writer.WriteString("id", concealed.Id);
                break;
            case LineageRecorded lineage:
                writer.WriteString("action", lineage.Action.ToString().ToLowerInvariant());
                Strings(writer, "consumed", lineage.Consumed);
                writer.WriteStartArray("produced");
                foreach (var produced in lineage.Produced)
                {
                    WriteNew(writer, produced);
                }

                writer.WriteEndArray();
                break;
            case InstanceEliminated eliminated:
                writer.WriteString("id", eliminated.Id);
                break;
            case EntryAttempted attempted:
                writer.WriteString("id", attempted.Id);
                writer.WriteString("target", attempted.Target.ToString());
                writer.WriteNumber("mf", attempted.Mf);
                break;
            case EntryForcedBack forced:
                writer.WriteString("id", forced.Id);
                writer.WriteString("attempt", forced.Attempt);
                writer.WriteString("returnedTo", forced.ReturnedTo.ToString());
                writer.WriteNumber("mf", forced.Mf);
                writer.WriteBoolean("followOnFireResolved", forced.FollowOnFireResolved);
                break;
            case DiceRolled rolled:
                writer.WriteString("roll", rolled.Roll);
                writer.WriteString("purpose", rolled.Purpose);
                writer.WriteNumber("count", rolled.Count);
                writer.WriteNumber("sides", rolled.Sides);
                writer.WriteStartArray("values");
                foreach (var value in rolled.Values)
                {
                    writer.WriteNumberValue(value);
                }

                writer.WriteEndArray();
                writer.WriteString("source", rolled.Source);
                writer.WriteString("actor", rolled.Actor);
                break;
            case RandomSelection selection:
                writer.WriteString("roll", selection.Roll);
                writer.WriteString("attempt", selection.Attempt);
                Strings(writer, "subjects", selection.Subjects);
                break;
            case OverrunDeclared declared:
                writer.WriteString("id", declared.Id);
                writer.WriteString("attempt", declared.Attempt);
                writer.WriteString("choice", declared.Choice);
                break;
            case TaskCheck check:
                writer.WriteString("id", check.Id);
                writer.WriteString("roll", check.Roll);
                writer.WriteString("purpose", check.Purpose);
                writer.WriteNumber("moraleLevel", check.MoraleLevel);
                writer.WriteStartArray("modifiers");
                foreach (var modifier in check.Modifiers)
                {
                    writer.WriteStartObject();
                    writer.WriteString("source", modifier.Source);
                    writer.WriteNumber("value", modifier.Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteNumber("finalDr", check.FinalDr);
                writer.WriteBoolean("passed", check.Passed);
                break;
            case FireResolved fire:
                Strings(writer, "firers", fire.Firers);
                if (fire.Director is { } director)
                {
                    writer.WriteString("director", director);
                }

                writer.WriteString("firerLocation", fire.FirerLocation);
                writer.WriteString("targetLocation", fire.TargetLocation);
                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in fire.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                fire.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                fire.Resolution.WriteTo(writer);
                if (fire.MovementStep is { } movementStep)
                {
                    writer.WriteNumber("movementStep", movementStep);
                }

                if (fire.TurretFacing is { } turretFacing)
                {
                    writer.WriteString("turretFacing", Documents.UnitFacings.Name(turretFacing));
                }

                break;
            case RallyAttempted rally:
                writer.WriteString("unit", rally.Unit);
                if (rally.Leader is { } rallyLeader)
                {
                    writer.WriteString("leader", rallyLeader);
                }

                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in rally.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                rally.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                rally.Resolution.WriteTo(writer);
                break;
            case ResidualFirePlaced residual:
                writer.WriteString("fire", residual.Fire);
                writer.WriteString("location", residual.Location.ToString());
                writer.WriteNumber("fp", residual.Fp);
                break;
            case RepairAttempted repair:
                writer.WriteString("unit", repair.Unit);
                writer.WriteString("equipment", repair.Equipment);
                writer.WriteString("roll", repair.Roll);
                writer.WriteNumber("repairNumber", repair.RepairNumber);
                writer.WriteString("result", repair.Result);
                if (repair.Weapon is { } repairedWeapon)
                {
                    writer.WriteString("weapon", repairedWeapon);
                }
                break;
            case VehicleCloseCombatResolved vehicleCombat:
                writer.WriteString("location", vehicleCombat.Location.ToString());
                writer.WriteString("vehicle", vehicleCombat.Vehicle);
                Strings(writer, "attackers", vehicleCombat.Attackers);
                Strings(writer, "defenders", vehicleCombat.Defenders);
                writer.WriteBoolean("byVehicle", vehicleCombat.ByVehicle);
                writer.WriteBoolean("reaction", vehicleCombat.Reaction);
                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in vehicleCombat.Rolls.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                vehicleCombat.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                vehicleCombat.Resolution.WriteTo(writer);
                if (vehicleCombat.Next is { } nextSide)
                {
                    writer.WriteString("next", nextSide);
                }

                writer.WriteBoolean("closed", vehicleCombat.Closed);
                if (vehicleCombat.CancelsFireLanes)
                {
                    writer.WriteBoolean("cancelsFireLanes", true);
                }

                if (vehicleCombat.CreatedLeader is { } createdLeader)
                {
                    writer.WriteString("createdLeader", createdLeader);
                }

                break;
            case VehicleCloseCombatPassed passed:
                writer.WriteString("location", passed.Location.ToString());
                writer.WriteString("side", passed.Side);
                if (passed.Next is { } passedNext)
                {
                    writer.WriteString("next", passedNext);
                }

                writer.WriteBoolean("closed", passed.Closed);
                break;
            case ShockRecoveryRolled shock:
                writer.WriteString("vehicle", shock.Vehicle);
                writer.WriteString("roll", shock.Roll);
                writer.WriteString("result", shock.Result);
                break;
            case MovementStepped moving:
                Strings(writer, "movers", moving.Movers);
                writer.WriteString("to", moving.To.ToString());
                writer.WriteNumber("halfMf", moving.HalfMf);
                if (moving.Assault)
                {
                    writer.WriteBoolean("assault", true);
                }

                writer.WriteNumber("step", moving.Step);
                if (moving.Charge is { } charge)
                {
                    writer.WriteString("charge", charge.ToString());
                }

                if (moving.DoubleTime)
                {
                    writer.WriteBoolean("doubleTime", true);
                }

                if (moving.Exit is { } exitEdge)
                {
                    writer.WriteString("exit", exitEdge);
                }

                if (moving.Smoke is { } smoke)
                {
                    writer.WriteString("smokeBy", smoke.Unit);
                    writer.WriteString("smokeAt", smoke.Target.ToString());
                    writer.WriteString("smokeRoll", smoke.Roll);
                    writer.WriteNumber("smokeDr", smoke.Dr);
                    writer.WriteNumber("smokeExponent", smoke.Exponent);
                    if (smoke.Cx)
                    {
                        writer.WriteBoolean("smokeCx", true);
                    }
                }

                if (moving.DcPlacement is { } placement)
                {
                    writer.WriteString("dcBy", placement.Unit);
                    writer.WriteString("dc", placement.Charge);
                    writer.WriteString("dcAt", placement.Target.ToString());
                    if (placement.Cx)
                    {
                        writer.WriteBoolean("dcCx", true);
                    }

                    if (placement.TargetsConcealed)
                    {
                        writer.WriteBoolean("dcConcealed", true);
                    }

                    // A23.5 (pass 60): the AFVs' Target Facings at the Placement; none in a record made before the pass.
                    if (placement.Facings is { Count: > 0 } facings)
                    {
                        writer.WriteStartArray("dcFacings");
                        foreach (var dcFacing in facings)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("vehicle", dcFacing.Vehicle);
                            writer.WriteString("hull", dcFacing.Hull);
                            if (dcFacing.Turret is { } turret)
                            {
                                writer.WriteString("turret", turret);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }
                }

                if (moving.PushedGun is { } pushedGun)
                {
                    writer.WriteString("pushedGun", pushedGun);
                }

                if (moving.Road)
                {
                    writer.WriteBoolean("road", true);
                }

                if (moving.RoadRate)
                {
                    writer.WriteBoolean("roadRate", true);
                }

                if (moving.Dash is { } dashTo)
                {
                    writer.WriteString("dash", dashTo.ToString());
                }

                if (moving.IpcTo is { } ipcTo)
                {
                    writer.WriteString("ipcTo", ipcTo);
                }

                if (moving.InfantryOverrun)
                {
                    writer.WriteBoolean("infantryOverrun", true);
                }

                if (moving.MinimumMove)
                {
                    writer.WriteBoolean("minimumMove", true);
                }

                if (moving.Attempted is { } attemptedAt)
                {
                    writer.WriteString("attempted", attemptedAt.ToString());
                }

                if (moving.Bypass is { } bypass)
                {
                    writer.WriteStartArray("bypass");
                    foreach (var side in bypass)
                    {
                        writer.WriteNumberValue((int)side);
                    }

                    writer.WriteEndArray();
                }

                // A17.2 (pass 71, ruling R71.16): the wounded men carried with the step; none in a step recorded before the pass.
                if (moving.Carried is { Count: > 0 } carriedMen)
                {
                    writer.WriteStartArray("carried");
                    foreach (var carry in carriedMen)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("man", carry.Man);
                        writer.WriteString("carrier", carry.Carrier);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                break;
            case BoreSighted sighted:
                writer.WriteString("gun", sighted.Gun);
                writer.WriteString("location", sighted.Location.ToString());
                writer.WriteString("crew", sighted.Crew);
                writer.WriteString("setupLocation", sighted.SetupLocation.ToString());
                break;
            case GunTurned turned:
                writer.WriteString("gun", turned.Gun);
                writer.WriteString("facing", Documents.UnitFacings.Name(turned.Facing));
                break;
            case ManhandlingRolled manhandling:
                writer.WriteString("gun", manhandling.Gun);
                writer.WriteString("roll", manhandling.Roll);
                writer.WriteNumber("drm", manhandling.Drm);
                writer.WriteNumber("manhandling", manhandling.Manhandling);
                writer.WriteString("result", manhandling.Result);
                break;
            case GunHooked hooked:
                writer.WriteString("vehicle", hooked.Vehicle);
                writer.WriteString("gun", hooked.Gun);
                writer.WriteString("crew", hooked.Crew);
                writer.WriteBoolean("hooked", hooked.Hooked);
                writer.WriteNumber("mp", hooked.Mp);
                if (hooked.Facing is { } unhooked)
                {
                    writer.WriteString("facing", Documents.UnitFacings.Name(unhooked));
                }

                if (hooked.Boards)
                {
                    writer.WriteBoolean("boards", true);
                }

                break;
            case VehicleStepped vehicle:
                writer.WriteString("vehicle", vehicle.Vehicle);
                writer.WriteString("kind", vehicle.Kind);
                writer.WriteString("at", vehicle.At.ToString());
                if (vehicle.Facing is { } vehicleFacing)
                {
                    writer.WriteString("facing", Documents.UnitFacings.Name(vehicleFacing));
                }

                writer.WriteNumber("halfMp", vehicle.HalfMp);
                writer.WriteNumber("step", vehicle.Step);
                if (vehicle.Turret is { } vehicleTurret)
                {
                    writer.WriteString("turret", Documents.UnitFacings.Name(vehicleTurret));
                }

                if (vehicle.Reverse)
                {
                    writer.WriteBoolean("reverse", true);
                }

                if (vehicle.Straddling is { } straddling)
                {
                    writer.WriteString("straddling", straddling.ToString());
                }

                if (vehicle.Overrunning)
                {
                    writer.WriteBoolean("overrun", true);
                }

                if (vehicle.MinimumMove)
                {
                    writer.WriteBoolean("minimumMove", true);
                }

                if (vehicle.BogRemoval)
                {
                    writer.WriteBoolean("bogRemoval", true);
                }

                if (vehicle.All)
                {
                    writer.WriteBoolean("all", true);
                }

                if (vehicle.Entry is { } entryEdge)
                {
                    writer.WriteString("entry", entryEdge);
                }

                if (vehicle.Edge is { } vehicleExitEdge)
                {
                    writer.WriteString("edge", vehicleExitEdge);
                }

                if (vehicle.UnitMf != 0)
                {
                    writer.WriteNumber("unitMf", vehicle.UnitMf);
                }

                // D6.5 (pass 65, ruling R65.15): written only by an unload since the pass.
                if (vehicle.Interim)
                {
                    writer.WriteBoolean("interim", true);
                }

                // D6.2 (pass 65, ruling R65.16): written only by a load of Riders since the pass.
                if (vehicle.Riders)
                {
                    writer.WriteBoolean("riders", true);
                }

                if (vehicle.Units is { } aboard)
                {
                    writer.WriteStartArray("units");
                    foreach (var unit in aboard)
                    {
                        writer.WriteStringValue(unit);
                    }

                    writer.WriteEndArray();
                }

                break;
            case RiderBailedOut bailed:
                writer.WriteString("unit", bailed.Unit);
                writer.WriteString("vehicle", bailed.Vehicle);
                writer.WriteString("at", bailed.At.ToString());
                writer.WriteString("cause", bailed.Cause);
                writer.WriteString("roll", bailed.Roll);
                writer.WriteNumber("morale", bailed.Morale);
                writer.WriteString("result", bailed.Result);
                if (bailed.Weapons is { } carried)
                {
                    writer.WriteStartArray("weapons");
                    foreach (var weapon in carried)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("weapon", weapon.Weapon);
                        if (weapon.Roll is { } weaponRoll)
                        {
                            writer.WriteString("roll", weaponRoll);
                        }

                        writer.WriteString("fate", weapon.Fate);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                if (bailed.Drm is { } bailDrm)
                {
                    writer.WriteNumber("drm", bailDrm);
                }

                if (bailed.Elr is { } bailElr)
                {
                    writer.WriteNumber("elr", bailElr);
                }

                break;
            case MoraleCheckResolved check:
                Strings(writer, "units", check.Units);
                writer.WriteString("location", check.Location);
                writer.WriteString("cause", check.Cause);
                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in check.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                check.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                check.Resolution.WriteTo(writer);
                break;
            case VehicleAbandoned abandoned:
                writer.WriteString("vehicle", abandoned.Vehicle);
                writer.WriteString("at", abandoned.At.ToString());
                writer.WriteString("crew", abandoned.Crew);
                WriteOptional(writer, "destroyed", abandoned.Destroyed);
                WriteOptional(writer, "removed", abandoned.Removed);
                WriteOptional(writer, "mount", abandoned.Mount);
                WriteOptional(writer, "fireCounter", abandoned.FireCounter);
                break;
            case VehicleRecrewed recrewed:
                writer.WriteString("vehicle", recrewed.Vehicle);
                writer.WriteString("at", recrewed.At.ToString());
                writer.WriteString("unit", recrewed.Unit);
                if (recrewed.Temporary)
                {
                    writer.WriteBoolean("temporary", true);
                }

                WriteOptional(writer, "returned", recrewed.Returned);
                WriteOptional(writer, "mount", recrewed.Mount);
                WriteOptional(writer, "manning", recrewed.Manning);
                if (recrewed.Captured)
                {
                    writer.WriteBoolean("captured", true);
                }

                break;
            case VehicleCaptured captured:
                writer.WriteString("vehicle", captured.Vehicle);
                writer.WriteString("at", captured.At.ToString());
                writer.WriteString("side", captured.Side);
                writer.WriteString("cause", captured.Cause);
                WriteOptional(writer, "driver", captured.Driver);
                break;
            case VehicleScrounged scrounged:
                writer.WriteString("vehicle", scrounged.Vehicle);
                writer.WriteString("at", scrounged.At.ToString());
                writer.WriteString("unit", scrounged.Unit);
                writer.WriteString("roll", scrounged.Roll);
                writer.WriteNumber("dr", scrounged.Dr);
                WriteOptional(writer, "selection", scrounged.Selection);
                if (scrounged.Taken is { } taken)
                {
                    writer.WriteStartArray("taken");
                    foreach (var weapon in taken)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("weapon", weapon.Weapon);
                        writer.WriteString("mount", weapon.Mount);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                if (scrounged.Mounts is { } mounts)
                {
                    writer.WriteStartArray("mounts");
                    foreach (var mount in mounts)
                    {
                        writer.WriteStringValue(mount);
                    }

                    writer.WriteEndArray();
                }

                break;
            case VehicleCheckRolled check:
                writer.WriteString("vehicle", check.Vehicle);
                writer.WriteString("check", check.Check);
                writer.WriteString("roll", check.Roll);
                writer.WriteNumber("drm", check.Drm);
                writer.WriteString("result", check.Result);
                if (check.Mp != 0)
                {
                    writer.WriteNumber("mp", check.Mp);
                }

                if (check.ArmorLeadership is { } checkLeadership)
                {
                    writer.WriteNumber("armorLeadership", checkLeadership);
                }

                break;
            case MotionAttempted motion:
                writer.WriteString("vehicle", motion.Vehicle);
                writer.WriteString("roll", motion.Roll);
                writer.WriteNumber("halfPoints", motion.HalfPoints);
                writer.WriteString("result", motion.Result);
                if (motion.Mover is { } motionMover)
                {
                    writer.WriteString("mover", motionMover);
                }

                if (motion.Facing is { } motionFacing)
                {
                    writer.WriteString("facing", Documents.UnitFacings.Name(motionFacing));
                }

                if (motion.Turret is { } motionTurret)
                {
                    writer.WriteString("turret", Documents.UnitFacings.Name(motionTurret));
                }

                break;
            case OverrunResolved overrun:
                writer.WriteString("vehicle", overrun.Vehicle);
                writer.WriteString("at", overrun.At.ToString());
                writer.WriteString("fire", overrun.Fire);
                break;
            case RoutStepped routed:
                writer.WriteString("unit", routed.Unit);
                writer.WriteString("to", routed.To.ToString());
                writer.WriteNumber("halfMf", routed.HalfMf);
                writer.WriteBoolean("lowCrawl", routed.LowCrawl);
                if (routed.Attempted is { } repulsedFrom)
                {
                    writer.WriteString("attempted", repulsedFrom.ToString());
                }

                if (routed.With is { } routedWith)
                {
                    writer.WriteString("with", routedWith);
                }

                break;
            case RoutInterdicted interdicted:
                writer.WriteString("unit", interdicted.Unit);
                writer.WriteString("at", interdicted.At.ToString());
                writer.WriteString("roll", interdicted.Roll);
                writer.WriteNumber("morale", interdicted.Morale);
                writer.WriteNumber("drm", interdicted.Drm);
                writer.WriteString("result", interdicted.Result);
                if (interdicted.Interdictor is { } interdictor)
                {
                    writer.WriteString("interdictor", interdictor);
                }

                if (interdicted.Leader is { } interdictedLeader)
                {
                    writer.WriteString("leader", interdictedLeader);
                }

                break;
            case VoluntaryBreak voluntary:
                Strings(writer, "units", voluntary.Units);
                break;
            case UnitsSearched search:
                Strings(writer, "units", search.Units);
                writer.WriteString("location", search.Location.ToString());
                writer.WriteString("roll", search.Roll);
                writer.WriteNumber("drm", search.Drm);
                writer.WriteNumber("accessible", search.Accessible);
                Strings(writer, "searched", [.. search.Searched.Select(location => location.ToString())]);
                if (search.CasualtyDrm is { } searchCasualtyDrm)
                {
                    writer.WriteNumber("casualtyDrm", searchCasualtyDrm);
                }

                break;
            case ConcealmentVerified verified:
                Strings(writer, "units", verified.Units);
                break;
            case DeploymentAttempted deployment:
                writer.WriteString("squad", deployment.Squad);
                if (deployment.Leader is { } deployLeader)
                {
                    writer.WriteString("leader", deployLeader);
                }

                writer.WriteString("roll", deployment.Roll);
                writer.WriteNumber("morale", deployment.Morale);
                writer.WriteNumber("drm", deployment.Drm);
                writer.WriteBoolean("passed", deployment.Passed);
                break;
            case RallyPhaseActionTaken rphAction:
                Strings(writer, "units", rphAction.Units);
                writer.WriteString("action", rphAction.Action);
                break;
            case RecoveryAttempted recovery:
                writer.WriteString("unit", recovery.Unit);
                writer.WriteString("weapon", recovery.Weapon);
                writer.WriteString("roll", recovery.Roll);
                writer.WriteNumber("drm", recovery.Drm);
                writer.WriteBoolean("recovered", recovery.Recovered);
                break;
            case OpportunityFireDeclared opportunity:
                Strings(writer, "units", opportunity.Units);
                break;
            case EncirclementPlaced encirclement:
                writer.WriteString("location", encirclement.Location.ToString());
                writer.WriteString("side", encirclement.Side);
                writer.WriteString("fire", encirclement.Fire);
                break;
            case FireLanePlaced lane:
                writer.WriteString("fire", lane.Fire);
                writer.WriteString("weapon", lane.Weapon);
                writer.WriteString("operator", lane.Operator);
                writer.WriteStartArray("entries");
                foreach (var entry in lane.Entries)
                {
                    writer.WriteStartObject();
                    writer.WriteString("location", entry.Location.ToString());
                    writer.WriteNumber("fp", entry.Fp);
                    writer.WriteNumber("hindrance", entry.HindranceDrm);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                break;
            case PaatcTaken paatc:
                Strings(writer, "units", paatc.Units);
                writer.WriteString("vehicle", paatc.Vehicle);
                writer.WriteString("roll", paatc.Roll);
                writer.WriteNumber("morale", paatc.Morale);
                writer.WriteNumber("drm", paatc.Drm);
                writer.WriteBoolean("passed", paatc.Passed);
                break;
            case InfantryOverrunNtc ntc:
                Strings(writer, "units", ntc.Units);
                writer.WriteString("taker", ntc.Taker);
                writer.WriteString("target", ntc.Target.ToString());
                writer.WriteString("roll", ntc.Roll);
                writer.WriteNumber("morale", ntc.Morale);
                writer.WriteNumber("drm", ntc.Drm);
                writer.WriteBoolean("passed", ntc.Passed);
                break;
            case MovementWindowClosed closed:
                writer.WriteNumber("step", closed.Step);
                break;
            case MovementEnded ended:
                Strings(writer, "movers", ended.Movers);
                break;
            case FireReported report:
                writer.WriteString("fire", report.Fire);
                writer.WriteString("firerLocation", report.FirerLocation);
                writer.WriteString("targetLocation", report.TargetLocation);
                writer.WritePropertyName("arithmetic");
                report.Arithmetic.WriteTo(writer);
                break;
            case AdvanceMoved advanced:
                Strings(writer, "units", advanced.Units);
                writer.WriteString("to", advanced.To.ToString());
                if (advanced.Exit is { } advanceExit)
                {
                    writer.WriteString("exit", advanceExit);
                }

                break;
            case AmbushRolled ambush:
                writer.WriteString("location", ambush.Location.ToString());
                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in ambush.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                if (ambush.Ambusher is { } ambusher)
                {
                    writer.WriteString("ambusher", ambusher);
                }

                writer.WritePropertyName("facts");
                ambush.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                ambush.Resolution.WriteTo(writer);
                break;
            case CloseCombatResolved combat:
                writer.WriteString("location", combat.Location.ToString());
                writer.WriteString("round", combat.Round);
                Strings(writer, "attackers", combat.Attackers);
                Strings(writer, "defenders", combat.Defenders);
                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in combat.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                combat.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                combat.Resolution.WriteTo(writer);

                // A11.31 (pass 70, ruling R70.30): a sequential round names the side next and whether the Location's CC is over; no other round does.
                if (combat.Next is { } sequentialNext)
                {
                    writer.WriteString("next", sequentialNext);
                }

                if (combat.Closed)
                {
                    writer.WriteBoolean("closed", true);
                }

                // A11.52 (pass 70, ruling R70.34): the attackers that tried to capture a vehicle's Passengers.
                if (combat.Captors is { } captors)
                {
                    Strings(writer, "captors", captors);
                }

                break;
            case OrdnanceFired ordnance:
                writer.WriteString("gun", ordnance.Gun);
                writer.WriteString("crew", ordnance.Crew);
                writer.WriteString("target", ordnance.Target.ToString());
                if (ordnance.Facing is { } facing)
                {
                    writer.WriteString("facing", Documents.UnitFacings.Name(facing));
                }

                writer.WriteBoolean("rateOfFireKept", ordnance.RateOfFireKept);
                writer.WriteNumber("acquisition", ordnance.Acquisition);
                if (ordnance.Acquired is { } acquiredAt)
                {
                    writer.WriteString("acquired", acquiredAt.ToString());
                }

                writer.WriteStartObject("rolls");
                foreach (var (key, roll) in ordnance.Rolls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.WriteString(key, roll);
                }

                writer.WriteEndObject();
                writer.WritePropertyName("facts");
                ordnance.Facts.WriteTo(writer);
                writer.WritePropertyName("resolution");
                ordnance.Resolution.WriteTo(writer);
                break;
            case SurrenderPending surrender:
                writer.WriteString("unit", surrender.Unit);
                Strings(writer, "captors", surrender.Captors);
                break;
            case InstanceCaptured captured:
                writer.WriteString("id", captured.Id);
                writer.WriteString("custodian", captured.Custodian);
                break;
            case ChoicePending pending:
                writer.WriteString("key", pending.Key);
                writer.WriteString("kind", pending.Kind);
                writer.WriteString("side", pending.Side);
                Strings(writer, "options", pending.Options);
                writer.WritePropertyName("resume");
                pending.Resume.WriteTo(writer);
                break;
            case ChoiceMade made:
                writer.WriteString("key", made.Key);
                writer.WriteString("option", made.Option);
                break;
            case GunDuelVoided voided:
                writer.WriteString("firer", voided.Firer);
                writer.WriteString("vehicle", voided.Vehicle);
                writer.WriteString("cause", voided.Cause);
                if (voided.Reason is { } notFired)
                {
                    writer.WriteString("reason", notFired);
                }

                break;
            case VehicleWrecked wreck:
                writer.WriteString("id", wreck.Id);
                if (wreck.Burning)
                {
                    writer.WriteBoolean("burning", true);
                }

                break;
            case SurrenderRejected rejected:
                writer.WriteString("unit", rejected.Unit);
                break;
            case PrisonerFreed freed:
                writer.WriteString("unit", freed.Unit);
                break;
            case WindChanged wind:
                writer.WriteString("roll", wind.Roll);
                if (wind.Nvr is { } nvr)
                {
                    writer.WriteNumber("nvr", nvr);
                }

                if (wind.Precipitation is { } precipitation)
                {
                    writer.WriteString("precipitation", precipitation);
                }

                if (wind.Gust)
                {
                    writer.WriteBoolean("gust", true);
                }

                if (wind.NvrRoll is { } nvrRoll)
                {
                    writer.WriteString("nvrRoll", nvrRoll);
                }

                break;
            case StarshellFired starshell:
                writer.WriteString("unit", starshell.Unit);
                writer.WriteString("from", starshell.From.ToString());
                writer.WriteString("method", starshell.Method);
                writer.WriteString("usageRoll", starshell.UsageRoll);
                if (starshell.Passed)
                {
                    writer.WriteBoolean("passed", true);
                }

                if (starshell.PlacementRoll is { } placementRoll)
                {
                    writer.WriteString("placementRoll", placementRoll);
                }

                if (starshell.At is { } at)
                {
                    writer.WriteString("at", at.ToString());
                }

                if (starshell.Starshell is { } starshellId)
                {
                    writer.WriteString("starshell", starshellId);
                }

                break;
            case SniperAttacked sniper:
                writer.WriteString("sniper", sniper.Sniper);
                writer.WriteString("trigger", sniper.Trigger);
                writer.WriteString("roll", sniper.Roll);
                writer.WriteNumber("dr", sniper.Dr);
                if (sniper.Target is { } sniped)
                {
                    writer.WriteString("target", sniped.ToString());
                }

                if (sniper.Unit is { } snipedUnit)
                {
                    writer.WriteString("unit", snipedUnit);
                }

                writer.WriteString("result", sniper.Result);
                break;
            case SanReduced reduced:
                writer.WriteString("side", reduced.Side);
                writer.WriteString("cause", reduced.Cause);
                break;
            case SniperChecked check:
                writer.WriteString("sniper", check.Sniper);
                writer.WriteString("location", check.Location.ToString());
                Strings(writer, "units", check.Units);
                writer.WriteString("roll", check.Roll);
                writer.WriteNumber("drm", check.Drm);
                writer.WriteString("result", check.Result);
                if (check.DrmCauses is { Count: > 0 } causes)
                {
                    Strings(writer, "drmCauses", causes);
                }

                break;
            case PrisonersMassacred massacre:
                Strings(writer, "units", massacre.Units);
                Strings(writer, "prisoners", massacre.Prisoners);
                if (massacre.Berserk)
                {
                    writer.WriteBoolean("berserk", true);
                }

                break;
            case BuildingMoppedUp mopped:
                writer.WriteString("building", mopped.Building);
                writer.WriteString("side", mopped.Side);
                Strings(writer, "units", mopped.Units);
                if (mopped.Secured is { } secured)
                {
                    Strings(writer, "secured", [.. secured.Select(location => location.ToString())]);
                }

                if (mopped.CasualtyDrm is { } mopCasualtyDrm)
                {
                    writer.WriteNumber("casualtyDrm", mopCasualtyDrm);
                }

                break;
            case AcquisitionChanged acquisition:
                writer.WriteString("gun", acquisition.Gun);
                writer.WriteString("location", acquisition.Location.ToString());
                Strings(writer, "units", acquisition.Units);
                break;
            default:
                throw new ArgumentException($"There is no writer for {payload.GetType().Name}.", nameof(payload));
        }
    }

    private static void WriteNew(Utf8JsonWriter writer, NewInstance instance)
    {
        writer.WriteStartObject();
        writer.WriteString("id", instance.Id);
        writer.WriteString("kind", instance.Kind);
        if (instance.Definition is not null)
        {
            writer.WriteString("definition", instance.Definition);
        }

        if (instance.Side is not null)
        {
            writer.WriteString("side", instance.Side);
        }

        if (instance.Group is { } group)
        {
            writer.WriteString("group", group);
        }

        if (instance.Position is { } position)
        {
            WritePosition(writer, "position", position);
        }

        WriteHolding(writer, instance.Holding);
        WriteConditions(writer, instance.Conditions);

        // Pass 55 (ruling R55.15): a vehicle's Armor Leader; whether he is revealed is the state's, never the record's.
        if (instance.ArmorLeader is { } armorLeader)
        {
            writer.WriteStartObject("armorLeader");
            writer.WriteNumber("morale", armorLeader.Morale);
            writer.WriteNumber("leadership", armorLeader.Leadership);
            writer.WriteEndObject();
        }

        // Pass 70 (ruling R70.28): the Carrier a Carrier HS left.
        if (instance.CrewOf is { } crewOf)
        {
            writer.WriteString("crewOf", crewOf);
        }

        writer.WriteEndObject();
    }

    private static void WritePosition(Utf8JsonWriter writer, string name, Position position)
    {
        writer.WriteStartObject(name);
        switch (position)
        {
            case MapPosition map:
                writer.WriteString("at", map.Location.ToString());
                if (map.OnBridge)
                {
                    writer.WriteBoolean("bridge", true);
                }

                if (map.Facing is { } facing)
                {
                    writer.WriteString("facing", Documents.UnitFacings.Name(facing));
                }

                break;
            case ContainedPosition contained:
                writer.WriteString("in", contained.Container);
                writer.WriteString("role", contained.Role switch
                {
                    ContainmentRole.Passenger => "passenger",
                    ContainmentRole.Rider => "rider",
                    ContainmentRole.Crew => "crew",
                    _ => "in-fortification",
                });
                break;
            case OffMapPosition offMap:
                writer.WriteBoolean("offMap", true);
                if (offMap.Entry is { } entry)
                {
                    writer.WriteString("entry", entry);
                }

                break;
            default:
                writer.WriteBoolean("notEntered", true);
                break;
        }

        writer.WriteEndObject();
    }

    /// <summary>A string property written only when it has a value, so a record without it reads as one made before it existed.</summary>
    private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteHolding(Utf8JsonWriter writer, Holding? holding)
    {
        if (holding is null)
        {
            return;
        }

        writer.WriteStartObject("holding");
        writer.WriteString("holder", holding.Holder);
        writer.WriteString("role", holding.Role.ToString().ToLowerInvariant());
        writer.WriteEndObject();
    }

    private static void WriteConditions(Utf8JsonWriter writer, IReadOnlyDictionary<string, ConditionState> conditions)
    {
        if (conditions.Count == 0)
        {
            return;
        }

        writer.WriteStartObject("conditions");
        foreach (var (name, state) in conditions)
        {
            switch (state)
            {
                case ConditionState.True:
                    writer.WriteBoolean(name, true);
                    break;
                case ConditionState.False:
                    writer.WriteBoolean(name, false);
                    break;
                default:
                    writer.WriteString(name, Conditions.Name(state));
                    break;
            }
        }

        writer.WriteEndObject();
    }

    private static void Strings(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }
}
