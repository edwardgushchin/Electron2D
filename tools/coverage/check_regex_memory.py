#!/usr/bin/env python3
"""Check each observed PCRE2 allocation failure through the production native bridge."""

import json
from pathlib import Path
import subprocess
import sys


def main():
    executable, output = map(Path, sys.argv[1:])
    results = []
    for mode in ("compile", "invalid", "match", "nomatch", "size", "substitute", "replacement", "budget"):
        command = [str(executable.resolve()), mode]
        normal = subprocess.run(command + ["0"], capture_output=True, text=True, timeout=20)
        row = {"mode": mode, "normal": {"exit": normal.returncode, "stdout": normal.stdout, "stderr": normal.stderr}, "failures": []}
        results.append(row)
        output.write_text(json.dumps(results, indent=2) + "\n")
        if normal.returncode:
            raise SystemExit(row)
        report = json.loads(normal.stdout)
        if report["Calls"] <= 0 or report["Current"] != 0 or report["Failures"] != 0:
            raise SystemExit("Normal PCRE2 case did not establish a balanced allocation cohort")
        for position in range(1, report["Calls"] + 1):
            result = subprocess.run(command + [str(position)], capture_output=True, text=True, timeout=20)
            row["failures"].append({"position": position, "exit": result.returncode, "stdout": result.stdout, "stderr": result.stderr})
            output.write_text(json.dumps(results, indent=2) + "\n")
            if result.returncode:
                raise SystemExit(row["failures"][-1])
            fault = json.loads(result.stdout)
            if fault["Current"] != 0 or fault["Failures"] != 1:
                raise SystemExit("PCRE2 child missed the requested failure or leaked native payload")
    print(f"PCRE2 memory: {len(results)} modes, {sum(len(row['failures']) for row in results)} failure positions; balanced teardown and successful retry.")


if __name__ == "__main__":
    main()
