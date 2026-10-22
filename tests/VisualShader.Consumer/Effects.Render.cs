using Electron2D;

internal static partial class GraphChecks
{
    private sealed partial class GraphScene
    {
        private Texture? _effectImage;
        private readonly List<(ShaderMaterial Material, string Name, Vector2 Position)> _effectCases = [];
        private void PrepareEffectCase(string name, VisualShader shader, ShaderMaterial material, Vector2 position)
        {
            if (!name.StartsWith("fx-pipeline-", StringComparison.Ordinal) && !name.StartsWith("fx-helper-", StringComparison.Ordinal)) return;
            if (_effectImage is null) { using var pixels = GraphImage(); _effectImage = Own(ImageTexture.CreateFromImage(pixels)); }
            var parameters = shader.GetNodeList(VisualShader.Type.Fragment).Select(id => shader.GetNode(VisualShader.Type.Fragment, id))
                .Concat(shader.GetNodeList(VisualShader.Type.Vertex).Select(id => shader.GetNode(VisualShader.Type.Vertex, id))).OfType<VisualShaderNodeImageParameter>().Distinct<VisualShaderNodeImageParameter>(ReferenceEqualityComparer.Instance);
            foreach (var parameter in parameters) shader.SetDefaultTextureParameter(parameter.ParameterName, _effectImage);
            if (name.StartsWith("fx-pipeline-", StringComparison.Ordinal)) _effectCases.Add((material, name, position));
        }
        private void ProcessEffects()
        {
            if (_frame is not (4 or 6 or 95) && _frame is not (> 30 and <= 94)) return;
            foreach (var item in _effectCases)
                if (_frame is 6 or 95) item.Material.SetShaderParameter("effect_shift", (Texture?)null);
                else item.Material.SetShaderParameter("effect_shift", _frame == 4 || _frame % 2 != 0 ? new Vector2(-.1875f, .1875f) : new Vector2(.0625f, -.0625f));
        }
        private void CheckEffectFrames()
        {
            if (_effectCases.Count == 0 || _frame is not (5 or 7 or 94 or 174)) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _effectCases)
            {
                var expected = EffectExpected(item.Name, GetWindow()!.UseHDR2D, _frame == 5 ? 1 : 0);
                if (!GetWindow()!.UseHDR2D) expected = new(Math.Clamp(expected.R, 0, 1), Math.Clamp(expected.G, 0, 1), Math.Clamp(expected.B, 0, 1), expected.A);
                var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                Check(EffectPixelsMatch(actual, expected, GetWindow()!.UseHDR2D),
                    $"Composed effect {item.Name} frame {_frame}: actual {actual}, expected {expected}.");
            }
            Console.WriteLine($"{_effectCases.Count} UV/image/color materials verified at frame {_frame} after parameter updates/reset.");
        }
    }
}
