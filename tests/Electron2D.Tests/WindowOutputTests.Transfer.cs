using Electron2D;
using System.Reflection;
using SDL = SDL3.SDL;

internal static partial class WindowOutputTests
{
    private static bool RejectSwapchain(nint device, nint window, SDL.GPUSwapchainComposition composition, SDL.GPUPresentMode mode) => false;
    private static void NativeFailure(Action action)
    {
        var field = typeof(SDL).GetField("SetGPUSwapchainParametersNativeFunction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = field.GetValue(null);
        var method = typeof(WindowOutputTests).GetMethod(nameof(RejectSwapchain), BindingFlags.NonPublic | BindingFlags.Static)!;
        try { field.SetValue(null, method.CreateDelegate(field.FieldType)); Reject<InvalidOperationException>(action); }
        finally { field.SetValue(null, previous); }
    }

    private static void Transfer()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = Shader.CreateFromSPIRV(BuiltInShaders.OutputFragment);
        using var material = new ShaderMaterial { Shader = shader };
        Color[] colors = [new(1, 0, 0), new(0, 1, 0), new(0, 0, 1), new(1, 1, 1), new(.01f, .1f, .5f), new(-1, 2.5f, 10), Colors.Black];
        using var image = Image.CreateEmpty(colors.Length, 1, false, Image.Format.Rgbaf);
        for (var i = 0; i < colors.Length; i++) image.SetPixel(i, 0, colors[i]);
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(128, 32), UseHDR2D = true };
        window.AddChild(new Sprite { Texture = texture, Centered = false, Scale = new(16, 16), Material = material, TextureFilter = TextureFilter.Nearest });
        using var output = window.GetTexture(); var frame = 0;
        material.SetShaderParameter("referenceNits", 80f); material.SetShaderParameter("maximumNits", 640f); material.SetShaderParameter("pqEncoding", 0f);
        window.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            frame++; var reference = frame == 1 ? 80f : 160f; var maximum = frame == 1 ? 640f : 320f;
            using var pixels = output.GetImage()!;
            for (var i = 0; i < colors.Length; i++)
            {
                var c = colors[i]; var r = Math.Clamp(c.R * reference, 0, maximum); var g = Math.Clamp(c.G * reference, 0, maximum); var b = Math.Clamp(c.B * reference, 0, maximum);
                var expected = frame == 1 ? new Color(r / 80, g / 80, b / 80) : new Color(
                    PQ(.6274040 * r + .3292820 * g + .0433136 * b), PQ(.0690970 * r + .9195400 * g + .0113612 * b), PQ(.0163916 * r + .0880132 * g + .8955950 * b));
                var actual = pixels.GetPixel(i * 16 + 4, 4);
                Check(MathF.Abs(actual.R - expected.R) < .002f && MathF.Abs(actual.G - expected.G) < .002f && MathF.Abs(actual.B - expected.B) < .002f,
                    $"Output transfer {frame}/{i}: {actual} != {expected}.");
            }
            if (frame == 1) { material.SetShaderParameter("referenceNits", 160f); material.SetShaderParameter("maximumNits", 320f); material.SetShaderParameter("pqEncoding", 1f); }
            else window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && frame == 2, "Output transfer native host.");
        Console.WriteLine("Native HDR output shader pixels: seven independent linear/scRGB and BT.2020 ST2084 cases, reference/peak clipping and negative inputs passed.");
    }

    private static float PQ(double nits)
    {
        var p = Math.Pow(Math.Clamp(nits / 10000, 0, 1), 2610d / 16384);
        return (float)Math.Pow((3424d / 4096 + 2413d / 128 * p) / (1 + 2392d / 128 * p), 2523d / 32);
    }
}
