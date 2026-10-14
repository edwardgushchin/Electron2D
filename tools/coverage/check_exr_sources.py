#!/usr/bin/env python3
"""Verify the immutable private EXR source closure and packaging adaptations."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
vendor = root / "src/Vendor/EXR"
manifest = json.loads((vendor / "manifest.json").read_text())
assert manifest["commit"] == "b57d68083c443da538a71410ac181cd232127f0a"
assert manifest["archive_sha256"] == "4071d5dc05448e0c613e6a13a4fe88564fcccc017264716181da324a7359a5a8"
actual = {p.relative_to(vendor).as_posix() for p in vendor.rglob("*.cs")}
assert actual == set(manifest["files"]) == set(manifest["original"])
for name, digest in manifest["files"].items():
    assert hashlib.sha256((vendor / name).read_bytes()).hexdigest() == digest, name
assert (vendor / "LICENSE").read_bytes() == (root / "licence/TinyEXR.NET-LICENSE.txt").read_bytes()
print(f"EXR private source: {len(actual)} files, original and adapted fingerprints retained")
