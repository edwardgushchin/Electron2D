Texture2D<float4> TEXTURE : register(t0,space2);
SamplerState pointSampler : register(s0,space2);
cbuffer Values : register(b0,space3) { float gain; };
float4 main(float4 color:COLOR0, float2 uv:TEXCOORD0) : SV_Target0 { return TEXTURE.Sample(pointSampler,uv)*gain*color; }
