#!/usr/bin/env python3
"""Check name-pool integrity with Python 3's standard library, not historical attestation.

Run validate.ps1 for JSON Schema checks as well.
"""
import argparse
import hashlib
import json
import re
import sys
import unicodedata
from pathlib import Path


class InvalidData(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise InvalidData(message)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON property: {key}")
        result[key] = value
    return result


def read(path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_object)


def index(items, label):
    result = {}
    for item in items:
        identifier = item["id"]
        require(re.fullmatch(r"[a-z][a-z0-9-]*", identifier), f"{label}: invalid id {identifier}")
        require(identifier not in result, f"{label}: duplicate id {identifier}")
        result[identifier] = item
    return result


def normalized(value):
    return unicodedata.normalize("NFC", value).casefold()


def require_supported_version(version, label):
    # Major identifies the format contract; minor/patch identify compatible data revisions.
    require(isinstance(version, str) and re.fullmatch(r"1\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)", version),
            f"{label}: unsupported version {version!r}; expected 1.x.y")


def validate(root):
    manifest = read(root / "manifest.json")
    require(manifest["format"] == "asl-name-manifest", "Unexpected manifest format")
    require_supported_version(manifest["version"], "manifest")
    sources = index(manifest["sources"], "sources")
    registered = index(manifest["pools"], "pools")
    contexts = index(manifest["nationalityContexts"], "contexts")
    forces = index(manifest["forceAffiliations"], "forces")
    groups = index(manifest["rulesGroups"], "groups")
    require(registered, "No pools registered")
    actual_files = {p.name for p in root.glob("*.names.json")}
    registered_files = {r["file"] for r in registered.values()}
    require(len(registered_files) == len(registered), "A pool file is registered twice")
    require(actual_files == registered_files, "Pool files differ from manifest: " + str(actual_files ^ registered_files))
    total_given = total_family = total_other = 0
    shortfalls = []
    for key, registration in registered.items():
        filename = registration["file"]
        require(filename == key + ".names.json", f"{key}: unsafe or inconsistent filename")
        path = root / filename
        pool = read(path)
        require(pool["id"] == key, f"{key}: inconsistent pool id")
        require(pool["format"] == "asl-name-pool", f"{key}: unknown format")
        require_supported_version(pool["version"], key)
        require(pool["version"] == registration["version"], f"{key}: version mismatch")
        require(hashlib.sha256(path.read_bytes()).hexdigest() == registration["sha256"], f"{key}: SHA-256 mismatch; update manifest after review")
        require(pool["reviewStatus"] == registration["reviewStatus"], f"{key}: review state mismatch")
        require(pool["provenance"]["sourceIds"] and set(pool["provenance"]["sourceIds"]) <= sources.keys(), f"{key}: unknown source")
        require(pool["intendedScenarioYears"]["from"] <= pool["intendedScenarioYears"]["to"], f"{key}: reversed dates")
        subgroup_ids = set(index(pool["subgroups"], key + " subgroups"))
        require(bool(subgroup_ids) == pool["requiresSubgroup"], f"{key}: subgroup requirement mismatch")
        component_ids = set()
        for field in ("givenNames", "familyNames", "patronymics", "communityNames"):
            entries = pool.get(field, [])
            values = set()
            for entry in entries:
                require(entry["id"] not in component_ids, f"{key}: duplicate component id")
                component_ids.add(entry["id"])
                require(re.fullmatch(r"[a-z][a-z0-9-]*", entry["id"]), f"{key}: bad component id")
                name = entry["text"]
                require(name and name == name.strip(), f"{key}: blank or untrimmed {field}")
                require(unicodedata.normalize("NFC", name) == name, f"{key}: name is not NFC")
                require(not any(unicodedata.category(c).startswith("C") for c in name), f"{key}: control character")
                require(normalized(name) not in values, f"{key}: duplicate {field} text: {name}")
                values.add(normalized(name))
                require(type(entry["weight"]) is int and entry["weight"] > 0, f"{key}: invalid weight")
                tags = entry.get("subgroupIds", [])
                require(len(tags) == len(set(tags)) and set(tags) <= subgroup_ids, f"{key}: invalid subgroup tag")
                if tags:
                    require(pool["requiresSubgroup"], f"{key}: tags without subgroup selection")
        require(pool["givenNames"], f"{key}: empty given-name list")
        role = pool["convention"]["secondComponentRole"]
        second_field = {"family": "familyNames", "father-given": "patronymics", "community": "communityNames"}.get(role)
        require(second_field, f"{key}: unknown second component role")
        second = pool.get(second_field, [])
        require(second, f"{key}: missing second components")
        for field in ("familyNames", "patronymics", "communityNames"):
            if field != second_field:
                require(not pool.get(field), f"{key}: conflicting second component collections")
        convention = pool["convention"]["id"]
        expected_orders = {"given-family": ("family", ["given", "family"]), "family-given": ("family", ["family", "given"]), "given-father": ("father-given", ["given", "father-given"]), "given-community": ("community", ["given", "community"])}
        require(convention in expected_orders, f"{key}: unsupported convention")
        require((role, pool["convention"]["componentOrder"]) == expected_orders[convention], f"{key}: component order/role mismatch")
        actual = {"givenNames": len(pool["givenNames"]), "familyNames": len(pool["familyNames"]), "secondComponent": len(second)}
        require(actual == pool["actualCounts"] == registration["actualCounts"], f"{key}: count mismatch")
        target = pool["targetCounts"]
        reached = actual["givenNames"] >= target["givenNames"] and actual["familyNames"] >= target["familyNames"] and actual["secondComponent"] >= target.get("secondComponent", target["familyNames"])
        completeness = "target-met" if reached else "smaller-curated-pool"
        require(completeness == pool["completeness"] == registration["completeness"], f"{key}: false completion claim")
        if not reached:
            shortfalls.append(key)
        capacities = {}
        for subgroup in sorted(subgroup_ids) or [None]:
            allowed_first = [e for e in pool["givenNames"] if "subgroupIds" not in e or subgroup in e["subgroupIds"]]
            allowed_second = [e for e in second if subgroup is None or subgroup in e.get("subgroupIds", [])]
            require(allowed_first and allowed_second, f"{key}: empty subgroup {subgroup}")
            strings = set()
            for first in allowed_first:
                for last in allowed_second:
                    if normalized(first["text"]) == normalized(last["text"]):
                        continue
                    parts = {"given": first["text"], role: last["text"]}
                    display = pool["convention"]["separator"].join(parts[c] for c in pool["convention"]["componentOrder"])
                    strings.add(normalized(display))
            capacities[subgroup or "all"] = len(strings)
        require(capacities == pool["availableCombinations"], f"{key}: false capacity claim")
        if subgroup_ids:
            require(all(e.get("subgroupIds") for e in second), f"{key}: unclassified second component")
        total_given += actual["givenNames"]
        total_family += actual["familyNames"]
        total_other += actual["secondComponent"] if role != "family" else 0
    used_pools = set()
    for key, context in contexts.items():
        choices = context["poolIds"]
        require(choices and len(choices) == len(set(choices)) and set(choices) <= registered.keys(), f"{key}: invalid pool choices")
        used_pools.update(choices)
        default = context["defaultPoolId"]
        require(default is None or default in choices, f"{key}: default not in choices")
        require((context["selection"] == "explicit-pool") == (default is None), f"{key}: ambiguous default")
        require(len(choices) == 1 or default is None, f"{key}: multi-pool context silently defaults")
    require(used_pools == set(registered), "Unreachable pools: " + str(set(registered) - used_pools))
    for key, force in forces.items():
        context = contexts.get(force["nationalityContextId"])
        require(context is not None, f"{key}: missing context")
        require(force["rulesNationalityContext"] in context["rulesNationalityContexts"], f"{key}: incompatible rules context")
    for key, group in groups.items():
        require(set(group["nationalityContextIds"]) <= contexts.keys(), f"{key}: missing group context")
    policy = manifest["poolSelectionPolicy"]
    require(policy["allowSilentFallback"] is False, "Silent fallback must remain disabled")
    require(policy["supportWeapons"] == "no-personnel-assignment", "Support weapons must remain unnamed")
    require(policy["authoredNames"] == "preserve", "Authored names must be preserved")
    return {"poolCount": len(registered), "nationalityContextCount": len(contexts), "forceAffiliationCount": len(forces), "givenNameEntries": total_given, "familyNameEntries": total_family, "otherSecondComponents": total_other, "smallerPools": shortfalls, "historicalReview": "not-attested-by-this-validator"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parents[2] / "units" / "names")
    args = parser.parse_args()
    try:
        result = validate(args.directory.resolve())
    except (InvalidData, OSError, ValueError, KeyError, TypeError) as error:
        print(f"Name-pool validation failed: {error}", file=sys.stderr)
        return 1
    print(json.dumps(result, indent=2, ensure_ascii=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
