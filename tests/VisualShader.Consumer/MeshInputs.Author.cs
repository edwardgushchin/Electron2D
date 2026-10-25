using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] MeshInputGraphs = ["mesh-nodes", "mesh-expressions", "mesh-global", "mesh-varying", "mesh-context", "mesh-provider"];
    internal static void MeshInputsCPU()
    {
        using var input = new VisualShaderNodeInput { InputName = "custom0" };
        var events = 0; input.InputTypeChanged += () => events++;
        input.InputName = "custom1"; Check(events == 0, "Sibling custom channels share one vector role.");
        input.InputName = "instance_id"; Check(events == 1 && input.GetInputRealName() == "INSTANCE_ID", "Logical instance IDs use signed integer data.");
        using var copy = (VisualShaderNodeInput)input.Duplicate(); Check(copy.InputName == input.InputName, "Copies retain the selected producer.");
        using var graph = new V(); graph.AddNode(V.Type.Vertex, input, default, 2); graph.AddNode(V.Type.Fragment, input, default, 2);
        using var duplicate = (V)graph.DuplicateDeep();
        Check(ReferenceEquals(duplicate.GetNode(V.Type.Vertex, 2), duplicate.GetNode(V.Type.Fragment, 2)), "A duplicated graph preserves shared definition aliases across stage contexts.");
        Console.WriteLine("Mesh input CPU: roles, events, copies and shared stage definitions passed.");
    }
    internal static void AuthorMeshInputs(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        File.WriteAllText(IOPath.Combine(folder, "mesh-process.comp.glsl"), """
            #version 450
            #extension GL_GOOGLE_include_directive : require
            #include "electron2d/particles.glslinc"
            layout(local_size_x=64) in;
            void main() {
                uint index=gl_GlobalInvocationID.x; if (index>=Counts.x) return;
                PARTICLES[index].Custom=vec4(float(index),1.0,.875,1.0);
                PARTICLES[index].VelocityAge.z=float(index);
            }
            """);
        foreach (var name in MeshInputGraphs)
        {
            using var graph = new V(); var owned = new List<Resource>();
            void Add(VisualShaderNode node, V.Type stage, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); }
            try
            {
                var stage = name == "mesh-context" ? V.Type.Fragment : V.Type.Vertex;
                using var expression = new VisualShaderNodeExpression(); expression.AddOutputPort(0, P.Vector3, "observed");
                using var provider = new CustomMeshInputsNode();
                graph.AddNode(stage, name == "mesh-provider" ? provider : expression, default, 2);
                if (name is "mesh-nodes" or "mesh-varying" or "mesh-context")
                {
                    for (var i = 0; i < 3; i++)
                    {
                        var input = new VisualShaderNodeInput { InputName = i == 2 ? "instance_id" : "custom" + i };
                        Add(input, stage, i + 3); expression.AddInputPort(i, stage == V.Type.Fragment ? P.Scalar : i == 2 ? P.ScalarInt : P.Vector4, "value" + i);
                        graph.ConnectNodes(stage, i + 3, 0, 2, i);
                    }
                }
                if (name == "mesh-context") expression.Expression = "observed=(value0==0.0 && value1==0.0 && value2==0.0)?vec3(.25,.5,.75):vec3(1,0,0);";
                else
                {
                    var offset = name is "mesh-nodes" or "mesh-varying" ? 3 : 0;
                    expression.AddInputPort(offset, P.Vector4, "expected0"); expression.AddInputPort(offset + 1, P.Vector4, "expected1"); expression.AddInputPort(offset + 2, P.Scalar, "factor");
                    Add(new VisualShaderNodeVec4Parameter { ParameterName = "expected0" }, stage, 7); Add(new VisualShaderNodeVec4Parameter { ParameterName = "expected1" }, stage, 8);
                    Add(new VisualShaderNodeFloatParameter { ParameterName = "gain", DefaultValueEnabled = true, DefaultValue = 1 }, stage, 9);
                    graph.ConnectNodes(stage, 7, 0, 2, offset); graph.ConnectNodes(stage, 8, 0, 2, offset + 1); graph.ConnectNodes(stage, 9, 0, 2, offset + 2);
                    var a = name is "mesh-nodes" or "mesh-varying" ? "value0" : "CUSTOM0"; var b = name is "mesh-nodes" or "mesh-varying" ? "value1" : "CUSTOM1"; var id = name is "mesh-nodes" or "mesh-varying" ? "value2" : "INSTANCE_ID";
                    var body = $"bool valid = all(lessThan(abs({a}-expected0),vec4(.00002))) && all(lessThan(abs({b}-expected1),vec4(.00002))) && {id} == (INSTANCE_CUSTOM.y>0.0?int(INSTANCE_CUSTOM.x)+(int(INSTANCE_CUSTOM.y)-1)*65536:0); observed=valid?vec3(.25,.5,.75)*factor:vec3(1,0,0);";
                    if (name == "mesh-global")
                    {
                        Add(new VisualShaderNodeGlobalExpression { Expression = "#if ELECTRON2D_SHADER_STAGE == 0\nvec4 mesh_values(){ return CUSTOM0 + CUSTOM1; }\nint logical_id(){ return INSTANCE_ID; }\n#endif" }, stage, 10);
                        body = body.Replace(a + "-expected0", "mesh_values()-expected0-expected1", StringComparison.Ordinal).Replace(id + " ==", "logical_id() ==", StringComparison.Ordinal);
                    }
                    expression.Expression = "float inMeshCustom0=0.0; float inMeshCustom1=0.0; " + body;
                }
                if (name == "mesh-varying")
                {
                    graph.AddVarying("meshResult", default, VisualShaderVaryingType.Vector3);
                    Add(new VisualShaderNodeVaryingSetter { VaryingName = "meshResult", VaryingType = VisualShaderVaryingType.Vector3 }, stage, 11);
                    graph.ConnectNodes(stage, 2, 0, 11, 0);
                    Add(new VisualShaderNodeVaryingGetter { VaryingName = "meshResult", VaryingType = VisualShaderVaryingType.Vector3 }, V.Type.Fragment, 2);
                    graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0);
                }
                else graph.ConnectNodes(stage, 2, 0, 0, stage == V.Type.Vertex ? 2 : 0);
                SaveDomain(graph, folder, name);
                using var restored = ResourceLoader.Load<V>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
                Check(name == "mesh-provider" ? restored.GetNode(stage, 2) is CustomMeshInputsNode : restored.GetNode(stage, 2) is VisualShaderNodeExpression, "Source archives reconstruct mesh input graphs before import.");
            }
            finally { foreach (var value in owned) value.Dispose(); }
        }
        foreach (var name in new[] { "CUSTOM0", "CUSTOM1", "INSTANCE_ID" })
        {
            using var graph = new V(); using var value = new VisualShaderNodeFloatParameter { ParameterName = name };
            graph.AddNode(V.Type.Vertex, value, default, 2); graph.ConnectNodes(V.Type.Vertex, 2, 0, 0, 2); SaveDomain(graph, IOPath.Combine(folder, "invalid"), name);
        }
        Console.WriteLine("Authored six mesh input graphs and three invalid reserved parameter graphs.");
    }
}
