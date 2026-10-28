[[vk::binding(1,0)]] RWTexture2D<uint> second;
struct Push {uint step;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main(float4 position:SV_Position):SV_Target0{uint2 p=uint2(position.xy);second[p]=pc.step+100+p.x+4*p.y;return float4(1,.5,.25,1);}
