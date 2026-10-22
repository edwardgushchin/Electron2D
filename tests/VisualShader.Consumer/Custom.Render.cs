using Electron2D;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    private static Color CustomExpected(string name, bool alternate = false, float u = 8.5f / 16, float v = 8.5f / 16)
    {
        if (name.StartsWith("custom-numeric-", StringComparison.Ordinal)) return ExpressionExpected(name.Replace("custom-numeric-", "expr-numeric-", StringComparison.Ordinal), alternate);
        var parts = name.Split('-'); var kind = int.Parse(parts[2]);
        return kind switch
        {
            0 => parts[3] == "0" ? new(.125f, .25f, .375f, 1) : new(.25f, .5f, .75f, 1),
            1 => new(.1875f, .25f, .375f, 1),
            2 or 5 or 10 => new(.25f, .5f, .75f, 1),
            3 => new(u, v, .25f, 1),
            4 => u < .5f ? Colors.Black : new(.75f, .5f, .25f, 1),
            6 => new(.125f, .375f, .625f, 1),
            7 => new(alternate ? .625f : .375f, .5f, .75f, 1),
            8 => new(.5f, .5f, .5f, 1),
            11 => new(.125f, .375f, .625f, 1),
            12 => new(.75f, .75f, .75f, 1),
            13 => new(.375f, .375f, .4375f, 1),
            _ => Colors.Black
        };
    }
    private sealed partial class GraphScene
    {
        private readonly List<(string Name, ShaderMaterial Material, Vector2 Position, int Role, int Kind)> _customCases = [];
        private Texture? _customImage;
        private void PrepareCustomCase(string name, VisualShader shader, ShaderMaterial material, Vector2 position)
        {
            var parts = name.Split('-'); var numeric = parts[1] == "numeric";
            var kind = numeric ? -1 : int.Parse(parts[2]);
            _customCases.Add((name, material, position, numeric && parts[3] == "2" ? int.Parse(parts[2]) : -1, kind));
            if (kind == 7) material.SetShaderParameter("raw_value", .375f);
            if (kind != 5) return;
            if (_customImage is null)
            { using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf); image.Fill(new(.25f, .5f, .75f, 1)); _customImage = Own(ImageTexture.CreateFromImage(image)); }
            shader.SetDefaultTextureParameter("custom_image", _customImage);
        }
        private void ProcessCustom()
        {
            if (_frame is not (4 or 6 or 95) && _frame is not (> 30 and <= 94)) return;
            var reset = _frame is 6 or 95 || _frame > 30 && _frame % 2 == 0;
            foreach (var item in _customCases)
            {
                if (item.Kind == 7) { item.Material.SetShaderParameter("raw_value", reset ? .375f : .625f); continue; }
                if (item.Role < 0) continue;
                if (reset) { item.Material.SetShaderParameter("custom_value", (Texture?)null); continue; }
                switch ((P)item.Role)
                {
                    case P.Scalar: item.Material.SetShaderParameter("custom_value", .625f); break;
                    case P.ScalarInt: item.Material.SetShaderParameter("custom_value", 3); break;
                    case P.ScalarUInt: item.Material.SetShaderParameter("custom_value", 5u); break;
                    case P.Boolean: item.Material.SetShaderParameter("custom_value", false); break;
                    case P.Vector2: item.Material.SetShaderParameter("custom_value", new Vector2(.75f, .25f)); break;
                    case P.Vector3: item.Material.SetShaderParameter("custom_value", new Vector3(.75f, .5f, .25f)); break;
                    case P.Vector4: item.Material.SetShaderParameter("custom_value", new Vector4(.75f, .5f, .25f, .125f)); break;
                    default: item.Material.SetShaderMatrixParameter("custom_value", ExpressionAlternateMatrix); break;
                }
            }
        }
        private void CheckCustomFrames()
        {
            if (_customCases.Count == 0) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _customCases)
            {
                var expected = CustomExpected(item.Name, _frame == 5); var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                Check(EffectPixelsMatch(actual, expected, GetWindow()!.UseHDR2D), $"Custom {item.Name} phase {_frame}: {actual} vs {expected}");
                if (item.Kind is not (3 or 4)) continue;
                foreach (var x in new[] { 2, 13 })
                {
                    expected = CustomExpected(item.Name, false, (x + .5f) / 16); actual = pixels.GetPixel((int)item.Position.X + x, (int)item.Position.Y + 8);
                    Check(EffectPixelsMatch(actual, expected, GetWindow()!.UseHDR2D), $"Custom spatial {item.Name} at {x}: {actual} vs {expected}");
                }
            }
        }
    }
}
