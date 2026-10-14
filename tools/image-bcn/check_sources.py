#!/usr/bin/env python3
"""Verify the delivered private block-codec closure, provenance and notices."""
import hashlib
import json
from pathlib import Path
from update_sources import PINS, ROOT
for name, commit in PINS.items():
    vendor = ROOT / "src/Vendor" / name
    manifest = json.loads((vendor / "manifest.json").read_text())
    assert manifest["commit"] == commit, name
    assert {p.relative_to(vendor).as_posix() for p in vendor.rglob("*") if p.is_file()} == set(manifest["files"]) | {"manifest.json"}, name
    for path, record in manifest["files"].items():
        assert hashlib.sha256((vendor / path).read_bytes()).hexdigest() == record["sha256"], path
        assert len(record["upstream_sha256"]) == 64, path
    notice = ROOT / "licence" / ("BCnEncoder-LICENSE.txt" if name == "BCn" else "BufferTools-LICENSE.txt")
    assert notice.read_bytes() == (vendor / ("LICENSE-MIT" if name == "BCn" else "LICENSE")).read_bytes()
print("Pinned BC/DDS codec source and license hashes passed.")
