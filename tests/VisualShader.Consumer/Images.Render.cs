using Electron2D;
using IP = Electron2D.VisualShaderNodeTextureParameter;

internal static partial class GraphChecks
{
    private static readonly Color ImageBackground = new(64f / 255, 128f / 255, 192f / 255, 1);
    private static readonly Color ImageNormal = new(64f / 255, 128f / 255, 192f / 255, 160f / 255);
    private static readonly Color ImageSpecular = new(192f / 255, 64f / 255, 32f / 255, 224f / 255);
    private sealed class ImageBackdrop(Vector2[] positions) : Entity
    { protected override void OnDraw() { foreach (var position in positions) DrawRect(new(position, new(16, 16)), ImageBackground); } }
    private sealed partial class GraphScene
    {
        private Texture? _imageCanvas;
        private Texture? _imageValue;
        private TextureLayered? _imageArray;
        private readonly Dictionary<(bool Array, bool Half), Resource> _floatImageInputs = [];
        private readonly HashSet<string> _imageGlobals = new(StringComparer.Ordinal);
        private Texture ImageCanvas(Texture white)
        {
            if (_imageCanvas is not null) return _imageCanvas;
            using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); pixels.Fill(ImageNormal);
            var normal = Own(ImageTexture.CreateFromImage(pixels)); pixels.Fill(ImageSpecular); var specular = Own(ImageTexture.CreateFromImage(pixels));
            return _imageCanvas = Own(new CanvasTexture { DiffuseTexture = white, NormalTexture = normal, SpecularTexture = specular });
        }
        private Resource FloatImageTexture(bool array, bool half)
        {
            if (_floatImageInputs.TryGetValue((array, half), out var retained)) return retained;
            using var image = Image.CreateEmpty(1, 1, false, half ? Image.Format.Rgbah : Image.Format.Rgbaf); image.Fill(new(-.25f, .5f, 1.5f, .625f));
            if (array) { var layers = Own(new TextureArray()); layers.CreateFromImages([image]); retained = layers; }
            else retained = Own(ImageTexture.CreateFromImage(image));
            _floatImageInputs.Add((array, half), retained); return retained;
        }
        private void PrepareImageCase(string name, VisualShader shader, ShaderMaterial material)
        {
            var nodes = shader.GetNodeList(VisualShader.Type.Fragment).Select(id => shader.GetNode(VisualShader.Type.Fragment, id))
                .Concat(shader.GetNodeList(VisualShader.Type.Vertex).Select(id => shader.GetNode(VisualShader.Type.Vertex, id))).ToArray();
            var sample = nodes.First(n => n is VisualShaderNodeTexture or VisualShaderNodeTextureArray);
            Check(nodes.Count(n => ReferenceEquals(n, sample)) == 2, "Fresh image archives preserve borrowed node aliases.");
            if (name.StartsWith("img-property-", StringComparison.Ordinal))
            {
                var imageName = shader.GetShaderUniformList().Single(d => d.Name.StartsWith("e2d_image_", StringComparison.Ordinal)).Name;
                var array = sample is VisualShaderNodeTextureArray;
                static Resource? Source(VisualShaderNode n) => n is VisualShaderNodeTextureArray a ? a.TextureArray : ((VisualShaderNodeTexture)n).Texture;
                Resource? Default(VisualShader s) => array ? s.GetDefaultLayeredTextureParameter(imageName) : s.GetDefaultTextureParameter(imageName);
                var source = Source(sample); Check(source is not null && ReferenceEquals(source, Default(shader)), "Imported node and compiled default preserve their borrowed image alias.");
                using (var deep = (VisualShader)shader.Duplicate(true))
                {
                    var stage = name.EndsWith("-1", StringComparison.Ordinal) ? VisualShader.Type.Vertex : VisualShader.Type.Fragment;
                    var copied = deep.GetNode(stage, 900);
                    Check(!ReferenceEquals(Source(copied), source) && ReferenceEquals(Source(copied), Default(deep)), "Deep graph copy preserves its new source/default image alias.");
                }
                Check(!source!.IsDisposed, "Disposing a deep graph copy preserves the original image.");
            }
            if (name.StartsWith("img-float-", StringComparison.Ordinal))
            {
                var p = name.Split('-'); var array = p[2] == "1"; var source = FloatImageTexture(array, p[4] == "1");
                if (array) shader.SetDefaultLayeredTextureParameter("image_value", (TextureLayered)source);
                else shader.SetDefaultTextureParameter("image_value", (Texture)source);
                return;
            }
            if (name.StartsWith("img-null-global-", StringComparison.Ordinal))
            {
                var definition = nodes.OfType<IP>().Single();
                if (definition is VisualShaderNodeTextureArrayParameter) RenderingServer.GlobalShaderParameterAdd(definition.ParameterName, (TextureLayered?)null);
                else RenderingServer.GlobalShaderParameterAdd(definition.ParameterName, (Texture?)null);
                return;
            }
            if (!name.StartsWith("img-param-", StringComparison.Ordinal) && !name.StartsWith("img-filter-", StringComparison.Ordinal) && !name.StartsWith("img-symbol-", StringComparison.Ordinal) && !name.StartsWith("img-mip-", StringComparison.Ordinal) && !name.StartsWith("img-device-", StringComparison.Ordinal) && name != "img-shared") return;
            if (_imageValue is null)
            {
                using var first = GraphImage(); using var second = GraphImage(1);
                _imageValue = Own(ImageTexture.CreateFromImage(first));
                var array = Own(new TextureArray()); array.CreateFromImages([first, second]); _imageArray = array;
            }
            if (name == "img-symbol-pair")
            {
                shader.SetDefaultTextureParameter("GLOBAL_pair", _imageValue);
                RenderingServer.GlobalShaderParameterAdd("pair", ((CanvasTexture)_imageCanvas!).NormalTexture);
                Check(material.GetPropertyList().Any(d => d.Name == "shader_parameter/GLOBAL_pair"), "A material image keeps a literal global-looking public name.");
                return;
            }
            var parameter = nodes.OfType<IP>().Distinct<IP>(ReferenceEqualityComparer.Instance).Single();
            var shape = parameter is VisualShaderNodeTextureArrayParameter;
            if (name.StartsWith("img-symbol-", StringComparison.Ordinal) && nodes.OfType<VisualShaderNodeFloatParameter>().Any(p => p.Qualifier == VisualShaderNodeParameter.QualifierEnum.Global) && _imageGlobals.Add("image_symbol_alpha"))
                RenderingServer.GlobalShaderParameterAdd("image_symbol_alpha", RenderingServer.GlobalShaderParameterType.Float, 1f);
            var symbol = parameter.ParameterName;
            if (name.StartsWith("img-device-", StringComparison.Ordinal))
            {
                var source = ImageDeviceTexture(shape);
                if (shape) shader.SetDefaultLayeredTextureParameter(symbol, (TextureLayered)source);
                else shader.SetDefaultTextureParameter(symbol, (Texture)source);
                return;
            }
            if (parameter.Qualifier != VisualShaderNodeParameter.QualifierEnum.Global)
                Check(shape ? shader.GetDefaultLayeredTextureParameter(symbol) is null : shader.GetDefaultTextureParameter(symbol) is null, "Render-only graph fallback is not an authored image default.");
            if (parameter.Qualifier == VisualShaderNodeParameter.QualifierEnum.Global)
            {
                if (_imageGlobals.Add(symbol))
                {
                    if (shape) RenderingServer.GlobalShaderParameterAdd(symbol, _imageArray);
                    else RenderingServer.GlobalShaderParameterAdd(symbol, _imageValue);
                }
                Check(!material.GetPropertyList().Any(d => d.Name == "shader_parameter/" + symbol), "Global image remains registry-owned.");
                return;
            }
            else if (shape) shader.SetDefaultLayeredTextureParameter(symbol, _imageArray);
            else shader.SetDefaultTextureParameter(symbol, _imageValue);
            Check(material.GetPropertyList().Any(d => d.Name == "shader_parameter/" + symbol), "Image parameter descriptor reaches the public material.");
        }
    }

    private static Color ImageExpected(string name, bool hdr)
    {
        var parts = name.Split('-');
        if (parts[1] == "fallback")
        {
            var shape = int.Parse(parts[2]); var type = int.Parse(parts[3]); var fallback = int.Parse(parts[4]); var alpha = int.Parse(parts[5]);
            var value = type >= 2 ? shape == 1 ? Colors.White : type == 2 ? new Color(128f / 255, 128f / 255, 1, 1) : new Color(1, 128f / 255, 1, 1)
                : fallback == 0 ? Colors.White : fallback == 1 ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, 0);
            return alpha == 0 ? value : new(value.A, value.A, value.A, 1);
        }
        if (parts[1] == "device") return DeviceImageExpected(name, hdr, 0);
        if (parts[1] == "screen") return hdr ? LinearImage(ImageBackground) : ImageBackground;
        if (parts[1] == "mip")
        {
            var layer = int.Parse(parts[3]); var level = int.Parse(parts[4]); var alpha = int.Parse(parts[5]);
            var value = level == 1 ? layer == 0 ? new Color(32f / 255, 64f / 255, 192f / 255, 224f / 255) : new Color(192f / 255, 32f / 255, 64f / 255, 96f / 255)
                : layer == 0 ? new Color(240f / 255, 128f / 255, 32f / 255, 96f / 255) : new Color(64f / 255, 192f / 255, 128f / 255, 32f / 255);
            return alpha != 0 ? new(value.A, value.A, value.A, 1) : hdr ? LinearImage(value) : value;
        }
        if (name == "img-symbol-pair") return new(84f / 255, 116f / 255, 160f / 255, 1);
        if (parts[1] == "shared") return new(70.4f / 255, 70.4f / 255, 128f / 255, 1);
        if (parts[1] == "float") return new(-.25f, .5f, 1.5f, 1);
        if (parts[1] == "null") return Colors.Black;
        if (parts[1] == "channel") return parts[2] == "normal_texture" ? new(128f / 255, 128f / 255, 1, 1) : Colors.White;
        if (parts[1] == "empty") return Colors.Black;
        if (parts[1] == "source")
        {
            if (parts[3] == "1") return Colors.Black;
            return parts[2] switch { "1" => hdr ? LinearImage(ImageBackground) : ImageBackground, "2" => Colors.White, _ => ImageNormal };
        }
        if (parts[1] == "input") return parts[2] switch { "texture" => Colors.White, "normal_texture" => ImageNormal, _ => ImageSpecular };
        if (parts[1] == "filter")
        {
            var filter = int.Parse(parts[3]); var repeat = int.Parse(parts[4]);
            if (filter >= 3) return new(32f / 255, 64f / 255, 192f / 255, 1);
            var linear = filter != 1;
            return new((repeat == 1 ? linear ? 80 : 64 : 192) / 255f, (linear ? 80 : 64) / 255f, 128f / 255, 1);
        }
        var color = new Color(104f / 255, 104f / 255, 128f / 255, 160f / 255);
        if (parts[1] == "property") return parts[3] == "1" ? new(102f / 255, 102f / 255, color.B, color.A) : color;
        if (parts[1] == "symbol") return name.EndsWith("-vertex", StringComparison.Ordinal) ? new(102f / 255, 102f / 255, color.B, color.A) : color;
        var typeValue = int.Parse(parts[3]); var alphaValue = int.Parse(parts[6]);
        if (alphaValue != 0) return new(color.A, color.A, color.A, 1);
        if (parts[5] == "1")
        {
            var edge = typeValue == 1 && hdr ? LinearImage(new(192f / 255, 192f / 255, color.B, color.A)) : new Color(192f / 255, 192f / 255, color.B, color.A);
            return new(edge.R * (8.5f / 16), edge.G * (8.5f / 16), edge.B, edge.A);
        }
        if (typeValue == 1 && hdr)
        {
            var low = LinearImage(new(64f / 255, 64f / 255, 128f / 255, color.A));
            var high = LinearImage(new(128f / 255, 128f / 255, 128f / 255, color.A));
            return new(low.R * .375f + high.R * .625f, low.G * .375f + high.G * .625f, high.B, color.A);
        }
        return color;
    }
    private static Color LinearImage(Color c)
    {
        static float Decode(float f) => f <= .04045f ? f / 12.92f : MathF.Pow((f + .055f) / 1.055f, 2.4f);
        return new(Decode(c.R), Decode(c.G), Decode(c.B), c.A);
    }
}
