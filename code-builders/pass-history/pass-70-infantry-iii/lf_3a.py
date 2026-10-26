"""Increment 3a: the files it wrote or changed as LF, UTF-8 without BOM (the brief); prints each file it rewrote and any changed file holding a CR."""
import pathlib
import subprocess

REPO = pathlib.Path(r"E:/Archive/GitHub/dlandi/LimboDancer.MCP")
NEW = [
    "src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Sight.cs",
    "src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1Sight.cs",
    "src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPagePass70Tests.cs",
    "src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass70Tests.cs",
    "src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass70RulesTests.cs",
    "code-builders/pass-history/pass-70-infantry-iii/games_3a.py",
    "code-builders/pass-history/pass-70-infantry-iii/cut_enemy_scan.py",
    "code-builders/pass-history/pass-70-infantry-iii/lf_3a.py",
]
changed = subprocess.run(["git", "-C", str(REPO), "diff", "--name-only"], capture_output=True, text=True, check=True).stdout.split()
for name in NEW + changed:
    path = REPO / name
    data = path.read_bytes()
    fixed = data.removeprefix(b"\xef\xbb\xbf").replace(b"\r\n", b"\n")
    if name in NEW and fixed != data:
        path.write_bytes(fixed)
        print("rewrote", name)
    elif b"\r" in data:
        print("CR in changed file", name)
