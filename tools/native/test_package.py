"""Reject mismatched native architectures and modified pinned source inputs."""

import hashlib
import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
from zipfile import ZipFile

import build_tls
import package
import test_consumer


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
            for code in ("3", "4"):
                television = [outputs[0], outputs[1].replace("platform 7", "platform " + code), outputs[2]]
                with patch.object(package.subprocess, "check_output", side_effect=television):
                    if code == "3":
                        self.assertEqual(package.archive_exports(path, "tvos-arm64"), {"e2d_text_init"})
                    else:
                        with self.assertRaises(ValueError):
                            package.archive_exports(path, "tvos-arm64")
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

    def test_apple_openssl_excludes_cli_helpers_and_keeps_target_flags_together(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            with patch.object(build_tls, "prepare", return_value=(path, path)), \
                    patch.object(build_tls.subprocess, "run") as run, patch.object(build_tls.shutil, "copy2"):
                build_tls.cross("tvossimulator-x64", path, {}, ["-target", "x86_64-apple-tvos15.0-simulator", "-isysroot", "/SDK path"])
                command = run.call_args_list[0].args[0]
                self.assertIn("no-apps", command)
                self.assertIn("CFLAGS=-target x86_64-apple-tvos15.0-simulator -isysroot '/SDK path'", command)
                self.assertNotIn("x86_64-apple-tvos15.0-simulator", command)

    def test_android_openssl_uses_unversioned_libraries_and_private_dependency(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            with patch.object(build_tls, "prepare", return_value=(path, path)), \
                    patch.object(build_tls.subprocess, "run") as run, patch.object(build_tls.shutil, "copy2") as copy:
                build_tls.cross("android-arm64", path, {}, [])
                self.assertEqual([call.args[0].name for call in copy.call_args_list], ["libcrypto.so", "libssl.so"])
                self.assertIn(["patchelf", "--page-size", "16384", "--replace-needed", "libcrypto.so",
                               "libElectron2DCrypto.so", str(path / "libElectron2DSSL.so")],
                              [call.args[0] for call in run.call_args_list])

    def test_consumer_accepts_manifested_native_dlls_but_rejects_managed_assemblies(self):
        with tempfile.TemporaryDirectory() as directory:
            feed = Path(directory)
            path = feed / "Electron2D.Native.Windows.fixture.nupkg"
            payload = b"native fixture"
            manifest = {"win-x64": {"files": {"FAudio.dll": hashlib.sha256(payload).hexdigest()}}}
            for extra in (None, "lib/net10.0/Managed.dll", "Managed.dll"):
                with ZipFile(path, "w") as archive:
                    archive.writestr("native-manifest.json", json.dumps(manifest))
                    archive.writestr("runtimes/win-x64/native/FAudio.dll", payload)
                    if extra:
                        archive.writestr(extra, b"managed fixture")
                if extra:
                    with self.assertRaises(RuntimeError):
                        test_consumer.read_manifests(feed)
                else:
                    self.assertEqual(test_consumer.read_manifests(feed), manifest)

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
