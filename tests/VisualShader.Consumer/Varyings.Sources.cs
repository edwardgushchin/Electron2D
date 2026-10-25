using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] VaryingSourcePrograms = ["source-glsl", "source-hlsl"];
    private static readonly string[] VaryingRenderPrograms = VaryingGraphs.Concat(VaryingSourcePrograms).ToArray();
    private static void AuthorVaryingSources(string folder)
    {
        const string vertex = """
            #version 450
            layout(location=0) in vec2 position;
            layout(location=1) in vec4 color;
            layout(location=2) in vec2 uv;
            layout(location=3) in vec4 custom;
            layout(location=0) out vec4 outColor;
            layout(location=1) out vec2 outUV;
            layout(location=2) out vec4 outCustom;
            layout(location=12) out vec2 payload;
            layout(location=15) flat out uint tag;
            layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
            void main(){gl_Position=vec4(position.x/size.x*2.0-1.0,1.0-position.y/size.y*2.0,0,1);outColor=color;outUV=uv;outCustom=custom;payload=uv*.5+vec2(.125,.25);tag=4294967291u;}
            """;
        File.WriteAllText(IOPath.Combine(folder, "source-glsl.vert.glsl"), vertex);
        File.WriteAllText(IOPath.Combine(folder, "source-glsl.frag.glsl"), """
            #version 450
            layout(location=0) in vec4 color;
            layout(location=1) in vec2 uv;
            layout(location=12) in vec2 payload;
            layout(location=15) flat in uint tag;
            layout(location=0) out vec4 result;
            layout(set=3,binding=0,std140) uniform RendererValues { float gain; };
            void main(){bool valid=tag==4294967291u && all(lessThan(abs(payload-(uv*.5+vec2(.125,.25))),vec2(.0001)));result=vec4(valid?vec3(uv*.5+.125,.625)*gain*color.rgb:vec3(1,0,0),1);}
            """);
        const string fields = "[[vk::location(0)]] float4 color:COLOR0; [[vk::location(1)]] float2 uv:TEXCOORD0; [[vk::location(2)]] float4 custom:TEXCOORD1; [[vk::location(12)]] float2 payload:TEXCOORD2; [[vk::location(15)]] nointerpolation uint tag:TEXCOORD3;";
        File.WriteAllText(IOPath.Combine(folder, "source-hlsl.vert.hlsl"), "cbuffer Frame:register(b0,space1){float2 size;};\nstruct Output{float4 position:SV_Position;" + fields + "};\n" +
            "Output main([[vk::location(0)]] float2 position:POSITION,[[vk::location(1)]] float4 color:COLOR0,[[vk::location(2)]] float2 uv:TEXCOORD0,[[vk::location(3)]] float4 custom:TEXCOORD1){Output o;o.position=float4(position.x/size.x*2-1,1-position.y/size.y*2,0,1);o.color=color;o.uv=uv;o.custom=custom;o.payload=uv*.5+float2(.125,.25);o.tag=4294967291u;return o;}");
        File.WriteAllText(IOPath.Combine(folder, "source-hlsl.frag.hlsl"), "cbuffer RendererValues:register(b0,space3){float gain;};\nstruct Input{" + fields + "};\n" +
            "float4 main(Input i):SV_Target0{bool valid=i.tag==4294967291u && all(abs(i.payload-(i.uv*.5+float2(.125,.25)))<.0001);return float4(valid?float3(i.uv*.5+.125,.625)*gain*i.color.rgb:float3(1,0,0),1);}");
        foreach (var location in new[] { 11, 16 }) File.WriteAllText(IOPath.Combine(folder, "invalid", "slot" + location + ".vert.glsl"), vertex.Replace("location=12", "location=" + location, StringComparison.Ordinal));
    }
}
