"""ASL-OT-01 source registry and fragment models."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from enum import StrEnum
from typing import Any


class SourceArtifactKind(StrEnum):
    MARKDOWN = "markdown"
    IMAGE = "image"


class SourceFragmentKind(StrEnum):
    HEADING = "heading"
    RULE_TEXT = "ruleText"
    RULE_CONTINUATION = "ruleContinuation"
    FIGURE_REFERENCE = "figureReference"
    STRUCTURED_TEXT = "structuredText"
    PARAGRAPH = "paragraph"


class VerificationStatus(StrEnum):
    UNVERIFIED = "unverified"
    VERIFIED = "verified"


@dataclass(frozen=True, slots=True)
class SourceArtifact:
    source_id: str
    path: str
    kind: SourceArtifactKind
    sha256: str
    size_bytes: int
    chapter: str | None = None
    start_page: int | None = None
    end_page: int | None = None

    def to_dict(self) -> dict[str, Any]:
        return _without_none(
            {
                "sourceId": self.source_id,
                "path": self.path,
                "kind": self.kind.value,
                "sha256": self.sha256,
                "sizeBytes": self.size_bytes,
                "chapter": self.chapter,
                "startPage": self.start_page,
                "endPage": self.end_page,
            }
        )


@dataclass(frozen=True, slots=True)
class ConversionTool:
    path: str
    sha256: str

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True, slots=True)
class SourceRegistryManifest:
    schema_version: str
    registry_id: str
    edition: str
    source_commit: str
    source_root: str
    conversion_tool: ConversionTool
    artifacts: tuple[SourceArtifact, ...]

    def to_dict(self) -> dict[str, Any]:
        return {
            "schemaVersion": self.schema_version,
            "registryId": self.registry_id,
            "edition": self.edition,
            "sourceCommit": self.source_commit,
            "sourceRoot": self.source_root,
            "generator": {"name": "utils.asl_ot", "version": "0.1.0"},
            "distribution": {
                "containsCopyrightedMaterial": True,
                "access": "repository-controlled",
                "redistribution": "source-license-governed",
            },
            "conversionTool": self.conversion_tool.to_dict(),
            "artifacts": [artifact.to_dict() for artifact in self.artifacts],
        }


@dataclass(frozen=True, slots=True)
class SourceLocator:
    start_line: int
    end_line: int
    start_page: int | None
    end_page: int | None
    heading_path: tuple[str, ...]
    published_element_id: str | None = None
    normalized_element_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return _without_none(
            {
                "startLine": self.start_line,
                "endLine": self.end_line,
                "startPage": self.start_page,
                "endPage": self.end_page,
                "headingPath": list(self.heading_path),
                "publishedElementId": self.published_element_id,
                "normalizedElementId": self.normalized_element_id,
            }
        )


@dataclass(frozen=True, slots=True)
class SourceFragment:
    fragment_id: str
    source_id: str
    source_path: str
    source_sha256: str
    kind: SourceFragmentKind
    content_sha256: str
    content: str
    locator: SourceLocator
    dependencies: tuple[str, ...]
    has_footnote_markers: bool
    verification_status: VerificationStatus = VerificationStatus.UNVERIFIED

    def to_manifest_dict(self) -> dict[str, Any]:
        return {
            "fragmentId": self.fragment_id,
            "sourceId": self.source_id,
            "sourcePath": self.source_path,
            "sourceSha256": self.source_sha256,
            "kind": self.kind.value,
            "contentSha256": self.content_sha256,
            "locator": self.locator.to_dict(),
            "dependencies": list(self.dependencies),
            "hasFootnoteMarkers": self.has_footnote_markers,
            "verificationStatus": self.verification_status.value,
        }


def _without_none(value: dict[str, Any]) -> dict[str, Any]:
    return {key: item for key, item in value.items() if item is not None}
