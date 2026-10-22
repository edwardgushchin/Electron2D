using Electron2D;
using K = Electron2D.RenderingServer.GlobalShaderParameterType;

internal static partial class GraphChecks
{
    private sealed partial class GraphScene
    {
        private sealed record ParameterCase(string Name, string Symbol, int Role, int Qualifier, int Initial, ShaderMaterial Material, GraphPainter Painter, Vector2 Position);
        private readonly List<ParameterCase> _parameterCases = [];
        private readonly HashSet<string> _parameterGlobals = new(StringComparer.Ordinal);
        private void PrepareParameterShader(string name, VisualShader shader)
        {
            VerifyParameterArchive(name, shader);
            if (name.StartsWith("parameter-layout-", StringComparison.Ordinal)) { if (_parameterGlobals.Add("p_layout_gain")) RenderingServer.GlobalShaderParameterAdd("p_layout_gain", K.Float, .125f); return; }
            var p = ParameterParts(name); var symbol = $"p_{p[0]}_{p[2]}";
            if (p[1] == 1 && _parameterGlobals.Add(symbol)) AddParameterGlobal(symbol, p[0], p[2] == 1);
        }
        private void AddParameterCase(string name, ShaderMaterial material, GraphPainter painter, Vector2 position)
        {
            if (name.StartsWith("parameter-layout-", StringComparison.Ordinal)) { _parameterCases.Add(new(name, "p_layout_scale", 9, 0, 1, material, painter, position)); return; }
            var p = ParameterParts(name);
            if (p[1] >= 2) CheckParameterHint(painter.GetPropertyList().Single(d => d.Name == "instance_shader_parameters/" + $"p_{p[0]}_{p[2]}"), p[0], p[2]);
            _parameterCases.Add(new(name, $"p_{p[0]}_{p[2]}", p[0], p[1], p[2], material, painter, position));
        }
        private void WriteParameterCases(bool reset)
        {
            foreach (var c in _parameterCases)
            {
                if (c.Role == 9) { WriteParameterLayout(c.Material, c.Painter, reset); continue; }
                if (c.Qualifier == 1) SetParameterGlobal(c.Symbol, c.Role, reset ? c.Initial == 1 : true);
                else if (c.Qualifier >= 2) SetParameterInstance(c.Painter, c.Symbol, c.Role, reset);
                else if (reset) c.Material.SetShaderParameter(c.Symbol, (Texture?)null);
                else SetParameterMaterial(c.Material, c.Symbol, c.Role, true);
            }
        }
        private void ProcessParameters()
        {
            if (_frame is 4 or 6 or 95 || _frame is > 30 and <= 94) WriteParameterCases(_frame is 6 or 95 || _frame > 30 && _frame % 2 == 0);
        }
        private void CheckParameterFrames()
        {
            if (_frame is not (5 or 7 or 97)) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var c in _parameterCases)
            {
                var expected = ParameterExpected(c.Name, _frame == 5 ? 1 : 0);
                if (!GetWindow()!.UseHDR2D) expected = new(Math.Clamp(expected.R, 0, 1), Math.Clamp(expected.G, 0, 1), Math.Clamp(expected.B, 0, 1), 1);
                var actual = pixels.GetPixel((int)c.Position.X + 8, (int)c.Position.Y + 8); var tolerance = c.Role is 1 or 2 ? .001f : .0021f;
                Check(MathF.Abs(actual.R - expected.R) < tolerance && MathF.Abs(actual.G - expected.G) < tolerance && MathF.Abs(actual.B - expected.B) < tolerance, $"Parameter {c.Name} phase {_frame}: {actual} vs {expected}.");
            }
            Console.WriteLine($"{_parameterCases.Count} parameter graphs verified after {(_frame == 5 ? "typed overrides" : "reset")}; source geometry retained.");
        }
    }
}
