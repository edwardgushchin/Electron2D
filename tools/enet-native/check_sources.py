#!/usr/bin/env python3
"""Verify unchanged ENet/FastLZ source pins and published license copies."""
import hashlib
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
count = 0
for name in ("ENet", "FastLZ"):
    folder = ROOT / "src" / "Vendor" / name
    manifest = json.loads((folder / "manifest.json").read_text())
    assert len(manifest["commit"]) == 40
    for file, digest in manifest["files"].items():
        assert hashlib.sha256((folder / file).read_bytes()).hexdigest() == digest, file
        count += 1
    assert (ROOT / "licence" / (name + "-LICENSE.txt")).read_bytes() == (folder / "LICENSE").read_bytes()
print(f"{count} immutable ENet/FastLZ files and two license copies verified")
