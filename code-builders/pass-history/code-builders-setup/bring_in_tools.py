"""Bring the tools that built the ASL codebase into the repository, under code-builders (the user, 2026-10-13).

Until that day the tools lived outside the repository, in a folder named pass32-tools, by an early rule not to commit them. This script is
the one-time move: the reusable tools go to a folder for what they do, under a name that says it; the one-time scripts of each pass go to
pass-history, as they ran. Output, logs, the proof store, built copies of the sources, and copies of repository code are left behind.

Usage: python bring_in_tools.py <old tools folder> <repository root> [--write]     (without --write it only lists)
"""
import os
import sys

OLD, REPO = sys.argv[1].rstrip("/"), sys.argv[2].rstrip("/")
NEW = REPO + "/code-builders"
HOOKS = "C:/Users/dland/.claude/hooks"

# The reusable tools: where each was, and where it goes.
ACTIVE = {
    "gate/gate.sh": "merge-gate/gate.sh",
    "gate/pass50.conf": "merge-gate/pass-configs/pass50.conf",
    "gate/pass55.conf": "merge-gate/pass-configs/pass55.conf",
    "gate/new_properties.py": "merge-gate/new_properties.py",
    "gate/baseline_key.py": "merge-gate/baseline_key.py",
    "gate/pick_cuts.py": "merge-gate/pick_sweep_cuts.py",
    "gate/trial_check.py": "merge-gate/trial_check.py",
    "gate/replay_games.py": "merge-gate/compare_replay_digests.py",
    "gate/sweep_pairs.py": "merge-gate/compare_sweeps.py",
    "textlist.py": "merge-gate/text_list.py",
    "suite.sh": "merge-gate/run_test_suite.sh",
    "chart.sh": "merge-gate/chart_supplement.sh",
    "chart_compare.py": "merge-gate/chart_compare.py",
    "proof/Program.cs": "proof-tool/Program.cs",
    "proof/Proof.csproj": "proof-tool/Proof.csproj",
    "proof-main/Proof.csproj": "proof-tool/Proof.Baseline.csproj",
    "gate/tool_selftest.sh": "proof-tool/selftest.sh",
    "pass55/dump_state.sh": "diagnostics/dump_state.sh",
    "pass55/dump_events.sh": "diagnostics/dump_planned_events.sh",
    "pass50/dump_fire.sh": "diagnostics/dump_fire_plans.sh",
    "pass50/dump_fire_diff.py": "diagnostics/first_differing_fire_plan.py",
    "pass55/dump_diff.py": "diagnostics/first_difference_of_dumps.py",
    "pass45/facts_diff.py": "diagnostics/json_facts_difference.py",
    "pass55/sweep_where.py": "diagnostics/sweep_differences_by_game.py",
    "pass35/session3/replay_compare.py": "diagnostics/replay_differences_by_game.py",
    "pass55/store_count.py": "diagnostics/proof_store_contents.py",
    "pass45/trx_failed.py": "diagnostics/failed_tests_of_trx.py",
    "pass55/own_replay.sh": "diagnostics/replay_live_games.sh",
    "pass55/own_diag.py": "diagnostics/replay_diagnostics.py",
    "pass55/ref_cut.py": "diagnostics/cut_digest_to_games.py",
    "pass55/build_studio.sh": "build-and-test/build_studio.sh",
    "pass55/test.sh": "build-and-test/run_test_classes.sh",
    "pass50/fmt.sh": "build-and-test/format_whitespace.sh",
    "pass55/commit.sh": "build-and-test/commit_tracked.sh",
    "pass55/commit_code.sh": "build-and-test/commit_code.sh",
    "pass55/eol_2e.py": "build-and-test/check_line_endings.py",
    "pass55/last_message.py": "build-and-test/agent_last_message.py",
    "pass45/p45lib.py": "test-games/p45lib.py",
    "pass50/p50lib.py": "test-games/p50lib.py",
    "pass55/p55lib.py": "test-games/p55lib.py",
    "pass40/make_board_game.py": "test-games/make_board_game.py",
    "pass40/game_events.py": "test-games/game_events.py",
    "pass40/game_payloads.py": "test-games/game_payloads.py",
    "pass40/store_events.py": "test-games/proof_store_events.py",
    "pass45/game_read.py": "test-games/game_played_events.py",
    "pass45/event_read.py": "test-games/game_event.py",
    "pass55/shots.py": "test-games/game_shots.py",
    "pass55/agents/t3_dump.py": "test-games/game_labels_and_payloads.py",
    "pass35/counter_scan.py": "rulebook/counter_scan.py",
    "pass55/term_scan.py": "rulebook/term_scan.py",
    "pass55/dump_pages.py": "rulebook/dump_pages.py",
    "pass55/ctx.py": "rulebook/page_context.py",
    "pass55/render_page.py": "rulebook/render_page.py",
    "pass55/agents/f_render.py": "rulebook/render_page_clip.py",
    "pass50/edits.py": "documents/edit_files.py",
    "pass50/append.py": "documents/append_fragment.py",
    "pass55/docs/verify_inventory.py": "documents/verify_inventory_counts.py",
    "pass55/docs/rows.py": "documents/inventory_rows.py",
    "pass55/docs/eol.py": "documents/check_document_line_endings.py",
    "pass35/session3/docs_inventory_tables.py": "documents/inventory_count_tables.py",
    "pass35/session3/docs_coverage_counts.py": "documents/coverage_counts.py",
    "pass40/hexfacts.py": "boards/hex_facts.py",
    "pass45/board3_buildings.py": "boards/board3_buildings.py",
    "pass40/woods_road.py": "boards/woods_road_hexes.py",
    "pass40/board_version.py": "boards/board_version.py",
    "pass40/cards_survey.py": "boards/scenario_cards_survey.py",
}
HOOK_FILES = {"asl_bash_guard.py": "claude-hooks/asl_bash_guard.py", "asl_bash_guard_test.py": "claude-hooks/asl_bash_guard_test.py"}

# The one-time scripts: the folder of each pass, and what of the old folder goes there.
PASS = {
    "32": "pass-32-rules-migration", "35": "pass-35-repairs", "40": "pass-40-infantry-i",
    "45": "pass-45-infantry-ii", "50": "pass-50-armored-combat-i", "55": "pass-55-armored-combat-ii",
}
SCRIPT = (".py", ".sh", ".ps1", ".md", ".conf")
HISTORY_DIRS = {
    "pass35": ("35", ""), "pass35/session2": ("35", "session-2/"), "pass35/session3": ("35", "session-3/"),
    "pass40": ("40", ""), "pass45": ("45", ""), "pass50": ("50", ""),
    "pass55": ("55", ""), "pass55/agents": ("55", "research-agents/"), "pass55/docs": ("55", "documents-agent/"),
}
GATE_LEFTOVERS = {"gate/docs_checklist.py": "50", "gate/memory_index.py": "50", "gate/selftest.sh": "50", "gate/repair_tool.py": "55"}


def root_pass(name):
    for number in ("35", "40", "45", "50"):
        if name.startswith("gate" + number):
            return number
    return "32"


moves = []
for source, target in ACTIVE.items():
    moves.append((OLD + "/" + source, NEW + "/" + target, True))
for source, target in HOOK_FILES.items():
    moves.append((HOOKS + "/" + source, NEW + "/" + target, True))
taken = set(ACTIVE)
for name in sorted(os.listdir(OLD)):
    if os.path.isfile(OLD + "/" + name) and name not in taken and (name.endswith(SCRIPT) or name.endswith(".cs")):
        moves.append((OLD + "/" + name, f"{NEW}/pass-history/{PASS[root_pass(name)]}/{name}", False))
for folder, (number, sub) in HISTORY_DIRS.items():
    for name in sorted(os.listdir(OLD + "/" + folder)):
        source = folder + "/" + name
        if os.path.isfile(OLD + "/" + source) and source not in taken and name.endswith(SCRIPT):
            moves.append((OLD + "/" + source, f"{NEW}/pass-history/{PASS[number]}/{sub}{name}", False))
for source, number in GATE_LEFTOVERS.items():
    moves.append((OLD + "/" + source, f"{NEW}/pass-history/{PASS[number]}/after-the-gate/{os.path.basename(source)}", False))

targets = [target for _, target, _ in moves]
assert len(targets) == len(set(targets)), [t for t in targets if targets.count(t) > 1][:5]
missing = [source for source, _, _ in moves if not os.path.isfile(source)]
assert not missing, missing
size = sum(os.path.getsize(source) for source, _, _ in moves)
print(f"{len(moves)} files, {size / 1e6:.1f} MB: {sum(1 for m in moves if m[2])} reusable tools, {sum(1 for m in moves if not m[2])} one-time scripts")
if "--write" not in sys.argv:
    by_folder = {}
    for _, target, _ in moves:
        by_folder[os.path.dirname(target)[len(NEW) + 1:]] = by_folder.get(os.path.dirname(target)[len(NEW) + 1:], 0) + 1
    for folder in sorted(by_folder):
        print(f"  {by_folder[folder]:4d}  {folder}")
    sys.exit(0)
for source, target, active in moves:
    os.makedirs(os.path.dirname(target), exist_ok=True)
    with open(source, "rb") as f:
        data = f.read()
    if active:
        # A tool that is kept up: LF, no BOM. A one-time script is a record and keeps its bytes.
        data = data.replace(b"\r\n", b"\n")
        if data.startswith(b"\xef\xbb\xbf"):
            data = data[3:]
    with open(target, "wb") as f:
        f.write(data)
print("written under", NEW)
