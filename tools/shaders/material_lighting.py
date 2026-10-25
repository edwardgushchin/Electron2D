"""Generate an internal lighting function and exact ID relocation data with pinned tools."""
import json
import re
import struct
import subprocess


def source(lighting, normal, specular, masks, authored=False):
    text = lighting[lighting.index('float cross2'):]
    main = text.index('float4 main(')
    body = text[main:]
    body = body[body.index('{') + 1:]
    body = re.sub(r'float4 base = .*?;', 'float4 base = e2d_output;', body, count=1)
    body = body.replace('return float4(result, base.a * (item.w == 2 ? lightOnlyAlpha : ambient.a));', 'e2d_output = float4(result, base.a * (item.w == 2 ? lightOnlyAlpha : ambient.a));')
    body = body.replace('any(localUV < 0)', 'any(lessThan(localUV, float2(0)))').replace('any(localUV >= 1)', 'any(greaterThanEqual(localUV, float2(1)))')
    body = body.replace('float4 mask = 1;', 'float4 mask = float4(1);').replace('? map.rgb : 1', '? map.rgb : float3(1)')
    if authored:
        begin = body.index('        float3 lit = reflection(')
        end = body.index('        lit = lerp(', begin)
        body = body[:begin] + '''
        float4 contribution = e2d_authored_light_hook(normal, float4(lightColor.rgb * mask.rgb, lightColor.a * mask.a), range.z,
            lightVector, x.w != 0, x.w == 0 ? float3(endpoint, y.w) : float3(0), float3(position.xy, 0),
            float4(specularColor, specular.a * (counts.w != 0 ? map.a : 1)), shadowColor);
        float3 lit = contribution.rgb;
        float weight = contribution.a;
''' + body[end:]
    text = text[:main] + 'void main() {\n' + '''
vec4 position = gl_FragCoord;
ambient = lighting[e2d_item]; specular = lighting[e2d_item+1];
item = floatBitsToUint(lighting[e2d_item+2]); counts = floatBitsToUint(lighting[e2d_item+3]); atlas = lighting[e2d_item+4];
''' + body
    header = '#version 450\nlayout(location=1) in vec2 uv;\nlayout(location=3) flat in uint e2d_item;\nlayout(location=0) out vec4 e2d_output;\n'
    header += 'vec4 ambient, specular, atlas; uvec4 item, counts;\n'
    slot = 0
    for name, scalar, sampler in [('NORMAL_TEXTURE', normal, 'normalSampler'), ('SPECULAR_TEXTURE', specular, 'specularSampler'), ('LIGHT_ATLAS', 0 if masks else -1, 'lightSampler')]:
        if scalar >= 0:
            header += f'layout(set=2,binding={slot}) uniform {("sampler2D", "isampler2D", "usampler2D")[scalar]} e2d_{name};\n'
            slot += 1
            text = re.sub(rf'{name}\.Sample\({sampler}, (.*?)\)', rf'texture(e2d_{name}, \1)', text)
        else:
            if name == 'NORMAL_TEXTURE':
                text = text.replace('counts.z != 0', 'false')
            elif name == 'SPECULAR_TEXTURE':
                text = text.replace('counts.w != 0', 'false')
            # The atlas expression contains clamp(), so use the complete fixed call.
            call = {'NORMAL_TEXTURE': 'NORMAL_TEXTURE.Sample(normalSampler, uv)', 'SPECULAR_TEXTURE': 'SPECULAR_TEXTURE.Sample(specularSampler, uv)', 'LIGHT_ATLAS': 'LIGHT_ATLAS.Sample(lightSampler, region.xy + clamp(localUV, margin, 1 - margin) * region.zw)'}[name]
            text = text.replace(call, 'float4(1)')
    # HLSL scalar/vector reinterpretation is retained in the generated GLSL function.
    header += f'layout(set=2,binding={slot},std430) readonly buffer E2DLighting {{ vec4 lighting[]; }};\n'
    header += 'layout(origin_upper_left) in vec4 gl_FragCoord;\n'
    header += 'int asint(float v){return floatBitsToInt(v);} int asint(uint v){return int(v);}\nuint asuint(float v){return floatBitsToUint(v);}\n'
    if authored:
        header += '''vec4 e2d_authored_light_hook(vec3 normal, vec4 color, float energy, vec3 direction, bool directional,
            vec3 light_position, vec3 vertex, vec4 specular_value, inout vec4 shadow_color) { return vec4(0.0); }\n'''
    for hlsl, glsl in [('float4','vec4'), ('float3','vec3'), ('float2','vec2'), ('lerp','mix'), ('ddx','dFdx'), ('ddy','dFdy')]:
        text = re.sub(rf'\b{hlsl}\b', glsl, text)
    return header + text


def relocations(binary, disassembler):
    words = struct.unpack('<' + 'I' * (len(binary.read_bytes()) // 4), binary.read_bytes())
    assembly = subprocess.check_output([str(disassembler), '--raw-id', '--no-header', '--no-indent', str(binary)], text=True)
    position = 5
    result = []
    for line in assembly.splitlines():
        tokens = re.findall(r'"(?:[^"\\]|\\.)*"|\S+', line)
        if not tokens or tokens[0].startswith(';'):
            continue
        result_id = None
        if tokens[0].startswith('%'):
            result_id = tokens.pop(0); assert tokens.pop(0) == '='
        opcode = tokens.pop(0)
        if result_id is not None:
            no_type = opcode.startswith('OpType') or opcode in ('OpLabel', 'OpString', 'OpExtInstImport', 'OpDecorationGroup')
            tokens.insert(0 if no_type else 1, result_id)
        word = position + 1
        for token in tokens:
            if token.startswith('%'):
                assert words[word] == int(token[1:]), (opcode, token, word)
                result.append(word)
            word += (len(json.loads(token).encode('utf8')) + 4) // 4 if token.startswith('"') else 1
        assert word == position + (words[position] >> 16), (opcode, word, position, tokens)
        position = word
    assert position == len(words)
    return struct.pack('<' + 'I' * len(result), *result)
