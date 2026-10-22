using Electron2D;
using CF = Electron2D.VisualShaderNodeColorFunc;
using CO = Electron2D.VisualShaderNodeColorOp;
using UF = Electron2D.VisualShaderNodeUVFunc;
using IP = Electron2D.VisualShaderNodeImageParameter;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void AuthorEffects(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var stage in new[] { VisualShader.Type.Vertex, VisualShader.Type.Fragment })
        {
            for (var function = 0; function < 6; function++) for (var index = 0; index < EffectColors.Length; index++)
                {
                    using var node = new CF { Function = (CF.FunctionEnum)function }; node.SetInputPortDefaultValue(0, EffectColors[index]);
                    SaveEffectNode(folder, $"fx-color-{function}-{index}-{(int)stage}", stage, node);
                }
            for (var operation = 0; operation < 9; operation++) for (var index = 0; index < EffectBlends.Length; index++)
                {
                    var value = EffectBlends[index]; using var node = new CO { Operator = (CO.OperatorEnum)operation };
                    node.SetInputPortDefaultValue(0, value.A); node.SetInputPortDefaultValue(1, value.B);
                    SaveEffectNode(folder, $"fx-blend-{operation}-{index}-{(int)stage}", stage, node);
                }
            for (var function = 0; function < 2; function++) for (var index = 0; index < EffectUVs.Length; index++)
                {
                    var value = EffectUVs[index]; using var node = new UF { Function = (UF.FunctionEnum)function };
                    node.SetInputPortDefaultValue(0, new Vector2(-8, 3)); node.SetInputPortDefaultValue(1, value.Scale); node.SetInputPortDefaultValue(2, value.Pivot);
                    SaveEffectNode(folder, $"fx-uv-{function}-{index}-{(int)stage}", stage, node, value.UV);
                }
            for (var index = 0; index < EffectPolars.Length; index++)
            {
                var value = EffectPolars[index]; using var node = new VisualShaderNodeUVPolarCoord();
                node.SetInputPortDefaultValue(0, new Vector2(-8, 3)); node.SetInputPortDefaultValue(1, value.Center); node.SetInputPortDefaultValue(2, value.Zoom); node.SetInputPortDefaultValue(3, value.Repeat);
                SaveEffectNode(folder, $"fx-polar-{index}-{(int)stage}", stage, node, value.UV);
            }
            for (var function = 0; function < 3; function++)
            {
                using VisualShaderNode node = function < 2 ? new UF { Function = (UF.FunctionEnum)function } : new VisualShaderNodeUVPolarCoord();
                node.SetInputPortDefaultValue(0, new Vector2(-8, 3));
                if (node is UF) { node.SetInputPortDefaultValue(1, new Vector2(1.5f, -.5f)); node.SetInputPortDefaultValue(2, new Vector2(.125f, .25f)); }
                SaveEffectNode(folder, $"fx-implicit-{function}-{(int)stage}", stage, node);
                SaveEffectPipeline(folder, stage, function, false);
            }
            using (var absent = new UF { Function = UF.FunctionEnum.Scaling })
            {
                absent.SetInputPortDefaultValue(1, new Vector2(2, 2)); absent.RemoveInputPortDefaultValue(2);
                SaveEffectNode(folder, $"fx-absence-1-{(int)stage}", stage, absent);
            }
            SaveEffectPipeline(folder, stage, 0, true);
            var boundaries = IOPath.Combine(folder, "effect-boundaries"); Directory.CreateDirectory(boundaries);
            foreach (var operation in new[] { CO.OperatorEnum.Dodge, CO.OperatorEnum.Burn })
            {
                using var node = new CO { Operator = operation }; node.SetInputPortDefaultValue(0, Vector3.One);
                node.SetInputPortDefaultValue(1, operation == CO.OperatorEnum.Dodge ? Vector3.One : Vector3.Zero);
                SaveEffectNode(boundaries, $"fx-divide-{(int)operation}-{(int)stage}", stage, node);
            }
            using var center = new VisualShaderNodeUVPolarCoord(); SaveEffectNode(boundaries, $"fx-center-{(int)stage}", stage, center, new(.5f, .5f));
        }
        Console.WriteLine($"Authored {Directory.GetFiles(folder, "fx-*.e2dres").Length} UV/color graphs and six native arithmetic boundaries.");
    }
    private static void SaveEffectNode(string folder, string name, VisualShader.Type stage, VisualShaderNode node, Vector2? uv = null)
    {
        using var graph = new VisualShader(); using var coordinates = new VisualShaderNodeVec2Constant { Constant = uv ?? Vector2.Zero };
        graph.AddNode(stage, node, default, 2); graph.AddNode(stage, node, default, 900);
        if (uv is not null) { graph.AddNode(stage, coordinates, default, 3); graph.ConnectNodes(stage, 3, 0, 2, 0); }
        graph.ConnectNodes(stage, 2, 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
        ResourceSaver.Save(graph, IOPath.Combine(folder, name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
    }
    private static void SaveEffectPipeline(string folder, VisualShader.Type stage, int operation, bool helper)
    {
        using var graph = new VisualShader(); var owned = new List<VisualShaderNode>();
        int Add(VisualShaderNode node, VisualShader.Type selected) { var id = graph.GetValidNodeID(selected); owned.Add(node); graph.AddNode(selected, node, default, id); return id; }
        void Link(VisualShader.Type selected, int from, int to, int port) => graph.ConnectNodes(selected, from, 0, to, port);
        try
        {
            var fragment = VisualShader.Type.Fragment; var sampleStage = helper ? stage : fragment;
            VisualShaderNode uv = operation < 2 ? new UF { Function = (UF.FunctionEnum)operation } : new VisualShaderNodeUVPolarCoord();
            var coordinate = Add(uv, stage); graph.AddNode(stage, uv, default, 900);
            if (!helper)
            {
                var shift = Add(new VisualShaderNodeVec2Parameter { ParameterName = "effect_shift", DefaultValueEnabled = true, DefaultValue = new(.0625f, -.0625f) }, stage);
                if (uv is UF) { uv.SetInputPortDefaultValue(1, new Vector2(.5f, .5f)); Link(stage, shift, coordinate, 2); }
                else
                {
                    uv.SetInputPortDefaultValue(2, .5f);
                    var add = new VisualShaderNodeVectorOp { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
                    add.SetInputPortDefaultValue(1, new Vector2(.25f, .25f)); var id = Add(add, stage); Link(stage, shift, id, 0); Link(stage, id, coordinate, 1);
                }
                if (stage == VisualShader.Type.Vertex) Link(stage, coordinate, 0, 1);
            }
            var parameter = new IP { ParameterName = helper ? "e2d_rgb_to_hsv" : "effect_image", TextureFilter = VisualShaderNodeTextureParameter.TextureFilterEnum.Nearest };
            var definition = Add(parameter, sampleStage); var sample = Add(new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port }, sampleStage); Link(sampleStage, definition, sample, 2);
            if (!helper && stage == fragment) Link(fragment, coordinate, sample, 0);
            var value = sample;
            if (helper)
            {
                var second = Add(new IP { ParameterName = "e2d_rgb_to_hsv_2", TextureFilter = VisualShaderNodeTextureParameter.TextureFilterEnum.Nearest }, sampleStage);
                var other = Add(new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port }, sampleStage); Link(sampleStage, second, other, 2);
                var add = Add(new VisualShaderNodeVectorOp(), sampleStage); Link(sampleStage, sample, add, 0); Link(sampleStage, other, add, 1);
                var half = new VisualShaderNodeVectorOp { Operator = VisualShaderNodeVectorOp.OperatorEnum.Multiply }; half.SetInputPortDefaultValue(1, new Vector3(.5f, .5f, .5f));
                value = Add(half, sampleStage); Link(sampleStage, add, value, 0);
            }
            var transform = Add(new CF { Function = helper ? CF.FunctionEnum.RGBToHSV : CF.FunctionEnum.Sepia }, sampleStage); Link(sampleStage, value, transform, 0); value = transform;
            if (!helper)
            {
                var blend = new CO(); blend.SetInputPortDefaultValue(1, new Vector3(.1f, .2f, .3f)); value = Add(blend, fragment); Link(fragment, transform, value, 0);
            }
            Link(sampleStage, value, 0, sampleStage == VisualShader.Type.Vertex ? 2 : 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, helper ? $"fx-helper-{(int)stage}.e2dres" : $"fx-pipeline-{operation}-{(int)stage}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        }
        finally { foreach (var node in owned) node.Dispose(); }
    }
    private static void VerifyEffectArchive(string name, VisualShader graph)
    {
        var p = name.Split('-'); var stage = (VisualShader.Type)int.Parse(p[^1]); var node = graph.GetNode(stage, 900);
        Check(graph.GetNodeList(stage).Count(id => ReferenceEquals(graph.GetNode(stage, id), node)) == 2, "Fresh effect archives retain their shared operation definition.");
        if (p[1] == "absence") Check(node.GetInputPortDefaultValue<Vector2>(2) is null, "Fresh Scaling archive preserves absent pivot.");
        if (p[1] is "uv" or "polar" or "implicit") Check(node.GetInputPortDefaultValue<Vector2>(0) == new Vector2(-8, 3), "Dormant numeric UV is preserved independently of the implicit producer.");
    }
}
