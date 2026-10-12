struct Parameters { float4 tint; float4 controls; };
[[vk::push_constant]] ConstantBuffer<Parameters> pc;
float4 main(uint id:SV_VertexID):SV_Position { float2 p=float2((id<<1)&2,id&2)*2-1; return float4(p*float2(.5,1)+float2(pc.controls.x,0),0,1); }
