[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
void main(float4 position:SV_Position){uint2 p=uint2(position.xy); uint old; InterlockedAdd(value[1],1,old); InterlockedAdd(value[8+p.x+p.y*7],1,old);}
