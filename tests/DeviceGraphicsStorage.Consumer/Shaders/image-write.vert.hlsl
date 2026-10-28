[[vk::binding(0,0)]] RWTexture2D<uint> first;
struct Push {uint step;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main(uint id:SV_VertexID):SV_Position{first[uint2(id,0)]=pc.step+id;float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}
