#!/usr/bin/env python3
"""Exercise every observed ICU allocation-failure position in a fresh native process."""

import concurrent.futures
import json
from pathlib import Path
import subprocess
import sys


def main():
    executable, data, output = map(Path, sys.argv[1:])
    command = [str(executable.resolve()), str(data.resolve())]

    def run(position):
        try:
            result = subprocess.run(command + [str(position)], capture_output=True, text=True, timeout=15)
            return {"position": position, "exit": result.returncode, "stdout": result.stdout, "stderr": result.stderr}
        except subprocess.TimeoutExpired:
            return {"position": position, "exit": None, "stderr": "Native child exceeded 15 seconds"}

    normal = run(0)
    if normal["exit"] != 0:
        output.write_text(json.dumps({"normal": normal}, indent=2) + "\n")
        raise SystemExit(normal)
    report = json.loads(normal["stdout"])
    if report["Calls"] <= 0 or report["Current"] != 0 or report["Failures"] != 0:
        raise SystemExit("The normal native oracle did not establish a balanced allocation cohort")
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        rows = list(pool.map(run, range(1, report["Calls"] + 1)))
    output.write_text(json.dumps({"command": command, "normal": normal, "failures": rows}, indent=2) + "\n")
    failed = [row for row in rows if row["exit"] != 0]
    if failed:
        raise SystemExit(f"ICU allocation failures: {failed[:4]}")
    print(f"ICU memory oracle: {report['Calls']} failure positions, peak {report['Peak']}, retained {report['Retained']}, final zero payload bytes.")


if __name__ == "__main__":
    main()
