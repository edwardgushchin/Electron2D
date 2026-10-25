using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;
using T = Electron2D.VisualShaderVaryingType;

internal static partial class GraphChecks
{
    private static readonly string[] LightGraphPrograms = ["constant", "builtins", "fragment-varying", "vertex-varying", "context", "values", "vertex-matrix", "parameters", "image", "normal", "specular", "shadow", "carry", "early", "nodes", "global", "custom"];
    private const string LightContextObservation = """
        bool valid=all(equal(LIGHT,vec4(0))) && all(equal(SHADOW_MODULATE,vec4(1))) &&
            all(lessThan(abs(NORMAL-vec3(0,0,1)),vec3(.0001))) &&
            all(lessThan(abs(LIGHT_VERTEX-vec3(FRAGCOORD.xy,0)),vec3(.0001))) &&
            all(lessThan(abs(SCREEN_UV-FRAGCOORD.xy/vec2(256,416)),vec2(.0001))) &&
            all(equal(TEXTURE_PIXEL_SIZE,vec2(1))) && all(equal(COLOR,vec4(1))) &&
            all(equal(SPECULAR_SHININESS,vec4(1))) && LIGHT_ENERGY>=0.0 && LIGHT_ENERGY<=.5 &&
            dot(LIGHT_COLOR.rgb,vec3(1))>.99;
        if(LIGHT_IS_DIRECTIONAL) valid=valid && all(equal(LIGHT_POSITION,vec3(0))) && all(equal(LIGHT_DIRECTION,vec3(0,0,1)));
        else valid=valid && all(lessThan(abs(LIGHT_POSITION-vec3(128,128,2)),vec3(.0001))) &&
            all(lessThan(abs(LIGHT_DIRECTION-normalize(LIGHT_POSITION-LIGHT_VERTEX)),vec3(.0001)));
        rgb=valid?vec3(.25,.5,.75):vec3(1,0,0); alpha=.5;
        """;

    private static void AuthorLightProbes(string folder)
    {
        foreach (var name in LightGraphPrograms.Skip(4))
        {
            using var graph = new V(); using var expression = new VisualShaderNodeExpression();
            var owned = new List<Resource>();
            void Add(V.Type stage, VisualShaderNode node, int id) { owned.Add(node); graph.AddNode(stage, node, default, id); }
            try
            {
                expression.AddOutputPort(0, P.Vector3, "rgb"); expression.AddOutputPort(1, P.Scalar, "alpha");
                expression.Expression = "rgb=vec3(.25,.5,.75); alpha=.5;";
                switch (name)
                {
                    case "context":
                        expression.Expression = LightContextObservation;
                        break;
                    case "nodes":
                        var inputs = new (string Name, P Type, string Symbol)[] {
                            ("light",P.Vector4,"LIGHT"), ("light_color",P.Vector4,"LIGHT_COLOR"), ("light_direction",P.Vector3,"LIGHT_DIRECTION"),
                            ("light_energy",P.Scalar,"LIGHT_ENERGY"), ("light_is_directional",P.Boolean,"LIGHT_IS_DIRECTIONAL"),
                            ("light_position",P.Vector3,"LIGHT_POSITION"), ("light_vertex",P.Vector3,"LIGHT_VERTEX"), ("normal",P.Vector3,"NORMAL"),
                            ("shadow",P.Vector4,"SHADOW_MODULATE"), ("specular_shininess",P.Vector4,"SPECULAR_SHININESS"),
                            ("fragcoord",P.Vector4,"FRAGCOORD"), ("screen_uv",P.Vector2,"SCREEN_UV"), ("texture_pixel_size",P.Vector2,"TEXTURE_PIXEL_SIZE"),
                            ("time",P.Scalar,"TIME"), ("texture",P.Sampler,"TEXTURE"), ("uv",P.Vector2,"UV") };
                        var replacements = inputs.ToDictionary(p => p.Symbol, p => "p_" + p.Name, StringComparer.Ordinal);
                        var observation = LightContextObservation.Replace("rgb=valid", "valid=valid && TIME>=0.0 && all(equal(texture(TEXTURE,UV),vec4(1))); rgb=valid", StringComparison.Ordinal);
                        expression.Expression = System.Text.RegularExpressions.Regex.Replace(observation, @"\b[A-Z_]+\b", m => replacements.GetValueOrDefault(m.Value, m.Value));
                        graph.AddNode(V.Type.Light, expression, default, 20);
                        for (var i = 0; i < inputs.Length; i++)
                        {
                            Add(V.Type.Light, new VisualShaderNodeInput { InputName = inputs[i].Name }, i + 2);
                            expression.AddInputPort(i, inputs[i].Type, "p_" + inputs[i].Name); graph.ConnectNodes(V.Type.Light, i + 2, 0, 20, i);
                        }
                        break;
                    case "global":
                        Add(V.Type.Light, new VisualShaderNodeGlobalExpression { Expression = "#if ELECTRON2D_SHADER_STAGE != 0\nvec3 observe_light(){return LIGHT_COLOR.rgb*LIGHT_ENERGY;}\n#endif" }, 2);
                        expression.Expression = "rgb=observe_light(); alpha=1.0;";
                        break;
                    case "custom":
                        Add(V.Type.Light, new CustomProcedureNode { Kind = 202 }, 2);
                        expression.AddInputPort(0, P.Vector3, "value"); expression.Expression = "rgb=value; alpha=.5;";
                        graph.AddNode(V.Type.Light, expression, default, 20); graph.ConnectNodes(V.Type.Light, 2, 0, 20, 0);
                        break;
                    case "early": expression.Expression = "if(LIGHT_IS_DIRECTIONAL)return; rgb=vec3(.25,.5,.75); alpha=.5;"; break;
                    case "carry":
                        var fragment = new VisualShaderNodeExpression(); fragment.AddOutputPort(0, P.Vector3, "rgb"); fragment.AddOutputPort(1, P.Scalar, "alpha");
                        fragment.Expression = "rgb=vec3(.2,.4,.6); alpha=.8; UV=vec2(.25,.75);";
                        Add(V.Type.Fragment, fragment, 2); graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0); graph.ConnectNodes(V.Type.Fragment, 2, 1, 0, 1);
                        expression.Expression = "bool valid=all(equal(COLOR,vec4(.2,.4,.6,.8)))&&all(equal(UV,vec2(.25,.75))); rgb=valid?vec3(.25,.5,.75):vec3(1,0,0); alpha=.5; COLOR=vec4(99); UV=vec2(99);";
                        break;
                    case "values":
                    case "vertex-matrix":
                        var checks = new List<string>(); var types = name == "values" ? Enumerable.Range(0, 8).Select(i => (T)i) : new[] { T.Transform };
                        var index = 0;
                        foreach (var type in types)
                        {
                            var symbol = "value" + index; var stage = name == "values" ? V.Type.Fragment : V.Type.Vertex;
                            graph.AddVarying(symbol, stage == V.Type.Fragment ? V.VaryingMode.FragmentToLight : V.VaryingMode.VertexToFragmentLight, type);
                            var producer = new VisualShaderNodeExpression(); producer.AddOutputPort(0, (P)type, "value"); producer.Expression = "value=" + VaryingValue(type) + ";";
                            var setter = new VisualShaderNodeVaryingSetter { VaryingName = symbol, VaryingType = type };
                            Add(stage, producer, 2 + index * 2); Add(stage, setter, 3 + index * 2); graph.ConnectNodes(stage, 2 + index * 2, 0, 3 + index * 2, 0);
                            Add(V.Type.Light, new VisualShaderNodeVaryingGetter { VaryingName = symbol, VaryingType = type }, 2 + index);
                            expression.AddInputPort(index, (P)type, symbol); checks.Add(VaryingEqual(type, symbol, VaryingValue(type)));
                            if (name == "values")
                            {
                                var empty = "empty" + index;
                                graph.AddVarying(empty, V.VaryingMode.FragmentToLight, type);
                                Add(V.Type.Light, new VisualShaderNodeVaryingGetter { VaryingName = empty, VaryingType = type }, 32 + index);
                                expression.AddInputPort(8 + index, (P)type, empty);
                                var zero = type switch { T.Int => "0", T.UInt => "0u", T.Boolean => "false", T.Float => "0.0", T.Vector2 => "vec2(0)", T.Vector3 => "vec3(0)", T.Vector4 => "vec4(0)", _ => "mat4(1)" };
                                checks.Add(VaryingEqual(type, empty, zero));
                            }
                            index++;
                        }
                        expression.Expression = "rgb=(" + string.Join(" && ", checks) + ")?vec3(.25,.5,.75):vec3(1,0,0); alpha=.5;";
                        graph.AddNode(V.Type.Light, expression, default, 20);
                        for (var i = 0; i < index; i++) graph.ConnectNodes(V.Type.Light, 2 + i, 0, 20, i);
                        if (name == "values") for (var i = 0; i < index; i++) graph.ConnectNodes(V.Type.Light, 32 + i, 0, 20, 8 + i);
                        break;
                    case "parameters":
                        var gain = new VisualShaderNodeFloatParameter { ParameterName = "light_gain", DefaultValueEnabled = true, DefaultValue = .125f };
                        Add(V.Type.Fragment, gain, 2); graph.AddNode(V.Type.Light, gain, default, 2); graph.ConnectNodes(V.Type.Fragment, 2, 0, 0, 0);
                        Add(V.Type.Light, new VisualShaderNodeFloatParameter { ParameterName = "light_global", Qualifier = VisualShaderNodeParameter.QualifierEnum.Global }, 3);
                        Add(V.Type.Light, new VisualShaderNodeFloatParameter { ParameterName = "light_item", Qualifier = VisualShaderNodeParameter.QualifierEnum.Instance }, 4);
                        for (var i = 0; i < 3; i++) expression.AddInputPort(i, P.Scalar, "p" + i);
                        expression.Expression = "rgb=vec3(p0,p1,p2); alpha=1.0;"; graph.AddNode(V.Type.Light, expression, default, 20);
                        for (var i = 0; i < 3; i++) graph.ConnectNodes(V.Type.Light, 2 + i, 0, 20, i);
                        break;
                    case "image":
                        using (var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8))
                        {
                            image.Fill(new(.25f, .5f, .75f)); var texture = ImageTexture.CreateFromImage(image); owned.Add(texture);
                            Add(V.Type.Light, new VisualShaderNodeTexture { Texture = texture }, 2);
                        }
                        expression.AddInputPort(0, P.Vector4, "sampled"); expression.Expression = "rgb=sampled.rgb; alpha=.5;";
                        graph.AddNode(V.Type.Light, expression, default, 20); graph.ConnectNodes(V.Type.Light, 2, 0, 20, 0);
                        break;
                    case "normal": expression.Expression = "rgb=NORMAL*.25+vec3(.25); alpha=1.0;"; break;
                    case "specular": expression.Expression = "rgb=vec3(SPECULAR_SHININESS.r,SPECULAR_SHININESS.b,SPECULAR_SHININESS.a)*.5; alpha=1.0;"; break;
                    case "shadow": graph.GetNode(V.Type.Light, 0).SetInputPortDefaultValue(2, new Vector3(.5f, .25f, .75f)); break;
                }
                if (name is not ("values" or "vertex-matrix" or "parameters" or "image" or "nodes" or "custom")) graph.AddNode(V.Type.Light, expression, default, 20);
                graph.ConnectNodes(V.Type.Light, 20, 0, 0, 0); graph.ConnectNodes(V.Type.Light, 20, 1, 0, 1);
                SaveDomain(graph, folder, "light-" + name);
            }
            finally { foreach (var resource in owned) resource.Dispose(); }
        }
    }
}
