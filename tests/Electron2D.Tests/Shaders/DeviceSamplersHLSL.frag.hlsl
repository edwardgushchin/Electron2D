Texture2D<float4> image : register(t7,space3);
SamplerState state : register(s13,space5);
float4 read_image(Texture2D<float4> source,SamplerState sampling,float2 coords) { return source.Sample(sampling,coords); }
float4 main(float4 tint:TEXCOORD0,float2 uv:TEXCOORD1):SV_Target0 { return (tint+read_image(image,state,uv))*.5; }
