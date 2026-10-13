#ifndef DIFFUSE_TYPE
#define DIFFUSE_TYPE float4
#endif
Texture2D<DIFFUSE_TYPE> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
Texture2D<float4> SCREEN_TEXTURE : register(t1, space2);
SamplerState screenSampler : register(s1, space2);
cbuffer ScreenInfo : register(b0, space3) { float2 SCREEN_PIXEL_SIZE; float2 padding; };
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1, float4 position : SV_Position, [[vk::location(11)]] float4 glyph : TEXCOORD11) : SV_Target0
{
    float alpha = color.a * (glyph.x == 0 ? TEXTURE.Sample(canvasSampler, uv).a : 1);
    return float4(SCREEN_TEXTURE.SampleLevel(screenSampler, position.xy * SCREEN_PIXEL_SIZE, 0).rgb, alpha);
}
