struct Parameters {float4 tint;float4 controls;};
[[vk::push_constant]] ConstantBuffer<Parameters> pc;
float4 main():SV_Target0{return pc.tint;}
