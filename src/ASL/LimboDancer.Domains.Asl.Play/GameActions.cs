using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The registered actions that may change a live game (ASL-UNIT-042; Governed Writes Design, section 5). Each commits
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

    public static readonly ActionDescriptor EnterBuilding = Descriptor("asl.game.enter-building", "Enter a building",
        "Move a Good Order squad into an adjacent ground-level ordinary building in its MPh. The adjudicator routes the entry to the reviewed Scenario A1 case that covers the target: an empty building is entered for 2 MF, a known enemy refuses the entry (A4.14), and one concealed or hidden enemy MMC is revealed and forces the squad back (A12.15). Anything else is refused.",
        PlayPermission, "asl.game.reviewed-entry-cases-v1", """
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
            packages = new[] { ScenarioA1Package.Identity.ToString(), ScenarioA1OccupiedPackage.Identity.ToString(), ScenarioA1PostRevealPackage.Identity.ToString() }
        }));

    public static readonly ActionDescriptor DeclareOverrun = Descriptor("asl.game.declare-overrun", "Declare an Infantry OVR",
        "After an entry reveals a lone enemy SMC, record the attacker's choice (A12.15, A4.15). Declining forces the unit back through the reviewed delegation to the PostReveal forced back; electing is refused until the OVR NTC is reviewed.",
        PlayPermission, "asl.game.reviewed-ovr-declaration-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "choice"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "choice": { "type": "string", "enum": ["decline", "elect"] }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            packages = new[] { ScenarioA1ConcealedSmcOverrunPackage.Identity.ToString(), ScenarioA1PostRevealPackage.Identity.ToString() }
        }));

    public static readonly ActionDescriptor Fire = Descriptor("asl.game.fire", "Fire",
        "Fire a fire group of Good Order squads and half-squads, with the MGs they possess, in one Location or across ADJACENT Locations, optionally directed by leaders there, at another Location: in the PFPh or AFPh (phasing side), the DFPh (the other side), or the MPh at the moving stack (Defensive First Fire, Subsequent First Fire, or FPF, by the firers' markers). The attack is resolved by the reviewed Fire package, and only when every outcome the dice can reach is decided; its rolls are drawn as the package asks for them.",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "firers", "target"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "firers": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "director": { "type": "string" },
            "directors": { "type": "array", "items": { "type": "string" } },
            "weapons": { "type": "object", "additionalProperties": { "type": "array", "items": { "type": "string" } } },
            "withoutInherent": { "type": "array", "items": { "type": "string" } },
            "target": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor Rally = Descriptor("asl.game.rally", "Rally",
        "Attempt to rally a broken unit in the RPh, by a Good Order leader in its Location or by Self-Rally, as the reviewed Rally package resolves it, and only when every outcome the dice can reach is decided.",
        PlayPermission, "asl.game.reviewed-rally-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "leader": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1RallyPackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor Repair = Descriptor("asl.game.repair", "Repair a SW",
        "Attempt in the RPh to repair a malfunctioned MG a Good Order unit possesses: a dr at most its Repair Number repairs it, a 6 eliminates it (A9.72).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor Move = Descriptor("asl.game.move", "Move",
        "Move a Good Order stack of the phasing side into an adjacent Location in its MPh at the terrain's MF cost, optionally by Assault Movement or with Double Time, which adds MF and makes the movers CX (A4.5); or, staying in its Location, have one of its squads attempt to place SMOKE grenades there or in an ADJACENT Location (A24.1). The DEFENDER may then fire at it before it moves again; Residual FP there attacks it first.",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds", "to"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "to": { "type": "string" }, "assault": { "type": "boolean" }, "doubleTime": { "type": "boolean" }, "pushGun": { "type": "string" },
            "smoke": { "type": "string" }, "smokeBy": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor PassFire = Descriptor("asl.game.pass-fire", "Pass on the moving stack",
        "The DEFENDER passes on the moving stack's latest MF expenditure, so the ATTACKER may continue or end its move (A8.11).",
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

    public static readonly ActionDescriptor EndMove = Descriptor("asl.game.end-move", "End the move",
        "The ATTACKER ends the move of the moving stack's members, all of them unless some are named; those units may not move again this MPh (A4.2, A8.11). A vehicle names the hex it wished to enter next when it ends in Motion with MP left (D2.4), and spends the MP it has left in its hex first, which the DEFENDER may fire at (D2.1).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "intended": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Advance = Descriptor("asl.game.advance", "Advance",
        "In the APh, Infantry of the phasing side in one Location that are not broken, pinned, berserk, or held in Melee advance into one ADJACENT Location at the same level, even one holding Known enemy units, where CC follows (A3.7, A4.7).",
        PlayPermission, "asl.game.reviewed-close-combat-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds", "to"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "to": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1CloseCombatPackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor Ambush = Descriptor("asl.game.ambush", "Roll for Ambush",
        "In the CCPh, before any CC in a Location where Infantry advanced into a woods or building Location, each side makes its Ambush dr, as the reviewed Close Combat package resolves it (A11.4).",
        PlayPermission, "asl.game.reviewed-close-combat-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "location"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "location": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1CloseCombatPackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor CloseCombat = Descriptor("asl.game.close-combat", "Close Combat",
        "In the CCPh, resolve a round of CC in one Location: the attacks both sides declare, or with an Ambush the ambusher's and then the other side's, with the SMC stacking declared first, as the reviewed Close Combat package resolves them, and only when every outcome the dice can reach is decided (A11.11 to A11.14).",
        PlayPermission, "asl.game.reviewed-close-combat-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "location", "attacks"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "location": { "type": "string" },
            "attacks": { "type": "array", "items": { "type": "object" } },
            "stacking": { "type": "object", "additionalProperties": { "type": "string" } }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1CloseCombatPackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor TakePrisoner = Descriptor("asl.game.take-prisoner", "Take a prisoner",
        "After a Heat of Battle Surrender, the captor's side chooses which ADJACENT Known Good Order enemy unit takes the surrendering unit as its prisoner (A15.5, A20.5), or rejects the surrender, eliminating the unit and facing its side with No Quarter (A20.3).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "captorId": { "type": "string" }, "reject": { "type": "boolean" }
          }
        }
        """);

    public static readonly ActionDescriptor FireOrdnance = Descriptor("asl.game.fire-ordnance", "Fire a Gun",
        "In the PFPh, AFPh, or DFPh, a Gun manned by its crew, or a tank's MA, fires HE at the enemy units of a Location on the Infantry Target Type, or AP, APCR, HEAT, or HE at one named enemy vehicle on the Vehicle Target Type, turning its barrel or turret to bring the target into its Covered Arc if it must; a light mortar fires HE on the Area Target Type, optionally spotted or directed by a leader; and a unit names '<unit>:pf' to make a PF Check and fire a Panzerfaust at an enemy AFV. The reviewed Ordnance package resolves the To Hit DR, the hit's IFT attack or To Kill DR, ROF, breakdown, and Acquisition, only when every outcome the dice can reach is decided (C3.3, C3.31, C3.32, C3.33, C7, C9, C13.3, D1.3).",
        PlayPermission, "asl.game.reviewed-ordnance-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "gunId", "target"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "gunId": { "type": "string" }, "target": { "type": "string" },
            "targetVehicle": { "type": "string" },
            "ammunition": { "type": "string", "enum": ["ap", "apcr", "heat", "he"] },
            "intensive": { "type": "boolean" },
            "spotter": { "type": "string" }, "director": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1OrdnancePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor RecoverShock = Descriptor("asl.game.recover-shock", "Roll for a Shocked AFV",
        "In the RPh after it was placed, a Shocked AFV makes a dr (1 or 2 removes the Shock, 3 to 6 makes it an Unconfirmed Kill), and an Unconfirmed Kill makes one (1 to 3 removes it, 4 to 6 wrecks the AFV); the RPh does not end until each has (C7.42).",
        PlayPermission, "asl.game.reviewed-ordnance-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1OrdnancePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor TurnGun = Descriptor("asl.game.turn-gun", "Turn a Gun",
        "In its side's fire phase, a Gun its Good Order, unpinned crew could still fire changes its CA without firing, and fires no more that phase; in the PFPh neither it nor its crew moves that Player Turn (C3.22).",
        PlayPermission, "asl.game.reviewed-ordnance-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "gunId", "facing"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "gunId": { "type": "string" }, "facing": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor HookGun = Descriptor("asl.game.hook-gun", "Hook up or unhook a Gun",
        "In its MPh, a Stopped truck or halftrack whose T# is at most a Gun's M# spends half its MP to hook up the Gun in its hex, whose crew then walks, or to unhook it there for a crew on foot, facing a hexspine (C10.11, C10.12).",
        PlayPermission, "asl.game.reviewed-ordnance-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId", "gunId", "hooked"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" }, "gunId": { "type": "string" }, "hooked": { "type": "boolean" }, "facing": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor MoveVehicle = Descriptor("asl.game.move-vehicle", "Move a vehicle",
        "In its MPh, a vehicle of the phasing side spends one MP expenditure: start (1 MP unless in Motion), a VCA change of one hexspine (1 MP), entering the hex its VCA points at over Open Ground, Grain, or a road, or stop (1 MP); the DEFENDER may fire after each (D2.1, D2.11 to D2.13, D2.16; ruling R25.3).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId", "kind"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" },
            "kind": { "type": "string", "enum": ["start", "turn", "enter", "stop", "exit"] },
            "facing": { "type": "string" }, "to": { "type": "string" },
            "edge": { "type": "string", "enum": ["top", "bottom", "left", "right"] }
          }
        }
        """);

    public static readonly ActionDescriptor ButtonUp = Descriptor("asl.game.button-up", "Button up or expose a crew",
        "In its owner's MPh or APh, an AFV's crew buttons up or becomes Crew Exposed, once per phase, not while Stunned and not in an MPh after it Prep Fired (D5.33; ruling R25.8).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId", "buttonedUp"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" }, "buttonedUp": { "type": "boolean" }
          }
        }
        """);

    public static readonly ActionDescriptor Choose = Descriptor("asl.game.choose", "Answer a choice",
        "The side a resolution waits for answers its option (ruling R5.8): the Leader Creation dr after a Self-Rally Original 2 (A18.11), Battle Hardening (A15.3), the Unlikely Kill dr (A7.309), or which Location keeps a Gun's Acquisition (C6.51); the resolution then continues.",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "key", "option"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "key": { "type": "string" }, "option": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Massacre = Descriptor("asl.game.massacre", "Massacre a prisoner",
        "In its own fire phase, a Russian or berserk Infantry unit not in Melee eliminates a prisoner in its Location as its attack for the phase; the victim side's ELR rises by one, once, and it is faced with No Quarter (A20.4).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "prisonerId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "prisonerId": { "type": "string" }
          }
        }
        """);

    public static IReadOnlyList<ActionDescriptor> All
    {
        get;
    } =
        [Setup, AdvancePhase, EnterEmptyBuilding, EnterBuilding, DeclareOverrun, Fire, Rally, Repair, Move, PassFire, EndMove, Advance, Ambush, CloseCombat, TakePrisoner,
            FireOrdnance, RecoverShock, TurnGun, HookGun, MoveVehicle, ButtonUp, Choose, Massacre];

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
