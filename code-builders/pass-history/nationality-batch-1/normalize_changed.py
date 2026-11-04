"""Normalize only this worktree's changed text files to UTF-8 without BOM and LF."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[3]
git = ['git', '-c', 'safe.directory=' + root.as_posix(), '-C', str(root)]
paths = set(subprocess.check_output(git + ['diff', '--name-only'], text=True).splitlines())
paths.update(subprocess.check_output(git + ['ls-files', '--others', '--exclude-standard'], text=True).splitlines())
for name in paths:
    path = root / name
    if path.is_file() and path.suffix in ('.cs', '.md', '.py', '.ps1', '.json'):
        content = path.read_text(encoding='utf-8-sig')
        path.write_text(content.rstrip('\n') + '\n', encoding='utf-8', newline='\n')
print(f'Normalized {len(paths)} changed paths.')
