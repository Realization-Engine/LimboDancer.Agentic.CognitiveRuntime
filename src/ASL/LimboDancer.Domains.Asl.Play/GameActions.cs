using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The registered actions that may change a live game (ASL-UNIT-042; Governed Writes Design, section 5). Each commits
/// events through the Execution Gate only. They are internal and cannot be undone once committed, so the default risk
/// policy asks for confirmation.
/// </summary>
public static class GameActions
{
    public const string WindowAdjudicationPermission = "asl.game.adjudicate-window";
    public static readonly ActionDescriptor ReviewPlacedDcWindow = Descriptor("asl.game.review-placed-dc-window", "Review Placed DC timing",
        "Record external AFPh opening-task evidence for one Placed DC at the current state; does not authorize an attack.",
        WindowAdjudicationPermission, "asl.game.sequence-v1", """
        { "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "review"],
          "properties": { "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 }, "review": { "type": "object" } }
        }
        """);

    public const string WorkAdjudicationPermission = "asl.game.adjudicate-work";
    public static readonly ActionDescriptor RecordTurnWork = Descriptor("asl.game.record-turn-work", "Record work adjudication",
        "Record externally adjudicated packing or replenishment facts and lifecycle changes. This does not certify unintegrated rules.",
        WorkAdjudicationPermission, "asl.game.sequence-v1", """
        { "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "workId", "operation", "ruleBasis"],
          "properties": { "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 }, "workId": { "type": "string" },
            "operation": { "type": "string", "enum": ["start", "interrupt", "resume", "cancel", "complete"] },
            "ruleBasis": { "type": "string" }, "declaration": { "type": "object" } }
        }
        """);

    public const string SetupPermission = "asl.game.setup";
    public const string PlayPermission = "asl.game.play";

    public static readonly ActionDescriptor DesignateSpotter = Descriptor("asl.game.designate-spotter", "Designate a mortar Spotter",
        "Designate a daytime Spotter, or replace a lost Spotter at the owner's MPh opening. Rules determines eligibility and the designation's effects. Setup activation and night remain pending.",
        PlayPermission, "asl.game.sequence-v1", """
        { "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "gunId", "spotter"],
          "properties": { "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 }, "gunId": { "type": "string" }, "spotter": { "type": "string" } }
        }
        """);

    public static readonly ActionDescriptor Setup = Descriptor("asl.game.setup", "Set up a game",
        "Start a live game on the boards in play, or add placements to one still in setup, as checked against the boards and the catalog; or place non-OB \"?\" once both sides have set up (A12.12).",
        SetupPermission, "asl.game.setup-open-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "placements"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "start": { "type": "object" },
            "placements": { "type": "array", "items": { "type": "object" } },
            "conceal": { "type": "object" }
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
        "In the RtPh, rout a broken unit that must or may rout along a route of ADJACENT Locations to the nearest woods or building Location, within six MF (three for a wounded SMC), or by Low Crawl one Location; entering Open Ground in the LOS and Normal Range of an unbroken enemy unit is Interdicted by a NMC (A10.5 to A10.533; ruling R13.3). A broken Passenger names no route and routs beneath its Stopped vehicle with all its MF (beneath; D6.1, pass 65). An unbroken, unpinned, non-berserk leader stacked with the unit may rout with it (withLeader): he takes no NMC, adds his leadership DRM to its Interdiction NMC, and is eliminated if it fails (A10.711; pass 70).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" },
            "route": { "type": "array", "items": { "type": "string" } },
            "lowCrawl": { "type": "boolean" },
            "keep": { "type": "array", "items": { "type": "string" } },
            "beneath": { "type": "boolean" },
            "withLeader": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor VoluntaryBreak = Descriptor("asl.game.voluntary-break", "Break units voluntarily",
        "At the start of the RtPh, before any unit routs, the owner breaks units of his side (even pinned ones) in the LOS and Normal Range of a Known armed unbroken enemy ground unit, or ADJACENT to a Known unbroken one, so that they may rout: each is broken and under DM, loses a pin and its \"?\"; not a unit already broken, a hero, a berserk or Unarmed unit, or one with no broken side; a leader's break calls for a LLTC (A10.41, A10.62, A10.2; pass 70).",
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

    public static readonly ActionDescriptor Deploy = Descriptor("asl.game.deploy", "Deploy a squad",
        "In the RPh, a Good Order squad with a Good Order leader of its nationality in its Location takes a NTC modified by his leadership to become two HS; the first keeps its SW unless some are named for the second. A squad with a retained Spotter assignment must choose which HS keeps spotting (A1.31; C9.3; ruling R13.4).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "squadId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "squadId": { "type": "string" }, "leader": { "type": "string" },
            "secondWeapons": { "type": "array", "items": { "type": "string" } },
            "spotterHalf": { "type": "string", "enum": ["first", "second"] }
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

    public static readonly ActionDescriptor Dismantle = Descriptor("asl.game.dismantle", "Dismantle or assemble a SW",
        "Its possessor dismantles or assembles a non-Russian MMG or HMG, or a light mortar of four PP or more, in its side's PFPh or DFPh, if the weapon has not fired in it; this counts as the weapon's use. A dismantled weapon portages at half its PP and is not fired, except a German MMG or HMG, which fires as a LMG (A9.8).",
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
        "Fire a fire group of Good Order squads and half-squads, with the MGs they possess, in one Location or across ADJACENT Locations, optionally directed by leaders there, at another Location: in the PFPh or AFPh (phasing side), the DFPh (the other side), or the MPh at the moving stack (Defensive First Fire, Subsequent First Fire, or FPF, by the firers' markers). The attack is resolved by the reviewed Fire package, and only when every outcome the dice can reach is decided; its rolls are drawn as the package asks for them. A FT or MOL attack may name an AFV in the target Location as its target (vehicle): a To Kill DR on the C7.34 FT or MOL column, then a Specific Collateral Attack on its CE crew (A22.34, A22.612).",
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
            "target": { "type": "string" }, "snapShot": { "type": "boolean" }, "sprayTarget": { "type": "string" }, "mol": { "type": "string" }, "vehicle": { "type": "string" },
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

    public static readonly ActionDescriptor Repair = Descriptor("asl.game.repair", "Repair a weapon",
        "Attempt in the RPh to repair a malfunctioned MG a Good Order unit possesses: a dr at most its Repair Number repairs it, a 6 eliminates it (A9.72).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" },
            "weapon": { "type": "string" }, "hero": { "type": ["string", "null"] }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor Move = Descriptor("asl.game.move", "Move",
        "Move a Good Order stack of the phasing side into an adjacent Location in its MPh at the terrain's MF cost, optionally by Assault Movement or with Double Time, which adds MF and makes the movers CX (A4.5); or, staying in its Location, have one of its squads attempt to place SMOKE grenades there or in an ADJACENT Location (A24.1), or have one of its units Place a DC in an ADJACENT Location for the MF entering it would cost (A23.3); or, naming an edge instead of a Location, leave the map from an edge hex (A2.6). A berserk unit names the 1PP SW it keeps within its IPC (A15.431). The DEFENDER may then fire at it before it moves again; Residual FP there attacks it first.",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "to": { "type": "string" }, "exit": { "type": "string", "enum": ["top", "bottom", "left", "right"] },
            "assault": { "type": "boolean" }, "doubleTime": { "type": "boolean" }, "pushGun": { "type": "string" },
            "smoke": { "type": "string" }, "smokeBy": { "type": "string" }, "minimumMove": { "type": "boolean" }, "terrainRate": { "type": "boolean" }, "dash": { "type": "string" }, "ipcTo": { "type": "string" }, "overrun": { "type": "boolean" },
            "placeDc": { "type": "string" }, "placeDcAt": { "type": "string" },
            "keep": { "type": "array", "items": { "type": "string" } },
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

    public static readonly ActionDescriptor ForfeitRof = Descriptor("asl.game.forfeit-rof", "Give up a weapon's ROF for Residual FP",
        "The DEFENDER, at once after a Defensive First Fire attack in which a weapon kept its Multiple ROF, gives the ROF up so that the weapon's FP counts toward the Residual FP left (A8.23).",
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

    public static readonly ActionDescriptor MotionAttempt = Descriptor("asl.game.motion-attempt", "Attempt Motion status",
        "In the enemy MPh, while the DEFENDER may still answer a moving unit's expenditure, a Mobile vehicle of the DEFENDER makes a dr against the MF or MP that unit has spent in its LOS in this MPh, having begun its move out of it: at or below, the vehicle gains Motion and may freely change its VCA and TCA; once per enemy MPh, and not under a First, Final, or Intensive Fire counter (D2.401).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" }, "facing": { "type": "string" }, "turret": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor InfantryOverrunNtc = Descriptor("asl.game.infantry-overrun-ntc", "Take an Infantry OVR's NTC",
        "The ATTACKER's stack takes the NTC that lets it enter the Location of a lone Known enemy SMC in its MPh: by its leader for the stack, or by its one MMC, against the TEM there (A4.15).",
        PlayPermission, "asl.game.sequence-v1", """
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
        """);

    public static readonly ActionDescriptor OverrunEvade = Descriptor("asl.game.overrun-evade", "Move away from an Infantry OVR",
        "The DEFENDER's SMC, before any Defensive First Fire at the units that entered its Location by an Infantry OVR, moves away into an adjacent Location of the ATTACKER's choice (A4.151).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }
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
        "In the APh, Infantry of the phasing side in one Location that are not broken, pinned, berserk, or held in Melee advance into one ADJACENT Location at the same level, even one holding Known enemy units, where CC follows (A3.7, A4.7); units waiting off board enter by advance into a hex of their entry edge (A2.5); or, naming an edge instead of a Location, units leave the map from an edge hex (A2.6).",
        PlayPermission, "asl.game.reviewed-close-combat-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "to": { "type": "string" }, "exit": { "type": "string", "enum": ["top", "bottom", "left", "right"] }
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
            "drops": { "type": "array", "items": { "type": "string" } },
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
            "location": { "type": "string" }, "unitIds": { "type": "array", "items": { "type": "string" } }, "to": { "type": "string" },
            "drops": { "type": "array", "items": { "type": "string" } }
          }
        }
        """);

    public static readonly ActionDescriptor StreetFightingReturn = Descriptor("asl.game.street-fighting-return", "Street Fighting return",
        "In the CCPh, once the CC of a vehicle's road Location is over, a unit that advanced there from the ground level of an ADJACENT building returns to that Location, unless broken or pinned; it may stay instead (A11.8).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }
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
        "In the PFPh, AFPh, or DFPh, a Gun manned by its crew, or a tank's MA, fires HE at the enemy units of a Location on the Infantry Target Type, or AP, APCR, APDS, HEAT, or HE at one named enemy vehicle on the Vehicle Target Type, turning its barrel or turret to bring the target into its Covered Arc if it must; a light mortar fires HE on the Area Target Type, optionally spotted or directed by a leader; and a unit names '<unit>:pf' to make a PF Check and fire a Panzerfaust at an enemy AFV. A Gun that lists Canister fires it with ammunition 'canister', an aim of 1 to 5 among the five vertices of its Covered Arc, and an aimFacing when it turns first; it names no target, makes no To Hit DR, and attacks each Location of the aim on one IFT DR (C8.4). The reviewed Ordnance package resolves the To Hit DR, the hit's IFT attack or To Kill DR, ROF, breakdown, and Acquisition, only when every outcome the dice can reach is decided (C3.3, C3.31, C3.32, C3.33, C7, C9, C13.3, D1.3).",
        PlayPermission, "asl.game.reviewed-ordnance-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "gunId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "gunId": { "type": "string" }, "target": { "type": "string" },
            "targetVehicle": { "type": "string" },
            "ammunition": { "type": "string", "enum": ["ap", "apcr", "apds", "heat", "he", "canister"] },
            "aim": { "type": "integer", "minimum": 1, "maximum": 5 },
            "aimFacing": { "type": "string", "enum": ["east", "north-east", "north-west", "west", "south-west", "south-east"] },
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
        "In its MPh, a vehicle of the phasing side spends one MP expenditure: start (1 MP unless in Motion; forward or in Reverse; a bogged vehicle's Bog Removal), a VCA change of one hexspine, entering a hex forward, in Reverse, or in VBM at the Terrain Chart's cost with any Bog DR, as a Minimum Move, or with an OVR, stop (1 MP), a Delay MP, or exit; or an ESB DR for more MP, or an OVR declared where it is after the concealed units there chose; or Infantry board it (units, before it spends any MP, or as the last step of their own move) or its Passengers unload (units; a halftrack or Carrier may keep their SW aboard, leave, and they may take one left there, take; a Carrier's inherent HS may unload too, Abandoning it, crew); a turret may turn with the step; the DEFENDER may fire after each (D2.1 to D2.7, D2.17, D3.12, D6.4, D6.5, D7.1, D8.2, D8.3; rulings R11.1 to R11.12, R26.2, R65.15).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId", "kind"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" },
            "kind": { "type": "string", "enum": ["start", "turn", "enter", "stop", "exit", "esb", "overrun", "load", "unload", "delay"] },
            "facing": { "type": "string" }, "to": { "type": "string" },
            "edge": { "type": "string", "enum": ["top", "bottom", "left", "right"] },
            "reverse": { "type": "boolean" }, "bypass": { "type": "boolean" }, "allMp": { "type": "boolean" },
            "overrun": { "type": "boolean" }, "minimumMove": { "type": "boolean" },
            "mp": { "type": "integer", "minimum": 1 },
            "units": { "type": "array", "items": { "type": "string" } },
            "turret": { "type": "string" },
            "leave": { "type": "array", "items": { "type": "string" } },
            "take": { "type": "object", "additionalProperties": { "type": "string" } },
            "riders": { "type": "boolean" }, "crew": { "type": "boolean" }
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
        "One CC attack in a Location holding a vehicle: Infantry attack the vehicle (one unit, or one with a SMC), to eliminate it or, with capture, to capture an Abandoned AFV (A21.2), or the vehicle attacks Infantry with its CC armament and any CE halftrack Passengers, one attack at a time, the non-vehicular side first, an ambusher first (A11.31, A11.34, A11.5, A11.62); or, with no vehicleId, Infantry attack enemy Infantry, a Rider among either, with the SMC stacking and director the attack declares (A11.31, A11.611; ruling R70.30), and with capture the Passengers of an unarmored stationary vehicle, to capture them and it (A11.52; ruling R70.34); or a side passes; or, in the MPh, a DEFENDER unit's CC Reaction Fire at a vehicle in its Location, after any PAATC (D7.21; rulings R11.13 to R11.16, R65.22).",
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
            "pass": { "type": "boolean" },
            "capture": { "type": "boolean" },
            "stacking": { "type": "object", "additionalProperties": { "type": "string" } },
            "director": { "type": "string" }
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

    public static readonly ActionDescriptor ThrowDc = Descriptor("asl.game.throw-dc", "Throw a DC",
        "A Good Order (or berserk) unpinned unit Throws the DC it possesses into an ADJACENT Location in its LOS at its level, in a friendly fire phase or as Defensive First Fire at the moving stack: 30 FP there with +2, then its own Location with +3, each on its own DR (+1 more in the AFPh unless an Opportunity Firer); a unit marked First Fire throws none. The DC is removed (A23.6 to A23.63; ruling R15.3). It may name an enemy AFV in the target Location (vehicle): a Position DR, then a To Kill DR on the C7.34 DC column (A23.5, C7.346).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "equipmentId", "target"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "equipmentId": { "type": "string" }, "target": { "type": "string" }, "vehicle": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor DetonateDc = Descriptor("asl.game.detonate-dc", "Detonate a Placed DC",
        "In its side's AFPh, a DC operably Placed this MPh detonates on its target Location with 30 FP, halved when every unit there was concealed at its Placement, with the Location's TEM and +1 when its placer was CX; an Original DR of 12 (10 for a non-elite owner, 8 for a captured DC) removes it with no effect. The AFPh does not end before every Placed DC detonates (A23.1, A23.3, A23.4; ruling R15.2). It may name the enemy AFV in its Location (vehicle): a Position DR, then a To Kill DR on the C7.34 DC column (A23.5, C7.346).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "equipmentId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "equipmentId": { "type": "string" }, "vehicle": { "type": "string" }, "guideRoute": { "type": "string" }, "guideProfile": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor FireStarshell = Descriptor("asl.game.fire-starshell", "Fire a Starshell",
        "At night, a Good Order, unpinned, not TI leader, CE AFV, or MMC fires a Starshell in the PFPh (phasing side) or in the DFPh or as Defensive First Fire (the other side), one attempt per hex per phase, after a Usage dr of 4 or less (leader) or 2 or less: method own-hex (its hex, then one hex in a Random Direction), at-target (a Gunflash or Known enemy unit in its LOS within six hexes, then a Random Direction DR with the Extent of Error halved), or three-hexes (a hex exactly three hexes away, then a Random Direction DR). The Starshell Illuminates three hexes around it until the end of the CCPh (E1.9 to E1.923; ruling R16.8).",
        PlayPermission, "asl.game.reviewed-fire-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "method"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "method": { "type": "string", "enum": ["own-hex", "at-target", "three-hexes"] }, "at": { "type": "string" }
          }
        }
        """, JsonSerializer.SerializeToElement(new
        {
            package = ScenarioA1FirePackage.Identity.ToString()
        }));

    public static readonly ActionDescriptor PlaceHidden = Descriptor("asl.game.place-hidden", "Place hidden units beneath \"?\"",
        "Hidden units of one side are placed on the map beneath a \"?\" and are concealed from then on: a hidden unit that is to move or advance must first be placed so, and one may give up its hidden status so at any time (A12.32; pass 23, ruling R23.5).",
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

    public static readonly ActionDescriptor RemoveConcealment = Descriptor("asl.game.remove-concealment", "Remove \"?\" voluntarily",
        "Once play has begun, in either side's Player Turn, the owner gives up the \"?\" of units of one side on the map: a concealed unit is revealed, a hidden one placed and revealed in full, a Dummy removed, and a crew's Gun or a vehicle's PRC with it (A12.14, A12.11, A12.31, A12.2; pass 70, ruling R70.20).",
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

    public static readonly ActionDescriptor Search = Descriptor("asl.game.search", "Search",
        "In the MPh, as the moving stack ends its move, its Good Order Infantry members (with at least one MMC, none pinned or using Assault Movement) spend one more MF to Search their own hex and the Accessible hexes named in order of preference: a Search dr leaves its Final dr of them unsearched; concealed enemy units in the hexes searched lose \"?\", hidden ones are placed beneath \"?\", Dummies are removed, and an armed Good Order enemy unit there draws a Casualty dr; the searchers become TI, and the DEFENDER may fire at that MF (A12.152, A12.154, A8.1; pass 70, ruling R70.24).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitIds"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "hexes": { "type": "array", "items": { "type": "string" } }
          }
        }
        """);

    public static readonly ActionDescriptor MopUp = Descriptor("asl.game.mop-up", "Mop Up a building",
        "In their side's PFPh, Infantry MMC in a multi-hex or multi-level building become TI to secure it (a building the card names by its id, any other by its board and lowest hex): hidden enemy units there are placed beneath \"?\" and Dummies removed; with no concealed enemy unit left, the side Controls every Location of the building and its broken enemy units surrender to the guard chosen (A12.153, A12.154, A26.11; pass 24, ruling R24.2; pass 70, ruling R70.25).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "building", "unitIds"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "building": { "type": "string" },
            "unitIds": { "type": "array", "items": { "type": "string" }, "minItems": 1 },
            "guard": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor AbandonVehicle = Descriptor("asl.game.abandon-vehicle", "Abandon a vehicle",
        "In its own MPh, the crew of an armed vehicle that has not moved or fired this Player Turn leaves it, spending all its MF to be placed beneath it, and the vehicle is Abandoned; the DEFENDER may fire at the crew with FFNAM (D5.41, D5.43). An Inherent crew may also destroy it as it leaves (destroy: wreck for a non-burning wreck, malfunction for every MG and the MA; D5.411), and the crew of an armored halftrack of its nationality may Remove its MG as its LMG (removeMg; D6.631). The crew takes the vehicle's Armor Leader with it (D3.43).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "vehicleId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "vehicleId": { "type": "string" },
            "destroy": { "type": "string", "enum": ["wreck", "malfunction"] },
            "removeMg": { "type": "boolean" }
          }
        }
        """);

    public static readonly ActionDescriptor RecrewVehicle = Descriptor("asl.game.recrew-vehicle", "Re-crew a vehicle",
        "In its own MPh, an Infantry unit in Good Order in the Location of its side's Abandoned vehicle since the MPh began enters it to become its crew, spending all its MF; not in the Player Turn its last crew left it, nor a Recalled crew into its own AFV. A unit other than a vehicle or infantry crew is a Temporary crew (A21.22). The crew brings back the Armor Leader and a MG it Removed (D5.42, D5.341, D3.43, D6.631).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "vehicleId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "vehicleId": { "type": "string" }
          }
        }
        """);

    public static readonly ActionDescriptor Scrounge = Descriptor("asl.game.scrounge", "Scrounge a vehicle",
        "In its own RPh, an unbroken, non-berserk Infantry MMC not in Melee scrounges a non-burning wreck or an Abandoned vehicle in its Location that is not Scrounged or Disabled: a dr of 1 to 3 salvages that many of its BMG not fixed, AAMG, or mortar MA, by Random Selection, a MG as its nationality's LMG. Whatever the dr, the vehicle is marked Scrounged, and the unit and what it salvages are TI for the rest of the Player Turn (D10.5 to D10.52).",
        PlayPermission, "asl.game.sequence-v1", """
        {
          "type": "object", "additionalProperties": false,
          "required": ["gameId", "attemptId", "expectedRevision", "unitId", "vehicleId"],
          "properties": {
            "gameId": { "type": "string" }, "attemptId": { "type": "string" },
            "expectedRevision": { "type": "integer", "minimum": 0 },
            "unitId": { "type": "string" }, "vehicleId": { "type": "string" }
          }
        }
        """);

    public static IReadOnlyList<ActionDescriptor> All
    {
        get;
    } =
        [Setup, AdvancePhase, DesignateSpotter, RecordTurnWork, ReviewPlacedDcWindow, EnterEmptyBuilding, EnterBuilding, DeclareOverrun, Fire, Rally, Repair, Move, PassFire, EndMove, Advance, Ambush, CloseCombat, TakePrisoner,
            FireOrdnance, RecoverShock, TurnGun, HookGun, MoveVehicle, Overrun, VehicleCloseCombat, ButtonUp, Choose, Massacre, OpportunityFire,
            Rout, Deploy, Recombine, Transfer, Drop, Recover, Dismantle, AmbushWithdraw, GuardPrisoners, ThrowDc, DetonateDc, FireStarshell, PlaceHidden, MopUp,
            InfantryOverrunNtc, OverrunEvade, ForfeitRof, MotionAttempt, AbandonVehicle, RecrewVehicle, Scrounge, VoluntaryBreak, RemoveConcealment, Search, StreetFightingReturn];

    public static string VersionKey(Guid tenant, string game) => $"asl.game:{tenant:N}:{game}";

    private static ActionDescriptor Descriptor(string id, string name, string description, string permission, string precondition, string schema,
        JsonElement? preconditionData = null)
    {
        // Pass 31 (ruling R31.6): every action may name the view that proposes it.
        var node = System.Text.Json.Nodes.JsonNode.Parse(schema)!;
        node["properties"]![GamePlanner.ProposedByArgument] = new System.Text.Json.Nodes.JsonObject { ["type"] = "string" };
        return new ActionDescriptor(new ActionId(id), new ActionVersion("1.0.0"), name, description, JsonSerializer.SerializeToElement(node), null,
            new ActionRiskProfile(ActionMutability.Write, ActionIdempotency.Idempotent, ActionReversibility.Irreversible, ActionBoundary.Internal,
                ActionPrivilege.Normal),
            [permission],
            [new PreconditionDescriptor(id + ".current-state", PreconditionKind.Operational, precondition,
                preconditionData ?? JsonSerializer.SerializeToElement(new { source = LiveGames.Source }), required: true)],
            [], IdempotencyMode.KeyRequired, new ExecutorBinding(id + ".executor.v1"));
    }
}
