Texture2D<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1) : SV_Target0
{
    return color * TEXTURE.Sample(canvasSampler, uv);
}
