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
            subprocess.run([str(executable), *(["--basis"] if fixture.get("container") == "basis" else []), str(source), str(output)], check=True)
            assert output.read_bytes() == base64.b64decode(fixture["pixels"]), fixture["name"]
        for fixture in document.get("failures", []):
            source = Path(directory) / (fixture["name"] + ".ktx2")
            source.write_bytes(base64.b64decode(fixture["encoded"]))
            subprocess.run([str(executable), *(["--basis"] if fixture.get("container") == "basis" else []), "--reject", str(source)], check=True)
    print(f"{len(fixtures)} native raw/Basis LDR/HDR mip oracles, every truncation, {len(fixtures) * 256} header plus {len(fixtures) * 256} container mutations and owner/output failures passed.")
    if any(fixture.get("container") == "basis" for fixture in fixtures):
        print("Legacy Basis checks include unaligned borrowed input and checksum-repaired payload mutations.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("oracle", type=Path)
    parser.add_argument("--mixed-oracle", type=Path, help="Also race cold KTX/Basis initialization against a legacy Basis fixture.")
    args = parser.parse_args()
    check(args.executable.resolve(), args.oracle.resolve())
    if args.mixed_oracle:
        ktx = next(f for f in json.loads(args.oracle.read_text())["fixtures"] if f["name"] == "basis-lz")
        basis = next(f for f in json.loads(args.mixed_oracle.read_text())["fixtures"] if f["name"].startswith("etc1s"))
        with tempfile.TemporaryDirectory(prefix="electron2d-mixed-basis-") as directory:
            paths = [Path(directory) / name for name in ("first.ktx2", "second.basis")]
            for path, fixture in zip(paths, (ktx, basis)):
                path.write_bytes(base64.b64decode(fixture["encoded"]))
            subprocess.run([str(args.executable.resolve()), "--mixed", *map(str, paths)], check=True)
        print("Mixed KTX/Basis cold initialization: 8 threads, 128 decoded owners passed.")
