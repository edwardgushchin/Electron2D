"""Compile the pinned C++ hard-limiter processor with a standalone audio shim."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_hard_limiter.cpp'
EXPECTED = '3a3f5e8679d47705bcd65efc59d2779fddbd531c78d029dae17fc5d35952747e'

source = urllib.request.urlopen(URL).read()
digest = hashlib.sha256(source).hexdigest()
assert digest == EXPECTED, digest
text = source.decode()
start = text.index('void AudioEffectHardLimiterInstance::process(')
end = text.index('Ref<AudioEffectInstance> AudioEffectHardLimiter::instantiate()', start)
with tempfile.TemporaryDirectory(prefix='e2d-hard-limiter-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert [(case['Rate'], case['Profile']) for case in cases] == [(rate, profile) for rate in (44100, 48000) for profile in range(2)]
    (ROOT / 'reference.json').write_bytes(result)
    print(f'{len(cases)} profiles, {len(cases[0]["PCM"])} channel samples each; source sha256 {digest}; output sha256 {hashlib.sha256(result).hexdigest()}')
