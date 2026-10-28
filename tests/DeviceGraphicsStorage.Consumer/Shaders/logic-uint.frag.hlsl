struct Push {uint4 value;};[[vk::push_constant]] ConstantBuffer<Push> pc;
uint4 main():SV_Target0{return pc.value;}
