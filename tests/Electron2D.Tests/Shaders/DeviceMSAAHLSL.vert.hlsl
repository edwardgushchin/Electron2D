[[vk::binding(7,5)]] cbuffer Parameters { float4 tint; float4 controls; };
float4 main(uint id:SV_VertexID):SV_Position { float2 p[3]={float2(-.83,-.71),float2(.91,-.57),float2(-.61,.93)}; return float4(p[id],controls.x,1); }
