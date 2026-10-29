"""Ask the newest MapHexIndex which boards hold a terrain or feature, and in which hexes (the MapHexIndex Design, ruling RMH.6).

The Map Studio's fidelity batch writes the index to {CACHE_ROOT}/hexindex/maphexindex-<start>.json. Set ASL_MAPS_CACHE when the Studio's
AslMaps:CacheRoot is set elsewhere.

Usage:
  python hex_index.py --features                    every feature id, with the number of boards holding it
  python hex_index.py --features woods              the ids that contain "woods", ignoring case
  python hex_index.py --has "terrain:Woods" flag:cliff  the boards holding every one, with the hexes of each
  python hex_index.py --board bd04 [--has ...]      one board's features (or only those asked)

The index narrows the boards; a question about neighbouring hexes is then confirmed in the board viewer, /boards/<board>?terrain=<id>.
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import CACHE_ROOT  # noqa: E402
import argparse
import glob
import json


def newest():
    files = sorted(glob.glob(CACHE_ROOT + "/hexindex/maphexindex-*.json"))
    if not files:
        _sys.exit("No MapHexIndex under " + CACHE_ROOT + "/hexindex: run the fidelity batch on the Studio's Fidelity page first.")
    with open(files[-1], encoding="utf-8") as stream:
        return files[-1], json.load(stream)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--features", nargs="?", const="", metavar="TEXT", help="list feature ids, optionally those containing TEXT")
    parser.add_argument("--has", nargs="+", metavar="ID", help="boards holding every one of these feature ids")
    parser.add_argument("--board", help="limit to one board")
    args = parser.parse_args()
    path, index = newest()
    print("index:", _os.path.basename(path), "| report", index["report"], "| derivation", index["derivationVersion"], "| VASL", index.get("vaslCommit"))
    boards = [board for board in index["boards"] if args.board is None or board["board"].lower() == args.board.lower()]
    if args.features is not None:
        counts = {}
        for board in boards:
            for feature in board["features"]:
                counts[feature["id"]] = counts.get(feature["id"], 0) + 1
        for feature_id in sorted(counts):
            if args.features.lower() in feature_id.lower():
                print(f"{feature_id}\t{counts[feature_id]} boards")
        return
    if not args.has and args.board is None:
        parser.print_help()
        return
    wanted = args.has or []
    found = 0
    for board in boards:
        features = {feature["id"]: feature["hexes"] for feature in board["features"]}
        if all(feature_id in features for feature_id in wanted):
            found += 1
            print(f"{board['board']} ({board['outcome']})")
            for feature_id in wanted or sorted(features):
                hexes = features[feature_id]
                print(f"  {feature_id}: {len(hexes)} hexes: {', '.join(hexes)}")
    unread = [board["board"] for board in boards if not board["features"]]
    print(f"{found} boards hold {' and '.join(wanted) if wanted else 'features'}; {len(unread)} boards have no features in the index (out of scope or failed).")


if __name__ == "__main__":
    main()
