float4 main([[vk::location(1)]] float2 primaryUV : TEXCOORD1, [[vk::location(2)]] float4 custom : TEXCOORD2, [[vk::location(4)]] float2 secondaryUV : TEXCOORD4) : SV_Target0
{ return float4(secondaryUV.x, primaryUV.x, custom.b, 1); }
