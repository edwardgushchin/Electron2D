[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
struct Push { uint step; };[[vk::push_constant]] ConstantBuffer<Push> pc;
[[vk::constant_id(0)]] const uint factor=1;
float4 main(uint id:SV_VertexID):SV_Position{uint old;InterlockedAdd(value[0],factor,old);if(id==0 && pc.step==1){for(uint i=0;i<2;i++){value[4+i*4]=3;value[5+i*4]=1;value[6+i*4]=0;value[7+i*4]=0;}}float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}
