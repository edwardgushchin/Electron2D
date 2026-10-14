#!/usr/bin/env python3
"""Verify the immutable MSDF generator closure and its delivered license."""
import hashlib
import json
from pathlib import Path
root = Path(__file__).resolve().parents[2]
vendor = root / "src/Vendor/MSDFGen"
manifest = json.loads((vendor / "manifest.json").read_text())
assert manifest["commit"] == "1874bcf7d9624ccc85b4bc9a85d78116f690f35b"
assert {p.relative_to(vendor).as_posix() for p in vendor.rglob("*") if p.is_file()} == set(manifest["files"]) | {"manifest.json"}
for name, expected in manifest["files"].items():
    assert hashlib.sha256((vendor / name).read_bytes()).hexdigest() == expected, name
assert (root / "licence/MSDFGen-LICENSE.txt").read_bytes() == (vendor / "LICENSE.txt").read_bytes()
print("Immutable MSDF generator and license hashes passed.")
