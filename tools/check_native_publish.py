#!/usr/bin/env python3
"""Check a self-contained HostExample publish for its RID-specific SDL payload."""

import json
import sys
from pathlib import Path


def check(rid: str, publish: Path) -> None:
    if rid.startswith("win-"):
        platform, host, names = "Windows", "HostExample.exe", ("SDL3.dll", "SDL3_image.dll", "SDL3_shadercross.dll")
    elif rid.startswith("osx-"):
        platform, host, names = "MacOS", "HostExample", ("libSDL3.dylib", "libSDL3_image.dylib", "libSDL3_shadercross.dylib")
    elif rid.startswith("linux-"):
        platform, host, names = "Linux", "HostExample", ("libSDL3.so", "libSDL3_image.so", "libSDL3_shadercross.so")
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
    assert actual == expected, f"Wrong SDL packages: {actual}"
    assert f"runtimepack.Microsoft.NETCore.App.Runtime.{rid}/10.0.1" in packages, "Missing self-contained runtime pack"
    assert all((publish / name).is_file() for name in names), f"Missing SDL native files: {names}"
    print(f"{rid}: self-contained host and {platform} SDL payload verified")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: python3 tools/check_native_publish.py RID PUBLISH_DIRECTORY")
    check(sys.argv[1], Path(sys.argv[2]))
