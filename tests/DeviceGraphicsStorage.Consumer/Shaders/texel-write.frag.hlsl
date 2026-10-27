[[vk::binding(7,3)]] [[vk::image_format("rgba8")]] RWBuffer<float4> texels;
[[vk::binding(2,4)]] RWBuffer<uint> counter;
struct Push{uint step;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main(float4 position:SV_Position):SV_Target0{uint count,old;texels.GetDimensions(count);uint i=uint(position.x)+uint(position.y)*4;texels[4+i]=pc.step==1?float4(1,0,0,1):float4(0,1,.5,1);texels[0xffffffffu]=1;texels[count]=1;InterlockedAdd(counter[1],pc.step,old);return float4(float(count)/40,1,.5,1);}
