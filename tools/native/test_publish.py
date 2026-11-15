"""Check runtime-pack selection before native payload inspection."""

import json
import hashlib
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_native_publish import check
sys.path.insert(0, str(Path(__file__).resolve().parent))
import package


class PublishTests(unittest.TestCase):
    def make_windows_publish(self, publish):
        (publish / "Consumer.exe").touch()
        (publish / "Electron2D.dll").touch()
        libraries = dict.fromkeys(("SDL3-CS.Windows/3.4.18", "SDL3-CS.Windows.Image/3.4.6.12",
            "SDL3-CS.Windows.Shadercross/3.0.0.13", "runtimepack.Microsoft.NETCore.App.Runtime.win-x64/10.0.1"), {})
        libraries["Electron2D.Windows/0.2.0-alpha"] = dict(type="package")
        (publish / "Consumer.deps.json").write_text(json.dumps(dict(libraries=libraries, targets={})))
        (publish / "Consumer.runtimeconfig.json").write_text(json.dumps(dict(runtimeOptions=dict(
            includedFrameworks=[dict(name="Microsoft.NETCore.App", version="10.0.1")]))))
        native = publish / "runtimes/win-x64/native"
        native.mkdir(parents=True)
        names = set(package.LIBRARIES["Windows"]) | {"SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll"}
        for name in names:
            (native / name).write_bytes(b"native fixture")
        hashes = {name: hashlib.sha256((native / name).read_bytes()).hexdigest() for name in package.LIBRARIES["Windows"]}
        (publish / "Electron2D.native-manifest.json").write_text(json.dumps({"win-x64": dict(files=hashes)}))

    def test_retired_foreign_runtime_file_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            publish = Path(directory)
            self.make_windows_publish(publish)
            foreign = publish / "runtimes/win-arm64/native/Electron2DWebRTC.dll"
            foreign.parent.mkdir(parents=True)
            foreign.write_bytes(b"retired module")
            with self.assertRaisesRegex(AssertionError, "Retired Windows payload leaked"):
                check("win-x64", publish, "Consumer")

    def test_selected_foreign_application_file_reaches_native_audit(self):
        with tempfile.TemporaryDirectory() as directory:
            publish = Path(directory)
            self.make_windows_publish(publish)
            relative = "runtimes/win-arm64/native/libHarfBuzzSharp.dll"
            selected = publish / relative
            selected.parent.mkdir(parents=True)
            selected.write_bytes(b"application fixture")
            checksum = hashlib.sha256(selected.read_bytes()).hexdigest()
            (publish / "Electron2D.application-native.sha256").write_text(f"{checksum}  {relative}\n")
            with mock.patch.object(package, "windows_exports", side_effect=RuntimeError("Reached PE audit")):
                with self.assertRaisesRegex(RuntimeError, "Reached PE audit"):
                    check("win-x64", publish, "Consumer")

    def test_duplicate_application_receipt_path_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            publish = Path(directory)
            self.make_windows_publish(publish)
            selected = publish / "libHarfBuzzSharp.dll"
            selected.write_bytes(b"application fixture")
            checksum = hashlib.sha256(selected.read_bytes()).hexdigest()
            line = f"{checksum}  libHarfBuzzSharp.dll\n"
            (publish / "Electron2D.application-native.sha256").write_text(line + line)
            with mock.patch.object(package, "windows_exports", side_effect=RuntimeError("Reached PE audit")):
                with self.assertRaisesRegex(AssertionError, "Duplicate application native receipt entry"):
                    check("win-x64", publish, "Consumer")

    def test_runtime_pack_matches_the_self_contained_framework(self):
        for version, framework, expected in (
                ("10.0.1", "10.0.1", "Missing native render/text files"),
                ("10.0.8", "10.0.8", "Missing native render/text files"),
                ("10.0.1", "10.0.8", "Missing self-contained runtime pack"),
                ("10.0.1", None, "Missing self-contained runtime pack")):
            with self.subTest(version=version, framework=framework), tempfile.TemporaryDirectory() as directory:
                publish = Path(directory)
                (publish / "Consumer.exe").touch()
                (publish / "Electron2D.dll").touch()
                libraries = dict.fromkeys(("SDL3-CS.Windows/3.4.18", "SDL3-CS.Windows.Image/3.4.6.12",
                    "SDL3-CS.Windows.Shadercross/3.0.0.13", "runtimepack.Microsoft.NETCore.App.Runtime.win-x64/" + version), {})
                (publish / "Consumer.deps.json").write_text(json.dumps(dict(libraries=libraries)))
                runtime = dict(tfm="net10.0")
                if framework:
                    runtime["includedFrameworks"] = [dict(name="Microsoft.NETCore.App", version=framework)]
                else:
                    runtime["framework"] = dict(name="Microsoft.NETCore.App", version=version)
                (publish / "Consumer.runtimeconfig.json").write_text(json.dumps(dict(runtimeOptions=runtime)))
                with self.assertRaisesRegex(AssertionError, expected):
                    check("win-x64", publish, "Consumer")


if __name__ == "__main__":
    unittest.main()
