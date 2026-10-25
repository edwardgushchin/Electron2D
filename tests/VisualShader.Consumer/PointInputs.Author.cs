using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] PointPrograms = ["node", "expression", "global", "custom", "light", "default", "output-default", "return", "matrix", "fragment-light", "procedural", "raw-glsl", "raw-hlsl"];
    internal static void AuthorPointInputs(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var name in PointPrograms.Where(n => !n.StartsWith("raw-", StringComparison.Ordinal)))
        {
            using var graph = new V(); var owned = new List<Resource>();
            void Add(V.Type stage, VisualShaderNode node, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); }
            try
            {
                if (name is not "default" and not "output-default")
                    Add(V.Type.Vertex, new VisualShaderNodeFloatParameter { ParameterName = "diameter", DefaultValueEnabled = true, DefaultValue = 9 }, 2);
                if (name == "node")
                {
                    Add(V.Type.Vertex, new VisualShaderNodeInput { InputName = "point_size" }, 3);
                    Add(V.Type.Vertex, new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Multiply }, 4);
                    graph.ConnectNodes(V.Type.Vertex, 2, 0, 4, 0); graph.ConnectNodes(V.Type.Vertex, 3, 0, 4, 1); graph.ConnectNodes(V.Type.Vertex, 4, 0, 0, 4);
                }
                else if (name == "output-default") graph.GetNode(V.Type.Vertex, 0).SetInputPortDefaultValue(4, 9f);
                else if (name == "custom")
                {
                    Add(V.Type.Vertex, new CustomProcedureNode { Kind = 23 }, 3); graph.ConnectNodes(V.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Vertex, 3, 0, 0, 2);
                }
                else if (name != "default")
                {
                    var size = new VisualShaderNodeExpression(); size.AddInputPort(0, P.Scalar, "diameter");
                    size.AddOutputPort(0, name == "return" ? P.Vector2 : P.Scalar, "value");
                    size.Expression = name == "return" ? "value=VERTEX; POINT_SIZE=diameter; return;" : name == "global" ? "value=point_scale(diameter);" : "value=POINT_SIZE*diameter;";
                    Add(V.Type.Vertex, size, 3); graph.ConnectNodes(V.Type.Vertex, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Vertex, 3, 0, 0, name == "return" ? 0 : 4);
                }
                if (name == "procedural")
                {
                    var position = new VisualShaderNodeExpression(); position.AddInputPort(0, P.ScalarInt, "identity"); position.AddOutputPort(0, P.Vector2, "position"); position.Expression = "position=vec2(16.5+float(identity)*24.0,16.5);";
                    Add(V.Type.Vertex, position, 9); Add(V.Type.Vertex, new VisualShaderNodeInput { InputName = "vertex_id" }, 10); graph.ConnectNodes(V.Type.Vertex, 10, 0, 9, 0); graph.ConnectNodes(V.Type.Vertex, 9, 0, 0, 0);
                }
                if (name is "global" or "matrix")
                    Add(V.Type.Fragment, new VisualShaderNodeGlobalExpression { Expression = "#if ELECTRON2D_SHADER_STAGE == 0\nfloat point_scale(float size){return POINT_SIZE*size;}\n#else\nvec3 point_color(){return vec3(POINT_COORD,.75);}\n#endif" }, 8);
                if (name is "default" or "matrix")
                {
                    var matrix = name == "matrix"; graph.AddVarying("witness", V.VaryingMode.VertexToFragmentLight, matrix ? T.Transform : T.Float);
                    var setter = new VisualShaderNodeVaryingSetter { VaryingName = "witness", VaryingType = matrix ? T.Transform : T.Float };
                    if (matrix) setter.SetInputPortDefaultValue(0, new System.Numerics.Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, .75f));
                    else { Add(V.Type.Vertex, new VisualShaderNodeInput { InputName = "point_size" }, 4); }
                    Add(V.Type.Vertex, setter, 5); if (!matrix) graph.ConnectNodes(V.Type.Vertex, 4, 0, 5, 0);
                    Add(V.Type.Fragment, new VisualShaderNodeVaryingGetter { VaryingName = "witness", VaryingType = matrix ? T.Transform : T.Float }, 5);
                }
                var stage = name is "light" or "fragment-light" ? V.Type.Light : V.Type.Fragment;
                if (name == "custom")
                {
                    Add(stage, new CustomProcedureNode { Kind = 23 }, 2); graph.ConnectNodes(stage, 2, 0, 0, 0);
                }
                else
                {
                    var color = new VisualShaderNodeExpression(); color.AddOutputPort(0, P.Vector3, "rgb");
                    color.Expression = name switch { "global" => "rgb=point_color();", "matrix" => "rgb=vec3(POINT_COORD,witness[3][3]);", "default" => "rgb=vec3(POINT_COORD,witness*.75);", "fragment-light" => "rgb=distance(saved,POINT_COORD)<.0001?vec3(saved,.75):vec3(1,0,0);", _ => "rgb=vec3(POINT_COORD,.75);" };
                    if (name is "node" or "light")
                    {
                        Add(stage, new VisualShaderNodeInput { InputName = "point_coord" }, 2); color.AddInputPort(0, P.Vector2, "coordinate"); color.Expression = "rgb=vec3(coordinate,.75);";
                    }
                    if (name is "default" or "matrix") color.AddInputPort(0, name == "matrix" ? P.Matrix : P.Scalar, "witness");
                    if (name == "fragment-light")
                    {
                        graph.AddVarying("saved", V.VaryingMode.FragmentToLight, T.Vector2);
                        Add(V.Type.Fragment, new VisualShaderNodeInput { InputName = "point_coord" }, 2);
                        Add(V.Type.Fragment, new VisualShaderNodeVaryingSetter { VaryingName = "saved", VaryingType = T.Vector2 }, 3); graph.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0);
                        Add(stage, new VisualShaderNodeVaryingGetter { VaryingName = "saved", VaryingType = T.Vector2 }, 2); color.AddInputPort(0, P.Vector2, "saved");
                    }
                    Add(stage, color, 3);
                    if (name is "node" or "light" or "fragment-light") graph.ConnectNodes(stage, 2, 0, 3, 0);
                    if (name is "default" or "matrix") graph.ConnectNodes(stage, 5, 0, 3, 0);
                    graph.ConnectNodes(stage, 3, 0, 0, 0);
                }
                graph.GetNode(V.Type.Fragment, 0).SetInputPortDefaultValue(1, 1f);
                if (stage == V.Type.Light)
                {
                    graph.GetNode(V.Type.Fragment, 0).SetInputPortDefaultValue(0, Vector3.One);
                    graph.GetNode(V.Type.Light, 0).SetInputPortDefaultValue(1, .25f);
                }
                SaveDomain(graph, folder, "point-" + name);
            }
            finally { foreach (var resource in owned) resource.Dispose(); }
        }
        var support = IOPath.Combine(folder, "support"); Directory.CreateDirectory(support);
        using var process = new V(); process.SetMode(Shader.Mode.Particles); SaveDomain(process, support, "point-process");
        foreach (var name in new[] { "vertex-coordinate", "fragment-size", "coordinate-write", "light-size" })
        {
            using var graph = new V(); using var node = new VisualShaderNodeExpression();
            var stage = name == "vertex-coordinate" ? V.Type.Vertex : name == "light-size" ? V.Type.Light : V.Type.Fragment;
            node.AddOutputPort(0, stage == V.Type.Vertex ? P.Vector2 : P.Vector3, "value");
            node.Expression = name switch { "vertex-coordinate" => "value=POINT_COORD;", "fragment-size" or "light-size" => "value=vec3(POINT_SIZE);", _ => "POINT_COORD=vec2(0);value=vec3(1);" };
            graph.AddNode(stage, node, default, 2); graph.ConnectNodes(stage, 2, 0, 0, 0);
            Directory.CreateDirectory(IOPath.Combine(folder, "invalid")); SaveDomain(graph, IOPath.Combine(folder, "invalid"), "point-" + name);
        }
        AuthorPointRaw(folder); Console.WriteLine($"Authored {PointPrograms.Length} point programs and four invalid stage/write programs.");
    }
}
