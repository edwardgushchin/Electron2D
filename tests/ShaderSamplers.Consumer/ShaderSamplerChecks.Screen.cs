using Electron2D;

internal static partial class ShaderSamplerChecks
{
    private static void Screen(string output)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(256, 64) }; root.AddChild(new ScreenScene(output)); Engine.Run(root);
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Screen sampler host tears down.");
    }
    private sealed class ScreenScene(string output) : Node
    {
        private readonly List<Resource> _owned = [];
        private int _frames;
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; AddChild(new ScreenBackground());
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
            var texture = ImageTexture.CreateFromImage(image); _owned.Add(texture);
            var i = 0;
            foreach (var language in new[] { "GLSL", "HLSL" })
                foreach (var explicitState in new[] { false, true })
                {
                    var shader = Load("Screen" + language); _owned.Add(shader);
                    if (explicitState) { using var state = new RDSamplerState(); shader.SetTextureSamplerState("SCREEN_TEXTURE", state); }
                    var material = new ShaderMaterial { Shader = shader }; _owned.Add(material); material.SetShaderParameter("Coordinates", new Vector4(.5f, .125f, 0, 0));
                    AddChild(new Quad(texture, 16) { Position = new(16 + i++ * 24, 40), Material = material });
                }
        }
        protected override void OnProcess(double delta)
        {
            if (++_frames != 4) return;
            using var image = GetWindow()!.GetTexture().GetImage()!;
            for (var i = 0; i < 4; i++) Match(image.GetPixel(24 + i * 24, 48), i % 2 == 0 ? new(.5f, .5f, 0, 1) : new(0, 1, 0, 1), "SCREEN_TEXTURE/" + i);
            image.SavePNG(output); Console.WriteLine("Four GLSL/HLSL screen snapshot probes distinguish default linear and explicit nearest sampling."); Tree!.Quit();
        }
        protected override void Dispose(bool disposing) { foreach (var resource in _owned) resource.Dispose(); base.Dispose(disposing); }
    }
    private sealed class ScreenBackground : Entity
    {
        protected override void OnDraw() { DrawRect(new(0, 0, 128, 64), Colors.Red); DrawRect(new(128, 0, 128, 64), new(0, 1, 0, 1)); }
    }
}
