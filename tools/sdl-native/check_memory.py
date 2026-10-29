"""Fresh-process SDL ownership/failure/retry matrix against the actual common C core."""

import argparse
import json
import os
from pathlib import Path
import subprocess


def check(build, report):
    env = {"PATH": os.defpath, "LANG": "C", "LC_ALL": "C"}
    # Exercise several environment hash-table growths without depending on host secrets/state.
    for i in range(80):
        env[f"E2D_MEMORY_VECTOR_{i}"] = "retained environment allocation " * 3
    env["LD_LIBRARY_PATH"] = os.pathsep.join(str(build / p) for p in ("fault", "sdl", "."))
    env["ASAN_OPTIONS"] = "detect_leaks=1:halt_on_error=1"
    env["UBSAN_OPTIONS"] = "halt_on_error=1:print_stacktrace=1"
    cases = []

    def run(role, position):
        result = subprocess.run([str(build / "Electron2DSDLMemoryCheck"), role, str(position)],
                                env=env, capture_output=True, text=True, timeout=60)
        if result.returncode:
            raise RuntimeError(f"{role}/{position}: exit {result.returncode}\n{result.stdout}\n{result.stderr}")
        value = json.loads(result.stdout.strip())
        cases.append(value)
        return value

    try:
        for role in ("surface", "properties", "io"):
            count = run(role, 0)["requests"]
            for position in range(1, count + 1):
                run(role, position)
    finally:
        report.write_text(json.dumps({"cases": cases, "failures": sum(c["position"] > 0 for c in cases)}, indent=2) + "\n")
    print(f"SDL ownership: {len(cases)} cases; {len(cases) - 3} failed requests; complete drain and healthy retry")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("build", type=Path)
    parser.add_argument("report", type=Path)
    args = parser.parse_args()
    check(args.build.resolve(), args.report)
