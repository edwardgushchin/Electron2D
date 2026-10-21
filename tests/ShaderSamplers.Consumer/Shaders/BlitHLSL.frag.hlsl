[[vk::binding(0,2)]] Texture2D<float4> SOURCE_TEXTURE0;
[[vk::binding(0,2)]] SamplerState pixelSampler;
[[vk::binding(0,3)]] cbuffer Parameters { float4 Coordinates; };
float4 main(float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target0
{ return SOURCE_TEXTURE0.Sample(pixelSampler, uv * Coordinates.zw + Coordinates.xy) * color; }
