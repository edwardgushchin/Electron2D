"""Generate EQ PCM using byte-verified pinned C++ sources and minimal standalone shims."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
BASE = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/'
HASHES = {
    'eq_filter.cpp': 'c486824bfe9d80f793d577b3d54c482718ea40d5092fffa1519ae14083dbd19e',
    'eq_filter.h': '0f6561c99bb53e59f11d0a1d43dd0fc23e38fd2615f5917743bedebefe61e3ec',
}
with tempfile.TemporaryDirectory(prefix='e2d-eq-oracle-') as temporary:
    work = Path(temporary)
    for name, expected in HASHES.items():
        data = urllib.request.urlopen(BASE + name).read()
        assert hashlib.sha256(data).hexdigest() == expected, name
        (work / name).write_bytes(data)
    (work / 'core/templates').mkdir(parents=True)
    (work / 'core/error').mkdir(parents=True)
    (work / 'core/math').mkdir(parents=True)
    (work / 'core/templates/vector.h').write_text('''#pragma once
#include <vector>
template<class T> struct Vector {
    std::vector<T> data;
    struct Writer { Vector* owner; T& operator[](int i) { return owner->data[i]; } } write{this};
    void clear() { data.clear(); }
    int size() const { return static_cast<int>(data.size()); }
    void resize(int count) { data.resize(count); }
    void push_back(const T& value) { data.push_back(value); }
    T& operator[](int i) { return data[i]; }
    const T& operator[](int i) const { return data[i]; }
};
''')
    (work / 'core/error/error_macros.h').write_text('#pragma once\n#define ERR_CONTINUE(condition) if (condition) continue\n#define ERR_FAIL_INDEX_V(index, count, value) if ((index) < 0 || (index) >= (count)) return (value)\n')
    (work / 'core/math/math_funcs.h').write_text('#pragma once\n#include <cmath>\nnamespace Math { constexpr double TAU = 6.283185307179586476925286766559; constexpr double SQRT12 = 0.7071067811865475244; using std::log; using std::pow; using std::round; using std::sin; using std::cos; using std::sqrt; }\n')
    subprocess.run(['c++', '-std=c++17', '-O2', '-I', str(work), str(work / 'eq_filter.cpp'), str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    data = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(data)
    assert [case['Bands'] for case in cases] == [6, 10, 21]
    (ROOT / 'reference.json').write_bytes(data)
    print(f'{len(cases)} presets; sha256 {hashlib.sha256(data).hexdigest()}')
