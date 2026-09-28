using System.Globalization;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>A recorded event stream: a synthetic game fixture until a live source is chosen (ASL-UNIT-050, D2).</summary>
public sealed record GameRecord(string Label, bool Synthetic, IReadOnlyList<GameEvent> Events);

public sealed record GameRecordResult(GameRecord? Record, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);
}

/// <summary>
/// Reads a game record (State Model Design, section 7): the scope, a label, whether it is synthetic, and the ordered
/// events. Reading checks only form; <see cref="GameProjector"/> checks meaning.
/// </summary>
public static class GameEventReader
{
    public const int SchemaVersion = 1;
    private const string Code = "UNIT-STATE-001";

    public static GameRecordResult Read(ReadOnlySpan<byte> json)
    {
        var diagnostics = new List<UnitDiagnostic>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json.ToArray());
        }
        catch (JsonException exception)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"Not valid JSON: line {exception.LineNumber + 1}, position {exception.BytePositionInLine + 1}."));
            return new GameRecordResult(null, diagnostics);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, "A game record must be a JSON object.", "$"));
                return new GameRecordResult(null, diagnostics);
            }

            var fields = new JsonFields(diagnostics, Code);
            if (fields.OptionalInteger(root, "schemaVersion", "$") != SchemaVersion)
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, $"The schema version must be {SchemaVersion}.", "$"));
            }

            var tenantText = fields.RequiredString(root, "tenant", "$");
            var game = fields.RequiredString(root, "game", "$");
            var label = fields.RequiredString(root, "label", "$") ?? string.Empty;
            var synthetic = fields.OptionalBoolean(root, "synthetic", "$");
            if (tenantText is not null && !Guid.TryParse(tenantText, out _))
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, $"The tenant '{tenantText}' is not a GUID.", "$"));
            }

            if (tenantText is null || game is null || !Guid.TryParse(tenantText, out var tenant))
            {
                return new GameRecordResult(null, diagnostics);
            }

            var scope = new GameScope(tenant, game);
            var events = new List<GameEvent>();
            foreach (var (item, path) in fields.Objects(root, "events", "$"))
            {
                if (ReadEvent(item, path, scope, fields, diagnostics) is { } gameEvent)
                {
                    events.Add(gameEvent);
                }
            }

            return diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error)
                ? new GameRecordResult(null, diagnostics)
                : new GameRecordResult(new GameRecord(label, synthetic, events), diagnostics);
        }
    }

    private static GameEvent? ReadEvent(JsonElement item, string path, GameScope scope, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        var id = fields.RequiredString(item, "eventId", path);
        var revision = item.TryGetProperty("revision", out var revisionElement) && revisionElement.TryGetInt64(out var number) ? number : (long?)null;
        var timeText = fields.RequiredString(item, "time", path);
        var source = fields.RequiredString(item, "source", path);
        var type = fields.RequiredString(item, "type", path);
        var rulePackage = fields.OptionalString(item, "rulePackage", path);
        var causes = fields.StringList(item, "causes", path);
        IReadOnlyList<string>? visibility = null;
        if (item.TryGetProperty("visibility", out var visibilityElement) && !(visibilityElement.ValueKind == JsonValueKind.String && visibilityElement.GetString() == "all"))
        {
            visibility = fields.StringList(item, "visibility", path);
        }

        if (revision is null)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, "'revision' must be a whole number.", path));
        }

        DateTimeOffset time = default;
        if (timeText is not null && !DateTimeOffset.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time))
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'{timeText}' is not a time.", path));
        }

        if (!item.TryGetProperty("payload", out var payloadElement) || payloadElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, "'payload' must be an object.", path));
            return null;
        }

        var payload = type is null ? null : ReadPayload(type, payloadElement, path + ".payload", fields, diagnostics);
        return id is null || revision is null || timeText is null || source is null || type is null || payload is null
            ? null
            : new GameEvent(scope, id, revision.Value, time, source, type, payload, rulePackage, causes, visibility);
    }

    private static EventPayload? ReadPayload(string type, JsonElement payload, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        switch (type)
        {
            case "game-started":
                var sides = new List<SideState>();
                foreach (var (side, sidePath) in fields.Objects(payload, "sides", path))
                {
                    var sideId = fields.RequiredString(side, "id", sidePath);
                    var nationality = fields.RequiredString(side, "nationality", sidePath);
                    var edge = fields.OptionalString(side, "friendlyEdge", sidePath);
                    if (edge is not null && !SideState.Edges.Contains(edge, StringComparer.Ordinal))
                    {
                        diagnostics.Add(UnitDiagnostic.Error(Code, $"'{edge}' is not a map edge ({string.Join(", ", SideState.Edges)}).", sidePath));
                    }

                    if (sideId is not null && nationality is not null)
                    {
                        sides.Add(new SideState(sideId, nationality, fields.OptionalInteger(side, "elr", sidePath), fields.OptionalInteger(side, "san", sidePath))
                        {
                            FriendlyEdge = edge,
                        });
                    }
                }

                if (!payload.TryGetProperty("map", out var map) || map.ValueKind != JsonValueKind.Object)
                {
                    diagnostics.Add(UnitDiagnostic.Error(Code, "'map' must be an object.", path));
                    return null;
                }

                var boards = new List<PlacedBoard>();
                foreach (var (board, boardPath) in fields.Objects(map, "boards", path + ".map"))
                {
                    var boardText = fields.RequiredString(board, "board", boardPath);
                    var boardVersion = fields.RequiredString(board, "version", boardPath);
                    var column = fields.OptionalInteger(board, "column", boardPath);
                    var row = fields.OptionalInteger(board, "row", boardPath);
                    var reversed = fields.OptionalBoolean(board, "reversed", boardPath);
                    if (boardText is not null && !BoardRef.TryParse(boardText, out _))
                    {
                        diagnostics.Add(UnitDiagnostic.Error(Code, $"'{boardText}' is not a board reference.", boardPath));
                    }
                    else if ((column is null) != (row is null) || (reversed && column is null))
                    {
                        diagnostics.Add(UnitDiagnostic.Error(Code, "A placed board needs both 'column' and 'row'; 'reversed' needs them too.", boardPath));
                    }
                    else if (boardText is not null && boardVersion is not null)
                    {
                        boards.Add(new PlacedBoard(BoardRef.Parse(boardText), boardVersion,
                            column is { } slotColumn && row is { } slotRow ? new BoardSlot(slotColumn, slotRow, reversed) : null));
                    }
                }

                var reference = fields.RequiredString(map, "reference", path + ".map");
                var mapVersion = fields.RequiredString(map, "version", path + ".map");
                var catalog = fields.RequiredString(payload, "catalog", path);
                var phase = fields.RequiredString(payload, "phase", path);
                var phasing = fields.RequiredString(payload, "phasingSide", path);
                var turn = fields.OptionalInteger(payload, "turn", path);
                return reference is null || mapVersion is null || catalog is null || phase is null || phasing is null || turn is null
                    ? null
                    : new GameStarted(sides, new MapInPlay(reference, mapVersion, boards), catalog, turn.Value, phase, phasing,
                        fields.OptionalBoolean(payload, "synthetic", path))
                    {
                        SpecialRules = fields.StringList(payload, "specialRules", path),
                        ScenarioMonth = Month(fields.OptionalInteger(payload, "scenarioMonth", path), path, diagnostics),
                        ScenarioYear = Year(fields.OptionalInteger(payload, "scenarioYear", path), path, diagnostics),
                        ScenarioDefender = fields.OptionalString(payload, "scenarioDefender", path),
                    };
            case "phase-changed":
                var nextTurn = fields.OptionalInteger(payload, "turn", path);
                var nextPhase = fields.RequiredString(payload, "phase", path);
                var nextPhasing = fields.RequiredString(payload, "phasingSide", path);
                return nextTurn is null || nextPhase is null || nextPhasing is null ? null : new PhaseChanged(nextTurn.Value, nextPhase, nextPhasing);
            case "instance-created":
                return payload.TryGetProperty("instance", out var created) && ReadNew(created, path + ".instance", fields, diagnostics) is { } instance
                    ? new InstanceCreated(instance) { Creator = fields.OptionalString(payload, "creator", path) }
                    : Missing(diagnostics, "'instance' must be an object.", path);
            case "instance-moved":
                var movedId = fields.RequiredString(payload, "id", path);
                var position = ReadPosition(payload, "position", path, fields, diagnostics);
                return movedId is null || position is null
                    ? Missing(diagnostics, "A move names the instance and its position.", path)
                    : new InstanceMoved(movedId, position, fields.OptionalInteger(payload, "mf", path));
            case "equipment-transferred":
                var transferredId = fields.RequiredString(payload, "id", path);
                var holding = ReadHolding(payload, path, fields, diagnostics);
                var left = payload.TryGetProperty("position", out _) ? ReadPosition(payload, "position", path, fields, diagnostics) : null;
                return transferredId is null ? null : new EquipmentTransferred(transferredId, holding, left);
            case "conditions-changed":
            case "crew-exposure-changed":
            case "concealment-lost":
                var changedId = fields.RequiredString(payload, "id", path);
                var conditions = ReadConditions(payload, path, diagnostics);
                return changedId is null ? null : new ConditionsChanged(changedId, conditions);
            case "lineage":
                var action = fields.RequiredString(payload, "action", path) switch
                {
                    "reduced" => LineageAction.Reduced,
                    "deployed" => LineageAction.Deployed,
                    "recombined" => LineageAction.Recombined,
                    "replaced" => LineageAction.Replaced,
                    null => (LineageAction?)null,
                    var other => Invalid<LineageAction>(diagnostics, $"'{other}' is not reduced, deployed, recombined, or replaced.", path),
                };
                var produced = new List<NewInstance>();
                foreach (var (item, itemPath) in fields.Objects(payload, "produced", path))
                {
                    if (ReadNew(item, itemPath, fields, diagnostics) is { } next)
                    {
                        produced.Add(next);
                    }
                }

                return action is null ? null : new LineageRecorded(action.Value, fields.StringList(payload, "consumed", path), produced);
            case "instance-eliminated":
                return fields.RequiredString(payload, "id", path) is { } eliminated ? new InstanceEliminated(eliminated) : null;
            case "entry-attempted":
                var attemptingId = fields.RequiredString(payload, "id", path);
                var attemptTarget = ReadLocation(payload, "target", path, fields, diagnostics);
                var attemptMf = fields.OptionalInteger(payload, "mf", path);
                return attemptingId is null || attemptTarget is null || attemptMf is null
                    ? Missing(diagnostics, "An attempt names the unit, its target location, and its MF.", path)
                    : new EntryAttempted(attemptingId, attemptTarget, attemptMf.Value);
            case "entry-forced-back":
                var forcedId = fields.RequiredString(payload, "id", path);
                var forcedAttempt = fields.RequiredString(payload, "attempt", path);
                var returnedTo = ReadLocation(payload, "returnedTo", path, fields, diagnostics);
                var forcedMf = fields.OptionalInteger(payload, "mf", path);
                return forcedId is null || forcedAttempt is null || returnedTo is null || forcedMf is null
                    ? Missing(diagnostics, "A forced back names the unit, its attempt, the location it returns to, and its MF.", path)
                    : new EntryForcedBack(forcedId, forcedAttempt, returnedTo, forcedMf.Value, fields.OptionalBoolean(payload, "followOnFireResolved", path));
            case "dice-rolled":
                var roll = fields.RequiredString(payload, "roll", path);
                var purpose = fields.RequiredString(payload, "purpose", path);
                var count = fields.OptionalInteger(payload, "count", path);
                var rollSides = fields.OptionalInteger(payload, "sides", path);
                var source = fields.RequiredString(payload, "source", path);
                var actor = fields.RequiredString(payload, "actor", path);
                var values = ReadIntegers(payload, "values", path, diagnostics);
                return roll is null || purpose is null || count is null || rollSides is null || source is null || actor is null || values is null
                    ? Missing(diagnostics, "A roll names its id, purpose, count, sides, values, source, and actor.", path)
                    : new DiceRolled(roll, purpose, count.Value, rollSides.Value, values, source, actor);
            case "random-selection":
                var selectionRoll = fields.RequiredString(payload, "roll", path);
                var selectionAttempt = fields.RequiredString(payload, "attempt", path);
                return selectionRoll is null || selectionAttempt is null
                    ? Missing(diagnostics, "A selection names its roll and its attempt.", path)
                    : new RandomSelection(selectionRoll, selectionAttempt, fields.StringList(payload, "subjects", path));
            case "overrun-declared":
                var declaringId = fields.RequiredString(payload, "id", path);
                var declaredAttempt = fields.RequiredString(payload, "attempt", path);
                var choice = fields.RequiredString(payload, "choice", path);
                return declaringId is null || declaredAttempt is null || choice is null
                    ? Missing(diagnostics, "A declaration names the unit, its attempt, and the choice.", path)
                    : new OverrunDeclared(declaringId, declaredAttempt, choice);
            case "task-check":
                var checkingId = fields.RequiredString(payload, "id", path);
                var checkRoll = fields.RequiredString(payload, "roll", path);
                var checkPurpose = fields.RequiredString(payload, "purpose", path);
                var morale = fields.OptionalInteger(payload, "moraleLevel", path);
                var finalDr = fields.OptionalInteger(payload, "finalDr", path);
                var modifiers = new List<TaskCheckModifier>();
                foreach (var (item, itemPath) in fields.Objects(payload, "modifiers", path))
                {
                    var modifierSource = fields.RequiredString(item, "source", itemPath);
                    var value = fields.OptionalInteger(item, "value", itemPath);
                    if (modifierSource is null || value is null)
                    {
                        return Missing(diagnostics, "A modifier names its source and value.", itemPath);
                    }

                    modifiers.Add(new TaskCheckModifier(modifierSource, value.Value));
                }

                if (!payload.TryGetProperty("passed", out var passed) || passed.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return Missing(diagnostics, "A task check records whether it passed.", path);
                }

                return checkingId is null || checkRoll is null || checkPurpose is null || morale is null || finalDr is null
                    ? Missing(diagnostics, "A task check names the unit, its roll, its purpose, the Morale Level, and the final DR.", path)
                    : new TaskCheck(checkingId, checkRoll, checkPurpose, morale.Value, modifiers, finalDr.Value, passed.GetBoolean());
            case "fire-resolved":
                var firerLocation = fields.RequiredString(payload, "firerLocation", path);
                var targetLocation = fields.RequiredString(payload, "targetLocation", path);
                var fireRolls = new Dictionary<string, string>(StringComparer.Ordinal);
                if (payload.TryGetProperty("rolls", out var rollMap) && rollMap.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in rollMap.EnumerateObject())
                    {
                        if (entry.Value.ValueKind != JsonValueKind.String)
                        {
                            return Missing(diagnostics, "Each fire roll names its roll id.", path + ".rolls");
                        }

                        fireRolls[entry.Name] = entry.Value.GetString()!;
                    }
                }

                var director = fields.OptionalString(payload, "director", path);
                return firerLocation is null || targetLocation is null
                    || !payload.TryGetProperty("facts", out var facts) || facts.ValueKind != JsonValueKind.Object
                    || !payload.TryGetProperty("resolution", out var resolution) || resolution.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "A fire record names its Locations, facts, and resolution.", path)
                    : new FireResolved(fields.StringList(payload, "firers", path), director, firerLocation, targetLocation, fireRolls,
                        facts.Clone(), resolution.Clone())
                    {
                        MovementStep = fields.OptionalInteger(payload, "movementStep", path),
                    };
            case "rally-attempted":
                var rallyUnit = fields.RequiredString(payload, "unit", path);
                var rallyRolls = RollMap(payload, path, diagnostics);
                return rallyUnit is null || rallyRolls is null
                    || !payload.TryGetProperty("facts", out var rallyFacts) || rallyFacts.ValueKind != JsonValueKind.Object
                    || !payload.TryGetProperty("resolution", out var rallyResolution) || rallyResolution.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "A Rally record names its unit, rolls, facts, and resolution.", path)
                    : new RallyAttempted(rallyUnit, fields.OptionalString(payload, "leader", path), rallyRolls, rallyFacts.Clone(), rallyResolution.Clone());
            case "residual-fp-placed":
                var residualFire = fields.RequiredString(payload, "fire", path);
                var residualAt = ReadLocation(payload, "location", path, fields, diagnostics);
                var residualFp = fields.OptionalInteger(payload, "fp", path);
                return residualFire is null || residualAt is null || residualFp is null
                    ? Missing(diagnostics, "Residual FP names its fire record, its Location, and its FP.", path)
                    : new ResidualFirePlaced(residualFire, residualAt, residualFp.Value);
            case "repair-attempted":
                var repairUnit = fields.RequiredString(payload, "unit", path);
                var repaired = fields.RequiredString(payload, "equipment", path);
                var repairRoll = fields.RequiredString(payload, "roll", path);
                var repairNumber = fields.OptionalInteger(payload, "repairNumber", path);
                var repairResult = fields.RequiredString(payload, "result", path);
                return repairUnit is null || repaired is null || repairRoll is null || repairNumber is null || repairResult is null
                    ? Missing(diagnostics, "A Repair record names its unit, SW, roll, Repair Number, and result.", path)
                    : new RepairAttempted(repairUnit, repaired, repairRoll, repairNumber.Value, repairResult);
            case "shock-recovery-rolled":
                var shocked = fields.RequiredString(payload, "vehicle", path);
                var shockRoll = fields.RequiredString(payload, "roll", path);
                var shockResult = fields.RequiredString(payload, "result", path);
                return shocked is null || shockRoll is null || shockResult is null
                    ? Missing(diagnostics, "A Shock recovery record names its vehicle, roll, and result.", path)
                    : new ShockRecoveryRolled(shocked, shockRoll, shockResult);
            case "movement-step":
                var stepTo = ReadLocation(payload, "to", path, fields, diagnostics);
                var halfMf = fields.OptionalInteger(payload, "halfMf", path);
                var step = fields.OptionalInteger(payload, "step", path);
                var charge = payload.TryGetProperty("charge", out _) ? ReadLocation(payload, "charge", path, fields, diagnostics) : null;
                return stepTo is null || halfMf is null || step is null
                    ? Missing(diagnostics, "A movement step names its movers, destination, half MF, and step.", path)
                    : new MovementStepped(fields.StringList(payload, "movers", path), stepTo, halfMf.Value, fields.OptionalBoolean(payload, "assault", path), step.Value)
                    {
                        Charge = charge,
                        DoubleTime = fields.OptionalBoolean(payload, "doubleTime", path),
                        PushedGun = fields.OptionalString(payload, "pushedGun", path),
                    };
            case "bore-sighted":
                var sighter = fields.RequiredString(payload, "gun", path);
                var sighted = ReadLocation(payload, "location", path, fields, diagnostics);
                var sighterCrew = fields.RequiredString(payload, "crew", path);
                var sighterAt = ReadLocation(payload, "setupLocation", path, fields, diagnostics);
                return sighter is null || sighted is null || sighterCrew is null || sighterAt is null
                    ? Missing(diagnostics, "A Bore Sighting names its Gun, Location, crew, and setup Location.", path)
                    : new BoreSighted(sighter, sighted, sighterCrew, sighterAt);
            case "gun-turned":
                var turnedGun = fields.RequiredString(payload, "gun", path);
                return turnedGun is null || !UnitFacings.TryParse(fields.RequiredString(payload, "facing", path), out var gunFacing)
                    ? Missing(diagnostics, "A Gun's CA change names the Gun and its facing.", path)
                    : new GunTurned(turnedGun, gunFacing);
            case "manhandling-rolled":
                var pushed = fields.RequiredString(payload, "gun", path);
                var pushRoll = fields.RequiredString(payload, "roll", path);
                var pushDrm = fields.OptionalInteger(payload, "drm", path);
                var pushNumber = fields.OptionalInteger(payload, "manhandling", path);
                var pushResult = fields.RequiredString(payload, "result", path);
                return pushed is null || pushRoll is null || pushDrm is null || pushNumber is null || pushResult is null
                    ? Missing(diagnostics, "A Manhandling record names its Gun, roll, DRM, M#, and result.", path)
                    : new ManhandlingRolled(pushed, pushRoll, pushDrm.Value, pushNumber.Value, pushResult);
            case "gun-hooked":
                var tower = fields.RequiredString(payload, "vehicle", path);
                var towed = fields.RequiredString(payload, "gun", path);
                var towCrew = fields.RequiredString(payload, "crew", path);
                var towMp = fields.OptionalInteger(payload, "mp", path);
                UnitFacing? unhookedFacing = null;
                if (payload.TryGetProperty("facing", out var towFacing))
                {
                    if (!UnitFacings.TryParse(towFacing.GetString(), out var parsedTow))
                    {
                        return Missing(diagnostics, "A Gun's unhooked facing is a hexspine name.", path);
                    }

                    unhookedFacing = parsedTow;
                }

                return tower is null || towed is null || towCrew is null || towMp is null
                    ? Missing(diagnostics, "A hook-up record names its vehicle, Gun, crew, and MP.", path)
                    : new GunHooked(tower, towed, towCrew, fields.OptionalBoolean(payload, "hooked", path), towMp.Value, unhookedFacing);
            case "vehicle-step":
                var vehicleAt = ReadLocation(payload, "at", path, fields, diagnostics);
                var vehicleId = fields.RequiredString(payload, "vehicle", path);
                var vehicleKind = fields.RequiredString(payload, "kind", path);
                var halfMp = fields.OptionalInteger(payload, "halfMp", path);
                var vehicleStep = fields.OptionalInteger(payload, "step", path);
                UnitFacing? turned = null;
                if (payload.TryGetProperty("facing", out var facingValue))
                {
                    if (!UnitFacings.TryParse(facingValue.GetString(), out var parsed))
                    {
                        return Missing(diagnostics, "A vehicle step's facing is a hexspine name.", path);
                    }

                    turned = parsed;
                }

                return vehicleAt is null || vehicleId is null || vehicleKind is null || halfMp is null || vehicleStep is null
                    ? Missing(diagnostics, "A vehicle step names its vehicle, kind, Location, half MP, and step.", path)
                    : new VehicleStepped(vehicleId, vehicleKind, vehicleAt, turned, halfMp.Value, vehicleStep.Value);
            case "movement-window-closed":
                var closedStep = fields.OptionalInteger(payload, "step", path);
                return closedStep is null ? Missing(diagnostics, "A closed window names its step.", path) : new MovementWindowClosed(closedStep.Value);
            case "movement-ended":
                return new MovementEnded(fields.StringList(payload, "movers", path));
            case "fire-reported":
                var reported = fields.RequiredString(payload, "fire", path);
                var reportedFrom = fields.RequiredString(payload, "firerLocation", path);
                var reportedAt = fields.RequiredString(payload, "targetLocation", path);
                return reported is null || reportedFrom is null || reportedAt is null
                    || !payload.TryGetProperty("arithmetic", out var arithmetic) || arithmetic.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "A fire report names its record, its Locations, and the arithmetic.", path)
                    : new FireReported(reported, reportedFrom, reportedAt, arithmetic.Clone());
            case "advanced":
                var advancedTo = ReadLocation(payload, "to", path, fields, diagnostics);
                return advancedTo is null
                    ? Missing(diagnostics, "An advance names its units and the Location they enter.", path)
                    : new AdvanceMoved(fields.StringList(payload, "units", path), advancedTo);
            case "ambush-rolled":
                var ambushAt = ReadLocation(payload, "location", path, fields, diagnostics);
                var ambushRolls = RollMap(payload, path, diagnostics);
                return ambushAt is null || ambushRolls is null
                    || !payload.TryGetProperty("facts", out var ambushFacts) || ambushFacts.ValueKind != JsonValueKind.Object
                    || !payload.TryGetProperty("resolution", out var ambushResolution) || ambushResolution.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "An Ambush record names its Location, rolls, facts, and resolution.", path)
                    : new AmbushRolled(ambushAt, ambushRolls, fields.OptionalString(payload, "ambusher", path), ambushFacts.Clone(), ambushResolution.Clone());
            case "close-combat-resolved":
                var combatAt = ReadLocation(payload, "location", path, fields, diagnostics);
                var round = fields.RequiredString(payload, "round", path);
                var combatRolls = RollMap(payload, path, diagnostics);
                return combatAt is null || round is null || combatRolls is null
                    || !payload.TryGetProperty("facts", out var combatFacts) || combatFacts.ValueKind != JsonValueKind.Object
                    || !payload.TryGetProperty("resolution", out var combatResolution) || combatResolution.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "A CC record names its Location, round, rolls, facts, and resolution.", path)
                    : new CloseCombatResolved(combatAt, round, fields.StringList(payload, "attackers", path), fields.StringList(payload, "defenders", path), combatRolls,
                        combatFacts.Clone(), combatResolution.Clone());
            case "ordnance-fired":
                var ordnanceGun = fields.RequiredString(payload, "gun", path);
                var ordnanceCrew = fields.RequiredString(payload, "crew", path);
                var ordnanceTarget = ReadLocation(payload, "target", path, fields, diagnostics);
                var ordnanceRolls = RollMap(payload, path, diagnostics);
                UnitFacing? ordnanceFacing = null;
                if (fields.OptionalString(payload, "facing", path) is { } facingName)
                {
                    if (!UnitFacings.TryParse(facingName, out var parsedFacing))
                    {
                        return Missing(diagnostics, $"'{facingName}' is not a hexspine.", path);
                    }

                    ordnanceFacing = parsedFacing;
                }

                BoardLocation? acquired = null;
                if (fields.OptionalString(payload, "acquired", path) is not null)
                {
                    acquired = ReadLocation(payload, "acquired", path, fields, diagnostics);
                    if (acquired is null)
                    {
                        return null;
                    }
                }

                return ordnanceGun is null || ordnanceCrew is null || ordnanceTarget is null || ordnanceRolls is null
                    || !payload.TryGetProperty("rateOfFireKept", out var kept) || kept.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                    || !payload.TryGetProperty("acquisition", out var level) || !level.TryGetInt32(out var acquisition)
                    || !payload.TryGetProperty("facts", out var ordnanceFacts) || ordnanceFacts.ValueKind != JsonValueKind.Object
                    || !payload.TryGetProperty("resolution", out var ordnanceResolution) || ordnanceResolution.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "An ordnance record names its Gun, crew, target, ROF, Acquisition, rolls, facts, and resolution.", path)
                    : new OrdnanceFired(ordnanceGun, ordnanceCrew, ordnanceTarget, ordnanceFacing, kept.GetBoolean(), acquisition, acquired, ordnanceRolls,
                        ordnanceFacts.Clone(), ordnanceResolution.Clone());
            case "surrender-pending":
                var surrendering = fields.RequiredString(payload, "unit", path);
                return surrendering is null
                    ? Missing(diagnostics, "A pending surrender names its unit and its captors.", path)
                    : new SurrenderPending(surrendering, fields.StringList(payload, "captors", path));
            case "instance-captured":
                var captured = fields.RequiredString(payload, "id", path);
                var custodian = fields.RequiredString(payload, "custodian", path);
                return captured is null || custodian is null ? null : new InstanceCaptured(captured, custodian);
            case "choice-pending":
                var pendingKey = fields.RequiredString(payload, "key", path);
                var pendingKind = fields.RequiredString(payload, "kind", path);
                var choosing = fields.RequiredString(payload, "side", path);
                var options = fields.StringList(payload, "options", path);
                return pendingKey is null || pendingKind is null || choosing is null || options.Count == 0
                    || !payload.TryGetProperty("resume", out var resume) || resume.ValueKind != JsonValueKind.Object
                    ? Missing(diagnostics, "A pending choice names its key, kind, side, options, and what the resolution resumes from.", path)
                    : new ChoicePending(pendingKey, pendingKind, choosing, options, resume.Clone());
            case "choice-made":
                var madeKey = fields.RequiredString(payload, "key", path);
                var option = fields.RequiredString(payload, "option", path);
                return madeKey is null || option is null ? Missing(diagnostics, "A choice names its key and the option chosen.", path) : new ChoiceMade(madeKey, option);
            case "vehicle-wrecked":
                return fields.RequiredString(payload, "id", path) is { } wreck ? new VehicleWrecked(wreck, fields.OptionalBoolean(payload, "burning", path)) : null;
            case "surrender-rejected":
                return fields.RequiredString(payload, "unit", path) is { } rejected ? new SurrenderRejected(rejected) : null;
            case "prisoners-massacred":
                return new PrisonersMassacred(fields.StringList(payload, "units", path), fields.StringList(payload, "prisoners", path),
                    fields.OptionalBoolean(payload, "berserk", path));
            case "acquisition-changed":
                var acquiringGun = fields.RequiredString(payload, "gun", path);
                var acquiredAt = ReadLocation(payload, "location", path, fields, diagnostics);
                return acquiringGun is null || acquiredAt is null
                    ? Missing(diagnostics, "An Acquisition names its Gun and its Location.", path)
                    : new AcquisitionChanged(acquiringGun, acquiredAt, fields.StringList(payload, "units", path));
            default:
                diagnostics.Add(UnitDiagnostic.Error(Code, $"'{type}' is not an event type.", path));
                return null;
        }
    }

    private static Dictionary<string, string>? RollMap(JsonElement payload, string path, List<UnitDiagnostic> diagnostics)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (payload.TryGetProperty("rolls", out var rolls) && rolls.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in rolls.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.String)
                {
                    diagnostics.Add(UnitDiagnostic.Error(Code, "Each roll names its roll id.", path + ".rolls"));
                    return null;
                }

                map[entry.Name] = entry.Value.GetString()!;
            }
        }

        return map;
    }

    private static NewInstance? ReadNew(JsonElement item, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = fields.RequiredString(item, "id", path);
        var kind = fields.RequiredString(item, "kind", path);
        var position = item.TryGetProperty("position", out _) ? ReadPosition(item, "position", path, fields, diagnostics) : null;
        return id is null || kind is null
            ? null
            : new NewInstance(id, kind, fields.OptionalString(item, "definition", path), fields.OptionalString(item, "side", path), position,
                ReadHolding(item, path, fields, diagnostics), ReadConditions(item, path, diagnostics));
    }

    private static Holding? ReadHolding(JsonElement item, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (!item.TryGetProperty("holding", out var holding) || holding.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var holder = fields.RequiredString(holding, "holder", path + ".holding");
        var role = fields.RequiredString(holding, "role", path + ".holding") switch
        {
            "possessed" => HoldingRole.Possessed,
            "manned" => HoldingRole.Manned,
            "towed" => HoldingRole.Towed,
            null => (HoldingRole?)null,
            var other => Invalid<HoldingRole>(diagnostics, $"'{other}' is not possessed, manned, or towed.", path),
        };
        return holder is null || role is null ? null : new Holding(holder, role.Value);
    }

    private static List<int>? ReadIntegers(JsonElement item, string name, string path, List<UnitDiagnostic> diagnostics)
    {
        if (!item.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'{name}' must be a list of integers.", path + "." + name));
            return null;
        }

        var values = new List<int>();
        foreach (var value in array.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, $"'{name}' must be a list of integers.", path + "." + name));
                return null;
            }

            values.Add(number);
        }

        return values;
    }

    private static BoardLocation? ReadLocation(JsonElement item, string name, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (fields.RequiredString(item, name, path) is not { } text)
        {
            return null;
        }

        if (!BoardLocation.TryParse(text, out var location))
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'{text}' is not a location such as bd01:E4:0.", path + "." + name));
            return null;
        }

        return location;
    }

    private static Position? ReadPosition(JsonElement item, string name, string path, JsonFields fields, List<UnitDiagnostic> diagnostics)
    {
        if (!item.TryGetProperty(name, out var position) || position.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'{name}' must be an object.", path));
            return null;
        }

        var positionPath = $"{path}.{name}";
        if (fields.OptionalString(position, "at", positionPath) is { } at)
        {
            if (!BoardLocation.TryParse(at, out var location))
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, $"'{at}' is not a board location such as bd01:E4:0.", positionPath));
                return null;
            }

            UnitFacing? facing = null;
            if (fields.OptionalString(position, "facing", positionPath) is { } facingText)
            {
                if (!UnitFacings.TryParse(facingText, out var parsed))
                {
                    diagnostics.Add(UnitDiagnostic.Error(Code, $"'{facingText}' is not a hexspine.", positionPath));
                    return null;
                }

                facing = parsed;
            }

            return new MapPosition(location, fields.OptionalBoolean(position, "bridge", positionPath), facing);
        }

        if (fields.OptionalString(position, "in", positionPath) is { } container)
        {
            var role = fields.RequiredString(position, "role", positionPath) switch
            {
                "passenger" => ContainmentRole.Passenger,
                "rider" => ContainmentRole.Rider,
                "in-fortification" => ContainmentRole.InFortification,
                null => (ContainmentRole?)null,
                var other => Invalid<ContainmentRole>(diagnostics, $"'{other}' is not passenger, rider, or in-fortification.", positionPath),
            };
            return role is null ? null : new ContainedPosition(container, role.Value);
        }

        if (fields.OptionalBoolean(position, "offMap", positionPath))
        {
            return OffMapPosition.Instance;
        }

        if (fields.OptionalBoolean(position, "notEntered", positionPath))
        {
            return NotEnteredPosition.Instance;
        }

        diagnostics.Add(UnitDiagnostic.Error(Code, "A position is at a location, in a container, off map, or not entered.", positionPath));
        return null;
    }

    private static Dictionary<string, ConditionState> ReadConditions(JsonElement item, string path, List<UnitDiagnostic> diagnostics)
    {
        var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
        if (!item.TryGetProperty("conditions", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return conditions;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, "'conditions' must be an object of condition states.", path));
            return conditions;
        }

        foreach (var property in element.EnumerateObject())
        {
            try
            {
                conditions[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.True => ConditionState.True,
                    JsonValueKind.False => ConditionState.False,
                    JsonValueKind.String => Conditions.Parse(property.Value.GetString()!),
                    _ => throw new FormatException("A condition is true, false, or a state name."),
                };
            }
            catch (FormatException exception)
            {
                diagnostics.Add(UnitDiagnostic.Error(Code, exception.Message, $"{path}.conditions.{property.Name}"));
            }
        }

        return conditions;
    }

    private static EventPayload? Missing(List<UnitDiagnostic> diagnostics, string message, string path)
    {
        diagnostics.Add(UnitDiagnostic.Error(Code, message, path));
        return null;
    }

    private static T? Invalid<T>(List<UnitDiagnostic> diagnostics, string message, string path)
        where T : struct
    {
        diagnostics.Add(UnitDiagnostic.Error(Code, message, path));
        return null;
    }

    private static int? Year(int? year, string path, List<UnitDiagnostic> diagnostics)
    {
        if (year is < 1939 or > 1945)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'scenarioYear' {year} is not a war year from 1939 to 1945.", path));
            return null;
        }

        return year;
    }

    private static int? Month(int? month, string path, List<UnitDiagnostic> diagnostics)
    {
        if (month is < 1 or > 12)
        {
            diagnostics.Add(UnitDiagnostic.Error(Code, $"'scenarioMonth' {month} is not a month from 1 to 12.", path));
            return null;
        }

        return month;
    }
}
