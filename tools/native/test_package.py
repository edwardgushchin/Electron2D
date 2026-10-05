"""Reject mismatched native architectures and modified pinned source inputs."""

import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

import build_tls
import package


class NativePackageTests(unittest.TestCase):
    def test_every_declared_rid_has_a_native_payload_and_package(self):
        matrix = json.loads((package.ROOT / "tools/ci/rids.json").read_text())
        self.assertEqual({row["rid"] for row in matrix}, set(package.RIDS))
        self.assertEqual(len(package.RIDS), len(set(package.RIDS)))
        for row in matrix:
            self.assertEqual(package.platform(row["rid"]), row["platform"])
            self.assertTrue(package.LIBRARIES[row["platform"]])

    def test_static_archives_reject_wrong_device_and_object_architecture(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "fixture.a"
            path.write_bytes(b"!<arch>\n")
            outputs = ["MH_MAGIC_64 ARM64 ALL OBJECT\n", "cmd LC_BUILD_VERSION\ncmdsize 24\nplatform 7\n",
                       "000000 T _e2d_text_init\n"]
            with patch.object(package.subprocess, "check_output", side_effect=outputs):
                self.assertEqual(package.archive_exports(path, "iossimulator-arm64"), {"e2d_text_init"})
            for rid in ("ios-arm64", "tvossimulator-arm64", "iossimulator-x64"):
                with patch.object(package.subprocess, "check_output", side_effect=outputs[:2]):
                    with self.assertRaises(ValueError):
                        package.archive_exports(path, rid)
            for arch in ("aarch64", "x86_64", ""):
                with patch.object(package.subprocess, "check_output", return_value="Format: WASM\nArch: " + arch + "\n"):
                    with self.assertRaises(ValueError):
                        package.archive_exports(path, "browser-wasm")

    def test_source_fingerprint_includes_the_native_build_recipe(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            recipe = root / ".github/workflows/native.yml"
            recipe.parent.mkdir(parents=True)
            recipe.write_text("container: ubuntu:22.04\n")
            with patch.object(package, "ROOT", root):
                original = package.source_hash()
                self.assertEqual(original, package.source_hash())
                recipe.write_text("container: ubuntu:26.04\n")
                self.assertNotEqual(original, package.source_hash())

    def test_macos_identity_architecture_imports_and_exports(self):
        with tempfile.TemporaryDirectory() as directory:
            library = Path(directory) / "libFAudio.0.dylib"
            library.write_bytes(struct.pack("<II", 0xfeedfacf, 0x0100000c) + bytes(24))

            def command(args, **kwargs):
                if args[1] == "-D":
                    return str(library) + ":\n@rpath/libFAudio.0.dylib\n"
                if args[1] == "-l":
                    return "cmd LC_ID_DYLIB\n"
                if args[1] == "-L":
                    return str(library) + ":\n@rpath/libSDL3.0.dylib (compatibility version 0.0.0)\n"
                return "000001 T _e2d_audio_output_latency\n000002 T _e2d_audio_select_output\n"

            with patch.object(package.subprocess, "check_output", side_effect=command):
                self.assertEqual(package.macos_exports(library, "osx-arm64", library.name),
                                 {"e2d_audio_output_latency", "e2d_audio_select_output"})
                with self.assertRaises(ValueError):
                    package.macos_exports(library, "osx-x64", library.name)
            with patch.object(package.subprocess, "check_output", return_value=str(library) + ":\n/build-machine/libFAudio.0.dylib\n"):
                with self.assertRaises(ValueError):
                    package.macos_exports(library, "osx-arm64", library.name)

    def test_openssl_source_is_verified_before_extraction(self):
        with tempfile.TemporaryDirectory() as directory:
            archive = Path(directory) / "fixture.tar.gz"
            archive.write_bytes(b"fixture")
            with self.assertRaises(ValueError):
                build_tls.checked_archive(archive)
            with patch.object(build_tls, "SHA256", hashlib.sha256(b"fixture").hexdigest()):
                build_tls.checked_archive(archive)

    def test_macos_freetype_rejects_global_codec_dependencies(self):
        with tempfile.TemporaryDirectory() as directory:
            library = Path(directory) / "libElectron2DFreeType.dylib"
            library.write_bytes(struct.pack("<II", 0xfeedfacf, 0x0100000c) + bytes(24))
            for dependency in ("/usr/lib/libSystem.B.dylib", "@rpath/libpng16.dylib",
                               "@rpath/libz.1.dylib", "@rpath/libbrotlidec.dylib", "@rpath/libharfbuzz.dylib"):
                outputs = [str(library) + ":\n@rpath/" + library.name + "\n", "cmd LC_ID_DYLIB\n",
                           str(library) + ":\n" + dependency + "\n", "000001 T _FT_Init_FreeType\n"]
                with patch.object(package.subprocess, "check_output", side_effect=outputs):
                    if dependency.startswith("/usr/lib/"):
                        self.assertEqual(package.macos_exports(library, "osx-arm64", library.name), {"FT_Init_FreeType"})
                    else:
                        with self.assertRaises(ValueError):
                            package.macos_exports(library, "osx-arm64", library.name)


if __name__ == "__main__":
    unittest.main()
