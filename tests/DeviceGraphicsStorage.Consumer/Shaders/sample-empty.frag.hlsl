[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
void main(){uint old;InterlockedAdd(value[0],1,old);}
