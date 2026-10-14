"""Imported by every Python tool of code-builders: where the tools, the repository, and the working folder are.

TOOLS  this folder
REPO   the repository's root
WORK   the working folder: the proof store, baselines, digests, sweeps, logs, and the built copy of the baseline's sources. It is
       outside the repository, on a short path, and nothing in it is committed. Set ASL_GATE_WORK to use another folder.
"""
import os

TOOLS = os.path.dirname(os.path.abspath(__file__)).replace("\\", "/")
REPO = os.path.dirname(TOOLS)
WORK = os.environ.get("ASL_GATE_WORK", "E:/Archive/GitHub/dlandi/asl-gate-work").replace("\\", "/").rstrip("/")
# The registered rulebook PDF is not in the repository. Set ASL_RULEBOOK_PDF where it is kept elsewhere.
RULEBOOK_PDF = os.environ.get(
    "ASL_RULEBOOK_PDF",
    "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf")
