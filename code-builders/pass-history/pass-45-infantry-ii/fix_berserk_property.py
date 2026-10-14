"""Move the Berserk fact from the DC record, where the first script put it, to the firer."""
import sys

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1FireModels.cs"
BLOCK = """    /// <summary>Whether the firer is berserk (A7.54, A7.9; pass 45): it joins no multi-Location group and never cowers; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

"""
ANCHOR = """    /// <summary>
    /// Whether a Green or Conscript firer is Inexperienced (A19.2), for the Heat of Battle DRM of an FPF NMC, and for its FT's removal number, one lower
"""
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
if text.count(BLOCK) != 1 or text.count(ANCHOR) != 1:
    sys.exit(f"anchors: {text.count(BLOCK)} {text.count(ANCHOR)}")
text = text.replace(BLOCK, "").replace(ANCHOR, BLOCK + ANCHOR)
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
