"""Pass 50, task 50.3: what the Studio games showed: a Delay leaves the vehicle stopped; the order of the Bounding First Fire refusals; the words."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
P = "LimboDancer.Domains.Asl.Units/State/GameProjector.cs"
e.edit(P, """            // D2.17 (pass 50): a Delay MP leaves the vehicle as it was, stopped or not yet started.
            var keeps = step.Kind is VehicleStepped.Remain or VehicleStepped.Delay;
""", """            // D2.17 (pass 50): a Delay MP is spent by a vehicle that is stopped or has not started, and leaves it stopped: it does not end its
            // move in Motion (D2.4).
""")
e.edit(P, """                    Started = keeps ? current?.Started ?? false : step.Kind != VehicleStepped.Stop,
                    Stopped = keeps ? current?.Stopped ?? false : step.Kind == VehicleStepped.Stop,
""", """                    Started = step.Kind == VehicleStepped.Remain ? current!.Started : step.Kind is not (VehicleStepped.Stop or VehicleStepped.Delay),
                    Stopped = step.Kind == VehicleStepped.Remain ? current!.Stopped : step.Kind is VehicleStepped.Stop or VehicleStepped.Delay,
""")
E = "LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceEligibility.cs"
e.edit(E, """    public static string? BoundingShotBar(string vehicleId, int? lastShotStep, int stepNow) =>
        lastShotStep is { } last && stepNow <= last
""", """    public static string? BoundingShotBar(string vehicleId, int? lastShotStep, int stepNow, bool rofSpent = false) =>
        // A vehicle under a Bounding Fire counter has spent its ROF: the calculator says so, in its own words.
        !rofSpent && lastShotStep is { } last && stepNow <= last
""")
L = "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs"
e.edit(L, """item.Weapon == "ma" && item.FirePhase == "mph")?.Step, state.Movement?.Step ?? 0) is { } mpBar)""",
       """item.Weapon == "ma" && item.FirePhase == "mph")?.Step, state.Movement?.Step ?? 0,
                    Is(gun, Conditions.BoundingFire)) is { } mpBar)""")
R = "LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs"
e.edit(R, """                VehicleStepped { Kind: not VehicleStepped.Turn } drive => $"{drive.Vehicle} moves{From(before, drive.Vehicle)} to {drive.At}",
""", """                // D2.17, D3.12 (pass 50): a Delay MP, and a turret turned with an MP expenditure.
                VehicleStepped { Kind: VehicleStepped.Delay } delay => $"{delay.Vehicle} spends a Delay MP in {delay.At} (D2.17)"
                    + (delay.Turret is { } delayTurret ? $", turning its turret to {UnitFacings.Name(delayTurret)} (D3.12)" : string.Empty),
                VehicleStepped { Kind: not VehicleStepped.Turn } drive => $"{drive.Vehicle} moves{From(before, drive.Vehicle)} to {drive.At}"
                    + (drive.Turret is { } driveTurret ? $", turning its turret to {UnitFacings.Name(driveTurret)} (D3.12)" : string.Empty),
""")
PAGE = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
e.edit(PAGE, """                                        MainArmament="@LiveOrdnance.IsTank(chosen)" Vehicles""",
       """                                        MainArmament="@(LiveOrdnance.IsTank(chosen) && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True)" Vehicles""")
e.apply()
