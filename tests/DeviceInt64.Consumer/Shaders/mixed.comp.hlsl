[[vk::binding(0,0)]] StructuredBuffer<uint> source;
[[vk::binding(1,0)]] RWStructuredBuffer<double> target;
[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){target[id.x]=double(source[id.x]);}
