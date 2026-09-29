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
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "retainDm": { "type": "array", "items": { "type": "string" } }
          }
        }
        """);

    public static readonly ActionDescriptor Rout = Descriptor("asl.game.rout", "Rout",
        "In the RtPh, rout a broken unit that must or may rout along a route of ADJACENT Locations to the nearest woods or building Location, within six MF (three for a wounded SMC), or by Low Crawl one Location; entering Open Ground in the LOS and Normal Range of an unbroken enemy unit is Interdicted by a NMC (A10.5 to A10.533; ruling R13.3).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "route"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" },
            "route": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "lowCrawl": { "type": "boolean" }
          }
        }
        """);

    public static readonly ActionDescriptor Deploy = Descriptor("asl.game.deploy", "Deploy a squad",
        "In the RPh, a Good Order squad with a Good Order leader of its nationality in its Location takes a NTC modified by his leadership (Guards need no leader and take it unmodified) to become two HS; the first keeps its SW unless some are named for the second (A1.31; ruling R13.4).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "squadId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "squadId": { "type": "string" }, "leader": { "type": "string" },
            "secondWeapons": { "type": "array", "items": { "type": "string" } }
          }
        }
        """);

    public static readonly ActionDescriptor Recombine = Descriptor("asl.game.recombine", "Recombine two HS",
        "In the RPh, two Good Order HS of one definition in a Location with a Good Order leader of their nationality (Guards need none) Recombine into their squad, keeping their SW (A1.32; ruling R13.4).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "halfSquads"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "halfSquads": { "type": "array", "items": { "type": "string" }, "minItems": 2, "maxItems": 2 },
            "leader": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Transfer = Descriptor("asl.game.transfer", "Transfer a SW",
        "Pass a SW between Good Order unpinned units of one side in one Location in their RPh, or in their APh before either advances (A4.431; ruling R13.5).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId", "toUnitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }, "toUnitId": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Drop = Descriptor("asl.game.drop", "Drop a SW",
        "Leave a SW in the unit's Location: an unbroken unit in its MPh before it moves, its APh before it advances, or at the start of the CCPh; a broken unit in the RtPh before it routs (A4.43; ruling R13.5).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Recover = Descriptor("asl.game.recover", "Recover a SW",
        "An unpinned Good Order unit tries to Recover an unpossessed SW in its Location on a Final dr below 6 (+1 CX), as its RPh action or in its MPh for one MF before it moves; a SMC may Recover one from its friendly broken unit (A4.44; ruling R13.5).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Dismantle = Descriptor("asl.game.dismantle", "Dismantle or assemble a MG",
        "Its possessor dismantles or assembles the German MMG in its side's PFPh or DFPh, if the MG has not fired in it; this counts as the MG's use. A dismantled MG is not fired and portages at half its PP (A9.8; ruling R13.6).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }
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
            "partners": { "type": "object", "additionalProperties": { "type": "string" } },
            "target": { "type": "string" }, "snapShot": { "type": "boolean" }, "sprayTarget": { "type": "string" },
            "fireLane": {
              "type": "object", "additionalProperties": false, "required": ["weapon", "to"],
              "properties": { "weapon": { "type": "string" }, "to": { "type": "string" } }
            }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor OpportunityFire = Descriptor("asl.game.opportunity-fire", "Declare Opportunity Fire",
        "In its PFPh, mark Good Order Infantry of the phasing side that have not fired with a Bounding Fire counter: they neither fire in the PFPh nor move in the MPh, and fire in the AFPh without its halving, their MGs keeping Multiple ROF; a concealed one in the LOS of a Good Order enemy ground unit within 16 hexes loses its \"?\" (A7.25; ruling R12.1).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 }
          }
        }
        """);

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
            "smoke": { "type": "string" }, "smokeBy": { "type": "string" }, "minimumMove": { "type": "boolean" },
            "bypass": { "type": "array", "items": { "type": "string", "enum": ["north", "northeast", "southeast", "south", "southwest", "northwest"] }, "minItems": 1, "maxItems": 2 }
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
            "stacking": { "type": "object", "additionalProperties": { "type": "string" } },
            "withdrawals": { "type": "object", "additionalProperties": { "type": "string" } },
            "infiltrations": { "type": "object", "additionalProperties": { "type": "string" } },
            "round": { "type": "string" }, "handToHand": { "type": "boolean" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1CloseCombatPackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor AmbushWithdraw = Descriptor("asl.game.ambush-withdraw", "Ambush Withdrawal",
        "In the CCPh, units of the side that ambushed withdraw from the Location to one a withdrawal could reach, before its first round or once its CC is over (A11.41).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "location", "unitIds", "to"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "location": { "type": "string" }, "unitIds": { "type": "array", "items": { "type": "string" } }, "to": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor GuardPrisoners = Descriptor("asl.game.guard-prisoners", "Transfer or abandon prisoners",
        "In its side's RPh or APh, a Guard not in Melee transfers its prisoners to another armed unit of its side in its Location with Guard capacity, or abandons them as Unarmed units of their own side (A20.5).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "guardId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "guardId": { "type": "string" }, "to": { "type": "string" }, "abandon": { "type": "boolean" }
          }
        }
        """);

    public static readonly ActionDescriptor TakePrisoner = Descriptor("asl.game.take-prisoner", "Take a prisoner",
        "After a Heat of Battle Surrender, the captor's side chooses which ADJACENT Known Good Order enemy unit takes the surrendering unit as its prisoner (A15.5, A20.5), or rejects the surrender, eliminating the unit and facing its side with No Quarter (A20.3).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "captorId": { "type": "string" }, "reject": { "type": "boolean" }, "free": { "type": "boolean" }
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
        "In its MPh, a vehicle of the phasing side spends one MP expenditure: start (1 MP unless in Motion; forward or in Reverse; a bogged vehicle's Bog Removal), a VCA change of one hexspine, entering a hex forward, in Reverse, or in VBM at the Terrain Chart's cost with any Bog DR, as a Minimum Move, or with an OVR, stop (1 MP), or exit; or an ESB DR for more MP, or an OVR declared where it is after the concealed units there chose; the DEFENDER may fire after each (D2.1 to D2.7, D7.1, D8.2, D8.3; rulings R11.1 to R11.12).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId", "kind"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" },
            "kind": { "type": "string", "enum": ["start", "turn", "enter", "stop", "exit", "esb", "overrun"] },
            "facing": { "type": "string" }, "to": { "type": "string" },
            "edge": { "type": "string", "enum": ["top", "bottom", "left", "right"] },
            "reverse": { "type": "boolean" }, "bypass": { "type": "boolean" }, "allMp": { "type": "boolean" },
            "overrun": { "type": "boolean" }, "minimumMove": { "type": "boolean" },
            "mp": { "type": "integer", "minimum": 1 }
          }
        }
        """);

    public static readonly ActionDescriptor Overrun = Descriptor("asl.game.overrun", "Resolve an OVR",
        "Once the DEFENDER passes on the MP expenditure of a declared OVR, the ATTACKER resolves it: the vehicle attacks every enemy unit in its Location on the IFT with its OVR FP, as the reviewed Fire package decides; the DEFENDER's Reaction Fire window follows (D7.1 to D7.2; ruling R11.11).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor VehicleCloseCombat = Descriptor("asl.game.vehicle-close-combat", "Close Combat with a vehicle",
        "One CC attack in a Location holding a vehicle: Infantry attack the vehicle (one unit, or one with a SMC), or the vehicle attacks Infantry with its CC armament, one attack at a time, the non-vehicular side first (A11.31, A11.5, A11.62; CC between Infantry there is not built); or a side passes; or, in the MPh, a DEFENDER unit's CC Reaction Fire at a vehicle in its Location, after any PAATC (D7.21; rulings R11.13 to R11.16).",
        PlayPermission, "asl.game.reviewed-close-combat-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "location"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "location": { "type": "string" },
            "attackers": { "type": "array", "items": { "type": "string" } },
            "defenders": { "type": "array", "items": { "type": "string" } },
            "vehicleId": { "type": "string" },
            "pass": { "type": "boolean" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1CloseCombatPackage.Identity.ToString()
        }));

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
            FireOrdnance, RecoverShock, TurnGun, HookGun, MoveVehicle, Overrun, VehicleCloseCombat, ButtonUp, Choose, Massacre, OpportunityFire,
            Rout, Deploy, Recombine, Transfer, Drop, Recover, Dismantle, AmbushWithdraw, GuardPrisoners];

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
