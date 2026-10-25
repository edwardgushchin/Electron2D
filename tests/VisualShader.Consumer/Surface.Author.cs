using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;

internal static partial class GraphChecks
{
    private static readonly string[] SurfacePrograms = ["normal", "map", "depth", "depth-zero", "depth-negative", "depth-over", "depth-only", "depth-only-empty", "light-vertex", "shadow-vertex", "combined", "auto-normal", "auto-map", "expression", "global", "return", "matrix", "discard", "custom", "parameter", "sample", "procedural"];
    internal static void AuthorSurface(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in SurfacePrograms)
        {
            using var graph = new V(); var owned = new List<Resource>();
            void Add(V.Type stage, VisualShaderNode node, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); }
            try
            {
                var root = graph.GetNode(V.Type.Fragment, 0);
                if (name is "map" or "depth" or "depth-zero" or "depth-negative" or "depth-over" or "combined" or "auto-map" or "parameter")
                {
                    root.SetInputPortDefaultValue(3, new Vector3(.8f, .5f, .8f));
                    root.SetInputPortDefaultValue(4, name switch { "depth" or "combined" => .5f, "depth-zero" => 0f, "depth-negative" => -1f, "depth-over" => 2f, _ => 1f });
                    if (name == "depth") root.SetInputPortDefaultValue(2, new Vector3(0, 1, 0));
                }
                else if (name is "depth-only" or "depth-only-empty") root.SetInputPortDefaultValue(4, .25f);
                else if (name is not ("expression" or "global" or "return" or "sample")) root.SetInputPortDefaultValue(2, new Vector3(.6f, 0, .8f));
                if (name is "light-vertex" or "combined") root.SetInputPortDefaultValue(5, new Vector3(128, 128, 16));
                if (name is "shadow-vertex" or "combined") root.SetInputPortDefaultValue(6, new Vector2(128, 128));
                if (name == "parameter")
                {
                    Add(V.Type.Fragment, new VisualShaderNodeFloatParameter { ParameterName = "surface_depth", DefaultValueEnabled = true, DefaultValue = .5f }, 2);
                    graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 4);
                }
                if (name == "sample")
                {
                    Add(V.Type.Fragment, new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.CanvasNormal }, 2);
                    graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 3);
                }
                if (name == "custom")
                {
                    Add(V.Type.Fragment, new CustomProcedureNode { Kind = 201 }, 2);
                    graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 2);
                }
                if (name is "expression" or "global" or "return" or "discard")
                {
                    if (name == "global") Add(V.Type.Fragment, new VisualShaderNodeGlobalExpression { Expression = "#if ELECTRON2D_SHADER_STAGE != 0\nvoid author_normal(){NORMAL=vec3(.6,0,.8);}\nvec3 read_normal(){return NORMAL;}\n#endif" }, 2);
                    var fragment = new VisualShaderNodeExpression(); fragment.AddOutputPort(0, P.Vector3, "rgb");
                    fragment.Expression = name switch
                    {
                        "global" => "author_normal(); rgb=COLOR.rgb;",
                        "return" => "NORMAL=vec3(.6,0,.8); if(TIME>=0.0)return; rgb=vec3(0);",
                        "discard" => "if(UV.x>.5)discard; NORMAL=vec3(.6,0,.8); rgb=COLOR.rgb;",
                        _ => "NORMAL=vec3(.6,0,.8); rgb=COLOR.rgb;"
                    };
                    Add(V.Type.Fragment, fragment, 3); graph.ConnectNodes(V.Type.Fragment, 3, 0, 0, 0);
                }
                if (name is "matrix" or "procedural")
                {
                    graph.AddVarying("basis", V.VaryingMode.VertexToFragmentLight, T.Transform);
                    var setter = new VisualShaderNodeVaryingSetter { VaryingName = "basis", VaryingType = T.Transform };
                    setter.SetInputPortDefaultValue(0, System.Numerics.Matrix4x4.Identity); Add(V.Type.Vertex, setter, 2);
                    var vertex = new VisualShaderNodeExpression(); vertex.AddOutputPort(0, P.Vector2, "position");
                    vertex.Expression = name == "procedural" ? "position=UV*16.0+vec2(MODEL_MATRIX[0][0]*.01,0);" : "position=(CANVAS_MATRIX*MODEL_MATRIX*vec4(UV*16.0,0,1)).xy+vec2(MODEL_MATRIX[0][0]*.01,0);";
                    Add(V.Type.Vertex, vertex, 3); graph.ConnectNodes(V.Type.Vertex, 3, 0, 0, 0);
                    Add(V.Type.Light, new VisualShaderNodeVaryingGetter { VaryingName = "basis", VaryingType = T.Transform }, 2);
                }
                if (!name.StartsWith("auto-", StringComparison.Ordinal))
                {
                    var observe = new VisualShaderNodeExpression(); observe.AddOutputPort(0, P.Vector3, "rgb");
                    observe.Expression = "rgb=NORMAL*.5+vec3(.5);";
                    if (name == "global") observe.Expression = "rgb=read_normal()*.5+vec3(.5); NORMAL=vec3(99); LIGHT_VERTEX=vec3(99);";
                    if (name == "light-vertex") observe.Expression = "bool valid=all(equal(LIGHT_VERTEX,vec3(128,128,16))); if(!LIGHT_IS_DIRECTIONAL)valid=valid&&all(lessThan(abs(LIGHT_DIRECTION-vec3(0,0,1)),vec3(.0001))); rgb=valid?vec3(.25,.5,.75):vec3(1,0,0);";
                    if (name == "shadow-vertex") observe.Expression = "rgb=vec3(.25,.5,.75);";
                    if (name is "matrix" or "procedural")
                    {
                        observe.AddInputPort(0, P.Matrix, "basis"); observe.Expression = "rgb=(" + VaryingEqual(T.Transform, "basis", "mat4(1)") + ")?NORMAL*.5+vec3(.5):vec3(1,0,0);";
                    }
                    Add(V.Type.Light, observe, 3); graph.ConnectNodes(V.Type.Light, 3, 0, 0, 0);
                    if (name is "matrix" or "procedural") graph.ConnectNodes(V.Type.Light, 2, 0, 3, 0);
                    graph.GetNode(V.Type.Light, 0).SetInputPortDefaultValue(1, .5f);
                }
                SaveDomain(graph, folder, "surface-" + name);
            }
            finally { foreach (var value in owned) value.Dispose(); }
        }
        using var process = new V(); process.SetMode(Shader.Mode.Particles);
        Directory.CreateDirectory(System.IO.Path.Combine(folder, "support"));
        SaveDomain(process, System.IO.Path.Combine(folder, "support"), "surface-process");
        Console.WriteLine($"Authored {SurfacePrograms.Length} canvas surface programs and a retained particle process graph.");
    }
}
