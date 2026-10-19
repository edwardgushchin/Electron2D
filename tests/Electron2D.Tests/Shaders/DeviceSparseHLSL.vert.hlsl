struct Output { float4 position:SV_Position; [[vk::location(1)]] float4 tint:TEXCOORD1; };
Output main(uint id:SV_VertexID) {
    float2 p=float2((id<<1)&2,id&2); Output o;
    o.position=float4(p*2-1,0,1); o.tint=float4(.25,.5,.75,1); return o;
}
