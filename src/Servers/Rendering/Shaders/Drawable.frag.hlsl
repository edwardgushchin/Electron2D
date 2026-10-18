#ifndef TARGETS
#define TARGETS 1
#endif
Texture2D<float4> SOURCE_TEXTURE0 : register(t0, space2); SamplerState source0 : register(s0, space2);
#if TARGETS > 1
Texture2D<float4> SOURCE_TEXTURE1 : register(t1, space2); SamplerState source1 : register(s1, space2);
#endif
#if TARGETS > 2
Texture2D<float4> SOURCE_TEXTURE2 : register(t2, space2); SamplerState source2 : register(s2, space2);
#endif
#if TARGETS > 3
Texture2D<float4> SOURCE_TEXTURE3 : register(t3, space2); SamplerState source3 : register(s3, space2);
#endif
struct Output {
    float4 color0 : SV_Target0;
#if TARGETS > 1
    float4 color1 : SV_Target1;
#endif
#if TARGETS > 2
    float4 color2 : SV_Target2;
#endif
#if TARGETS > 3
    float4 color3 : SV_Target3;
#endif
};
Output main([[vk::location(0)]] float4 modulate : TEXCOORD0, [[vk::location(1)]] float2 uv : TEXCOORD1)
{
    Output o; o.color0 = SOURCE_TEXTURE0.Sample(source0, uv) * modulate;
#if TARGETS > 1
    o.color1 = SOURCE_TEXTURE1.Sample(source1, uv) * modulate;
#endif
#if TARGETS > 2
    o.color2 = SOURCE_TEXTURE2.Sample(source2, uv) * modulate;
#endif
#if TARGETS > 3
    o.color3 = SOURCE_TEXTURE3.Sample(source3, uv) * modulate;
#endif
    return o;
}
