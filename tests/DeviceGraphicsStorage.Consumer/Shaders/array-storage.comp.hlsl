[[vk::binding(0,0)]] RWTexture2DArray<float4> destination;
[[vk::binding(1,0)]] RWTexture2DArray<uint> counter;
[[vk::binding(2,0)]] Texture2DArray<float> source;
[[vk::binding(2,0)]] SamplerState source_sampler;
[[vk::binding(3,0)]] RWStructuredBuffer<uint4> values;
[numthreads(4,4,1)]void main(uint3 p:SV_DispatchThreadID){uint width,height,layers;destination.GetDimensions(width,height,layers);if(any(p>=uint3(width,height,layers)))return;
 uint prior;InterlockedAdd(counter[uint3(p.xy,2)],1u,prior);float stored=asfloat(counter[uint3(p.xy,1)]);
 destination[p]=float4(source.SampleLevel(source_sampler,float3(0.5,0.5,0),1),float(p.z+1),stored,1);
 uint cw,ch,cl,sw,sh,sl,levels;counter.GetDimensions(cw,ch,cl);source.GetDimensions(0,sw,sh,sl,levels);
 values[(p.z*height+p.y)*width+p.x]=uint4(prior,layers,cl,levels);}
