Texture2D<float4> TEXTURE : register(t0,space2);
SamplerState textureSampler : register(s0,space2);
cbuffer Shared : register(b0,space3) { float scale; };
cbuffer Instance : register(b1,space3) { float4 tint : packoffset(c0); bool enabled : packoffset(c1.x); uint number : packoffset(c1.y); int2 channel : packoffset(c1.z); };
float4 main(float4 color:TEXCOORD0,float2 uv:TEXCOORD1):SV_Target0 { return color * TEXTURE.Sample(textureSampler,uv) * tint * scale * (enabled ? 1.0 : 0.0) * float(channel.x+channel.y) / float(number); }
