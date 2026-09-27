#!/usr/bin/env python3
"""Restore pinned ICU sources and reproduce private text data; --check is offline."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
VENDOR = ROOT / "src/Vendor/ICU"
MANIFEST = VENDOR / "manifest.json"


def verify_bytes(data, record, label):
    actual = hashlib.sha256(data).hexdigest()
    if actual != record["sha256"] or ("bytes" in record and len(data) != record["bytes"]):
        raise ValueError(f"Pinned content mismatch: {label} ({len(data)} bytes, SHA256 {actual})")


def check(manifest):
    expected = {entry["path"] for entry in manifest["files"]}
    expected.update(("manifest.json", "UPSTREAM.md", manifest["data"]["path"]))
    actual = {str(path.relative_to(VENDOR)) for path in VENDOR.rglob("*") if path.is_file()}
    if expected != actual:
        raise ValueError(f"Vendor inventory mismatch: missing={sorted(expected-actual)}, extra={sorted(actual-expected)}")
    for entry in manifest["files"] + [manifest["data"]]:
        path = VENDOR / entry["path"]
        if path.is_symlink():
            raise ValueError(f"Symlink is not a pinned source file: {path}")
        verify_bytes(path.read_bytes(), entry, entry["path"])
    license_info = manifest["license"]
    verify_bytes((ROOT / license_info["path"]).read_bytes(), license_info, license_info["path"])
    print(f"ICU {manifest['version']}: {len(manifest['files'])} unchanged source/header files, "
          f"{len(manifest['data']['items'])} data items, {manifest['data']['bytes']} data bytes; offline hashes PASS")


def archive_read(archive, member):
    entry = archive.getmember(member)
    if not entry.isfile():
        raise ValueError(f"Expected regular archive member: {member}")
    return archive.extractfile(entry).read()


def run(command, **kwargs):
    subprocess.run([str(value) for value in command], check=True, **kwargs)


def restore(manifest, archive_path, jobs):
    with tempfile.TemporaryDirectory(prefix="electron2d-icu-text-") as temporary:
        work = Path(temporary)
        if archive_path is None:
            archive_path = work / "icu-sources.tgz"
            request = urllib.request.Request(manifest["archive"]["url"], headers={"User-Agent": "Electron2D source updater"})
            with urllib.request.urlopen(request, timeout=120) as response, archive_path.open("wb") as output:
                shutil.copyfileobj(response, output)
        verify_bytes(archive_path.read_bytes(), manifest["archive"], "source archive")
        with tarfile.open(archive_path, "r:gz") as archive:
            # Validate all inputs before replacing any tracked file. No archive paths are extracted.
            source_bytes = {}
            for entry in manifest["files"]:
                data = archive_read(archive, "icu/source/" + entry["path"])
                verify_bytes(data, entry, entry["path"])
                source_bytes[entry["path"]] = data
            license_bytes = archive_read(archive, "icu/LICENSE")
            verify_bytes(license_bytes, manifest["license"], "ICU license")
            original = work / "icudt78l.dat"
            original.write_bytes(archive_read(archive, "icu/source/data/in/icudt78l.dat"))
        for name, data in source_bytes.items():
            target = VENDOR / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        (ROOT / manifest["license"]["path"]).write_bytes(license_bytes)

        # The host utility comes from this exact source closure, never from a system ICU.
        build = work / "host-build"
        run(["cmake", "-S", ROOT / "src/Servers/Text/Native", "-B", build,
             "-G", "Ninja", "-DCMAKE_BUILD_TYPE=Release", "-DELECTRON2D_ICU_BUILD_TOOL=ON"])
        run(["cmake", "--build", build, "--target", "e2d_icupkg", "--parallel", jobs])
        tool = build / ("e2d_icupkg.exe" if (build / "e2d_icupkg.exe").exists() else "e2d_icupkg")
        listing = subprocess.check_output([str(tool), "-l", str(original)], text=True).splitlines()
        keep = set(manifest["data"]["items"])
        if not keep.issubset(listing):
            raise ValueError(f"Missing pinned data items: {sorted(keep-set(listing))}")
        removal = work / "remove.txt"
        removal.write_text("\n".join(sorted(set(listing)-keep)) + "\n", encoding="utf-8")
        filtered = work / "TextBreak78.dat"
        run([tool, "--toc_prefix", manifest["data"]["tocPrefix"], "-r", removal, original, filtered])
        result = filtered.read_bytes()
        verify_bytes(result, manifest["data"], "reproduced text data")
        (VENDOR / manifest["data"]["path"]).write_bytes(result)
    check(manifest)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify the complete recorded closure offline; no compiler/network needed")
    parser.add_argument("--archive", type=Path, help="Use an already downloaded pinned source tarball")
    parser.add_argument("--jobs", type=int, default=8, help="Host C++ build parallelism (default: 8)")
    args = parser.parse_args()
    if args.jobs < 1:
        parser.error("--jobs must be positive")
    if args.check and args.archive:
        parser.error("--check does not read an archive")
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if args.check:
        check(manifest)
    else:
        restore(manifest, args.archive, args.jobs)


if __name__ == "__main__":
    main()
