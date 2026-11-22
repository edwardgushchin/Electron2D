#!/usr/bin/env python3
"""Check a self-contained desktop application and its private native ABIs."""

import json
import hashlib
import re
import subprocess
import sys
from pathlib import Path


TEXT_EXPORTS = {
    "e2d_regex_create", "e2d_regex_destroy", "e2d_regex_info", "e2d_regex_match", "e2d_regex_substitute", "e2d_regex_error",
    "e2d_text_init", "e2d_text_memory", "e2d_text_versions", "e2d_text_is_nonprinting",
    "e2d_text_case", "e2d_text_strip_diacritics", "e2d_text_character_properties", "e2d_text_is_identifier",
    "e2d_spoof_open", "e2d_spoof_close", "e2d_spoof_skeleton", "e2d_spoof_check",
    "e2d_break_open", "e2d_break_set", "e2d_break_first", "e2d_break_next",
    "e2d_break_status", "e2d_break_close", "e2d_font_msdf_generate", "e2d_font_msdf_free",
}


WEBRTC_EXPORTS = {"e2d_rtc_" + name for name in (
    "tls_version",
    "error", "create", "destroy", "close", "state", "offer", "local", "remote", "candidate", "event",
    "event_kind", "event_string", "event_channel", "event_release", "channel_create", "channel_destroy",
    "channel_close", "channel_poll", "channel_info", "channel_string", "channel_size", "channel_read", "channel_send")}

WEBP_EXPORTS = {"e2d_webp_encode", "e2d_webp_decode", "e2d_webp_memory"}


def check_private_webp(rid: str, library: Path) -> None:
    header = library.read_bytes()[:20]
    assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "WebP ELF architecture mismatch"
    dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(library)], text=True)
    assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libElectron2DWebP.so"], "WebP SONAME mismatch"
    assert not re.search(r"\((RPATH|RUNPATH)\)", dynamic), "WebP retains a build-machine search path"
    dependencies = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
    allowed = {"libm.so.6", "libc.so.6", {"linux-x64": "ld-linux-x86-64.so.2", "linux-arm64": "ld-linux-aarch64.so.1"}[rid]}
    assert dependencies <= allowed, f"Unreviewed WebP dependencies: {dependencies}"
    symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(library)], text=True)
    assert WEBP_EXPORTS <= {line.split()[-1] for line in symbols.splitlines()}, "WebP encoder/decoder bridge exports are missing"


def check_private_webrtc(rid: str, library: Path) -> None:
    header = library.read_bytes()[:20]
    assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "WebRTC ELF architecture mismatch"
    dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(library)], text=True)
    assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libElectron2DWebRTC.so"], "WebRTC SONAME mismatch"
    paths = re.findall(r"\((?:RPATH|RUNPATH)\).*\[([^]]+)\]", dynamic)
    assert paths == ["$ORIGIN"], "WebRTC must resolve its packaged TLS pair beside the bridge"
    dependencies = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
    allowed = {"libssl.so.4", "libcrypto.so.4", "libstdc++.so.6", "libm.so.6", "libgcc_s.so.1", "libc.so.6", "libpthread.so.0", "libdl.so.2", {"linux-x64": "ld-linux-x86-64.so.2", "linux-arm64": "ld-linux-aarch64.so.1"}[rid]}
    assert {"libssl.so.4", "libcrypto.so.4"} <= dependencies <= allowed, f"Unreviewed WebRTC dependencies: {dependencies}"
    symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(library)], text=True)
    assert {line.split()[-1] for line in symbols.splitlines()} == WEBRTC_EXPORTS, "WebRTC private ABI mismatch"


def check_private_text(rid: str, library: Path) -> None:
    with library.open("rb") as stream:
        header = stream.read(20)
    assert header[:6] == b"\x7fELF\x02\x01", "Expected a little-endian ELF64 private text library"
    machine = {"linux-x64": 62, "linux-arm64": 183}[rid]
    assert int.from_bytes(header[18:20], "little") == machine, f"Private text library has the wrong architecture for {rid}"
    symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(library)], text=True)
    exports = {line.split()[-1] for line in symbols.splitlines() if line.strip()}
    assert exports == TEXT_EXPORTS, f"Unexpected private text exports: {exports ^ TEXT_EXPORTS}"
    dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(library)], text=True)
    needed = re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic)
    assert not any(name.lower().startswith(("libicu", "libpcre2")) for name in needed), f"Private text library depends on global text/regex libraries: {needed}"
    sonames = re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic)
    assert sonames == ["libElectron2DTextBreak.so"], f"Unexpected private text SONAME: {sonames}"


def check(rid: str, publish: Path, app_name: str = "CharacterMovement") -> None:
    if not app_name or Path(app_name).name != app_name:
        raise ValueError("The application name must be a nonempty basename")
    if rid.startswith("win-"):
        platform, host, names = "Windows", app_name + ".exe", ("SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll", "libElectron2D.dll", "freetype.dll", "harfbuzz.dll", "libcrypto-4.dll", "libssl-4.dll")
    elif rid.startswith("osx-"):
        platform, host, names = "MacOS", app_name, ("libSDL3.dylib", "libSDL3_image.dylib", "libSDL3_shadercross.dylib", "libElectron2D.dylib", "libElectron2DFreeType.dylib", "libHarfBuzzSharp.dylib", "libElectron2DCrypto.4.dylib", "libElectron2DSSL.4.dylib")
    elif rid.startswith("linux-"):
        platform, host, names = "Linux", app_name, ("libSDL3.so", "libSDL3_image.so", "libSDL3_shadercross.so", "libElectron2D.so", "libElectron2DFreeType.so", "libHarfBuzzSharp.so", "libcrypto.so.4", "libssl.so.4")
    else:
        raise ValueError(f"No self-contained desktop host for {rid}")

    assert (publish / host).is_file() and (publish / "Electron2D.dll").is_file(), "Missing application host or engine"
    dependencies = json.loads((publish / (app_name + ".deps.json")).read_text())
    packages = dependencies["libraries"]
    expected = {
        f"SDL3-CS.{platform}/3.4.20",
        f"SDL3-CS.{platform}.Image/3.4.6.12",
        f"SDL3-CS.{platform}.Shadercross/3.0.0.13",
    }
    actual = {name for name in packages if name.startswith("SDL3-CS.")}
    if platform != "Windows":
        assert not any(name.startswith("HarfBuzzSharp.NativeAssets.") for name in packages), "Shaping must use the selected source-built native payload"
    assert actual == expected, f"Wrong SDL packages: {actual}"
    runtime = json.loads((publish / (app_name + ".runtimeconfig.json")).read_text())["runtimeOptions"]
    version = next((framework["version"] for framework in runtime.get("includedFrameworks", [])
                    if framework["name"] == "Microsoft.NETCore.App"), "")
    assert re.fullmatch(r"10\.0\.\d+", version) and f"runtimepack.Microsoft.NETCore.App.Runtime.{rid}/{version}" in packages, "Missing self-contained runtime pack"
    native = publish / "runtimes" / rid / "native"
    assert all((native / name).is_file() for name in names), f"Missing native render/text files: {names}"
    if platform != "Windows":
        assert not any((publish / name).exists() for name in names), "Native render/text files must retain their RID directory"
        assert {path.name for path in (publish / "runtimes").iterdir()} == {rid}, "Foreign RID assets leaked into the game"
    platform_packages = {name.split("/")[0] for name, library in packages.items() if name.startswith("Electron2D.") and library["type"] == "package"}
    assert platform_packages == {f"Electron2D.{platform}"}, f"Wrong engine platform packages: {platform_packages}"
    sys.path.insert(0, str(Path(__file__).resolve().parent / "native"))
    import package as native_package
    if platform == "Windows":
        receipt = json.loads((publish / "Electron2D.native-manifest.json").read_text())[rid]
        assert set(receipt["files"]) == set(native_package.LIBRARIES["Windows"]), "Wrong Windows native manifest"
        application_files = set()
        native_directories = [directory for directory in (publish / "runtimes").glob("*/native") if directory.is_dir()]
        retired_files = {directory / name for directory in (publish, *native_directories)
                         for name in native_package.retired_windows_files()}
        cleanup_names = set(native_package.retired_windows_files()) | set(receipt["files"])
        for target in dependencies["targets"].values():
            for owner, assets in target.items():
                if owner.startswith("SDL3-CS.Windows"):
                    cleanup_names.update(Path(path).name for path in assets.get("native", {}))
                    cleanup_names.update(Path(path).name for path, metadata in assets.get("runtimeTargets", {}).items()
                                         if metadata.get("assetType") == "native")
        application_candidates = {directory / name for directory in (publish, *native_directories)
                                  for name in cleanup_names}
        application_receipt = publish / "Electron2D.application-native.sha256"
        if application_receipt.is_file():
            for line in application_receipt.read_text(encoding="utf-8-sig").splitlines():
                checksum, relative = line.split(maxsplit=1)
                path = publish / relative.replace("\\", "/")
                assert path in application_candidates and re.fullmatch(r"[0-9A-Fa-f]{64}", checksum), "Invalid application native receipt entry"
                assert path not in application_files, "Duplicate application native receipt entry"
                assert path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest() == checksum.lower(), f"Application native checksum mismatch: {relative}"
                application_files.add(path)
        else:
            for target in dependencies["targets"].values():
                for owner, assets in target.items():
                    if owner.startswith("HarfBuzzSharp.NativeAssets."):
                        application_files.update(native / Path(path).name for path in assets.get("native", {}))
                        application_files.update(native / Path(path).name for path, metadata in assets.get("runtimeTargets", {}).items()
                                                if metadata.get("assetType") == "native" and metadata.get("rid") == rid)
            application_files.intersection_update({native / "libHarfBuzzSharp.dll", native / "libHarfBuzzSharp.pdb"})
        assert not any(path.is_file() and path not in application_files for path in retired_files), "Retired Windows payload leaked into publish"
        assert not any((publish / name).exists() and publish / name not in application_files for name in names), "Native render/text files must retain their RID directory"
        assert not any(path.is_file() and path not in application_files
                       for directory in (publish / "runtimes").iterdir() if directory.name != rid
                       for path in directory.rglob("*")), "Foreign RID assets leaked into the game"
        for name, checksum in receipt["files"].items():
            library = native / name
            assert hashlib.sha256(library.read_bytes()).hexdigest() == checksum, f"Native manifest checksum mismatch: {name}"
            exports = native_package.windows_exports(library, rid, name)
            required = native_package.required_exports(name)
            assert required <= exports, f"Missing native exports: {name}"
            if name == "libElectron2D.dll":
                assert exports == required, "Unified Windows native ABI mismatch"
    engine = native / ("libElectron2D.dll" if platform == "Windows" else "libElectron2D.dylib" if platform == "MacOS" else "libElectron2D.so")
    assert engine.is_file(), "Unified native engine must retain its RID directory"
    required = native_package.required_exports(engine.name)
    if platform == "Linux":
        actual = native_package.elf_exports(engine)
        assert actual == required, f"Unified native ABI mismatch: {actual ^ required}"
        audio = native / "libFAudio.so.0"
        assert audio.is_file(), "Missing pinned native audio backend"
        header = audio.read_bytes()[:20]
        assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "Audio ELF architecture mismatch"
        dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(audio)], text=True)
        needed = re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic)
        assert "libSDL3.so.0" in needed and not any("SDL2" in name for name in needed), f"Audio must share SDL3: {needed}"
        audio_symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(audio)], text=True)
        audio_exports = {line.split()[-1] for line in audio_symbols.splitlines()}
        assert {"e2d_audio_select_output", "e2d_audio_output_latency", "e2d_audio_create_offline_master", "e2d_audio_mix_offline"} <= audio_exports, "Audio output bridge exports are missing"
        assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libFAudio.so.0"], "Audio SONAME mismatch"
        assert not (publish / "FAudio.dll").exists(), "Managed backend leaked outside the engine assembly"
        assert not list(publish.rglob("libicu*")), "A private text publish must not deliver global ICU libraries"
        print(f"{rid}: self-contained host, SDL/FAudio/FreeType/HarfBuzz and private text/ENet/WebRTC/WebP backends verified (no global ICU dependency)")
    else:
        assert not list(publish.rglob("libElectron2DENet.so")), "Linux ENet backend leaked into a foreign publish"
        assert not list(publish.rglob("libFAudio*")), "Linux audio library leaked into a foreign publish"
        assert not list(publish.rglob("libElectron2DTextBreak.so")), "Linux private text backend leaked into a foreign publish"
        print(f"{rid}: self-contained host and {platform} SDL/FreeType/HarfBuzz payload verified; common bridge ABI requires target execution")


if __name__ == "__main__":
    if len(sys.argv) not in (3, 4):
        raise SystemExit("Usage: python3 tools/check_native_publish.py RID PUBLISH_DIRECTORY [APPLICATION_NAME]")
    check(sys.argv[1], Path(sys.argv[2]), sys.argv[3] if len(sys.argv) > 3 else "CharacterMovement")
