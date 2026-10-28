# Instructions for coding agents

This repository has standing rules that every agent follows, whatever tool it runs in. Read these documents before changing anything under `src/ASL`; they are the authority, and this file only points to them:

1. [Pass Standing Rules and Harness Lessons](<src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md>): the checklist (section 0), the standing rules (section 2), where things are (section 3), and the lessons of past work (section 4).
2. [Conventions of the Rules project](src/ASL/LimboDancer.Domains.Asl.Rules/CONVENTIONS.md): how ASL rules are written.
3. [ASL Memory and Performance Guidelines](<src/ASL/docs/Memory-And-Performance/ASL Memory and Performance Guidelines.md>): every change follows them, and every plan answers their checklist.

## The rule most often missed

**Every ASL rule lives in the Rules project alone** (`src/ASL/LimboDancer.Domains.Asl.Rules`). A rule is a calculator there that takes facts and returns a verdict. Play reads the state and the map, hands the facts over, and writes the events; it decides no rule. The Units projector asks the same calculators and applies their answers. Rules references Abstractions only, never Units, Maps, or Play. Any new project (for example `ASL.Turns`) is bound the same way: it gathers facts and presents verdicts; it does not decide legality, eligibility, timing, outcomes, or thresholds itself.

A sign that a rule has been written in the wrong place: code outside Rules that cites a rule number (A1.31, C9.3, G10.11) while computing the decision itself.

## Other rules that apply to every change

- **The rulebook first.** Read the rulebook PDF before stating or coding a rule, and cite the rule and its page. Search the whole rulebook before saying what a counter or rule needs.
- **How a change reaches main.** Code, project files, lock files, the workflows, anything under `code-builders`, and any commit that adds, removes, or renames a file go through a pull request and a green CI run. Only edits to the text of existing documents go straight to main. `bash code-builders/build-and-test/how_to_reach_main.sh` says which.
- **The merge gate.** `code-builders/merge-gate/gate.sh` proves that older games replay and sweep as before. Its stages and configuration are described in [code-builders/README.md](code-builders/README.md).
- **Staging.** Stage explicit paths only. Never `git add -A` or `git add src/ASL`; never add `src/ASL/boards/` or `.claude/`. No force push.
- **Unit tests.** Run only the test methods that assert the changed behavior; the full suite waits for the gate and CI.
- **Lock files.** Restores run in locked mode. A new project reference changes the lock files of every project that depends on it, the Campaign projects included (`src/Campaign/Campaign.slnx`); regenerate and commit them with the change.
- **Out of scope.** Whatever a change leaves out or simplifies gets a row in [the backlog](<src/ASL/docs/ASL Unit Backlog.md>).
- **Tools** live under `code-builders/`, not under `src/`.
- **Files** use LF line endings and UTF-8 without a BOM.
- **Writing.** Plain language: no em-dashes, no emoticons, no marketing phrases. A hex is written in brackets, "[G4]".
- **Questions.** A question to the user carries its analysis and a proposal, never a bare question.
