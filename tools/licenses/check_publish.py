#!/usr/bin/env python3
"""Check the audited Linux native inventory and bundled notices in a publish."""

import argparse
import fnmatch
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGES = {
    "SDL3-CS.Linux/3.4.18",
    "SDL3-CS.Linux.Image/3.4.6.12",
    "SDL3-CS.Linux.Shadercross/3.0.0.13",
    "MonoGame.Library.FreeType/2.13.2.5",
    "HarfBuzzSharp.NativeAssets.Linux/14.2.1.301",
}
NATIVE_GROUPS = {
    "dotnet": ("createdump", "libSystem.*.so", "libclrgc.so", "libclrjit.so",
               "libclrgcexp.so", "libcoreclr.so", "libcoreclrtraceptprovider.so", "libhostfxr.so",
               "libhostpolicy.so", "libmscordaccore.so", "libmscordbi.so"),
    "SDL": ("libSDL3.so*",),
    "SDL_image": ("libSDL3_image.so*",),
    "FreeType": ("libfreetype.so",),
    "HarfBuzz": ("libHarfBuzzSharp.so",),
    "FAudio": ("libFAudio.so.0",),
    "ENet": ("libElectron2DENet.so",),
    "ICUText": ("libElectron2DTextBreak.so",),
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


def check(publish: Path, application_licenses=()) -> None:
    deps_files = list(publish.glob("*.deps.json"))
    assert len(deps_files) == 1, "Expected one application deps.json"
    app = deps_files[0].name.removesuffix(".deps.json")
    packages = set(json.loads(deps_files[0].read_text())["libraries"])
    runtime = [p for p in packages if p.startswith("runtimepack.Microsoft.NETCore.App.Runtime.")]
    assert runtime == ["runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/10.0.1"], f"Unreviewed runtime pack: {runtime}"
    private_packages = {p for p in packages if p.startswith("Electron2D.") and p.split("/", 1)[0] in {"Electron2D." + name for name in ("Linux", "MacOS", "Windows", "Android", "iOS", "tvOS", "Web")}}
    version = ET.parse(ROOT / "tools/native-package.props").findtext(".//Electron2DVersion")
    assert private_packages == {"Electron2D.Linux/" + version}, f"Wrong selected native package: {private_packages}"
    assert PACKAGES <= packages, f"Unreviewed native package versions: {PACKAGES - packages}"

    elf = [p.name for p in publish.rglob("*") if p.is_file() and p.open("rb").read(4) == b"\x7fELF"]
    assert len(elf) == 68, f"Expected 68 audited ELF files, found {len(elf)}"
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
    assert len(expected) == 71, f"Expected 71 license and notice files, found {len(expected)}"
    extra = {p.name: p for p in application_licenses}
    assert len(extra) == len(application_licenses) and not expected.intersection(extra), "Duplicate application notice names"
    assert {p.name for p in delivered.iterdir() if p.is_file()} == expected | extra.keys(), "Unexpected published license files"
    for name in expected:
        assert (delivered / name).read_bytes() == (source / name).read_bytes(), name
    for name, path in extra.items():
        assert (delivered / name).read_bytes() == path.read_bytes(), name
    for stale in ("LICENSE", "THIRD_PARTY_NOTICES.md", "docs/licenses", "docs/coverage/ReferenceData-LICENSE.txt", "src/Vendor"):
        assert not (publish / stale).exists(), f"License outside licence/: {stale}"
    for link in re.findall(r"\]\(([^)]+)\)", (delivered / "THIRD_PARTY_NOTICES.md").read_text()):
        if not link.startswith(("http:", "https:")):
            assert (delivered / link.split("#", 1)[0]).exists(), f"Broken published notice link: {link}"
    print(f"{runtime[0]}: {len(elf)} audited ELF files, {len(expected)} matching engine notices, {len(extra)} matching application notices")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("publish", type=Path)
    parser.add_argument("--application-license", type=Path, action="append", default=[])
    args = parser.parse_args()
    check(args.publish, args.application_license)
