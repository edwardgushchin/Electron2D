"""Compare recorded line coverage/attributes against an unchanged pinned C++ builder."""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import random
import struct
import subprocess
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
PIN = 'ed1daf0bf001b61586d9930840f2f1394092c079'
SOURCES = {
    'scene/2d/line_builder.cpp': 'ddf0a8d1b996e48fdc453fb4e1799ff507eab9f1764d23401e384ac101a396c3',
    'scene/2d/line_builder.h': '74e8d50cbe4beeb614a45a4565bc594a12d41183fd1c54564c37ee20b5295368',
    'core/math/geometry_2d.h': '5ee24348394e74bddf853dbd64c9ad819829095997f4411788f92b1e53e47eb0',
}


def triangles(vertices):
    result = []
    for i in range(0, len(vertices), 3):
        a, b, c = vertices[i:i + 3]
        area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
        if abs(area) > 1e-8:
            result.append((a, b, c, area))
    return result


def sample_at(tris, point):
    hits = []
    x, y = point
    for a, b, c, area in tris:
        wb = ((x - a[0]) * (c[1] - a[1]) - (y - a[1]) * (c[0] - a[0])) / area
        wc = ((b[0] - a[0]) * (y - a[1]) - (b[1] - a[1]) * (x - a[0])) / area
        wa = 1 - wb - wc
        if min(wa, wb, wc) < -.0001:
            continue
        # Avoid attributing a native shared-edge tie to one of two different triangulations.
        if min(wa, wb, wc) < .0001:
            return None
        hits.append([wa * a[i] + wb * b[i] + wc * c[i] for i in range(2, 8)])
    return hits


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('fixtures', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--cache', type=Path)
    args = parser.parse_args()
    cache = args.cache or args.output.parent / 'reference'
    cache.mkdir(parents=True, exist_ok=True)
    for source, digest in SOURCES.items():
        target = cache / Path(source).name
        if not target.exists():
            target.write_bytes(urllib.request.urlopen(f'https://raw.githubusercontent.com/godotengine/godot/{PIN}/{source}', timeout=60).read())
        assert hashlib.sha256(target.read_bytes()).hexdigest() == digest, source
    # Extract the exact pinned segment test; only minimal value containers live in the adapter.
    text = (cache / 'geometry_2d.h').read_text()
    start = text.index('\tstatic bool segment_intersects_segment(')
    end = text.index('\n\tstatic inline bool is_point_in_circle', start)
    stub = cache / 'stubs/core/math/geometry_2d.h'
    stub.parent.mkdir(parents=True, exist_ok=True)
    stub.write_text('#pragma once\n#include "scene/2d/line_2d.h"\nstruct Geometry2D{\n' + text[start:end] + '\n};\n')
    executable = cache / 'line-oracle'
    command = ['g++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(cache), '-I', str(cache / 'stubs'), '-I', str(ROOT / 'tools/line/reference'), str(cache / 'line_builder.cpp'), str(ROOT / 'tools/line/reference/oracle.cpp'), '-o', str(executable)]
    subprocess.run(command, check=True)
    fixtures = json.loads(args.fixtures.read_text())
    data = bytearray(struct.pack('<I', len(fixtures)))
    for f in fixtures:
        data += struct.pack('<9iff', len(f['Points']), f['Joint'], f['Begin'], f['End'], f['Mode'], f['Closed'], f['Curve'], f['Gradient'], f['Precision'], f['SharpLimit'], 2.)
        data += b''.join(struct.pack('<ff', *p) for p in f['Points'])
    output = subprocess.check_output([str(executable)], input=data, timeout=120)
    stream = io.BytesIO(output)
    assert struct.unpack('<I', stream.read(4))[0] == len(fixtures)
    mismatches = []
    compared = skipped = covered = 0
    maximum_difference = 0.0
    per_case_covered = []
    rng = random.Random(92731)
    for f in fixtures:
        count = struct.unpack('<I', stream.read(4))[0]
        reference = [struct.unpack('<8f', stream.read(32)) for _ in range(count)]
        actual = f['Geometry']
        expected_tris, actual_tris = triangles(reference), triangles(actual)
        all_vertices = reference + actual
        assert all(math.isfinite(v) for vertex in all_vertices for v in vertex), f['Sample']
        points = [(sum(t[i][0] for i in range(3)) / 3, sum(t[i][1] for i in range(3)) / 3) for t in expected_tris + actual_tris]
        if all_vertices:
            lo = [min(v[i] for v in all_vertices) for i in range(2)]
            hi = [max(v[i] for v in all_vertices) for i in range(2)]
            points += [(rng.uniform(lo[0] - .5, hi[0] + .5), rng.uniform(lo[1] - .5, hi[1] + .5)) for _ in range(64)]
        case_covered = 0
        for point in points:
            a, b = sample_at(expected_tris, point), sample_at(actual_tris, point)
            if a is None or b is None:
                skipped += 1
                continue
            compared += 1
            difference = max((abs(x - y) for u, v in zip(a, b) for x, y in zip(u, v)), default=0)
            maximum_difference = max(maximum_difference, difference)
            if a or b:
                covered += 1
                case_covered += 1
            if len(a) != len(b) or difference > .001:
                mismatches.append(dict(sample=f['Sample'], point=point, expected=a, actual=b, difference=difference))
                if len(mismatches) >= 32:
                    break
        per_case_covered.append(case_covered)
        if len(mismatches) >= 32:
            break
    assert mismatches or all(n > 0 for n in per_case_covered), "A case had no compared interior sample"
    assert not stream.read(1) if not mismatches else True
    result = dict(pin=PIN, source_hashes=SOURCES, compile_command=command, cases=len(fixtures), samples_compared=compared, covered_samples=covered, minimum_covered_samples_per_case=min(per_case_covered), maximum_attribute_difference=maximum_difference, shared_edge_samples_skipped=skipped, mismatches=mismatches)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k not in ['compile_command', 'source_hashes', 'mismatches']}))
    assert not mismatches, mismatches[:3]


if __name__ == '__main__':
    main()
