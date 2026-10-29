#!/usr/bin/env python3
"""Inject one failure at every observed MSDF allocation through the actual bridge."""
import json
from pathlib import Path
import subprocess
import sys


def main():
    executable, output = map(Path, sys.argv[1:])
    rows = []
    for mode in ("line", "quadratic", "cubic", "teardrop", "hole", "reverse", "invalid", "budget", "empty"):
        row = {"mode": mode, "failures": []}
        rows.append(row)
        def run(position):
            result = subprocess.run([str(executable.resolve()), mode, str(position)], capture_output=True, text=True, timeout=20)
            report = {"position": position, "exit": result.returncode, "stdout": result.stdout, "stderr": result.stderr}
            if position: row["failures"].append(report)
            else: row["normal"] = report
            output.write_text(json.dumps(rows, indent=2) + "\n")
            if result.returncode: raise SystemExit(report)
            counts = json.loads(result.stdout)
            if counts["Current"] != 0 or counts["Failures"] != bool(position):
                raise SystemExit("MSDF failure was missed or native ownership leaked")
            return counts
        normal = run(0)
        if mode != "empty" and normal["Calls"] <= 0: raise SystemExit("MSDF bypassed native accounting")
        for position in range(1, normal["Calls"] + 1): run(position)
    print(f"MSDF memory: {len(rows)} modes, {sum(len(r['failures']) for r in rows)} allocation failures; exact stock pixels, balanced ownership and retry passed.")


if __name__ == "__main__":
    main()
