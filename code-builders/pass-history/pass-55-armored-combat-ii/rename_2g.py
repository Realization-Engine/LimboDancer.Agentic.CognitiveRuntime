"""Increment 2g: the Gun Duel tests' hex constant gets a name of its own (the file already has a GunHex), LF kept."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass55Tests.cs"
with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
    text = handle.read()
mark = "    // Task 55.7 (increment 2g)"
head, tail = text.split(mark)
tail = tail.replace("SecondGunHex", "SecondDuelHex").replace("GunHex", "DuelHex")
with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(head + mark + tail)
print(tail.count("DuelHex"))
