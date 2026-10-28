[[vk::binding(0,0)]] Texture2D<float4> source; [[vk::binding(0,0)]] SamplerState sourceSampler;
[[vk::binding(1,0)]] RWTexture2D<uint> counter;
[[vk::binding(2,0)]] RWStructuredBuffer<uint> result;
[[vk::binding(3,0)]] Texture2DArray<float4> layered; [[vk::binding(3,0)]] SamplerState layeredSampler;
[numthreads(4,4,1)] void main(uint3 id:SV_DispatchThreadID){uint w,h,m,aw,ah,l,am;source.GetDimensions(0,w,h,m);layered.GetDimensions(1,aw,ah,l,am);uint i=(id.y*4+id.x)*4;
result[i]=asuint(source.SampleLevel(sourceSampler,(float2(id.xy)+.5)/float2(w,h),0).r);
result[i+1]=asuint(layered.SampleLevel(layeredSampler,float3((float2(id.xy)+.5)/float2(aw,ah),2),1).r);
InterlockedAdd(counter[id.xy],1,result[i+2]);result[i+3]=w|(aw<<8)|(m<<16)|(am<<24);}
