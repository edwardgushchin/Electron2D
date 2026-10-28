[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
[[vk::constant_id(0)]] const uint factor=1;
void main(uint id:SV_VertexID){uint old; InterlockedAdd(value[0],factor,old);}
