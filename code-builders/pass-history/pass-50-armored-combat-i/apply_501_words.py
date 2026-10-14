"""Pass 50, tasks 50.1 and 50.2: what the Studio games showed: the gate's check of a weapon's repair, and the words."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
e.edit("LimboDancer.Domains.Asl.Play/GameGate.cs", """        ? state.RepairsThisPhase.Contains(repair.Unit)
""", """        ? state.RepairsThisPhase.Contains(repair.Weapon is { } repairedWeapon ? repair.Unit + ":" + repairedWeapon : repair.Unit)
""")
e.edit("LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs",
       """            return new FirerFirepower(mg is null ? vehicle.VehicleId! : vehicle.VehicleId + ":" + mg, printed, multipliers,""",
       """            return new FirerFirepower(mg is null ? vehicle.VehicleId! : vehicle.VehicleId + " " + mg.ToUpperInvariant(), printed, multipliers,""")
e.edit("LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs",
       """            // D1.8 (pass 50): the BMG's or the CMG's own FP when the attack names it, under the vehicle's id and the weapon's name.""",
       """            // D1.8 (pass 50): the BMG's or the CMG's own FP when the attack names it, under the vehicle's id and the weapon's name ("de-tank BMG").""")
S = "LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs"
e.edit(S, """            return $"{byVehicle.VehicleId}'s AAMG in {byVehicle.LocationId}";
""", """            // D3.5 (pass 50): a tank's BMG and CMG by name; a vehicle whose MA is its MG as before.
            return byVehicle.Weapons is { Count: > 0 } mgs
                ? $"{byVehicle.VehicleId}'s {string.Join(" and ", mgs.Select(item => (item.Weapon ?? string.Empty).ToUpperInvariant()))} in {byVehicle.LocationId}"
                : $"{byVehicle.VehicleId}'s AAMG in {byVehicle.LocationId}";
""")
e.edit(S, """                    records.Add((item.EventId, "repair", $"{repair.Unit}{In(before, repair.Unit)} repairs {repair.Equipment}: dr {dr} against R{repair.RepairNumber}: {result}"));
""", """                    // D3.7 (pass 50): a vehicle's weapon by its name; a 6 disables it, and a tank whose MA is disabled is Recalled.
                    records.Add((item.EventId, "repair", repair.Weapon is { } repairedWeapon
                        ? $"{repair.Unit}{In(before, repair.Unit)} tries to repair its {repairedWeapon.ToUpperInvariant()}: dr {dr}: "
                            + (repair.Result == RepairAttempted.Eliminated ? "disabled (a 6; D3.7)" + (repairedWeapon == "ma" ? ", and the vehicle is Recalled if it carries no Passengers and tows nothing" : "") : result)
                        : $"{repair.Unit}{In(before, repair.Unit)} repairs {repair.Equipment}: dr {dr} against R{repair.RepairNumber}: {result}"));
""")
P = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
e.edit(P, """            + (shots is null ? Marked(Conditions.PrepFire) || Marked(Conditions.FinalFire) ? ", fired this Player Turn" : ""
                : shots.RateOfFireKept ? $", {shots.Shots} shot(s), may fire again" : ", fired")
            + (acquired is null ? "" : $", Acquisition {acquired.Level} on {acquired.Location}")
            + (depleted.Length == 0 ? "" : $", out of {string.Join(", ", depleted)}") + flags + ")";
    }
""", """            // D3.5 (pass 50): a fire counter its BMG or CMG placed is not the MA's.
            + (shots is null ? Marked(Conditions.PrepFire) || Marked(Conditions.FinalFire)
                    ? state is not null && state.VehicleWeaponShots.Any(item => item.Vehicle == tank.Id) && state.VehicleWeaponShots.Where(item => item.Vehicle == tank.Id).All(item => item.Weapon != "ma")
                        ? ", its MGs have fired" : ", fired this Player Turn"
                    : ""
                : shots.RateOfFireKept ? $", {shots.Shots} shot(s), may fire again" : ", fired")
            + (acquired is null ? "" : $", Acquisition {acquired.Level} on {acquired.Location}")
            + (depleted.Length == 0 ? "" : $", out of {string.Join(", ", depleted)}") + flags + ")";
    }
""")
R = "LimboDancer.Domains.Asl.MapStudio/Components/Play/RepairActionPanel.razor"
e.edit(R, """        Malfunctioned SW or AAMG
""", """        Malfunctioned SW or vehicle weapon
""")
e.edit(R, """Choose the malfunctioned SW or AAMG.""", """Choose the malfunctioned SW or vehicle weapon.""")
e.apply()
