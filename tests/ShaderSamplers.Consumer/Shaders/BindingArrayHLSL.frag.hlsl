[[vk::binding(0,2)]] Texture2D<float4> pixels[2];
[[vk::binding(0,2)]] SamplerState pixelSampler[2];
[[vk::binding(0,3)]] cbuffer Parameters { float4 Coordinates; };
float4 main(float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target0
{ return pixels[1].Sample(pixelSampler[1], uv * Coordinates.zw + Coordinates.xy) * color; }
