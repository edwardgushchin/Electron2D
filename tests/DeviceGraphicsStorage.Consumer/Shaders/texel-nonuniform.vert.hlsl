[[vk::binding(7,3)]]RWBuffer<uint> counters[2];
float4 main(uint id:SV_VertexID):SV_Position{uint old;InterlockedAdd(counters[NonUniformResourceIndex(id&1)][0],1,old);float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}
