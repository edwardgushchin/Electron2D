Texture2D<float4> colorMaps[2] : register(t0, space2);
SamplerState colorSamplers[2] : register(s0, space2);
cbuffer Material : register(b0, space3) { int selected; };
float4 readMap(Texture2D<float4> source, SamplerState state, float2 uv) { return source.Sample(state,uv); }
float4 main(float4 color : TEXCOORD0) : SV_Target0 { return color*readMap(colorMaps[selected],colorSamplers[selected],float2(0.5,0.5)); }
