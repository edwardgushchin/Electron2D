[[vk::combinedImageSampler]] Texture2D<uint4> values : register(t0, space2);
[[vk::combinedImageSampler]] SamplerState valueSampler : register(s0, space2);
[[vk::combinedImageSampler]] Texture2DArray<uint4> layers : register(t1, space2);
[[vk::combinedImageSampler]] SamplerState layerSampler : register(s1, space2);
cbuffer Params : register(b0, space3) { float selectedLayer; float selectedMip; float exposure; };
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1) : SV_Target0 {
    uint3 a=values.Load(int3(0,0,int(selectedMip))).rgb;
    uint3 b=layers.Load(int4(0,0,int(selectedLayer),int(selectedMip))).rgb;
    return float4(float3(a+b)*exposure,1)*color;
}
