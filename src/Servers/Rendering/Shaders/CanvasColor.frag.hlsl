Texture2D<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
cbuffer Operations : register(b0, space3) { float sourceMip; float decodeSRGB; float encodeSRGB; float padding; };
float3 Decode(float3 value)
{
    return float3(value.r <= .04045 ? value.r / 12.92 : pow((value.r + .055) / 1.055, 2.4),
                  value.g <= .04045 ? value.g / 12.92 : pow((value.g + .055) / 1.055, 2.4),
                  value.b <= .04045 ? value.b / 12.92 : pow((value.b + .055) / 1.055, 2.4));
}
float3 Encode(float3 value)
{
    return float3(value.r <= .0031308 ? value.r * 12.92 : 1.055 * pow(value.r, 1.0 / 2.4) - .055,
                  value.g <= .0031308 ? value.g * 12.92 : 1.055 * pow(value.g, 1.0 / 2.4) - .055,
                  value.b <= .0031308 ? value.b * 12.92 : 1.055 * pow(value.b, 1.0 / 2.4) - .055);
}
float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 value = TEXTURE.SampleLevel(canvasSampler, uv, sourceMip);
    if (decodeSRGB != 0) value.rgb = Decode(value.rgb);
    if (encodeSRGB != 0) value.rgb = Encode(value.rgb);
    return value;
}
