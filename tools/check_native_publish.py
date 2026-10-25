#!/usr/bin/env python3
"""Check a self-contained desktop application and its private native ABIs."""

import json
import re
import subprocess
import sys
from pathlib import Path


TEXT_EXPORTS = {
    "e2d_regex_create", "e2d_regex_destroy", "e2d_regex_info", "e2d_regex_match", "e2d_regex_substitute", "e2d_regex_error",
    "e2d_text_init", "e2d_text_versions", "e2d_text_is_nonprinting",
    "e2d_text_case", "e2d_text_strip_diacritics", "e2d_text_character_properties", "e2d_text_is_identifier",
    "e2d_spoof_open", "e2d_spoof_close", "e2d_spoof_skeleton", "e2d_spoof_check",
    "e2d_break_open", "e2d_break_set", "e2d_break_first", "e2d_break_next",
    "e2d_break_status", "e2d_break_close", "e2d_font_msdf_generate", "e2d_font_msdf_free",
}


WEBRTC_EXPORTS = {"e2d_rtc_" + name for name in (
    "error", "create", "destroy", "close", "state", "offer", "local", "remote", "candidate", "event",
    "event_kind", "event_string", "event_channel", "event_release", "channel_create", "channel_destroy",
    "channel_close", "channel_poll", "channel_info", "channel_string", "channel_size", "channel_read", "channel_send")}

WEBP_EXPORTS = {"e2d_webp_encode", "e2d_webp_decode"}


def check_private_webp(rid: str, library: Path) -> None:
    header = library.read_bytes()[:20]
    assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "WebP ELF architecture mismatch"
    dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(library)], text=True)
    assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libElectron2DWebP.so"], "WebP SONAME mismatch"
    assert not re.search(r"\((RPATH|RUNPATH)\)", dynamic), "WebP retains a build-machine search path"
    dependencies = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
    assert dependencies <= {"libm.so.6", "libc.so.6"}, f"Unreviewed WebP dependencies: {dependencies}"
    symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(library)], text=True)
    assert WEBP_EXPORTS <= {line.split()[-1] for line in symbols.splitlines()}, "WebP encoder/decoder bridge exports are missing"


def check_private_webrtc(rid: str, library: Path) -> None:
    header = library.read_bytes()[:20]
    assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "WebRTC ELF architecture mismatch"
    dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(library)], text=True)
    assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libElectron2DWebRTC.so"], "WebRTC SONAME mismatch"
    assert not re.search(r"\((RPATH|RUNPATH)\)", dynamic), "WebRTC retains a build-machine search path"
    dependencies = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
    allowed = {"libssl.so.3", "libcrypto.so.3", "libstdc++.so.6", "libm.so.6", "libgcc_s.so.1", "libc.so.6", "libpthread.so.0", "libdl.so.2", {"linux-x64": "ld-linux-x86-64.so.2", "linux-arm64": "ld-linux-aarch64.so.1"}[rid]}
    assert {"libssl.so.3", "libcrypto.so.3"} <= dependencies <= allowed, f"Unreviewed WebRTC dependencies: {dependencies}"
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
        platform, host, names = "Windows", app_name + ".exe", ("SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll", "Electron2DFreeType.dll", "libHarfBuzzSharp.dll")
    elif rid.startswith("osx-"):
        platform, host, names = "MacOS", app_name, ("libSDL3.dylib", "libSDL3_image.dylib", "libSDL3_shadercross.dylib", "libElectron2DFreeType.dylib", "libHarfBuzzSharp.dylib")
    elif rid.startswith("linux-"):
        platform, host, names = "Linux", app_name, ("libSDL3.so", "libSDL3_image.so", "libSDL3_shadercross.so", "libfreetype.so", "libHarfBuzzSharp.so")
    else:
        raise ValueError(f"No self-contained desktop host for {rid}")

    assert (publish / host).is_file() and (publish / "Electron2D.dll").is_file(), "Missing application host or engine"
    packages = json.loads((publish / (app_name + ".deps.json")).read_text())["libraries"]
    expected = {
        f"SDL3-CS.{platform}/3.4.18",
        f"SDL3-CS.{platform}.Image/3.4.6.12",
        f"SDL3-CS.{platform}.Shadercross/3.0.0.13",
    }
    actual = {name for name in packages if name.startswith("SDL3-CS.")}
    text_platform = {"Windows": "Win32", "MacOS": "macOS", "Linux": "Linux"}[platform]
    if platform == "Linux":
        assert "MonoGame.Library.FreeType/2.13.2.5" in packages, "Missing pinned native FreeType package"
    assert f"HarfBuzzSharp.NativeAssets.{text_platform}/14.2.1.301" in packages, "Missing pinned native HarfBuzz package"
    assert actual == expected, f"Wrong SDL packages: {actual}"
    assert f"runtimepack.Microsoft.NETCore.App.Runtime.{rid}/10.0.1" in packages, "Missing self-contained runtime pack"
    native = publish / "runtimes" / rid / "native"
    assert all((native / name).is_file() for name in names), f"Missing native render/text files: {names}"
    assert not any((publish / name).exists() for name in names), "Native render/text files must retain their RID directory"
    assert {path.name for path in (publish / "runtimes").iterdir()} == {rid}, "Foreign RID assets leaked into the game"
    platform_packages = {name.split("/")[0] for name, library in packages.items() if name.startswith("Electron2D.") and library["type"] == "package"}
    assert platform_packages == {f"Electron2D.{platform}"}, f"Wrong engine platform packages: {platform_packages}"
    private_text = native / "libElectron2DTextBreak.so"
    assert not (publish / "libElectron2DTextBreak.so").exists(), "Private text library must be under runtimes/RID/native"
    assert not (publish / "libFAudio.so.0").exists(), "Private audio library must be under runtimes/RID/native"
    if platform == "Linux":
        assert private_text.is_file(), "Missing private ICU text backend"
        check_private_text(rid, private_text)
        enet = native / "libElectron2DENet.so"
        assert enet.is_file() and not (publish / enet.name).exists(), "ENet must exist only in its RID directory"
        header = enet.read_bytes()[:20]
        assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "ENet ELF architecture mismatch"
        dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(enet)], text=True)
        assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libElectron2DENet.so"], "ENet SONAME mismatch"
        needed = set(re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic))
        assert needed == {"libz.so.1", "libzstd.so.1", "libc.so.6", {"linux-x64": "ld-linux-x86-64.so.2", "linux-arm64": "ld-linux-aarch64.so.1"}[rid]}, f"Unreviewed ENet dependencies: {needed}"
        symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(enet)], text=True)
        exports = {line.split()[-1] for line in symbols.splitlines()}
        assert exports == {"e2d_enet_" + name for name in ("callbacks", "create", "destroy", "connect", "service", "flush", "send", "packet", "release", "peer", "stat", "host", "compress")} | {"e2d_file_encode", "e2d_file_decode"}, "ENet private ABI export mismatch"
        webrtc = native / "libElectron2DWebRTC.so"
        assert webrtc.is_file() and not (publish / webrtc.name).exists(), "WebRTC must retain its RID directory"
        check_private_webrtc(rid, webrtc)
        webp = native / "libElectron2DWebP.so"
        assert webp.is_file() and not (publish / webp.name).exists(), "WebP must retain its RID directory"
        check_private_webp(rid, webp)
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
        ogv = native / "libElectron2DOGV.so"
        assert ogv.is_file(), "Missing private OGV encoder"
        ogv_symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(ogv)], text=True)
        assert {line.split()[-1] for line in ogv_symbols.splitlines()} == {"e2d_ogv_create", "e2d_ogv_frame", "e2d_ogv_finish", "e2d_ogv_destroy",
                                                                      "e2d_video_create", "e2d_video_frame", "e2d_video_audio", "e2d_video_seek_audio", "e2d_video_destroy"}, "OGV private ABI mismatch"
        ogv_dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(ogv)], text=True)
        assert not any(name in ogv_dynamic for name in ("libogg", "libvorbis", "libtheora", "RPATH", "RUNPATH")), "OGV codecs must remain private and relocatable"
        ktx = native / "libElectron2DKTX.so"
        assert ktx.is_file(), "Missing private KTX/Basis decoder"
        ktx_symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(ktx)], text=True)
        assert {line.split()[-1] for line in ktx_symbols.splitlines()} == {"e2d_ktx_create", "e2d_ktx_level", "e2d_ktx_destroy", "e2d_basis_create", "e2d_block_decode", "e2d_image_encode", "e2d_image_encoded_free"}, "KTX private ABI mismatch"
        ktx_dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(ktx)], text=True)
        assert not any(name in ktx_dynamic for name in ("libktx", "libbasis", "libzstd", "RPATH", "RUNPATH")), "KTX codec closure must remain private and relocatable"
        assert not (publish / "FAudio.dll").exists(), "Managed backend leaked outside the engine assembly"
        assert not list(publish.rglob("libicu*")), "A private text publish must not deliver global ICU libraries"
        print(f"{rid}: self-contained host, SDL/FAudio/FreeType/HarfBuzz and private text/ENet/WebRTC/WebP backends verified (no global ICU dependency)")
    else:
        assert not list(publish.rglob("libElectron2DENet.so")), "Linux ENet backend leaked into a foreign publish"
        assert not list(publish.rglob("libFAudio*")), "Linux audio library leaked into a foreign publish"
        assert not list(publish.rglob("libElectron2DTextBreak.so")), "Linux private text backend leaked into a foreign publish"
        print(f"{rid}: self-contained host and {platform} SDL/FreeType/HarfBuzz payload verified; private text backend is not supplied")


if __name__ == "__main__":
    if len(sys.argv) not in (3, 4):
        raise SystemExit("Usage: python3 tools/check_native_publish.py RID PUBLISH_DIRECTORY [APPLICATION_NAME]")
    check(sys.argv[1], Path(sys.argv[2]), sys.argv[3] if len(sys.argv) > 3 else "CharacterMovement")
