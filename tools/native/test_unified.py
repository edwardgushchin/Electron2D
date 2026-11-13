"""Check the unified ABI, loader identities and archive ownership."""
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import unittest
import merge
import package


class UnifiedNativeTests(unittest.TestCase):
    def test_every_profile_has_one_engine_module(self):
        for profile, names in package.LIBRARIES.items():
            engine = [name for name in names if name.startswith("libElectron2D.")]
            self.assertEqual(len(engine), 1, profile)
            self.assertFalse(any(re.search(r"Electron2D(?:Render|Mesh|TextBreak|ENet|WebRTC|OGV|KTX|WebP)", name) for name in names), profile)

    def test_allowlist_contains_every_managed_engine_import(self):
        expected = package.required_exports("libElectron2D.so")
        for path in (package.ROOT / "src").rglob("*.cs"):
            if "Vendor" in path.parts:
                continue
            source = path.read_text(encoding="utf-8")
            if "NativeLibraries.EngineLibrary" not in source:
                continue
            libraries = [r'NativeLibraries\.EngineLibrary']
            libraries.extend(re.findall(r'\bconst\s+string\s+(\w+)\s*=\s*NativeLibraries\.EngineLibrary\s*;', source))
            imports = re.finditer(
                r'\[LibraryImport\((' + '|'.join(libraries) + r')\b([^\]]*)\)\]'
                r'(?:\s*\[[^\r\n]*\])*\s*(?:\w+\s+)*?\bpartial\s+\S+\s+(\w+)\s*\(', source)
            for match in imports:
                entry = re.search(r'EntryPoint\s*=\s*"([^"]+)"', match[2])
                symbol = entry[1] if entry else match[3]
                with self.subTest(path=path, symbol=symbol):
                    self.assertTrue(symbol in expected, f"Missing unified export: {symbol}")

    @unittest.skipUnless(all(shutil.which(tool) for tool in ("cc", "ar")), "Archive tools are unavailable")
    def test_archive_merge_preserves_colliding_member_basenames(self):
        with tempfile.TemporaryDirectory(prefix="native archives ") as directory:
            root = Path(directory)
            inputs = []
            for index, function in enumerate(("first", "second")):
                folder = root / str(index)
                folder.mkdir()
                source = folder / "member.c"
                source.write_text("int " + function + "(void) { return 1; }\n")
                subprocess.run(["cc", "-c", str(source), "-o", str(folder / "member.o")], check=True)
                archive = folder / "input.a"
                subprocess.run(["ar", "rcs", str(archive), str(folder / "member.o")], check=True)
                inputs.append(archive)
            output = root / "libElectron2D.a"
            merge.merge(output, shutil.which("ar"), inputs)
            source = root / "main.c"
            source.write_text("int first(void), second(void); int main(void) { return first() + second() != 2; }\n")
            subprocess.run(["cc", str(source), str(output), "-o", str(root / "consumer")], check=True)
            subprocess.run([str(root / "consumer")], check=True)

    @unittest.skipUnless(all(shutil.which(tool) for tool in ("cmake", "cc", "ar")), "Native tools are unavailable")
    def test_cmake_archive_closure_and_incremental_rebuild(self):
        with tempfile.TemporaryDirectory(prefix="native closure ") as directory:
            root = Path(directory)
            (root / "unified.c").write_text("void unused(void) {}\n")
            (root / "core.c").write_text("int core(void) { return 1; }\n")
            (root / "bridge.c").write_text("int core(void); int bridge(void) { return core(); }\n")
            (root / "main.c").write_text("int bridge(void); int main(void) { return bridge() != 2; }\n")
            (root / "CMakeLists.txt").write_text("""cmake_minimum_required(VERSION 3.24)
project(ArchiveClosure C)
add_library(core STATIC core.c)
add_library(alias ALIAS core)
add_library(bridge STATIC bridge.c)
target_link_libraries(bridge PRIVATE alias)
add_library(engine STATIC unified.c)
include("@MERGE@")
e2d_merge_archives(engine bridge)
add_executable(consumer main.c)
target_link_libraries(consumer PRIVATE engine)
""".replace("@MERGE@", str(package.ROOT / "tools/native/merge.cmake")))
            subprocess.run(["cmake", "-S", str(root), "-B", str(root / "build")], check=True, capture_output=True)
            for value in (1, 2):
                (root / "core.c").write_text("int core(void) { return " + str(value) + "; }\n")
                subprocess.run(["cmake", "--build", str(root / "build")], check=True, capture_output=True)
                result = subprocess.run([str(root / "build/consumer")])
                self.assertEqual(result.returncode, 1 if value == 1 else 0)

    def test_archive_merge_rejects_missing_or_self_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "libElectron2D.a"
            output.write_bytes(b"original")
            for inputs in ([], [output], [output.parent / "missing.a"]):
                with self.assertRaises(ValueError):
                    merge.merge(output, "ar", inputs)
                self.assertEqual(output.read_bytes(), b"original")


if __name__ == "__main__":
    unittest.main()
