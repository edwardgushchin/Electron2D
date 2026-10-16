Texture2D<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
Texture2D<float4> values : register(t1, space2);
SamplerState valueSampler : register(s1, space2);
cbuffer Parameters : register(b0, space3) { float lod; float multiplier; float dataWeight; };
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1) : SV_Target0
{
    float4 result = color * multiplier * TEXTURE.SampleLevel(canvasSampler, uv, lod) + dataWeight * values.SampleLevel(valueSampler, uv, 0);
    result.a = 1; return result;
}
