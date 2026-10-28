[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
float4 main():SV_Target0 { uint old; InterlockedAdd(value[0],1,old); return float4(1,.5,.25,.25); }
