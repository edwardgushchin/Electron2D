[[vk::binding(7,3)]]RWBuffer<uint> counters[2];
float4 main(float4 position:SV_Position):SV_Target0{uint slot=uint(position.x)&1;uint old;InterlockedAdd(counters[NonUniformResourceIndex(slot)][1],1,old);return slot==0?float4(1,0,0,1):float4(0,1,0,1);}
