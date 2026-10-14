"""After point_tools_at_paths.py: the references between tools, which the first script could not know (the user, 2026-10-13).

Each tool names its helpers where they now are; work files get names that say what they hold, not which pass first made them; the
commit helpers take their branch from the caller; the rulebook's path is one setting. Three diagnostics that are bound to one pass's
builds go to that pass's history. Every anchor is checked before anything is written.

Usage: python rewire_tools.py <repository root>
"""
import os
import sys

NEW = sys.argv[1].rstrip("/") + "/code-builders"
PDF = '"E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"'
IMPORT = "from paths import REPO, WORK  # noqa: E402"

# file: [(old, new, count or None for one or more)]
EDITS = {
    "merge-gate/gate.sh": [
        ("#   bash gate/gate.sh <pass> <base commit> <stage>", "#   bash code-builders/merge-gate/gate.sh <pass> <base commit> <stage>", 1),
        ('S="$WORK"\n', 'S="$WORK"\nG="$TOOLS/merge-gate"\n', 1),
        ('if [ ! -f "$S/gate/pass$PASS.conf" ]; then echo "STOP: write $S/gate/pass$PASS.conf first', 'if [ ! -f "$G/pass-configs/pass$PASS.conf" ]; then echo "STOP: write $G/pass-configs/pass$PASS.conf first', 1),
        ('source "$S/gate/pass$PASS.conf"', 'source "$G/pass-configs/pass$PASS.conf"', 1),
        ('"$S/gate/pick_cuts.py"', '"$G/pick_sweep_cuts.py"', 1),
        ('"$S/gate/.main-files"', '"$S/.main-files"', None),
        ('"$S/gate/.head-files"', '"$S/.head-files"', None),
        ('"$S/gate/strict-ok"', '"$S/strict-ok"', None),
        ('"$S/gate/new_properties.py"', '"$G/new_properties.py"', 1),
        ('"$S/gate/baseline_key.py"', '"$G/baseline_key.py"', 1),
        ('"$S/gate/trial_check.py"', '"$G/trial_check.py"', 1),
        ('"$S/gate/replay_games.py"', '"$G/compare_replay_digests.py"', 1),
        ('"$S/gate/sweep_pairs.py"', '"$G/compare_sweeps.py"', 1),
        ('"$S/textlist.py"', '"$G/text_list.py"', 2),
        ('bash "$S/suite.sh"', 'bash "$G/run_test_suite.sh"', 1),
        ('bash "$S/chart.sh"', 'bash "$G/chart_supplement.sh"', 1),
        ('  dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/"',
         '  mkdir -p "$S/proof"; cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof/"\n  dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/"', 1),
        ('  cp "$S/proof/Program.cs" "$S/proof-main/Program.cs"\n  dotnet build "$S/proof-main/Proof.csproj" --disable-build-servers',
         '  mkdir -p "$S/proof-main"; cp "$TOOLS/proof-tool/Program.cs" "$S/proof-main/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$S/proof-main/Proof.csproj"\n  dotnet build "$S/proof-main/Proof.csproj" "-p:StudioBin=$S/main-bin/" --disable-build-servers', 1),
        ('echo "A project that failed: python pass45/trx_failed.py suite/<project>.trx with PYTHONIOENCODING=utf-8"',
         'echo "A project that failed: python code-builders/diagnostics/failed_tests_of_trx.py $S/suite/<project>.trx with PYTHONIOENCODING=utf-8"', 1),
        ("# The merge gate of a rule pass, one script for every pass (written after pass 50; repaired after pass 55's gate, 2026-10-13).",
         "# The merge gate of a rule pass, one script for every pass (written after pass 50; repaired after pass 55's gate, 2026-10-13).\n"
         "# The script and its helpers are in the repository; what they write goes to the working folder that paths.sh names.", 1),
    ],
    "merge-gate/baseline_key.py": [
        (IMPORT, "from paths import TOOLS, WORK  # noqa: E402", 1),
        ('S + "/proof/Program.cs"', 'TOOLS + "/proof-tool/Program.cs"', 1),
    ],
    "merge-gate/chart_supplement.sh": [
        ("SW='E:\\Archive\\GitHub\\dlandi\\pass32-tools\\chart'\nmkdir -p \"$S\"", 'mkdir -p "$S"\nSW="$(cygpath -w "$S")"', 1),
        ('python "$S/../chart_compare.py"', 'python "$TOOLS/merge-gate/chart_compare.py"', 1),
        ("cd $R || exit 1", 'cd "$R" || exit 1', 1),
    ],
    "merge-gate/run_test_suite.sh": [("cd $R || exit 1", 'cd "$R" || exit 1', 1)],
    "proof-tool/Proof.Baseline.csproj": [("dlandi\\pass32-tools\\main-bin\\", "dlandi\\asl-gate-work\\main-bin\\", 1)],
    "proof-tool/Program.cs": [
        ('var boardsRoot = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards";',
         '// ASL_REPO and ASL_VASL_ROOT (code-builders/paths.sh) say where the repository and the VASL checkout are; the defaults are this machine\'s.\n'
         'var repository = (Environment.GetEnvironmentVariable("ASL_REPO") is { Length: > 0 } given ? given : "E:/Archive/GitHub/dlandi/LimboDancer.MCP").Replace(\'\\\\\', \'/\').TrimEnd(\'/\');\n'
         'var boardsRoot = repository + "/src/ASL/boards";', 1),
        ('    VaslRoot = "E:/Archive/GitHub/dlandi/vasl",', '    VaslRoot = Environment.GetEnvironmentVariable("ASL_VASL_ROOT") is { Length: > 0 } vasl ? vasl : "E:/Archive/GitHub/dlandi/vasl",', 1),
        ('    OracleFixtures = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle",',
         '    OracleFixtures = repository + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle",', 1),
    ],
    "diagnostics/dump_planned_events.sh": [
        ("#   dump_events.sh build         both tools rebuilt, each copy checked against its build\n#   dump_events.sh run <game>    the texts go to dbg55/main.txt and dbg55/gate.txt",
         "#   dump_planned_events.sh build                      both tools rebuilt, each copy checked against its build\n"
         "#   dump_planned_events.sh run <game> <base commit>   the texts go to dump-events/main.txt and dump-events/gate.txt in the working folder", 1),
        ('  cp "$S/proof/Program.cs" "$S/proof-main/Program.cs"',
         '  mkdir -p "$S/proof" "$S/proof-main"; cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof/"\n'
         '  cp "$TOOLS/proof-tool/Program.cs" "$S/proof-main/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$S/proof-main/Proof.csproj"', 1),
        ('dotnet build "$S/proof-main/Proof.csproj" --disable-build-servers', 'dotnet build "$S/proof-main/Proof.csproj" "-p:StudioBin=$S/main-bin/" --disable-build-servers', 1),
        ('  NEW=$(python "$S/gate/new_properties.py" 846eb5a3 < /dev/null)', '  NEW=$(python "$TOOLS/merge-gate/new_properties.py" "$3" < /dev/null) || exit 1', 1),
        ("dbg55", "dump-events", None),
    ],
    "build-and-test/build_studio.sh": [("LOG=$WORK/pass55/build_studio.log", "LOG=$WORK/build-studio.log", 1)],
    "build-and-test/run_test_classes.sh": [
        ("# Run test classes of one test project into a folder of its own: test.sh <Rules|Play|Units|MapStudio> <filter> [more dotnet test args]",
         "# Run test classes of one test project into a folder of its own: run_test_classes.sh <Rules|Play|Units|MapStudio> <filter> [more dotnet test args]\n"
         "# It builds with warnings NOT as errors, to keep a filtered run short: it says nothing about the strict build (merge-gate/gate.sh strict).", 1),
        ("OUT=$WORK/pass55/trx", "OUT=$WORK/test-classes", 1),
        ("-p:BaseOutputPath=bin/p55/", "-p:BaseOutputPath=bin/classes/", 1),
        ("python $WORK/pass45/trx_failed.py", 'python "$TOOLS/diagnostics/failed_tests_of_trx.py"', 1),
    ],
    "build-and-test/commit_tracked.sh": [
        ("commit.sh <message file>", "ASL_PASS_BRANCH=<branch> commit_tracked.sh <message file>", 1),
        ('if [ "$BRANCH" != "feature/asl-backlog-pass-55" ]; then echo "wrong branch: $BRANCH"; exit 1; fi',
         'if [ -z "$ASL_PASS_BRANCH" ] || [ "$BRANCH" != "$ASL_PASS_BRANCH" ]; then echo "the branch is $BRANCH, and ASL_PASS_BRANCH says \'$ASL_PASS_BRANCH\'"; exit 1; fi', 1),
        ("LIST=$WORK/pass55/paths.txt", "LIST=$WORK/commit-paths.txt", 1),
    ],
    "build-and-test/commit_code.sh": [
        ("commit_code.sh <message file>", "ASL_PASS_BRANCH=<branch> commit_code.sh <message file>", 1),
        ('if [ "$BRANCH" != "feature/asl-backlog-pass-55" ]; then echo "wrong branch: $BRANCH"; exit 1; fi',
         'if [ -z "$ASL_PASS_BRANCH" ] || [ "$BRANCH" != "$ASL_PASS_BRANCH" ]; then echo "the branch is $BRANCH, and ASL_PASS_BRANCH says \'$ASL_PASS_BRANCH\'"; exit 1; fi', 1),
        ("LIST=$WORK/pass55/paths.txt", "LIST=$WORK/commit-paths.txt", 1),
    ],
    "build-and-test/format_whitespace.sh": [
        ("fmt.sh <project folder name>", "format_whitespace.sh <project folder name>", 1),
        ("cd $R/src/ASL || exit 1", 'cd "$R/src/ASL" || exit 1', 1),
        ("git -C $R diff", 'git -C "$R" diff', 1),
    ],
    "rulebook/dump_pages.py": [
        ('"""Dump PDF pages to text files in pass55/pages: dump_pages.py <page>..."""', '"""Dump pages of the rulebook PDF to text files, one a page, in the working folder\'s rulebook-pages: dump_pages.py <page>..."""', 1),
        (IMPORT, "from paths import RULEBOOK_PDF, WORK  # noqa: E402", 1),
        ("PDF = " + PDF, "PDF = RULEBOOK_PDF", 1),
        ('OUT = WORK + "/pass55/pages"', 'OUT = WORK + "/rulebook-pages"', 1),
    ],
    "rulebook/page_context.py": [
        ("ctx.py <regex>", "page_context.py <regex>", 1),
        ('T = WORK + "/pass35/session3/pages/p%03d.txt"', 'T = WORK + "/rulebook-pages/p%03d.txt"', 1),
    ],
    "rulebook/render_page_clip.py": [('WORK + "/pass55/agents"', 'WORK + "/page-renders"', 1)],
}
TO_HISTORY = {
    "diagnostics/dump_state.sh": "pass-history/pass-55-armored-combat-ii/dump_state.sh",
    "diagnostics/replay_live_games.sh": "pass-history/pass-55-armored-combat-ii/own_replay.sh",
    "diagnostics/dump_fire_plans.sh": "pass-history/pass-50-armored-combat-i/dump_fire.sh",
    "diagnostics/first_differing_fire_plan.py": "pass-history/pass-50-armored-combat-i/dump_fire_diff.py",
}

out = {}
for name, edits in EDITS.items():
    with open(NEW + "/" + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new, count in edits:
        found = text.count(old)
        assert (found >= 1) if count is None else (found == count), (name, old[:70], found)
        text = text.replace(old, new)
    out[name] = text
# The rulebook's path: one setting in paths.py, for every tool that reads the PDF.
for folder in ("rulebook", "documents", "boards", "test-games", "diagnostics", "build-and-test"):
    for name in sorted(os.listdir(NEW + "/" + folder)):
        key = folder + "/" + name
        if not name.endswith(".py") or key == "rulebook/dump_pages.py":
            continue
        text = out.get(key)
        if text is None:
            with open(NEW + "/" + key, encoding="utf-8", newline="") as f:
                text = f.read()
        if PDF in text:
            assert IMPORT in text or "import fitz" in text, key
            text = text.replace(PDF, "RULEBOOK_PDF")
            if IMPORT in text:
                text = text.replace(IMPORT, "from paths import REPO, RULEBOOK_PDF, WORK  # noqa: E402")
            else:
                at = text.index("import ")
                text = (text[:at] + 'import os as _os\nimport sys as _sys\n_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))\n'
                        "from paths import RULEBOOK_PDF  # noqa: E402\n" + text[at:])
            out[key] = text
            print("rulebook path:", key)
for old in TO_HISTORY:
    assert os.path.isfile(NEW + "/" + old), old
for name, text in out.items():
    with open(NEW + "/" + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
for old, new in TO_HISTORY.items():
    os.replace(NEW + "/" + old, NEW + "/" + new)
print("rewired:", len(out), "files; to history:", len(TO_HISTORY))
