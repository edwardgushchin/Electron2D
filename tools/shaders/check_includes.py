#!/usr/bin/env python3
"""Exercise public include authoring through the published HLSL/GLSL importer and actual GPU pixels."""
from pathlib import Path
import argparse
import os
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tool', type=Path, required=True)
parser.add_argument('--runner', type=Path, required=True)
args = parser.parse_args()
tool = str(args.tool.resolve())
runner = str(args.runner.resolve())


def run(command, success=True):
    result = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    assert (result.returncode == 0) == success, result.stdout
    return result.stdout


with tempfile.TemporaryDirectory(prefix='electron2d-shader-includes-') as folder:
    root = Path(folder)
    factor = root / 'factor.hlslinc'
    nested = root / 'entry.glslinc'
    run(['dotnet', runner, '--shader-include-write', str(nested), '#ifndef INCLUDED\n#define INCLUDED\n#if 0\n#include "missing-inactive.hlslinc"\n#endif\n#include "factor.hlslinc"\n#endif\n'])
    hlsl = root / 'surface.hlsl'
    glsl = root / 'surface.glsl'
    hlsl.write_text('#include "entry.glslinc"\nfloat4 main(float4 color:TEXCOORD0):SV_Target0{return float4(color.rgb*FACTOR,color.a);}\n')
    glsl.write_text('#version 450\n#extension GL_GOOGLE_include_directive : require\n#include "entry.glslinc"\nlayout(location=0) in vec4 color;\nlayout(location=0) out vec4 result;\nvoid main(){result=vec4(color.rgb*FACTOR,color.a);}\n')
    outputs = [root / 'hlsl.spv', root / 'glsl.spv']
    previous = None
    for value in ['0.5', '0.2']:
        stamp = factor.stat() if factor.exists() else None
        run(['dotnet', runner, '--shader-include-write', str(factor), '#define FACTOR ' + value + '\n'])
        if stamp:
            os.utime(factor, ns=(stamp.st_atime_ns, stamp.st_mtime_ns))
        for source, output in zip([hlsl, glsl], outputs):
            run([tool, str(source), 'fragment', str(output)])
        current = [p.read_bytes() for p in outputs]
        if previous:
            assert all(a != b for a, b in zip(previous, current)), 'Include edit was missed when its timestamp was unchanged.'
        previous = current
        run(['dotnet', runner, '--shader-include-render', *map(str, outputs), value])
        print('Public include edit, nested guarded import and both-language GPU pixels:', value, flush=True)
    run(['dotnet', runner, '--shader-include-write', str(factor), '#error INCLUDE_FAILURE\n'])
    for source, output, preserved in zip([hlsl, glsl], outputs, previous):
        diagnostic = run([tool, str(source), 'fragment', str(output)], success=False)
        assert 'INCLUDE_FAILURE' in diagnostic and output.read_bytes() == preserved
    print('Compiler diagnostics and failed-import payload preservation passed.', flush=True)
