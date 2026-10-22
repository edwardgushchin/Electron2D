#include "Includes/electron2d/color.hlslinc"
Texture2DArray<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
cbuffer Operations : register(b0, space3) { float sourceMip; float decodeSRGB; float sourceLayer; float reserved; };
float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 value = TEXTURE.SampleLevel(canvasSampler, float3(uv, sourceLayer), sourceMip);
    if (decodeSRGB != 0) value.rgb = Electron2D_SRGBToLinear(value.rgb);
    return value;
}
