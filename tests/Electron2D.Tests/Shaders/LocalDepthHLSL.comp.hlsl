[[vk::binding(13,2)]] Texture2DArray<float> depthMap;
[[vk::binding(13,2)]] SamplerComparisonState comparisonSampler;
[[vk::binding(0,3)]] RWStructuredBuffer<float> values;
[numthreads(1,1,1)]
void main(uint3 id:SV_DispatchThreadID) { values[id.x]=depthMap.SampleCmpLevelZero(comparisonSampler,float3(.5,.5,id.x),.25); }
