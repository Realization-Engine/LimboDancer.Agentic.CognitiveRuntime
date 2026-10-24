"""Increment 2f: the Play page offers a Carrier's inherent HS to unload with the step (D6.83). Each replacement must match the stated number of times."""
import os

PAGE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "src", "ASL", "LimboDancer.Domains.Asl.MapStudio", "Components", "Pages",
                    "Play.razor")
EDITS = [
    # The three resets of the per-expenditure options.
    ("\n        vehicleRiders = false;\n", "\n        vehicleRiders = false;\n        vehicleCrewUnload = false;\n", 2),
    ("\n                vehicleRiders = false;\n", "\n                vehicleRiders = false;\n                vehicleCrewUnload = false;\n", 1),
    ("    private bool vehicleRiders;\n", "    private bool vehicleRiders;\n    private bool vehicleCrewUnload;\n", 1),
    ("            case \"riders\":\n                vehicleRiders = option.Value;\n                break;\n",
     "            case \"riders\":\n                vehicleRiders = option.Value;\n                break;\n"
     "            case \"crew\":\n                vehicleCrewUnload = option.Value;\n                break;\n", 1),
    ("                                    RiderNote=\"@RiderNote(chosen)\" AsRiders=\"@BoardsAsRiders(chosen)\"",
     "                                    CrewUnloads=\"@CrewUnloads(chosen)\" UnloadCrew=\"@vehicleCrewUnload\"\n"
     "                                    RiderNote=\"@RiderNote(chosen)\" AsRiders=\"@BoardsAsRiders(chosen)\"", 1),
    ("        // D6.2 (pass 65, ruling R65.16): a load names Riders when they board as Riders.\n",
     "        // D6.83 (pass 65, ruling R65.18): a Carrier's inherent HS unloads with the step when asked.\n"
     "        if (kind == \"unload\" && vehicle is not null && vehicleCrewUnload && CrewUnloads(vehicle))\n"
     "        {\n"
     "            arguments[\"crew\"] = true;\n"
     "        }\n\n"
     "        // D6.2 (pass 65, ruling R65.16): a load names Riders when they board as Riders.\n", 1),
    ("    /// <summary>The Passengers that may unload (D6.5): the vehicle's, while it is on the map and stopped.</summary>\n",
     "    /// <summary>Whether the chosen vehicle, stopped, is a Carrier whose inherent HS may unload as its Passengers do (D6.83; pass 65, ruling R65.18).</summary>\n"
     "    private bool CrewUnloads(UnitInstance vehicle) =>\n"
     "        state is not null && VehicleStopped(vehicle, boggedToo: ScenarioA1VehicleProjection.BoggedMaySpend(\"unload\", false)) && GamePlanner.CrewMayUnload(state, vehicle);\n\n"
     "    /// <summary>The Passengers that may unload (D6.5): the vehicle's, while it is on the map and stopped.</summary>\n", 1),
]

with open(PAGE, encoding="utf-8", newline="") as source:
    text = source.read()
for old, new, times in EDITS:
    count = text.count(old)
    if count != times:
        raise SystemExit(f"'{old[:60]}' found {count} times, expected {times}")
    text = text.replace(old, new)
with open(PAGE, "w", encoding="utf-8", newline="") as target:
    target.write(text)
print("ok")
