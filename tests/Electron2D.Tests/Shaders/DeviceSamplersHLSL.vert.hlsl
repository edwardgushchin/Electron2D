Texture2D<float4> image : register(t7,space3);
SamplerState state : register(s7,space3);
struct Vertex { float4 position:SV_Position; float4 tint:TEXCOORD0; float2 uv:TEXCOORD1; };
Vertex main(uint id:SV_VertexID) { Vertex output; float2 p=float2((id<<1)&2,id&2); output.position=float4(p*2-1,0,1); output.uv=p; output.tint=image.SampleLevel(state,float2(.75,.25),0); return output; }
