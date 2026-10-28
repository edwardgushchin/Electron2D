#!/usr/bin/env python3
"""Compare calibrated CLR allocation callbacks with bounded NativeAOT helper routes."""
import hashlib
import json
import os
from pathlib import Path
import platform
import resource
import signal
import subprocess
import sys
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if sys.platform != 'linux' or platform.machine() != 'x86_64':
        raise SystemExit('This diagnostic requires Linux x64 and the selected NativeAOT runtime.')
    library, calibration, clr, aot, output = [Path(x).resolve() for x in sys.argv[1:]]
    output.mkdir(parents=True, exist_ok=False)
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    root = Path(__file__).resolve().parents[3]
    runtime_lines = subprocess.check_output(['dotnet', '--list-runtimes'], text=True).splitlines()
    roots = {Path(line.split('[', 1)[1].removesuffix(']')).parents[1]
             for line in runtime_lines if line.startswith('Microsoft.NETCore.App 10.')}
    assert len(roots) == 1, 'Select one .NET 10 runtime root'
    dotnet_root = str(roots.pop())
    env = os.environ | {'LD_LIBRARY_PATH': str(library.parent) + ':' + os.environ.get('LD_LIBRARY_PATH', ''),
                        'DOTNET_ROOT': dotnet_root, 'DOTNET_ROOT_X64': dotnet_root}
    env.pop('E2D_ALLOCATION_TRACE', None)

    def run(label, command, changes, expected=0):
        selected = env | changes
        start = time.monotonic()
        mappings = set()
        with (output / (label + '.log')).open('w') as log:
            child = subprocess.Popen(command, env=selected, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
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
        receipt = {'command': command, 'environment': changes, 'exit': child.returncode,
                   'seconds': time.monotonic() - start, 'dotnetRoot': dotnet_root, 'mappings': sorted(mappings)}
        (output / (label + '.json')).write_text(json.dumps(receipt, indent=2) + '\n')
        assert child.returncode == expected, (label, child.returncode)
        return (output / (label + '.log')).read_text(), mappings

    run('calibration-driver', [sys.executable, '-B', str(root / 'tools/ci/test_managed_allocation_trace.py'),
                              str(library), str(calibration), str(output / 'calibration')], {})
    qualified = json.loads((output / 'calibration/checks.json').read_text())
    assert qualified['librarySHA256'] == digest(library)
    reports = {}
    for label, executable, traced, enabled in [('clr-plain', clr, False, False), ('clr-traced', clr, True, True),
                                               ('clr-disabled', clr, True, False), ('aot-routes', aot, False, False),
                                               ('aot-traced', aot, True, True)]:
        changes = {'CORECLR_ENABLE_PROFILING': '1' if enabled else '0',
                   'CORECLR_PROFILER': '{4CB0917C-5E03-4787-83BC-98C86DC7BE4C}',
                   'CORECLR_PROFILER_PATH': str(library), 'E2D_ALLOCATION_TRACE': str(output / (label + '.trace'))}
        rejected = label in ('clr-disabled', 'aot-traced')
        text, mappings = run(label, [str(executable)] + (['--trace'] if traced else []), changes, 132 if rejected else 0)
        assert mappings, label
        if executable == aot:
            assert not any('coreclr' in name.lower() for name in mappings)
        if rejected:
            assert not list(output.glob(label + '.trace.*')), label
        else:
            reports[label] = json.loads(text.splitlines()[-1])
    plain, traced, native = (reports[x] for x in ['clr-plain', 'clr-traced', 'aot-routes'])
    assert plain['DynamicCode'] and traced['DynamicCode'] and not native['DynamicCode']
    assert plain['Runtime'] == traced['Runtime'] == native['Runtime'], 'Use matching CLR and NativeAOT runtime versions'
    assert [(p['Name'], p['Objects'], p['ManagedBytes']) for p in plain['Phases']] == [
        (p['Name'], p['Objects'], p['ManagedBytes']) for p in traced['Phases']]
    files = list(output.glob('clr-traced.trace.*'))
    assert len(files) == 1
    rows = [line.split('\t') for line in files[0].read_text().splitlines()]
    assert rows[0] == ['INITIALIZE', '0'] and rows[-1] == ['SHUTDOWN', 'recursive:0']
    assert [r[1:] for r in rows if r[0] == 'BEGIN'] == [r[1:] for r in rows if r[0] == 'END']
    counts = []
    for tag, phase in enumerate(traced['Phases'], 1):
        allocations = [r for r in rows if r[0] == 'ALLOC' and int(r[1]) == tag]
        assert len(allocations) == phase['Objects'], (tag, len(allocations))
        assert all(r[6] == '0' for r in allocations)
        raw = sum(int(r[3]) for r in allocations)
        aligned = sum((int(r[3]) + 7) & ~7 for r in allocations)
        assert aligned == phase['ManagedBytes'], (tag, raw, aligned, phase)
        if tag == 1:
            assert all(int(r[5], 16) == traced['WitnessToken'] for r in allocations)
        counts.append({'phase': phase['Name'], 'objects': len(allocations), 'rawBytes': raw, 'alignedBytes': aligned})
    assert not [r for r in rows if r[0] == 'ALLOC' and r[1] == '9']
    assert native['Phases'][0]['FastEntries'] == native['ObjectCount']
    assert native['Phases'][0]['SlowEntries'] < native['ObjectCount']
    assert native['Phases'][2]['FastEntries'] == native['Phases'][2]['SlowEntries'] == 0
    summary = {'reports': reports, 'clrAllocationCallbacks': counts, 'profilerSHA256': digest(library),
               'clrAssemblySHA256': digest(clr.with_suffix('.dll')), 'aotExecutableSHA256': digest(aot),
               'calibration': qualified, 'sourceSHA256': {x.name: digest(x) for x in Path(__file__).parent.iterdir() if x.is_file()}}
    (output / 'checks.json').write_text(json.dumps(summary, indent=2) + '\n')
    print('Calibrated CLR object callbacks, NativeAOT fast/slow route gaps and profiler initialization controls passed.')


if __name__ == '__main__':
    main()
