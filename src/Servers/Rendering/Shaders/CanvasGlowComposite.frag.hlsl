#include "Includes/electron2d/tonemap.hlslinc"

Texture2D<float4> sourceImage : register(t0, space2);
Texture2D<float4> level0 : register(t1, space2);
Texture2D<float4> level1 : register(t2, space2);
Texture2D<float4> level2 : register(t3, space2);
Texture2D<float4> level3 : register(t4, space2);
Texture2D<float4> level4 : register(t5, space2);
Texture2D<float4> level5 : register(t6, space2);
Texture2D<float4> level6 : register(t7, space2);
Texture2D<float4> mapImage : register(t8, space2);
SamplerState sourceSampler : register(s0, space2);
SamplerState sampler0 : register(s1, space2);
SamplerState sampler1 : register(s2, space2);
SamplerState sampler2 : register(s3, space2);
SamplerState sampler3 : register(s4, space2);
SamplerState sampler4 : register(s5, space2);
SamplerState sampler5 : register(s6, space2);
SamplerState sampler6 : register(s7, space2);
SamplerState mapSampler : register(s8, space2);
cbuffer Glow : register(b0, space3)
{
    float4 weights0;
    float4 weights1;
    float4 options; // intensity, map strength, blend mode, bicubic
    float4 mapRegion;
    float4 tone;
    float4 curve;
};

float3 SampleLevel(Texture2D<float4> image, SamplerState filter, float2 uv, float weight)
{
    if (weight == 0.0) return 0.0;
    if (options.w == 0.0) return image.SampleLevel(filter, uv, 0).rgb * weight;

    // Cubic B-spline reconstruction using four bilinear taps.
    uint width, height;
    image.GetDimensions(width, height);
    float2 size = float2(width, height);
    float2 pixel = uv * size - 0.5;
    float2 f = frac(pixel);
    float2 t = floor(pixel);
    float2 g = 1.0 - f;
    float2 w0 = g * g * g / 6.0;
    float2 w1 = (4.0 + f * f * (3.0 * f - 6.0)) / 6.0;
    float2 w2 = (4.0 + g * g * (3.0 * g - 6.0)) / 6.0;
    float2 w3 = f * f * f / 6.0;
    float2 a = w0 + w1;
    float2 b = w2 + w3;
    float2 left = (t - 0.5 + w1 / a) / size;
    float2 right = (t + 1.5 + w3 / b) / size;
    return weight * (image.SampleLevel(filter, left, 0).rgb * a.x * a.y +
                     image.SampleLevel(filter, float2(right.x, left.y), 0).rgb * b.x * a.y +
                     image.SampleLevel(filter, float2(left.x, right.y), 0).rgb * a.x * b.y +
                     image.SampleLevel(filter, right, 0).rgb * b.x * b.y);
}

float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 source = sourceImage.SampleLevel(sourceSampler, uv, 0);
    source.rgb = ExposeCanvas(source.rgb, tone.y);
    float3 glow = SampleLevel(level0, sampler0, uv, weights0.x) + SampleLevel(level1, sampler1, uv, weights0.y) +
                  SampleLevel(level2, sampler2, uv, weights0.z) + SampleLevel(level3, sampler3, uv, weights0.w) +
                  SampleLevel(level4, sampler4, uv, weights1.x) + SampleLevel(level5, sampler5, uv, weights1.y) +
                  SampleLevel(level6, sampler6, uv, weights1.z);
    if (options.y != 0)
        glow *= lerp(1.0, mapImage.SampleLevel(mapSampler, mapRegion.xy + uv * mapRegion.zw, 0).rgb, options.y);
    if (options.z == 4) source.rgb = lerp(source.rgb, glow, options.x);
    else
    {
        glow *= options.x;
        if (options.z == 0) source.rgb += glow;
        else if (options.z == 1) source.rgb += clamp(glow, 0.0, tone.w) * (1.0 - source.rgb / tone.w);
        else if (options.z == 2)
        {
            float3 value = TonemapCanvas(source.rgb, tone, curve);
            glow = TonemapCanvas(glow, tone, curve);
            float3 curve = lerp((16.0 * value - 12.0) * value * value + 4.0 * value, sqrt(max(value, 0.0)), step(0.25, value));
            source.rgb = lerp(value + saturate(glow) * (curve - value), value, step(1.0, value));
        }
        else source.rgb = glow;
    }
    if (options.z != 2) source.rgb = TonemapCanvas(source.rgb, tone, curve);
    return float4(clamp(source.rgb, -65504.0, 65504.0), source.a);
}
