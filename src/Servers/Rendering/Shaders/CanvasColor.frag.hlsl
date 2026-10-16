#include "Includes/electron2d/color.hlslinc"
Texture2D<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
cbuffer Operations : register(b0, space3) { float sourceMip; float decodeSRGB; float encodeSRGB; float padding; };
float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 value = TEXTURE.SampleLevel(canvasSampler, uv, sourceMip);
    if (decodeSRGB != 0) value.rgb = Electron2D_SRGBToLinear(value.rgb);
    if (encodeSRGB != 0) value.rgb = Electron2D_LinearToSRGB(value.rgb);
    return value;
}
