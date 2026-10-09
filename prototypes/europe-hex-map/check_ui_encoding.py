"""Reject UTF-8 text that was accidentally decoded as Windows-1252."""
import re
from pathlib import Path
UI_FILES = ('atlas-navigation.js', 'ardennes-workspace.js', 'ardennes-spatial.js', 'ardennes-command.html', 'ardennes-command-view.js', 'ardennes-command-state.js', 'ardennes-command-bridge.js', 'viewer.html', 'index.html', 'app.template.js', 'theater-view.js', 'workflow-view.js',
            'regional-view.js', 'situation-state.js', 'situation-view.js', 'tactical-reference.js', 'tactical-handoff.js', 'formation-state.js', 'formation-view.js', 'theater-counters.js', 'regional-state.js', 'app.js', 'site.css', 'image-export.js', 'panzer-situation-view.js', 'panzer-situation-state.js', 'panzer-map-art.js')
def check_text(text, name):
    if '\ufffd' in text:
        raise ValueError(f'{name}: replacement character in UI text')
    for match in re.finditer(r'[^\x00-\x7f]+', text):
        token = match.group()
        try:
            decoded = token.encode('cp1252').decode('utf-8')
        except (UnicodeEncodeError, UnicodeDecodeError):
            continue
        if decoded != token:
            raise ValueError(f'{name}: probable UTF-8/Windows-1252 corruption')
def check_files(root):
    for name in UI_FILES:
        check_text((root / name).read_text(encoding='utf-8-sig'), name)
if __name__ == '__main__':
    check_files(Path(__file__).resolve().parent)
    print('UI encoding checks passed.')
