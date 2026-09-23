"""Deterministic ASL 3.10 source-registry construction."""

from __future__ import annotations

import hashlib
import re
from pathlib import Path

from .models import (
    ConversionTool,
    SourceArtifact,
    SourceArtifactKind,
    SourceRegistryManifest,
)


class SourceRegistryError(ValueError):
    """Raised when the registered source set is incomplete or unexpected."""


class AslSourceRegistryBuilder:
    SCHEMA_VERSION = "1.0.0"
    REGISTRY_ID = "asl-easlrb-3.10-a-e"
    EDITION = "3.10"
    SOURCE_ROOT = "docs/ASL/Rulebook_Markdown"
    CONVERSION_TOOL_PATH = "utils/pdf_to_markdown.py"
    EXPECTED_MARKDOWN = {
        "00 - Table of Contents.md": ("asl-easlrb-3.10:contents", None),
        "01 - Index and Glossary.md": ("asl-easlrb-3.10:index-glossary", None),
        "02 - Chapter A - Infantry and Basic Game Rules.md": ("asl-easlrb-3.10:chapter-a", "A"),
        "03 - Chapter B - Terrain.md": ("asl-easlrb-3.10:chapter-b", "B"),
        "04 - Chapter C - Ordnance and Offboard Artillery.md": ("asl-easlrb-3.10:chapter-c", "C"),
        "05 - Chapter D - Vehicles.md": ("asl-easlrb-3.10:chapter-d", "D"),
        "06 - Chapter E - Miscellaneous.md": ("asl-easlrb-3.10:chapter-e", "E"),
    }

    def build(self, repository_root: Path, source_commit: str) -> SourceRegistryManifest:
        root = repository_root.resolve()
        source_root = root / self.SOURCE_ROOT
        conversion_tool = root / self.CONVERSION_TOOL_PATH
        self._validate_inputs(source_root, conversion_tool, source_commit)

        artifacts: list[SourceArtifact] = []
        for filename, (source_id, chapter) in sorted(self.EXPECTED_MARKDOWN.items()):
            path = source_root / filename
            artifacts.append(self._artifact(root, path, source_id, SourceArtifactKind.MARKDOWN, chapter))

        images_root = source_root / "images"
        image_paths = sorted(path for path in images_root.rglob("*") if path.is_file())
        if not image_paths:
            raise SourceRegistryError("The ASL rulebook image set is empty.")
        for path in image_paths:
            relative_image = path.relative_to(images_root).as_posix()
            source_id = f"asl-easlrb-3.10:image/{relative_image}"
            artifacts.append(self._artifact(root, path, source_id, SourceArtifactKind.IMAGE, None))

        artifacts.sort(key=lambda artifact: artifact.path)
        return SourceRegistryManifest(
            schema_version=self.SCHEMA_VERSION,
            registry_id=self.REGISTRY_ID,
            edition=self.EDITION,
            source_commit=source_commit,
            source_root=self.SOURCE_ROOT,
            conversion_tool=ConversionTool(
                path=self.CONVERSION_TOOL_PATH,
                sha256=sha256_file(conversion_tool),
            ),
            artifacts=tuple(artifacts),
        )

    def _validate_inputs(self, source_root: Path, conversion_tool: Path, source_commit: str) -> None:
        if not source_commit.strip():
            raise SourceRegistryError("A non-empty source commit is required.")
        if not source_root.is_dir():
            raise SourceRegistryError(f"Source root does not exist: {source_root}")
        if not conversion_tool.is_file():
            raise SourceRegistryError(f"Conversion tool does not exist: {conversion_tool}")

        actual_markdown = {path.name for path in source_root.glob("*.md")}
        expected_markdown = set(self.EXPECTED_MARKDOWN)
        missing = sorted(expected_markdown - actual_markdown)
        unexpected = sorted(actual_markdown - expected_markdown)
        if missing or unexpected:
            raise SourceRegistryError(
                f"Unexpected ASL Markdown source set; missing={missing}, unexpected={unexpected}."
            )

    @staticmethod
    def _artifact(
        repository_root: Path,
        path: Path,
        source_id: str,
        kind: SourceArtifactKind,
        chapter: str | None,
    ) -> SourceArtifact:
        start_page, end_page = _page_range(path, kind)
        return SourceArtifact(
            source_id=source_id,
            path=path.relative_to(repository_root).as_posix(),
            kind=kind,
            sha256=sha256_file(path),
            size_bytes=path.stat().st_size,
            chapter=chapter,
            start_page=start_page,
            end_page=end_page,
        )


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def _page_range(path: Path, kind: SourceArtifactKind) -> tuple[int | None, int | None]:
    if kind is SourceArtifactKind.MARKDOWN:
        pages = [
            int(value)
            for value in re.findall(
                r"<!--\s*page\s+(\d+)\s*-->",
                path.read_text(encoding="utf-8"),
                flags=re.IGNORECASE,
            )
        ]
    else:
        match = re.search(r"-p(\d+)(?:-|\.)", path.name, flags=re.IGNORECASE)
        pages = [int(match.group(1))] if match else []
    return (min(pages), max(pages)) if pages else (None, None)
