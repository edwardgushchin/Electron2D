[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
float4 main(uint id:SV_SampleIndex):SV_Target0 { uint old; InterlockedAdd(value[id],1,old); return float4(float(id)/4,0,0,1); }
