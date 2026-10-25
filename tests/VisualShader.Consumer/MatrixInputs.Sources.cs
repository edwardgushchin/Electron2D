using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static void AuthorMatrixSources(string folder)
    {
        File.WriteAllText(IOPath.Combine(folder, "matrix-process.comp.glsl"), """
            #version 450
            #extension GL_GOOGLE_include_directive : require
            #include "electron2d/particles.glslinc"
            layout(local_size_x=64) in;
            void main() {
                uint i=gl_GlobalInvocationID.x; if(i>=Counts.x) return;
                PARTICLES[i].TransformX=vec4(1.0+float(i)*.25,-.25,0,3.0+float(i)*5.0);
                PARTICLES[i].TransformY=vec4(.125,1,0,4);
                PARTICLES[i].VelocityAge.z=float(i);
            }
            """);
        File.WriteAllText(IOPath.Combine(folder, "matrix.vert.glsl"), """
            #version 450
            layout(location=0) in vec2 inPosition;
            layout(location=1) in vec4 inColor;
            layout(location=2) in vec2 inUV;
            layout(location=3) in vec4 inCustom;
            layout(location=0) out vec4 outColor;
            layout(location=1) out vec2 outUV;
            layout(location=2) out vec4 outCustom;
            layout(set=0,binding=0) uniform sampler2D vertexMap;
            layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
            layout(set=1,binding=1,std140) uniform RendererValues { mat4 MODEL_MATRIX; mat4 CANVAS_MATRIX; mat4 SCREEN_MATRIX; float row; };
            void main(){
                int c=int(inUV.x)/4,r=int(inUV.x)%4; float scale=c==3&&r<2?0.001953125:0.125; float ss=c==r&&r<2?32.0:0.125;
                outColor=vec4(vec3(MODEL_MATRIX[c][r]*scale,CANVAS_MATRIX[c][r]*scale,SCREEN_MATRIX[c][r]*ss)+vec3(.5),1)*inColor;
                outColor*=textureLod(vertexMap,vec2(0),0);
                vec2 p=vec2(4+inUV.x*4,row+inUV.y*8+float(gl_InstanceIndex)*12)+inPosition*1e-20;
                gl_Position=vec4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);outUV=inUV;outCustom=inCustom;
            }
            """);
        File.WriteAllText(IOPath.Combine(folder, "matrix.vert.hlsl"), """
            Texture2D<float4> vertexMap : register(t0,space0);
            SamplerState vertexSampler : register(s0,space0);
            cbuffer Frame : register(b0,space1) { float2 size; };
            cbuffer RendererValues : register(b1,space1) { float4x4 MODEL_MATRIX; float4x4 CANVAS_MATRIX; float4x4 SCREEN_MATRIX; float row; };
            struct Input { float2 position:TEXCOORD0; float4 color:TEXCOORD1; float2 uv:TEXCOORD2; float4 custom:TEXCOORD3; uint instance:SV_InstanceID; };
            struct Output { float4 position:SV_Position; float4 color:TEXCOORD0; float2 uv:TEXCOORD1; float4 custom:TEXCOORD2; };
            Output main(Input input){
                Output result;int c=int(input.uv.x)/4,r=int(input.uv.x)%4;float scale=c==3&&r<2?0.001953125:0.125;float ss=c==r&&r<2?32.0:0.125;
                result.color=float4(float3(MODEL_MATRIX[c][r]*scale,CANVAS_MATRIX[c][r]*scale,SCREEN_MATRIX[c][r]*ss)+.5,1)*input.color;
                result.color*=vertexMap.SampleLevel(vertexSampler,float2(0,0),0);
                float2 p=float2(4+input.uv.x*4,row+input.uv.y*8+float(input.instance)*12)+input.position*1e-20;
                result.position=float4(p.x/size.x*2-1,1-p.y/size.y*2,0,1);result.uv=input.uv;result.custom=input.custom;return result;
            }
            """);
        File.WriteAllText(IOPath.Combine(folder, "matrix.frag.glsl"), """
            #version 450
            layout(location=0) in vec4 color;
            layout(location=0) out vec4 result;
            void main(){result=color;}
            """);
        var invalid = IOPath.Combine(folder, "invalid"); Directory.CreateDirectory(invalid);
        foreach (var name in new[] { "MODEL_MATRIX", "CANVAS_MATRIX", "SCREEN_MATRIX" })
            foreach (var kind in new[] { "scalar", "small", "integer", "array", "Globals", "Instance", "fragment", "image" })
            {
                var field = kind switch { "scalar" => "float " + name, "small" => "mat3 " + name, "integer" => "ivec4 " + name, "array" => "mat4 " + name + "[2]", _ => "mat4 " + name };
                var value = kind switch { "scalar" => name, "small" => name + "[0][0]", "integer" => name + "[0]", "array" => name + "[0][0][0]", _ => name + "[0][0]" };
                var block = kind is "Globals" or "Instance" ? kind : "RendererValues";
                var source = kind == "image" ? "layout(set=0,binding=0) uniform sampler2D " + name + ";\n" : "layout(set=" + (kind == "fragment" ? "3,binding=0" : "1,binding=1") + ",std140) uniform " + block + " { " + field + "; };\n";
                var vertex = kind != "fragment";
                File.WriteAllText(IOPath.Combine(invalid, name + "-" + kind + ".glsl"), "#version 450\n" + source +
                    (vertex ? "layout(location=0) in vec2 pos;layout(location=1) in vec4 color;layout(location=2) in vec2 uv;layout(location=3) in vec4 custom;layout(location=0) out vec4 result;layout(location=1) out vec2 outUV;layout(location=2) out vec4 outCustom;layout(set=1,binding=0,std140) uniform Frame { vec2 size; };\nvoid main(){gl_Position=vec4(pos,0,1);outUV=uv;outCustom=custom;result=color*" + (kind == "image" ? "texture(" + name + ",uv)" : "float(" + value + ")") + ";}" : "layout(location=0) out vec4 result;void main(){result=vec4(" + value + ");}"));
            }
    }
}
