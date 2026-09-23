"""Structure-aware Markdown source-fragment location for ASL-OT-01."""

from __future__ import annotations

import hashlib
import re

from .models import SourceFragment, SourceFragmentKind, SourceLocator


_PAGE = re.compile(r"^<!--\s*page\s+(?P<page>\d+)\s*-->$")
_HEADING = re.compile(r"^(?P<marks>#{1,6})\s+(?P<title>.+?)\s*$")
_FIGURE = re.compile(r"!\[[^]]*]\((?P<path>[^)]+)\)")
_RULE = re.compile(
    r"^\*\*(?:\\?\*)?(?P<id>(?:[A-Z]\.)?\d+(?:\.\d+)*)\b",
    re.IGNORECASE,
)
_FOOTNOTE = re.compile(r"<sup>.+?</sup>", re.IGNORECASE)


class MarkdownFragmentLocator:
    """Locates structural, immutable provenance units without interpreting rule semantics."""

    def locate(
        self,
        source_id: str,
        source_path: str,
        source_sha256: str,
        chapter: str | None,
        content: str,
    ) -> tuple[SourceFragment, ...]:
        if not source_id.strip() or not source_path.strip() or len(source_sha256) != 64:
            raise ValueError("Source identity, path, and SHA-256 are required.")

        lines = content.splitlines(keepends=True)
        fragments: list[SourceFragment] = []
        heading_path: list[str] = []
        current_page: int | None = None
        current_rule: tuple[str, str] | None = None
        index = 0

        while index < len(lines):
            line_text = _line_text(lines[index])
            page_match = _PAGE.match(line_text)
            if page_match:
                current_page = int(page_match.group("page"))
                index += 1
                continue
            if not line_text.strip():
                index += 1
                continue

            heading_match = _HEADING.match(line_text)
            if heading_match:
                level = len(heading_match.group("marks"))
                title = heading_match.group("title").strip()
                heading_path = heading_path[: level - 1]
                heading_path.append(title)
                current_rule = None
                fragments.append(
                    self._fragment(
                        source_id,
                        source_path,
                        source_sha256,
                        SourceFragmentKind.HEADING,
                        lines[index],
                        index + 1,
                        index + 1,
                        current_page,
                        tuple(heading_path),
                        None,
                        None,
                    )
                )
                index += 1
                continue

            if line_text.startswith("```"):
                end = index + 1
                while end < len(lines) and not _line_text(lines[end]).startswith("```"):
                    end += 1
                if end < len(lines):
                    end += 1
                fragments.append(
                    self._fragment(
                        source_id,
                        source_path,
                        source_sha256,
                        SourceFragmentKind.STRUCTURED_TEXT,
                        "".join(lines[index:end]),
                        index + 1,
                        end,
                        current_page,
                        tuple(heading_path),
                        *(current_rule or (None, None)),
                    )
                )
                index = end
                continue

            if _FIGURE.fullmatch(line_text.strip()):
                fragments.append(
                    self._fragment(
                        source_id,
                        source_path,
                        source_sha256,
                        SourceFragmentKind.FIGURE_REFERENCE,
                        lines[index],
                        index + 1,
                        index + 1,
                        current_page,
                        tuple(heading_path),
                        *(current_rule or (None, None)),
                    )
                )
                index += 1
                continue

            end = index + 1
            while end < len(lines) and not self._is_boundary(lines[end]):
                end += 1
            raw = "".join(lines[index:end])
            rule_match = _RULE.match(line_text) if chapter is not None else None
            if rule_match:
                published_id = rule_match.group("id")
                normalized_id = normalize_rule_id(published_id, chapter)
                current_rule = (published_id, normalized_id)
                kind = SourceFragmentKind.RULE_TEXT
            elif current_rule is not None:
                published_id, normalized_id = current_rule
                kind = SourceFragmentKind.RULE_CONTINUATION
            else:
                published_id = normalized_id = None
                kind = SourceFragmentKind.PARAGRAPH

            fragments.append(
                self._fragment(
                    source_id,
                    source_path,
                    source_sha256,
                    kind,
                    raw,
                    index + 1,
                    end,
                    current_page,
                    tuple(heading_path),
                    published_id,
                    normalized_id,
                )
            )
            index = end

        return tuple(fragments)

    @staticmethod
    def _is_boundary(line: str) -> bool:
        text = _line_text(line)
        return (
            not text.strip()
            or _PAGE.match(text) is not None
            or _HEADING.match(text) is not None
            or text.startswith("```")
            or _FIGURE.fullmatch(text.strip()) is not None
            or _RULE.match(text) is not None
        )

    @staticmethod
    def _fragment(
        source_id: str,
        source_path: str,
        source_sha256: str,
        kind: SourceFragmentKind,
        content: str,
        start_line: int,
        end_line: int,
        page: int | None,
        heading_path: tuple[str, ...],
        published_id: str | None,
        normalized_id: str | None,
    ) -> SourceFragment:
        content_hash = hashlib.sha256(content.encode("utf-8")).hexdigest()
        identity_material = (
            f"{source_id}\n{source_sha256}\n{start_line}\n{end_line}\n{content_hash}"
        ).encode("utf-8")
        fragment_hash = hashlib.sha256(identity_material).hexdigest()
        dependencies = tuple(match.group("path") for match in _FIGURE.finditer(content))
        return SourceFragment(
            fragment_id=f"asl-fragment:sha256:{fragment_hash}",
            source_id=source_id,
            source_path=source_path,
            source_sha256=source_sha256,
            kind=kind,
            content_sha256=content_hash,
            content=content,
            locator=SourceLocator(
                start_line=start_line,
                end_line=end_line,
                start_page=page,
                end_page=page,
                heading_path=heading_path,
                published_element_id=published_id,
                normalized_element_id=normalized_id,
            ),
            dependencies=dependencies,
            has_footnote_markers=_FOOTNOTE.search(content) is not None,
        )


def normalize_rule_id(published_id: str, chapter: str | None) -> str:
    """Adds required chapter context while leaving the published identifier available unchanged."""
    value = published_id.strip()
    if re.match(r"^[A-Z](?:\.|\d)", value, re.IGNORECASE):
        return value.upper()
    return f"{chapter.upper()}{value}" if chapter else value


def _line_text(line: str) -> str:
    return line.rstrip("\r\n")
