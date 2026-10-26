"""Pass 71, 4c: the card tests' Act helpers place the Sniper counters a card game owes before any action but setup (ruling R71.11).

The gate holds every action but setup until the counters are placed (SetupOnlyBar), not only the phase's advance. Replaces the block's condition
written by card_snipers_helpers_4c.py; refuses a file where it is not found exactly once.
"""
import pathlib
import sys

D = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests")
FILES = ["BacklogPass20Tests", "BacklogPass20TablePlayerTests", "BacklogPass21Tests", "BacklogPass21TablePlayerTests", "BacklogPass24Tests",
         "BacklogPass25Tests", "BacklogPass26Tests"]
OLD = ("        // Pass 71 (A14.2; ruling R71.11): play starts from a card once each side that owes one has placed its Sniper counter.\n"
       "        if (action.Id == GameActions.AdvancePhase.Id)\n")
NEW = ("        // Pass 71 (A14.2; ruling R71.11): nothing but setup happens in a card's game until each side that owes one has placed its Sniper counter.\n"
       "        if (action.Id != GameActions.Setup.Id)\n")


def main():
    for name in FILES:
        path = D / f"{name}.cs"
        data = path.read_bytes().decode("utf-8")
        if data.count(OLD) != 1:
            print(f"{name}: block not found once")
            return 1
        path.write_bytes(data.replace(OLD, NEW).encode("utf-8"))
        print(f"{name}: done")
    return 0


if __name__ == "__main__":
    sys.exit(main())
