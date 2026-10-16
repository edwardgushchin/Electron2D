cbuffer Parameters : register(b0, space3) { float4 padding; float gain; };
float4 main([[vk::location(0)]] float4 color : TEXCOORD0, [[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0 { return color * gain + float4(uv, 0, 0) * padding; }
