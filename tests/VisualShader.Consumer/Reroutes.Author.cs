using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    internal static void AuthorReroutes(string folder)
    {
        var canvas = System.IO.Path.Combine(folder, "canvas"); var domains = System.IO.Path.Combine(folder, "domains");
        Directory.CreateDirectory(canvas); Directory.CreateDirectory(domains);
        foreach (var stage in ParticleStages.Concat([V.Type.Vertex, V.Type.Fragment, V.Type.TextureBlit]))
            SaveRerouteGraph(stage, stage is V.Type.Vertex or V.Type.Fragment ? canvas : domains);
        using (var graph = new V())
        {
            graph.SetMode(Shader.Mode.Particles);
            graph.GetNode(V.Type.Start, 0).SetInputPortDefaultValue(4, new Vector2(16, 16));
            graph.GetNode(V.Type.Start, 0).SetInputPortDefaultValue(2, new Vector3(.25f, .5f, .75f));
            SaveDomain(graph, domains, "particle-basic");
        }
        for (var count = 1; count <= 4; count++)
        {
            using var graph = new V(); graph.SetMode(Shader.Mode.TextureBlit);
            for (var i = 0; i < count; i++) graph.GetNode(V.Type.TextureBlit, 0).SetInputPortDefaultValue(i, new Vector4((i + 1) / 4f, .5f, .75f, 1));
            SaveDomain(graph, domains, "blit-" + count);
        }
        foreach (var array in new[] { false, true })
            for (var kind = -1; kind < (array ? 3 : 5); kind++) SaveRerouteFallback(domains, array, kind);
        SaveInvalidReroutes(System.IO.Path.Combine(folder, "invalid"));
        Console.WriteLine("Authored eight reroute programs: every numeric role, matrix components, ordinary/array sampler branches and three-node chains.");
    }

    private static void SaveRerouteFallback(string folder, bool array, int kind)
    {
        using var graph = new V(); graph.SetMode(Shader.Mode.TextureBlit); var stage = V.Type.TextureBlit;
        using VisualShaderNodeTextureParameter image = array ? new VisualShaderNodeTextureArrayParameter() : new VisualShaderNodeImageParameter();
        image.ParameterName = "fallback_value";
        if (kind is >= 0 and < 3) image.ColorDefault = (VisualShaderNodeTextureParameter.ColorDefaultEnum)kind;
        else if (kind >= 3) image.TextureType = kind == 3 ? VisualShaderNodeTextureParameter.TextureTypeEnum.NormalMap : VisualShaderNodeTextureParameter.TextureTypeEnum.Anisotropy;
        using var route = new VisualShaderNodeReroute();
        using VisualShaderNode sample = array ? new VisualShaderNodeTextureArray { Source = VisualShaderNodeLayeredSample.SourceEnum.Port } : new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port };
        using var observe = new VisualShaderNodeExpression();
        graph.AddNode(stage, image, default, 2); graph.AddNode(stage, route, default, 3); graph.AddNode(stage, sample, default, 4); graph.AddNode(stage, observe, default, 5);
        graph.ConnectNodes(stage, 2, 0, 3, 0); if (kind == -1) graph.DisconnectNodes(stage, 2, 0, 3, 0);
        graph.ConnectNodes(stage, 3, 0, 4, 2);
        observe.AddInputPort(0, P.Vector4, "rgba"); observe.AddOutputPort(0, P.Vector4, "result");
        var expected = kind switch { -1 or 2 => "vec4(0)", 1 => "vec4(0,0,0,1)", 3 => "vec4(128.0/255.0,128.0/255.0,1,1)", 4 => "vec4(1,128.0/255.0,1,1)", _ => "vec4(1)" };
        observe.Expression = "result=all(lessThan(abs(rgba-" + expected + "),vec4(.00001)))?vec4(.25,.5,.75,1):vec4(1,0,0,1);";
        graph.ConnectNodes(stage, 4, 0, 5, 0); graph.ConnectNodes(stage, 5, 0, 0, 0);
        SaveDomain(graph, folder, "blit-reroute-fallback-" + array + "-" + kind);
    }

    private static void SaveInvalidReroutes(string folder)
    {
        Directory.CreateDirectory(folder);
        for (var kind = 0; kind < 3; kind++)
        {
            using var graph = new V(); using var first = new VisualShaderNodeReroute(); using var second = new VisualShaderNodeReroute();
            using var matrix = new VisualShaderNodeMatrixConstant(); using var image = new VisualShaderNodeTextureArrayParameter();
            using var sample = new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port };
            var stage = V.Type.Fragment; graph.AddNode(stage, first, default, 2); graph.AddNode(stage, second, default, 3);
            if (kind == 0) { graph.ConnectNodesForced(stage, 2, 0, 3, 0); graph.ConnectNodesForced(stage, 3, 0, 2, 0); graph.ConnectNodesForced(stage, 3, 0, 0, 0); }
            else if (kind == 1) { graph.AddNode(stage, matrix, default, 4); graph.ConnectNodesForced(stage, 4, 0, 2, 0); graph.ConnectNodesForced(stage, 2, 0, 0, 0); }
            else { graph.AddNode(stage, image, default, 4); graph.AddNode(stage, sample, default, 5); graph.ConnectNodes(stage, 4, 0, 2, 0); graph.ConnectNodes(stage, 2, 0, 5, 2); graph.ConnectNodes(stage, 5, 0, 0, 0); }
            SaveDomain(graph, folder, "invalid-reroute-" + kind);
        }
    }

    private static void SaveRerouteGraph(V.Type stage, string folder)
    {
        using var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : stage is V.Type.Vertex or V.Type.Fragment ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
        var owned = new List<Resource>(); var next = 2; var checks = new List<string>(); var port = 0;
        T Own<T>(T resource) where T : Resource { owned.Add(resource); return resource; }
        int Add(VisualShaderNode node) { var id = next++; graph.AddNode(stage, node, default, id); return id; }
        var observe = Own(new VisualShaderNodeExpression()); var observer = Add(observe);
        int Chain(int source)
        {
            for (var i = 0; i < 3; i++) { var route = Add(Own(new VisualShaderNodeReroute())); graph.ConnectNodes(stage, source, 0, route, 0); source = route; }
            return source;
        }
        void Observe(int source, P type, string expected)
        {
            var name = "v" + port; observe.AddInputPort(port, type, name); graph.ConnectNodes(stage, source, 0, observer, port++);
            checks.Add(expected.Replace("VALUE", name, StringComparison.Ordinal));
        }
        try
        {
            (VisualShaderNode Node, P Type, string Expected)[] constants =
            [
                (new VisualShaderNodeFloatConstant { Constant = .375f }, P.Scalar, "VALUE==.375"),
                (new VisualShaderNodeIntConstant { Constant = -7 }, P.ScalarInt, "VALUE==-7"),
                (new VisualShaderNodeUIntConstant { Constant = 11 }, P.ScalarUInt, "VALUE==11u"),
                (new VisualShaderNodeVec2Constant { Constant = new(.25f, .5f) }, P.Vector2, "all(equal(VALUE,vec2(.25,.5)))"),
                (new VisualShaderNodeVec3Constant { Constant = new(.125f, .25f, .5f) }, P.Vector3, "all(equal(VALUE,vec3(.125,.25,.5)))"),
                (new VisualShaderNodeVec4Constant { Constant = new(.125f, .25f, .5f, .75f) }, P.Vector4, "all(equal(VALUE,vec4(.125,.25,.5,.75)))"),
                (new VisualShaderNodeBooleanConstant { Constant = true }, P.Boolean, "VALUE"),
                (new VisualShaderNodeMatrixConstant { Constant = new(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16) }, P.Matrix,
                    "all(equal(VALUE[0],vec4(1,5,9,13))) && all(equal(VALUE[1],vec4(2,6,10,14))) && all(equal(VALUE[2],vec4(3,7,11,15))) && all(equal(VALUE[3],vec4(4,8,12,16)))")
            ];
            foreach (var value in constants)
            {
                var source = Chain(Add(Own(value.Node))); Observe(source, value.Type, value.Expected); Observe(source, value.Type, value.Expected);
                Check(((VisualShaderNodeReroute)graph.GetNode(stage, source)).GetPortType() == value.Type, "Authored numeric chains preserve the exact role.");
            }
            foreach (var array in new[] { false, true })
            {
                VisualShaderNodeTextureParameter image = array ? new VisualShaderNodeTextureArrayParameter() : new VisualShaderNodeImageParameter();
                image.ParameterName = array ? "array_value" : "image_value"; Add(Own(image));
                var reference = Add(Own(new VisualShaderNodeParameterRef { ParameterName = image.ParameterName })); var source = Chain(reference);
                for (var branch = 0; branch < 2; branch++)
                {
                    VisualShaderNode sample = array ? new VisualShaderNodeTextureArray { Source = VisualShaderNodeLayeredSample.SourceEnum.Port } : new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port };
                    var sampled = Add(Own(sample)); graph.ConnectNodes(stage, source, 0, sampled, 2);
                    var uv = array ? Add(Own(new VisualShaderNodeVec3Constant { Constant = new(.5f, .5f, 0) })) : Add(Own(new VisualShaderNodeVec2Constant { Constant = new(.5f, .5f) }));
                    graph.ConnectNodes(stage, uv, 0, sampled, 0); sample.SetInputPortDefaultValue(1, 0f);
                    Observe(sampled, P.Vector3, "all(equal(VALUE,vec3(1)))");
                }
                Check(((VisualShaderNodeReroute)graph.GetNode(stage, source)).GetPortType() == P.Sampler, "Graph-local parameter references select opaque sampler chains.");
            }
            observe.AddOutputPort(0, stage == V.Type.TextureBlit ? P.Vector4 : P.Vector3, "result");
            observe.Expression = "result=(" + string.Join(" && ", checks.Select(c => "(" + c + ")")) + (stage == V.Type.TextureBlit ? ")?vec4(.25,.5,.75,1):vec4(1,0,0,1);" : ")?vec3(.25,.5,.75):vec3(1,0,0);");
            graph.ConnectNodes(stage, observer, 0, 0, stage == V.Type.Vertex ? 2 : stage is V.Type.Fragment or V.Type.TextureBlit ? 0 : ColorPort(stage));
            SaveDomain(graph, folder, stage == V.Type.TextureBlit ? "blit-reroute" : stage is V.Type.Vertex or V.Type.Fragment ? "canvas-reroute-" + (int)stage : "particle-reroute-" + (int)stage);
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
    }
}
