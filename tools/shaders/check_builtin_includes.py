#!/usr/bin/env python3
"""Verify native compiler consumption and delivery of the shared shader library."""
import argparse
import hashlib
import json
import os
import subprocess
import tempfile
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tool', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
tool, output = args.tool.resolve(), args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
rows = []


def run(source, language, stage='fragment', success=True):
    target = output / (language + ('.frag' if stage == 'fragment' else '.' + stage) + '.spv')
    command = ['dotnet', str(tool), str(source), stage, str(target)]
    if stage != 'fragment':
        command += ['--device']
    result = subprocess.run(command, text=True, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, timeout=60,
                            env=dict(os.environ, TMPDIR=str(source.parent)))
    (output / (language + '-' + stage + ('' if success else '-failure') + '.log')).write_text(result.stdout)
    assert (result.returncode == 0) == success, result.stdout
    assert not list(source.parent.glob('electron2d-includes-*')), 'Library temporary files leaked'
    assert not list(source.parent.glob('electron2d-shader-*')), 'Compiler temporary files leaked'
    return target


with tempfile.TemporaryDirectory(prefix='electron2d-builtins-') as temporary:
    root = Path(temporary) / 'project with spaces'
    nested = root / 'nested'
    nested.mkdir(parents=True)
    for language, extension, v4, close in [
        ('GLSL', 'glsl', 'vec4', 'all(lessThan(abs(a-b), vec4(0.0003)))'),
        ('HLSL', 'hlsl', 'float4', 'all(abs(a-b) < 0.0003)'),
    ]:
        header = nested / 'Entry.inc'
        (nested / 'Local.inc').write_text('#define PROJECT_MARKER 1\n')
        name = 'electron2d/color.' + extension + 'inc'
        header.write_text('#ifndef PROJECT_ENTRY\n#define PROJECT_ENTRY\n#include "Local.inc"\n#if PROJECT_MARKER != 1\n#error LOCAL_INCLUDE_FAILED\n#endif\n#if 0\n'
                          '#include "absent-inactive.inc"\n#endif\n'
                          f'#include "{name}"\n#include "{name}"\n#endif\n')
        common = f'bool close_values({v4} a, {v4} b) {{ return {close}; }}\n'
        math = f'''{v4} decoded = Electron2D_SRGBToLinear(value);
{v4} encoded = Electron2D_LinearToSRGB(value);
bool valid = close_values(decoded, expectedDecode) && close_values(encoded, expectedEncode)
 && close_values({v4}(Electron2D_LinearToSRGB(Electron2D_SRGBToLinear(value.rgb)), value.a), value)
 && abs(Electron2D_SRGBToLinear(value.r) - expectedDecode.r) < 0.0003;
'''
        if language == 'GLSL':
            prefix = '#version 450\n#extension GL_GOOGLE_include_directive : require\n#include "nested/Entry.inc"\n'
            source = prefix + ('layout(set=3,binding=0,std140) uniform Params { vec4 value; '
                               'vec4 expectedDecode; vec4 expectedEncode; };\n'
                               'layout(location=0) out vec4 result;\n') + common
            source += 'void main(){' + math + 'result=valid?vec4(0,1,0,1):vec4(1,0,0,1);}\n'
            vertex = prefix + 'void main(){gl_Position=Electron2D_SRGBToLinear(vec4(0,0,0,1));}\n'
            compute = prefix + ('layout(local_size_x=1) in;\nlayout(set=0,binding=0,std430) buffer Output { vec4 value; };\n'
                                'void main(){value=Electron2D_LinearToSRGB(vec4(0.5));}\n')
        else:
            prefix = '#include "nested/Entry.inc"\n'
            source = prefix + ('cbuffer Params : register(b0,space3) { float4 value; '
                               'float4 expectedDecode; float4 expectedEncode; };\n') + common
            source += 'float4 main():SV_Target0{' + math + 'return valid?float4(0,1,0,1):float4(1,0,0,1);}\n'
            vertex = prefix + 'float4 main():SV_Position{return Electron2D_SRGBToLinear(float4(0,0,0,1));}\n'
            compute = prefix + ('RWStructuredBuffer<float4> Result : register(u0,space0);\n'
                                '[numthreads(1,1,1)] void main(){Result[0]=Electron2D_LinearToSRGB(float4(0.5,0.5,0.5,1));}\n')
        path = root / ('Fragment.' + extension)
        path.write_text(source)
        target = run(path, language)
        data, stamp = target.read_bytes(), target.stat().st_mtime_ns
        run(path, language)
        assert target.read_bytes() == data and target.stat().st_mtime_ns == stamp, 'Unchanged import rewrote bytecode'
        path.write_text(source.replace('nested/Entry.inc', 'electron2d/missing.' + extension))
        run(path, language, success=False)
        assert 'electron2d/missing.' + extension in (output / (language + '-fragment-failure.log')).read_text()
        assert target.read_bytes() == data, 'Missing include replaced valid bytecode'
        for stage, text in [('vertex', vertex), ('compute', compute)]:
            path.write_text(text)
            run(path, language, stage)
        rows.append({'language': language, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
(output / 'results.json').write_text(json.dumps(rows, indent=2) + '\n')
print(rows)
