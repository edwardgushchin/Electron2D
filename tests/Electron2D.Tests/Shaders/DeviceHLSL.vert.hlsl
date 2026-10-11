struct Output { float4 position : SV_Position; float4 tint : TEXCOORD0; };
[[vk::binding(7,5)]] cbuffer Settings { float4 color; };
[[vk::binding(8,6)]] StructuredBuffer<float4> modifier;
Output main(float2 position:TEXCOORD0,float2 offset:TEXCOORD1) { Output o; o.position=float4(position+offset,0,1); o.tint=color*modifier[0]; return o; }
