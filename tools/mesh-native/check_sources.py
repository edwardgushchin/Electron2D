#!/usr/bin/env python3
"""Check the immutable simplifier source closure."""
import hashlib
import json
from pathlib import Path
root = Path(__file__).resolve().parents[2] / "src/Vendor/meshoptimizer"
manifest = json.loads((root / "manifest.json").read_text())
assert manifest["commit"] == "9e1f07b159d3cb777f1c67ed31fc11fd117986f4"
for name, expected in manifest["files"].items():
    assert hashlib.sha256((root / name).read_bytes()).hexdigest() == expected, name
print("Immutable meshoptimizer source hashes passed.")
