using Electron2D;
using P = Electron2D.VisualShaderNode.PortType;
using M4 = System.Numerics.Matrix4x4;

internal static partial class GraphChecks
{
    private static Color ExpressionExpected(string name, bool alternate = false, float u = 8.5f / 16, float v = 8.5f / 16)
    {
        var p = name.Split('-').Skip(2).Select(int.Parse).ToArray();
        if (name.StartsWith("expr-numeric-", StringComparison.Ordinal))
        {
            var type = (P)p[0]; var zero = p[1] == 0; alternate &= p[1] == 2;
            return type switch
            {
                P.Scalar => new(zero ? 0 : alternate ? .625f : .25f, zero ? 0 : alternate ? .625f : .25f, zero ? 0 : alternate ? .625f : .25f, 1),
                P.ScalarInt => new(zero ? 0 : alternate ? 3f / 16 : 7f / 16, zero ? 0 : alternate ? 3f / 16 : 7f / 16, zero ? 0 : alternate ? 3f / 16 : 7f / 16, 1),
                P.ScalarUInt => new(zero ? 0 : alternate ? 5f / 16 : 11f / 16, zero ? 0 : alternate ? 5f / 16 : 11f / 16, zero ? 0 : alternate ? 5f / 16 : 11f / 16, 1),
                P.Boolean => new(zero || alternate ? .25f : .75f, zero || alternate ? .25f : .75f, zero || alternate ? .25f : .75f, 1),
                P.Vector2 => zero ? new(0, 0, .25f, 1) : alternate ? new(.75f, .25f, .25f, 1) : new(.125f, .625f, .25f, 1),
                P.Vector3 => zero ? new(0, 0, 0, 1) : alternate ? new(.75f, .5f, .25f, 1) : new(.125f, .5f, .875f, 1),
                P.Vector4 => zero ? new(0, 0, 0, 1) : alternate ? new(.75f, .25f, .125f, 1) : new(.125f, .625f, .875f, 1),
                _ => zero ? new(.1f, .1f, .1f, 1) : alternate ? new(.5f, .6f, .7f, 1) : new(.2f, .3f, .4f, 1)
            };
        }
        return p[0] switch
        {
            0 => new(.2f, .4f, .6f, 1),
            1 => new(.5f, .25f, .75f, 1),
            2 => new(.25f, .1f, .8f, 1),
            3 => new(.25f, .5f, .75f, 1),
            4 => new(u, v, .25f, 1),
            5 => p[1] == 0 ? new(.25f + .5f * u, .5f, .75f - .5f * u, 1) : u > .5f ? new(.75f, .5f, .25f, 1) : new(.25f, .5f, .75f, 1),
            6 => new(7f / 24, 1f / 3, .3f, 1),
            7 => new(.125f, .375f, .625f, 1),
            8 => new(.25f, .5f, .75f, 1),
            10 => u < .5f ? new(0, 0, 0, 1) : new(.75f, .5f, .25f, 1),
            11 => new(.25f, .5f, .75f, 1),
            12 => new(alternate ? .625f : .375f, .5f, .75f, 1),
            _ => new(.375f, .5f, .75f, 1)
        };
    }
    private static readonly float[] ExpressionAlternateMatrix = [5, 0, 0, 0, 0, 6, 0, 0, 0, 0, 7, 0, 0, 0, 0, 1];
    private sealed partial class GraphScene
    {
        private Texture? _expressionImage;
        private readonly List<(string Name, ShaderMaterial Material, Vector2 Position)> _expressionCases = [];
        private void PrepareExpressionCase(string name, VisualShader shader, ShaderMaterial material, Vector2 position)
        {
            _expressionCases.Add((name, material, position));
            if (name.StartsWith("expr-procedural-12-", StringComparison.Ordinal)) material.SetShaderParameter("raw_value", .375f);
            if (!name.StartsWith("expr-procedural-8-", StringComparison.Ordinal)) return;
            if (_expressionImage is null)
            { using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf); image.Fill(new(.25f, .5f, .75f, 1)); _expressionImage = Own(ImageTexture.CreateFromImage(image)); }
            shader.SetDefaultTextureParameter("expression_image", _expressionImage);
        }
        private void ProcessExpressions()
        {
            if (_frame is not (4 or 6 or 95) && _frame is not (> 30 and <= 94)) return;
            foreach (var item in _expressionCases)
            {
                if (item.Name.StartsWith("expr-procedural-12-", StringComparison.Ordinal))
                { item.Material.SetShaderParameter("raw_value", _frame is 6 or 95 || _frame > 30 && _frame % 2 == 0 ? .375f : .625f); continue; }
                if (!item.Name.StartsWith("expr-numeric-", StringComparison.Ordinal) || item.Name[^3] != '2') continue;
                if (_frame is 6 or 95 || _frame > 30 && _frame % 2 == 0) { item.Material.SetShaderParameter("expression_value", (Texture?)null); continue; }
                switch ((P)(item.Name[13] - '0'))
                {
                    case P.Scalar: item.Material.SetShaderParameter("expression_value", .625f); break;
                    case P.ScalarInt: item.Material.SetShaderParameter("expression_value", 3); break;
                    case P.ScalarUInt: item.Material.SetShaderParameter("expression_value", 5u); break;
                    case P.Boolean: item.Material.SetShaderParameter("expression_value", false); break;
                    case P.Vector2: item.Material.SetShaderParameter("expression_value", new Vector2(.75f, .25f)); break;
                    case P.Vector3: item.Material.SetShaderParameter("expression_value", new Vector3(.75f, .5f, .25f)); break;
                    case P.Vector4: item.Material.SetShaderParameter("expression_value", new Vector4(.75f, .5f, .25f, .125f)); break;
                    default: item.Material.SetShaderMatrixParameter("expression_value", ExpressionAlternateMatrix); break;
                }
            }
        }
        private void CheckExpressionFrames()
        {
            if (_expressionCases.Count == 0) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _expressionCases)
            {
                var expected = ExpressionExpected(item.Name, _frame == 5); var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                Check(EffectPixelsMatch(actual, expected, GetWindow()!.UseHDR2D), $"Expression {item.Name} phase {_frame}: {actual} vs {expected}");
                if (!item.Name.StartsWith("expr-procedural-5-", StringComparison.Ordinal) && !item.Name.StartsWith("expr-procedural-10-", StringComparison.Ordinal)) continue;
                foreach (var x in new[] { 2, 13 })
                { expected = ExpressionExpected(item.Name, false, (x + .5f) / 16); actual = pixels.GetPixel((int)item.Position.X + x, (int)item.Position.Y + 8); Check(EffectPixelsMatch(actual, expected, GetWindow()!.UseHDR2D), "Conditional return keeps the stage epilogue and vertex interpolation."); }
            }
            Console.WriteLine($"{_expressionCases.Count} expression materials verified after update/reset at frame {_frame}.");
        }
    }
}
