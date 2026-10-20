struct Input { [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; };
float4 main(Input input) : SV_Target0 { return input.color + float4(input.uv,0,0); }
