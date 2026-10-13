#ifndef SPECULAR_TYPE
#define SPECULAR_TYPE float4
#endif
#ifndef NORMAL_TYPE
#define NORMAL_TYPE float4
#endif
#ifndef DIFFUSE_TYPE
#define DIFFUSE_TYPE float4
#endif
Texture2D<DIFFUSE_TYPE> TEXTURE : register(t0, space2);
SamplerState colorSampler : register(s0, space2);
Texture2D<NORMAL_TYPE> NORMAL_TEXTURE : register(t1, space2);
SamplerState normalSampler : register(s1, space2);
Texture2D<SPECULAR_TYPE> SPECULAR_TEXTURE : register(t2, space2);
SamplerState specularSampler : register(s2, space2);
Texture2D<float4> LIGHT_ATLAS : register(t3, space2);
SamplerState lightSampler : register(s3, space2);
StructuredBuffer<float4> lighting : register(t4, space2);
cbuffer Item : register(b0, space3) {
    float4 ambient;
    float4 specular;
    uint4 item; // mask, signed Z, signed layer, shading mode
    uint4 counts; // lights, segments, normal channel, specular channel
    float4 atlas;
};
float cross2(float2 a, float2 b) { return a.x * b.y - a.y * b.x; }
float shadow(float2 position, float2 endpoint, uint mask, int layerMin, int layerMax) {
    float2 ray = endpoint - position;
    uint start = counts.x * 9;
    for (uint i = 0; i < counts.y; i++) {
        float4 edge = lighting[start + i * 2];
        float4 meta = lighting[start + i * 2 + 1];
        int layer = asint(meta.y);
        if ((asuint(meta.x) & mask) == 0 || layer < layerMin || layer > layerMax) continue;
        float2 segment = edge.zw - edge.xy;
        float determinant = cross2(ray, segment);
        if (abs(determinant) < 0.000001) continue;
        if (meta.z == 1 && determinant < 0 || meta.z == 2 && determinant > 0) continue;
        float2 delta = edge.xy - position;
        float t = cross2(delta, segment) / determinant;
        float u = cross2(delta, ray) / determinant;
        if (t > 0.00001 && t < 1 && u >= 0 && u <= 1) return 0;
    }
    return 1;
}
float3 reflection(float3 normal, float3 lightVector, float3 lightColor, float3 base, float3 specularColor, float shininess, bool normalUsed) {
    float ndotl = max(0, dot(normal, lightVector));
    if (!normalUsed) return lightColor * base;
    float3 diffuse = lightColor * base * ndotl;
    if (shininess >= 1) return diffuse;
    float3 h = lightVector + float3(0, 0, 1);
    h = dot(h, h) > 0.000001 ? normalize(h) : float3(0, 0, 1);
    float exponent = exp2(15 * shininess + 1) * 0.25;
    float blinn = pow(max(0, dot(normal, h)), exponent) * (exponent + 8) / (8 * 3.14159265359);
    float amount = blinn / max(4 * max(0, normal.z) * ndotl, 0.75);
    return diffuse + specularColor * lightColor * amount;
}
float4 main(float4 position : SV_Position, float4 color : TEXCOORD0, float2 uv : TEXCOORD1, [[vk::location(11)]] nointerpolation float4 glyph : TEXCOORD11) : SV_Target0 {
    float4 base = glyph.x == 0 ? color * float4(TEXTURE.Sample(colorSampler, uv)) : color;
    float3 normal = float3(0, 0, 1);
    if (counts.z != 0) {
        float2 mapped = float4(NORMAL_TEXTURE.Sample(normalSampler, uv)).rg * float2(2, -2) - float2(1, -1);
        float3 local = float3(mapped, sqrt(max(0, 1 - dot(mapped, mapped))));
        float2 dx = ddx(uv), dy = ddy(uv);
        float determinant = cross2(dx, dy);
        if (abs(determinant) > 0.000001) {
            float orientation = determinant < 0 ? -1 : 1;
            float2 tangent = normalize(float2(dy.y, -dx.y)) * orientation;
            float2 bitangent = normalize(float2(-dy.x, dx.x)) * orientation;
            normal = float3(tangent * local.x + bitangent * local.y, local.z);
            normal = dot(normal, normal) > 0.000001 ? normalize(normal) : float3(0, 0, 1);
        }
    }
    float4 map = float4(SPECULAR_TEXTURE.Sample(specularSampler, uv));
    float3 specularColor = specular.rgb * (counts.w != 0 ? map.rgb : 1);
    float3 result = item.w == 2 ? base.rgb : base.rgb * ambient.rgb;
    float lightOnlyAlpha = 0;
    for (uint i = 0; i < counts.x; i++) {
        uint first = i * 8;
        float4 x = lighting[first], y = lighting[first + 1];
        float4 origin = lighting[first + 2], lightColor = lighting[first + 3];
        float4 shadowColor = lighting[first + 4], parameters = lighting[first + 5];
        float4 membership = lighting[first + 6], range = lighting[first + 7];
        int layerMin = asint(range.x), layerMax = asint(range.y);
        if (asint(item.y) < asint(membership.z) || asint(item.y) > asint(membership.w) ||
            asint(item.z) < layerMin || asint(item.z) > layerMax) continue;
        float4 mask = 1;
        float2 endpoint;
        float3 lightVector;
        if (x.w == 0) {
            if ((item.x & asuint(membership.x)) == 0) continue;
            float2 localUV = float2(dot(x.xy, position.xy) + x.z, dot(y.xy, position.xy) + y.z);
            if (any(localUV < 0) || any(localUV >= 1)) continue;
            float4 region = lighting[counts.x * 8 + i];
            float2 margin = atlas.xy * 0.5 / region.zw;
            mask = LIGHT_ATLAS.Sample(lightSampler, region.xy + clamp(localUV, margin, 1 - margin) * region.zw);
            endpoint = origin.xy;
            lightVector = float3(endpoint - position.xy, y.w);
        } else {
            endpoint = position.xy - origin.zw * max(0, parameters.z);
            lightVector = float3(-origin.zw * (1 - y.w), y.w);
        }
        lightVector = dot(lightVector, lightVector) > 0.000001 ? normalize(lightVector) : float3(0, 0, 1);
        float visible = 1;
        if (parameters.x > 0) {
            int samples = parameters.x == 1 ? 1 : parameters.x == 2 ? 5 : 13;
            float2 delta = endpoint - position.xy;
            float2 perpendicular = dot(delta, delta) > 0.000001 ? normalize(float2(-delta.y, delta.x)) : float2(1, 0);
            visible = 0;
            for (int sampleIndex = 0; sampleIndex < samples; sampleIndex++) {
                float offset = (sampleIndex - (samples - 1) * 0.5) * max(1, parameters.y);
                visible += shadow(position.xy + perpendicular * offset, endpoint, asuint(membership.y), layerMin, layerMax);
            }
            visible /= samples;
        }
        float3 lit = reflection(normal, lightVector, lightColor.rgb * mask.rgb * (x.w == 0 ? lightColor.a : 1), base.rgb, specularColor, specular.a * (counts.w != 0 ? map.a : 1), counts.z != 0);
        float weight = mask.a * (x.w == 0 ? 1 : lightColor.a);
        lit = lerp(shadowColor.rgb, lit, visible);
        weight *= lerp(shadowColor.a, 1, visible);
        lightOnlyAlpha += weight;
        if (parameters.w == 0) result += lit * weight;
        else if (parameters.w == 1) result -= lit * weight;
        else result = lerp(result, lit, weight);
    }
    return float4(result, base.a * (item.w == 2 ? lightOnlyAlpha : ambient.a));
}
