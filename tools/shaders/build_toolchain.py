#!/usr/bin/env python3
"""Build the pinned host shader tools; no shader compiler is installed globally."""
from contextlib import contextmanager
import errno
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tarfile
import tempfile
import urllib.request


root = Path(__file__).resolve().parent
system = {'Linux': 'linux', 'Darwin': 'osx', 'Windows': 'win'}.get(platform.system())
architecture = {'x86_64': 'x64', 'amd64': 'x64', 'aarch64': 'arm64', 'arm64': 'arm64'}.get(platform.machine().lower())
if not system or not architecture:
    raise SystemExit('Shader compilation requires a Linux, macOS or Windows x64/arm64 build host.')
host = f'{system}-{architecture}'
suffix = '.exe' if system == 'win' else ''


@contextmanager
def exclusive(path):
    """Serialize parallel builds without retaining a lock after a crashed process."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('a+b') as handle:
        if os.name == 'nt':
            import msvcrt
            if handle.tell() == 0:
                handle.write(b'\0'); handle.flush()
            handle.seek(0)
            # LK_LOCK has a ten-second retry limit; native bootstrap can take longer.
            import time
            while True:
                try:
                    msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
                    break
                except OSError as error:
                    if error.errno not in (errno.EACCES, errno.EDEADLK):
                        raise
                    time.sleep(0.1)
        else:
            import fcntl
            fcntl.flock(handle, fcntl.LOCK_EX)
        try:
            yield
        finally:
            if os.name == 'nt':
                handle.seek(0)
                msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)


def build():
    with exclusive(root / f'obj/toolchain-build/{host}.lock'):
        return build_locked()


def build_locked():
    lock = (root / 'toolchain.lock.json').read_bytes()
    output = root / f'obj/toolchain/{host}'
    recipe = hashlib.sha256(lock + Path(__file__).read_bytes()).hexdigest()
    receipt = output / 'recipe.sha256'
    if (receipt.exists() and receipt.read_text() == recipe and
            all((output / 'bin' / (name + suffix)).is_file() for name in ('glslangValidator', 'spirv-val'))):
        return output
    build_sources(lock, output)
    receipt.write_text(recipe)
    return output


def build_sources(lock, output):
    sources = json.loads(lock)
    work = root / f'obj/toolchain-build/{host}'
    work.mkdir(parents=True, exist_ok=True)
    source = work / 'source'
    stamp = work / 'sources.json'
    if not stamp.exists() or stamp.read_bytes() != lock or not (source / 'CMakeLists.txt').exists():
        with tempfile.TemporaryDirectory(dir=work) as temporary:
            staging = Path(temporary)
            for entry in sources:
                archive = work / (entry['name'] + '.tar.gz')
                if not archive.exists():
                    with urllib.request.urlopen(entry['url'], timeout=60) as response:
                        data = response.read()
                    if hashlib.sha256(data).hexdigest() != entry['sha256']:
                        raise SystemExit(f"Archive checksum mismatch: {entry['name']}")
                    archive.write_bytes(data)
                if hashlib.sha256(archive.read_bytes()).hexdigest() != entry['sha256']:
                    raise SystemExit(f'Archive checksum mismatch: {archive}')
                unpack = staging / entry['name']
                with tarfile.open(archive) as tar:
                    tar.extractall(unpack, filter='data')
                extracted, = unpack.iterdir()
                destination = staging / 'source' / entry['directory']
                # The parent archives contain empty dependency directories.
                if destination.exists():
                    shutil.rmtree(destination)
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.move(extracted, destination)
            if source.exists():
                shutil.rmtree(source)
            shutil.move(staging / 'source', source)
            stamp.write_bytes(lock)
    subprocess.run(['cmake', '-S', str(source), '-B', str(work / 'build'), '-G', 'Ninja',
                    '-DCMAKE_BUILD_TYPE=Release', '-DBUILD_SHARED_LIBS=OFF', '-DGLSLANG_TESTS=OFF',
                    '-DSPIRV_SKIP_TESTS=ON', '-DENABLE_HLSL=ON', '-DENABLE_OPT=ON',
                    '-DCMAKE_EXE_LINKER_FLAGS='], check=True)
    build_environment = dict(os.environ, FORCED_BUILD_VERSION_DESCRIPTION=next(
        entry['commit'] for entry in sources if entry['name'] == 'SPIRV-Tools'))
    # Archive builds must report the upstream revision, not an enclosing engine Git checkout.
    subprocess.run([sys.executable, str(source / 'External/spirv-tools/utils/update_build_version.py'),
                    str(source / 'External/spirv-tools/CHANGES'), str(work / 'build/External/spirv-tools/build-version.inc')],
                   env=build_environment, check=True)
    subprocess.run(['cmake', '--build', str(work / 'build'), '--parallel', str(min(4, os.cpu_count() or 1)),
                    '--target', 'glslang-standalone', 'spirv-val'], env=build_environment, check=True)
    (output / 'bin').mkdir(parents=True, exist_ok=True)
    for name, built in [('glslangValidator', 'StandAlone/glslang'),
                        ('spirv-val', 'External/spirv-tools/tools/spirv-val')]:
        shutil.copy2(work / 'build' / (built + suffix), output / 'bin' / (name + suffix))
    for entry in sources:
        license_directory = output / 'licence'
        license_directory.mkdir(parents=True, exist_ok=True)
        upstream = source / entry['directory']
        for candidate in upstream.glob('LICENSE*'):
            for license_file in (candidate.rglob('*') if candidate.is_dir() else (candidate,)):
                if license_file.is_file():
                    name = entry['name'] + '-' + '-'.join(license_file.relative_to(upstream).parts)
                    shutil.copy2(license_file, license_directory / name)
    (output / 'toolchain.lock.json').write_bytes(lock)
    print(f'Shader import toolchain: {output}')


if __name__ == "__main__":
    print(build())
