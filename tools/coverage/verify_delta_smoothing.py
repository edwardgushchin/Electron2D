#!/usr/bin/env python3
"""Compare measured C# clock traces with the pinned executable timer policy."""
import argparse
import hashlib
import json
import subprocess
import urllib.request
from pathlib import Path

PIN = "ed1daf0bf001b61586d9930840f2f1394092c079"
SOURCE_SHA256 = {
    "main_timer_sync.cpp": "041915dcd4464fd62273f7dc41ec4a3746201a0884e15e3066e13c403e3b40b0",
    "main_timer_sync.h": "f36be81f4b3faa68c308c8c0e2aaa16ff12da9a6a3372af477ec823371dbab50",
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("trace", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    sources = {}
    for name in ["main_timer_sync.cpp", "main_timer_sync.h"]:
        target = args.output / name
        if not target.exists():
            target.write_bytes(urllib.request.urlopen(f"https://raw.githubusercontent.com/godotengine/godot/{PIN}/main/{name}", timeout=30).read())
        if hashlib.sha256(target.read_bytes()).hexdigest() != SOURCE_SHA256[name]:
            raise ValueError(f"Pinned source digest mismatch: {target}")
        sources[name] = target.read_text()
    cpp, header = sources["main_timer_sync.cpp"], sources["main_timer_sync.h"]
    start = header.index("class DeltaSmoother {")
    end = header.index("} _delta_smoother;", start)
    declaration = header[start:end] + "};"
    start = cpp.index("void MainTimerSync::DeltaSmoother::made_new_estimate()")
    end = cpp.index("// returns the fraction of p_physics_step", start)
    methods = cpp[start:end]
    notice = cpp[:cpp.index('#include "main_timer_sync.h"')]
    harness = notice + r'''
#include <iostream>
#include <cmath>
#include <algorithm>
#include <cstdint>
#define MAX(a,b) ((a) > (b) ? (a) : (b))
namespace Math { inline double round(double n) { return std::round(n); } }
struct OS { inline static bool enabled = true; static OS *get_singleton() { static OS v; return &v; } bool is_delta_smoothing_enabled() { return enabled; } };
struct Engine { static Engine *get_singleton() { static Engine v; return &v; } bool is_editor_hint() { return false; } };
namespace DisplayServerEnums { enum VSyncMode { VSYNC_DISABLED, VSYNC_ENABLED }; constexpr int MAIN_WINDOW_ID = 0; }
struct DisplayServer { inline static bool enabled = true; static DisplayServer *get_singleton() { static DisplayServer v; return &v; } DisplayServerEnums::VSyncMode window_get_vsync_mode(int) { return enabled ? DisplayServerEnums::VSYNC_ENABLED : DisplayServerEnums::VSYNC_DISABLED; } static constexpr int MAIN_WINDOW_ID = 0; };
class MainTimerSync { public:
''' + declaration + "\n};\n" + methods + r'''
int main() {
    MainTimerSync::DeltaSmoother smoother;
    int reset, enabled, vsync; int64_t input;
    while (std::cin >> reset >> input >> enabled >> vsync) {
        if (reset) smoother = MainTimerSync::DeltaSmoother();
        OS::enabled = enabled; DisplayServer::enabled = vsync;
        std::cout << smoother.smooth_delta(input) << '\n';
    }
}
'''
    source = args.output / "oracle.cpp"
    source.write_text(harness)
    binary = args.output / "oracle"
    subprocess.run(["c++", "-std=c++17", "-O2", str(source), "-o", str(binary)], check=True)
    rows = [line.split() for line in args.trace.read_text().splitlines()]
    if not rows or any(len(row) != 5 for row in rows):
        parser.error("trace must contain five-column samples")
    input_text = "".join(" ".join(row[:4]) + "\n" for row in rows)
    result = subprocess.run([str(binary)], input=input_text, text=True, capture_output=True, check=True)
    expected = [int(line) for line in result.stdout.splitlines()]
    mismatches = [{"row": i + 1, "input": row[:4], "expected": wanted, "actual": int(row[4])}
                  for i, (row, wanted) in enumerate(zip(rows, expected)) if int(row[4]) != wanted]
    report = {"pin": PIN, "samples": len(rows), "outputs": len(expected), "mismatches": mismatches[:20],
              "source_sha256": {name: hashlib.sha256((args.output / name).read_bytes()).hexdigest() for name in sources},
              "trace_sha256": hashlib.sha256(args.trace.read_bytes()).hexdigest()}
    (args.output / "result.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))
    if mismatches or len(expected) != len(rows):
        raise SystemExit(1)


if __name__ == "__main__":
    main()
