#!/usr/bin/env python3
"""Check a self-contained HostExample publish and its private Linux text ABI."""

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
        platform, host, names = "Windows", "HostExample.exe", ("SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll", "freetype.dll", "libHarfBuzzSharp.dll")
    elif rid.startswith("osx-"):
        platform, host, names = "MacOS", "HostExample", ("libSDL3.dylib", "libSDL3_image.dylib", "libSDL3_shadercross.dylib", "libfreetype.dylib", "libHarfBuzzSharp.dylib")
    elif rid.startswith("linux-"):
        platform, host, names = "Linux", "HostExample", ("libSDL3.so", "libSDL3_image.so", "libSDL3_shadercross.so", "libfreetype.so", "libHarfBuzzSharp.so")
    else:
        raise ValueError(f"No self-contained desktop host for {rid}")

    assert (publish / host).is_file() and (publish / "Electron2D.dll").is_file(), "Missing application host or engine"
    packages = json.loads((publish / "HostExample.deps.json").read_text())["libraries"]
    expected = {
        f"SDL3-CS.{platform}/3.4.16",
        f"SDL3-CS.{platform}.Image/3.4.6.9",
        f"SDL3-CS.{platform}.Shadercross/3.0.0.11",
    }
    actual = {name for name in packages if name.startswith("SDL3-CS.")}
    text_platform = {"Windows": "Win32", "MacOS": "macOS", "Linux": "Linux"}[platform]
    assert "MonoGame.Library.FreeType/2.13.2.5" in packages, "Missing pinned native FreeType package"
    assert f"HarfBuzzSharp.NativeAssets.{text_platform}/14.2.1.201" in packages, "Missing pinned native HarfBuzz package"
    assert actual == expected, f"Wrong SDL packages: {actual}"
    assert f"runtimepack.Microsoft.NETCore.App.Runtime.{rid}/10.0.1" in packages, "Missing self-contained runtime pack"
    assert all((publish / name).is_file() for name in names), f"Missing native render/text files: {names}"
    private_text = publish / "libElectron2DTextBreak.so"
    if platform == "Linux":
        assert private_text.is_file(), "Missing private ICU text backend"
        check_private_text(rid, private_text)
        assert not list(publish.glob("libicu*")), "A private text publish must not deliver global ICU libraries"
        print(f"{rid}: self-contained host, SDL/FreeType/HarfBuzz and private text backend verified (9 exports, no global ICU dependency)")
    else:
        assert not private_text.exists(), "Linux private text backend leaked into a foreign publish"
        print(f"{rid}: self-contained host and {platform} SDL/FreeType/HarfBuzz payload verified; private text backend is not supplied")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: python3 tools/check_native_publish.py RID PUBLISH_DIRECTORY")
    check(sys.argv[1], Path(sys.argv[2]))
