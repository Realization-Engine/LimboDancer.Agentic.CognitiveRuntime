"""Read-only LOS fixture survey. Usage: python survey-boards.py <VASL checkout>.

Reads terrain bytes, not artwork or metadata comments. Includes historical boards
and boards with unreadable/missing metadata; an absent input is reported explicitly.
The output contains counts and source identities, never board artwork or grids.
"""

import collections
import gzip
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET


def blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


def payload(data):
    stream = gzip.decompress(data)
    if stream[:4] != b"\xac\xed\x00\x05":
        raise ValueError("Not a Java object stream")
    result = bytearray()
    offset = 4
    while offset < len(stream):
        tag = stream[offset]
        offset += 1
        if tag == 0x77:
            size = stream[offset]
            offset += 1
        elif tag == 0x7A:
            size = struct.unpack_from(">I", stream, offset)[0]
            offset += 4
        else:
            raise ValueError(f"Unexpected Java stream tag {tag:#x}")
        if offset + size > len(stream):
            raise ValueError("Truncated Java block")
        result.extend(stream[offset:offset + size])
        offset += size
    return result


def group(terrain):
    name = terrain["name"]
    category = terrain.get("LOSCategory")
    if category == "ENTRENCHMENT":
        return "entrenchments"
    if category == "TUNNEL":
        return "tunnels"
    if "Roofless" in name or "Gutted" in name:
        return "rooflessBuildings"
    if "Embankment" in name or "Rrembankment" in name:
        return "railroadEmbankments"
    if "PartialOrchard" in name:
        return "partialOrchards"
    return None


def survey(root):
    catalog_bytes = (root / "dist/boardData/SharedBoardMetadata.xml").read_bytes()
    catalog = {int(t.attrib["typeCode"]): t.attrib
               for t in ET.fromstring(catalog_bytes).iter("terrainType")}
    boards = []
    totals = collections.Counter()
    for directory in sorted((root / "boards/src").glob("bd*")):
        if not directory.is_dir():
            continue
        entry = {"board": directory.name}
        totals["directories"] += 1
        metadata = directory / "BoardMetadata.xml"
        if metadata.exists():
            data = metadata.read_bytes()
            entry["metadataContentBlob"] = blob(data)
            try:
                xml = ET.fromstring(data)
                entry["geometry"] = {key: value for key, value in xml.attrib.items()
                                     if key in {"width", "height", "hexWidth", "hexHeight",
                                                "A1CenterX", "A1CenterY", "altHexGrain", "HexGridConfig"}}
                entry["annotations"] = {name: len(list(xml.iter(name)))
                                        for name in ("partialorchard", "rrembankment")}
            except ET.ParseError as error:
                entry["metadataError"] = str(error)
        else:
            entry["metadataError"] = "missing"
        los = directory / "LOSData"
        if los.exists():
            data = los.read_bytes()
            entry["losDataBlob"] = blob(data)
            try:
                raw = payload(data)
                width, height, grid_width, grid_height = struct.unpack_from(">4I", raw)
                count = grid_width * grid_height
                if len(raw) < 16 + 2 * count:
                    raise ValueError("Truncated terrain grid")
                entry["header"] = [width, height, grid_width, grid_height]
                codes = collections.Counter(raw[17:16 + 2 * count:2])
                entry["terrainPixels"] = {catalog[code]["name"]: pixels
                                          for code, pixels in sorted(codes.items())
                                          if code in catalog and group(catalog[code])}
                entry["unknownCodes"] = sorted(set(codes) - set(catalog))
                totals["decodedGrids"] += 1
            except (ValueError, OSError, EOFError, struct.error, IndexError) as error:
                entry["losError"] = str(error)
        else:
            entry["losError"] = "missing"
        if entry.get("terrainPixels") or any(entry.get("annotations", {}).values()):
            totals["candidateBoards"] += 1
        boards.append(entry)
    return {
        "vaslCommit": subprocess.check_output(
            ["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip(),
        "sharedMetadataContentBlob": blob(catalog_bytes),
        "summary": dict(totals),
        "boards": boards,
    }


if __name__ == "__main__":
    print(json.dumps(survey(Path(sys.argv[1])), indent=2))
