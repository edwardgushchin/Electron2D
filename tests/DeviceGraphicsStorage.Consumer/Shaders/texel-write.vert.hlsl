[[vk::binding(7,3)]] [[vk::image_format("rgba8")]] RWBuffer<float4> texels;
[[vk::binding(2,4)]] RWBuffer<uint> counter;
struct Push{uint step;};[[vk::push_constant]] ConstantBuffer<Push> pc;
[[vk::constant_id(0)]]const uint factor=1;
float4 main(uint id:SV_VertexID):SV_Position{uint old;texels[id]=pc.step==1?float4(1,0,0,1):float4(0,1,.5,1);InterlockedAdd(counter[0],factor,old);float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}
