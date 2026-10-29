"""Check generated lazy-loader ownership without changing either pinned source tree."""

import hashlib
from pathlib import Path
import sys
import tempfile
import unittest

import prepare

SOURCES = [Path(sys.argv[1]).resolve()]
sys.argv[1:] = []


class PreparationTests(unittest.TestCase):
    def test_shared_pin_and_both_epoch_producers(self):
        self.assertEqual(len(SOURCES), 1)
        for source in SOURCES:
            original = (source / "hb-machinery.hh").read_bytes()
            self.assertEqual(hashlib.sha256(original).hexdigest(), prepare.SOURCE_SHA256)
            with tempfile.TemporaryDirectory(prefix="e2d-shaping-prepare-") as directory:
                destination = Path(directory) / "owned"
                for epoch in ("e2d_memory_failure_epoch", "e2d_ft_hb_failure_epoch"):
                    prepare.prepare(source, destination, epoch)
                    generated = (destination / "hb-machinery.hh").read_text()
                    self.assertIn(prepare.REPLACEMENT.replace("e2d_memory_failure_epoch", epoch), generated)
                    self.assertNotIn(prepare.ORIGINAL, generated)
                    self.assertEqual((source / "hb-machinery.hh").read_bytes(), original)

    def test_drift_rejected_before_output(self):
        with tempfile.TemporaryDirectory(prefix="e2d-shaping-prepare-") as directory:
            source = Path(directory) / "source"
            source.mkdir()
            destination = Path(directory) / "owned"
            (source / "hb-machinery.hh").write_bytes((SOURCES[0] / "hb-machinery.hh").read_bytes() + b"\n")
            with self.assertRaisesRegex(ValueError, "source changed"):
                prepare.prepare(source, destination)
            self.assertFalse(destination.exists())
            with self.assertRaisesRegex(ValueError, "Unknown"):
                prepare.prepare(source, destination, "unowned_epoch")


if __name__ == "__main__":
    unittest.main()
