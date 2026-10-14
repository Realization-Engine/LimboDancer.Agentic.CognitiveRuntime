"""Put the Assemble summary back above Assemble, and give MgBoreSighted its own summary head."""
import sys

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs"
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
start = text.index("    /// <summary>\n    /// The attack of a fire group assembled (unit steps 18 to 23)")
end = text.index("    /// C6.44 (p. 174; pass 45)")
summary = text[start:end]
if not summary.rstrip().endswith("/// </summary>") or summary.count("\n") != 4:
    sys.exit("unexpected summary: " + repr(summary))
tail = "    /// <summary>\n    public static FireAttack Assemble(AttackAssemblyFacts facts)"
if text.count(tail) != 1:
    sys.exit("tail anchor")
text = text[:start] + "    /// <summary>\n" + text[end:]
text = text.replace(tail, summary + "    public static FireAttack Assemble(AttackAssemblyFacts facts)")
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
