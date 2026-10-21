using Electron2D;

internal static partial class DefaultChecks
{
    private static void DomainResources()
    {
        foreach (var language in new[] { "hlsl", "glsl" })
            foreach (var mode in new[] { Shader.Mode.CanvasItem, Shader.Mode.TextureBlit, Shader.Mode.Particles })
            {
                var name = (mode == Shader.Mode.CanvasItem ? "Defaults.frag." : mode == Shader.Mode.TextureBlit ? "Blit.frag." : "Particles.comp.") + language;
                var code = Stage(name); using var shader = Shader.CreateFromSPIRV(code, mode);
                Check(shader.GetMode() == mode, "Shader reports its executing compiled domain.");
                Reject<ArgumentOutOfRangeException>(() => shader.SetSPIRV(code, (Shader.Mode)3));
                Check(shader.GetMode() == mode && shader.GetSPIRV().SequenceEqual(code), "Invalid domain preserves the old program.");
                shader.Dispose(); Reject<ObjectDisposedException>(() => shader.GetMode());
            }
    }
    private static void Near(Color actual, Color expected, string message)
    {
        Check(MathF.Abs(actual.R - expected.R) < .015f && MathF.Abs(actual.G - expected.G) < .015f && MathF.Abs(actual.B - expected.B) < .015f && MathF.Abs(actual.A - expected.A) < .015f, message + ": " + actual);
    }
    private sealed partial class DefaultScene
    {
        private readonly List<ShaderMaterial> _processes = [];
        private void Domains()
        {
            var x = 24;
            foreach (var language in new[] { "hlsl", "glsl" })
            {
                var blit = Own(Shader.CreateFromSPIRV(Stage("Blit.frag." + language), Shader.Mode.TextureBlit));
                var material = Own(new ShaderMaterial { Shader = blit }); var target = Own(new DrawableTexture()); target.Setup(8, 8, 0, Colors.Black);
                Check(material.GetShaderParameter<Vector4>("tint") == new Vector4(.25f, .5f, .75f, 1) && material.GetShaderParameter<bool>("enabled"), "TextureBlit defaults initialize material state.");
                foreach (var state in new[] { 0, 1, 2 })
                {
                    if (state == 1) material.SetShaderParameter("enabled", false);
                    if (state == 2) material.SetShaderParameter("enabled", null);
                    target.BlitRect(new(0, 0, 8, 8), null, material: material);
                    using var result = target.GetImage()!; Near(result.GetPixel(4, 4), state == 1 ? Colors.Magenta : new Color(.25f, .5f, .75f, 1), "TextureBlit compiled default/reset pixels");
                }
                var processShader = Own(Shader.CreateFromSPIRV(Stage("Particles.comp." + language), Shader.Mode.Particles));
                var process = Own(new ShaderMaterial { Shader = processShader }); _processes.Add(process);
                Check(RenderingServer.ShaderGetParameterDefault<bool>(processShader.GetRID(), "enabled"), "Particle defaults query through borrowed identity.");
                using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White); var texture = Own(ImageTexture.CreateFromImage(image));
                var particles = new GPUParticles { Texture = texture, ProcessMaterial = process, Amount = 1, Lifetime = 1000, Emitting = false, FixedFPS = 0, Interpolate = false, LocalCoords = true, Position = new(x, 216), Scale = new(16, 16) };
                AddChild(particles); particles.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position); x += 64;
            }
        }
    }
}
