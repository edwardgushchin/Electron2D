using Electron2D;
using V = Electron2D.VisualShader;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderCurves(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(192, 96), Title = "Curve image graphs", UseHDR2D = args.Contains("hdr") };
        var scene = new CurveScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Curve graph host retires after its native checks.");
    }
    private sealed class CurvePainter(Texture texture) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    }
    private sealed class CurveScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<GPUParticles> _particles = [];
        private Curve _curve = null!;
        private ShaderMaterial _blit = null!, _waiting = null!;
        private CurvePainter _live = null!;
        private DirectionalLight _light = null!;
        private CanvasModulate _modulate = null!;
        private int _frame, _draws;
        private bool _alternate;
        private long _owner, _all;
        internal bool Complete;
        private T OwnCurve<T>(T value) where T : Resource { _owned.Add(value); return value; }
        private ShaderMaterial Load(string name)
        {
            var graph = OwnCurve(ResourceLoader.Load<V>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            return OwnCurve(new ShaderMaterial { Shader = graph });
        }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(Colors.White); var white = OwnCurve(ImageTexture.CreateFromImage(pixels));
            foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
                AddChild(new CurvePainter(white) { Position = new(8 + 24 * (int)stage, 8), Material = Load("curves-" + (int)stage) });
            var live = Load("curves-live"); var graph = (V)live.Shader!;
            _curve = ((VisualShaderNodeCurveTexture)graph.GetNode(V.Type.Vertex, 2)).Texture!.Curve!;
            Check(ReferenceEquals(_curve, ((VisualShaderNodeCurveXYZTexture)graph.GetNode(V.Type.Fragment, 2)).Texture!.CurveX), "Imported stages retain the shared authored curve.");
            _live = new CurvePainter(white) { Position = new(64, 8), Material = live }; AddChild(_live);
            _light = new DirectionalLight { Color = Colors.White, Energy = 1, Height = 1, Enabled = false }; AddChild(_light);
            _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_modulate); _blit = Load("curves-" + (int)V.Type.TextureBlit); _waiting = Load("curves-uninitialized");
            var contour = OwnCurve(new OccluderPolygon { Polygon = [new(4, 40), new(144, 40), new(144, 72), new(4, 72)] });
            AddChild(new LightOccluder { Occluder = contour });
            for (var i = 0; i < ParticleStages.Length; i++)
            {
                var particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, ProcessMaterial = Load("curves-" + (int)ParticleStages[i]), Texture = white };
                _particles.Add(particle); AddChild(particle);
                particle.EmitParticle(new Transform(0, new Vector2(16 + i * 24, 56)), Vector2.Zero, Colors.White, Colors.White, (ParticlesEmitFlags)31); particle.RequestParticlesProcess(.125f);
            }
            RenderingServer.FramePostDraw += AfterDraw;
        }
        private void AfterDraw()
        {
            _frame++;
            if (_frame is 1 or 4 or 7 or 10)
            {
                CheckCurveBlit();
                using var image = GetWindow()!.GetTexture().GetImage()!; var marker = new Color(.25f, .5f, .75f);
                NearCurve(image.GetPixel(16, 16), marker, "Vertex curve samples"); NearCurve(image.GetPixel(40, 16), marker, "Fragment curve samples");
                var value = _alternate ? .75f : .5f; NearCurve(image.GetPixel(72, 16), new(value * value, .5f * value, .75f * value), "Live shared numeric curve pixels");
                if (GetWindow()!.UseHDR2D) marker = marker.SRGBToLinear();
                for (var i = 0; i < _particles.Count; i++) { var point = _particles[i].CaptureRect().GetCenter(); NearCurve(image.GetPixel((int)point.X, (int)point.Y), marker, "Particle curve stage " + ParticleStages[i]); }
                if (_frame is 1 or 4) { _alternate = _frame == 1; _curve.SetPointValue(0, _alternate ? .75f : .5f); _curve.SetPointValue(1, _alternate ? .75f : .5f); }
                Console.WriteLine($"Curve native phase {_frame}: eight stages, raw float samples and shared curve rebakes passed.");
            }
            if (_frame == 8) { _light.Enabled = true; _modulate.Color = Colors.Black; }
            if (_frame == 24) { _draws = _live.Draws; _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame == 88)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Curve graph idle: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0 && _live.Draws == _draws, "Prepared curve lookup draws allocate zero managed bytes and retain commands."); Complete = true; Tree!.Quit();
            }
        }
        private void CheckCurveBlit()
        {
            using var target = new DrawableTexture(); target.Setup(CurveSampleCount, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
            if (_frame == 1)
            {
                Reject<InvalidOperationException>(() => target.BlitRectMulti(new(0, 0, CurveSampleCount, 1), [], [], material: _waiting));
                using var unchanged = target.GetImage()!; Check(unchanged.GetPixel(0, 0) == Colors.Magenta, "An uninitialized curve image rejects before modifying the target.");
                var pending = ((VisualShaderNodeCurveXYZTexture)((V)_waiting.Shader!).GetNode(V.Type.TextureBlit, 2)).Texture!;
                pending.CurveX = OwnCurve(CurveLine(.75f, .75f));
                target.BlitRectMulti(new(0, 0, CurveSampleCount, 1), [], [], material: _waiting);
                using var ready = target.GetImage()!; NearCurve(ready.GetPixel(0, 0), new(.75f, 0, 0), "Initializing the same imported curve image makes it drawable");
            }
            using var status = new DrawableTexture(); status.Setup(CurveSampleCount, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
            target.BlitRectMulti(new(0, 0, CurveSampleCount, 1), [], [status], material: _blit); using var image = target.GetImage()!;
            using var x = CurveLine(-.5f, 1.5f); using var y = CurveLine(.25f, .75f); using var z = CurveLine(1.25f, -.75f);
            float Sample(Curve c, float u) { var at = Math.Clamp(u * 32 - .5f, 0, 31); var a = (int)MathF.Floor(at); return Mathf.Lerp(c.SampleBaked(a / 32f), c.SampleBaked(Math.Min(a + 1, 31) / 32f), at - a); }
            for (var row = 0; row < CurveSampleCount; row++)
            {
                var u = row < CurveCoordinates.Length ? CurveCoordinates[row] : 0; var vector = row is 2 or 4 or 6 or 8 or 10 or 12 or 13;
                var expected = row is 9 or 10 ? Vector3.Zero : row is 7 or 8 ? Vector3.One : row is 11 or 12 ? Vector3.Zero : row == 13 ? new(Sample(x, u), 0, 0) : vector ? new(Sample(x, u), Sample(y, u), Sample(z, u)) : Vector3.One * Sample(x, u);
                var actual = image.GetPixel(row, 0); NearCurve(actual, new(expected.X, expected.Y, expected.Z), "Raw curve float row " + row);
            }
            using var checks = status.GetImage()!; Check(checks.GetPixel(0, 0) == Colors.Green, "Every curve sample matches the independent CPU interpolation oracle.");
        }
        private static void NearCurve(Color actual, Color expected, string message) => Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f, $"{message}: {actual}, expected {expected}.");
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterDraw;
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
