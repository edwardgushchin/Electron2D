float4 main([[vk::location(0)]] float4 color : TEXCOORD0, [[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0 { return color + uv.y * 0.00001; }
