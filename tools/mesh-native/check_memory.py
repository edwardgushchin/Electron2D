"""Check every observed simplifier allocation failure against the pristine oracle."""
import hashlib
import json
import os
from pathlib import Path
import resource
import subprocess
import sys

build, report = map(Path, sys.argv[1:])
resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
env = os.environ | {'ASAN_OPTIONS': 'detect_leaks=1:halt_on_error=1', 'UBSAN_OPTIONS': 'halt_on_error=1:print_stacktrace=1'}
oracle = json.loads(subprocess.check_output([str(build / 'Electron2DMeshOracle')], env=env, text=True))
rows = []
for profile, record in enumerate(oracle['records']):
    position, maximum = 0, 0
    while position <= maximum:
        result = subprocess.run([str(build / 'Electron2DMeshMemoryCheck'), str(profile), str(position)], env=env, text=True, capture_output=True)
        if result.returncode:
            raise RuntimeError(f'profile={profile}, failAt={position}: {result.returncode}\n{result.stdout}\n{result.stderr}')
        row = json.loads(result.stdout)
        if row['levels'] != record['levels']:
            raise RuntimeError('Prepared output changed from pristine oracle')
        if position == 0:
            maximum = row['attempts']
        elif row['attempts'] < position:
            raise RuntimeError('Failure position was not exercised')
        rows.append(row)
        position += 1
library = build / 'libElectron2DMesh.so'
report.write_text(json.dumps({'librarySHA256': hashlib.sha256(library.read_bytes()).hexdigest(), 'faultPositions': sum(r['failAt'] != 0 for r in rows), 'cases': rows}, indent=2) + '\n', encoding='utf-8')
print(f"Passed {len(rows)} cases, {sum(r['failAt'] != 0 for r in rows)} failed requests; exact pristine levels, typed failure, full drain and retry.")
