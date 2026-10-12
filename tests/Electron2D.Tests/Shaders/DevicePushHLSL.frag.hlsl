struct Parameters { float4 tint; float4 controls; };
[[vk::push_constant]] ConstantBuffer<Parameters> pc;
cbuffer User : register(b0,space0) { float4 multiplier; }
float4 main():SV_Target0 { return pc.tint*multiplier; }
