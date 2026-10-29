"""Check the framing copy and refuse changed source/target ownership."""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SOURCE = Path(sys.argv.pop(1)).resolve() if __name__ == "__main__" else None
ADAPTER = Path(__file__).with_name("ogg-memory.cmake").resolve()


@unittest.skipIf(SOURCE is None, "Run with the pinned framing.c path")
class OggSourceTests(unittest.TestCase):
    def configure(self, root, content, target="src/framing.c"):
        source = root / "source"
        (source / "src").mkdir(parents=True)
        (source / "src/framing.c").write_bytes(content)
        (source / "other.c").write_text("int unused;\n")
        (source / "CMakeLists.txt").write_text(
            'cmake_minimum_required(VERSION 3.24)\nproject(Probe C)\n'
            'set(electron2dogg_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}")\n'
            f'add_library(ogg STATIC {target})\ninclude("{ADAPTER.as_posix()}")\n')
        return subprocess.run(["cmake", "-S", str(source), "-B", str(root / "build")], capture_output=True, text=True)

    def test_copy_routes_every_allocator_without_mutating_source(self):
        original = SOURCE.read_bytes()
        with tempfile.TemporaryDirectory(prefix="e2d-ogg-source-") as folder:
            root = Path(folder)
            result = self.configure(root, original)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            copy = (root / "build/framing-memory.c").read_text()
            self.assertTrue(copy.startswith('#include "memory.h"\n'))
            for old, new in (("malloc", "allocate"), ("realloc", "resize"), ("free", "release")):
                self.assertNotIn("_ogg_" + old, copy)
                self.assertIn("e2d_ogv_" + new, copy)
            self.assertEqual((root / "source/src/framing.c").read_bytes(), original)
        self.assertEqual(SOURCE.read_bytes(), original)

    def test_changed_source_is_rejected_before_generation(self):
        with tempfile.TemporaryDirectory(prefix="e2d-ogg-source-") as folder:
            root = Path(folder)
            result = self.configure(root, SOURCE.read_bytes() + b"\nchanged\n")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Re-audit Ogg container allocation", result.stderr)
            self.assertFalse((root / "build/framing-memory.c").exists())

    def test_changed_target_is_rejected_before_generation(self):
        with tempfile.TemporaryDirectory(prefix="e2d-ogg-source-") as folder:
            root = Path(folder)
            result = self.configure(root, SOURCE.read_bytes(), "other.c")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Pinned Ogg target no longer owns", result.stderr)
            self.assertFalse((root / "build/framing-memory.c").exists())


if __name__ == "__main__":
    unittest.main()
