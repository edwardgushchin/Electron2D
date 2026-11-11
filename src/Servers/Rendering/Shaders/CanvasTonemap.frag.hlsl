#include "Includes/electron2d/tonemap.hlslinc"
#include "Includes/electron2d/adjustment.hlslinc"

Texture2D<float4> sourceImage : register(t0, space2);
Texture2D<float4> lookupImage : register(t1, space2);
SamplerState sourceSampler : register(s0, space2);
SamplerState lookupSampler : register(s1, space2);
cbuffer ToneMap : register(b0, space3)
{
    float4 tone;
    float4 curve;
    float4 adjustment;
    float4 lookupRegion;
};

float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 source = sourceImage.SampleLevel(sourceSampler, uv, 0);
    float3 color = TonemapCanvas(ExposeCanvas(source.rgb, tone.y), tone, curve);
    color = AdjustCanvasColor(color, adjustment, lookupRegion, lookupImage, lookupSampler);
    return float4(clamp(color, -65504.0, 65504.0), source.a);
}
