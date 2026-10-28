struct Push {float4 value;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main():SV_Target0{return pc.value;}
