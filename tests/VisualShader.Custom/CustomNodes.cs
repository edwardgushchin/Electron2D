using Electron2D;
using P = Electron2D.VisualShaderNode.PortType;
using M4 = System.Numerics.Matrix4x4;

internal static class CustomNodes
{
    internal static Action? SourceHook { get; set; }
    internal static readonly List<string> Calls = [];
    internal static void Register()
    {
        ResourceFileTypes.RegisterResource("checks.CustomNumeric", CreateNumeric);
        ResourceFileTypes.RegisterResource("checks.CustomProcedure", CreateProcedure);
        ResourceFileTypes.RegisterResource("checks.CustomOther", CreateOther);
    }
    private static CustomNumericNode CreateNumeric() => new();
    private static CustomProcedureNode CreateProcedure() => new();
    private static CustomOtherNode CreateOther() => new();
    internal static VisualShaderPortDefault Value(P role) => role switch
    {
        P.Scalar => new VisualShaderPortDefault<float>(0, .25f),
        P.ScalarInt => new VisualShaderPortDefault<int>(0, 7),
        P.ScalarUInt => new VisualShaderPortDefault<uint>(0, 11),
        P.Boolean => new VisualShaderPortDefault<bool>(0, true),
        P.Vector2 => new VisualShaderPortDefault<Vector2>(0, new(.125f, .625f)),
        P.Vector3 => new VisualShaderPortDefault<Vector3>(0, new(.125f, .5f, .875f)),
        P.Vector4 => new VisualShaderPortDefault<Color>(0, new(.125f, .375f, .625f, .875f)),
        _ => new VisualShaderPortDefault<M4>(0, M4.CreateScale(2, 3, 4))
    };
}

internal sealed class CustomNumericNode : VisualShaderNodeCustom
{
    private P _role; private int _mode;
    internal bool Constructed;
    internal CustomNumericNode() { Constructed = true; }
    internal P Role { get => _role; set { _role = value; RefreshDefinition(); } }
    internal int InputMode { get => _mode; set { _mode = value; RefreshDefinition(); } }
    public override int GetInputPortCount() { if (!Constructed) throw new InvalidOperationException("Premature constructor callback."); return 1; }
    public override int GetOutputPortCount() => 1;
    public override P GetInputPortType(int port) => Role;
    public override P GetOutputPortType(int port) => Role;
    public override VisualShaderPortDefault? GetInputPortDefaultValue(int port) => InputMode == 1 ? CustomNodes.Value(Role) : null;
    public override string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, VisualShader.Type type)
    {
        CustomNodes.SourceHook?.Invoke(); CustomNodes.Calls.Add("numeric:" + type);
        if (inputVariables is IList<string> list && !list.IsReadOnly) throw new InvalidOperationException("Input symbol list is mutable.");
        if (inputVariables[0].Length == 0) return "null";
        return outputVariables[0] + "=" + inputVariables[0] + ";";
    }
    protected override Resource CreateDuplicateInstance() => new CustomNumericNode();
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); var copy = (CustomNumericNode)target; copy._role = _role; copy._mode = _mode; }
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        yield return new PropertyDescriptor<CustomNumericNode, P>("Role", n => n.Role, (n, v) => n.Role = v, stored: true);
        yield return new PropertyDescriptor<CustomNumericNode, int>("InputMode", n => n.InputMode, (n, v) => n.InputMode = v, stored: true);
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
    }
}

internal class CustomProcedureNode : VisualShaderNodeCustom
{
    private int _kind;
    internal int Kind { get => _kind; set { _kind = value; RefreshDefinition(); } }
    public override string GetName() => "Compiled procedure";
    public override string GetCategory() => "Checks/Compiled";
    public override string GetDescription() => "A reusable source provider.";
    public override P? GetReturnIconType() => P.Vector3;
    public override bool IsHighEnd() => true;
    public override bool IsAvailable(Shader.Mode mode, VisualShader.Type type) => false;
    public override int GetPropertyCount() => GetOptionIndex(0) == 1 ? 2 : 1;
    public override int GetPropertyDefaultIndex(int index) => index == 0 ? 1 : 0;
    public override string[] GetPropertyOptions(int index) => ["Zero", "One", "Two"];
    public override string GetPropertyName(int index) => "selection";
    public override int GetDefaultInputPort(P type) => type == P.Sampler ? 1 : 0;
    public override int GetInputPortCount() => Kind is 5 or 11 ? 2 : Kind == 8 ? 1 : Kind == 12 ? GetOptionIndex(0) == 0 ? 1 : 2 : 0;
    public override P GetInputPortType(int port) => Kind is 5 or 11 ? port == 0 ? P.Sampler : P.Vector2 : Kind == 12 && port == 0 ? GetOptionIndex(0) == 0 ? P.Boolean : P.Vector4 : P.Scalar;
    public override int GetOutputPortCount() => Kind is 1 or 107 ? 2 : 1;
    public override P GetOutputPortType(int port) => port == 0 ? P.Vector3 : Kind == 107 ? P.Sampler : P.Scalar;
    public override VisualShaderPortDefault? GetInputPortDefaultValue(int port) => Kind == 12 ? port == 0 ? GetOptionIndex(0) == 0 ? new VisualShaderPortDefault<bool>(0, true) : new VisualShaderPortDefault<Color>(0, new(.25f, .5f, .75f, 1)) : new VisualShaderPortDefault<float>(1, .5f) : Kind == 8 ? new VisualShaderPortDefault<float>(0, .25f) :
        port == 1 ? new VisualShaderPortDefault<Vector2>(1, new(.25f, .75f)) : null;
    public override string GetGlobalCode(Shader.Mode mode)
    {
        CustomNodes.Calls.Add("global");
        return Kind switch
        {
            0 => "vec3 customShared(){return vec3(" + (GetOptionIndex(0) == 0 ? "0.125,0.25,0.375" : "0.25,0.5,0.75") + ");}",
            6 => "vec3 Frame(vec3 x){return x;} vec3 n2p0(vec3 x){return x;} vec3 result(vec3 x){return x;} vec3 e2d_graph(vec3 x){return x;}",
            7 => "#if ELECTRON2D_SHADER_STAGE == 0\nlayout(set=1,binding=2,std140) uniform CustomValues {float raw_value;};\n#else\nlayout(set=3,binding=1,std140) uniform CustomValues {float raw_value;};\n#endif\n",
            109 => "layout(set=7,binding=0,std140) uniform InvalidCustomValues {float bad_custom_value;};",
            110 => "/*" + new string('a', 5 * 1024 * 1024) + "*/",
            _ => ""
        };
    }
    public override string GetFuncCode(Shader.Mode mode, VisualShader.Type type)
    {
        CustomNodes.Calls.Add("function:" + type);
        return Kind == 0 ? "vec3 customLocal=customShared();" : Kind == 10 ? "COLOR=vec4(0.25,0.5,0.75,1.0);return;" : Kind == 110 ? "/*" + new string('b', 5 * 1024 * 1024) + "*/" : "";
    }
    public override string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, VisualShader.Type type)
    {
        CustomNodes.SourceHook?.Invoke(); CustomNodes.Calls.Add("local:" + type);
        var output = outputVariables[0];
        return Kind switch
        {
            0 => output + "=customLocal;",
            1 => "float value=0.0;for(int i=0;i<4;i++){if(i!=1)value+=0.125;}" + output + "=vec3(value,0.5,0.75);" + outputVariables[1] + "=0.5;",
            2 => "COLOR=vec4(0.25,0.5,0.75,1.0);return;",
            3 => output + "=vec3(UV,0.25);",
            4 => "if(UV.x<0.5)discard;" + output + "=vec3(0.75,0.5,0.25);",
            5 or 11 => output + "=" + (inputVariables[0].Length == 0 ? "vec3(0.125,0.375,0.625)" : "texture(" + inputVariables[0] + "," + inputVariables[1] + ").rgb") + ";",
            6 => "vec3 n2p0Extra=vec3(0.125,0.375,0.625);" + output + "=Frame(n2p0(result(e2d_graph(n2p0Extra)))); // n2p0 result\n",
            7 => output + "=vec3(raw_value+TIME*0.0,0.5,0.75);",
            8 => inputVariables[0] + "+=0.25;" + output + "=vec3(" + inputVariables[0] + ");",
            9 => "null",
            10 => output + "=vec3(0.9);",
            12 => output + "=" + (GetOptionIndex(0) == 0 ? "vec3(" + inputVariables[0] + " ? 0.75 : 0.25)" : inputVariables[0] + ".rgb*" + inputVariables[1]) + ";",
            100 => "this is invalid GLSL;",
            101 => null!,
            102 => "\ud800",
            103 => new string('a', 8 * 1024 * 1024 + 1),
            104 => type == VisualShader.Type.Vertex ? "discard;" : "",
            105 => Hang(),
            106 => FailMutation(),
            109 => output + "=vec3(bad_custom_value);",
            _ => ""
        };
    }
    private string FailMutation() { SetOptionIndex(0, 0); return ""; }
    private static string Hang() { Thread.Sleep(Timeout.Infinite); return ""; }
    protected override Resource CreateDuplicateInstance() => new CustomProcedureNode();
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); ((CustomProcedureNode)target)._kind = _kind; }
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        yield return new PropertyDescriptor<CustomProcedureNode, int>("Kind", n => n.Kind, (n, v) => n.Kind = v, stored: true);
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
    }
}

internal sealed class CustomOtherNode : CustomProcedureNode
{
    public override string GetGlobalCode(Shader.Mode mode) => "vec3 customOther(){return vec3(0.5,0.25,0.125);}";
    public override string GetFuncCode(Shader.Mode mode, VisualShader.Type type) => "vec3 customOtherLocal=customOther();";
    public override string GetCode(IReadOnlyList<string> inputVariables, IReadOnlyList<string> outputVariables, Shader.Mode mode, VisualShader.Type type) => outputVariables[0] + "=customOtherLocal;";
    protected override Resource CreateDuplicateInstance() => new CustomOtherNode();
}
