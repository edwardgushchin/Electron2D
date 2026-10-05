#!/usr/bin/env python3
"""Check a self-contained CharacterMovement publish and its private Linux text/ENet ABIs."""

import json
import re
import subprocess
import sys
from pathlib import Path


TEXT_EXPORTS = {
    "e2d_text_init", "e2d_text_versions", "e2d_text_is_nonprinting",
    "e2d_break_open", "e2d_break_set", "e2d_break_first", "e2d_break_next",
    "e2d_break_status", "e2d_break_close",
}


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
    assert not any(name.lower().startswith("libicu") for name in needed), f"Private text library depends on global ICU: {needed}"
    sonames = re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic)
    assert sonames == ["libElectron2DTextBreak.so"], f"Unexpected private text SONAME: {sonames}"


def check(rid: str, publish: Path) -> None:
    if rid.startswith("win-"):
        platform, host, names = "Windows", "CharacterMovement.exe", ("SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll", "Electron2DFreeType.dll", "libHarfBuzzSharp.dll")
    elif rid.startswith("osx-"):
        platform, host, names = "MacOS", "CharacterMovement", ("libSDL3.dylib", "libSDL3_image.dylib", "libSDL3_shadercross.dylib", "libElectron2DFreeType.dylib", "libHarfBuzzSharp.dylib")
    elif rid.startswith("linux-"):
        platform, host, names = "Linux", "CharacterMovement", ("libSDL3.so", "libSDL3_image.so", "libSDL3_shadercross.so", "libfreetype.so", "libHarfBuzzSharp.so")
    else:
        raise ValueError(f"No self-contained desktop host for {rid}")

    assert (publish / host).is_file() and (publish / "Electron2D.dll").is_file(), "Missing application host or engine"
    packages = json.loads((publish / "CharacterMovement.deps.json").read_text())["libraries"]
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
    platform_packages = {name.split("/")[0] for name in packages if name.startswith("Electron2D.")}
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
        assert exports == {"e2d_enet_" + name for name in ("callbacks", "create", "destroy", "connect", "service", "flush", "send", "packet", "release", "peer", "stat", "host", "compress")}, "ENet private ABI export mismatch"
        audio = native / "libFAudio.so.0"
        assert audio.is_file(), "Missing pinned native audio backend"
        header = audio.read_bytes()[:20]
        assert header[:6] == b"\x7fELF\x02\x01" and int.from_bytes(header[18:20], "little") == {"linux-x64": 62, "linux-arm64": 183}[rid], "Audio ELF architecture mismatch"
        dynamic = subprocess.check_output(["readelf", "--wide", "--dynamic", str(audio)], text=True)
        needed = re.findall(r"\(NEEDED\).*\[([^]]+)\]", dynamic)
        assert "libSDL3.so.0" in needed and not any("SDL2" in name for name in needed), f"Audio must share SDL3: {needed}"
        audio_symbols = subprocess.check_output(["nm", "-D", "--defined-only", str(audio)], text=True)
        audio_exports = {line.split()[-1] for line in audio_symbols.splitlines()}
        assert {"e2d_audio_select_output", "e2d_audio_output_latency"} <= audio_exports, "Audio output bridge exports are missing"
        assert re.findall(r"\(SONAME\).*\[([^]]+)\]", dynamic) == ["libFAudio.so.0"], "Audio SONAME mismatch"
        assert not (publish / "FAudio.dll").exists(), "Managed backend leaked outside the engine assembly"
        assert not list(publish.rglob("libicu*")), "A private text publish must not deliver global ICU libraries"
        print(f"{rid}: self-contained host, SDL/FAudio/FreeType/HarfBuzz and private text/ENet backends verified (9/13 exports, no global ICU dependency)")
    else:
        assert not list(publish.rglob("libElectron2DENet.so")), "Linux ENet backend leaked into a foreign publish"
        assert not list(publish.rglob("libFAudio*")), "Linux audio library leaked into a foreign publish"
        assert not list(publish.rglob("libElectron2DTextBreak.so")), "Linux private text backend leaked into a foreign publish"
        print(f"{rid}: self-contained host and {platform} SDL/FreeType/HarfBuzz payload verified; private text backend is not supplied")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: python3 tools/check_native_publish.py RID PUBLISH_DIRECTORY")
    check(sys.argv[1], Path(sys.argv[2]))
