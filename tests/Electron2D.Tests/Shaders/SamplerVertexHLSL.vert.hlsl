cbuffer Frame : register(b0,space1) { float2 size; };
cbuffer Material : register(b1,space1) { int selected; };
Texture2D<float4> colorMaps[2] : register(t0,space0);
SamplerState states[2] : register(s0,space0);
struct Input {float2 position:POSITION; float4 color:COLOR0; float2 uv:TEXCOORD0;};
struct Output {float4 position:SV_Position; float4 color:TEXCOORD0; float2 uv:TEXCOORD1;};
Output main(Input input) {
    Output output;
    output.position=float4(input.position.x/size.x*2-1,1-input.position.y/size.y*2,0,1);
    output.color=input.color*colorMaps[selected].SampleLevel(states[selected],float2(.5,.5),0);
    output.uv=input.uv;
    return output;
}
