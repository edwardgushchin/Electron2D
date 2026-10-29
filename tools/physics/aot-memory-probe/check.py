#!/usr/bin/env python3
"""Qualify source-instrumented NativeAOT allocation/GC lifetime in bounded child processes."""
import hashlib
import json
import os
from pathlib import Path
import platform
import resource
import signal
import subprocess
import sys
import tarfile
import tempfile
import time


ARCHIVE_SHA256 = '4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def patch_controls(workspace, output):
    script = Path(__file__).with_name('patch_runtime.py')
    files = ['src/coreclr/runtime/amd64/AllocFast.S', 'src/coreclr/nativeaot/Runtime/GCHelpers.cpp',
             'src/coreclr/nativeaot/Runtime/gcenv.ee.cpp', 'src/coreclr/gc/gc.cpp']
    assert digest(workspace / 'runtime.tar.gz') == ARCHIVE_SHA256
    with tempfile.TemporaryDirectory(prefix='patch-control-', dir=workspace) as temporary:
        root = Path(temporary)
        with tarfile.open(workspace / 'runtime.tar.gz') as archive:
            for name in files:
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(archive.extractfile('runtime-10.0.1/' + name).read())
        original = {name: (root / name).read_bytes() for name in files}
        target = root / files[1]
        target.write_bytes(original[files[1]] + b'// invalid pin\n')
        before = {name: digest(root / name) for name in files}
        rejected = subprocess.run([sys.executable, '-B', str(script), str(root), str(root / 'bad.patch')],
                                  capture_output=True, text=True)
        assert rejected.returncode != 0 and 'unmodified v10.0.1' in rejected.stderr
        assert before == {name: digest(root / name) for name in files} and not (root / 'bad.patch').exists()
        optimized = subprocess.run([sys.executable, '-O', '-B', str(script), str(root), str(root / 'optimized.patch')],
                                   capture_output=True, text=True)
        assert optimized.returncode != 0 and 'unmodified v10.0.1' in optimized.stderr
        assert before == {name: digest(root / name) for name in files} and not (root / 'optimized.patch').exists()
        target.write_bytes(original[files[1]])
        accepted = subprocess.run([sys.executable, '-B', str(script), str(root), str(root / 'good.patch')],
                                  capture_output=True, text=True)
        assert accepted.returncode == 0, accepted.stderr
        for name in files:
            assert (root / name).read_bytes() == (workspace / 'runtime-10.0.1' / name).read_bytes(), name
        applied = {name: digest(root / name) for name in files}
        repeated = subprocess.run([sys.executable, '-B', str(script), str(root), str(root / 'again.patch')],
                                  capture_output=True, text=True)
        assert repeated.returncode != 0 and applied == {name: digest(root / name) for name in files}
        receipt = {'archiveSHA256': ARCHIVE_SHA256, 'mismatchRejectedBeforeMutation': True,
                   'optimizedMismatchRejected': True, 'reapplicationRejected': True, 'instrumentedSources': applied}
        (output / 'patch-controls.json').write_text(json.dumps(receipt, indent=2) + '\n')


def main():
    if not __debug__:
        raise SystemExit('Run this checker without Python optimization.')
    if sys.platform != 'linux' or platform.machine() != 'x86_64':
        raise SystemExit('This diagnostic requires Linux x64 and NativeAOT 10.0.1.')
    workspace, output = [Path(arg).resolve() for arg in sys.argv[1:]]
    output.mkdir(parents=True, exist_ok=False)
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    patch_controls(workspace, output)
    native = workspace / 'runtime-10.0.1/artifacts/obj/coreclr/linux.x64.Release/nativeaot/Runtime/Full'
    reports = {}
    runtime_inputs = {}
    for name in ['probe', 'server', 'source-baseline', 'sdk-baseline', 'concurrent', 'deferred-common']:
        artifact = 'managed' if name == 'probe' else 'managed-' + name
        link = workspace / artifact / 'obj/AOTMemoryProbe/release_linux-x64/native/e2d-runtime-inputs.txt'
        paths = [Path(line.strip()) for line in link.read_text().splitlines()
                 if line.strip().endswith(('WorkstationGC.a', 'ServerGC.a'))]
        assert len(paths) == 1, (name, paths)
        if name == 'source-baseline':
            assert paths[0] == workspace / 'unmodified.WorkstationGC.a'
        elif name == 'sdk-baseline':
            assert '/microsoft.netcore.app.runtime.nativeaot.linux-x64/10.0.1/' in str(paths[0])
        else:
            assert paths[0] == native / ('libRuntime.ServerGC.a' if name == 'server' else 'libRuntime.WorkstationGC.a')
        runtime_inputs[name] = {'path': str(paths[0]), 'sha256': digest(paths[0]), 'linkArguments': link.read_text().splitlines()}
    environment = os.environ | {'DOTNET_PROCESSOR_COUNT': '2', 'DOTNET_GCHeapCount': '2'}
    cases = [('workstation', 'probe', [], 0), ('server', 'server', [], 0),
             ('source-baseline', 'source-baseline', [], 132), ('sdk-baseline', 'sdk-baseline', [], 132),
             ('concurrent', 'concurrent', [], 135), ('invalid-watch', 'probe', ['--invalid-watch'], 132),
             ('invalid-snapshot', 'probe', ['--invalid-snapshot'], 132),
             ('invalid-frozen', 'probe', ['--invalid-frozen'], 132),
             ('invalid-native', 'probe', ['--invalid-native'], 132),
             ('deferred-common', 'deferred-common', [], -signal.SIGABRT),
             ('common-oom', 'probe', ['--common-oom'], 0),
             ('server-common-oom', 'server', ['--common-oom'], 0)]
    for label, image, arguments, expected in cases:
        executable = workspace / image / 'AOTMemoryProbe'
        command = [str(executable), *arguments]
        started = time.monotonic()
        mappings = set()
        memory_library = executable.parent / 'runtimes/linux-x64/native/libElectron2DENet.so'
        child_environment = environment | {'LD_LIBRARY_PATH': str(memory_library.parent)} | ({'DOTNET_GCHeapHardLimit': '8000000'} if 'common-oom' in label else {})
        with (output / (label + '.log')).open('w') as log:
            child = subprocess.Popen(command, env=child_environment, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
            try:
                while child.poll() is None:
                    try:
                        mappings.update(line.split()[-1] for line in Path(f'/proc/{child.pid}/maps').read_text().splitlines() if '/' in line)
                    except OSError:
                        pass
                    if time.monotonic() - started > 90:
                        raise TimeoutError(label)
                    time.sleep(.001)
            except BaseException:
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                child.wait()
                raise
        receipt = {'command': command, 'exit': child.returncode, 'seconds': time.monotonic() - started,
                   'GCHeapHardLimit': child_environment.get('DOTNET_GCHeapHardLimit'),
                   'memoryLibrarySHA256': digest(memory_library),
                   'executableSHA256': digest(executable), 'mappings': sorted(mappings)}
        (output / (label + '.json')).write_text(json.dumps(receipt, indent=2) + '\n')
        assert child.returncode == expected, (label, child.returncode)
        text = (output / (label + '.log')).read_text()
        if label == 'deferred-common':
            assert 'Delayed common peak missed reservation' in text, label
            continue
        if expected:
            reason = ('hooks are absent' if 'baseline' in label else 'stopped EE and non-concurrent GC' if label == 'concurrent'
                      else 'invalid type watch' if label == 'invalid-watch' else 'invalid frozen resize' if label == 'invalid-frozen'
                      else 'invalid native allocation' if label == 'invalid-native' else 'invalid snapshot')
            assert ('aot-memory-producer: ' in text or 'aot-frozen-fixture: ' in text) and reason in text, label
            continue
        assert str(executable) in mappings and not any('libcoreclr' in path.lower() for path in mappings)
        assert {path for path in mappings if path.endswith('/libElectron2DENet.so')} == {str(memory_library)}, label
        report = json.loads(text.splitlines()[-1])
        if 'common-oom' in label:
            assert report['Runtime'] == '.NET 10.0.1' and report['ServerGC'] == (image == 'server')
            assert 0 < report['HeapLimit'] <= 134217728 and report['CollectionsAfter'] > report['CollectionsBefore']
            assert report['Failed'] == 1 and report['Reverted'] == report['TaggedCurrent'] == report['TaggedAllocated'] == 0
            continue
        assert report['Runtime'] == '.NET 10.0.1' and not report['DynamicCode']
        assert report['ServerGC'] == (label == 'server') and report['ReadBytes'] == 0
        assert report['Allocated'] - report['Retired'] == report['Current'] and report['Peak'] >= report['Current']
        assert report['ConcurrentCollections'] > 0 and report['FrozenCount'] > 0 and report['FrozenBytes'] > 0
        assert report['MidpointCollection'], 'The mutator midpoint must contain a collection before either worker completes'
        assert len(report['Rows']) == 29
        assert all(row['Allocated'] - row['Retired'] == row['Current'] for row in report['Rows'])
        hidden = next(row for row in report['Rows'] if row['Phase'] == 'peak-between-reads')
        rejected = next(row for row in report['Rows'] if row['Phase'] == 'rejected-array')
        assert hidden['Current'] == 0 and hidden['Peak'] == 12288
        assert rejected['Current'] == rejected['Peak'] == rejected['Allocated'] == 0
        failed = next(row for row in report['Rows'] if row['Phase'] == 'injected-failure')
        retry = next(row for row in report['Rows'] if row['Phase'] == 'retry-retired')
        assert failed['Current'] == failed['Peak'] == failed['Allocated'] == 0 and failed['Failures'] == 1
        assert retry['Current'] == 0 and retry['Allocated'] == retry['Retired'] == 123480 and retry['Failures'] == 1
        frozen = report['Frozen']
        assert frozen['StartupObserved'] and frozen['NoGCTransitions'] and frozen['MidpointCollection']
        assert frozen['ReadBytes'] == 0 and frozen['InterleavedCollections'] > 0 and len(frozen['Rows']) == 13
        assert all(row['Allocated'] - row['Retired'] == row['Current'] and
                   row['ManagedCurrent'] == row['Current'] + row['DynamicCurrent'] for row in frozen['Rows'])
        phases = {row['Phase']: row for row in frozen['Rows']}
        base = phases['startup']
        assert phases['registration-failure']['Failures'] == base['Failures'] + 1
        assert phases['grow-five']['Current'] == base['Current'] + 120
        assert phases['unregistered']['Current'] == base['Current'] and phases['unregistered']['Segments'] == base['Segments']
        assert phases['concurrent-lifetime']['Allocated'] == base['Allocated'] + 120 + 7680
        assert phases['unobserved-frozen-peak']['Current'] == base['Current']
        assert phases['unobserved-frozen-peak']['Peak'] == base['Current'] + 2400000
        assert phases['disjoint-peak']['ManagedPeak'] < phases['disjoint-peak']['Peak'] + phases['disjoint-peak']['DynamicPeak']
        process = report['Process']
        assert process['MidpointCollection'] and process['InterleavedCollections'] > 0 and process['ReadBytes'] == 0
        assert len(process['Rows']) == 14
        assert all(row['Current'] == row['ManagedCurrent'] + row['NativeCurrent'] and row['Peak'] >= row['Current'] and
                   row['Allocated'] - row['Retired'] == row['NativeCurrent'] for row in process['Rows'])
        native = {row['Phase']: row for row in process['Rows']}
        assert [native[name]['NativeCurrent'] for name in ['allocated', 'grown', 'shrunk', 'zero-size']] == [73, 4097, 7, 0]
        assert native['worker-retired']['Allocated'] == native['failed-allocations']['Allocated'] + 5056
        assert native['worker-retired']['Retired'] == native['worker-retired']['Allocated']
        assert native['unobserved-native-peak']['NativePeak'] == 7000000
        assert native['unobserved-native-peak']['NativeCurrent'] == 0
        assert native['disjoint-process-peak']['Peak'] < native['disjoint-process-peak']['ManagedPeak'] + native['disjoint-process-peak']['NativePeak']
        assert native['wide-allocation']['NativeCurrent'] == (1 << 32) + 17
        assert native['wide-resize']['NativeCurrent'] == (1 << 32) + 33
        assert native['wide-retired']['NativeCurrent'] == 0 and native['wide-retired']['Peak'] >= native['wide-resize']['Peak']
        common = report['Common']
        assert common['ReadBytes'] == 0 and common['ExpectedDelayedPeak'] == common['ObservedDelayedPeak']
        assert common['ObservedDelayedPeak'] > 5 << 30 and len(common['Rows']) == 7
        assert all(row['Pending'] == 0 and row['Attempts'] == row['Completed'] + row['Failed'] and
                   row['SOH'] + row['FreeList'] + row['Segment'] == row['Completed'] + row['Reverted'] for row in common['Rows'])
        phases = {row['Phase']: row for row in common['Rows']}
        assert phases['free-list-reuse']['FreeList'] > phases['startup']['FreeList']
        assert phases['finalizer-failure']['Failed'] == phases['delayed-completion']['Failed'] + 1
        assert phases['finalizer-failure']['Reverted'] == phases['delayed-completion']['Reverted'] + 1
        reports[label] = report
    summary = {'reports': reports, 'runtimeInputs': runtime_inputs, 'bridgeSHA256': digest(workspace / 'producer.o'),
               'deferredBridgeSHA256': digest(workspace / 'producer-deferred.o'),
               'frozenFixtureSHA256': digest(workspace / 'frozen-fixture.o'),
               'memoryLibrarySHA256': digest(workspace / 'native/libElectron2DENet.so'),
               'sourceSHA256': {path.name: digest(path) for path in Path(__file__).parent.iterdir() if path.is_file()}}
    (output / 'checks.json').write_text(json.dumps(summary, indent=2) + '\n')
    print('NativeAOT raw common/fast/frozen/native commits, delayed-peak RED control, bounded OOM and lifecycle witnesses passed.')


if __name__ == '__main__':
    main()
