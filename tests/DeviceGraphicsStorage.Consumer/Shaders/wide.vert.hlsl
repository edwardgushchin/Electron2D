float4 main(uint id:SV_VertexID):SV_Position { float2 p[3]={float2(-.75,0),float2(.75,0),float2(0,.75)}; return float4(p[id%3],0,1); }
