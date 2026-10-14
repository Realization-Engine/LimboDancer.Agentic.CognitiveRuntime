"""Pass 45, task 45.7: the dismantle action's name and description say what it now dismantles."""
PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Play/GameActions.cs"
OLD = """    public static readonly ActionDescriptor Dismantle = Descriptor("asl.game.dismantle", "Dismantle or assemble a MG",
        "Its possessor dismantles or assembles the German MMG in its side's PFPh or DFPh, if the MG has not fired in it; this counts as the MG's use. A dismantled MG is not fired and portages at half its PP (A9.8; ruling R13.6).",
"""
NEW = """    public static readonly ActionDescriptor Dismantle = Descriptor("asl.game.dismantle", "Dismantle or assemble a SW",
        "Its possessor dismantles or assembles a non-Russian MMG or HMG, or a light mortar of four PP or more, in its side's PFPh or DFPh, if the weapon has not fired in it; this counts as the weapon's use. A dismantled weapon portages at half its PP and is not fired, but a German MMG or HMG, which fires as a LMG (A9.8).",
"""
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
assert text.count(OLD) == 1
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text.replace(OLD, NEW))
print("ok")
