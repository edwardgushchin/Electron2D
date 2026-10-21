#!/usr/bin/env python3
"""Verify authored defaults, complete typed metadata and atomic import failures."""
import argparse
import base64
import json
from pathlib import Path
import struct
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tool', type=Path, required=True)
args = parser.parse_args()
tool = str(args.tool.resolve())


def records(code):
    words = struct.unpack('<' + 'I' * (len(code) // 4), code)
    at = 5
    result = {}
    while at < len(words):
        count = words[at] >> 16
        if words[at] & 65535 == 7:
            text = code[(at + 2) * 4:(at + count) * 4].split(b'\0')[0].decode()
            if text.startswith('Electron2D:default:'):
                _, _, version, kind, length, name, value = text.split(':')
                assert version == '1'
                result[base64.b64decode(name).decode()] = (kind, int(length), base64.b64decode(value))
        at += count
    return result


with tempfile.TemporaryDirectory(prefix='electron2d-defaults-') as directory:
    directory = Path(directory)
    output = directory / 'program.spv'

    def invoke(source, success=True, stage='fragment'):
        previous = output.read_bytes() if output.exists() else None
        run = subprocess.run([tool, str(source), stage, str(output)], text=True, capture_output=True)
        diagnostic = run.stdout + run.stderr
        assert (run.returncode == 0) == success, diagnostic
        if not success:
            assert str(source) in diagnostic, diagnostic
            assert output.read_bytes() == previous, 'Failed defaults replaced the usable program'
        return diagnostic

    reference = None
    for language in ('hlsl', 'glsl'):
        source = root / f'tests/ShaderDefaults.Consumer/Shaders/Defaults.frag.{language}'
        invoke(source)
        assert output.read_bytes() == Path(str(source) + '.spv').read_bytes(), 'Source defaults fixture drifted'
        values = records(output.read_bytes())
        assert len(values) == 38
        if reference is None:
            reference = values
        else:
            assert values == reference, 'HLSL expressions and GLSL configuration disagree on typed words'
        stamp = output.stat().st_mtime_ns
        assert 'Unchanged ' in invoke(source) and output.stat().st_mtime_ns == stamp

    source = directory / 'values.glsl'
    source.write_text((root / 'tests/ShaderDefaults.Consumer/Shaders/Defaults.frag.glsl').read_text())
    config = Path(str(source) + '.defaults.json')
    configured = {'version': 1, 'parameters': {'f1': .25}}
    config.write_text(json.dumps(configured)); invoke(source)
    assert records(output.read_bytes())['f1'][2] == struct.pack('<f', .25)
    configured['parameters']['f1'] = .5
    config.write_text(json.dumps(configured)); invoke(source)
    assert records(output.read_bytes())['f1'][2] == struct.pack('<f', .5)
    config.unlink(); invoke(source)
    assert not records(output.read_bytes()), 'Removed authoring defaults survived reimport'
    for parameters in ({'F1': .25}, {'f1': True}, {'b1': 1}, {'i1': 2147483648}, {'u1': -1},
                       {'f1': 1e99}, {'f1a': [.25]}, {'f1a': [[.25], [.5]]}, {'m3': [1, 2]}):
        config.write_text(json.dumps({'version': 1, 'parameters': parameters}))
        assert str(config) in invoke(source, success=False)
    for invalid in ('{"version":1,"parameters":{"f1":0,"f1":1}}',
                    '{"version":1,"version":1,"parameters":{}}',
                    '{"version":2,"parameters":{}}', '{"version":1,"parameters":{},"extra":0}', '{'):
        config.write_text(invalid)
        assert str(config) in invoke(source, success=False)
    config.unlink()

    source = directory / 'runtime.hlsl'
    source.write_text('cbuffer Values:register(b0,space3){float runtimeValue; float authored = runtimeValue;};\n'
                      'float4 main():SV_Target {return float4(authored+runtimeValue,0,0,1);}')
    diagnostic = invoke(source, success=False)
    assert 'constant' in diagnostic, diagnostic
    source.write_text('cbuffer Values:register(b0,space3){float runtimeValue, authored = 0.25;};\n'
                      'float4 main():SV_Target {return float4(authored+runtimeValue,0,0,1);}')
    invoke(source)
    assert list(records(output.read_bytes())) == ['authored']
    source.write_text('cbuffer Values:register(b0,space3){vector<float,3> value = vector<float,3>(1,2,3);};\n'
                      'float4 main():SV_Target {return float4(value,1);}')
    invoke(source)
    assert records(output.read_bytes())['value'] == ('f3', 0, struct.pack('<3f', 1, 2, 3))
    source.write_text('cbuffer Values:register(b0,space3){float one[1] = {0.25};};\n'
                      'float4 main():SV_Target {return float4(one[0],0,0,1);}')
    invoke(source)
    assert records(output.read_bytes())['one'] == ('f1', 1, struct.pack('<f', .25))
    source.write_text('cbuffer Values:register(b0,space3){float value = asfloat(0x7f800000u);};\n'
                      'float4 main():SV_Target {return float4(value,0,0,1);}')
    invoke(source, success=False)

    include = directory / 'values.hlsli'
    include.write_text('#define BASE 0.5\nfloat square(float x) { return x*x; }\n')
    source = directory / 'expressions.hlsl'
    source.write_text('#include "values.hlsli"\nnamespace Demo {\n'
                      '[[vk::binding(0,3)]] cbuffer Parameters { float amount = square(BASE), other = -0.0; };\n}\n'
                      'float4 main():SV_Target { return float4(amount,other,0,1); }\n')
    invoke(source)
    values = records(output.read_bytes())
    assert values['amount'] == ('f1', 0, struct.pack('<f', .25))
    assert values['other'][2] == struct.pack('<f', -0.0)
    config = Path(str(source) + '.defaults.json')
    config.write_text('{"version":1,"parameters":{"amount":0.25,"other":0.0}}'); invoke(source)
    config.write_text('{"version":1,"parameters":{"amount":0.5}}'); invoke(source, success=False)
    config.unlink()

    for name in ('TIME', 'SCREEN_PIXEL_SIZE'):
        declaration = 'float TIME = 0.25;' if name == 'TIME' else 'float2 SCREEN_PIXEL_SIZE = float2(1,1);'
        value = name if name == 'TIME' else name + '.x'
        source.write_text('cbuffer Values:register(b0,space3){' + declaration + '};\n'
                          'float4 main():SV_Target { return float4(' + value + ',0,0,1); }')
        invoke(source, success=False)
    source.write_text('cbuffer Globals:register(b0,space3){float sceneValue = 0.25;};\n'
                      'float4 main():SV_Target { return float4(sceneValue,0,0,1); }')
    invoke(source, success=False)

print('Thirty-eight typed source defaults, expressions/includes/namespaces, configuration edits/removal, '
      'length-one arrays, signed zero, conflicts, runtime dependencies, renderer ownership and twenty atomic invalid imports passed.')
