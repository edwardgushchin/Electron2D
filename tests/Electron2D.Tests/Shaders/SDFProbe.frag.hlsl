Texture2D<float4> SDF_TEXTURE : register(t0, space2);
SamplerState fieldSampler : register(s0, space2);
cbuffer Frame : register(b0, space3) { float4 SDF_COORDS; };
float4 main(float4 position : SV_Position) : SV_Target0
{
    float distance = SDF_TEXTURE.Sample(fieldSampler, position.xy * SDF_COORDS.xy + SDF_COORDS.zw).r;
    return float4(distance < 0 ? 1 : 0, saturate(abs(distance) / 16), 0, 1);
}
