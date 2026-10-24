#!/usr/bin/env python3
"""Author and import the complete rectangular-uniform consumer fixtures."""
from pathlib import Path
import json
import subprocess
import sys

output = Path(sys.argv[1]).resolve()
importer = Path(sys.argv[2]).resolve()
output.mkdir(parents=True, exist_ok=True)
tool = ['dotnet', str(importer)] if importer.suffix == '.dll' else [str(importer)]
fields = [(f'{kind}{c}r{r}', c, r, kind, 2 if kind == 'a' else 1)
          for c in range(2, 5) for r in range(2, 5) if c != r for kind in 'cra']
assert len(fields) == 18 and sum(c * r * count for _, c, r, _, count in fields) == 208


def values(name, c, r, kind, element=0, alternate=False):
    if name == 'c3r2':
        return [1, 0, 0, 1, 8 if alternate else 4, 0]
    sign = -1 if kind == 'r' or element == 1 else 1
    divisor = 32 if kind == 'c' else 16 if kind == 'r' else 8
    return [sign * (i + 1) / divisor + (.25 if alternate else 0) for i in range(c * r)]


def number(value):
    return format(value, '.9g') + ('.0' if int(value) == value else '')


receipts = []
for language in ('glsl', 'hlsl'):
    for profile in ('fragment', 'vertex', 'reordered', 'changed', 'changed-vertex', 'single-array', 'blit', 'particles'):
        vertex = profile in ('vertex', 'changed-vertex')
        stage = 'vertex' if vertex else 'compute' if profile == 'particles' else 'fragment'
        changed = profile in ('changed', 'changed-vertex')
        declarations, defaults, checks, raw = [], {}, [], []
        ordered = list(reversed(fields)) if profile == 'reordered' else fields
        for name, c, r, kind, count in ordered:
            if profile == 'single-array' and kind == 'a': count = 1
            cc, rr = (3, 2) if changed and name == 'c2r3' else (c, r)
            shape = f'mat{cc}x{rr}' if language == 'glsl' else f'float{cc}x{rr}'
            order = 'row_major' if (kind != 'c') != (profile == 'reordered') else 'column_major'
            data = [values(name, c, r, kind, e) for e in range(count)]
            defaults[name] = data if kind == 'a' else data[0]
            array = f'[{count}]' if kind == 'a' else ''
            initialize = ('{' + ','.join(shape + '(' + ','.join(map(number, v)) + ')' for v in data) + '}') if kind == 'a' else shape + '(' + ','.join(map(number, data[0])) + ')'
            declarations.append(f'layout({order}) {shape} {name}{array};' if language == 'glsl' else f'{order} {shape} {name}{array} = {initialize};')
            for e in range(count):
                for i, initial in enumerate(data[e]):
                    access = name + (f'[{e}]' if kind == 'a' else '') + f'[{i // rr}][{i % rr}]'
                    alternate = values(name, c, r, kind, e, True)[i]
                    wanted = f'(phase == 0.0 ? {number(initial)} : {number(alternate)})'
                    checks.append(f'valid = valid && {access} == {wanted};')
                    raw.append((access, wanted))
        defaults.update(phase=0, tail=.875)
        declarations.extend(['float phase' + ('' if language == 'glsl' else ' = 0.0') + ';', 'float tail' + ('' if language == 'glsl' else ' = .875') + ';'])
        binding = 1 if vertex or profile == 'particles' else 0
        space = 1 if vertex else 2 if profile == 'particles' else 3
        block = (f'layout(set={space},binding={binding},std140) uniform Values {{\n' if language == 'glsl' else f'cbuffer Values : register(b{binding},space{space}) {{\n') + '\n'.join(declarations) + '\n};\n'
        valid = 'bool valid = tail == .875;\n' + '\n'.join(checks) + '\n'
        if language == 'glsl':
            header = '#version 450\n'
            if profile == 'particles':
                header += '#extension GL_GOOGLE_include_directive : require\n#include "electron2d/particles.glslinc"\n'
                body = 'layout(local_size_x=64) in;\nvoid main(){uint index=gl_GlobalInvocationID.x;if(index>=Counts.x)return;\n' + valid + 'Electron2D_Particle p=PARTICLES[index];if(p.State.x!=0)p.Color=valid?vec4(.25,.5,.75,1):vec4(1,0,1,1);PARTICLES[index]=p;}\n'
            elif vertex:
                header += 'layout(location=0) in vec2 position;layout(location=1) in vec4 color;layout(location=2) in vec2 uv;layout(location=0) out vec4 outColor;layout(location=1) out vec2 outUV;layout(set=1,binding=0,std140) uniform Frame{vec2 size;};\n'
                body = 'void main(){\n' + valid + 'vec2 p=c3r2*vec3(position,1);gl_Position=vec4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);outColor=valid?color:vec4(1,0,1,1);outUV=uv;}\n'
            elif profile == 'blit':
                header += 'layout(location=1) in vec2 uv;layout(location=0) out vec4 outputColor;\n'
                body = 'void main(){\n' + valid + 'int index=int(uv.x*208.0);outputColor=vec4(0,0,0,1);\n' + '\n'.join(f'if(index=={i})outputColor=vec4({actual},{expected},valid?1:0,1);' for i, (actual, expected) in enumerate(raw)) + '\n}\n'
            else:
                header += 'layout(location=0) in vec4 color;layout(location=0) out vec4 outputColor;\n'
                body = 'void main(){\n' + valid + 'outputColor=valid?vec4(.25,.5,.75,1)*color:vec4(1,0,1,1);}\n'
        else:
            header = '#include "electron2d/particles.hlslinc"\n' if profile == 'particles' else ''
            if profile == 'particles':
                body = '[numthreads(64,1,1)] void main(uint3 id:SV_DispatchThreadID){uint index=id.x;if(index>=Counts.x)return;\n' + valid + 'Electron2D_Particle p=PARTICLES[index];if(p.State.x!=0)p.Color=valid?float4(.25,.5,.75,1):float4(1,0,1,1);PARTICLES[index]=p;}\n'
            elif vertex:
                header += 'cbuffer Frame:register(b0,space1){float2 size;};struct Output{float4 position:SV_Position;float4 color:TEXCOORD0;float2 uv:TEXCOORD1;};\n'
                body = 'Output main(float2 position:TEXCOORD0,float4 color:TEXCOORD1,float2 uv:TEXCOORD2){\n' + valid + 'float2 p=mul(float3(position,1),c3r2);Output o;o.position=float4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);o.color=valid?color:float4(1,0,1,1);o.uv=uv;return o;}\n'
            elif profile == 'blit':
                body = 'float4 main(float4 modulate:TEXCOORD0,float2 uv:TEXCOORD1):SV_Target0{\n' + valid + 'int index=int(uv.x*208.0);float4 result=float4(0,0,0,1);\n' + '\n'.join(f'if(index=={i})result=float4({actual},{expected},valid?1:0,1);' for i, (actual, expected) in enumerate(raw)) + '\nreturn result;}\n'
            else:
                body = 'float4 main(float4 color:TEXCOORD0):SV_Target0{\n' + valid + 'return valid?float4(.25,.5,.75,1)*color:float4(1,0,1,1);}\n'
        source = output / f'{profile}.{language}'
        source.write_text(header + block + body)
        if language == 'glsl':
            Path(str(source) + '.defaults.json').write_text(json.dumps({'version': 1, 'parameters': defaults}, indent=2) + '\n')
        command = tool + [str(source), stage, str(source) + '.spv']
        if profile == 'blit': command.append('--texture-blit')
        if profile == 'particles': command.append('--particles')
        run = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        receipts.append({'source': source.name, 'command': command, 'exit': run.returncode, 'diagnostic': run.stdout})
        (output / 'imports.json').write_text(json.dumps(receipts, indent=2) + '\n')
        print(source.name, run.returncode, flush=True)
        if run.returncode:
            print(run.stdout[-5000:])
            raise SystemExit(run.returncode)

for columns in (3, 4):
    source = output / f'global{columns}.glsl'
    source.write_text(f'#version 450\nlayout(location=0) out vec4 outputColor;layout(set=3,binding=0,std140) uniform Globals{{mat{columns}x2 rectangular_global;}};void main(){{outputColor=vec4(rectangular_global[0],0,1);}}\n')
    command = tool + [str(source), 'fragment', str(source) + '.spv', '--texture-blit']
    run = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    receipts.append({'source': source.name, 'command': command, 'exit': run.returncode, 'diagnostic': run.stdout})
    (output / 'imports.json').write_text(json.dumps(receipts, indent=2) + '\n')
    print(source.name, run.returncode, flush=True)
    if run.returncode:
        print(run.stdout[-5000:])
        raise SystemExit(run.returncode)
