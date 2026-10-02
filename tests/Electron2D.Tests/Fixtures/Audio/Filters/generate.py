"""Regenerate independent PCM from byte-verified pinned C++ sources."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
BASE = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/'
HASHES = {
    'audio_filter_sw.h': '1b954ac2c428a8eba9f68b18c26d96861e5b404ce160659df5a32e44fdd5dd01',
    'audio_filter_sw.cpp': '5639b43558814dd7cfd6dbbac11af85a9b0b09631e1932eaebc21f49345c02e0',
}
with tempfile.TemporaryDirectory(prefix='e2d-filter-oracle-') as temporary:
    work = Path(temporary)
    for name, expected in HASHES.items():
        data = urllib.request.urlopen(BASE + name).read()
        assert hashlib.sha256(data).hexdigest() == expected, name
        (work / name).write_bytes(data)
    (work / 'core/math').mkdir(parents=True)
    (work / 'core/typedefs.h').write_text('#pragma once\n#define _ALWAYS_INLINE_ inline\n')
    (work / 'core/math/math_funcs.h').write_text('#pragma once\n#include <cmath>\nnamespace Math { constexpr double TAU = 6.283185307179586476925286766559; using std::sin; using std::cos; using std::pow; using std::sqrt; using std::log; using std::sinh; }\n')
    subprocess.run(['c++', '-std=c++17', '-O2', '-I', str(work), str(work / 'audio_filter_sw.cpp'), str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    data = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(data)
    assert len(cases) == 72
    (ROOT / 'reference.json').write_bytes(data)
    print(f'{len(cases)} cases; sha256 {hashlib.sha256(data).hexdigest()}')
