using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void FormatsCanvas(bool masked)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var local = RenderingServer.CreateLocalRenderingDevice();
        Check(local.SamplerIsFormatSupportedForFilter(DataFormat.R32G32B32A32SFLOAT, SamplerFilter.Linear) == !masked, "Canvas and local devices observe the same controlled format features.");
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgbaf); image.Fill(Colors.Green);
        using var texture = ImageTexture.CreateFromImage(image); using var canvas = Load("CanvasGLSL");
        using var process = Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "ParticleGLSL.comp.glsl.spv")), Shader.Mode.Particles);
        using var white = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); white.Fill(Colors.White); using var particleTexture = ImageTexture.CreateFromImage(white);
        foreach (var shader in new[] { canvas, process })
        {
            var particles = ReferenceEquals(shader, process); using var material = new ShaderMaterial { Shader = shader };
            material.SetShaderParameter("pixels", texture); if (!particles) material.SetShaderParameter("Coordinates", new Vector4(0, 0, 1, 1));
            using var linear = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear };
            shader.SetTextureSamplerState("pixels", linear);
            using var point = new RDSamplerState();
            if (masked)
            {
                try { RunCanvas(false); throw new InvalidOperationException("Unsupported canvas filtering unexpectedly executed."); }
                catch (ArgumentException error) when (error.Message.Contains("native texture format", StringComparison.Ordinal)) { }
                catch (AggregateException error) when (error.Flatten().InnerExceptions.All(e => e is ArgumentException && e.Message.Contains("native texture format", StringComparison.Ordinal))) { }
                Check(!RenderingServer.IsAvailable, "Rejected canvas filtering retires its renderer.");
            }
            else RunCanvas(true);
            shader.SetTextureSamplerState("pixels", point);
            RunCanvas(true);
            Check(local.SamplerIsFormatSupportedForFilter(DataFormat.R32G32B32A32SFLOAT, SamplerFilter.Linear) == !masked, "The retained local lease preserves the immutable physical snapshot across renderer sessions.");
            Console.WriteLine($"Native format canvas/blit/particles: masked={masked}, particles={particles}, configured image validation, real pixels, renderer retirement and nearest recovery passed without window presentation.");

            void RunCanvas(bool expected)
            {
                using var root = new Window { Size = new(24, 24), Borderless = true }; var frames = 0;
                GPUParticles? particle = null;
                if (particles) { particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 0, Interpolate = false, LocalCoords = true, Texture = particleTexture, Position = new(8, 8), ProcessMaterial = material }; root.AddChild(particle); }
                else root.AddChild(new Quad(texture, 16) { Material = material }); root.AddChild(new CoordinateTick(() => RenderingServer.ForceDraw(swapBuffers: false)));
                root.Ready += _ =>
                {
                    RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black);
                    particle?.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, ParticlesEmitFlags.Position);
                    using var blitShader = Load("BlitGLSL", true); using var blitMaterial = new ShaderMaterial { Shader = blitShader };
                    using var target = new DrawableTexture(); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent);
                    blitMaterial.SetShaderParameter("Coordinates", new Vector4(0, 0, 1, 1)); blitShader.SetTextureSamplerState("SOURCE_TEXTURE0", linear);
                    if (masked) Reject<ArgumentException>(() => target.BlitRect(new(0, 0, 4, 4), texture, material: blitMaterial));
                    else target.BlitRect(new(0, 0, 4, 4), texture, material: blitMaterial);
                    blitShader.SetTextureSamplerState("SOURCE_TEXTURE0", point);
                    target.BlitRect(new(0, 0, 4, 4), texture, material: blitMaterial); using var result = target.GetImage()!; Match(result.GetPixel(2, 2), Colors.Green, "native format-filter blit recovery");
                    RenderingServer.FramePostDraw += Frame;
                };
                try { Engine.Run(root); }
                finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Frame; }
                Check(expected && frames >= 3 && root.IsDisposed, "Supported canvas filtering produces pixels and closes its scene.");
                void Frame()
                {
                    if (++frames < 3) return;
                    using var output = root.GetTexture().GetImage()!; Match(output.GetPixel(8, 8), Colors.Green, "native format-filter canvas output"); RenderingServer.FramePostDraw -= Frame; root.Tree!.Quit();
                }
            }
        }
    }
}
