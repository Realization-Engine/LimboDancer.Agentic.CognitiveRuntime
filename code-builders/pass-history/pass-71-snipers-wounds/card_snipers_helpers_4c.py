"""Pass 71, 4c: the card tests' Act helpers place the Sniper counters a card game owes before the phase is advanced (ruling R71.11).

Inserts one block at the head of each file's Act helper; refuses a file whose helper is not found exactly once or that has the block already.
"""
import pathlib
import sys

D = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests")
FILES = ["BacklogPass20Tests", "BacklogPass20TablePlayerTests", "BacklogPass21Tests", "BacklogPass21TablePlayerTests", "BacklogPass24Tests",
         "BacklogPass25Tests", "BacklogPass26Tests"]
HEAD = "    private async Task<PlayResult> Act(Abstractions.Actions.ActionDescriptor action, object? arguments = null)\n    {\n"
BLOCK = ("        // Pass 71 (A14.2; ruling R71.11): play starts from a card once each side that owes one has placed its Sniper counter.\n"
         "        if (action.Id == GameActions.AdvancePhase.Id)\n"
         "        {\n"
         "            await CardSnipers.PlaceDue(Planner, () => store.Read(Scope)?.Events, sniper => Act(GameActions.Setup, sniper));\n"
         "        }\n"
         "\n")


def main():
    for name in FILES:
        path = D / f"{name}.cs"
        data = path.read_bytes().decode("utf-8")
        if "\r\n" in data:
            print(f"{name}: CRLF, skipped")
            return 1
        if data.count(HEAD) != 1 or "CardSnipers.PlaceDue" in data:
            print(f"{name}: helper not found once, or done already")
            return 1
        path.write_bytes(data.replace(HEAD, HEAD + BLOCK).encode("utf-8"))
        print(f"{name}: done")
    return 0


if __name__ == "__main__":
    sys.exit(main())
