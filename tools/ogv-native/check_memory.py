"""Exercise every direct bridge allocation refusal against a built native checker."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def check(checker, fixtures, output):
    results = []
    for fixture in fixtures:
        for role in ("encode", "decode"):
            command = [str(checker.resolve()), str(fixture.resolve()), role]
            baseline = json.loads(subprocess.check_output([*command, "0"], text=True))
            for position in range(1, baseline["attempts"] + 1):
                result = subprocess.run([*command, str(position)], capture_output=True, text=True)
                if result.returncode:
                    raise RuntimeError(f"{fixture.name}/{role}/{position}: {result.returncode}\n{result.stdout}{result.stderr}")
                row = json.loads(result.stdout)
                assert row["failure"] == position and row["attempts"] == position, row
            results.append({"fixture": str(fixture), "sha256": hashlib.sha256(fixture.read_bytes()).hexdigest(), **baseline})
    output.write_text(json.dumps({"checker_sha256": hashlib.sha256(checker.read_bytes()).hexdigest(), "results": results}, indent=2) + "\n")
    print(f"Direct OGV bridge: {sum(row['attempts'] for row in results)} refusal positions; preparation/retirement/retry passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("checker", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("fixtures", type=Path, nargs="+")
    args = parser.parse_args()
    check(args.checker, args.fixtures, args.output)
