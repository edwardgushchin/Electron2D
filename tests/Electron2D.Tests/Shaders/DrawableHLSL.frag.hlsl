Texture2D<float4> SOURCE_TEXTURE0 : register(t0,space2); SamplerState source0 : register(s0,space2);
Texture2D<float4> SOURCE_TEXTURE1 : register(t1,space2); SamplerState source1 : register(s1,space2);
Texture2D<float4> detail : register(t2,space2); SamplerState detailSampler : register(s2,space2);
cbuffer Params : register(b0,space3) { float4 gain; };
struct Output { float4 color0 : SV_Target0; float4 color1 : SV_Target1; };
Output main([[vk::location(0)]] float4 MODULATE : TEXCOORD0, [[vk::location(1)]] float2 UV : TEXCOORD1)
{
    Output o; o.color0=SOURCE_TEXTURE0.Sample(source0,UV)*MODULATE*gain;
    o.color1=SOURCE_TEXTURE1.Sample(source1,UV)*detail.Sample(detailSampler,UV)*gain; return o;
}
