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

                break;
            case PhaseChanged phase:
                writer.WriteNumber("turn", phase.Turn);
                writer.WriteString("phase", phase.Phase);
                writer.WriteString("phasingSide", phase.PhasingSide);
                break;
            case InstanceCreated created:
                writer.WritePropertyName("instance");
                WriteNew(writer, created.Instance);
                break;
            case InstanceMoved moved:
                writer.WriteString("id", moved.Id);
                WritePosition(writer, "position", moved.Position);
                if (moved.Mf is { } mf)
                {
                    writer.WriteNumber("mf", mf);
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
            case InstanceCaptured captured:
                writer.WriteString("id", captured.Id);
                writer.WriteString("custodian", captured.Custodian);
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

        if (instance.Position is { } position)
        {
            WritePosition(writer, "position", position);
        }

        WriteHolding(writer, instance.Holding);
        WriteConditions(writer, instance.Conditions);
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
                    _ => "in-fortification",
                });
                break;
            case OffMapPosition:
                writer.WriteBoolean("offMap", true);
                break;
            default:
                writer.WriteBoolean("notEntered", true);
                break;
        }

        writer.WriteEndObject();
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
