"""Compare the shaping domain with an unmodified build at the same pin, including every refusal."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import resource
import subprocess


def check(executable, library, fault, oracle, core, fonts, report):
    env = os.environ.copy()
    env["LD_LIBRARY_PATH"] = str(core.parent)
    env["ASAN_OPTIONS"] = "detect_leaks=1:halt_on_error=1"
    env["UBSAN_OPTIONS"] = "halt_on_error=1:print_stacktrace=1"
    cases = []

    def limits():
        resource.setrlimit(resource.RLIMIT_CORE, (0, 0))

    def run(lib, font, role, position=0):
        p = subprocess.run([str(executable), str(lib), str(font), str(role), str(position)],
                           env=env, capture_output=True, text=True, timeout=60, preexec_fn=limits)
        if p.returncode:
            raise RuntimeError(f"{font.name}/{role}/{position}: {p.returncode}\n{p.stdout}\n{p.stderr}")
        assert "Native shaping exit drain passed" in p.stderr
        return json.loads(p.stdout)

    try:
        for font in fonts:
            for role in range(16):
                expected = run(oracle, font, role)
                owned = run(library, font, role)
                assert owned["ok"] and owned["glyphs"] == expected["glyphs"], (font, role, owned, expected)
                normal = run(fault, font, role)
                assert normal["ok"] and normal["glyphs"] == expected["glyphs"]
                cases.append({"font": font.name, "role": role, "position": 0, "requests": normal["requests"]})
                for position in range(1, normal["requests"] + 1):
                    value = run(fault, font, role, position)
                    assert not value["ok"] or value["glyphs"] == expected["glyphs"], (font, role, position, value, expected)
                    assert value["retry"] == expected["glyphs"], (font, role, position, "retry", value, expected)
                    cases.append({"font": font.name, "role": role, "position": position, "recovered": value["ok"]})
    finally:
        report.write_text(json.dumps({"cases": cases, "inputs": {str(p): hashlib.sha256(p.read_bytes()).hexdigest()
                                                                for p in [executable, library, fault, oracle, core, *fonts]}}, indent=2))
    print(f"Shaping: {len(fonts) * 16} exact reference vectors, {len(cases)} cases, {len(cases) - len(fonts) * 16} refused requests, exit drain and exact retry")


if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    for name in ("executable", "library", "fault", "oracle", "core", "report"):
        p.add_argument(name, type=Path)
    p.add_argument("fonts", type=Path, nargs="+")
    a = p.parse_args()
    check(a.executable.resolve(), a.library.resolve(), a.fault.resolve(), a.oracle.resolve(), a.core.resolve(), a.fonts, a.report)
