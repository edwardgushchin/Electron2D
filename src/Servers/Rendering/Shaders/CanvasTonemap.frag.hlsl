#include "Includes/electron2d/tonemap.hlslinc"

Texture2D<float4> sourceImage : register(t0, space2);
SamplerState sourceSampler : register(s0, space2);
cbuffer ToneMap : register(b0, space3)
{
    float4 tone;
    float4 curve;
};

float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 source = sourceImage.SampleLevel(sourceSampler, uv, 0);
    float3 color = TonemapCanvas(ExposeCanvas(source.rgb, tone.y), tone, curve);
    return float4(clamp(color, -65504.0, 65504.0), source.a);
}
