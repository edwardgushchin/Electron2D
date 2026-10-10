[[vk::binding(0,2)]] Texture2DArray<float4> layers;
[[vk::binding(0,2)]] SamplerState layerSampler;
[[vk::binding(1,2)]] Texture2D<float4> detailMap;
[[vk::binding(1,2)]] SamplerState detailSampler;
[[vk::binding(0,3)]] cbuffer Params { float selectedLayer; };
float4 main(float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target0
{
    return layers.SampleLevel(layerSampler, float3(uv, selectedLayer), 0) * detailMap.SampleLevel(detailSampler, uv, 0) * color;
}
