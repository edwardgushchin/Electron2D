"""Exercise the actual consumer targets against stale packaged audio assets."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]


class AudioDeliveryTests(unittest.TestCase):
    def run_target(self, rid, library=True, publish=True):
        with tempfile.TemporaryDirectory(prefix="electron2d audio delivery ") as directory:
            root = Path(directory)
            source = root / "local library"
            source.write_bytes(b"prepared audio with offline output")
            os.utime(source, (1, 1))
            name = "libFAudio.0.dylib" if rid.startswith("osx-") else "libFAudio.so.0"
            outputs = [root / "build"] + ([root / "publish"] if publish else [])
            for output in outputs:
                native = output / "runtimes" / rid / "native"
                native.mkdir(parents=True)
                (native / name).write_bytes(b"newer packaged audio")
            project = root / "Consumer.proj"
            project.write_text('<Project><PropertyGroup>'
                f'<RuntimeIdentifier>{rid}</RuntimeIdentifier>'
                f'<TargetDir>{escape(str(outputs[0]))}/</TargetDir>'
                + (f'<PublishDir>{escape(str(outputs[1]))}/</PublishDir>' if publish else '')
                + (f'<Electron2DAudioNativeLibrary>{escape(str(source if library is True else root / "missing"))}</Electron2DAudioNativeLibrary>' if library else '')
                + '</PropertyGroup>'
                f'<Import Project="{escape(str(ROOT / "tools/native-consumer.props"))}" />'
                '<Target Name="CopyFilesToOutputDirectory" /><Target Name="Publish" /></Project>')
            result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo",
                "-target:CopyFilesToOutputDirectory;Publish"], capture_output=True, text=True)
            if library == "missing":
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("MSB3030", result.stdout + result.stderr)
            else:
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            expected = source.read_bytes() if library is True and rid.startswith(("linux-", "osx-")) else b"newer packaged audio"
            for output in outputs:
                self.assertEqual((output / "runtimes" / rid / "native" / name).read_bytes(), expected)
                self.assertFalse((output / name).exists())
            self.assertEqual(source.read_bytes(), b"prepared audio with offline output")

    def test_explicit_library_replaces_newer_build_and_publish_assets(self):
        for rid in ("linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
            with self.subTest(rid=rid):
                self.run_target(rid)

    def test_build_without_publish_directory(self):
        self.run_target("linux-x64", publish=False)

    def test_no_override_and_foreign_target_preserve_assets(self):
        self.run_target("linux-x64", library=False)
        self.run_target("android-arm64")

    def test_missing_explicit_library_fails(self):
        self.run_target("linux-x64", library="missing")


if __name__ == "__main__":
    unittest.main()
