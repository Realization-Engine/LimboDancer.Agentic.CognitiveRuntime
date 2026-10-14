"""Pass 55: re-pin the catalog's digest in the four packages' case matrices and manifests, and their digests in the code and tests.

repin.py <old catalog sha256>. Each digest is replaced wherever it stands in docs/ASL/SourceRegistry/*.json and src/ASL/**/*.cs (not boards/).
"""
import glob
import hashlib
import io
import os
import sys

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
REGISTRY = ROOT + "/docs/ASL/SourceRegistry"
CATALOG = ROOT + "/src/ASL/units/catalog/scenario-a1.catalog.json"
PACKAGES = ["fire", "rally", "close-combat", "ordnance"]


def sha(path):
    with io.open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def files():
    found = glob.glob(REGISTRY + "/*.json")
    for folder, _, names in os.walk(ROOT + "/src/ASL"):
        folder = folder.replace("\\", "/")
        if "/boards" in folder or "/bin" in folder or "/obj" in folder:
            continue
        found += [folder + "/" + name for name in names if name.endswith(".cs")]
    return found


def replace(old, new, expect_in):
    assert old != new
    changed = []
    for path in files():
        with io.open(path, "r", encoding="utf-8", newline="") as f:
            text = f.read()
        if old in text:
            with io.open(path, "w", encoding="utf-8", newline="") as f:
                f.write(text.replace(old, new))
            changed.append(path[len(ROOT) + 1:])
    print(old[:12], "->", new[:12])
    for path in changed:
        print("   ", path)
    assert any(expect_in in path for path in changed), expect_in
    return changed


old_catalog = sys.argv[1]
matrices = {name: REGISTRY + "/asl-scenario-a1.%s-case-matrix.json" % name for name in PACKAGES}
manifests = {name: REGISTRY + "/asl-scenario-a1.%s-package.json" % name for name in PACKAGES}
old_matrix = {name: sha(path) for name, path in matrices.items()}
old_manifest = {name: sha(path) for name, path in manifests.items()}
changed = replace(old_catalog, sha(CATALOG), "fire-case-matrix")
assert len(changed) == 8, changed
for name in PACKAGES:
    replace(old_matrix[name], sha(matrices[name]), "%s-package.json" % name)
for name in PACKAGES:
    replace(old_manifest[name], sha(manifests[name]), "Package.cs")
