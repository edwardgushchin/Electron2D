#!/usr/bin/env python3
"""Verify both source import paths, diagnostics, binary validation and atomic output replacement."""
from pathlib import Path
import argparse
import base64
import re
import json
import shutil
import subprocess
import struct
import sys
import tempfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tool', type=Path, help='Verify an existing published ShaderImport executable without building.')
args = parser.parse_args()
if args.tool:
    tool = [str(args.tool.resolve())]
    packaged = args.tool.resolve().parent / 'toolchain'
    assert (packaged / 'toolchain.lock.json').read_bytes() == (root / 'tools/shaders/toolchain.lock.json').read_bytes()
    assert all(any((args.tool.resolve().parent / 'licence').glob(name + '-LICENSE*')) for name in ('glslang', 'SPIRV-Tools', 'SPIRV-Headers'))
    revision = next(entry['commit'] for entry in json.loads((packaged / 'toolchain.lock.json').read_bytes())
                    if entry['name'] == 'SPIRV-Tools')
    assert revision in subprocess.check_output([str(packaged / 'bin/spirv-val'), '--version'], text=True)
else:
    subprocess.run([sys.executable, str(root / 'tools/shaders/build_toolchain.py')], check=True)
    subprocess.run(['dotnet', 'build', str(root / 'tools/shaders/ShaderImport.csproj'), '-c', 'Release'], check=True)
    tool = ['dotnet', str(root / 'tools/shaders/bin/Release/net10.0/Electron2D.ShaderImport.dll')]

def invoke(source, output, stage='fragment', success=True):
    run = subprocess.run([*tool, str(source), stage, str(output)], text=True, capture_output=True)
    diagnostic = run.stdout + run.stderr
    assert (run.returncode == 0) == success, diagnostic
    if not success:
        assert str(source) in diagnostic and stage in diagnostic, diagnostic
    return diagnostic

with tempfile.TemporaryDirectory(prefix='electron2d-import-check-') as directory:
    directory = Path(directory)
    output = directory / 'program.spv'
    for language, artifact in [('hlsl', 'SwapHlsl'), ('glsl', 'SwapGlsl')]:
        source = root / f'tests/Electron2D.Tests/Shaders/Swap.frag.{language}'
        invoke(source, output)
        expected = root / f'tests/Electron2D.Tests/Shaders/{artifact}.spv'
        assert output.read_bytes() == expected.read_bytes(), f'{language} compiler output differs from the recorded fixture'
        profile = directory / f'profile.{language}'
        if language == 'hlsl':
            guard = '#if __HLSL_VERSION != 2021 || __SHADER_TARGET_MAJOR != 6 || __SHADER_TARGET_MINOR != 0\n#error Unexpected HLSL profile\n#endif\n'
            profile.write_text(guard + source.read_text())
        else:
            guard = '#if __VERSION__ != 450 || VULKAN != 100\n#error Unexpected GLSL profile\n#endif\n'
            profile.write_text(source.read_text().replace('#version 450\n', '#version 450\n' + guard, 1))
        invoke(profile, output)
        assert output.read_bytes() == expected.read_bytes(), f'{language} profile guard changed the program'
        stamp = output.stat().st_mtime_ns
        assert 'Unchanged ' in invoke(source, output)
        assert output.stat().st_mtime_ns == stamp, 'An unchanged import rewrote the artifact'
        previous = output.read_bytes()
        invalid = directory / f'broken.{language}'
        invalid.write_text('this is invalid source\n')
        invoke(invalid, output, success=False)
        assert output.read_bytes() == previous, 'A failed import replaced the last usable artifact'
    external = directory / 'external.spv'
    external.write_bytes(output.read_bytes())
    invoke(external, output)
    assert output.read_bytes() == external.read_bytes(), 'Valid third-party bytecode changed during import'
    external.write_bytes(b'not SPIR-V')
    invoke(external, output, success=False)
    assert output.read_bytes() == previous, 'Invalid bytecode replaced the usable artifact'
    for stage, stem in [('vertex', 'Canvas.vert'), ('fragment', 'Canvas.frag')]:
        source = root / f'src/Servers/Rendering/Shaders/{stem}.hlsl'
        invoke(source, output, stage)
        assert output.read_bytes() == (root / f'src/Servers/Rendering/Shaders/{stem}.spv').read_bytes()
    for stem, artifact in [('Canvas', 'CanvasGLSL'), ('CanvasUV', 'CanvasUV')]:
        invoke(root / f'tests/Electron2D.Tests/Shaders/{stem}.frag.glsl', output)
        assert output.read_bytes() == (root / f'tests/Electron2D.Tests/Shaders/{artifact}.spv').read_bytes()
    assert (root / 'tests/Electron2D.Tests/Shaders/CanvasHLSL.spv').read_bytes() == (root / 'src/Servers/Rendering/Shaders/Canvas.frag.spv').read_bytes()
    invoke(root / 'tests/Electron2D.Tests/Shaders/InstanceCustom.frag.hlsl', output)
    assert output.read_bytes() == (root / 'tests/Electron2D.Tests/Shaders/InstanceCustom.spv').read_bytes()
    instance_glsl = directory / 'instance-data.glsl'
    instance_glsl.write_text('#version 450\nlayout(location=2) in vec4 data;\nlayout(location=0) out vec4 result;\nvoid main() { result=data; }\n')
    invoke(instance_glsl, output)
    previous = output.read_bytes()
    instance_glsl.write_text(instance_glsl.read_text().replace('in vec4 data', 'in vec3 data').replace('result=data', 'result=vec4(data,1)'))
    assert 'varyings' in invoke(instance_glsl, output, success=False)
    assert output.read_bytes() == previous, 'An invalid instance channel replaced the valid program'
    missing_uv = directory / 'missing-uv.vert.glsl'
    missing_uv.write_text('''#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; } frame;
layout(location=0) out vec4 tint;
layout(location=2) out vec4 data;
void main() { gl_Position=vec4((position+uv)/frame.size,0,1); tint=color; data=color; }
''')
    assert 'outputs require color and UV' in invoke(missing_uv, output, stage='vertex', success=False)
    assert output.read_bytes() == previous, 'An incomplete vertex interface replaced the valid program'
    for language, stem, artifact in [('hlsl', 'Material', 'MaterialHlsl'), ('glsl', 'Material', 'MaterialGlsl'), ('glsl', 'MaterialReordered', 'MaterialReordered'),
                                     ('hlsl', 'Texture', 'TextureHlsl'), ('glsl', 'Texture', 'TextureGlsl'), ('glsl', 'TextureReordered', 'TextureReordered'),
                                     ('hlsl', 'Booleans', 'BooleansHLSL'), ('glsl', 'Booleans', 'BooleansGLSL'), ('glsl', 'BooleansReordered', 'BooleansReordered'),
                                     ('hlsl', 'Matrices', 'MatricesHLSL'), ('glsl', 'Matrices', 'MatricesGLSL'), ('glsl', 'MatricesReordered', 'MatricesReordered'),
                                     ('hlsl', 'Values', 'ValuesHLSL'), ('glsl', 'Values', 'ValuesGLSL'), ('glsl', 'ValuesReordered', 'ValuesReordered'), ('glsl', 'ValuesSigned', 'ValuesSigned'),
                                     ('hlsl', 'Time', 'TimeHLSL'), ('glsl', 'Time', 'TimeGLSL'), ('glsl', 'TimeReordered', 'TimeReordered'), ('glsl', 'TimeOnly', 'TimeOnly')]:
        source = root / f'tests/Electron2D.Tests/Shaders/{stem}.frag.{language}'
        invoke(source, output)
        assert output.read_bytes() == (root / f'tests/Electron2D.Tests/Shaders/{artifact}.spv').read_bytes()
        previous = output.read_bytes()
        wrong_binding = directory / f'wrong-binding.{language}'
        wrong_binding.write_text(source.read_text().replace('space3', 'space0').replace('set = 3', 'set = 0'))
        assert 'descriptor set' in invoke(wrong_binding, output, success=False)
        assert output.read_bytes() == previous, 'An incompatible resource interface replaced the usable artifact'
    original = (root / 'tests/Electron2D.Tests/Shaders/Material.frag.glsl').read_text()
    for label, source, diagnostic in [
        ('gap', original.replace('binding = 1', 'binding = 2'), 'contiguous'),
        ('collision', original.replace('binding = 1', 'binding = 0'), 'contiguous'),
        ('large-buffer', original.replace('weights[2]', 'weights[2000]'), '16384'),
    ]:
        invalid = directory / f'{label}.glsl'
        invalid.write_text(source)
        assert diagnostic in invoke(invalid, output, success=False)
        assert output.read_bytes() == previous
    glsl = (root / 'tests/Electron2D.Tests/Shaders/Texture.frag.glsl').read_text()
    hlsl = (root / 'tests/Electron2D.Tests/Shaders/Texture.frag.hlsl').read_text()
    for label, language, source, diagnostic in [
        ('texture-set', 'glsl', glsl.replace('set = 2', 'set = 0'), 'descriptor set 2'),
        ('canvas-binding', 'glsl', glsl.replace('detailMap', 'TEXTURE'), 'binding zero'),
        ('texture-gap', 'glsl', glsl.replace('binding = 1', 'binding = 2'), 'contiguous'),
        ('texture-collision', 'glsl', glsl.replace('binding = 1', 'binding = 0'), 'unique'),
        ('texture-array', 'glsl', glsl.replace('colorMap;', 'colorMap[2];').replace('textureLod(colorMap,', 'textureLod(colorMap[0],'), 'arrays'),
        ('texture-dimension', 'glsl', glsl.replace('sampler2D', 'sampler3D').replace(', uv, lod)', ', vec3(uv, 0), lod)'), '2D'),
        ('sampler-gap', 'hlsl', hlsl.replace('register(s1,', 'register(s2,'), 'contiguous'),
        ('sampler-pair', 'hlsl', hlsl.replace('colorMap.SampleLevel(colorSampler', 'colorMap.SampleLevel(detailSampler')
                                    .replace('detailMap.SampleLevel(detailSampler', 'detailMap.SampleLevel(colorSampler'), 'same binding'),
    ]:
        invalid = directory / f'{label}.{language}'
        invalid.write_text(source)
        assert diagnostic in invoke(invalid, output, success=False)
        assert output.read_bytes() == previous, 'Invalid texture bindings replaced the usable artifact'
    unused = directory / 'unused.glsl'
    unused.write_text(glsl.replace('void main()', 'layout(set = 2, binding = 7) uniform sampler2D unusedMap;\nvoid main()'))
    invoke(unused, output)
    for language in ('hlsl', 'glsl'):
        clock = (root / f'tests/Electron2D.Tests/Shaders/Time.frag.{language}').read_text()
        for label, source in [
            ('time-integer', clock.replace('float TIME;', 'int TIME;')),
            ('time-vector', clock.replace('float TIME;', ('float2' if language == 'hlsl' else 'vec2') + ' TIME;').replace('TIME * 4', 'TIME.x * 4')),
            ('time-array', clock.replace('float TIME;', 'float TIME[1];').replace('TIME * 4', 'TIME[0] * 4')),
        ]:
            invalid = directory / f'{label}.{language}'
            invalid.write_text(source)
            previous = output.read_bytes()
            assert 'TIME' in invoke(invalid, output, success=False)
            assert output.read_bytes() == previous
    clock = (root / 'tests/Electron2D.Tests/Shaders/Time.frag.glsl').read_text()
    for label, source, diagnostic in [
        ('time-texture', glsl.replace('colorMap', 'TIME'), 'TIME'),
        ('time-duplicate', clock.replace('void main()', 'layout(set = 3, binding = 1, std140) uniform More { float TIME; } other;\nvoid main()').replace('TIME * 4', '(TIME + other.TIME) * 4'), 'unique'),
    ]:
        invalid = directory / f'{label}.glsl'
        invalid.write_text(source)
        assert diagnostic in invoke(invalid, output, success=False)
        assert output.read_bytes() == previous
    # External producers enter the exact same TIME validation path as source languages.
    invalid.write_text(clock.replace('float TIME;', 'int TIME;'))
    compiler = (args.tool.resolve().parent / 'toolchain' if args.tool else root / 'tools/shaders/bin/Release/net10.0/toolchain') / 'bin/glslangValidator'
    subprocess.run([str(compiler), '-V', '--target-env', 'vulkan1.0', '-S', 'frag', '-e', 'main', '-o', str(external), str(invalid)], check=True, capture_output=True)
    assert 'TIME' in invoke(external, output, success=False)
    assert output.read_bytes() == previous
    vertex = directory / 'vertex-time.hlsl'
    vertex.write_text((root / 'src/Servers/Rendering/Shaders/Canvas.vert.hlsl').read_text()
                      .replace('float2 size;', 'float2 size; float TIME;').replace('o.color = color;', 'o.color = color * TIME;'))
    assert 'TIME' in invoke(vertex, output, stage='vertex', success=False)
    assert output.read_bytes() == previous
    # External vector producers use the same layout checks, without source-only metadata.
    vectors = (root / 'tests/Electron2D.Tests/Shaders/ValuesGLSL.spv').read_bytes()
    external.write_bytes(vectors)
    invoke(external, output)
    assert output.read_bytes() == vectors
    for label in ('rgb-offset', 'vector-stride'):
        words = list(struct.unpack('<' + 'I' * (len(vectors) // 4), vectors))
        at, changed = 5, False
        while at < len(words):
            count, opcode = words[at] >> 16, words[at] & 0xffff
            if label == 'rgb-offset' and opcode == 72 and count == 5 and words[at + 2:at + 5] == [0, 35, 0]:
                words[at + 4], changed = 4, True
            if label == 'vector-stride' and opcode == 71 and count == 4 and words[at + 2:at + 4] == [6, 16]:
                words[at + 3], changed = 12, True
            at += count
        assert changed, label
        external.write_bytes(struct.pack('<' + 'I' * len(words), *words))
        invoke(external, output, success=False)
        assert output.read_bytes() == vectors, 'Invalid vector layout replaced the last usable artifact'
    # Matrix storage is part of the common external-bytecode contract too.
    matrix = (root / 'tests/Electron2D.Tests/Shaders/MatricesGLSL.spv').read_bytes()
    matrix_words = list(struct.unpack('<' + 'I' * (len(matrix) // 4), matrix))
    words, at = matrix_words.copy(), 5
    while at < len(words):
        count, opcode = words[at] >> 16, words[at] & 0xffff
        if opcode == 72 and count == 5 and words[at + 3] in (7, 35):
            words[at + 4] *= 2
        if opcode == 71 and count == 4 and words[at + 2] == 6:
            words[at + 3] *= 2
        at += count
    external.write_bytes(struct.pack('<' + 'I' * len(words), *words))
    invoke(external, output)
    previous = output.read_bytes()
    assert previous == external.read_bytes(), 'Valid widened matrix layout changed during import'
    for fault in ('missing-order', 'missing-stride', 'short-stride', 'unaligned-stride', 'offset'):
        words, at = matrix_words.copy(), 5
        while at < len(words):
            count, opcode = words[at] >> 16, words[at] & 0xffff
            if opcode == 72:
                if fault == 'missing-stride' and words[at + 3] == 7:
                    del words[at:at + count]
                    break
                if fault == 'missing-order' and words[at + 3] in (4, 5):
                    words[at + 3] = 0
                if fault in ('short-stride', 'unaligned-stride') and words[at + 3] == 7:
                    words[at + 4] = 8 if fault == 'short-stride' else 20
                if fault == 'offset' and words[at + 3] == 35 and words[at + 4] == 0:
                    words[at + 4] = 4
            at += count
        external.write_bytes(struct.pack('<' + 'I' * len(words), *words))
        invoke(external, output, success=False)
        assert output.read_bytes() == previous, 'Invalid matrix layout replaced the last usable artifact'
    for language, source in [
        ('hlsl', 'cbuffer Values : register(b0, space3) { float3x3 invalid; }; float4 main() : SV_Target0 { return float4(invalid[0], 1); }'),
        ('glsl', '#version 450\nlayout(location = 0) out vec4 outputColor; layout(set = 3, binding = 0, std140) uniform Values { mat3 invalid; }; void main() { outputColor = vec4(invalid[0], 1); }'),
    ]:
        invalid = directory / f'unsupported-matrix.{language}'
        invalid.write_text(source)
        assert 'float2x2' in invoke(invalid, output, success=False)
        assert output.read_bytes() == previous
    # Source type reflection must preserve bool without relabeling neighboring uint.
    def boolean_records(code):
        words = struct.unpack('<' + 'I' * (len(code) // 4), code)
        at, records = 5, {}
        while at < len(words):
            count, opcode = words[at] >> 16, words[at] & 0xffff
            if opcode == 7:
                text = code[(at + 2) * 4:(at + count) * 4].split(b'\0', 1)[0]
                if text.startswith(b'Electron2D:'):
                    kind, version, width, length, name = text.decode().split(':')[1:]
                    assert kind == 'bool' and version == '1'
                    name = base64.b64decode(name).decode()
                    assert name not in records
                    records[name] = (int(width), int(length))
            at += count
        return records

    expected = {'enabled': (1, 0), 'switches': (1, 3), 'pair': (2, 0), 'triple': (3, 0),
                'quad': (4, 0), 'pairs': (2, 2), 'triples': (3, 2), 'quads': (4, 2)}
    for language, artifact in [('hlsl', 'BooleansHLSL'), ('glsl', 'BooleansGLSL')]:
        source = (root / f'tests/Electron2D.Tests/Shaders/Booleans.frag.{language}').read_text()
        original = (root / f'tests/Electron2D.Tests/Shaders/{artifact}.spv').read_bytes()
        assert boolean_records(original) == expected
        external.write_bytes(original)
        invoke(external, output)
        assert output.read_bytes() == original
        # Native frontend metadata handles macros/includes, typedefs and unused resources.
        if language == 'hlsl':
            header = '#if !defined(__spirv__) || __SPIRV_MAJOR_VERSION__ != 1 || __SPIRV_MINOR_VERSION__ != 0\n#error Unexpected target\n#endif\ntypedef bool Flag;\n'
            variant = '#include "flags.inc"\ncbuffer Unused : register(b3, space3) { bool dead; };\n' + source.replace('bool enabled;', 'Flag enabled;')
        else:
            header = '#define Flag bool\n'
            variant = source.replace('#version 450', '#version 450\n#extension GL_GOOGLE_include_directive : require\n#include "flags.inc"')
            variant = variant.replace('bool enabled;', 'Flag enabled;').replace('void main()', 'layout(set = 3, binding = 3, std140) uniform Unused { bool dead; };\nvoid main()')
        (directory / 'flags.inc').write_text(header)
        input_variant = directory / f'source-types.{language}'
        input_variant.write_text(variant)
        invoke(input_variant, output)
        assert boolean_records(output.read_bytes()) == expected
        if language == 'hlsl':
            end = source.index('};') + 2
            variant = source[:end].replace('cbuffer Values : register(b0, space3)', 'struct Values') + '\nConstantBuffer<Values> values : register(b0, space3);' + source[end:]
            end = variant.index('float bits')
            variant = variant[:end] + re.sub(r'\b(' + '|'.join([*expected, 'number', 'afterTriple', 'tail']) + r')\b', r'values.\1', variant[end:])
            input_variant.write_text(variant)
            invoke(input_variant, output)
            assert boolean_records(output.read_bytes()) == expected
            input_variant.write_text(source.replace('cbuffer Values : register(b0, space3)', '[[vk::binding(0, 3)]] cbuffer Values'))
            invoke(input_variant, output)
            assert boolean_records(output.read_bytes()) == expected
        # A one-element array remains an array, even if optimization reuses its reads.
        variant = source.replace('switches[1]', 'switches[0]').replace('switches[2]', 'switches[0]').replace('switches[3]', 'switches[1]')
        input_variant.write_text(variant)
        invoke(input_variant, output)
        assert boolean_records(output.read_bytes()) == expected | {'switches': (1, 1)}
        external.write_bytes(original)
        invoke(external, output)
        for before, after in [(b'Electron2D:bool:1:1:0:', b'Electron2D:bool:2:1:0:'),
                              (b'Electron2D:bool:1:1:0:', b'Electron2D:bool:1:2:0:'),
                              (b'Electron2D:bool:1:1:0:', b'Electron2D:bool:1:1:1:'),
                              (b'ZW5hYmxlZA==', b'ZW5hYmxlZQ=='),
                              (b'ZW5hYmxlZA==', b'/w5hYmxlZA==')]:
            # Last two mutations exercise absent names and malformed UTF-8 in base64 data.
            invalid_bytes = original.replace(before, after)
            assert invalid_bytes != original
            external.write_bytes(invalid_bytes)
            invoke(external, output, success=False)
            assert output.read_bytes() == original, 'Invalid bool metadata replaced the usable artifact'
    if args.tool:
        sandbox = directory / 'package'
        shutil.copytree(args.tool.resolve().parent, sandbox)
        validator = sandbox / 'toolchain/bin/spirv-val'
        previous = output.read_bytes()
        for replacement, diagnostic in [('#!/bin/sh\nprintf "SPIRV-Tools v2026.30 fake\\n"\n', 'reported version'),
                                         (None, 'is missing')]:
            validator.unlink()
            if replacement is not None:
                validator.write_text(replacement)
                validator.chmod(0o755)
            run = subprocess.run([str(sandbox / args.tool.name), str(unused), 'fragment', str(output)],
                                 capture_output=True, text=True)
            assert run.returncode == 1 and diagnostic in run.stderr, run.stderr
            assert output.read_bytes() == previous, 'A broken toolchain replaced the last usable artifact'
print('Shader import checks passed: HLSL 2021/SM6.0, GLSL 450/Vulkan1.0, SPIR-V, diagnostics, atomic replacement, embedded programs, material buffers, texture/sampler bindings, RGB/Rect2/unsigned-vector/mat2 mappings, boolean source/artifact metadata, reserved TIME and unused resources.')
