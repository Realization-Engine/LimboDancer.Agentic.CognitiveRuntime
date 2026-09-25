using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The registered actions that may change a live game (ASL-UNIT-042; Governed Writes Design, section 4). Each commits
/// events through the Execution Gate only. They are internal and cannot be undone once committed, so the default risk
/// policy asks for confirmation.
/// </summary>
public static class GameActions
{
    public const string SetupPermission = "asl.game.setup";
    public const string PlayPermission = "asl.game.play";

    public static readonly ActionDescriptor Setup = Descriptor("asl.game.setup", "Set up a game",
        "Start a live game on the boards in play, or add placements to one still in setup, as checked against the boards and the catalog.",
        SetupPermission, "asl.game.setup-open-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "placements"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "start": { "type": "object" },
            "placements": { "type": "array", "items": { "type": "object" } }
          }
        }
        """);

    public static readonly ActionDescriptor AdvancePhase = Descriptor("asl.game.advance-phase", "Advance the phase",
        "Move a live game to the next phase of the Player Turn (A3.1 to A3.8), and to the other side's Player Turn after the CCPh.",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 }
          }
        }
        """);

    public static readonly ActionDescriptor EnterEmptyBuilding = Descriptor("asl.game.enter-empty-building", "Enter an empty building",
        "Move a Good Order squad into an adjacent, known-empty, ground-level ordinary building in its MPh for 2 MF, only when the reviewed Scenario A1 first case concludes it definitively.",
        PlayPermission, "asl.game.reviewed-first-case-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "location"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "location": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1Package.Identity.ToString()
        }));

    public static IReadOnlyList<ActionDescriptor> All { get; } = [Setup, AdvancePhase, EnterEmptyBuilding];

    public static string VersionKey(Guid tenant, string game) => $"asl.game:{tenant:N}:{game}";

    private static ActionDescriptor Descriptor(string id, string name, string description, string permission, string precondition, string schema,
        JsonElement? preconditionData = null)
    {
        using var document = JsonDocument.Parse(schema);
        return new ActionDescriptor(new ActionId(id), new ActionVersion("1.0.0"), name, description, document.RootElement.Clone(), null,
            new ActionRiskProfile(ActionMutability.Write, ActionIdempotency.Idempotent, ActionReversibility.Irreversible, ActionBoundary.Internal,
                ActionPrivilege.Normal),
            [permission],
            [new PreconditionDescriptor(id + ".current-state", PreconditionKind.Operational, precondition,
                preconditionData ?? JsonSerializer.SerializeToElement(new { source = LiveGames.Source }), required: true)],
            [], IdempotencyMode.KeyRequired, new ExecutorBinding(id + ".executor.v1"));
    }
}
