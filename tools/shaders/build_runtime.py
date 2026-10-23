#!/usr/bin/env python3
"""Compile built-in shaders without depending on the runtime being built."""
import argparse
import hashlib
import json
import itertools
import os
from pathlib import Path
import subprocess
import tempfile

import build_toolchain
import material_lighting


ROOT = Path(__file__).resolve().parents[2]
VULKAN_OPTIMIZED_PHYSICS = (
    'PhysicsResidentCCD.comp.glsl', 'PhysicsResidentShapeQueries.comp.glsl',
    'PhysicsResidentMotionQueries.comp.glsl')


def generate(output, source_root=ROOT):
    toolchain = build_toolchain.build()
    compiler = toolchain / 'bin' / ('glslangValidator' + build_toolchain.suffix)
    validator = toolchain / 'bin' / ('spirv-val' + build_toolchain.suffix)
    recipe = (toolchain / 'recipe.sha256').read_bytes() + Path(__file__).read_bytes() + Path(material_lighting.__file__).read_bytes()
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
            programs = []
            for source in sources:
                if '.inc.' in source.name:
                    continue
                variants = [(source.stem, [])]
                if source.name == 'PhysicsResidentShapeQueries.comp.glsl':
                    variants = [(f'PhysicsResidentShapeQueries{mode}.comp', [f'-DE2D_SHAPE_QUERY_MODE={mode}u']) for mode in range(4)]
                for stem, defines in variants:
                    programs.append((source, stem, defines))
                    if domain == 'Physics' and source.name in VULKAN_OPTIMIZED_PHYSICS:
                        if source.name == 'PhysicsResidentMotionQueries.comp.glsl':
                            for phase in range(3):
                                programs.append((source, f'PhysicsResidentMotionQueries{phase}.comp.portable', [f'-DE2D_MOTION_STAGE={phase}']))
                        else:
                            programs.append((source, stem + '.portable', defines))
                if source.name in ('Canvas.vert.hlsl', 'CanvasInstanced.vert.hlsl'):
                    programs.append((source, source.stem.replace('Canvas', 'CanvasLighting'), ['-DE2D_LIGHTING=1']))
                if source.name == 'Lighting.frag.hlsl':
                    for types in itertools.product(range(3), repeat=3):
                        if types == (0, 0, 0):
                            continue
                        defines = [f'-D{name}_TYPE={("float4", "int4", "uint4")[kind]}'
                                   for name, kind in zip(('DIFFUSE', 'NORMAL', 'SPECULAR'), types)]
                        programs.append((source, f'Lighting{"".join(map(str, types))}.frag', defines))
                elif source.name == 'Drawable.frag.hlsl':
                    for count in (2, 3, 4):
                        programs.append((source, f'Drawable{count}.frag', [f'-DTARGETS={count}']))
                elif source.name == 'Clip.frag.hlsl':
                    for kind in (1, 2):
                        programs.append((source, f'Clip{kind}.frag', [f'-DDIFFUSE_TYPE={("float4", "int4", "uint4")[kind]}']))
            for source, stem, defines in programs:
                stage = source.stem.rsplit('.', 1)[-1]
                if stage not in ('vert', 'frag', 'comp'):
                    raise ValueError(f'Unknown built-in shader stage: {source}')
                name = f'{domain}/{stem}.spv'
                target = output / name
                fingerprint = hashlib.sha256(digest.digest() + ' '.join(defines).encode()).hexdigest()
                old = previous.get(name, {})
                if (old.get('source') == fingerprint and target.exists() and
                        old.get('binary') == hashlib.sha256(target.read_bytes()).hexdigest()):
                    current[name] = old
                    continue
                target.parent.mkdir(parents=True, exist_ok=True)
                with tempfile.TemporaryDirectory(dir=target.parent) as temporary:
                    compiled = Path(temporary) / 'shader.spv'
                    command = [str(compiler), '-V', '--target-env', 'vulkan1.0', '-S', stage, '-e', 'main']
                    if domain == 'Physics' and source.name in VULKAN_OPTIMIZED_PHYSICS and not stem.endswith('.portable'):
                        # Reduce cold driver compilation before these sweeps block a live simulation.
                        command += ['-O']
                    if source.suffix == '.hlsl':
                        command += ['-D', '--hlsl-iomap']
                    subprocess.run([*command, *defines, '-o', str(compiled), str(source)], check=True)
                    subprocess.run([str(validator), '--target-env', 'vulkan1.0', str(compiled)], check=True)
                    data = compiled.read_bytes()
                    if not target.exists() or target.read_bytes() != data:
                        os.replace(compiled, target)
                    current[name] = {'source': fingerprint, 'binary': hashlib.sha256(data).hexdigest()}
            if domain == 'Rendering':
                for stem in ('GlyphDecode.frag', 'GlyphLCD.frag', 'TexelBuffer.comp'):
                    name = f'Rendering/{stem}.reloc'
                    binary = output / f'Rendering/{stem}.spv'
                    if not binary.exists(): continue
                    fingerprint = hashlib.sha256(recipe + binary.read_bytes()).hexdigest()
                    target = output / name
                    old = previous.get(name, {})
                    if old.get('source') == fingerprint and target.exists() and old.get('binary') == hashlib.sha256(target.read_bytes()).hexdigest():
                        current[name] = old
                        continue
                    relocation = material_lighting.relocations(binary, toolchain / 'bin' / ('spirv-dis' + build_toolchain.suffix))
                    if not target.exists() or target.read_bytes() != relocation: target.write_bytes(relocation)
                    current[name] = {'source': fingerprint, 'binary': hashlib.sha256(relocation).hexdigest()}
            if domain == 'Rendering' and (directory / 'Lighting.frag.hlsl').exists():
                lighting = (directory / 'Lighting.frag.hlsl').read_text()
                for normal, specular, masks in itertools.product(range(-1, 3), range(-1, 3), (False, True)):
                    key = ((normal + 1) * 4 + specular + 1) * 2 + int(masks)
                    shader = material_lighting.source(lighting, normal, specular, masks)
                    fingerprint = hashlib.sha256(recipe + shader.encode()).hexdigest()
                    name = f'Rendering/LightCompose{key}.frag.spv'
                    reloc_name = name.removesuffix('.spv') + '.reloc'
                    target, reloc_target = output / name, output / reloc_name
                    names = (name, reloc_name)
                    if all(previous.get(n, {}).get('source') == fingerprint and (output/n).exists() and previous[n].get('binary') == hashlib.sha256((output/n).read_bytes()).hexdigest() for n in names):
                        for n in names: current[n] = previous[n]
                        continue
                    with tempfile.TemporaryDirectory(dir=target.parent) as temporary:
                        source = Path(temporary) / 'library.frag.glsl'; source.write_text(shader)
                        compiled = Path(temporary) / 'shader.spv'
                        subprocess.run([str(compiler), '-V', '--target-env', 'vulkan1.0', '-S', 'frag', '-e', 'main', '-o', str(compiled), str(source)], check=True)
                        subprocess.run([str(validator), '--target-env', 'vulkan1.0', str(compiled)], check=True)
                        binary = compiled.read_bytes()
                        relocation = material_lighting.relocations(compiled, toolchain / 'bin' / ('spirv-dis' + build_toolchain.suffix))
                        for n, data in ((name, binary), (reloc_name, relocation)):
                            file = output/n
                            if not file.exists() or file.read_bytes() != data: file.write_bytes(data)
                            current[n] = {'source': fingerprint, 'binary': hashlib.sha256(data).hexdigest()}
        for stale in output.glob('*/*'):
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
