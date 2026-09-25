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
                    if (sideId is not null && nationality is not null)
                    {
                        sides.Add(new SideState(sideId, nationality, fields.OptionalInteger(side, "elr", sidePath), fields.OptionalInteger(side, "san", sidePath)));
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
                    if (boardText is not null && !BoardRef.TryParse(boardText, out _))
                    {
                        diagnostics.Add(UnitDiagnostic.Error(Code, $"'{boardText}' is not a board reference.", boardPath));
                    }
                    else if (boardText is not null && boardVersion is not null)
                    {
                        boards.Add(new PlacedBoard(BoardRef.Parse(boardText), boardVersion));
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
                    };
            case "phase-changed":
                var nextTurn = fields.OptionalInteger(payload, "turn", path);
                var nextPhase = fields.RequiredString(payload, "phase", path);
                var nextPhasing = fields.RequiredString(payload, "phasingSide", path);
                return nextTurn is null || nextPhase is null || nextPhasing is null ? null : new PhaseChanged(nextTurn.Value, nextPhase, nextPhasing);
            case "instance-created":
                return payload.TryGetProperty("instance", out var created) && ReadNew(created, path + ".instance", fields, diagnostics) is { } instance
                    ? new InstanceCreated(instance)
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
            case "instance-captured":
                var captured = fields.RequiredString(payload, "id", path);
                var custodian = fields.RequiredString(payload, "custodian", path);
                return captured is null || custodian is null ? null : new InstanceCaptured(captured, custodian);
            default:
                diagnostics.Add(UnitDiagnostic.Error(Code, $"'{type}' is not an event type.", path));
                return null;
        }
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
}
