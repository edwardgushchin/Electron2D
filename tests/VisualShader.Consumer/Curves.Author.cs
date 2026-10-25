using Electron2D;
using System.Text;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private const int CurveSampleCount = 14;
    private static readonly float[] CurveCoordinates = [0, .25f, .5f, .75f, 1, -1, 2];
    internal static void AuthorCurves(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (var stage in ParticleStages.Concat([V.Type.Vertex, V.Type.Fragment, V.Type.TextureBlit])) SaveCurveStage(folder, stage);
        using var graph = new V(); using var x = CurveLine(.5f, .5f); using var y = CurveLine(.5f, .5f); using var z = CurveLine(.75f, .75f);
        using var scalar = new CurveTexture { Curve = x }; using var xyz = new CurveXYZTexture { CurveX = x, CurveY = y, CurveZ = z };
        using var a = new VisualShaderNodeCurveTexture { Texture = scalar }; using var b = new VisualShaderNodeCurveXYZTexture { Texture = xyz };
        using var observe = new VisualShaderNodeExpression(); observe.AddInputPort(0, P.Vector3, "value"); observe.AddOutputPort(0, P.Vector3, "observed"); observe.Expression = "observed=value*COLOR.rgb;";
        graph.AddNode(V.Type.Vertex, a, default, 2); graph.ConnectNodes(V.Type.Vertex, 2, 0, 0, 2);
        graph.AddNode(V.Type.Fragment, b, default, 2); graph.AddNode(V.Type.Fragment, observe, default, 3);
        graph.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Fragment, 3, 0, 0, 0); SaveDomain(graph, folder, "curves-live");
        using var waiting = new V(); waiting.SetMode(Shader.Mode.TextureBlit); using var uninitialized = new CurveXYZTexture(); using var pending = new VisualShaderNodeCurveXYZTexture { Texture = uninitialized };
        using var opaque = new VisualShaderNodeExpression(); opaque.AddInputPort(0, P.Vector3, "value"); opaque.AddOutputPort(0, P.Vector4, "observed"); opaque.Expression = "observed=vec4(value,1);";
        waiting.AddNode(V.Type.TextureBlit, pending, default, 2); waiting.AddNode(V.Type.TextureBlit, opaque, default, 3); waiting.ConnectNodes(V.Type.TextureBlit, 2, 0, 3, 0); waiting.ConnectNodes(V.Type.TextureBlit, 3, 0, 0, 0); SaveDomain(waiting, folder, "curves-uninitialized");
        Directory.CreateDirectory(System.IO.Path.Combine(folder, "invalid"));
        using var invalid = new V(); using var observer = new VisualShaderNodeExpression(); var samples = new List<VisualShaderNodeCurveTexture>();
        try
        {
            invalid.AddNode(V.Type.Fragment, observer, default, 2); observer.AddOutputPort(0, P.Vector3, "observed");
            for (var i = 0; i < 17; i++) { var sample = new VisualShaderNodeCurveTexture(); samples.Add(sample); invalid.AddNode(V.Type.Fragment, sample, default, i + 3); observer.AddInputPort(i, P.Scalar, "sample" + i); invalid.ConnectNodes(V.Type.Fragment, i + 3, 0, 2, i); }
            observer.Expression = "observed=vec3(" + string.Join("+", Enumerable.Range(0, 17).Select(i => "sample" + i)) + ");"; invalid.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0);
            SaveDomain(invalid, System.IO.Path.Combine(folder, "invalid"), "curves-over-capacity");
        }
        finally { foreach (var sample in samples) sample.Dispose(); }
    }
    private static void SaveCurveStage(string folder, V.Type stage)
    {
        using var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : stage is V.Type.Vertex or V.Type.Fragment ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
        var owned = new List<Resource>(); var next = 2;
        T Own<T>(T value) where T : Resource { owned.Add(value); return value; }
        int Add(VisualShaderNode value) { var id = next++; graph.AddNode(stage, value, default, id); return id; }
        try
        {
            var x = Own(CurveLine(-.5f, 1.5f)); var y = Own(CurveLine(.25f, .75f)); var z = Own(CurveLine(1.25f, -.75f));
            var scalar = Own(new CurveTexture { Curve = x, Width = 32, TextureMode = CurveTexture.TextureModeEnum.Red });
            var xyz = Own(new CurveXYZTexture { CurveX = x, CurveY = y, CurveZ = z, Width = 32 });
            var empty = Own(new CurveTexture { Width = 32 }); var emptyXYZ = Own(new CurveXYZTexture { Width = 32 });
            var missingChannels = Own(new CurveXYZTexture { CurveX = x, Width = 32 });
            var observer = Own(new VisualShaderNodeExpression()); var observerID = Add(observer); var conditions = new List<string>(); var results = new List<string>();
            float Sample(Curve curve, float u)
            {
                var at = Math.Clamp(u * 32 - .5f, 0, 31); var a = (int)MathF.Floor(at); var b = Math.Min(a + 1, 31);
                return Mathf.Lerp(curve.SampleBaked(a / 32f), curve.SampleBaked(b / 32f), at - a);
            }
            for (var row = 0; row < CurveSampleCount; row++)
            {
                var vector = row is 2 or 4 or 6 or 8 or 10 or 12 or 13; var absent = row is 9 or 10; var missing = row is 7 or 8;
                VisualShaderNode sampler = vector ? Own(new VisualShaderNodeCurveXYZTexture { Texture = missing ? null : row == 12 ? emptyXYZ : row == 13 ? missingChannels : xyz }) : Own(new VisualShaderNodeCurveTexture { Texture = missing ? null : row == 11 ? empty : scalar });
                var id = Add(sampler); var u = row < CurveCoordinates.Length ? CurveCoordinates[row] : 0;
                if (absent) sampler.RemoveInputPortDefaultValue(0);
                else if (row == 3)
                {
                    sampler.SetInputPortDefaultValue(0, -9f); var input = Own(new VisualShaderNodeFloatConstant { Constant = u }); graph.ConnectNodes(stage, Add(input), 0, id, 0);
                }
                else if (row != 0) sampler.SetInputPortDefaultValue(0, u);
                var name = "curve" + row; observer.AddInputPort(row, vector ? P.Vector3 : P.Scalar, name); graph.ConnectNodes(stage, id, 0, observerID, row);
                var expected = absent ? Vector3.Zero : missing ? Vector3.One : row is 11 or 12 ? Vector3.Zero : row == 13 ? new Vector3(Sample(x, u), 0, 0) : new Vector3(Sample(x, u), Sample(y, u), Sample(z, u));
                var expectedText = vector ? $"vec3({GLSLFloat(expected.X)},{GLSLFloat(expected.Y)},{GLSLFloat(expected.Z)})" : GLSLFloat(expected.X);
                conditions.Add(vector ? $"all(lessThan(abs({name}-({expectedText})),vec3(.0003)))" : $"abs({name}-({expectedText}))<.0003");
                results.Add($"vec3({name})");
            }
            var text = new StringBuilder("bool valid=true;\n"); foreach (var condition in conditions) text.Append("valid=valid&&(").Append(condition).AppendLine(");");
            observer.AddOutputPort(0, stage == V.Type.TextureBlit ? P.Vector4 : P.Vector3, "observed");
            if (stage == V.Type.TextureBlit)
            {
                text.Append("int pixel=int(UV.x*").Append(results.Count).AppendLine(".0);vec3 raw=vec3(0);");
                for (var row = 0; row < results.Count; row++) text.Append("if(pixel==").Append(row).Append(")raw=").Append(results[row]).AppendLine(";");
                text.AppendLine("observed=vec4(raw,1);status=valid?vec4(0,1,0,1):vec4(1,0,0,1);");
                observer.AddOutputPort(1, P.Vector4, "status"); graph.ConnectNodes(stage, observerID, 1, 0, 1);
            }
            else text.AppendLine("observed=valid?vec3(.25,.5,.75):vec3(1,0,0);");
            observer.Expression = text.ToString(); graph.ConnectNodes(stage, observerID, 0, 0, stage == V.Type.Vertex ? 2 : stage == V.Type.Fragment || stage == V.Type.TextureBlit ? 0 : ColorPort(stage));
            SaveDomain(graph, folder, "curves-" + (int)stage);
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
    }
}
