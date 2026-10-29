#!/usr/bin/env python3
"""Qualify the copied CoreCLR reservation/frozen/native producer in bounded processes."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import platform
import resource
import shutil
import signal
import subprocess
import sys
import tarfile
import tempfile
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def patch_controls(workspace, output):
    script = Path(__file__).with_name('patch_runtime.py')
    files = ['src/coreclr/gc/gc.cpp', 'src/coreclr/vm/gchelpers.cpp', 'src/coreclr/vm/gcenv.ee.cpp',
             'src/coreclr/dlls/mscoree/coreclr/CMakeLists.txt', 'src/coreclr/dlls/mscoree/mscorwks_unixexports.src']
    assert digest(workspace / 'runtime.tar.gz') == '4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092'
    with tempfile.TemporaryDirectory(prefix='clr-patch-', dir=workspace) as temp:
        root = Path(temp)
        with tarfile.open(workspace / 'runtime.tar.gz') as archive:
            for file in files:
                path = root / file
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(archive.extractfile('runtime-10.0.1/' + file).read())
        target = root / files[1]
        original = target.read_bytes()
        target.write_bytes(original + b'// wrong pin\n')
        before = {file: digest(root / file) for file in files}
        for flags in [[], ['-O']]:
            child = subprocess.run([sys.executable, *flags, '-B', str(script), str(root), str(root / 'bad.patch')],
                                   capture_output=True, text=True)
            assert child.returncode != 0 and 'unmodified v10.0.1' in child.stderr
            assert before == {file: digest(root / file) for file in files} and not (root / 'bad.patch').exists()
        target.write_bytes(original)
        subprocess.run([sys.executable, '-B', str(script), str(root), str(root / 'good.patch')], check=True, capture_output=True)
        for file in files:
            assert (root / file).read_bytes() == (workspace / 'runtime-10.0.1' / file).read_bytes(), file
        applied = {file: digest(root / file) for file in files}
        child = subprocess.run([sys.executable, '-B', str(script), str(root), str(root / 'again.patch')], capture_output=True)
        assert child.returncode != 0 and applied == {file: digest(root / file) for file in files}
        (output / 'patch-controls.json').write_text(json.dumps({'pinsRejectedBeforeMutation': True,
            'optimizedPinsRejected': True, 'reapplicationRejected': True, 'instrumentedSources': applied}, indent=2) + '\n')


def main():
    if not __debug__:
        raise SystemExit('Run this checker without Python optimization.')
    if sys.platform != 'linux' or platform.machine() != 'x86_64':
        raise SystemExit('This diagnostic requires Linux x64 / CoreCLR v10.0.1 source.')
    workspace, output = [Path(arg).resolve() for arg in sys.argv[1:]]
    output.mkdir(parents=True, exist_ok=False)
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    patch_controls(workspace, output)
    shared = module('shared_report', Path(__file__).parent.parent / 'aot-memory-probe/check.py')
    live = module('profiler_abi', Path(__file__).parent.parent / 'managed-live-probe/check.py')
    abi = live.check_abi(workspace / 'runtime-10.0.1/src/coreclr/inc/corprof.idl')
    roots = {Path(line.split('[', 1)[1].removesuffix(']')).parents[1]
             for line in subprocess.check_output(['dotnet', '--list-runtimes'], text=True).splitlines()
             if line.startswith('Microsoft.NETCore.App 10.0.1 [')}
    assert len(roots) == 1, 'Select the qualified .NET 10.0.1 SDK root'
    installed = roots.pop()
    source_root = workspace / 'dotnet'
    baseline = Path(tempfile.mkdtemp(prefix='baseline-dotnet-', dir=workspace))
    baseline_shared = baseline / 'shared/Microsoft.NETCore.App/10.0.1'
    baseline_shared.mkdir(parents=True, exist_ok=False)
    shutil.copy2(source_root / 'dotnet', baseline / 'dotnet')
    (baseline / 'host').symlink_to(source_root / 'host', target_is_directory=True)
    for path in (source_root / 'shared/Microsoft.NETCore.App/10.0.1').iterdir():
        if path.name == 'libcoreclr.so':
            shutil.copy2(workspace / 'unmodified-coreclr.so', baseline_shared / path.name)
        else:
            (baseline_shared / path.name).symlink_to(path)
    app = workspace / 'managed/bin/CLRMemoryProbe/release/CLRMemoryProbe'
    cases = [('workstation', [], {}, 0), ('server', [], {'DOTNET_gcServer': '1'}, 0),
             ('source-baseline', [], {'DOTNET_ROOT': str(baseline)}, 132),
             ('sdk-baseline', [], {'DOTNET_ROOT': str(installed)}, 132),
             ('disabled', [], {'CORECLR_ENABLE_PROFILING': '0'}, 132),
             ('concurrent', [], {'DOTNET_gcConcurrent': '1'}, 135),
             ('invalid-watch', ['--invalid-watch'], {}, 132),
             ('invalid-snapshot', ['--invalid-snapshot'], {}, 132),
             ('invalid-frozen', ['--invalid-frozen'], {}, 132),
             ('invalid-native', ['--invalid-native'], {}, 132),
             ('deferred-common', [], {}, -signal.SIGABRT),
             ('common-oom', ['--common-oom'], {'DOTNET_GCHeapHardLimit': '8000000'}, 0),
             ('server-common-oom', ['--common-oom'], {'DOTNET_GCHeapHardLimit': '8000000', 'DOTNET_gcServer': '1'}, 0)]
    reports = {}
    for label, arguments, changes, expected in cases:
        directory = workspace / ('deferred' if label == 'deferred-common' else 'concurrent' if label == 'concurrent' else '.')
        producer = directory / 'libElectron2DAOTMemory.so'
        env = os.environ | {'CORECLR_ENABLE_PROFILING': '1',
            'CORECLR_PROFILER': '{4CB0917C-5E03-4787-83BC-98C86DC7BE4C}', 'CORECLR_PROFILER_PATH': str(producer),
            'DOTNET_ROOT': str(source_root), 'DOTNET_PROCESSOR_COUNT': '2', 'DOTNET_GCHeapCount': '2',
            'DOTNET_gcServer': '0', 'DOTNET_gcConcurrent': '0', 'LD_LIBRARY_PATH': str(directory) + ':' + str(workspace)} | changes
        env['DOTNET_ROOT_X64'] = env['DOTNET_ROOT']
        mappings = set()
        command = [str(app), *arguments]
        started = time.monotonic()
        with (output / (label + '.log')).open('w') as log:
            child = subprocess.Popen(command, env=env, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
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
        (output / (label + '.json')).write_text(json.dumps({'command': command, 'exit': child.returncode,
            'seconds': time.monotonic() - started, 'environment': {key: env[key] for key in ['CORECLR_ENABLE_PROFILING', 'CORECLR_PROFILER', 'CORECLR_PROFILER_PATH', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_PROCESSOR_COUNT', 'DOTNET_GCHeapCount', 'DOTNET_gcServer', 'DOTNET_gcConcurrent', 'DOTNET_GCHeapHardLimit', 'LD_LIBRARY_PATH'] if key in env},
            'mappings': sorted(mappings), 'producerSHA256': digest(producer)}, indent=2) + '\n')
        assert child.returncode == expected, (label, child.returncode)
        text = (output / (label + '.log')).read_text()
        if expected:
            reason = ('Delayed common peak missed reservation' if label == 'deferred-common' else
                      'hooks are absent' if 'baseline' in label else 'uninitialized producer' if label == 'disabled' else
                      'non-concurrent GC' if label == 'concurrent' else 'invalid')
            assert reason in text, (label, text[-1000:])
            continue
        assert {path for path in mappings if path.endswith('/libcoreclr.so')} == {str(source_root / 'shared/Microsoft.NETCore.App/10.0.1/libcoreclr.so')}
        assert {path for path in mappings if path.endswith('/libElectron2DENet.so')} == {str(workspace / 'libElectron2DENet.so')}
        assert str(producer) in mappings
        report = json.loads(text.splitlines()[-1])
        if 'common-oom' in label:
            assert report['Runtime'] == '.NET 10.0.1-dev' and report['ServerGC'] == (label == 'server-common-oom')
            assert 0 < report['HeapLimit'] <= 134217728 and report['CollectionsAfter'] > report['CollectionsBefore']
            assert report['Failed'] == 1 and report['Reverted'] == report['TaggedCurrent'] == report['TaggedAllocated'] == 0
        else:
            shared.check_report(report, label == 'server', True, '.NET 10.0.1-dev')
            reports[label] = report
    runtime = source_root / 'shared/Microsoft.NETCore.App/10.0.1'
    (output / 'checks.json').write_text(json.dumps({'reports': reports, 'abi': abi,
        'runtimeSHA256': {name: digest(runtime / name) for name in ['libcoreclr.so', 'libclrjit.so', 'System.Private.CoreLib.dll']},
        'memorySHA256': digest(workspace / 'libElectron2DENet.so'),
        'sources': {str(p): digest(p) for folder in [Path(__file__).parent, Path(__file__).parent.parent / 'aot-memory-probe', Path(__file__).parent.parent / 'managed-live-probe'] for p in folder.iterdir() if p.is_file()}}, indent=2) + '\n')
    print('CoreCLR reservation, frozen/image lifetime, native composition, delayed RED and bounded OOM controls passed.')


if __name__ == '__main__':
    main()
