using Electron2D;

internal static partial class DefaultChecks
{
    private static void Render(string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var root = new Window { Size = new(160, 256), Title = "Compiled shader uniform defaults", UseHDR2D = args.Contains("hdr") };
        root.AddChild(new DefaultScene()); Engine.Run(root);
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Default render host releases its service.");
    }
    private sealed class DefaultPainter : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(0, 0, 48, 32), Colors.White); }
    }
    private sealed partial class DefaultScene : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<DefaultPainter> _painters = [];
        private readonly List<DefaultPainter> _instances = [];
        private RID _shader, _material; private int _frame; private long _ownerBytes, _allBytes; private int[] _drawCounts = [];
        private T Own<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            var x = 8;
            foreach (var language in new[] { "hlsl", "glsl" })
            {
                var shader = Own(Load(language)); var material = Own(new ShaderMaterial { Shader = shader }); Roles(material, true);
                var painter = new DefaultPainter { Position = new(x, 8), Material = material }; _painters.Add(painter); AddChild(painter); x += 64;
            }
            _shader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(_shader, ((ShaderMaterial)_painters[0].Material!).Shader!.GetSPIRV());
            _material = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(_material, _shader);
            var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, GetWindow()!.FindWorld()!.Canvas);
            RenderingServer.CanvasItemSetMaterial(item, _material); RenderingServer.CanvasItemAddRect(item, new(8, 56, 48, 32), Colors.White);
            x = 8;
            foreach (var language in new[] { "hlsl", "glsl" })
            {
                var shader = Own(Shader.CreateFromSPIRV(Stage("Instance.frag." + language))); var material = Own(new ShaderMaterial { Shader = shader });
                var instance = new DefaultPainter { Position = new(x, 104), Material = material }; AddChild(instance); _instances.Add(instance); InstanceRoles(instance, true);
                var pairedShader = Own(Paired(language)); var pairedMaterial = Own(new ShaderMaterial { Shader = pairedShader }); Roles(pairedMaterial, true);
                var paired = new DefaultPainter { Position = new(x, 152), Material = pairedMaterial }; AddChild(paired); _painters.Add(paired); x += 64;
            }
            Check(RenderingServer.ShaderGetParameterDefault<float>(_shader, "f1") == .125f, "Owned shader defaults.");
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(_material, "f1"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(default, "f1"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<int>(_shader, "f1"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(_shader, "f1a"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefaultArray<float>(_shader, "f1"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(_shader, "F1"));
            Reject<ArgumentException>(() => RenderingServer.ShaderGetMatrixParameterDefault(_shader, "f1"));
            var untouched = new float[] { 12, 13, 14 };
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(_shader, "f1a", untouched));
            Check(untouched.SequenceEqual(new float[] { 12, 13, 14 }), "Rejected span length preserves destination.");
            var stale = RenderingServer.ShaderCreate(); RenderingServer.FreeRID(stale);
            Reject<ArgumentException>(() => RenderingServer.ShaderGetParameterDefault<float>(stale, "f1"));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.ShaderGetParameterDefault<float>(_shader, "f1"))).GetAwaiter().GetResult();
            Domains();
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            _frame++;
            if (_frame == 4)
            {
                foreach (var painter in _painters) ((ShaderMaterial)painter.Material!).SetShaderParameter("f1", 0f);
                RenderingServer.MaterialSetParam(_material, "f1", 0f);
                foreach (var instance in _instances) instance.SetInstanceShaderParameter<float>("f1", 0f);
                foreach (var process in _processes) process.SetShaderParameter("enabled", false);
            }
            if (_frame == 5)
            {
                foreach (var painter in _painters) ((ShaderMaterial)painter.Material!).SetShaderParameter("f1", null);
                RenderingServer.MaterialSetParam(_material, "f1", default(RID));
                foreach (var instance in _instances) instance.SetInstanceShaderParameter<float>("f1", null);
                foreach (var process in _processes) process.SetShaderParameter("enabled", null);
            }
            if (_frame > 21 && _frame <= 85)
            {
                RenderingServer.MaterialSetParam(_material, "f1", .125f); RenderingServer.MaterialSetParam(_material, "f1", default(RID));
                Check(RenderingServer.ShaderGetParameterDefault<float>(_shader, "f1") == .125f, "Prepared default query.");
                Span<float> target = stackalloc float[2]; RenderingServer.ShaderGetParameterDefault(_shader, "f1a", target);
                Span<float> matrix = stackalloc float[18]; RenderingServer.ShaderGetMatrixParameterDefault(_shader, "m3a", matrix);
                foreach (var instance in _instances) { instance.SetInstanceShaderParameter<float>("f1", .125f); instance.SetInstanceShaderParameter<float>("f1", null); }
            }
        }
        private void AfterDraw()
        {
            if (_frame is >= 3 and <= 5)
            {
                using var pixels = GetWindow()!.GetTexture().GetImage()!; var expected = _frame == 4 ? new Color(1f / 255, 0, 1, 1) : Colors.Green;
                foreach (var point in new[] { new Vector2i(24, 24), new Vector2i(88, 24), new Vector2i(24, 72), new Vector2i(24, 120), new Vector2i(88, 120), new Vector2i(24, 168), new Vector2i(88, 168) })
                {
                    var actual = pixels.GetPixel(point.X, point.Y);
                    Check(MathF.Abs(actual.R - expected.R) < .015f && MathF.Abs(actual.G - expected.G) < .015f && MathF.Abs(actual.B - expected.B) < .015f, $"All default bytes execute in frame {_frame} at {point}: {actual}.");
                }
                if (_frame == 3) _drawCounts = _painters.Concat(_instances).Select(p => p.Draws).ToArray();
                if (_frame == 5) Check(_painters.Concat(_instances).Select(p => p.Draws).SequenceEqual(_drawCounts), "Numeric override/reset reuses retained draw commands.");
                var particleColor = _frame == 4 ? Colors.Magenta : new Color(.25f, .5f, .75f, 1);
                if (GetWindow()!.UseHDR2D) particleColor = particleColor.SRGBToLinear();
                foreach (var x in new[] { 24, 88 }) Near(pixels.GetPixel(x, 216), particleColor, "GPU particle compiled default/reset pixels");
            }
            if (_frame is 21 or 106) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 85 or 170)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Console.WriteLine($"Defaults {(_frame == 85 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} bytes.");
                Check(owner == 0 && all == 0, "Prepared scalar/default array/matrix queries and reset allocate no managed bytes.");
                if (_frame == 170) { Console.WriteLine("Native HLSL/GLSL defaults, RID queries, reset and retained pixels passed."); Tree!.Quit(); }
            }
        }
        protected override void Dispose(bool disposing) { foreach (var resource in _owned) resource.Dispose(); base.Dispose(disposing); }
    }
}
