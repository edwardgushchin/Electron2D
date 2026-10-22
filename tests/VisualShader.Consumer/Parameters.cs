using Electron2D;
using IOPath = System.IO.Path;
using M4 = System.Numerics.Matrix4x4;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;
using K = Electron2D.RenderingServer.GlobalShaderParameterType;

internal static partial class GraphChecks
{
    private static class ParameterMatrices
    { internal static readonly float[] A = MatrixWords(MatrixA), B = MatrixWords(MatrixB); }
    private static float[] MatrixWords(M4 matrix)
    { var words = new float[16]; for (var c = 0; c < 4; c++) for (var r = 0; r < 4; r++) words[c * 4 + r] = matrix[r, c]; return words; }
    private static VisualShaderNodeParameter Parameter(int role) => role switch
    {
        0 => new VisualShaderNodeFloatParameter { DefaultValue = .375f },
        1 => new VisualShaderNodeIntParameter { DefaultValue = unchecked((int)0xa1b2c3d4u) },
        2 => new VisualShaderNodeUIntParameter { DefaultValue = 0xf1e2d3c4u },
        3 => new VisualShaderNodeBooleanParameter { DefaultValue = true },
        4 => new VisualShaderNodeVec2Parameter { DefaultValue = new Vector2(.125f, .625f) },
        5 => new VisualShaderNodeVec3Parameter { DefaultValue = new Vector3(.125f, .5f, .875f) },
        6 => new VisualShaderNodeVec4Parameter { DefaultValue = new Vector4(.125f, .375f, .625f, .875f) },
        7 => new VisualShaderNodeColorParameter { DefaultValue = new Color(.125f, .375f, .625f, .875f) },
        8 => new VisualShaderNodeMatrixParameter { DefaultValue = MatrixA },
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static void ParameterEnabled(VisualShaderNodeParameter node, bool enabled)
    {
        switch (node)
        {
            case VisualShaderNodeFloatParameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeIntParameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeUIntParameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeBooleanParameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeVec2Parameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeVec3Parameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeVec4Parameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeColorParameter p: p.DefaultValueEnabled = enabled; break;
            case VisualShaderNodeMatrixParameter p: p.DefaultValueEnabled = enabled; break;
        }
    }
    private static object ParameterDefault(VisualShaderNodeParameter node) => node switch
    {
        VisualShaderNodeFloatParameter p => p.DefaultValue,
        VisualShaderNodeIntParameter p => p.DefaultValue,
        VisualShaderNodeUIntParameter p => p.DefaultValue,
        VisualShaderNodeBooleanParameter p => p.DefaultValue,
        VisualShaderNodeVec2Parameter p => p.DefaultValue,
        VisualShaderNodeVec3Parameter p => p.DefaultValue,
        VisualShaderNodeVec4Parameter p => p.DefaultValue,
        VisualShaderNodeColorParameter p => p.DefaultValue,
        VisualShaderNodeMatrixParameter p => p.DefaultValue,
        _ => throw new ArgumentException("Unknown parameter.")
    };
    private static void ParametersCPU()
    {
        using var graph = new VisualShader(); using var first = new VisualShaderNodeFloatParameter { ParameterName = " 9 hello world!" };
        graph.AddNode(VisualShader.Type.Vertex, first, default, 2); Check(first.ParameterName == "hello_world", "Parameter names sanitize on initial attachment.");
        using var second = new VisualShaderNodeFloatParameter { ParameterName = "hello_world" }; graph.AddNode(VisualShader.Type.Fragment, second, default, 2);
        Check(second.ParameterName == "hello_world2", "Names are unique across stages.");
        var changes = 0; first.Changed += _ => changes++; first.Qualifier = Q.None; Check(changes == 0, "An unchanged qualifier is quiet.");
        first.ParameterName = first.ParameterName; first.InstanceIndex = 0; Check(changes == 2, "Name/explicit index assignments retain their notification policy.");
        Reject<ArgumentOutOfRangeException>(() => first.InstanceIndex = -1); Reject<ArgumentOutOfRangeException>(() => first.InstanceIndex = 16);
        Reject<ArgumentOutOfRangeException>(() => first.Qualifier = Q.Max); Reject<ArgumentNullException>(() => first.ParameterName = null!);
        Reject<ArgumentException>(() => first.DefaultValue = float.NaN); Reject<ArgumentException>(() => first.Min = float.PositiveInfinity);
        first.DefaultValue = .5f; var before = changes; first.DefaultValue = MathF.BitIncrement(.5f); Check(changes == before && first.DefaultValue == .5f, "Approximate scalar changes preserve stored values.");
        first.Hint = VisualShaderNodeFloatParameter.HintEnum.RangeStep; first.Min = 2; first.Max = -2; first.Step = -.25f;
        Check(first.Min == 2 && first.Max == -2 && first.Step == -.25f, "Hints remain advisory and do not constrain values.");
        Reject<ArgumentOutOfRangeException>(() => first.Hint = VisualShaderNodeFloatParameter.HintEnum.Max);
        using var enumeration = new VisualShaderNodeIntParameter { Hint = VisualShaderNodeIntParameter.HintEnum.Enum, EnumNames = ["Low:-7", "High:23"] };
        var labels = enumeration.EnumNames; labels[0] = "changed"; Check(enumeration.EnumNames[0] == "Low:-7", "Enum labels are copied.");
        Reject<ArgumentNullException>(() => enumeration.EnumNames = [null!]); Reject<ArgumentOutOfRangeException>(() => enumeration.Hint = VisualShaderNodeIntParameter.HintEnum.Max);
        for (var role = 0; role < 9; role++)
        {
            using var node = Parameter(role); using var copy = Parameter(role); node.ParameterName = "sample"; node.Qualifier = Q.Global; node.InstanceIndex = 15; ParameterEnabled(node, true);
            copy.CopyFromResource(node); Check(copy.ParameterName == "sample" && copy.Qualifier == Q.Global && copy.InstanceIndex == 15 && ParameterDefault(copy).Equals(ParameterDefault(node)), "Definition copy retains typed values and dormant slots.");
            var notified = 0; node.Changed += _ => notified++; ParameterEnabled(node, true);
            Check(notified == (role is 4 or 5 or 6 or 8 ? 1 : 0), "Vector/matrix and scalar default-enable notification policies remain distinct.");
            node.Dispose(); Reject<ObjectDisposedException>(() => _ = node.ParameterName);
        }
        using var a = new VisualShader(); using var b = new VisualShader(); using var color = new VisualShaderNodeColorParameter { ParameterName = "shared" };
        using var matrix = new VisualShaderNodeMatrixParameter { ParameterName = "shared" }; using var reference = new VisualShaderNodeParameterRef { ParameterName = "shared" };
        using var decompose = new VisualShaderNodeMatrixDecompose();
        a.AddNode(VisualShader.Type.Fragment, color, default, 2); a.AddNode(VisualShader.Type.Fragment, reference, default, 3);
        b.AddNode(VisualShader.Type.Fragment, matrix, default, 2); b.AddNode(VisualShader.Type.Fragment, reference, default, 3); b.AddNode(VisualShader.Type.Fragment, decompose, default, 4);
        for (var repeat = 0; repeat < 3; repeat++)
        {
            Check(b.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 4, 0) && !b.CanConnectNodes(VisualShader.Type.Fragment, 3, 1, 0, 1), "Matrix references resolve one output in their borrowing graph.");
            Check(a.CanConnectNodes(VisualShader.Type.Fragment, 3, 1, 0, 1) && a.CanConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0), "The same reference independently resolves color RGB/alpha.");
        }
        var descriptor = new PropertyDescriptor<VisualShaderNodeFloatParameter, float>("range", n => n.DefaultValue, (n, v) => n.DefaultValue = v) { Hint = PropertyHint.Range, HintString = "0,1,.1" };
        descriptor.SetValue(first, 3); Check(first.DefaultValue == 3 && descriptor.HintString == "0,1,.1", "Typed hint metadata does not clamp writes.");
        foreach (var hint in Enum.GetValues<PropertyHint>()) if (hint != PropertyHint.Max)
            {
                var item = new PropertyDescriptor<VisualShaderNodeFloatParameter, float>("hint", n => n.DefaultValue) { Hint = hint, HintString = "metadata" };
                Check(item.Hint == hint && item.HintString == "metadata", "Every applicable hint survives immutable provider metadata.");
            }
        Reject<ArgumentOutOfRangeException>(() => _ = new PropertyDescriptor<VisualShaderNodeFloatParameter, float>("invalid", n => n.DefaultValue) { Hint = PropertyHint.Max });
        Reject<ArgumentNullException>(() => _ = new PropertyDescriptor<VisualShaderNodeFloatParameter, float>("invalid", n => n.DefaultValue) { HintString = null! });
        using var container = new ParameterContainer { Value = first };
        var parent = new PropertyDescriptor<ParameterContainer, VisualShaderNodeFloatParameter?>("Value", n => n.Value, (n, v) => n.Value = v);
        var projection = PropertyDescriptor.Through(parent, descriptor);
        Check(projection.Hint == PropertyHint.Range && projection.HintString == "0,1,.1", "Through projections retain immutable leaf hint metadata.");
        Console.WriteLine("Graph parameters: authoring, copies, qualifiers, hints and graph-local reference types passed.");
    }
    private sealed class ParameterContainer : Resource { internal VisualShaderNodeFloatParameter? Value; }
    private static int ParameterVariants(int role) => role == 0 ? 3 : role == 1 ? 4 : 2;
    private static int ParameterObservations(int role) => role == 8 ? 9 : role is 1 or 2 or 6 or 7 ? 2 : 1;
    internal static void AuthorParameters(string folder)
    {
        Directory.CreateDirectory(folder);
        for (var role = 0; role < 9; role++) for (var q = 0; q < 4; q++)
                for (var authored = 0; authored < ParameterVariants(role); authored++) for (var reference = 0; reference < 2; reference++) for (var vertex = 0; vertex < 2; vertex++)
                            for (var observation = 0; observation < ParameterObservations(role); observation++)
                            {
                                if (role == 8 && q >= 2) continue;
                                using var graph = new VisualShader(); using var node = Parameter(role); using var read = new VisualShaderNodeParameterRef();
                                var stage = vertex == 1 ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
                                var declarationStage = reference == 1 ? (vertex == 1 ? VisualShader.Type.Fragment : VisualShader.Type.Vertex) : stage;
                                node.ParameterName = $"p_{role}_{authored}"; node.Qualifier = (Q)q; node.InstanceIndex = 15; ParameterEnabled(node, authored == 1 && q != 1);
                                if (node is VisualShaderNodeFloatParameter f)
                                { f.Hint = authored == 0 ? VisualShaderNodeFloatParameter.HintEnum.Range : authored == 1 ? VisualShaderNodeFloatParameter.HintEnum.RangeStep : VisualShaderNodeFloatParameter.HintEnum.None; f.Min = authored == 0 ? -1f : -.5f; f.Max = authored == 0 ? .25f : .5f; f.Step = .125f; }
                                if (node is VisualShaderNodeIntParameter i)
                                { i.Hint = authored == 0 ? VisualShaderNodeIntParameter.HintEnum.RangeStep : authored == 1 ? VisualShaderNodeIntParameter.HintEnum.Enum : authored == 2 ? VisualShaderNodeIntParameter.HintEnum.None : VisualShaderNodeIntParameter.HintEnum.Range; i.Min = authored == 3 ? -8 : -5; i.Max = authored == 3 ? 8 : 5; i.Step = 2; i.EnumNames = ["Низкий:-7", "High:23"]; }
                                graph.AddNode(declarationStage, node, default, 2); read.ParameterName = node.ParameterName;
                                if (reference == 1) graph.AddNode(stage, read, default, 2);
                                var owned = new List<VisualShaderNode>();
                                try
                                {
                                    if (role == 8) ObserveMatrix(graph, stage, 2, observation, owned);
                                    else if (role is 1 or 2) EncodeInteger(graph, stage, 2, observation == 1, owned);
                                    else if (role is 6 or 7 && observation == 1)
                                    {
                                        if (role == 7 && reference == 1) graph.ConnectNodes(stage, 2, 1, 0, vertex == 1 ? 2 : 0);
                                        else { var split = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 }; owned.Add(split); graph.AddNode(stage, split, default, 3); graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 3, 0, vertex == 1 ? 2 : 0); }
                                    }
                                    else graph.ConnectNodes(stage, 2, 0, 0, vertex == 1 ? 2 : 0);
                                    var name = $"parameter-{role}-{q}-{authored}-{reference}-{vertex}-{observation}";
                                    ResourceSaver.Save(graph, IOPath.Combine(folder, name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
                                }
                                finally { foreach (var item in owned) item.Dispose(); }
                            }
        AuthorParameterLayouts(folder);
        AuthorInvalidParameters(folder);
        Console.WriteLine("Typed parameter programs authored.");
    }
    private static int[] ParameterParts(string name) => name.Split('-').Skip(1).Select(int.Parse).ToArray();
    private static Color ParameterExpected(string name, int phase)
    {
        if (name.StartsWith("parameter-layout-", StringComparison.Ordinal)) return ParameterLayoutExpected(name, phase);
        var p = ParameterParts(name); var role = p[0]; var second = phase == 1; var enabled = phase == 1 || p[1] == 1 || p[2] == 1;
        if (p[1] == 1 && phase != 1) second = p[2] == 1;
        if (role == 8) return MatrixObserved(MatrixData(enabled ? (second ? MatrixB : MatrixA) : M4.Identity), p[5]);
        uint value = role == 1 ? (second ? 0x10293847u : 0xa1b2c3d4u) : second ? 0x89abcdefu : 0xf1e2d3c4u;
        if (role is 1 or 2)
        {
            if (!enabled) value = 0;
            if (p[5] == 1) { var high = (value >> 24) / 255f; return new(high, high, high, 1); }
            return new((value & 255) / 255f, ((value >> 8) & 255) / 255f, ((value >> 16) & 255) / 255f, 1);
        }
        if (!enabled) return new(0, 0, 0, 1);
        return role switch
        {
            0 => new(second ? .625f : .375f, second ? .625f : .375f, second ? .625f : .375f, 1),
            3 => second ? new(0, 0, 0, 1) : new(1, 1, 1, 1),
            4 => second ? new(.75f, .25f, 0, 1) : new(.125f, .625f, 0, 1),
            5 => second ? new(.75f, .25f, .5f, 1) : new(.125f, .5f, .875f, 1),
            _ => p[5] == 1 ? (second ? new(.125f, .125f, .125f, 1) : new(.875f, .875f, .875f, 1)) : (second ? new(.75f, .5f, .25f, 1) : new(.125f, .375f, .625f, 1))
        };
    }
    private static void VerifyParameterArchive(string name, VisualShader graph)
    {
        if (name.StartsWith("parameter-layout-", StringComparison.Ordinal)) { VerifyParameterLayout(name, graph); return; }
        var p = ParameterParts(name); var stage = (p[3] == 1 ? p[4] == 1 : p[4] == 0) ? VisualShader.Type.Fragment : VisualShader.Type.Vertex;
        var declaration = (VisualShaderNodeParameter)graph.GetNode(stage, 2);
        Check(declaration.ParameterName == $"p_{p[0]}_{p[2]}" && declaration.Qualifier == (Q)p[1] && declaration.InstanceIndex == 15, "Archived parameter keeps its name, qualifier and dormant slot.");
        using var expected = Parameter(p[0]); Check(ParameterDefault(declaration).Equals(ParameterDefault(expected)), "Every typed parameter default survives a portable archive.");
        if (declaration is VisualShaderNodeFloatParameter f) Check((int)f.Hint == (p[2] == 0 ? 1 : p[2] == 1 ? 2 : 0) && f.Min == (p[2] == 0 ? -1f : -.5f) && f.Max == (p[2] == 0 ? .25f : .5f) && f.Step == .125f, "Float hint state survives archives.");
        if (declaration is VisualShaderNodeIntParameter i) Check((int)i.Hint == (p[2] == 0 ? 2 : p[2] == 1 ? 3 : p[2] == 2 ? 0 : 1) && i.EnumNames.SequenceEqual(["Низкий:-7", "High:23"]), "Integer enum labels and hint state survive archives.");

        var descriptors = graph.GetShaderUniformList();
        if (p[1] == 1 || p[1] >= 2) Check(!descriptors.Any(d => d.Name == declaration.ParameterName), "Global/per-item fields do not become material properties.");
        else Check(descriptors.Single(d => d.Name == declaration.ParameterName).ValueType == (p[0] == 7 ? typeof(Color) : p[0] == 8 ? typeof(float[]) : ParameterDefault(expected).GetType()), "Imported field keeps its typed descriptor, including Color and numeric matrix carriers.");
        if (p[1] == 0) CheckParameterHint(descriptors.Single(d => d.Name == declaration.ParameterName), p[0], p[2]);
    }
    private static void CheckParameterHint(PropertyDescriptor descriptor, int role, int variant)
    {
        var hint = role == 0 && variant < 2 || role == 1 && variant is 0 or 3 ? PropertyHint.Range : role == 1 && variant == 1 ? PropertyHint.Enum : PropertyHint.None;
        var text = role == 0 ? (variant == 0 ? "-1,0.25" : variant == 1 ? "-0.5,0.5,0.125" : "") : role == 1 ? (variant == 0 ? "-5,5,2" : variant == 1 ? "Низкий:-7,High:23" : variant == 3 ? "-8,8" : "") : "";
        Check(descriptor.Hint == hint && descriptor.HintString == text, "Compiled tooling hints preserve their exact advisory metadata.");
    }
    private static void SetParameterMaterial(ShaderMaterial material, string name, int role, bool second)
    {
        switch (role)
        {
            case 0: material.SetShaderParameter<float>(name, second ? .625f : .375f); break;
            case 1: material.SetShaderParameter<int>(name, second ? 0x10293847 : unchecked((int)0xa1b2c3d4u)); break;
            case 2: material.SetShaderParameter<uint>(name, second ? 0x89abcdefu : 0xf1e2d3c4u); break;
            case 3: material.SetShaderParameter<bool>(name, second ? false : true); break;
            case 4: material.SetShaderParameter<Vector2>(name, second ? new Vector2(.75f, .25f) : new Vector2(.125f, .625f)); break;
            case 5: material.SetShaderParameter<Vector3>(name, second ? new Vector3(.75f, .25f, .5f) : new Vector3(.125f, .5f, .875f)); break;
            case 6: material.SetShaderParameter<Vector4>(name, second ? new Vector4(.75f, .5f, .25f, .125f) : new Vector4(.125f, .375f, .625f, .875f)); break;
            case 7: material.SetShaderParameter<Color>(name, second ? new Color(.75f, .5f, .25f, .125f) : new Color(.125f, .375f, .625f, .875f)); break;
            case 8: material.SetShaderMatrixParameter(name, second ? ParameterMatrices.B : ParameterMatrices.A); break;
        }
    }
    private static void SetParameterInstance(Entity item, string name, int role, bool clear)
    {
        switch (role)
        {
            case 0: item.SetInstanceShaderParameter<float>(name, clear ? null : .625f); break;
            case 1: item.SetInstanceShaderParameter<int>(name, clear ? null : 0x10293847); break;
            case 2: item.SetInstanceShaderParameter<uint>(name, clear ? null : 0x89abcdefu); break;
            case 3: item.SetInstanceShaderParameter<bool>(name, clear ? null : false); break;
            case 4: item.SetInstanceShaderParameter<Vector2>(name, clear ? null : new Vector2(.75f, .25f)); break;
            case 5: item.SetInstanceShaderParameter<Vector3>(name, clear ? null : new Vector3(.75f, .25f, .5f)); break;
            case 6: item.SetInstanceShaderParameter<Vector4>(name, clear ? null : new Vector4(.75f, .5f, .25f, .125f)); break;
            case 7: item.SetInstanceShaderParameter<Color>(name, clear ? null : new Color(.75f, .5f, .25f, .125f)); break;
        }
    }
    private static void AddParameterGlobal(string name, int role, bool second)
    {
        switch (role)
        {
            case 0: RenderingServer.GlobalShaderParameterAdd<float>(name, K.Float, second ? .625f : .375f); break;
            case 1: RenderingServer.GlobalShaderParameterAdd<int>(name, K.Int, second ? 0x10293847 : unchecked((int)0xa1b2c3d4u)); break;
            case 2: RenderingServer.GlobalShaderParameterAdd<uint>(name, K.UInt, second ? 0x89abcdefu : 0xf1e2d3c4u); break;
            case 3: RenderingServer.GlobalShaderParameterAdd<bool>(name, K.Bool, second ? false : true); break;
            case 4: RenderingServer.GlobalShaderParameterAdd<Vector2>(name, K.Vec2, second ? new Vector2(.75f, .25f) : new Vector2(.125f, .625f)); break;
            case 5: RenderingServer.GlobalShaderParameterAdd<Vector3>(name, K.Vec3, second ? new Vector3(.75f, .25f, .5f) : new Vector3(.125f, .5f, .875f)); break;
            case 6: RenderingServer.GlobalShaderParameterAdd<Vector4>(name, K.Vec4, second ? new Vector4(.75f, .5f, .25f, .125f) : new Vector4(.125f, .375f, .625f, .875f)); break;
            case 7: RenderingServer.GlobalShaderParameterAdd<Color>(name, K.Color, second ? new Color(.75f, .5f, .25f, .125f) : new Color(.125f, .375f, .625f, .875f)); break;
            case 8: RenderingServer.GlobalShaderParameterAdd(name, K.Mat4, (ReadOnlySpan<float>)(second ? ParameterMatrices.B : ParameterMatrices.A)); break;
        }
    }
    private static void SetParameterGlobal(string name, int role, bool second)
    {
        switch (role)
        {
            case 0: RenderingServer.GlobalShaderParameterSet<float>(name, second ? .625f : .375f); break;
            case 1: RenderingServer.GlobalShaderParameterSet<int>(name, second ? 0x10293847 : unchecked((int)0xa1b2c3d4u)); break;
            case 2: RenderingServer.GlobalShaderParameterSet<uint>(name, second ? 0x89abcdefu : 0xf1e2d3c4u); break;
            case 3: RenderingServer.GlobalShaderParameterSet<bool>(name, second ? false : true); break;
            case 4: RenderingServer.GlobalShaderParameterSet<Vector2>(name, second ? new Vector2(.75f, .25f) : new Vector2(.125f, .625f)); break;
            case 5: RenderingServer.GlobalShaderParameterSet<Vector3>(name, second ? new Vector3(.75f, .25f, .5f) : new Vector3(.125f, .5f, .875f)); break;
            case 6: RenderingServer.GlobalShaderParameterSet<Vector4>(name, second ? new Vector4(.75f, .5f, .25f, .125f) : new Vector4(.125f, .375f, .625f, .875f)); break;
            case 7: RenderingServer.GlobalShaderParameterSet<Color>(name, second ? new Color(.75f, .5f, .25f, .125f) : new Color(.125f, .375f, .625f, .875f)); break;
            case 8: RenderingServer.GlobalShaderParameterSet(name, (ReadOnlySpan<float>)(second ? ParameterMatrices.B : ParameterMatrices.A)); break;
        }
    }
}
