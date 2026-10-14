"""The version a game records for a VASL board: the Git blob id of the board archive's LOSData entry.

Usage: python board_version.py 03 04 05
"""
import hashlib
import sys
import zipfile

VASL = "E:/Archive/GitHub/dlandi/vasl/boards/bdFiles/bd"


def version(name):
    with zipfile.ZipFile(VASL + name) as archive:
        data = archive.read("LOSData")
    return hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()


if __name__ == "__main__":
    for name in sys.argv[1:]:
        print("bd" + name, version(name))
