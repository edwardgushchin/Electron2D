"""Verify source preservation and fail-closed MSDF build preparation."""
import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("prepare_msdf", ROOT / "src/Servers/Text/Native/prepare_msdf.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PreparationTests(unittest.TestCase):
    def test_preserves_sources_and_unchanged_outputs(self):
        source = ROOT / "src/Vendor/MSDFGen"
        before = {p: p.read_bytes() for p in source.rglob("*") if p.is_file()}
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)
            module.prepare(source, output)
            times = {p: p.stat().st_mtime_ns for p in output.rglob("*") if p.is_file()}
            module.prepare(source, output)
            self.assertEqual(times, {p: p.stat().st_mtime_ns for p in times})
        self.assertTrue(all(p.read_bytes() == value for p, value in before.items()))

    def test_rejects_changed_or_extra_inputs_under_optimization(self):
        with tempfile.TemporaryDirectory() as directory:
            source, output = Path(directory) / "source", Path(directory) / "output"
            shutil.copytree(ROOT / "src/Vendor/MSDFGen", source)
            (source / "core/Shape.cpp").write_text("invalid")
            with self.assertRaises(ValueError): module.prepare(source, output)
            self.assertFalse(output.exists())
            shutil.copy2(ROOT / "src/Vendor/MSDFGen/core/Shape.cpp", source / "core/Shape.cpp")
            (source / "extra.cpp").touch()
            with self.assertRaises(ValueError): module.prepare(source, output)

    def test_rejects_in_place_output(self):
        source = ROOT / "src/Vendor/MSDFGen"
        for output in (source, source / "generated", source.parent):
            with self.assertRaises(ValueError): module.prepare(source, output)


if __name__ == "__main__":
    unittest.main()
