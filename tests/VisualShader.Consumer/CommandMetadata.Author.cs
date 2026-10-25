using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private static readonly string[] CommandMetadataPrograms = ["region-node", "specular-node", "region-expression", "specular-expression", "global", "uv-write", "custom", "light", "raw-glsl", "raw-hlsl"];
    internal static void AuthorCommandMetadata(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in CommandMetadataPrograms)
        {
            if (name.StartsWith("raw-", StringComparison.Ordinal)) continue;
            using var graph = new V(); var owned = new List<Resource>();
            void Add(VisualShaderNode node, int id) { owned.Add(node); graph.AddNode(V.Type.Fragment, node, default, id); }
            try
            {
                Add(new VisualShaderNodeFloatParameter { ParameterName = "component", DefaultValueEnabled = true }, 2);
                if (name == "custom")
                {
                    Add(new CustomProcedureNode { Kind = 22 }, 4); graph.ConnectNodes(V.Type.Fragment, 2, 0, 4, 0); graph.ConnectNodes(V.Type.Fragment, 4, 0, 0, 0);
                    SaveDomain(graph, folder, "metadata-" + name); continue;
                }
                var observe = new VisualShaderNodeExpression(); observe.AddInputPort(0, P.Scalar, "component"); observe.AddOutputPort(0, P.Vector3, "rgb");
                var field = name.StartsWith("region", StringComparison.Ordinal) ? "REGION_RECT" : "SPECULAR_SHININESS";
                var prefix = "";
                if (name.EndsWith("-node", StringComparison.Ordinal))
                {
                    Add(new VisualShaderNodeInput { InputName = field.ToLowerInvariant() }, 3); observe.AddInputPort(1, P.Vector4, "value");
                    field = "value"; graph.AddNode(V.Type.Fragment, observe, default, 4); owned.Add(observe); graph.ConnectNodes(V.Type.Fragment, 3, 0, 4, 1);
                }
                else
                {
                    if (name is "global" or "light")
                    {
                        Add(new VisualShaderNodeGlobalExpression { Expression = "#if ELECTRON2D_SHADER_STAGE == 1\nvec4 command_values(){return REGION_RECT+SPECULAR_SHININESS;}\n#endif" }, 3);
                        field = "command_values()";
                    }
                    if (name == "uv-write") prefix = "UV=vec2(.99); ";
                    Add(observe, 4);
                }
                observe.Expression = prefix + "float encoded=(" + field + ")[int(component)]/256.0+.5; rgb=vec3(fract(encoded*255.0),floor(encoded*255.0)/255.0,.75);";
                graph.ConnectNodes(V.Type.Fragment, 2, 0, 4, 0); graph.ConnectNodes(V.Type.Fragment, 4, 0, 0, 0);
                graph.GetNode(V.Type.Fragment, 0).SetInputPortDefaultValue(1, 1f);
                if (name == "light")
                {
                    var read = new VisualShaderNodeExpression(); read.AddOutputPort(0, P.Vector3, "rgb");
                    read.Expression = "float encoded=command_values()[int(component)]/256.0+.5; rgb=vec3(fract(encoded*255.0),floor(encoded*255.0)/255.0,.75);";
                    // The material field is shared by the fragment and light functions.
                    read.AddInputPort(0, P.Scalar, "component");
                    var reference = new VisualShaderNodeParameterRef { ParameterName = "component" }; owned.Add(reference); owned.Add(read);
                    graph.AddNode(V.Type.Light, reference, default, 2); graph.AddNode(V.Type.Light, read, default, 3);
                    graph.ConnectNodes(V.Type.Light, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Light, 3, 0, 0, 0); graph.GetNode(V.Type.Light, 0).SetInputPortDefaultValue(1, .125f);
                }
                SaveDomain(graph, folder, "metadata-" + name);
            }
            finally { foreach (var resource in owned) resource.Dispose(); }
        }
        using var process = new V(); process.SetMode(Shader.Mode.Particles);
        Directory.CreateDirectory(System.IO.Path.Combine(folder, "support")); SaveDomain(process, System.IO.Path.Combine(folder, "support"), "metadata-process");
        File.WriteAllText(System.IO.Path.Combine(folder, "metadata-raw-glsl.frag.glsl"), "#version 450\nlayout(location=1) in vec2 uv; layout(location=0) out vec4 result;\nlayout(set=3,binding=0,std140) uniform Values {vec4 REGION_RECT;vec4 CANVAS_SPECULAR;float component;};\nlayout(set=2,binding=0) uniform sampler2D SPECULAR_SHININESS_TEXTURE;\nvoid main(){vec4 value=REGION_RECT+texture(SPECULAR_SHININESS_TEXTURE,uv)*CANVAS_SPECULAR;float encoded=value[int(component)]/256.0+.5;result=vec4(fract(encoded*255.0),floor(encoded*255.0)/255.0,.75,1);}\n");
        File.WriteAllText(System.IO.Path.Combine(folder, "metadata-raw-hlsl.frag.hlsl"), "cbuffer Values:register(b0,space3){float4 REGION_RECT;float4 CANVAS_SPECULAR;float component;};\nTexture2D<float4> SPECULAR_SHININESS_TEXTURE:register(t0,space2);SamplerState specSampler:register(s0,space2);\nstruct Input{[[vk::location(1)]] float2 uv:TEXCOORD0;};\nfloat4 main(Input input):SV_Target{float4 value=REGION_RECT+SPECULAR_SHININESS_TEXTURE.Sample(specSampler,input.uv)*CANVAS_SPECULAR;float encoded=value[int(component)]/256.0+.5;return float4(frac(encoded*255.0),floor(encoded*255.0)/255.0,.75,1);}\n");
        var invalid = System.IO.Path.Combine(folder, "invalid"); Directory.CreateDirectory(invalid);
        foreach (var name in new[] { "region-write", "specular-write", "vertex-region" })
        {
            using var graph = new V(); using var expression = new VisualShaderNodeExpression();
            var stage = name == "vertex-region" ? V.Type.Vertex : V.Type.Fragment;
            expression.AddOutputPort(0, stage == V.Type.Vertex ? P.Vector2 : P.Vector3, "value");
            expression.Expression = name switch { "region-write" => "REGION_RECT=vec4(9);value=vec3(1);", "specular-write" => "SPECULAR_SHININESS=vec4(9);value=vec3(1);", _ => "value=REGION_RECT.xy;" };
            graph.AddNode(stage, expression, default, 2); graph.ConnectNodes(stage, 2, 0, 0, 0); SaveDomain(graph, invalid, "metadata-" + name);
        }
        Console.WriteLine($"Authored {CommandMetadataPrograms.Length} command metadata programs.");
    }
}
