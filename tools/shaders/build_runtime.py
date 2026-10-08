#!/usr/bin/env python3
"""Compile built-in shaders without depending on the runtime being built."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

import build_toolchain


ROOT = Path(__file__).resolve().parents[2]


def generate(output, source_root=ROOT):
    toolchain = build_toolchain.build()
    compiler = toolchain / 'bin' / ('glslangValidator' + build_toolchain.suffix)
    validator = toolchain / 'bin' / ('spirv-val' + build_toolchain.suffix)
    recipe = (toolchain / 'recipe.sha256').read_bytes() + Path(__file__).read_bytes()
    output = output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    with build_toolchain.exclusive(output.parent / (output.name + '.lock')):
        manifest = output / 'manifest.json'
        previous = json.loads(manifest.read_text()) if manifest.exists() else {}
        current = {}
        for domain in ('Rendering', 'Physics'):
            directory = source_root / f'src/Servers/{domain}/Shaders'
            sources = sorted([*directory.glob('*.hlsl'), *directory.glob('*.glsl')])
            if not sources:
                raise ValueError(f'Missing built-in shader sources: {directory}')
            # Hash the small complete include directory, including added/deleted files.
            digest = hashlib.sha256(recipe)
            for dependency in sorted(directory.rglob('*')):
                if dependency.is_file() and dependency.suffix != '.spv':
                    digest.update(dependency.relative_to(directory).as_posix().encode())
                    digest.update(dependency.read_bytes())
            for source in sources:
                if '.inc.' in source.name:
                    continue
                stage = source.stem.rsplit('.', 1)[-1]
                if stage not in ('vert', 'frag', 'comp'):
                    raise ValueError(f'Unknown built-in shader stage: {source}')
                name = f'{domain}/{source.stem}.spv'
                target = output / name
                fingerprint = digest.hexdigest()
                old = previous.get(name, {})
                if (old.get('source') == fingerprint and target.exists() and
                        old.get('binary') == hashlib.sha256(target.read_bytes()).hexdigest()):
                    current[name] = old
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                with tempfile.TemporaryDirectory(dir=target.parent) as temporary:
                    compiled = Path(temporary) / 'shader.spv'
                    command = [str(compiler), '-V', '--target-env', 'vulkan1.0', '-S', stage, '-e', 'main']
                    if source.suffix == '.hlsl':
                        command += ['-D', '--hlsl-iomap']
                    subprocess.run([*command, '-o', str(compiled), str(source)], check=True)
                    subprocess.run([str(validator), '--target-env', 'vulkan1.0', str(compiled)], check=True)
                    data = compiled.read_bytes()
                    if not target.exists() or target.read_bytes() != data:
                        os.replace(compiled, target)
                    current[name] = {'source': fingerprint, 'binary': hashlib.sha256(data).hexdigest()}
        for stale in output.glob('*/*.spv'):
            if stale.relative_to(output).as_posix() not in current:
                stale.unlink()
        content = json.dumps(current, indent=2) + '\n'
        if not manifest.exists() or manifest.read_text() != content:
            temporary = manifest.with_suffix('.tmp')
            temporary.write_text(content)
            os.replace(temporary, manifest)
    return current


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    generate(args.output)
