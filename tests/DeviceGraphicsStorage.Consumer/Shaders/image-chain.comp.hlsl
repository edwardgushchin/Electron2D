[[vk::binding(0,0)]] RWTexture2D<float4> destination;
[numthreads(4,4,1)] void main(uint3 id:SV_DispatchThreadID){destination[id.xy]+=float4(.125,0,0,0);}
