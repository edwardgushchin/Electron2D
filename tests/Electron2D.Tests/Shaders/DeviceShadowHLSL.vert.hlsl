[[vk::binding(7,5)]] cbuffer Parameters { float4 tint; float4 controls; };
float4 main(uint id:SV_VertexID):SV_Position { float2 p=float2((id<<1)&2,id&2)*2-1; if(controls.y!=0) p.x=-p.x; return float4(p,controls.x,1); }
