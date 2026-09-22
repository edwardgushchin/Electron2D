#!/usr/bin/env python3
"""Build the pinned Linux x64 import tools; no shader compiler is installed globally."""
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
if platform.system() != 'Linux' or platform.machine() != 'x86_64':
    raise SystemExit('The packaged shader import toolchain currently targets Linux x64.')
lock = (root / 'toolchain.lock.json').read_bytes()
sources = json.loads(lock)
work = root / 'obj/toolchain-build/linux-x64'
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
                '-DSPIRV_SKIP_TESTS=ON', '-DENABLE_HLSL=OFF', '-DENABLE_OPT=ON',
                '-DCMAKE_EXE_LINKER_FLAGS='], check=True)
build_environment = dict(os.environ, FORCED_BUILD_VERSION_DESCRIPTION=next(
    entry['commit'] for entry in sources if entry['name'] == 'SPIRV-Tools'))
# Archive builds must report the upstream revision, not an enclosing engine Git checkout.
subprocess.run([sys.executable, str(source / 'External/spirv-tools/utils/update_build_version.py'),
                str(source / 'External/spirv-tools/CHANGES'), str(work / 'build/External/spirv-tools/build-version.inc')],
               env=build_environment, check=True)
subprocess.run(['cmake', '--build', str(work / 'build'), '--parallel', str(min(4, os.cpu_count() or 1)),
                '--target', 'glslang-standalone', 'spirv-val'], env=build_environment, check=True)
output = root / 'obj/toolchain/linux-x64'
(output / 'bin').mkdir(parents=True, exist_ok=True)
for name, built in [('glslangValidator', 'StandAlone/glslang'),
                    ('spirv-val', 'External/spirv-tools/tools/spirv-val')]:
    shutil.copy2(work / 'build' / built, output / 'bin' / name)
for entry in sources:
    license_directory = output / 'licenses' / entry['name']
    license_directory.mkdir(parents=True, exist_ok=True)
    for license in (source / entry['directory']).glob('LICENSE*'):
        if license.is_file():
            shutil.copy2(license, license_directory / license.name)
        elif license.is_dir():
            shutil.copytree(license, license_directory / license.name, dirs_exist_ok=True)
(output / 'toolchain.lock.json').write_bytes(lock)
print(f'Shader import toolchain: {output}')
