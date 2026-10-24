"""Documents step: 2d's rows of the backlog fragment cite design section 10.1, not 9.1. Lines 28 to 32 only; asserts each count first."""
import io
import sys

path = sys.argv[1]
lines = io.open(path, encoding="utf-8", newline="").read().split("\n")
for i in range(27, 32):
    assert lines[i].count("(design 9.1") == 1, (i + 1, lines[i][:80])
for i in range(27, 32):
    lines[i] = lines[i].replace("(design 9.1", "(design 10.1")
io.open(path, "w", encoding="utf-8", newline="").write("\n".join(lines))
print("fixed 5 lines")
