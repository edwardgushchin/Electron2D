Texture2D<float4> TEXTURE : register(t0, space2);
SamplerState canvasSampler : register(s0, space2);
cbuffer Output : register(b0, space3) { float referenceNits; float maximumNits; float pqEncoding; float unused; };
float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float4 value = TEXTURE.SampleLevel(canvasSampler, uv, 0);
    float3 nits = clamp(value.rgb * referenceNits, 0.0, maximumNits);
    if (pqEncoding != 0)
    {
        nits = mul(float3x3(0.6274040, 0.3292820, 0.0433136,
                           0.0690970, 0.9195400, 0.0113612,
                           0.0163916, 0.0880132, 0.8955950), nits);
        float3 power = pow(saturate(nits / 10000.0), 2610.0 / 16384.0);
        value.rgb = pow((3424.0 / 4096.0 + (2413.0 / 128.0) * power) /
                        (1.0 + (2392.0 / 128.0) * power), 2523.0 / 32.0);
    }
    else value.rgb = nits / 80.0;
    return value;
}
