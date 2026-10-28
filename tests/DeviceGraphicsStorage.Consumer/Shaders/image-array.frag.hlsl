[[vk::binding(1,0)]] RWTexture2D<uint> images[2];
struct Push {uint selected;};[[vk::push_constant]] ConstantBuffer<Push> pc;
void main(float4 position:SV_Position){uint old;InterlockedAdd(images[pc.selected][int2(position.xy)],2,old);}
