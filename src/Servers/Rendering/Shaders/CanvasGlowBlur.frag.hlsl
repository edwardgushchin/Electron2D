#include "Includes/electron2d/tonemap.hlslinc"

Texture2D<float4> inputImage : register(t0, space2);
SamplerState linearSampler : register(s0, space2);
cbuffer Glow : register(b0, space3)
{
    float2 texel;
    float strength;
    float exposure;
    float threshold;
    float transition;
    float bloom;
    float cap;
};

float3 ReadGlow(float2 uv)
{
    float3 color = inputImage.SampleLevel(linearSampler, uv, 0).rgb;
    if (exposure < 0) return color;
    color = ExposeCanvas(color, exposure);

    float brightness = max(color.r, max(color.g, color.b));
    float feedback = transition > 0 ? smoothstep(threshold, threshold + transition, brightness) : step(threshold, brightness);
    return clamp(color * max(feedback, bloom), 0, cap);
}

float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float2 d = texel * strength;
    float3 center = ReadGlow(uv) * 4.0;
    float3 sides = ReadGlow(uv + float2(d.x, 0)) + ReadGlow(uv - float2(d.x, 0)) +
                   ReadGlow(uv + float2(0, d.y)) + ReadGlow(uv - float2(0, d.y));
    float3 corners = ReadGlow(uv + d) + ReadGlow(uv - d) +
                     ReadGlow(uv + float2(d.x, -d.y)) + ReadGlow(uv + float2(-d.x, d.y));
    return float4(min((center + sides * 2.0 + corners) * (strength / 16.0), 65504.0), 1);
}
