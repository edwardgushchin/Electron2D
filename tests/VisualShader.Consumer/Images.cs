using Electron2D;
using IOPath = System.IO.Path;
using I = Electron2D.VisualShaderNodeTexture;
using IP = Electron2D.VisualShaderNodeTextureParameter;
using LS = Electron2D.VisualShaderNodeLayeredSample;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

internal static partial class GraphChecks
{
    private static void ImagesCPU()
    {
        using var sample = new I(); using var array = new VisualShaderNodeTextureArray();
        using var parameter = new VisualShaderNodeImageParameter(); using var arrayParameter = new VisualShaderNodeTextureArrayParameter();
        Check(sample.Source == I.SourceEnum.Texture && sample.Texture is null && sample.TextureType == I.TextureTypeEnum.Data && sample.GetDefaultInputValues().Count == 0, "Image sampler defaults.");
        Check(array.Source == LS.SourceEnum.Texture && array.TextureArray is null && array.GetDefaultInputValues().Count == 0, "Array sampler defaults.");
        Check(parameter.TextureType == IP.TextureTypeEnum.Data && parameter.ColorDefault == IP.ColorDefaultEnum.White && parameter.TextureFilter == IP.TextureFilterEnum.Default && parameter.TextureRepeat == IP.TextureRepeatEnum.Default && parameter.TextureSource == IP.TextureSourceEnum.None, "Image parameter defaults.");
        var changes = 0; parameter.Changed += _ => changes++;
        parameter.TextureType = IP.TextureTypeEnum.Data; parameter.ColorDefault = IP.ColorDefaultEnum.White;
        parameter.TextureFilter = IP.TextureFilterEnum.Default; parameter.TextureRepeat = IP.TextureRepeatEnum.Default; parameter.TextureSource = IP.TextureSourceEnum.None;
        Check(changes == 0, "Unchanged image policies are quiet.");
        Reject<ArgumentOutOfRangeException>(() => sample.Source = (I.SourceEnum)4);
        Reject<ArgumentOutOfRangeException>(() => sample.Source = I.SourceEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => sample.TextureType = I.TextureTypeEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => array.Source = LS.SourceEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => parameter.TextureType = IP.TextureTypeEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => parameter.ColorDefault = IP.ColorDefaultEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => parameter.TextureFilter = IP.TextureFilterEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => parameter.TextureRepeat = IP.TextureRepeatEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => parameter.TextureSource = (IP.TextureSourceEnum)2);
        Reject<ArgumentOutOfRangeException>(() => parameter.TextureSource = IP.TextureSourceEnum.Max);
        Reject<ArgumentException>(() => sample.SetInputPortDefaultValue(2, 0f));
        Reject<ArgumentException>(() => array.SetInputPortDefaultValue(2, 0f));
        Reject<ArgumentException>(() => sample.SetDefaultInputValues([new VisualShaderPortDefault<float>(2, 0)]));
        Check(sample.GetDefaultInputPort(VisualShaderNode.PortType.Sampler) == 0 && sample.GetDefaultInputValues().Count == 0, "Opaque ports retain base connection policy and have no numeric fallback.");
        using var graph = new VisualShader(); using var number = new VisualShaderNodeFloatConstant();
        graph.AddNode(VisualShader.Type.Fragment, parameter, default, 2); graph.AddNode(VisualShader.Type.Fragment, sample, default, 3); graph.AddNode(VisualShader.Type.Fragment, number, default, 4);
        Check(graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 2) && !graph.CanConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0) && !graph.CanConnectNodes(VisualShader.Type.Fragment, 4, 0, 3, 2), "Opaque ports connect only to opaque ports.");
        using var input = new VisualShaderNodeInput(); var roleChanges = 0; input.InputTypeChanged += () => roleChanges++;
        foreach (var name in new[] { "texture", "normal_texture", "specular_shininess_texture" }) { input.InputName = name; Check(input.GetInputRealName() == name.ToUpperInvariant(), "Canvas sampled-image source names."); }
        Check(roleChanges == 1, "Sampler names share one logical port role.");
        using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); pixels.Fill(new(.25f, .5f, .75f, .625f));
        using var image = ImageTexture.CreateFromImage(pixels); using var layers = new TextureArray(); layers.CreateFromImages([pixels]);
        sample.Texture = image; array.TextureArray = layers;
        using var shallow = (I)sample.Duplicate(); using var deep = (I)sample.Duplicate(true);
        Check(ReferenceEquals(shallow.Texture, image) && deep.Texture is not null && !ReferenceEquals(deep.Texture, image), "Image node duplication preserves shallow and deep resource policy.");
        using var arrayDeep = (VisualShaderNodeTextureArray)array.Duplicate(true);
        Check(arrayDeep.TextureArray is not null && !ReferenceEquals(arrayDeep.TextureArray, layers), "Array node deep duplication copies its image graph.");
        parameter.Changed += _ => throw new InvalidOperationException("Image policy observer");
        Reject<InvalidOperationException>(() => parameter.TextureFilter = IP.TextureFilterEnum.Nearest);
        Check(parameter.TextureFilter == IP.TextureFilterEnum.Nearest, "Image policy commits before observer publication.");
        using var disposed = new I(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.Source = I.SourceEnum.Port);
        Console.WriteLine("Sampled-image graph CPU contracts passed.");
    }

    private static Image GraphImage(int layer = 0)
    {
        using var image = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8);
        var bytes = image.GetData();
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++)
            {
                var at = (y * 4 + x) * 4;
                bytes[at] = layer == 0 ? (byte)(x * 64) : (byte)192;
                bytes[at + 1] = layer == 0 ? (byte)(y * 64) : (byte)64;
                bytes[at + 2] = layer == 0 ? (byte)128 : (byte)32;
                bytes[at + 3] = 160;
            }
        for (var at = 64; at < 80; at += 4) { bytes[at] = layer == 0 ? (byte)32 : (byte)192; bytes[at + 1] = layer == 0 ? (byte)64 : (byte)32; bytes[at + 2] = layer == 0 ? (byte)192 : (byte)64; bytes[at + 3] = layer == 0 ? (byte)224 : (byte)96; }
        bytes[80] = layer == 0 ? (byte)240 : (byte)64; bytes[81] = layer == 0 ? (byte)128 : (byte)192; bytes[82] = layer == 0 ? (byte)32 : (byte)128; bytes[83] = layer == 0 ? (byte)96 : (byte)32;
        return Image.CreateFromData(4, 4, true, Image.Format.Rgba8, bytes);
    }

    internal static void AuthorImages(string folder)
    {
        Directory.CreateDirectory(folder); Directory.CreateDirectory(IOPath.Combine(folder, "invalid"));
        for (var shape = 0; shape < 2; shape++)
        {
            for (var type = 0; type < 4; type++) for (var fallback = 0; fallback < 3; fallback++) for (var alpha = 0; alpha < 2; alpha++)
                        SaveImageGraph(folder, $"img-fallback-{shape}-{type}-{fallback}-{alpha}", shape, type, fallback, alpha: alpha);
            for (var filter = 0; filter < 7; filter++) for (var repeat = 0; repeat < 3; repeat++)
                    SaveImageGraph(folder, $"img-filter-{shape}-{filter}-{repeat}", shape, filter: filter, repeat: repeat, provided: true, lod: true);
            for (var type = 0; type < 4; type++) for (var qualifier = 0; qualifier < 2; qualifier++) for (var vertex = 0; vertex < 2; vertex++) for (var alpha = 0; alpha < 2; alpha++)
                            SaveImageGraph(folder, $"img-param-{shape}-{type}-{qualifier}-{vertex}-{alpha}", shape, type, qualifier: qualifier, vertex: vertex != 0, reference: vertex != 0, provided: true, alpha: alpha);
            for (var vertex = 0; vertex < 2; vertex++)
            {
                SaveImageGraph(folder, $"img-empty-{shape}-{vertex}", shape, vertex: vertex != 0, missingPort: true);
                SaveImageGraph(folder, $"img-property-{shape}-{vertex}", shape, vertex: vertex != 0, property: true, provided: true);
            }
        }
        for (var shape = 0; shape < 2; shape++) for (var layer = 0; layer <= shape; layer++) for (var level = 1; level < 3; level++) for (var alpha = 0; alpha < 2; alpha++)
                        SaveImageGraph(folder, $"img-mip-{shape}-{layer}-{level}-{alpha}", shape, type: 1, filter: 4, repeat: 2, provided: true, lod: true, layer: layer, mip: level, alpha: alpha);
        for (var shape = 0; shape < 2; shape++) for (var layer = 0; layer <= shape; layer++) for (var level = 0; level < 3; level++) for (var type = 0; type < 2; type++) for (var alpha = 0; alpha < 2; alpha++)
                            SaveImageGraph(folder, $"img-device-{shape}-{layer}-{level}-{type}-{alpha}", shape, type: type, filter: 4, repeat: 2, provided: true, lod: true, layer: layer, mip: level, alpha: alpha);
        for (var type = 0; type < 2; type++) for (var qualifier = 0; qualifier < 2; qualifier++) foreach (var filter in new[] { 0, 4 })
                    SaveImageGraph(folder, $"img-screen-{type}-{qualifier}-{filter}", type: type, qualifier: qualifier, filter: filter, screen: true);
        foreach (var source in new[] { 1, 2, 3 }) for (var vertex = 0; vertex < 2; vertex++)
                SaveImageGraph(folder, $"img-source-{source}-{vertex}", source: source, vertex: vertex != 0);
        for (var shape = 0; shape < 2; shape++) for (var type = 0; type < 2; type++) for (var half = 0; half < 2; half++)
                    SaveImageGraph(folder, $"img-float-{shape}-{type}-{half}", shape, type: type, provided: true);
        for (var shape = 0; shape < 2; shape++)
            SaveImageGraph(folder, "img-null-global-" + shape, shape, fallback: 2, qualifier: 1, alpha: 1, symbol: "missing_image_" + shape);
        foreach (var input in new[] { "normal_texture", "specular_shininess_texture" })
            SaveImageGraph(folder, "img-channel-" + input, input: input);
        foreach (var input in new[] { "texture", "normal_texture", "specular_shininess_texture" })
            SaveImageGraph(folder, "img-input-" + input, input: input);
        foreach (var symbol in new[] { "UV", "GLOBAL_literal", "COLOR", "VERTEX", "INSTANCE_CUSTOM", "e2d_values", "e2d_globals", "e2d_instance", "n4p0", "e2d_image_1_4", "Frame", "RendererValues", "Globals", "Instance" })
        {
            SaveImageGraph(folder, "img-symbol-" + symbol, provided: true, symbol: symbol);
            SaveImageGraph(folder, "img-symbol-" + symbol + "-vertex", provided: true, symbol: symbol, vertex: true);
        }
        using (var pair = new VisualShader())
        using (var materialImage = new VisualShaderNodeImageParameter { ParameterName = "GLOBAL_pair" })
        using (var globalImage = new VisualShaderNodeImageParameter { ParameterName = "pair", Qualifier = Q.Global })
        using (var m = new I { Source = I.SourceEnum.Port })
        using (var g = new I { Source = I.SourceEnum.Port })
        using (var add = new VisualShaderNodeVectorOp { Operator = VisualShaderNodeVectorOp.OperatorEnum.Add })
        using (var scale = new VisualShaderNodeVectorOp { Operator = VisualShaderNodeVectorOp.OperatorEnum.Multiply })
        {
            var stage = VisualShader.Type.Fragment;
            pair.AddNode(stage, materialImage, default, 2); pair.AddNode(stage, globalImage, default, 3); pair.AddNode(stage, m, default, 4); pair.AddNode(stage, g, default, 5);
            pair.AddNode(stage, add, default, 6); pair.AddNode(stage, scale, default, 7); pair.AddNode(stage, m, default, 900);
            scale.SetInputPortDefaultValue(1, new Vector3(.5f, .5f, .5f));
            pair.ConnectNodes(stage, 2, 0, 4, 2); pair.ConnectNodes(stage, 3, 0, 5, 2); pair.ConnectNodes(stage, 4, 0, 6, 0); pair.ConnectNodes(stage, 5, 0, 6, 1);
            pair.ConnectNodes(stage, 6, 0, 7, 0); pair.ConnectNodes(stage, 7, 0, 0, 0);
            ResourceSaver.Save(pair, IOPath.Combine(folder, "img-symbol-pair.e2dres"), SaverFlags.BundleResources);
        }
        using (var shared = new VisualShader())
        using (var definition = new VisualShaderNodeImageParameter { ParameterName = "image_value" })
        using (var v = new I { Source = I.SourceEnum.Port })
        using (var f = new I { Source = I.SourceEnum.Port })
        {
            foreach (var stage in new[] { VisualShader.Type.Vertex, VisualShader.Type.Fragment })
            {
                var sample = stage == VisualShader.Type.Vertex ? v : f;
                shared.AddNode(stage, definition, default, 2); shared.AddNode(stage, sample, default, 3); shared.AddNode(stage, sample, default, 900);
                shared.ConnectNodes(stage, 2, 0, 3, 2); shared.ConnectNodes(stage, 3, 0, 0, stage == VisualShader.Type.Vertex ? 1 : 0);
            }
            ResourceSaver.Save(shared, IOPath.Combine(folder, "img-shared.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
        }
        for (var shape = 0; shape < 2; shape++)
            SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-integer-image-" + shape, shape, property: true, provided: true, integerImage: true);
        foreach (var qualifier in new[] { 2, 3 }) SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-instance-" + qualifier, qualifier: qualifier);
        SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-wrong-shape", wrongShape: true);
        SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-screen-array", shape: 1, screen: true);
        SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-screen-vertex", screen: true, vertex: true);
        SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-screen-normal", type: 2, screen: true);
        SaveImageGraph(IOPath.Combine(folder, "invalid"), "img-screen-black", fallback: 1, screen: true);
        foreach (var stage in new[] { VisualShader.Type.Vertex, VisualShader.Type.Fragment })
            SaveImageCapacity(IOPath.Combine(folder, "invalid"), stage);
        Console.WriteLine($"Authored {Directory.GetFiles(folder, "img-*.e2dres").Length} sampled-image programs.");
    }

    private static void SaveImageCapacity(string folder, VisualShader.Type stage)
    {
        using var graph = new VisualShader(); var owned = new List<VisualShaderNode>();
        try
        {
            var previous = -1;
            for (var i = 0; i < 17; i++)
            {
                var sample = new I(); var sampleID = graph.GetValidNodeID(stage); owned.Add(sample); graph.AddNode(stage, sample, default, sampleID);
                if (previous >= 0)
                {
                    var add = new VisualShaderNodeVectorOp(); var id = graph.GetValidNodeID(stage); owned.Add(add); graph.AddNode(stage, add, default, id);
                    graph.ConnectNodes(stage, previous, 0, id, 0); graph.ConnectNodes(stage, sampleID, 0, id, 1); previous = id;
                }
                else previous = sampleID;
            }
            graph.ConnectNodes(stage, previous, 0, 0, stage == VisualShader.Type.Vertex ? 2 : 0);
            ResourceSaver.Save(graph, IOPath.Combine(folder, "img-capacity-" + (int)stage + ".e2dres"), SaverFlags.BundleResources);
        }
        finally { foreach (var node in owned) node.Dispose(); }
    }

    private static void SaveImageGraph(string folder, string name, int shape = 0, int type = 0, int fallback = 0, int filter = 0, int repeat = 0,
        int qualifier = 0, bool vertex = false, bool reference = false, bool provided = false, bool property = false, bool lod = false,
        int alpha = 0, int source = 0, string? input = null, bool missingPort = false, bool wrongShape = false, bool screen = false, string? symbol = null, float layer = 0, float mip = 1, bool integerImage = false)
    {
        using var graph = new VisualShader(); var owned = new List<Resource>(); var stage = vertex ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
        int Add(VisualShaderNode node, VisualShader.Type? selected = null) { var id = owned.Count + 2; owned.Add(node); graph.AddNode(selected ?? stage, node, default, id); return id; }
        void Link(int from, int to, int port, int output = 0) => graph.ConnectNodes(stage, from, output, to, port);
        try
        {
            using var pixels = integerImage ? Image.CreateEmpty(4, 4, false, Image.Format.Rgba16I) : GraphImage();
            using var second = integerImage ? Image.CreateEmpty(4, 4, false, Image.Format.Rgba16I) : GraphImage(1);
            var image = ImageTexture.CreateFromImage(pixels); var array = new TextureArray(); array.CreateFromImages([pixels, second]); owned.Add(image); owned.Add(array);
            VisualShaderNode sampler = shape == 0 ? new I { Source = property ? I.SourceEnum.Texture : source != 0 ? (I.SourceEnum)source : I.SourceEnum.Port, Texture = property && provided ? image : null, TextureType = (I.TextureTypeEnum)Math.Min(type, 2) }
                : new VisualShaderNodeTextureArray { Source = property ? LS.SourceEnum.Texture : LS.SourceEnum.Port, TextureArray = property && provided ? array : null };
            var sample = Add(sampler);
            if (!property && source == 0 && !missingPort)
            {
                var parameterShape = wrongShape ? 1 - shape : shape;
                IP parameter = parameterShape == 0 ? new VisualShaderNodeImageParameter() : new VisualShaderNodeTextureArrayParameter();
                parameter.ParameterName = symbol ?? (qualifier == 1 ? shape == 1 ? "image_array" : "image_ordinary" : "image_value"); parameter.Qualifier = (Q)qualifier; parameter.TextureType = (IP.TextureTypeEnum)type;
                parameter.ColorDefault = (IP.ColorDefaultEnum)fallback; parameter.TextureFilter = (IP.TextureFilterEnum)filter; parameter.TextureRepeat = (IP.TextureRepeatEnum)repeat;
                parameter.TextureSource = screen ? IP.TextureSourceEnum.Screen : IP.TextureSourceEnum.None;
                var p = Add(parameter, reference ? vertex ? VisualShader.Type.Fragment : VisualShader.Type.Vertex : stage);
                if (reference) p = Add(new VisualShaderNodeParameterRef { ParameterName = parameter.ParameterName });
                if (input is not null) p = Add(new VisualShaderNodeInput { InputName = input });
                Link(p, sample, 2);
                if (screen && !vertex) Link(Add(new VisualShaderNodeInput { InputName = "screen_uv" }), sample, 0);
            }
            if (lod)
            {
                var coordinates = shape == 0 ? Add(new VisualShaderNodeVec2Constant { Constant = new(1.4375f, .4375f) }) : Add(new VisualShaderNodeVec3Constant { Constant = new(1.4375f, .4375f, layer) });
                Link(coordinates, sample, 0); var level = Add(new VisualShaderNodeFloatConstant { Constant = mip }); Link(level, sample, 1);
            }
            var observed = sample;
            if (alpha != 0)
            {
                var split = Add(new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 }); Link(sample, split, 0);
                var compose = new VisualShaderNodeVectorCompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector3 }; var c = Add(compose);
                for (var i = 0; i < 3; i++) Link(split, c, i, 3); observed = c;
            }
            Link(observed, 0, vertex ? 2 : 0);
            if (symbol is not null)
            {
                var q = symbol is "Globals" or "e2d_globals" ? Q.Global : symbol is "Instance" or "e2d_instance" ? Q.Instance : Q.None;
                var a = Add(new VisualShaderNodeFloatParameter { ParameterName = "image_symbol_alpha", Qualifier = q, DefaultValueEnabled = q != Q.Global, DefaultValue = 1 });
                Link(a, 0, vertex ? 3 : 1);
            }
            graph.AddNode(stage, sampler, default, 900);
            ResourceSaver.Save(graph, IOPath.Combine(folder, name + ".e2dres"), SaverFlags.BundleResources | (shape == 1 ? SaverFlags.SaveBigEndian | SaverFlags.Compress : 0));
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); }
    }
}
