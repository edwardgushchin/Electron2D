Texture2D<float4> TEXTURE : register(t0,space2);
SamplerState textureSampler : register(s0,space2);
cbuffer A : register(b0,space3) { float4 tint; float threshold; };
cbuffer B : register(b1,space3) { float scaleB; };
cbuffer C : register(b2,space3) { float scaleC; };
cbuffer D : register(b3,space3) { float scaleD; };
float4 main(float4 color:TEXCOORD0, float2 uv:TEXCOORD1):SV_Target0 {
    if (uv.x < threshold) discard;
    float4 result = color*tint*TEXTURE.Sample(textureSampler,uv)*(scaleB+scaleC+scaleD);
    if (uv.y < 0.5) return result;
    result.rgb *= 0.5;
    return result;
}
