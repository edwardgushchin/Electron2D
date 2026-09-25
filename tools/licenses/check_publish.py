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
NATIVE_GROUPS = {
    "dotnet": ("createdump", "libSystem.*.so", "libclrgc.so", "libclrjit.so",
               "libclrgcexp.so", "libcoreclr.so", "libcoreclrtraceptprovider.so", "libhostfxr.so",
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
    assert runtime == ["runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/10.0.1"], f"Unreviewed runtime pack: {runtime}"
    assert PACKAGES <= packages, f"Unreviewed native package versions: {PACKAGES - packages}"

    elf = [p.name for p in publish.iterdir() if p.is_file() and p.open("rb").read(4) == b"\x7fELF"]
    assert len(elf) == 63, f"Expected 63 audited ELF files, found {len(elf)}"
    assert app in elf, "Expected the native application host"
    for name in elf:
        groups = [group for group, patterns in NATIVE_GROUPS.items()
                  if any(fnmatch.fnmatchcase(name, pattern) for pattern in patterns)]
        if name == app:
            groups.append("dotnet")
        assert len(groups) == 1, f"Unclassified or ambiguous native file: {name}: {groups}"
    for group, patterns in NATIVE_GROUPS.items():
        assert any(any(fnmatch.fnmatchcase(name, pattern) for pattern in patterns) for name in elf), group

    source = ROOT / "licence"
    expected = {p.name for p in source.iterdir() if p.is_file()} - {"ReferenceData-LICENSE.txt"}
    delivered = publish / "licence"
    assert len(expected) == 28, f"Expected 28 license and notice files, found {len(expected)}"
    assert {p.name for p in delivered.iterdir() if p.is_file()} == expected, "Unexpected published license files"
    for name in expected:
        assert (delivered / name).read_bytes() == (source / name).read_bytes(), name
    for stale in ("LICENSE", "THIRD_PARTY_NOTICES.md", "docs/licenses", "docs/coverage/ReferenceData-LICENSE.txt", "src/Vendor"):
        assert not (publish / stale).exists(), f"License outside licence/: {stale}"
    for link in re.findall(r"\]\(([^)]+)\)", (delivered / "THIRD_PARTY_NOTICES.md").read_text()):
        if not link.startswith(("http:", "https:")):
            assert (delivered / link.split("#", 1)[0]).exists(), f"Broken published notice link: {link}"
    print(f"{runtime[0]}: {len(elf)} audited ELF files, {len(expected)} matching notices")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python3 tools/licenses/check_publish.py PUBLISH_DIRECTORY")
    check(Path(sys.argv[1]))
