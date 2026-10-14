"""Pass 45 documents: the backlog rows of section 56 that the pass built, fixed, or moved on, each marked in its last cell."""
P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog.md"
MARKS = {
    "| A hero lending his IPC; the leader's IPC as a choice in the APh |": "**Not built in pass 45 (design 11.3); moved to pass 75, with a row in section 57.1.**",
    "| The German dismantled MMG or HMG firing as a LMG; INF and RCL SW |": "**Built in pass 45 for the German dm MMG and HMG (task 45.7): it fires as a LMG, and so in the AFPh after moving. INF and RCL SW wait on the catalog.**",
    "| Hindrance within the Location in the NTC's DRM; minefields, FFE, and Wire among the SMC's Locations |": "**Not touched by pass 45; passes 75 and 120.**",
    "| One Mandatory FG of units whose fire counters differ |": "**Built in pass 45 (task 45.8, ruling R45.1): each firer fires by its own counter, so every unit that owes the attack makes it.**",
    "| A Gun's shot, a thrown DC, and a Starshell held behind the attack owed |": "**Built in pass 45 (task 45.8, ruling R45.2).**",
    "| An FPF whose NMC replaces a firer by ELR is refused at the commit |": "**Fixed in pass 45 (task 45.8): a firer its own NMC eliminates, Reduces, or Replaces takes no fire marker.**",
    "| A CX leader still lends his IPC (older than the pass) |": "**Fixed in pass 45 (task 45.10): a leader who is CX, or Double Times with his stack, lends none.**",
    "| A SMC's Recovery at once when a unit is eliminated, surrenders, or routs away | Pass 40 (design 9.4) |": "**Not built in pass 45 (design 11.3); moved to pass 75, with a row in section 57.1.**",
}
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
for start, mark in MARKS.items():
    found = [index for index, line in enumerate(lines) if line.startswith(start)]
    assert len(found) == 1, (start, len(found))
    assert "pass 45" not in lines[found[0]].split("|")[-2], start
for start, mark in MARKS.items():
    index = next(index for index, line in enumerate(lines) if line.startswith(start))
    lines[index] = lines[index].rstrip()[:-1].rstrip() + " " + mark + " |"
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines))
print("marked", len(MARKS))
