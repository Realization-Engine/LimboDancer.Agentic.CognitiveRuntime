"""Command-line entry point for reproducible ASL-OT-01 manifests."""

from __future__ import annotations

import argparse
import json
import posixpath
from pathlib import Path
from typing import Any

from .fragments import MarkdownFragmentLocator
from .models import SourceArtifactKind, SourceFragment, SourceFragmentKind
from .registry import AslSourceRegistryBuilder


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate ASL-OT-01 source and fragment manifests.")
    parser.add_argument("--repository-root", type=Path, default=Path.cwd())
    parser.add_argument("--source-commit", required=True)
    parser.add_argument("--registry-output", type=Path, required=True)
    parser.add_argument("--verification-output", type=Path, required=True)
    args = parser.parse_args()

    repository_root = args.repository_root.resolve()
    registry = AslSourceRegistryBuilder().build(repository_root, args.source_commit)
    fragments: list[SourceFragment] = []
    locator = MarkdownFragmentLocator()
    for artifact in registry.artifacts:
        if artifact.kind is not SourceArtifactKind.MARKDOWN:
            continue
        content = (repository_root / artifact.path).read_text(encoding="utf-8")
        fragments.extend(
            locator.locate(
                artifact.source_id,
                artifact.path,
                artifact.sha256,
                artifact.chapter,
                content,
            )
        )

    validate_fragment_dependencies(fragments, {artifact.path for artifact in registry.artifacts})

    _write_json(args.registry_output, registry.to_dict())
    _write_json(
        args.verification_output,
        {
            "schemaVersion": "1.0.0",
            "registryId": registry.registry_id,
            "purpose": "Representative fragments awaiting comparison with the authoritative ASL 3.10 edition.",
            "selectionPolicy": "First structural examples plus chapter coverage and selected chapter-local identifiers.",
            "requiresAuthoritativeEditionComparison": True,
            "fragments": [fragment.to_manifest_dict() for fragment in select_verification_sample(fragments)],
        },
    )
    return 0


def select_verification_sample(fragments: list[SourceFragment]) -> tuple[SourceFragment, ...]:
    selected: list[SourceFragment] = []
    seen: set[str] = set()

    def add(fragment: SourceFragment | None) -> None:
        if fragment is not None and fragment.fragment_id not in seen:
            selected.append(fragment)
            seen.add(fragment.fragment_id)

    for kind in SourceFragmentKind:
        add(next((fragment for fragment in fragments if fragment.kind is kind), None))

    for chapter in "ABCDE":
        add(
            next(
                (
                    fragment
                    for fragment in fragments
                    if fragment.kind is SourceFragmentKind.RULE_TEXT
                    and fragment.locator.normalized_element_id is not None
                    and fragment.locator.normalized_element_id.startswith(chapter)
                ),
                None,
            )
        )

    for identifier in ("A1.1", "B1.13", "C1.2"):
        add(
            next(
                (
                    fragment
                    for fragment in fragments
                    if fragment.locator.normalized_element_id == identifier
                ),
                None,
            )
        )

    add(next((fragment for fragment in fragments if fragment.has_footnote_markers), None))
    return tuple(selected)


def validate_fragment_dependencies(
    fragments: list[SourceFragment],
    registered_paths: set[str],
) -> None:
    unresolved: list[str] = []
    for fragment in fragments:
        source_directory = posixpath.dirname(fragment.source_path)
        for dependency in fragment.dependencies:
            resolved = posixpath.normpath(posixpath.join(source_directory, dependency))
            if resolved not in registered_paths:
                unresolved.append(f"{fragment.fragment_id} -> {resolved}")
    if unresolved:
        raise ValueError("Unregistered fragment dependencies: " + ", ".join(unresolved))


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    serialized = json.dumps(value, indent=2, ensure_ascii=False) + "\n"
    path.write_text(serialized, encoding="utf-8", newline="\n")


if __name__ == "__main__":
    raise SystemExit(main())
