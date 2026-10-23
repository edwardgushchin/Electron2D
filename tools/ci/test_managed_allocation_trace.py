#!/usr/bin/env python3
"""Calibrate one compiled profiling library and matching executable on its host."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def main():
    library, executable, output = map(lambda value: Path(value).resolve(), sys.argv[1:])
    output.mkdir(parents=True, exist_ok=False)
    env = os.environ | {
        'CORECLR_ENABLE_PROFILING': '1',
        'CORECLR_PROFILER': '{4CB0917C-5E03-4787-83BC-98C86DC7BE4C}',
        'CORECLR_PROFILER_PATH': str(library),
        'DOTNET_TieredCompilation': '0',
        'E2D_ALLOCATION_TRACE': str(output / 'calibration.trace'),
    }
    for name, changes in [('disabled', {'CORECLR_ENABLE_PROFILING': '0'}), ('missing-output', {'E2D_ALLOCATION_TRACE': None})]:
        rejected_env = env | changes
        rejected_env = {key: value for key, value in rejected_env.items() if value is not None}
        with (output / (name + '.log')).open('w') as log:
            rejected = subprocess.run([str(executable)], env=rejected_env, stdout=log, stderr=subprocess.STDOUT, umask=0o077, timeout=30)
        assert rejected.returncode == 132, (name, rejected.returncode)
    with (output / 'output-error.log').open('w') as log:
        rejected = subprocess.run([str(executable), '--fail-output'], env=env | {'E2D_ALLOCATION_TRACE': str(output / 'failed-output.trace')}, stdout=log, stderr=subprocess.STDOUT, umask=0o077, timeout=30)
    assert rejected.returncode == 134, rejected.returncode
    with (output / 'calibration.log').open('w') as log:
        result = subprocess.run([str(executable)], env=env, stdout=log, stderr=subprocess.STDOUT, umask=0o077, timeout=30)
    assert result.returncode == 0, (result.returncode, output / 'calibration.log')
    architecture = [line.removeprefix('Allocation calibration architecture: ') for line in (output / 'calibration.log').read_text().splitlines() if line.startswith('Allocation calibration architecture: ')]
    assert len(architecture) == 1 and architecture[0] in ('X64', 'Arm64'), architecture
    traces = list(output.glob('calibration.trace.*'))
    assert len(traces) == 1, traces
    rows = [line.split('\t') for line in traces[0].read_text().splitlines()]
    assert rows[0] == ['INITIALIZE', '0'] and rows[-1] == ['SHUTDOWN', 'recursive:0']
    allocated = [row for row in rows if row[0] == 'ALLOC']
    assert [int(row[3]) for row in allocated] == [64, 32, 32, 64], allocated
    assert [int(row[1]) for row in allocated] == [1, 2, 2, 3], allocated
    assert all(row[6:8] == ['0', '0'] for row in allocated), allocated
    assert allocated[0][2] == allocated[1][2] == allocated[2][2] != allocated[3][2]
    assert [row for row in rows if row[0] == 'STACK_END'] == [['STACK_END', '0']] * 4
    begins = [row[1:] for row in rows if row[0] == 'BEGIN']
    assert len(begins) == 7 and begins == [row[1:] for row in rows if row[0] == 'END']
    receipt = {
        'librarySHA256': hashlib.sha256(library.read_bytes()).hexdigest(),
        'executableSHA256': hashlib.sha256(executable.read_bytes()).hexdigest(),
        'assemblySHA256': hashlib.sha256(executable.with_suffix('.dll').read_bytes()).hexdigest(),
        'sourceSHA256': hashlib.sha256(Path(__file__).with_name('managed_allocation_trace.cpp').read_bytes()).hexdigest(),
        'processArchitecture': architecture[0],
        'traceSHA256': hashlib.sha256(traces[0].read_bytes()).hexdigest(),
        'objectSizes': [int(row[3]) for row in allocated],
        'balancedBrackets': len(begins),
        'emptyTags': [4, 5],
        'workerCounterBytes': [0, 64, 64],
        'recursiveCallbacks': 0,
        'stackHRESULTs': [0] * 4,
        'rejectedConfigurations': {'disabled': 132, 'missing-output': 132, 'output-error': 134},
    }
    (output / 'checks.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print('Managed allocation trace: one/two owner objects, a worker object, empty controls, stacks and cleanup passed.')


if __name__ == '__main__':
    main()
