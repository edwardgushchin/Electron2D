#!/usr/bin/env python3
"""Check the audited Linux native inventory and bundled notices in a publish."""

import argparse
import fnmatch
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGES = {
    "SDL3-CS.Linux/3.4.18",
    "SDL3-CS.Linux.Image/3.4.6.12",
    "SDL3-CS.Linux.Shadercross/3.0.0.13",
}
NATIVE_GROUPS = {
    "dotnet": ("createdump", "libSystem.*.so", "libclrgc.so", "libclrjit.so",
               "libclrgcexp.so", "libcoreclr.so", "libcoreclrtraceptprovider.so", "libhostfxr.so",
               "libhostpolicy.so", "libmscordaccore.so", "libmscordbi.so"),
    "SDL": ("libSDL3.so*", "libElectron2DRender.so"),
    "SDL_image": ("libSDL3_image.so*",),
    "FreeType": ("libElectron2DFreeType.so",),
    "HarfBuzz": ("libHarfBuzzSharp.so",),
    "FAudio": ("libFAudio.so.0",),
    "ENet": ("libElectron2DENet.so",),
    "libdatachannel": ("libElectron2DWebRTC.so",),
    "OGV": ("libElectron2DOGV.so",),
    "KTX": ("libElectron2DKTX.so",),
    "meshoptimizer": ("libElectron2DMesh.so",),
    "ICUText": ("libElectron2DTextBreak.so",),
    "libaom": ("libaom.so*",),
    "libavif": ("libavif.so*",),
    "dav1d": ("libdav1d.so*",),
    "libpng": ("libpng16.so*",),
    "libtiff": ("libtiff.so*",),
    "libwebp": ("libwebp.so*", "libwebpdemux.so*", "libwebpmux.so*", "libElectron2DWebP.so"),
    "SDL_shadercross": ("libSDL3_shadercross.so*",),
    "SPIRV-Cross": ("libspirv-cross-c-shared.so*",),
    "DirectXShaderCompiler": ("libdxcompiler.so", "libdxil.so"),
    "vkd3d": ("libvkd3d.so*", "libvkd3d-shader.so*", "libvkd3d-utils.so*"),
}


def check(publish: Path, application_licenses=(), native_package_version: str | None = None) -> None:
    deps_files = list(publish.glob("*.deps.json"))
    assert len(deps_files) == 1, "Expected one application deps.json"
    app = deps_files[0].name.removesuffix(".deps.json")
    packages = set(json.loads(deps_files[0].read_text())["libraries"])
    runtime = [p for p in packages if p.startswith("runtimepack.Microsoft.NETCore.App.Runtime.")]
    assert runtime == ["runtimepack.Microsoft.NETCore.App.Runtime.linux-x64/10.0.1"], f"Unreviewed runtime pack: {runtime}"
    private_packages = {p for p in packages if p.startswith("Electron2D.") and p.split("/", 1)[0] in {"Electron2D." + name for name in ("Linux", "MacOS", "Windows", "Android", "iOS", "tvOS", "Web")}}
    version = native_package_version or ET.parse(ROOT / "tools/native-package.props").findtext(".//Electron2DVersion")
    assert private_packages == {"Electron2D.Linux/" + version}, f"Wrong selected native package: {private_packages}"
    assert PACKAGES <= packages, f"Unreviewed native package versions: {PACKAGES - packages}"
    assert not any(name.startswith("HarfBuzzSharp.NativeAssets.") for name in packages), "Unexpected older shaping package"

    elf = [p.name for p in publish.rglob("*") if p.is_file() and p.open("rb").read(4) == b"\x7fELF"]
    assert len(elf) == 74, f"Expected 74 audited ELF files, found {len(elf)}"
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
    assert len(expected) == 100, f"Expected 100 source, license and notice files, found {len(expected)}"
    receipt = json.loads((delivered / "vkd3d-SOURCE.json").read_text())
    assert receipt["source"]["componentSourceRef"] == "e55cf5e31ced6f3d1be5cc6d0c50e99384f9f4ba", "Unreviewed Vkd3d producer"
    assert hashlib.sha256((delivered / "vkd3d-2.0.tar.xz").read_bytes()).hexdigest() == receipt["source"]["sha256"] == "9ad29bb236808186a47ec66e853b21e6fca59b9dc62a9474b05d5e3eda2710ef", "Vkd3d corresponding source differs"
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
    parser.add_argument("--native-package-version", help="Exact expected native package version; defaults to the product version")
    args = parser.parse_args()
    check(args.publish, args.application_license, args.native_package_version)
