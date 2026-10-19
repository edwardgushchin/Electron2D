[[vk::constant_id(3)]] const float scale=1;
struct Output { [[vk::location(1)]] float4 color:SV_Target1; [[vk::location(6)]] uint4 numbers:SV_Target6; };
Output main([[vk::location(1)]] float4 tint:TEXCOORD1) { Output o; o.color=float4(tint.rgb*scale,tint.a); o.numbers=uint4(42,43,44,45); return o; }
