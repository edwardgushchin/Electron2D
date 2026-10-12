struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 tint : TEXCOORD0; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1)
{ Output result; result.position=float4(position,0,1); result.tint=color; return result; }
