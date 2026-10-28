[[vk::binding(0,0)]] Texture2D<float4> source; [[vk::binding(0,0)]] SamplerState sourceSampler;
[[vk::binding(1,0)]] RWTexture2D<uint> firstCounter;
[[vk::binding(2,0)]] RWTexture2D<uint> secondCounter;
[[vk::binding(3,0)]] RWStructuredBuffer<uint> result;
float4 main(float4 position:SV_Position):SV_Target0{uint2 p=uint2(position.xy);uint a,b,w,h,m;source.GetDimensions(0,w,h,m);InterlockedAdd(firstCounter[p],1,a);InterlockedAdd(secondCounter[p],2,b);result[p.y*4+p.x]=a*10+b;return float4(source.SampleLevel(sourceSampler,(float2(p)+.5)/float2(w,h),0).r,float(b),.5,1);}
