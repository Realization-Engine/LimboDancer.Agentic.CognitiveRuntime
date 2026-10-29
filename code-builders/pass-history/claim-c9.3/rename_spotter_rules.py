"""The C9.3 claim (ruling RC93.1): the four Turns spotter rules are folded into ScenarioA1SpotterCalculator and their types lose the Turn prefix.
Rewrites the references in the C# sources and tests under src/ASL. Prints each file changed.

    python rename_spotter_rules.py            dry run
    python rename_spotter_rules.py --apply
"""
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "src", "ASL")

# Longest names first, so that TurnSpotterUseRules is not read as TurnSpotterUse.
RENAMES = [
    (r"\bTurnSpotterTimingRules\.Evaluate\b", "ScenarioA1SpotterCalculator.Timing"),
    (r"\bTurnSpotterDesignationRules\.Refusal\b", "ScenarioA1SpotterCalculator.DesignationRefusal"),
    (r"\bTurnSpotterUseRules\.Compatible\b", "ScenarioA1SpotterCalculator.SharesWith"),
    (r"\bTurnSpotterDesignationRules\b", "ScenarioA1SpotterCalculator"),
    (r"\bTurnSpotterLossRules\b", "ScenarioA1SpotterCalculator"),
    (r"\bTurnSpotterTimingRules\b", "ScenarioA1SpotterCalculator"),
    (r"\bTurnSpotterUseRules\b", "ScenarioA1SpotterCalculator"),
    (r"\bTurnSpotterDesignationFacts\b", "SpotterDesignationFacts"),
    (r"\bTurnSpotterTimingFacts\b", "SpotterTimingFacts"),
    (r"\bTurnSpotterTimingVerdict\b", "SpotterTimingVerdict"),
    (r"\bTurnSpotterTimingState\b", "SpotterTimingState"),
    (r"\bTurnSpotterChange\b", "SpotterChange"),
    (r"\bTurnSpotterUse\b", "SpotterUse"),
]
SKIP = ("ScenarioA1SpotterCalculator.cs", "ScenarioA1SpotterModels.cs")


def main():
    apply = "--apply" in sys.argv
    for folder, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in ("bin", "obj", "boards")]
        for name in files:
            if not name.endswith((".cs", ".razor")) or name in SKIP:
                continue
            path = os.path.join(folder, name)
            text = open(path, encoding="utf-8", newline="").read()
            changed = text
            for pattern, replacement in RENAMES:
                changed = re.sub(pattern, replacement, changed)
            if changed != text:
                print(os.path.relpath(path, ROOT))
                if apply:
                    open(path, "w", encoding="utf-8", newline="").write(changed)


if __name__ == "__main__":
    main()
