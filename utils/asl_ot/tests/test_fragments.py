from __future__ import annotations

import hashlib
import unittest
from pathlib import Path

from utils.asl_ot.fragments import MarkdownFragmentLocator, normalize_rule_id
from utils.asl_ot.cli import validate_fragment_dependencies
from utils.asl_ot.models import SourceFragmentKind


class MarkdownFragmentLocatorTests(unittest.TestCase):
    def test_locates_exact_structural_fragments_and_preserves_published_identity(self) -> None:
        content = (
            "<!-- page 43 -->\n\n"
            "# A. INFANTRY\n\n"
            "**A.1 DICE:** Exact first rule.\n\n"
            "## 1. PERSONNEL COUNTERS\n\n"
            "**1.1** Local rule with a footnote.<sup>2</sup>\n\n"
            "Continuation of the local rule.\n\n"
            "![Figure](images/example.png)\n\n"
            "```text\nTABLE CELL\n```\n"
        )

        fragments = MarkdownFragmentLocator().locate(
            "asl-easlrb-3.10:chapter-a",
            "chapter-a.md",
            hashlib.sha256(content.encode("utf-8")).hexdigest(),
            "A",
            content,
        )

        local_rule = next(
            fragment
            for fragment in fragments
            if fragment.locator.normalized_element_id == "A1.1"
            and fragment.kind is SourceFragmentKind.RULE_TEXT
        )
        self.assertEqual("1.1", local_rule.locator.published_element_id)
        self.assertEqual("A1.1", local_rule.locator.normalized_element_id)
        self.assertEqual(43, local_rule.locator.start_page)
        self.assertTrue(local_rule.has_footnote_markers)
        self.assertEqual(
            hashlib.sha256(local_rule.content.encode("utf-8")).hexdigest(),
            local_rule.content_sha256,
        )

        continuation = next(
            fragment for fragment in fragments if fragment.kind is SourceFragmentKind.RULE_CONTINUATION
        )
        self.assertEqual("A1.1", continuation.locator.normalized_element_id)
        figure = next(fragment for fragment in fragments if fragment.kind is SourceFragmentKind.FIGURE_REFERENCE)
        self.assertEqual(("images/example.png",), figure.dependencies)
        self.assertTrue(any(fragment.kind is SourceFragmentKind.STRUCTURED_TEXT for fragment in fragments))

    def test_fragment_identity_changes_with_source_content(self) -> None:
        locator = MarkdownFragmentLocator()
        first_content = "**1.1** First.\n"
        changed_content = "**1.1** Changed.\n"
        first_hash = hashlib.sha256(first_content.encode("utf-8")).hexdigest()
        changed_hash = hashlib.sha256(changed_content.encode("utf-8")).hexdigest()
        first = locator.locate("source", "source.md", first_hash, "B", first_content)[0]
        repeated = locator.locate("source", "source.md", first_hash, "B", first_content)[0]
        changed = locator.locate("source", "source.md", changed_hash, "B", changed_content)[0]

        self.assertEqual(first.fragment_id, repeated.fragment_id)
        self.assertNotEqual(first.fragment_id, changed.fragment_id)
        self.assertNotEqual(first.content_sha256, changed.content_sha256)

    def test_real_chapter_local_identifier_is_normalized_with_chapter_context(self) -> None:
        repository_root = _repository_root()
        relative = Path("docs/ASL/Rulebook_Markdown/03 - Chapter B - Terrain.md")
        content = (repository_root / relative).read_text(encoding="utf-8")

        fragments = MarkdownFragmentLocator().locate(
            "asl-easlrb-3.10:chapter-b",
            relative.as_posix(),
            hashlib.sha256(content.encode("utf-8")).hexdigest(),
            "B",
            content,
        )

        fragment = next(
            item for item in fragments if item.locator.normalized_element_id == "B1.13"
        )
        self.assertEqual("1.13", fragment.locator.published_element_id)
        self.assertEqual("B1.13", normalize_rule_id("1.13", "B"))

    def test_unregistered_figure_dependency_fails_closed(self) -> None:
        content = "![Figure](images/missing.png)\n"
        source_hash = hashlib.sha256(content.encode("utf-8")).hexdigest()
        fragments = list(
            MarkdownFragmentLocator().locate(
                "source",
                "docs/source.md",
                source_hash,
                None,
                content,
            )
        )

        with self.assertRaisesRegex(ValueError, "Unregistered fragment dependencies"):
            validate_fragment_dependencies(fragments, {"docs/source.md"})


def _repository_root() -> Path:
    return Path(__file__).resolve().parents[3]


if __name__ == "__main__":
    unittest.main()
