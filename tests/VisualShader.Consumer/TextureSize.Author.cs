using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] TextureSizeGraphs = ["size-vertex", "size-fragment", "size-paired", "size-expression-vertex", "size-expression-fragment"];
    internal static void TextureSizeCPU()
    {
        using var input = new VisualShaderNodeInput { InputName = "texture_pixel_size" };
        Check(input.GetInputRealName() == "TEXTURE_PIXEL_SIZE", "Texture size keeps its named numeric role.");
        using var graph = new V(); graph.AddNode(V.Type.Vertex, input, default, 2); graph.AddNode(V.Type.Fragment, input, default, 2);
        Check(graph.CanConnectNodes(V.Type.Vertex, 2, 0, 0, 0) && graph.CanConnectNodes(V.Type.Fragment, 2, 0, 0, 0), "One borrowed input resolves in both canvas stages.");
        Console.WriteLine("Texture size CPU: named role and shared stage definitions passed.");
    }
    internal static void AuthorTextureSize(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        for (var sample = 0; sample < TextureSizeGraphs.Length; sample++)
        {
            using var graph = new V(); using var input = new VisualShaderNodeInput { InputName = "texture_pixel_size" };
            using var gain = new VisualShaderNodeFloatParameter { ParameterName = "gain", DefaultValueEnabled = true, DefaultValue = 1 };
            var owned = new List<Resource>();
            try
            {
                foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
                {
                    if (sample != 2 && stage != (sample is 0 or 3 ? V.Type.Vertex : V.Type.Fragment)) continue;
                    var expression = new VisualShaderNodeExpression(); owned.Add(expression);
                    expression.AddInputPort(0, P.Scalar, "factor"); expression.AddOutputPort(0, P.Vector3, "observed");
                    if (sample < 3) { expression.AddInputPort(1, P.Vector2, "pixels"); expression.Expression = "observed = vec3(pixels, 0.25) * factor;"; }
                    else expression.Expression = "float e2d_values = 0.0; observed = vec3(TEXTURE_PIXEL_SIZE, 0.25) * factor + vec3(e2d_values);";
                    graph.AddNode(stage, gain, default, 2); graph.AddNode(stage, expression, default, 3);
                    graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 0, stage == V.Type.Vertex ? 2 : 0);
                    if (sample < 3) { graph.AddNode(stage, input, default, 4); graph.ConnectNodes(stage, 4, 0, 3, 1); }
                }
                SaveDomain(graph, folder, TextureSizeGraphs[sample]);
            }
            finally { foreach (var value in owned) value.Dispose(); }
        }
        using (var graph = new V())
        using (var value = new VisualShaderNodeVec2Parameter { ParameterName = "TEXTURE_PIXEL_SIZE" })
        { graph.AddNode(V.Type.Fragment, value, default, 2); graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0); SaveDomain(graph, IOPath.Combine(folder, "invalid"), "reserved-size"); }
        File.WriteAllText(IOPath.Combine(folder, "size.glsl"), "#version 450\nlayout(set=3,binding=0,std140) uniform RendererValues { vec2 TEXTURE_PIXEL_SIZE; float gain; };\nlayout(location=0) out vec4 result;\nvoid main(){result=vec4(vec3(TEXTURE_PIXEL_SIZE,0.25)*gain,1);}\n");
        File.WriteAllText(IOPath.Combine(folder, "size.hlsl"), "cbuffer RendererValues : register(b0,space3) { float2 TEXTURE_PIXEL_SIZE; float gain; };\nfloat4 main() : SV_Target0 { return float4(float3(TEXTURE_PIXEL_SIZE,0.25)*gain,1); }\n");
        foreach (var pair in new[] { ("scalar", "float TEXTURE_PIXEL_SIZE;", "vec4(TEXTURE_PIXEL_SIZE)"), ("integer", "ivec2 TEXTURE_PIXEL_SIZE;", "vec4(vec2(TEXTURE_PIXEL_SIZE),0,1)"), ("array", "vec2 TEXTURE_PIXEL_SIZE[2];", "vec4(TEXTURE_PIXEL_SIZE[0],0,1)") })
            File.WriteAllText(IOPath.Combine(folder, "invalid", pair.Item1 + ".glsl"), "#version 450\nlayout(set=3,binding=0,std140) uniform RendererValues { " + pair.Item2 + " };\nlayout(location=0) out vec4 result;\nvoid main(){result=" + pair.Item3 + ";}\n");
        File.WriteAllText(IOPath.Combine(folder, "invalid", "image.glsl"), "#version 450\nlayout(set=2,binding=0) uniform sampler2D TEXTURE_PIXEL_SIZE;\nlayout(location=0) out vec4 result;\nvoid main(){result=texture(TEXTURE_PIXEL_SIZE,vec2(0));}\n");
        foreach (var block in new[] { "Instance", "Globals" })
            File.WriteAllText(IOPath.Combine(folder, "invalid", block + ".glsl"), "#version 450\nlayout(set=3,binding=0,std140) uniform " + block + " { vec2 TEXTURE_PIXEL_SIZE; };\nlayout(location=0) out vec4 result;\nvoid main(){result=vec4(TEXTURE_PIXEL_SIZE,0,1);}\n");
        Console.WriteLine("Authored five portable texture-size graphs, two source languages and seven invalid interfaces.");
    }
}
