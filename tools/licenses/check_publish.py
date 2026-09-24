#!/usr/bin/env python3
"""Check the audited Linux native inventory and bundled notices in a publish."""

import fnmatch
import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGES = {
    "SDL3-CS.Linux/3.4.16",
    "SDL3-CS.Linux.Image/3.4.6.9",
    "SDL3-CS.Linux.Shadercross/3.0.0.11",
}
SOURCE_NOTICES = (
    "src/Vendor/SDL3-CS/UPSTREAM-LICENSE.txt",
    "src/Vendor/Box2D.NET/LICENSE",
    "src/Vendor/Clipper2/LICENSE",
    "src/Vendor/FastNoiseLite/LICENSE",
    "docs/licenses/PCG32-LICENSE.txt",
    "docs/licenses/PolyPartition-LICENSE.txt",
    "docs/coverage/GODOT-LICENSE.txt",
)
NATIVE_GROUPS = {
    "dotnet": ("createdump", "libSystem.*.so", "libclrgc.so", "libclrjit.so",
               "libcoreclr.so", "libcoreclrtraceptprovider.so", "libhostfxr.so",
               "libhostpolicy.so", "libmscordaccore.so", "libmscordbi.so"),
    "SDL": ("libSDL3.so*",),
    "SDL_image": ("libSDL3_image.so*",),
    "libaom": ("libaom.so*",),
    "libavif": ("libavif.so*",),
    "dav1d": ("libdav1d.so*",),
    "libpng": ("libpng16.so*",),
    "libtiff": ("libtiff.so*",),
    "libwebp": ("libwebp.so*", "libwebpdemux.so*", "libwebpmux.so*"),
    "SDL_shadercross": ("libSDL3_shadercross.so*",),
    "SPIRV-Cross": ("libspirv-cross-c-shared.so*",),
    "DirectXShaderCompiler": ("libdxcompiler.so", "libdxil.so"),
    "vkd3d": ("libvkd3d.so*", "libvkd3d-shader.so*", "libvkd3d-utils.so*"),
}


def check(publish: Path) -> None:
    deps_files = list(publish.glob("*.deps.json"))
    assert len(deps_files) == 1, "Expected one application deps.json"
    app = deps_files[0].name.removesuffix(".deps.json")
    packages = set(json.loads(deps_files[0].read_text())["libraries"])
    runtime = [p for p in packages if p.startswith("runtimepack.Microsoft.NETCore.App.Runtime.")]
    assert len(runtime) == 1 and runtime[0] in {
        "runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/8.0.22",
        "runtimepack.Microsoft.NETCore.App.Runtime.linux-arm64/8.0.22",
    }, f"Unreviewed runtime pack: {runtime}"
    assert PACKAGES <= packages, f"Unreviewed native package versions: {PACKAGES - packages}"

    elf = [p.name for p in publish.iterdir() if p.is_file() and p.open("rb").read(4) == b"\x7fELF"]
    assert len(elf) == 62, f"Expected 62 audited ELF files, found {len(elf)}"
    assert app in elf, "Expected the native application host"
    for name in elf:
        groups = [group for group, patterns in NATIVE_GROUPS.items()
                  if any(fnmatch.fnmatchcase(name, pattern) for pattern in patterns)]
        if name == app:
            groups.append("dotnet")
        assert len(groups) == 1, f"Unclassified or ambiguous native file: {name}: {groups}"
    for group, patterns in NATIVE_GROUPS.items():
        assert any(any(fnmatch.fnmatchcase(name, pattern) for pattern in patterns) for name in elf), group

    notices = {"LICENSE": "LICENSE", "THIRD_PARTY_NOTICES.md": "THIRD_PARTY_NOTICES.md"}
    notices.update({p: p for p in SOURCE_NOTICES})
    notices.update({f"docs/licenses/native/{p.name}": f"docs/licenses/native/{p.name}"
                    for p in (ROOT / "docs/licenses/native").iterdir() if p.is_file()})
    assert len(notices) == 29, f"Expected 29 license and notice files, found {len(notices)}"
    for delivered, source in notices.items():
        target = publish / delivered
        assert target.is_file() and target.read_bytes() == (ROOT / source).read_bytes(), delivered
    for link in re.findall(r"\]\(([^)]+)\)", (publish / "THIRD_PARTY_NOTICES.md").read_text()):
        if not link.startswith(("http:", "https:")):
            assert (publish / link.split("#", 1)[0]).exists(), f"Broken published notice link: {link}"
    print(f"{runtime[0]}: {len(elf)} audited ELF files, {len(notices)} matching notices")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python3 tools/licenses/check_publish.py PUBLISH_DIRECTORY")
    check(Path(sys.argv[1]))
