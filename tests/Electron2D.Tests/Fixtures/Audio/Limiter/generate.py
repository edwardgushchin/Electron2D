"""Compile the byte-pinned legacy C++ limiter process function without changing it."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_limiter.cpp'
EXPECTED = 'b3159a4736e10e333aff60d20c76223ea6e23918b9cf1bcf2e91010a94a6faa4'

source = urllib.request.urlopen(URL, timeout=30).read()
digest = hashlib.sha256(source).hexdigest()
assert digest == EXPECTED, digest
text = source.decode()
start = text.index('void AudioEffectLimiterInstance::process(')
end = text.index('Ref<AudioEffectInstance> AudioEffectLimiter::instantiate()', start)
with tempfile.TemporaryDirectory(prefix='e2d-limiter-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work),
                    str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert len(cases) == 8 and all(len(case['PCM']) == 512 for case in cases)
    (ROOT / 'reference.json').write_bytes(result)
    print(f'8 profiles/4096 channel samples; source sha256 {digest}; output sha256 {hashlib.sha256(result).hexdigest()}')
