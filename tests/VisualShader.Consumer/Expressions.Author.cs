using Electron2D;
using IOPath = System.IO.Path;
using E = Electron2D.VisualShaderNodeExpression;
using P = Electron2D.VisualShaderNode.PortType;
using V = Electron2D.VisualShader;
using M4 = System.Numerics.Matrix4x4;

internal static partial class GraphChecks
{
    internal static void AuthorExpressions(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid-expressions"));
        foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
        {
            for (var type = 0; type < 8; type++) for (var mode = 0; mode < 3; mode++) SaveNumericExpression(folder, stage, (P)type, mode);
            for (var kind = 0; kind < 10; kind++) SaveProceduralExpression(folder, stage, kind);
        }
        SaveProceduralExpression(folder, V.Type.Fragment, 10);
        foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment }) SaveProceduralExpression(folder, stage, 11);
        foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment }) SaveProceduralExpression(folder, stage, 12);
        for (var kind = 0; kind < 10; kind++) SaveInvalidExpression(folder, kind);
        Console.WriteLine("Authored 73 expression programs and ten invalid source/ABI workflows.");
    }
    private static VisualShaderNodeParameter ExpressionParameter(P type) => type switch
    {
        P.Scalar => new VisualShaderNodeFloatParameter { DefaultValue = .25f },
        P.ScalarInt => new VisualShaderNodeIntParameter { DefaultValue = 7 },
        P.ScalarUInt => new VisualShaderNodeUIntParameter { DefaultValue = 11 },
        P.Boolean => new VisualShaderNodeBooleanParameter { DefaultValue = true },
        P.Vector2 => new VisualShaderNodeVec2Parameter { DefaultValue = new(.125f, .625f) },
        P.Vector3 => new VisualShaderNodeVec3Parameter { DefaultValue = new(.125f, .5f, .875f) },
        P.Vector4 => new VisualShaderNodeVec4Parameter { DefaultValue = new(.125f, .375f, .625f, .875f) },
        _ => new VisualShaderNodeMatrixParameter { DefaultValue = M4.CreateScale(2, 3, 4) }
    };
    private static void ExpressionDefault(E expression, P type)
    {
        switch (type)
        {
            case P.Scalar: expression.SetInputPortDefaultValue(0, .25f); break;
            case P.ScalarInt: expression.SetInputPortDefaultValue(0, 7); break;
            case P.ScalarUInt: expression.SetInputPortDefaultValue(0, 11u); break;
            case P.Boolean: expression.SetInputPortDefaultValue(0, true); break;
            case P.Vector2: expression.SetInputPortDefaultValue(0, new Vector2(.125f, .625f)); break;
            case P.Vector3: expression.SetInputPortDefaultValue(0, new Vector3(.125f, .5f, .875f)); break;
            case P.Vector4: expression.SetInputPortDefaultValue(0, new Vector4(.125f, .375f, .625f, .875f)); break;
            default: expression.SetInputPortDefaultValue(0, M4.CreateScale(2, 3, 4)); break;
        }
    }
    private static string ExpressionObservation(P type) => type switch
    {
        P.Scalar => "vec3(value)",
        P.ScalarInt or P.ScalarUInt => "vec3(float(value)/16.0)",
        P.Boolean => "vec3(value ? 0.75 : 0.25)",
        P.Vector2 => "vec3(value,0.25)",
        P.Vector3 => "value",
        P.Vector4 => "vec3(value.x,value.z,value.w)",
        _ => "vec3(value[0][0],value[1][1],value[2][2])*0.1"
    };
    private static void SaveNumericExpression(string folder, V.Type stage, P type, int mode)
    {
        using var graph = new V(); using var expression = new E { Size = new(180, -32) }; using var observe = new E(); using var parameter = ExpressionParameter(type);
        expression.AddInputPort(0, type, "sourceData"); expression.AddOutputPort(0, type, "typedValue");
        if (mode != 0) expression.Expression = "typedValue=sourceData;";
        if (mode == 1) ExpressionDefault(expression, type);
        observe.AddInputPort(0, type, "value"); observe.AddOutputPort(0, P.Vector3, "colorValue"); observe.Expression = "colorValue=" + ExpressionObservation(type) + ";";
        graph.AddNode(stage, expression, default, 2); graph.AddNode(stage, expression, default, 900); graph.AddNode(stage, observe, default, 3);
        graph.ConnectNodes(stage, 2, 0, 3, 0); graph.ConnectNodes(stage, 3, 0, 0, stage == V.Type.Vertex ? 2 : 0);
        if (mode == 2) { parameter.ParameterName = "expression_value"; ParameterEnabled(parameter, true); graph.AddNode(stage, parameter, default, 4); graph.ConnectNodes(stage, 4, 0, 2, 0); }
        ResourceSaver.Save(graph, IOPath.Combine(folder, $"expr-numeric-{(int)type}-{mode}-{(int)stage}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
    }
    private static void SaveProceduralExpression(string folder, V.Type stage, int kind)
    {
        using var graph = new V(); using var expression = new E { Size = new(256, 192) }; using var global = new VisualShaderNodeGlobalExpression(); using var extra = new VisualShaderNodeGlobalExpression(); var owned = new List<Resource>();
        var other = stage == V.Type.Vertex ? V.Type.Fragment : V.Type.Vertex;
        expression.AddOutputPort(0, P.Vector3, "colorValue"); graph.AddNode(stage, expression, default, 2);
        switch (kind)
        {
            case 0:
                global.Expression = "const vec3 sharedColor=vec3(0.2,0.4,0.6); vec3 fromOtherStage(){return sharedColor;}";
                expression.Expression = "colorValue=fromOtherStage();"; graph.AddNode(other, global, default, 2); break;
            case 1:
                global.Expression = "float sumValue(float x){float total=0.0;for(int i=0;i<4;i++){total+=x;}return total;}";
                expression.Expression = "float total=0.0;for(int i=0;i<3;i++){if(i!=1)total+=sumValue(0.0625);}colorValue=vec3(total,0.25,0.75);"; graph.AddNode(other, global, default, 2); break;
            case 2:
                expression.AddInputPort(0, P.Scalar, "x"); expression.AddInputPort(1, P.Scalar, "e"); expression.AddInputPort(2, P.ScalarInt, "q"); expression.SetInputPortDefaultValue(2, 2); expression.SetInputPortDefaultValue(0, .25f); expression.SetInputPortDefaultValue(1, .5f);
                expression.Expression = "struct Sample {float x;}; Sample sampleValue; sampleValue.x=0.8; float xExtra=0.1; int hexValue=0x1e+q; colorValue=vec3(x,float(hexValue)/320.0+1e-1*0.0+e*0.0,sampleValue.x); // x e colorValue n2p0\n/* xExtra x e */"; break;
            case 3:
                expression.Expression = "colorValue=vec3(0.9);COLOR=vec4(0.25,0.5,0.75,1.0);return;"; break;
            case 4:
                expression.Expression = "colorValue=vec3(UV,0.25);"; break;
            case 5:
                expression.Expression = "if(UV.x>0.5){COLOR=vec4(0.75,0.5,0.25,1.0);return;}colorValue=vec3(0.25,0.5,0.75);"; break;
            case 6:
                global.Expression = "vec3 e2d_rgb_to_hsv(vec3 x){return x*0.5;} float e2d_graph(float x){return x;}";
                graph.AddNode(other, global, default, 2); expression.AddInputPort(0, P.Vector3, "inputColor"); expression.Expression = "colorValue=e2d_rgb_to_hsv(inputColor)+vec3(e2d_graph(0.0));";
                var color = new VisualShaderNodeColorFunc { Function = VisualShaderNodeColorFunc.FunctionEnum.RGBToHSV }; owned.Add(color); color.SetInputPortDefaultValue(0, new Vector3(.2f, .4f, .6f)); graph.AddNode(stage, color, default, 3); graph.ConnectNodes(stage, 3, 0, 2, 0); break;
            case 7:
                global.Expression = "vec3 Frame(vec3 x){return x;} vec3 n2p0(vec3 x){return x;} vec3 result(vec3 x){return x;} vec3 inColor(vec3 x){return x;} vec3 inPosition(vec3 x){return x;} vec3 outColor(vec3 x){return x;} vec3 size(vec3 x){return x;} vec3 e2d_frame(vec3 x){return x;}"; graph.AddNode(other, global, default, 2);
                expression.Expression = "vec3 n2p0Extra=vec3(0.125,0.375,0.625);colorValue=Frame(n2p0(result(inColor(inPosition(outColor(size(e2d_frame(n2p0Extra))))))));"; break;
            case 8:
                expression.AddInputPort(0, P.Sampler, "image"); expression.Expression = "colorValue=texture(image,UV).rgb;";
                var imageParameter = new VisualShaderNodeImageParameter { ParameterName = "expression_image" }; owned.Add(imageParameter); graph.AddNode(stage, imageParameter, default, 3); graph.ConnectNodes(stage, 3, 0, 2, 0); break;
            case 10:
                expression.Expression = "if(UV.x<0.5)discard;colorValue=vec3(0.75,0.5,0.25);"; break;
            case 11:
                expression.ClearOutputPorts(); expression.AddOutputPort(0, P.Scalar, "red"); expression.AddOutputPort(1, P.Vector2, "gb"); expression.AddOutputPort(2, P.Boolean, "hit"); expression.AddOutputPort(3, P.Matrix, "basis");
                expression.Expression = "red=0.25;gb=vec2(0.5,0.75);hit=true;basis=mat4(2.0);";
                var observe = new E(); owned.Add(observe); observe.AddInputPort(0, P.Scalar, "red"); observe.AddInputPort(1, P.Vector2, "gb"); observe.AddInputPort(2, P.Boolean, "hit"); observe.AddInputPort(3, P.Matrix, "basis"); observe.AddOutputPort(0, P.Vector3, "colorValue");
                observe.Expression = "colorValue=vec3(red*(hit?1.0:0.0),gb.x,gb.y*basis[0][0]*0.5);"; graph.AddNode(stage, observe, default, 3);
                for (var port = 0; port < 4; port++) graph.ConnectNodes(stage, 2, port, 3, port); break;
            case 12:
                global.Expression = "#if ELECTRON2D_SHADER_STAGE == 0\nlayout(set=1,binding=1,std140)uniform UserValues{float raw_value;};\n#else\nlayout(set=3,binding=0,std140)uniform UserValues{float raw_value;};\n#endif\n";
                graph.AddNode(other, global, default, 2); expression.Expression = "colorValue=vec3(raw_value,0.5,0.75);"; break;
            default:
                global.Expression = "float sourceTime(){return TIME*0.0+0.375;}"; graph.AddNode(other, global, default, 2);
                expression.Expression = "colorValue=vec3(sourceTime(),0.5,0.75);"; break;
        }
        try
        {
            graph.AddNode(stage, expression, default, 900); graph.ConnectNodes(stage, kind == 11 ? 3 : 2, 0, 0, stage == V.Type.Vertex ? 2 : 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, $"expr-procedural-{kind}-{(int)stage}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        }
        finally { foreach (var resource in owned) resource.Dispose(); }
    }
    private static void SaveInvalidExpression(string folder, int kind)
    {
        using var graph = new V(); using var expression = new E(); using var global = new VisualShaderNodeGlobalExpression();
        expression.AddOutputPort(0, P.Vector3, "value"); expression.Expression = kind switch
        {
            0 => "value=vec3(;",
            1 => "vec3 nested(){return vec3(1.0);} value=nested();",
            2 => "value=vec3(dFdx(1.0));",
            3 => "value=texture(missingImage,vec2(0)).rgb;",
            4 => "value=vec3(notDeclared());",
            _ => "value=vec3(0.5);"
        };
        var stage = kind == 2 ? V.Type.Vertex : V.Type.Fragment;
        if (kind == 5) { global.Expression = "void main(){}"; graph.AddNode(V.Type.Fragment, global, default, 3); }
        if (kind == 6) { global.Expression = "vec3 duplicate(){return vec3(1.0);}"; graph.AddNode(V.Type.Vertex, global, default, 3); graph.AddNode(V.Type.Fragment, global, default, 3); }
        if (kind == 8) { global.Expression = "#if ELECTRON2D_SHADER_STAGE == 0\nlayout(set=1,binding=4,std140)uniform UserValues{float raw_value;};\n#else\nlayout(set=3,binding=4,std140)uniform UserValues{float raw_value;};\n#endif\n"; graph.AddNode(V.Type.Fragment, global, default, 3); expression.Expression = "value=vec3(raw_value);"; }
        if (kind == 9) { global.Expression = "/*" + new string('x', 4 * 1024 * 1024 - 4) + "*/"; graph.AddNode(V.Type.Vertex, global, default, 3); graph.AddNode(V.Type.Fragment, global, default, 3); }
        if (kind == 7) { expression.SetOutputPortType(0, P.Sampler); using var sample = new VisualShaderNodeTexture { Source = VisualShaderNodeTexture.SourceEnum.Port }; graph.AddNode(stage, sample, default, 4); graph.AddNode(stage, expression, default, 2); graph.ConnectNodes(stage, 2, 0, 4, 2); graph.ConnectNodes(stage, 4, 0, 0, 0); ResourceSaver.Save(graph, IOPath.Combine(folder, "invalid-expressions", $"expr-invalid-{kind}.e2dres"), SaverFlags.BundleResources); return; }
        graph.AddNode(stage, expression, default, 2); graph.ConnectNodes(stage, 2, 0, 0, stage == V.Type.Vertex ? 2 : 0);
        ResourceSaver.Save(graph, IOPath.Combine(folder, "invalid-expressions", $"expr-invalid-{kind}.e2dres"), SaverFlags.BundleResources);
    }
    internal static void ExpressionMetadata(string folder, bool compiled = true)
    {
        var files = Directory.GetFiles(folder, "expr-*.e2dres"); Check(files.Length == 73, "Complete expression archive corpus.");
        foreach (var file in files)
        {
            using var graph = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep); var name = IOPath.GetFileNameWithoutExtension(file); var stage = (V.Type)int.Parse(name.AsSpan(name.LastIndexOf('-') + 1));
            var node = (E)graph.GetNode(stage, 2); Check(ReferenceEquals(node, graph.GetNode(stage, 900)) && node.Size != Vector2.Zero, "Fresh expression aliases and layout.");
            Check(node.GetOutputPortCount() == (name.StartsWith("expr-procedural-11-", StringComparison.Ordinal) ? 4 : 1) && node.GetInputPortCount() >= 0, "Fresh dynamic port schema.");
            Check(!compiled || graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length > 20, "Compiled expression archive retains authored stages.");
            if (compiled && name.StartsWith("expr-numeric-", StringComparison.Ordinal) && name[^3] == '2')
            {
                var kind = (P)(name[13] - '0'); var expectedType = kind switch { P.Scalar => typeof(float), P.ScalarInt => typeof(int), P.ScalarUInt => typeof(uint), P.Boolean => typeof(bool), P.Vector2 => typeof(Vector2), P.Vector3 => typeof(Vector3), P.Vector4 => typeof(Vector4), _ => typeof(float[]) };
                var descriptors = graph.GetShaderUniformList();
                Check(descriptors.Single(p => p.Name == "expression_value").ValueType == expectedType, name + " reflected numeric role: " + string.Join(", ", descriptors.Select(p => p.Name + "=" + p.ValueType.Name)));
            }
            using var copy = (V)graph.Duplicate(true); Check(((E)copy.GetNode(stage, 2)).GetInputs() == node.GetInputs() && ((E)copy.GetNode(stage, 2)).Expression == node.Expression, "Fresh graphs duplicate complete expression state.");
        }
        Console.WriteLine($"73 fresh {(compiled ? "compiled" : "source")} expression archives retain code, schema, aliases, layout and copies.");
    }
}
