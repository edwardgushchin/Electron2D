Texture2D<float4> SCREEN_TEXTURE : register(t0, space2);
SamplerState screenSampler : register(s0, space2);
cbuffer Effect : register(b0, space3)
{
    float2 SCREEN_PIXEL_SIZE;
    float lod;
    float unpremultiply;
    float4 tint;
    float2 offset;
};
float4 main(float4 color : TEXCOORD0, float4 position : SV_Position) : SV_Target0
{
    float4 c = SCREEN_TEXTURE.SampleLevel(screenSampler, (position.xy+offset) * SCREEN_PIXEL_SIZE, lod);
    if (unpremultiply > 0.5 && c.a > 0.0001) c.rgb /= c.a;
    return color * tint * c;
}
