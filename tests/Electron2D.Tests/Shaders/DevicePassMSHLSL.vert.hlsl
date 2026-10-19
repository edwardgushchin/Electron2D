float4 main(uint vertex : SV_VertexID) : SV_Position {
 float2 p=float2((vertex<<1)&2,vertex&2);return float4(p*2-1,0,1);
}
