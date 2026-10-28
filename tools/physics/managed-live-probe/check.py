#!/usr/bin/env python3
"""Qualify the Linux x64 CoreCLR dynamic-heap callback ledger in isolated children."""
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import resource
import signal
import subprocess
import sys
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_abi(path):
    assert digest(path) == '92a2457e24291130b85970e23129a115a6044da2b87e121924791475ee036c99', 'Use v10.0.1 corprof.idl'
    source = re.sub(r'/\*.*?\*/|//[^\n]*', '', path.read_text(), flags=re.S)
    actual = {}
    for stem in ['ICorProfilerCallback', 'ICorProfilerInfo']:
        slot = 3
        for suffix in ['', '2', '3', '4']:
            body = re.search(r'interface ' + stem + suffix + r'\s*:\s*\w+\s*\{(.*?)\n\}', source, re.S)[1]
            for name in re.findall(r'HRESULT\s+(\w+)\s*\(', body):
                actual[name] = slot
                slot += 1
    expected = {'Initialize': 3, 'Shutdown': 4, 'ObjectAllocated': 50, 'GarbageCollectionStarted': 73,
                'GarbageCollectionFinished': 75, 'MovedReferences2': 87, 'SurvivingReferences2': 88,
                'SetEventMask': 16, 'GetObjectGeneration': 55, 'GetObjectSize2': 80}
    assert {key: actual[key] for key in expected} == expected
    return expected


def main():
    if sys.platform != 'linux' or platform.machine() != 'x86_64':
        raise SystemExit('This diagnostic requires Linux x64 / CoreCLR 10.0.1.')
    library, app, idl, output = [Path(arg).resolve() for arg in sys.argv[1:]]
    abi = check_abi(idl)
    output.mkdir(parents=True, exist_ok=False)
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    roots = {Path(line.split('[', 1)[1].removesuffix(']')).parents[1]
             for line in subprocess.check_output(['dotnet', '--list-runtimes'], text=True).splitlines()
             if line.startswith('Microsoft.NETCore.App 10.0.1 [')}
    assert len(roots) == 1, 'Select the qualified .NET 10.0.1 runtime root'
    runtime_root = str(roots.pop())
    env = os.environ | {'CORECLR_ENABLE_PROFILING': '1',
                        'CORECLR_PROFILER': '{4CB0917C-5E03-4787-83BC-98C86DC7BE4C}',
                        'CORECLR_PROFILER_PATH': str(library), 'DOTNET_ROOT': runtime_root,
                        'DOTNET_ROOT_X64': runtime_root, 'DOTNET_PROCESSOR_COUNT': '2',
                        'DOTNET_GCHeapCount': '2'}
    reports = {}
    cases = [('workstation', [], {'DOTNET_gcServer': '0'}, 0),
             ('server', [], {'DOTNET_gcServer': '1'}, 0),
             ('disabled', [], {'CORECLR_ENABLE_PROFILING': '0'}, 132),
             ('invalid-tag', ['--invalid-tag'], {}, 132),
             ('invalid-snapshot', ['--invalid-snapshot'], {}, 132)]
    for label, arguments, changes, expected in cases:
        command = [str(app), *arguments]
        start = time.monotonic()
        mappings = set()
        with (output / (label + '.log')).open('w') as log:
            child = subprocess.Popen(command, env=env | changes, stdout=log, stderr=subprocess.STDOUT,
                                     start_new_session=True)
            try:
                while child.poll() is None:
                    try:
                        mappings.update(line.split()[-1] for line in Path(f'/proc/{child.pid}/maps').read_text().splitlines() if '/' in line)
                    except OSError:
                        pass
                    if time.monotonic() - start > 90:
                        raise TimeoutError(label)
                    time.sleep(.001)
            except BaseException:
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                child.wait()
                raise
        receipt = {'command': command, 'exit': child.returncode, 'seconds': time.monotonic() - start,
                   'runtimeRoot': runtime_root, 'environment': changes, 'mappings': sorted(mappings)}
        (output / (label + '.json')).write_text(json.dumps(receipt, indent=2) + '\n')
        assert child.returncode == expected, (label, child.returncode)
        text = (output / (label + '.log')).read_text()
        if expected:
            assert 'live-memory-producer: uninitialized producer or invalid' in text, label
            continue
        assert str(library) in mappings and any('/10.0.1/libcoreclr.so' in x for x in mappings), label
        report = json.loads(text.splitlines()[-1])
        assert report['Runtime'] == '.NET 10.0.1' and report['ServerGC'] == (label == 'server')
        assert report['ReadBytes'] == 0 and report['ConcurrentCollections'] > 0
        assert report['GlobalAllocated'] - report['GlobalRetired'] == report['GlobalCurrent']
        assert report['GlobalPeak'] >= report['GlobalCurrent'] and report['GlobalMoved'] > 0
        assert len(report['Reports']) == 25
        assert all(row['Allocated'] - row['Retired'] == row['Current'] for row in report['Reports'])
        hidden = next(row for row in report['Reports'] if row['Phase'] == 'peak-between-reads')
        assert hidden['Current'] == 0 and hidden['Peak'] == 12288
        reports[label] = report
    summary = {'reports': reports, 'abi': abi, 'corprofIDL': digest(idl), 'librarySHA256': digest(library),
               'assemblySHA256': digest(app.with_suffix('.dll')),
               'sourceSHA256': {path.name: digest(path) for path in Path(__file__).parent.iterdir() if path.is_file()}}
    (output / 'checks.json').write_text(json.dumps(summary, indent=2) + '\n')
    print('CLR dynamic-heap GC retirement, compaction, pinning, finalization, worker and peak controls passed.')


if __name__ == '__main__':
    main()
