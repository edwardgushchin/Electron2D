"""Compile the byte-pinned C++ phaser processor against a standalone audio shim."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_phaser.cpp'
EXPECTED = 'b35c05757aaa26316f159523d9647109ee1c0c31a013c07ccbe30c559c4c3dd2'

source = urllib.request.urlopen(URL).read()
digest = hashlib.sha256(source).hexdigest()
assert digest == EXPECTED, digest
text = source.decode()
start = text.index('void AudioEffectPhaserInstance::process(')
end = text.index('Ref<AudioEffectInstance> AudioEffectPhaser::instantiate()', start)
with tempfile.TemporaryDirectory(prefix='e2d-phaser-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert [(case['Rate'], case['Profile']) for case in cases] == [(rate, profile) for rate in (44100, 48000) for profile in range(2)]
    (ROOT / 'reference.json').write_bytes(result)
    print(f'{len(cases)} profiles, {len(cases[0]["PCM"])} channel samples each; source sha256 {digest}; output sha256 {hashlib.sha256(result).hexdigest()}')
