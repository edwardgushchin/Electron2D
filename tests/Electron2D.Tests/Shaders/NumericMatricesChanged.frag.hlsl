cbuffer Values : register(b0,space3) { float4x4 m3; float4x4 m4; float3x3 a3[1]; float4x4 a4[2]; float tail; };
float4 main():SV_Target0 { return float4(m3[0].xyz+m4[0].xyz+a3[0][0]+a4[0][0].xyz,tail); }
