#!/usr/bin/env python3
"""Check the immutable simplifier source closure."""
import hashlib
import json
from pathlib import Path
root = Path(__file__).resolve().parents[2] / "src/Vendor/meshoptimizer"
manifest = json.loads((root / "manifest.json").read_text())
assert manifest["commit"] == "b22872835dbabc56a6e4a366ea9917f62b7daf1a"
for name, expected in manifest["files"].items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == expected, name
print("Immutable meshoptimizer source hashes passed.")
