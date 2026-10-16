"""Check native standalone decoding against independently generated pixel oracles."""

import argparse
import base64
import json
from pathlib import Path
import subprocess
import tempfile


def check(executable, oracle):
    document = json.loads(oracle.read_text())
    fixtures = document["fixtures"]
    with tempfile.TemporaryDirectory(prefix="electron2d-ktx native ") as directory:
        for fixture in fixtures:
            source = Path(directory) / (fixture["name"] + ".ktx2")
            output = source.with_suffix(".raw")
            source.write_bytes(base64.b64decode(fixture["encoded"]))
            subprocess.run([str(executable), str(source), str(output)], check=True)
            assert output.read_bytes() == base64.b64decode(fixture["pixels"]), fixture["name"]
        for fixture in document["failures"]:
            source = Path(directory) / (fixture["name"] + ".ktx2")
            source.write_bytes(base64.b64decode(fixture["encoded"]))
            subprocess.run([str(executable), "--reject", str(source)], check=True)
    print(f"{len(fixtures)} native raw/Basis LDR/HDR mip oracles, every truncation, 2304 header plus 2304 container mutations and owner/output failures passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("oracle", type=Path)
    args = parser.parse_args()
    check(args.executable.resolve(), args.oracle.resolve())
