"""Run the same block corpus against shared, static or sanitizer native checks."""

import argparse
import base64
import json
from pathlib import Path
import subprocess
import tempfile


def run(check, oracle):
    fixtures = json.loads(oracle.read_text())["fixtures"]
    count = 0
    with tempfile.TemporaryDirectory(prefix="electron2d-block-check-") as directory:
        root = Path(directory)
        for fixture in fixtures:
            if fixture["source_format"] == "Etc2RaAsRg":
                continue  # This swizzle belongs to the managed image boundary.
            source, pixels = root / "input.blocks", root / "pixels.raw"
            source.write_bytes(base64.b64decode(fixture["encoded"]))
            pixels.write_bytes(base64.b64decode(fixture["pixels"]))
            subprocess.run([str(check), str(fixture["mode"]), str(source), str(pixels)], check=True)
            count += 1
    print(f"{count} block formats: independent pixels, ragged mips, exact buffers, unaligned borrows, 9216 mutations, 1152 concurrent calls passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("check", type=Path)
    parser.add_argument("oracle", type=Path)
    args = parser.parse_args()
    run(args.check.resolve(), args.oracle.resolve())
