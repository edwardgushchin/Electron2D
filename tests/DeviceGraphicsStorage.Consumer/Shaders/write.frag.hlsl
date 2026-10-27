[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
struct Push { uint step; };[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main():SV_Target0{uint old;InterlockedAdd(value[1],pc.step,old);return pc.step==1?float4(1,0,0,1):float4(0,1,0,1);}
