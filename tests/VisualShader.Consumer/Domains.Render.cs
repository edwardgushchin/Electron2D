using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderDomains(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(128, 128), Title = "Compiled particle and writable-texture graphs", UseHDR2D = args.Contains("hdr") };
        window.AddChild(new DomainScene(folder)); Engine.Run(window);
        Check(window.IsDisposed && !RenderingServer.IsAvailable, "Domain scene retires native ownership.");
    }
    private sealed partial class DomainScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private GPUParticles _particles = null!;
        private int _frame;
        private long _ownerBytes, _allBytes;
        private T OwnDomain<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        protected override void OnReady()
        {
            RenderingServer.GlobalShaderParameterAdd("domain_global", RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(.25f, .5f, .75f));
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            var shader = OwnDomain(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "particle-basic.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var retained = shader.GetSPIRV(); shader.SetMode(Shader.Mode.TextureBlit);
            Check(shader.GetSPIRV().AsSpan().SequenceEqual(retained), "Source mode edits retain the immutable compute program used by the following render.");
            var material = OwnDomain(new ShaderMaterial { Shader = shader });
            using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White);
            var texture = OwnDomain(ImageTexture.CreateFromImage(image));
            _particles = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, ProcessMaterial = material, Texture = texture, UseFixedSeed = true, Seed = 123 };
            AddChild(_particles); _particles.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, 0); _particles.RequestParticlesProcess(.125f);
            PrepareRandomParticles(folder, texture);
            RenderingServer.FramePostDraw += AfterDomainDraw;
        }
        protected override void OnProcess(double delta)
        { if (_frame is >= 30 and < 94) { _particles.RequestParticlesProcess(.125f); foreach (var particle in _randomParticles) particle.RequestParticlesProcess(.125f); } }
        private void AfterDomainDraw()
        {
            _frame++;
            if (_frame == 1)
            {
                using var pixels = GetWindow()!.GetTexture().GetImage()!;
                var actual = pixels.GetPixel(16, 16); var expected = new Color(.25f, .5f, .75f);
                if (GetWindow()!.UseHDR2D) expected = expected.SRGBToLinear();
                Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f, "Compiled particle graph executes initialization and supplies actual canvas pixels: " + actual);
                var bounds = _particles.CaptureRect(); Check(bounds.GetCenter().DistanceTo(new(16, 16)) < .001f, "Compiled graph position reaches native bounds.");
                for (var count = 1; count <= 4; count++)
                {
                    using var shader = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "blit-" + count + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
                    using var material = new ShaderMaterial { Shader = shader };
                    var targets = new DrawableTexture[count];
                    try
                    {
                        for (var i = 0; i < count; i++) { targets[i] = new(); targets[i].Setup(8, 8, DrawableTexture.DrawableFormat.RGBA8, Colors.Black); }
                        targets[0].BlitRectMulti(new(0, 0, 8, 8), [], targets.AsSpan(1), material: material);
                        for (var i = 0; i < count; i++)
                        {
                            using var result = targets[i].GetImage()!; var color = result.GetPixel(3, 3);
                            Check(MathF.Abs(color.R - (i + 1) / 4f) < .005f && MathF.Abs(color.G - .5f) < .005f && MathF.Abs(color.B - .75f) < .005f, "Each compiled writable-texture graph output reaches its own target.");
                        }
                    }
                    finally { foreach (var target in targets) target?.Dispose(); }
                }
                CheckRandomParticles(pixels);
                CheckDomainBlits(folder);
                Console.WriteLine("Compiled domain graphs: actual particle pixels/bounds and one through four writable-texture targets passed.");
            }
            if (_frame is 30 or 110) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Console.WriteLine($"Particle graphs {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} bytes.");
                Check(owner == 0 && all == 0, "Prepared graph compute and retained replay allocate zero owner/all-thread managed bytes.");
                if (_frame == 174) Tree!.Quit();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterDomainDraw;
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
