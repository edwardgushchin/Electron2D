[[vk::combinedImageSampler]] Texture2D<uint4> values : register(t0, space2);
[[vk::combinedImageSampler]] SamplerState valueSampler : register(s0, space2);
[[vk::combinedImageSampler]] Texture2DArray<int4> layers : register(t1, space2);
[[vk::combinedImageSampler]] SamplerState layerSampler : register(s1, space2);
cbuffer Params : register(b0, space3) { float selectedLayer; };
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1) : SV_Target0 {
    float4 result=float4(values.Load(int3(int2(uv*4),0)))/255.0 + float4(layers.Load(int4(int2(uv*4),int(selectedLayer),0)))*.01;
    result.a=1; return result*color;
}
