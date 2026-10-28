struct Push {int4 value;};[[vk::push_constant]] ConstantBuffer<Push> pc;
int4 main():SV_Target0{return pc.value;}
