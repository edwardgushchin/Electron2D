"""Exercise every direct bridge allocation refusal against a built native checker."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def check(checker, fixtures, output):
    container = checker.with_name("Electron2DOGGContainerCheck")
    container_baseline = json.loads(subprocess.check_output([str(container.resolve()), "0"], text=True))
    for position in range(1, container_baseline["attempts"] + 1):
        result = subprocess.run([str(container.resolve()), str(position)], capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(f"Container/{position}: {result.returncode}\n{result.stdout}{result.stderr}")
        row = json.loads(result.stdout)
        assert row["failure"] == position and row["attempts"] >= position, row
    results = []
    for fixture in fixtures:
        for role in ("encode", "encode-noise", "decode"):
            command = [str(checker.resolve()), str(fixture.resolve()), role]
            baseline = json.loads(subprocess.check_output([*command, "0"], text=True))
            for position in range(1, baseline["attempts"] + 1):
                result = subprocess.run([*command, str(position)], capture_output=True, text=True)
                if result.returncode:
                    raise RuntimeError(f"{fixture.name}/{role}/{position}: {result.returncode}\n{result.stdout}{result.stderr}")
                row = json.loads(result.stdout)
                assert row["failure"] == position and row["attempts"] >= position, row
            results.append({"fixture": str(fixture), "sha256": hashlib.sha256(fixture.read_bytes()).hexdigest(), **baseline})
    output.write_text(json.dumps({"checker_sha256": hashlib.sha256(checker.read_bytes()).hexdigest(),
                                 "container_checker_sha256": hashlib.sha256(container.read_bytes()).hexdigest(),
                                 "container": container_baseline, "results": results}, indent=2) + "\n")
    print(f"OGV bridge/container: {sum(row['attempts'] for row in results)} bridge and {container_baseline['attempts']} container refusal positions; preparation/growth/retirement/retry passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("checker", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("fixtures", type=Path, nargs="+")
    args = parser.parse_args()
    check(args.checker, args.fixtures, args.output)
