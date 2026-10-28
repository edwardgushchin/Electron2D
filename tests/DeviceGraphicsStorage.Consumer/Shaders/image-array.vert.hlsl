[[vk::binding(0,0)]] RWTexture2D<uint> images[2];
struct Push {uint selected;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main(uint id:SV_VertexID):SV_Position{uint old;InterlockedAdd(images[pc.selected][int2(id,0)],1,old);float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}
