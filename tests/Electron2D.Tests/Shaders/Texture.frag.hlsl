Texture2D<float4> colorMap : register(t0, space2);
SamplerState colorSampler : register(s0, space2);
Texture2D<float4> detailMap : register(t1, space2);
SamplerState detailSampler : register(s1, space2);
cbuffer Material : register(b0, space3) { float4 tint; float lod; float detailAmount; };
float4 main(float4 color : TEXCOORD0, float4 position : SV_Position) : SV_Target0 {
    float2 uv = position.xy / 64;
    return color * tint * colorMap.SampleLevel(colorSampler, uv, lod) + detailAmount * detailMap.SampleLevel(detailSampler, uv, lod);
}
