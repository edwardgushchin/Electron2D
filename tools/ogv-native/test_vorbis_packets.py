"""Validate the pinned header-copy guards and their source ownership."""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SOURCE = Path(sys.argv.pop(1)).resolve() if __name__ == "__main__" else None
ADAPTER = Path(__file__).with_name("vorbis-packets.cmake").resolve()


@unittest.skipIf(SOURCE is None, "Run with the pinned Vorbis info.c path")
class VorbisPacketSourceTests(unittest.TestCase):
    def configure(self, root, content, owned=True):
        source = root / "source"
        (source / "lib").mkdir(parents=True)
        (source / "lib/info.c").write_bytes(content)
        entries = '"${CMAKE_CURRENT_SOURCE_DIR}/lib/info.c"' if owned else '"other.c"'
        (source / "CMakeLists.txt").write_text(
            'cmake_minimum_required(VERSION 3.24)\nproject(Probe NONE)\n'
            'set(electron2dvorbis_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}")\n'
            f'set(vorbis_files {entries})\ninclude("{ADAPTER.as_posix()}")\n')
        return subprocess.run(["cmake", "-S", str(source), "-B", str(root / "build")], capture_output=True, text=True)

    def test_every_packet_copy_is_guarded_without_changing_source(self):
        original = SOURCE.read_bytes()
        with tempfile.TemporaryDirectory(prefix="e2d-vorbis-packets-") as folder:
            root = Path(folder)
            result = self.configure(root, original)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            copied = (root / "build/info-memory.c").read_text()
            self.assertEqual(copied.count("oggpack_writecheck(&opb)"), 8)
            for name in ("header", "header1", "header2"):
                self.assertIn("if(!b->" + name + "){ret=OV_EFAULT;goto err_out;}", copied)
            self.assertIn("if(!op->packet){oggpack_writeclear(&opb);return OV_EFAULT;}", copied)
            self.assertIn("op->packet = e2d_ogv_allocate(oggpack_bytes(&opb));", copied)
            self.assertNotIn("op->packet = _ogg_malloc", copied)
            self.assertEqual((root / "source/lib/info.c").read_bytes(), original)
        self.assertEqual(SOURCE.read_bytes(), original)

    def test_changed_source_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="e2d-vorbis-packets-") as folder:
            root = Path(folder)
            result = self.configure(root, SOURCE.read_bytes() + b"changed")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Re-audit Vorbis packet publication", result.stderr)
            self.assertFalse((root / "build/info-memory.c").exists())

    def test_missing_owned_source_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix="e2d-vorbis-packets-") as folder:
            root = Path(folder)
            result = self.configure(root, SOURCE.read_bytes(), owned=False)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("Pinned Vorbis target no longer owns", result.stderr)
            self.assertFalse((root / "build/info-memory.c").exists())


if __name__ == "__main__":
    unittest.main()
