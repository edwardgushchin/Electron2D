[[vk::binding(0,0)]] RWStructuredBuffer<uint> value;
float4 main([[vk::location(0)]] sample float2 coord:TEXCOORD0):SV_Target0 { uint old; InterlockedAdd(value[0],1,old); return float4(coord,0,1); }
