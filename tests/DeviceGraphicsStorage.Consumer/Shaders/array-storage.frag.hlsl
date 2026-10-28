[[vk::constant_id(0)]] const uint factor=7;
[[vk::binding(0,0)]] RWTexture2DArray<float4> destinations[2];
[[vk::binding(1,0)]] RWTexture2DArray<uint> unsignedImages[2];
[[vk::binding(2,0)]] RWTexture2DArray<int> signedImages[2];
[[vk::binding(3,0)]] Texture2DArray<float4> sampled;
[[vk::binding(3,0)]] SamplerState sampled_sampler;
struct Push {uint selected;};
[[vk::push_constant]] Push push;
float4 main(float4 position:SV_Position):SV_Target0{for(int layer=0;layer<3;layer++){int3 at=int3(int2(position.xy)+int2(0,1),layer);uint u;int s;InterlockedAdd(unsignedImages[push.selected][at],2*factor,u);InterlockedAdd(signedImages[push.selected][at],-2*2*int(factor),s);uint w,h,l;destinations[push.selected].GetDimensions(w,h,l);destinations[push.selected][at]=float4(sampled.SampleLevel(sampled_sampler,float3(.5,.5,0),1).r,float(unsignedImages[push.selected][at]+u),float(signedImages[push.selected][at]+s),float(l));}return float4(.375,.25,.5,1);}
