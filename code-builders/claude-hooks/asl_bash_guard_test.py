"""The guard's own check: each rule with a violation it must refuse and commands it must let through. Run: python asl_bash_guard_test.py"""
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from asl_bash_guard import check  # noqa: E402

REFUSED = [
    "python - <<'EOF'\nprint(1)\nEOF",
    "cat > x.sh <<EOF\necho\nEOF",
    "sed -i 's/a/b/' gate.sh",
    "sed -E -i 's/a/b/' gate.sh",
    "cd /e/tools && python -c \"\nimport re\nprint(1)\n\" < /dev/null",
    "python - file.txt",
    "python -I -c \"import sys;[open(f,'w',encoding='utf-8').write(open(f,encoding='utf-8').read().replace('a','b')) for f in sys.argv[1:]]\" x.cs",
    "python -c \"p='x';open(p,'w').write(open(p).read())\"",
    "python.exe -c \"\nimport re\nprint(1)\n\"",
    "py -c \"\nprint(1)\n\"",
    "\"/c/Python313/python.exe\" -c \"\nprint(1)\n\"",
    "cd src/ASL/docs && grep -n x file.md",
    "cd \"/e/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs\" && ls",
    "cd /e/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/X && grep y *.cs",
    "ls; cd E:/Archive/GitHub/dlandi/LimboDancer.MCP/src && ls",
    "git add -A",
    "git add .",
    "git add src/ASL",
    "git -C /e/x add --all",
    "git push --force origin main",
    "git push -f",
    "git -c safe.directory=x -C x push --force-with-lease origin a:main",
]
ALLOWED = [
    "cd /e/Archive/GitHub/dlandi/LimboDancer.MCP && git status --short",
    "cd E:/Archive/GitHub/dlandi/LimboDancer.MCP && git log -1",
    "cd /e/Archive/GitHub/dlandi/pass32-tools && bash gate/gate.sh 55 abc123 trial",
    "python script.py < /dev/null",
    "python -c \"print(1 << 3)\"",
    "python -c \"print(open('x.txt').read()[:200])\"",
    "python -I script.py x.cs",
    "sed -n 1,20p file.txt",
    "grep -c '<<' file.sh",
    "git add -- src/ASL/docs/a.md \"src/ASL/docs/b c.md\"",
    "git add --pathspec-from-file=paths.txt",
    "git push origin merge-55:main",
    "dotnet build x.csproj -f net10.0",
    "echo done; date +%H:%M",
]
failed = 0
for command in REFUSED:
    if not check(command):
        failed += 1
        print("NOT REFUSED:", repr(command))
for command in ALLOWED:
    if check(command):
        failed += 1
        print("WRONGLY REFUSED:", repr(command), check(command))

# The hook as the app calls it: JSON on standard input, exit code 2 and a reason for a violation, 0 otherwise.
script = str(Path(__file__).parent / "asl_bash_guard.py")
for command, want in ((REFUSED[2], 2), (ALLOWED[0], 0)):
    run = subprocess.run([sys.executable, script], input=json.dumps({"tool_name": "Bash", "tool_input": {"command": command}}), capture_output=True, text=True)
    if run.returncode != want or (want == 2 and "Refused" not in run.stderr):
        failed += 1
        print("HOOK:", repr(command), run.returncode, run.stderr)
print(f"{len(REFUSED)} refused, {len(ALLOWED)} allowed, {failed} failed")
sys.exit(1 if failed else 0)
