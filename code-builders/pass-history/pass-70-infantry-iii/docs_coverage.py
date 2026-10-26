"""Pass 70 documents: the coverage's text beside the sections the pass changed, A12's and A17's status (Mostly built by section 1's rule), the
chapter table's A row and total, and the status paragraph. The count cells are coverage_counts.py's. Every anchor is checked before any file is written."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders")
from edit_files import Edits  # noqa: E402

C = "docs/ASL Rule Coverage.md"
e = Edits()
S = " **Pass 70 (2026-10-25):** "
e.edit(C, "a pin on a Rider by a hit that is not the IFT's. | 47, 50, 51; ruling |",
       "a pin on a Rider by a hit that is not the IFT's." + S + "TPBF at PRC leaves them in an enemy Location, not held in Melee, said on the "
       "proposal (7.211; R70.32, a deviation); its fire record line is backlog 72.1. | 47, 50, 51; ruling |")
e.edit(C, "a Fanatic's Interdiction NMC is right; no voluntary rout without DM (R35.4). | 37, 45, 48 |",
       "a Fanatic's Interdiction NMC is right; no voluntary rout without DM (R35.4)." + S + "Voluntary Break (10.41) and Voluntary Rout (10.711) "
       "are built (R70.11, R70.12, R70.43); vehicles and Guns Interdict (R70.14); a routing unit may ignore its own building (10.51's EXC, "
       "R70.13); Fanaticism by SSR (R70.15); a LLMC follows a leader lost outside fire, through one Morale Check member (10.2; R70.3, R70.44); a "
       "concealed unit may give up its \"?\" (10.533; R70.20). Left: the fortification rout options (pass 115). | 37, 45, 48 |")
e.edit(C, "PRC in CC (11.611, 11.71) are decided in Rules and not yet played (pass 70). | 48, 51, 53, 56, 63; ruling |",
       "PRC in CC (11.611, 11.71) are decided in Rules and not yet played (pass 70)." + S + "Infantry against Infantry in a Location holding a "
       "vehicle is fought in the vehicle sequence, with its Ambush and the vehicle drm (11.31, 11.34, 11.4; R70.30); CC with a Gun's crew and its "
       "Gun's loss (11.13; R70.27); withdrawal above the IPC and into a concealed Location (11.21; R70.26); PRC in CC in Play (11.611, 11.71; "
       "R70.31); Street Fighting (11.8; R70.33, its Reaction Fire pass 105); the capture of Passengers (11.52; R70.34, a reading for the user). "
       "Left: Pillbox CC (pass 115), withdrawal to another level (pass 135). | 48, 51, 53, 56, 63; ruling |")
e.edit(C, "| A12 | Concealment | Partly |", "| A12 | Concealment | Mostly built |")
e.edit(C, "is Good Order, not hidden, and not a Passenger (R35.5). | 37, 48 |",
       "is Good Order, not hidden, and not a Passenger (R35.5)." + S + "one read of who sees for every caller (R70.16); the Right of Inspection "
       "by LOS in every view (12.16; R70.18, R70.19, R70.40); the actions that cost a \"?\" (12.141; R70.17); voluntary removal and the viewer's "
       "option on a move (12.14; R70.20, R70.21); \"?\" for an Emplaced Gun's crew and for vehicles, HIP for vehicles and an Emplaced Gun out of "
       "Concealment Terrain, reinforcements under a non-OB \"?\" (12.12, 12.121, 12.2, 12.3, 12.34; R70.22, R70.35, R70.36); Bypass endings "
       "(12.151, 12.42; R70.23, R70.37); Searching (12.152; R70.24, R70.39, R70.42); Mopping Up in any building (12.153; R70.25). Now Mostly "
       "built. Left: the loss forced outside the move (R31d.2), mines and Booby Traps (pass 120), Fortifications (pass 115). | 37, 48 |")
e.edit(C, "no return to normal after AFPh TPBF (15.46); Unarmed units and PRC not exempt (15.1) | 49 |",
       "no return to normal after AFPh TPBF (15.46); Unarmed units and PRC not exempt (15.1)." + S + "Unarmed units and PRC are exempt, and a "
       "MC outside Fire and Rally calls Heat of Battle through the Morale Check member (15.1; R70.3, R70.7); Battle Hardening is always taken "
       "there (backlog 72.1). | 49 |")
e.edit(C, "| A17 | Wounds | Partly |", "| A17 | Wounds | Mostly built |")
e.edit(C, "the Wound Severity dr is made in every path. | 45, 49 |",
       "the Wound Severity dr is made in every path." + S + "a SMC Rider's Casualty Reduction in a Bail Out takes its dr (the crash of a null "
       "severity repaired), through the Morale Check member (17.1; R70.3, R70.9). Now Mostly built. | 45, 49 |")
e.edit(C, "No Leader Creation at all in CC against a vehicle (18.12), and so no drm for it | 49 |",
       "No Leader Creation at all in CC against a vehicle (18.12), and so no drm for it." + S + "Leader Creation in CC against a vehicle is "
       "built (18.12; R70.34, R70.46). | 49 |")
e.edit(C, "a Disrupted unit routs only when it must, and never by Low Crawl. | 45, 49 |",
       "a Disrupted unit routs only when it must, and never by Low Crawl." + S + "an Unarmed unit is never Replaced (19.11; R70.7); PRC are "
       "never Disrupted (19.12). | 45, 49 |")
e.edit(C, "a vehicle's FG with its Passengers (pass 105); Bypass (pass 100). | 51 |",
       "a vehicle's FG with its Passengers (pass 105); Bypass (pass 100)." + S + "PRC in CC and D6.651's reach in Play (R70.8, R70.31); the Bail "
       "Out NMC's leadership, wound, and ELR (6.24; R70.9); a Carrier HS recombines with no leader (6.82; R70.28). | 51 |")
e.edit(C, "| A. Infantry and basic rules | 26 | 0 | 17 | 6 | 1 | 0 | 2 |", "| A. Infantry and basic rules | 26 | 0 | 19 | 4 | 1 | 0 | 2 |")
e.edit(C, "| **A to E** | **105** | **0** | **30** | **23** | **25** | **0** | **27** |", "| **A to E** | **105** | **0** | **32** | **21** | **25** | **0** | **27** |")
e.edit(C, "A5, A21, D5, D6, and D10 are now Mostly built, and B13 and D9 Partly.",
       "A5, A21, D5, D6, and D10 are now Mostly built, and B13 and D9 Partly. **Pass 70 (2026-10-25, on its branch)** changed 80 rows of the "
       "inventory (48 of its own and 32 that its code touched) and the counts of eight sections (A7, A10, A11, A12, A17, A18, A19, D6), and added "
       "its sentence to those rows and to A15; A12 and A17 are now Mostly built.")
e.apply()
