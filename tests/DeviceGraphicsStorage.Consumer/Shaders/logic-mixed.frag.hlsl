[[vk::binding(0,0)]] Texture2D<float4> sampled;[[vk::binding(0,0)]] SamplerState nearest;
[[vk::binding(1,0)]] RWTexture2D<uint> counter;
struct Out{uint4 integerColor:SV_Target0;float4 color:SV_Target1;};
Out main(float4 p:SV_Position){Out o;o.color=sampled.SampleLevel(nearest,float2(.5,.5),0);o.integerColor=uint4(round(o.color*255));uint old;InterlockedAdd(counter[int2(p.xy)],1,old);return o;}
