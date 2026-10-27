using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void MirrorClampCanvas(bool unavailable)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(32, 32), Borderless = true }; var cases = 0;
        root.Ready += _ =>
        {
            RenderingServer.RenderLoopEnabled = false;
            using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); Color[] colors = [Colors.Red, Colors.Green, Colors.Blue, Colors.White];
            for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) image.SetPixel(x, y, colors[y * 2 + x]);
            using var texture = ImageTexture.CreateFromImage(image);
            using var target = new DrawableTexture(); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent);
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                using var shader = Load("Blit" + language, true); using var material = new ShaderMaterial { Shader = shader };
                foreach (var axis in new[] { 1, 2, 3 })
                {
                    var u = (axis & 1) != 0 ? SamplerRepeatMode.MirrorClampToEdge : SamplerRepeatMode.ClampToEdge;
                    var v = (axis & 2) != 0 ? SamplerRepeatMode.MirrorClampToEdge : SamplerRepeatMode.ClampToEdge;
                    using var state = new RDSamplerState { RepeatU = u, RepeatV = v }; shader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
                    if (unavailable)
                    {
                        Reject<NotSupportedException>(() => target.BlitRect(new(0, 0, 4, 4), texture, material: material));
                        state.RepeatU = state.RepeatV = u = v = SamplerRepeatMode.ClampToEdge; shader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
                    }
                    state.RepeatU = state.RepeatV = SamplerRepeatMode.Repeat; state.Dispose();
                    foreach (var coordinate in new[] { -2.25f, -.75f, -.25f, 0, .25f, .75f, 1, 1.25f, 2.25f })
                    {
                        var p = new Vector2(coordinate, coordinate + .5f); material.SetShaderParameter("Coordinates", new Vector4(p.X, p.Y, 0, 0));
                        target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: material);
                        using var result = target.GetImage()!;
                        var x = MirrorIndex((int)MathF.Floor(p.X * 2), 2, u); var y = MirrorIndex((int)MathF.Floor(p.Y * 2), 2, v);
                        Match(result.GetPixel(2, 2), colors[y * 2 + x], $"copied mirror shader {language}/{axis}/{coordinate}"); cases++;
                    }
                }
            }
            root.Tree!.Quit();
        };
        Engine.Run(root); Console.WriteLine($"PASS copied mirror GLSL/HLSL GPU drawable pixels: {cases}; unavailable={unavailable}.");
    }
}
