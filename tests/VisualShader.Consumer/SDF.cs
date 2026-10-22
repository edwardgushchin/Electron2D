using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static VisualShaderNode[] SDFNodes() => [new VisualShaderNodeTextureSDF(), new VisualShaderNodeTextureSDFNormal(), new VisualShaderNodeSDFRaymarch(), new VisualShaderNodeSDFToScreenUV(), new VisualShaderNodeScreenUVToSDF()];
    private static void SDFCPU()
    {
        foreach (var node in SDFNodes())
        {
            using (node)
            {
                Check(node.GetDefaultInputValues().Count == 0, "SDF constructors have absent defaults.");
                node.SetInputPortDefaultValue(0, new Vector2(-3, 7));
                if (node is VisualShaderNodeSDFRaymarch) node.SetInputPortDefaultValue(1, new Vector2(12, 9));
                using var copy = (VisualShaderNode)node.Duplicate(true);
                Check(copy.GetType() == node.GetType() && copy.GetInputPortDefaultValue<Vector2>(0) == new Vector2(-3, 7), "SDF copies preserve typed definitions.");
                copy.RemoveInputPortDefaultValue(0); Check(node.GetInputPortDefaultValue<Vector2>(0) == new Vector2(-3, 7), "SDF copied defaults are independent.");
                copy.CopyFromResource(node); Check(copy.GetInputPortDefaultValue<Vector2>(0) == new Vector2(-3, 7), "SDF CopyFromResource restores defaults.");
                Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, new Vector2(float.NaN, 0)));
                Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, 1f));
                using var graph = new VisualShader(); using var old = new VisualShaderNodeFloatConstant();
                graph.AddNode(VisualShader.Type.Fragment, old, default, 2); graph.ReplaceNode(VisualShader.Type.Fragment, 2, node.GetType().Name);
                Check(graph.GetNode(VisualShader.Type.Fragment, 2).GetType() == node.GetType(), "SDF registered replacement factories.");
                copy.Dispose(); Reject<ObjectDisposedException>(() => copy.SetInputPortDefaultValue(0, Vector2.Zero));
            }
        }
        Console.WriteLine("SDF typed defaults, absence, independent copies, factories, validation and lifetime passed.");
    }
    internal static void AuthorSDF(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        for (var mode = 0; mode < 21; mode++) SaveSDFGraph(folder, mode);
        foreach (var node in SDFNodes())
        {
            using (node)
            using (var graph = new VisualShader())
            {
                graph.AddNode(VisualShader.Type.Vertex, node, default, 2); graph.ConnectNodes(VisualShader.Type.Vertex, 2, 0, 0, 2);
                ResourceSaver.Save(graph, IOPath.Combine(folder, "invalid", node.GetType().Name + ".e2dres"), SaverFlags.BundleResources);
            }
        }
        Console.WriteLine("Authored 21 SDF programs and five unsupported vertex workflows.");
    }
    private static void SaveSDFGraph(string folder, int mode)
    {
        using var graph = new VisualShader(); var owned = new List<VisualShaderNode>(); var stage = VisualShader.Type.Fragment;
        int Add(VisualShaderNode node) { var id = graph.GetValidNodeID(stage); graph.AddNode(stage, node, default, id); owned.Add(node); return id; }
        void Link(int from, int to, int port = 0, int output = 0) => graph.ConnectNodes(stage, from, output, to, port);
        int Scale(int input, float factor, float offset, int output = 0)
        {
            var multiply = new VisualShaderNodeFloatOp { Operator = VisualShaderNodeFloatOp.OperatorEnum.Multiply }; multiply.SetInputPortDefaultValue(1, factor);
            var value = Add(multiply); Link(input, value, output: output);
            if (offset == 0) return value;
            var add = new VisualShaderNodeFloatOp(); add.SetInputPortDefaultValue(1, offset); var result = Add(add); Link(value, result); return result;
        }
        int Position(Vector2 value)
        { return Add(new VisualShaderNodeVec2Parameter { ParameterName = "sdf_position", DefaultValueEnabled = true, DefaultValue = value }); }
        try
        {
            int result;
            if (mode is <= 2 or 15 or 17 or 18)
            {
                var sample = Add(new VisualShaderNodeTextureSDF());
                if (mode is 1 or 2 or 17 or 18) Link(Position(mode == 1 ? new(60, 40) : new(90, 40)), sample);
                if (mode == 15) graph.GetNode(stage, sample).SetInputPortDefaultValue(0, new Vector2(90, 40));
                result = Scale(sample, 1f / 256, .5f);
                if (mode == 17) { var dead = new VisualShaderNodeSDFRaymarch(); owned.Add(dead); graph.AddNode(VisualShader.Type.Vertex, dead, default, 2); }

            }
            else if (mode is >= 3 and <= 6 or 19)
            {
                var normal = Add(new VisualShaderNodeTextureSDFNormal()); Link(Position(mode switch { 3 or 19 => new(90, 40), 4 => new(40, 40), 5 => new(60, 16), _ => new(60, 72) }), normal);
                var multiply = new VisualShaderNodeVectorOp { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2, Operator = VisualShaderNodeVectorOp.OperatorEnum.Multiply }; multiply.SetInputPortDefaultValue(1, new Vector2(.25f, .25f)); var scaled = Add(multiply); Link(normal, scaled);
                var add = new VisualShaderNodeVectorOp { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 }; add.SetInputPortDefaultValue(1, new Vector2(.5f, .5f)); result = Add(add); Link(scaled, result);
            }
            else if (mode is >= 7 and <= 11 or 16 or 20)
            {
                var ray = new VisualShaderNodeSDFRaymarch(); var id = Add(ray);
                ray.SetInputPortDefaultValue(0, mode switch { 8 => new Vector2(60, 40), 9 => new Vector2(16, 10), 16 => new Vector2(90, 40), _ => new Vector2(16, 40) });
                ray.SetInputPortDefaultValue(1, mode switch { 9 => new Vector2(110, 10), 10 => new Vector2(16, 40), 11 => new Vector2(48, 40), 16 => new Vector2(90, 40), _ => new Vector2(110, 40) });
                var red = Scale(id, 1f / 256, .5f); var green = Add(new VisualShaderNodeSwitch()); Link(id, green, output: 1);
                var split = Add(new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 }); Link(id, split, output: 2);
                var blue = Scale(split, 1f / 192, 0); result = Add(new VisualShaderNodeVectorCompose()); Link(red, result); Link(green, result, 1); Link(blue, result, 2);
            }
            else
            {
                var uv = Add(new VisualShaderNodeScreenUVToSDF());
                if (mode == 12) Link(Add(new VisualShaderNodeVec2Constant { Constant = new(.25f, .75f) }), uv);
                if (mode == 13) graph.GetNode(stage, uv).SetInputPortDefaultValue(0, new Vector2(999, -999));
                result = Add(new VisualShaderNodeSDFToScreenUV());
                if (mode == 14) graph.GetNode(stage, result).SetInputPortDefaultValue(0, new Vector2(96, 24)); else Link(uv, result);
            }
            if (mode >= 18)
            {
                foreach (var name in new[] { "e2d_sdf", "e2d_sdf_2", "e2d_sdf_normal", "e2d_sdf_normal_2", "e2d_sdf_raymarch", "e2d_sdf_raymarch_2" })
                {
                    var image = Add(new VisualShaderNodeImageParameter { ParameterName = name }); var sampleImage = Add(new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port }); Link(image, sampleImage, 2);
                    var multiply = new VisualShaderNodeVectorOp { Operator = VisualShaderNodeVectorOp.OperatorEnum.Multiply }; multiply.SetInputPortDefaultValue(1, Vector3.Zero); var zero = Add(multiply); Link(sampleImage, zero);
                    var add = Add(new VisualShaderNodeVectorOp()); Link(result, add); Link(zero, add, 1); result = add;
                }
            }
            Link(result, 0); graph.AddNode(stage, graph.GetNode(stage, result), default, 900);
            ResourceSaver.Save(graph, IOPath.Combine(folder, $"sdf-{mode:D2}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        }
        finally { foreach (var node in owned) node.Dispose(); }
    }
    internal static void SDFMetadata(string folder, bool compiled = true)
    {
        foreach (var file in Directory.GetFiles(folder, "sdf-*.e2dres"))
        {
            using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep);
            Check(!compiled || graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length > 20, "SDF imported archive retains the authored vertex module.");
            var id = int.Parse(IOPath.GetFileNameWithoutExtension(file).AsSpan(4));
            Check(ReferenceEquals(graph.GetNode(VisualShader.Type.Fragment, 900), graph.GetNode(VisualShader.Type.Fragment, graph.GetNodeConnections(VisualShader.Type.Fragment).Single(c => c.ToNode == 0).FromNode)), "SDF archives retain resource aliases.");
            Check(graph.GetShaderUniformList().All(p => p.Name is not ("SDF_COORDS" or "SDF_TEXTURE" or "SCREEN_PIXEL_SIZE")), "Renderer fields remain outside material descriptors.");
            Reject<ArgumentException>(() => graph.SetDefaultTextureParameter("SDF_TEXTURE", null));
            using var material = new ShaderMaterial { Shader = graph };
            Reject<ArgumentException>(() => material.SetShaderParameter("SDF_COORDS", Vector4.One));
            if (id == 13) Check(graph.GetNode(VisualShader.Type.Fragment, 2).GetInputPortDefaultValue<Vector2>(0) == new Vector2(999, -999), "Implicit screen UV keeps dormant authored defaults.");
            if (id == 17) Check(graph.GetNode(VisualShader.Type.Vertex, 2) is VisualShaderNodeSDFRaymarch, "Unreachable vertex nodes remain authoring data.");
        }
        Console.WriteLine($"21 fresh SDF {(compiled ? "compiled" : "source")} archives, aliases, engine-owned bindings and dormant defaults passed.");
    }
}
