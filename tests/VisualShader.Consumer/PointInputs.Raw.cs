using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void AuthorPointRaw(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(IOPath.Combine(folder, "point-raw-glsl.vert.glsl"), "#version 450\nlayout(location=0) in vec2 position;layout(location=1) in vec4 color;layout(location=2) in vec2 uv;\nlayout(set=1,binding=0,std140) uniform Frame{vec2 size;};layout(set=1,binding=1,std140) uniform Values{float diameter;};layout(location=0) out vec4 outColor;layout(location=1) out vec2 outUV;\nvoid main(){gl_Position=vec4(position.x/size.x*2-1,1-position.y/size.y*2,0,1);gl_PointSize=diameter;outColor=color;outUV=uv;}\n");
        File.WriteAllText(IOPath.Combine(folder, "point-raw-glsl.frag.glsl"), "#version 450\nlayout(location=0) out vec4 result;void main(){result=vec4(gl_PointCoord,.75,1);}\n");
        File.WriteAllText(IOPath.Combine(folder, "point-raw-hlsl.vert.hlsl"), "cbuffer Frame:register(b0,space1){float2 size;};cbuffer Values:register(b1,space1){float diameter;};\nstruct Input{[[vk::location(0)]] float2 position:TEXCOORD0;[[vk::location(1)]] float4 color:COLOR0;[[vk::location(2)]] float2 uv:TEXCOORD1;};struct Output{float4 position:SV_Position;[[vk::builtin(\"PointSize\")]] float pointSize:PSIZE;[[vk::location(0)]] float4 color:COLOR0;[[vk::location(1)]] float2 uv:TEXCOORD0;};\nOutput main(Input input){Output o;o.position=float4(input.position.x/size.x*2-1,1-input.position.y/size.y*2,0,1);o.pointSize=diameter;o.color=input.color;o.uv=input.uv;return o;}\n");
        File.WriteAllText(IOPath.Combine(folder, "point-raw-hlsl.frag.hlsl"), "[[vk::ext_builtin_input(16)]] static const float2 coordinate; float4 main():SV_Target{return float4(coordinate,.75,1);}\n");
    }
}
