"""Increment 3a: remove GamePlanner.Map.cs's EnemyScan and its three reads, which GamePlanner.Sight.cs replaces (ruling R70.16)."""
import pathlib

path = pathlib.Path(r"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Map.cs")
text = path.read_bytes().decode("utf-8")
start = text.index("    /// <summary>\n    /// The units of the game as the scans for a seeing enemy read them (pass 32.b)")
end = text.rindex("}\n")
cut = text[start:end]
assert "private bool EnemyGoodOrderInLosWithin16" in cut and cut.count("class EnemyScan") == 1, "unexpected block"
text = text[:start].rstrip("\n") + "\n}\n"
path.write_bytes(text.encode("utf-8"))
print("cut", cut.count("\n"), "lines")
