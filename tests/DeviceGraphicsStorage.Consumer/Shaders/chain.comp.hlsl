[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
[numthreads(1,1,1)] void main(){value[2]=value[0]+value[1];}
