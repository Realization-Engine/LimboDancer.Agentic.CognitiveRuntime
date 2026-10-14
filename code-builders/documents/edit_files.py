"""Edits over several files: every anchor is checked before any file is written."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import io

ROOT = REPO + "/src/ASL/"


class Edits:
    def __init__(self):
        self.files = {}
        self.pending = []

    def _text(self, path):
        if path not in self.files:
            raw = io.open(ROOT + path, encoding="utf-8", newline="").read()
            self.files[path] = ["\r\n" in raw, raw.replace("\r\n", "\n")]
        return self.files[path]

    def edit(self, path, old, new, count=1):
        self._text(path)
        self.pending.append((path, old, new, count))

    def apply(self):
        bad = []
        for path, old, new, count in self.pending:
            found = self.files[path][1].count(old)
            if found != count:
                bad.append((path, found, old[:100]))
        if bad:
            for item in bad:
                print("ANCHOR", item)
            raise SystemExit(1)
        for path, old, new, count in self.pending:
            entry = self.files[path]
            if entry[1].count(old) != count:
                print("ANCHOR moved by an earlier edit", path, old[:100])
                raise SystemExit(1)
            entry[1] = entry[1].replace(old, new)
        for path, (crlf, text) in self.files.items():
            io.open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace("\n", "\r\n") if crlf else text)
        print("applied", len(self.pending), "edits to", len(self.files), "files")
