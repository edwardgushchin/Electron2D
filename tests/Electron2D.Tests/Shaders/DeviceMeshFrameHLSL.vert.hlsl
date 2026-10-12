struct Output { float4 position:SV_Position; [[vk::location(0)]] float3 normal:TEXCOORD0; [[vk::location(1)]] float4 tangent:TEXCOORD1; };
Output main([[vk::location(0)]] float2 position:TEXCOORD0,[[vk::location(1)]] float3 normal:TEXCOORD1,[[vk::location(2)]] float4 tangent:TEXCOORD2){Output o;o.position=float4(position,0,1);o.normal=normal;o.tangent=tangent;return o;}
