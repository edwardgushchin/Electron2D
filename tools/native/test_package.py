"""Reject mismatched native architectures and modified pinned source inputs."""

import hashlib
import json
import platform
import re
import shutil
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch
from zipfile import ZipFile

import build_tls
import build_webrtc
import package
import test_consumer


class NativePackageTests(unittest.TestCase):
    @unittest.skipUnless(sys.platform == "linux" and all(shutil.which(tool) for tool in ("cc", "nm", "readelf")), "Linux ELF tools are unavailable")
    def test_webp_publish_rejects_encoder_only_and_nonportable_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "bridge.c"
            library = root / "libElectron2DWebP.so"
            rid = "linux-x64" if platform.machine() == "x86_64" else "linux-arm64"

            def build(decoder=True, rpath=False, memory=True):
                source.write_text("void e2d_webp_encode(void) {}\n" + ("void e2d_webp_memory(void) {}\n" if memory else "") + ("void e2d_webp_decode(void) {}\n" if decoder else ""))
                command = ["cc", "-shared", "-fPIC", "-Wl,-soname,libElectron2DWebP.so", str(source), "-o", str(library)]
                if rpath:
                    command.append("-Wl,-rpath,/unshipped-build-directory")
                subprocess.run(command, check=True, capture_output=True)

            build(decoder=False)
            with self.assertRaisesRegex(AssertionError, "WebP"):
                package.check_private_webp(rid, library)
            build(memory=False)
            with self.assertRaisesRegex(AssertionError, "WebP"):
                package.check_private_webp(rid, library)
            build()
            package.check_private_webp(rid, library)
            with self.assertRaisesRegex(AssertionError, "architecture"):
                package.check_private_webp("linux-arm64" if rid == "linux-x64" else "linux-x64", library)
            build(rpath=True)
            with self.assertRaisesRegex(AssertionError, "search path"):
                package.check_private_webp(rid, library)

    @unittest.skipUnless(shutil.which("cmake"), "CMake is unavailable")
    def test_webrtc_windows_finds_the_selected_private_import_libraries(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            tls = root / "tls"
            headers = tls / ("openssl-" + build_tls.VERSION) / "include/openssl"
            headers.mkdir(parents=True)
            (headers / "ssl.h").write_text("")
            (headers / "opensslv.h").write_text("#define OPENSSL_VERSION_MAJOR 3\n#define OPENSSL_VERSION_MINOR 6\n#define OPENSSL_VERSION_PATCH 5\n")
            (tls / "build/include").mkdir(parents=True)
            for name in ("libcrypto.lib", "libssl.lib"):
                (tls / "build" / name).write_bytes(b"selected private import library")
            with patch.object(build_webrtc.subprocess, "run") as run:
                build_webrtc.build("win-x86", root / "bridge", tls)
                options = [arg for arg in run.call_args_list[0].args[0] if arg.startswith("-DOPENSSL_")]
            project = root / "probe"
            project.mkdir()
            (project / "CMakeLists.txt").write_text('''cmake_minimum_required(VERSION 3.24)
project(PrivateOpenSSL NONE)
set(WIN32 TRUE)
set(MSVC TRUE)
set(CMAKE_FIND_LIBRARY_PREFIXES "")
set(CMAKE_FIND_LIBRARY_SUFFIXES ".lib")
find_package(OpenSSL REQUIRED)
if(NOT OPENSSL_CRYPTO_LIBRARY STREQUAL "${EXPECTED}/libcrypto.lib" OR NOT OPENSSL_SSL_LIBRARY STREQUAL "${EXPECTED}/libssl.lib")
  message(FATAL_ERROR "A foreign OpenSSL import library was selected")
endif()
''')
            command = ["cmake", "-S", str(project), "-B", str(root / "found"), "-DEXPECTED=" + str(tls / "build"), *options]
            subprocess.run(command, check=True, capture_output=True, text=True)
            missing = [arg.replace(str(root / "found"), str(root / "missing")) for arg in command if not arg.startswith("-DOPENSSL_ROOT_DIR=")]
            self.assertNotEqual(subprocess.run(missing, capture_output=True).returncode, 0)

    @unittest.skipUnless(shutil.which("cmake"), "CMake is unavailable")
    def test_ktx_architecture_adapter_is_guarded_and_repeatable(self):
        guard = 'if (CPU_ARCHITECTURE STREQUAL x86)\n    message(FATAL_ERROR "This project cannot be built for x86 cpu.")\nendif()'
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "CMakeLists.txt"
            source.write_text("before\n" + guard + "\nafter\n")
            library = root / "lib/CMakeLists.txt"
            library.parent.mkdir()
            library.write_bytes(source.read_bytes())
            command = ["cmake", "-DROOT=" + str(root), "-P", str(package.ROOT / "tools/ktx-native/configure-guards.cmake")]
            subprocess.run(command, check=True)
            patched = source.read_bytes()
            self.assertTrue(patched.startswith(b"before\n") and patched.endswith(b"\nafter\n"))
            self.assertNotIn(b"FATAL_ERROR", patched)
            self.assertEqual(library.read_bytes(), patched)
            subprocess.run(command, check=True)
            self.assertEqual(source.read_bytes(), patched)
            self.assertEqual(library.read_bytes(), patched)
            source.write_text("unknown upstream source\n")
            result = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("guard no longer matches", result.stderr)

    @unittest.skipUnless(all(shutil.which(tool) for tool in ("cc", "ar", "cmake")), "Native archive tools are unavailable")
    def test_static_ktx_closure_links_decoder_and_encoder(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ("bridge", "codec", "encoder"):
                source = root / (name + ".c")
                source.write_text("int " + name + "(void) { return 1; }\n")
                subprocess.run(["cc", "-c", str(source), "-o", str(root / (name + ".o"))], check=True)
            for name, members in (("bridge", ("bridge",)), ("codec", ("codec", "encoder"))):
                subprocess.run(["ar", "rcs", str(root / (name + ".a")),
                                *[str(root / (member + ".o")) for member in members]], check=True)
            subprocess.run(["cmake", "-DOUTPUT=" + str(root / "bridge.a"), "-DCODEC=" + str(root / "codec.a"),
                            "-DAR=" + shutil.which("ar"),
                            "-DAPPLE_HOST=" + ("ON" if sys.platform == "darwin" else "OFF"),
                            "-P", str(package.ROOT / "tools/ktx-native/merge.cmake")], check=True)
            members = subprocess.check_output(["ar", "t", str(root / "bridge.a")], text=True).splitlines()
            self.assertEqual(members.count("encoder.o"), 1)
            source = root / "main.c"
            source.write_text("int bridge(void), codec(void), encoder(void); int main(void) { return bridge() + codec() + encoder() != 3; }\n")
            executable = root / "consumer"
            archive = str(root / "bridge.a")
            load = ["-Wl,-force_load," + archive] if sys.platform == "darwin" else ["-Wl,--whole-archive", archive, "-Wl,--no-whole-archive"]
            subprocess.run(["cc", str(source), *load, "-o", str(executable)], check=True)
            subprocess.run([str(executable)], check=True)

    def test_private_codec_export_lists_match_the_required_abi(self):
        for component, name in (("mesh-native", "libElectron2DMesh.so"), ("ktx-native", "libElectron2DKTX.so"), ("render-native", "libElectron2DRender.so")):
            directory = package.ROOT / "tools" / component
            expected = package.required_exports(name)
            for filename in ("exports.map", "exports.macos", "exports.def"):
                with self.subTest(component=component, filename=filename):
                    symbols = set(re.findall(r"\be2d_\w+", (directory / filename).read_text().replace("_e2d_", "e2d_")))
                    self.assertEqual(symbols, expected)

    def test_text_unicode_operations_are_in_every_private_abi(self):
        additions = {"e2d_text_case", "e2d_text_strip_diacritics", "e2d_text_character_properties", "e2d_text_is_identifier", "e2d_spoof_open", "e2d_spoof_close", "e2d_spoof_skeleton", "e2d_spoof_check", "e2d_text_memory"}
        for profile in ("Linux", "MacOS", "Windows"):
            name = next(name for name in package.LIBRARIES[profile] if "Electron2DTextBreak" in name)
            exports = package.required_exports(name)
            self.assertEqual(len(exports), 26)
            self.assertTrue(additions <= exports)
        native = package.ROOT / "src/Servers/Text/Native"
        for symbol in additions:
            self.assertIn(symbol + ";", (native / "exports.map").read_text())
            self.assertIn("_" + symbol + "\n", (native / "exports-macos.txt").read_text())

    def test_graphics_storage_private_exports(self):
        additions = {"e2d_render_graphics_storage_support", "e2d_render_storage_barrier"}
        native = package.ROOT / "tools/render-native"
        for profile in ("Linux", "MacOS", "Windows", "iOS"):
            name = next(name for name in package.LIBRARIES[profile] if "Electron2DRender" in name)
            self.assertTrue(additions <= package.required_exports(name))
        for symbol in additions:
            self.assertIn(symbol + ";", (native / "exports.map").read_text())
            self.assertIn("_" + symbol + "\n", (native / "exports.macos").read_text())
            self.assertIn("    " + symbol + "\n", (native / "exports.def").read_text())

    def test_regex_private_sources_and_symbols(self):
        import subprocess
        subprocess.run(["python3", "-B", str(package.ROOT / "tools/update-pcre2.py"), "--check"], check=True)
        additions = {"e2d_regex_" + name for name in ("create", "destroy", "info", "match", "substitute", "error")}
        self.assertTrue(additions <= package.TEXT_EXPORTS)
        native = package.ROOT / "src/Servers/Text/Native"
        for symbol in additions:
            self.assertIn(symbol + ";", (native / "exports.map").read_text())
            self.assertIn("_" + symbol + "\n", (native / "exports-macos.txt").read_text())
        for platform in package.LIBRARIES:
            name = next(name for name in package.LIBRARIES[platform] if "TextBreak" in name)
            self.assertTrue(additions <= package.required_exports(name))

    def test_text_static_symbols_cannot_collide_with_application_pcre2(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in package.LIBRARIES["iOS"]:
                (root / name).write_bytes(b"!<arch>\n")
            for symbol in ("pcre2_compile_32", "_pcre2_match_32"):
                def exports(path, rid):
                    return package.required_exports(path.name) | ({symbol} if "TextBreak" in path.name else set())
                with patch.object(package, "archive_exports", side_effect=exports):
                    with self.assertRaisesRegex(ValueError, "PCRE2 symbols"):
                        package.inspect("ios-arm64", root)

    def test_webrtc_desktop_exports_and_pinned_sources(self):
        for profile in ("Linux", "MacOS", "Windows"):
            name = next(name for name in package.LIBRARIES[profile] if "Electron2DWebRTC" in name)
            self.assertEqual(package.required_exports(name), package.WEBRTC_EXPORTS)
            self.assertEqual(len(package.required_exports(name)), 23)
        root = package.ROOT / "src/Vendor/libdatachannel"
        manifest = json.loads((root / "manifest.json").read_text())
        self.assertEqual(manifest["commit"], "6b1e2e620f1e37f0eafeee702eaea0043cb305fd")
        self.assertEqual({p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file() and p.name != "manifest.json"}, set(manifest["files"]))
        for name, checksum in manifest["files"].items():
            self.assertEqual(hashlib.sha256((root / name).read_bytes()).hexdigest(), checksum, name)

    def test_webrtc_private_tls_build_rejects_missing_tls(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(FileNotFoundError):
                build_webrtc.build("win-x64", root / "bridge", root / "missing-tls")
            with self.assertRaises(ValueError):
                build_webrtc.build("browser-wasm", root / "bridge", root / "tls")

    def test_webp_decoder_and_animation_closure_are_required(self):
        for profile, names in package.LIBRARIES.items():
            name = next(name for name in names if "Electron2DWebP" in name)
            self.assertEqual(package.required_exports(name), {"e2d_webp_encode", "e2d_webp_decode", "e2d_webp_memory"})
            if profile in ("iOS", "tvOS", "Web"):
                self.assertEqual(names.count("libwebpdemux.a"), 1)
                self.assertIn("WebPAnimDecoderGetNext", package.required_exports("libwebpdemux.a"))

    def test_ktx_bridge_is_required_in_every_profile(self):
        for profile, names in package.LIBRARIES.items():
            name = next(name for name in names if "Electron2DKTX" in name)
            self.assertEqual(package.required_exports(name), {"e2d_ktx_create", "e2d_ktx_level", "e2d_ktx_destroy", "e2d_basis_create", "e2d_block_decode", "e2d_image_encode", "e2d_image_encoded_free"})

    def test_ktx_static_symbols_cannot_collide_with_font_codecs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in package.LIBRARIES["iOS"]:
                (root / name).write_bytes(b"!<arch>\n")
            def exports(path, rid):
                return package.required_exports(path.name) | ({symbol} if "Electron2DKTX" in path.name else set())
            for symbol in ("ZSTD_getFrameContentSize", "ZSTDMT_freeCCtx", "XXH64", "FSE_decompress"):
                with patch.object(package, "archive_exports", side_effect=exports):
                    with self.assertRaisesRegex(ValueError, "global codec symbols"):
                        package.inspect("ios-arm64", root)
            symbol = "e2d_ktx_private_ZSTDMT_freeCCtx"
            with patch.object(package, "archive_exports", side_effect=exports):
                self.assertEqual(set(package.inspect("ios-arm64", root)), set(package.LIBRARIES["iOS"]))

    def test_ogv_bridge_is_required_in_every_profile(self):
        for profile, names in package.LIBRARIES.items():
            name = next(name for name in names if "Electron2DOGV" in name)
            self.assertEqual(package.required_exports(name), {"e2d_ogv_create", "e2d_ogv_frame", "e2d_ogv_finish", "e2d_ogv_destroy",
                                                            "e2d_video_create", "e2d_video_frame", "e2d_video_audio", "e2d_video_seek_audio", "e2d_video_destroy"})

    def test_memory_and_codec_exports_are_part_of_every_enet_payload(self):
        for name in ['libElectron2DENet.so', 'Electron2DENet.dll', 'libElectron2DENet.a']:
            exports = package.required_exports(name)
            self.assertEqual(len(exports), 29)
            self.assertTrue({'e2d_enet_memory', 'e2d_enet_callbacks', 'e2d_file_encode', 'e2d_file_decode',
                             'e2d_gzip_create', 'e2d_gzip_process', 'e2d_gzip_reset', 'e2d_gzip_destroy',
                             'e2d_memory_abi', 'e2d_memory_allocate', 'e2d_memory_reallocate', 'e2d_memory_release',
                             'e2d_memory_snapshot', 'e2d_memory_lock', 'e2d_memory_unlock',
                             'e2d_memory_managed_locked', 'e2d_memory_snapshot_locked'} <= exports)

    def test_windows_producer_copies_tls_by_name_after_mesh_insertion(self):
        with patch.dict("sys.modules", {"windows": Mock()}):
            import build_windows
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with patch.object(build_windows.platform, "system", return_value="Windows"), \
                 patch.dict(build_windows.os.environ, {"VSCMD_ARG_TGT_ARCH": "x64"}), \
                 patch.object(build_windows.windows, "sdl_import_library"), \
                 patch.object(build_windows.subprocess, "run"), \
                 patch.object(build_windows.build_tls, "build"), \
                 patch.object(build_windows.build_webrtc, "build"), \
                 patch.object(build_windows.package, "stage"), \
                 patch.object(build_windows.shutil, "copy2") as copy:
                build_windows.build("win-x64", root / "SDL3.dll", root / "artifacts")
            sources = [call.args[0] for call in copy.call_args_list]
            self.assertEqual([path.name for path in sources if path.parent.name == "tls"],
                             ["libcrypto-3-Electron2D.dll", "libssl-3-Electron2D.dll"])
            self.assertIn("Electron2DMesh.dll", [path.name for path in sources])

    def test_default_elf_symbol_versions_resolve_but_nondefault_versions_do_not(self):
        symbols = "0001 T BIO_s_dgram_pair@@OPENSSL_3.2.0\n0002 T ERR_get_error@@OPENSSL_3.0.0\n0003 T TLS_method@LEGACY\n0004 T e2d_audio_select_output\n"
        with patch.object(package.subprocess, "check_output", return_value=symbols):
            exports = package.elf_exports(Path("fixture.so"))
            self.assertEqual(exports, {"BIO_s_dgram_pair", "ERR_get_error", "TLS_method@LEGACY", "e2d_audio_select_output"})
            self.assertNotIn("TLS_method", exports)

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
            audit = root / "tools/check_native_publish.py"
            audit.parent.mkdir(parents=True)
            audit.write_text("assert True\n")
            with patch.object(package, "ROOT", root):
                original = package.source_hash()
                self.assertEqual(original, package.source_hash())
                recipe.write_text("container: ubuntu:26.04\n")
                self.assertNotEqual(original, package.source_hash())
                updated = package.source_hash()
                audit.write_text("assert False\n")
                self.assertNotEqual(updated, package.source_hash())

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
                return "000001 T _e2d_audio_output_latency\n000002 T _e2d_audio_select_output\n000003 T _e2d_audio_create_offline_master\n000004 T _e2d_audio_mix_offline\n"

            with patch.object(package.subprocess, "check_output", side_effect=command):
                self.assertEqual(package.macos_exports(library, "osx-arm64", library.name),
                                 {"e2d_audio_output_latency", "e2d_audio_select_output", "e2d_audio_create_offline_master", "e2d_audio_mix_offline"})
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
            path = feed / "Electron2D.Windows.fixture.nupkg"
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

    def test_linux_freetype_rejects_global_codec_dependencies(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in package.LIBRARIES["Linux"]:
                data = bytearray(64)
                data[:6] = b"\x7fELF\x02\x01"
                data[18:20] = (62).to_bytes(2, "little")
                (root / name).write_bytes(data)
            for dependency in ("libc.so.6", "libpng16.so.16", "libz.so.1", "libbrotlidec.so.1", "libharfbuzz.so.0"):
                def dynamic(command, **kwargs):
                    name = Path(command[-1]).name
                    needed = "libSDL3.so.0" if name == "libFAudio.so.0" else dependency if "FreeType" in name else "libc.so.6"
                    return f"(SONAME) [{name}]\n(NEEDED) [{needed}]\n"
                with patch.object(package.subprocess, "check_output", side_effect=dynamic), \
                        patch.object(package, "elf_exports", side_effect=lambda path: package.required_exports(path.name)), \
                        patch.object(package, "check_private_text"), patch.object(package, "check_private_webrtc"), \
                        patch.object(package, "check_private_webp"):
                    if dependency == "libc.so.6":
                        package.inspect("linux-x64", root)
                    else:
                        with self.assertRaisesRegex(ValueError, "statically linked"):
                            package.inspect("linux-x64", root)

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
