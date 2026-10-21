[[vk::binding(0,2)]] Texture2D<float4> GLOBAL_sampler_image;
[[vk::binding(0,2)]] SamplerState pixelSampler;
[[vk::binding(0,3)]] cbuffer Parameters { float4 Coordinates; };
float4 main(float4 color : COLOR0, float2 uv : TEXCOORD0) : SV_Target0
{ return GLOBAL_sampler_image.Sample(pixelSampler, uv * Coordinates.zw + Coordinates.xy) * color; }
