#!/usr/bin/env python3
"""Run pinned C++ style geometry with minimal float math/container/recording hosts.

Requires a C++17 compiler only for regeneration; runtime tests read the fixture.
No geometry code is translated or changed. The immutable source is checksum-checked,
then its helper, bounds and draw functions are compiled verbatim at oversampling one.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
from urllib.request import urlopen

COMMIT = "ed1daf0bf001b61586d9930840f2f1394092c079"
SOURCE_SHA256 = "69d56a1feb8705043380f30073af12571c4b9b084838208c6748488133c51173"
SOURCE_URL = f"https://raw.githubusercontent.com/godotengine/godot/{COMMIT}/scene/resources/style_box_flat.cpp"
ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = ROOT / "tests/Electron2D.Tests/Fixtures/StyleBoxFlatGeometry.json"


def profiles():
    common = {"rect": [3.25, 4.5, 18.75, 13.25], "border_width": [2, 3, 4, 1], "corner_radius": [7, 2, 5, 3],
              "corner_detail": 4, "aa_size": .75, "bg_color": [.2, .4, .6, .8], "border_color": [.8, .2, .1, .7],
              "skew": [.2, -.15], "expand_margin": [1, 2, 3, 4]}
    return [
        {"name": "default", "rect": [0, 0, 20, 12]},
        {"name": "plain-border", "rect": [0, 0, 20, 12], "border_width": [2, 2, 2, 2]},
        {"name": "rounded-fill-aa", "rect": [0, 0, 20, 12], "corner_radius": [4, 4, 4, 4], "corner_detail": 3},
        {**common, "name": "asymmetric-aa"},
        {**common, "name": "asymmetric-no-aa", "anti_aliased": False},
        {**common, "name": "blended-filled", "blend_border": True},
        {**common, "name": "blended-hollow", "blend_border": True, "draw_center": False},
        {"name": "shadow-only", "rect": [0, 0, 20, 12], "draw_center": False, "corner_radius": [5, 5, 5, 5],
         "corner_detail": 3, "shadow_size": 4, "shadow_offset": [2, -1], "shadow_color": [.1, .2, .3, .5]},
        {**common, "name": "shadow-border-aa", "shadow_size": 4, "shadow_offset": [-2, 3], "expand_margin": [-1, 2, 3, -1]},
        {"name": "oversized", "rect": [0, 0, 9, 7], "border_width": [20, 8, 9, 4], "corner_radius": [20, 14, 13, 27], "corner_detail": 5},
        {"name": "partial-border-aa", "rect": [0, 0, 15, 9], "border_width": [0, 3, 0, 1], "corner_radius": [5, 0, 2, 0], "corner_detail": 3},
        {"name": "signed-widths-radii", "rect": [0, 0, 15, 9], "border_width": [-1, 2, -3, 1], "corner_radius": [-2, 4, 3, -1], "shadow_size": -2, "corner_detail": 3},
        {"name": "zero-size", "rect": [0, 0, 0, 9]},
        {"name": "disabled", "rect": [0, 0, 15, 9], "draw_center": False},
        {"name": "negative-size", "rect": [0, 0, -15, 9]},
    ]


def cpp_float(value):
    return f"{float(value):.9f}f"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, help="Use an already downloaded checksum-matching C++ source")
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--compiler", default="c++")
    args = parser.parse_args()
    raw = args.source.read_bytes() if args.source else urlopen(SOURCE_URL).read()
    if hashlib.sha256(raw).hexdigest() != SOURCE_SHA256:
        raise SystemExit("Pinned geometry source SHA-256 mismatch")
    source = raw.decode()
    geometry = source[source.index("inline void set_inner_corner_radius"):source.index("void StyleBoxFlat::_bind_methods()")]
    shim = Path(__file__).with_name("style_box_flat_oracle_shim.h").read_text()
    cases = profiles()
    calls = ["int main() {"]
    for case in cases:
        calls.append("{ StyleBoxFlat s;")
        for key, value in case.items():
            if key in ("name", "rect"):
                continue
            if key in ("border_width", "corner_radius", "expand_margin"):
                calls.extend(f"s.{key}[{i}]={cpp_float(v)};" for i, v in enumerate(value))
            elif isinstance(value, list):
                calls.append(f"s.{key}={{{','.join(cpp_float(v) for v in value)}}};")
            elif isinstance(value, bool):
                calls.append(f"s.{key}={'true' if value else 'false'};")
            else:
                calls.append(f"s.{key}={cpp_float(value)};")
        calls.append(f"output_geometry(s, {{{','.join(cpp_float(v) for v in case['rect'])}}}); }}")
    calls.append("}")
    with tempfile.TemporaryDirectory(prefix="style-flat-oracle-") as directory:
        folder = Path(directory); unit = folder / "oracle.cpp"; binary = folder / "oracle"
        unit.write_text(shim + "\n" + geometry + "\n" + "\n".join(calls))
        subprocess.run([args.compiler, "-std=c++17", "-O0", "-ffp-contract=off", str(unit), "-o", str(binary)], check=True)
        results = [json.loads(line) for line in subprocess.check_output([str(binary)], text=True).splitlines()]
    fixture = {"sourceCommit": COMMIT, "sourceSHA256": SOURCE_SHA256, "sourceURL": SOURCE_URL,
               "generator": "tools/coverage/generate_style_box_flat_oracle.py", "precision": "float32, no FMA, oversampling 1",
               "vertexLayout": ["x", "y", "r", "g", "b", "a", "u", "v"], "cases": [], "nonfiniteCases": []}
    for case, result in zip(cases, results, strict=True):
        if result is None:
            fixture["nonfiniteCases"].append(case)
        else:
            fixture["cases"].append({**case, **result})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(fixture, separators=(",", ":"), allow_nan=False) + "\n")
    print(f"{len(fixture['cases'])} finite cases; {len(fixture['nonfiniteCases'])} nonfinite cases; {args.output.stat().st_size} bytes; source SHA-256 {SOURCE_SHA256}")


if __name__ == "__main__":
    main()
