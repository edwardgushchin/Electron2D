[[vk::binding(7,5)]] cbuffer Parameters { float4 tint; float4 controls; };
[[vk::binding(13,2)]] Texture2D<float> shadowMap;
[[vk::binding(13,2)]] SamplerComparisonState comparisonSampler;
float4 main():SV_Target0 { float s=shadowMap.SampleCmpLevelZero(comparisonSampler,float2(.5,.5),controls.w); return float4(s,s,s,1); }
