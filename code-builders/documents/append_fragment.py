"""Append a fragment file to a document, with one blank line between: append.py <document> <fragment>. LF, UTF-8, no BOM."""
import io
import sys

doc, fragment = sys.argv[1], sys.argv[2]
text = io.open(doc, encoding="utf-8", newline="").read().replace("\r\n", "\n").rstrip("\n")
more = io.open(fragment, encoding="utf-8", newline="").read().replace("\r\n", "\n").strip("\n")
io.open(doc, "w", encoding="utf-8", newline="").write(text + "\n\n" + more + "\n")
print("appended", len(more.splitlines()), "lines")
