"""Compile the byte-pinned C++ compressor function with an independent audio-server shim."""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parent
URL = 'https://raw.githubusercontent.com/godotengine/godot/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_compressor.cpp'
EXPECTED = 'd073aa69a35842e29a46bcf4fdd326920ff595007dad1f6d29a9646c86e5106e'
source = urllib.request.urlopen(URL, timeout=30).read()
assert hashlib.sha256(source).hexdigest() == EXPECTED
text = source.decode()
start = text.index('void AudioEffectCompressorInstance::process(')
end = text.index('Ref<AudioEffectInstance> AudioEffectCompressor::instantiate()', start)
with tempfile.TemporaryDirectory(prefix='e2d-compressor-oracle-') as temporary:
    work = Path(temporary)
    (work / 'process.inc').write_text(text[start:end])
    subprocess.run(['c++', '-std=c++17', '-O2', '-ffp-contract=off', '-I', str(work), str(ROOT / 'oracle.cpp'), '-o', str(work / 'oracle')], check=True)
    result = subprocess.check_output([str(work / 'oracle')])
    cases = json.loads(result)
    assert len(cases) == 14 and all(len(case['PCM']) == 2048 for case in cases)
    (ROOT / 'reference.json').write_bytes(result)
    print('14 profiles/28672 channel samples; source sha256', EXPECTED, '; output sha256', hashlib.sha256(result).hexdigest())
