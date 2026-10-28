[[vk::binding(5,0)]] RWTexture2D<uint> checkImage;
float4 main(uint id:SV_VertexID):SV_Position{float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,checkImage[int2(0,0)]==3?0:2,1);}
