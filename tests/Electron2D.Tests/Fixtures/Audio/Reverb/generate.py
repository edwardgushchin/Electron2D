"""Compile byte-pinned upstream reverb_filter.cpp with tiny standalone memory/math shims."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
BASE = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/'
HASHES = {
    'reverb_filter.cpp': 'd09bd31bdc4291cff8a0d18579690c531c9fc99ed3eafced19696a3052f6b806',
    'reverb_filter.h': '8b90f4990d0cdb2cf6adcb21a803028f79e462519a55fd3a5958d7957828ad9b',
}
with tempfile.TemporaryDirectory(prefix='e2d-reverb-oracle-') as temporary:
    work = Path(temporary)
    for name, expected in HASHES.items():
        data = urllib.request.urlopen(BASE + name).read()
        assert hashlib.sha256(data).hexdigest() == expected, name
        (work / name).write_bytes(data)
    (work / 'core/math').mkdir(parents=True)
    (work / 'core/os').mkdir(parents=True)
    (work / 'core/math/audio_frame.h').write_text('''#pragma once
#include <cmath>
#include <cstdint>
#include <cstring>
namespace Math { constexpr double TAU = 6.283185307179586476925286766559; }
inline float undenormalize(float value) {
    uint32_t bits;
    std::memcpy(&bits, &value, sizeof(bits));
    return (bits & 0x7f800000u) < 0x08000000u ? 0.0f : value;
}
''')
    (work / 'core/os/memory.h').write_text('#pragma once\n#define memnew_arr(type, count) new type[count]\n#define memdelete_arr(value) delete[] value\n')
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(work / 'reverb_filter.cpp'), str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    data = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(data)
    assert [case['Profile'] for case in cases] == [0, 1, 2]
    (ROOT / 'reference.json').write_bytes(data)
    print(f'{len(cases)} profiles; sha256 {hashlib.sha256(data).hexdigest()}')
