using Electron2D;

internal static partial class GraphChecks
{
    private sealed partial class GraphScene
    {
        private readonly List<(VisualShader Shader, string Name, Vector2 Position)> _imageSamplerCases = [];
        private void ProcessImageSamplers()
        {
            if (_frame is not (4 or 6)) return;
            using var settings = new RDSamplerState
            {
                MinFilter = RenderingDevice.SamplerFilter.Nearest,
                MagFilter = RenderingDevice.SamplerFilter.Nearest,
                MIPFilter = RenderingDevice.SamplerFilter.Nearest,
                MinLOD = 1,
                MaxLOD = 1
            };
            foreach (var item in _imageSamplerCases)
            {
                item.Shader.SetTextureSamplerState("image_value", _frame == 4 ? settings : null);
                using var copied = item.Shader.GetTextureSamplerState("image_value");
                Check(_frame == 4 ? copied is { MinLOD: 1, MaxLOD: 1 } : copied is null, "Explicit graph sampler state copies and resets independently of the image hint.");
            }
        }
        private void CheckImageSamplerFrames()
        {
            if (_imageSamplerCases.Count == 0 || _frame is not (5 or 7)) return;
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _imageSamplerCases)
            {
                var expected = _frame == 5 ? new Color(32f / 255, 64f / 255, 192f / 255, 1) : ImageExpected(item.Name, GetWindow()!.UseHDR2D);
                var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                Check(MathF.Abs(actual.R - expected.R) < .0021f && MathF.Abs(actual.G - expected.G) < .0021f && MathF.Abs(actual.B - expected.B) < .0021f,
                    $"Graph sampler override {item.Name}: actual {actual}, expected {expected}.");
            }
            Console.WriteLine($"{_imageSamplerCases.Count} graph sampler hints verified after {(_frame == 5 ? "explicit LOD override" : "reset")}.");
        }
    }
}
