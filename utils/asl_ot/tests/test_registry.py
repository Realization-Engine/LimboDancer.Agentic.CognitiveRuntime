from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from utils.asl_ot.registry import AslSourceRegistryBuilder, SourceRegistryError


class SourceRegistryTests(unittest.TestCase):
    SOURCE_COMMIT = "a3254ff1d492dbdd28483d86f5b42437b48e80d4"

    def test_registered_repository_source_set_is_complete_and_reproducible(self) -> None:
        repository_root = _repository_root()
        builder = AslSourceRegistryBuilder()

        first = builder.build(repository_root, self.SOURCE_COMMIT)
        second = builder.build(repository_root, self.SOURCE_COMMIT)

        self.assertEqual(first, second)
        self.assertEqual(7, sum(artifact.kind.value == "markdown" for artifact in first.artifacts))
        self.assertEqual(661, sum(artifact.kind.value == "image" for artifact in first.artifacts))
        self.assertTrue(all(len(artifact.sha256) == 64 for artifact in first.artifacts))
        self.assertEqual(sorted(artifact.path for artifact in first.artifacts), [artifact.path for artifact in first.artifacts])
        chapter_a = next(artifact for artifact in first.artifacts if artifact.chapter == "A")
        self.assertEqual((43, 111), (chapter_a.start_page, chapter_a.end_page))

    def test_committed_registry_matches_current_source_bytes(self) -> None:
        repository_root = _repository_root()
        expected = AslSourceRegistryBuilder().build(repository_root, self.SOURCE_COMMIT).to_dict()
        manifest_path = repository_root / "docs/ASL/SourceRegistry/asl-3.10-a-e.source-registry.json"
        actual = json.loads(manifest_path.read_text(encoding="utf-8"))

        self.assertEqual(expected, actual)

    def test_missing_registered_markdown_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / AslSourceRegistryBuilder.SOURCE_ROOT
            source.mkdir(parents=True)
            (source / "images").mkdir()
            (root / "utils").mkdir()
            (root / AslSourceRegistryBuilder.CONVERSION_TOOL_PATH).write_text("tool", encoding="utf-8")

            with self.assertRaisesRegex(SourceRegistryError, "missing="):
                AslSourceRegistryBuilder().build(root, self.SOURCE_COMMIT)

    def test_verification_sample_is_metadata_only_and_unverified(self) -> None:
        repository_root = _repository_root()
        sample_path = repository_root / "docs/ASL/SourceRegistry/asl-3.10-a-e.verification-sample.json"
        sample = json.loads(sample_path.read_text(encoding="utf-8"))

        self.assertEqual(14, len(sample["fragments"]))
        self.assertTrue(sample["requiresAuthoritativeEditionComparison"])
        self.assertTrue(all(item["verificationStatus"] == "unverified" for item in sample["fragments"]))
        self.assertTrue(all("content" not in item for item in sample["fragments"]))


def _repository_root() -> Path:
    return Path(__file__).resolve().parents[3]


if __name__ == "__main__":
    unittest.main()
