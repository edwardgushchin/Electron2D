"""Compile the pinned C++ pitch-shift processor with a standalone audio shim."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_pitch_shift.cpp'
EXPECTED = '17badfd0941b6c384ecc732751c35f81bebbf63f8785850f0946612e95aeb600'

source = urllib.request.urlopen(URL).read()
digest = hashlib.sha256(source).hexdigest()
assert digest == EXPECTED, digest
text = source.decode()
start = text.index('void SMBPitchShift::PitchShift(')
end = text.index('/* Godot code again */', start)
with tempfile.TemporaryDirectory(prefix='e2d-pitch-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert [(case['Rate'], case['Size']) for case in cases] == [(rate, size) for rate in (44100, 48000) for size in (256, 2048)]
    (ROOT / 'reference.json').write_bytes(result)
    print(f'{len(cases)} profiles; source sha256 {digest}; output sha256 {hashlib.sha256(result).hexdigest()}')
