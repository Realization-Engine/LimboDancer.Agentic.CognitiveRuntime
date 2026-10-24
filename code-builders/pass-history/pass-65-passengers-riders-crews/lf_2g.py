"""Increment 2g: the new files to LF, and a check that no changed file gained a CR."""
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[3]
NEW = [
    "src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play/VehicleCrewActions.razor",
    "src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.CrewActs.cs",
    "src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1CrewActsRules.cs",
    "code-builders/pass-history/pass-65-passengers-riders-crews/games_2g.py",
    "code-builders/pass-history/pass-65-passengers-riders-crews/lf_2g.py",
]

for name in NEW:
    path = ROOT / name
    data = path.read_bytes()
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    path.write_bytes(data.replace(b"\r\n", b"\n"))
    print(name, "LF")

changed = subprocess.run(["git", "-C", str(ROOT), "diff", "--name-only"], capture_output=True, text=True).stdout.split()
for name in changed:
    path = ROOT / name
    if path.exists() and b"\r" in path.read_bytes():
        print("CR in", name)
