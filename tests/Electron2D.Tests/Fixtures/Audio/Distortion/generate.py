"""Compile the exact pinned C++ distortion processor with a standalone audio shim."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_distortion.cpp'
EXPECTED = '633aa12deed0800c4ba738407374435a7d58fa123d53cf6e21326248495c047b'

source = urllib.request.urlopen(URL).read()
digest = hashlib.sha256(source).hexdigest()
assert digest == EXPECTED, digest
text = source.decode()
start = text.index('void AudioEffectDistortionInstance::process(')
end = text.index('Ref<AudioEffectInstance> AudioEffectDistortion::instantiate()', start)
with tempfile.TemporaryDirectory(prefix='e2d-distortion-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert [(case['Rate'], case['Mode']) for case in cases] == [(rate, mode) for rate in (44100, 48000) for mode in range(5)]
    (ROOT / 'reference.json').write_bytes(result)
    print(f'{len(cases)} modes, {len(cases[0]["PCM"])} channel samples per mode; source sha256 {digest}; output sha256 {hashlib.sha256(result).hexdigest()}')
