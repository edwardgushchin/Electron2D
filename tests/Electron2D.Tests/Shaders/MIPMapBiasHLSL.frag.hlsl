Texture2D<float4> colorMaps[2] : register(t0, space2);
SamplerState colorSamplers[2] : register(s0, space2);
cbuffer Material : register(b0, space3) { int selected; };
float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1) : SV_Target0
{ return color*colorMaps[selected].Sample(colorSamplers[selected],uv); }
