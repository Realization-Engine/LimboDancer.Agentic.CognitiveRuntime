"""Normalize only this branch's changed text files to UTF-8 without BOM and LF."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[3]
git = ['git', '-c', 'safe.directory=' + root.as_posix(), '-C', str(root)]
paths = set()
for args in (['diff', '--name-only', 'HEAD'], ['ls-files', '--others', '--exclude-standard']):
    paths.update(subprocess.check_output(git + args, text=True).splitlines())
changed = 0
for name in sorted(paths):
    path = root / name
    if not path.is_file() or path.suffix not in {'.md', '.cs', '.json', '.py', '.ps1'}:
        continue
    before = path.read_bytes()
    after = before.decode('utf-8-sig').replace('\r\n', '\n').encode('utf-8')
    if before != after:
        path.write_bytes(after)
        changed += 1
print(f'Normalized {changed} changed text files.')
