[[vk::binding(7,5)]] cbuffer Settings { float4 color; };
[[vk::binding(8,6)]] StructuredBuffer<float4> modifier;
[[vk::binding(13,2)]] Texture2D<float4> sourceImage;
[[vk::binding(13,2)]] SamplerState sourceSampler;
float4 main(float4 tint:TEXCOORD0):SV_Target0 { return tint*color*modifier[0]*sourceImage.Sample(sourceSampler,float2(.5,.5)); }
