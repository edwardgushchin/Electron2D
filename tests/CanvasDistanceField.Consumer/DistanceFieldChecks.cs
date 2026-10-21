using Electron2D;
using Path = System.IO.Path;

internal static partial class DistanceFieldChecks
{
    private static readonly Color Background = new(.1f, .2f, .3f, 1), Tint = new(.8f, .3f, .1f, .5f);
    private static readonly byte[] Samples = [64, 64, 191, 230, 191, 64, 191, 230, 128, 128, 128, 230, 64, 191, 64, 255];
    internal static void Run(string[] args)
    {
        Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetDistanceFieldMode(default, true));
        if (args.Length > 0 && args[0].StartsWith("integration", StringComparison.Ordinal)) { Integration(args[0], args.Length > 1 ? args[1] : null); return; }
        if (args.Length > 0 && args[0] == "integer") { Integer(); return; }
        var compatibility = args.Length > 0 && args[0] == "compatibility";
        if (compatibility) { Compatibility(); return; }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(320, 256), Title = "Retained MSDF contract" }; root.AddChild(new Scene(args.Length > 1 ? args[1] : null));
        Engine.Run(root); Check(root.IsDisposed && !RenderingServer.IsAvailable, "Render host cleans up.");
        Sessions(); Console.WriteLine("Public retained distance-field workflow passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static Shader Load(string name)
    {
        var file = name.StartsWith("Raw", StringComparison.Ordinal) ? name + ".frag." + (name.EndsWith("HLSL", StringComparison.Ordinal) ? "hlsl" : "glsl") + ".spv" : name + ".spv";
        return Shader.CreateFromSPIRV(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", file)));
    }
    private sealed class Painter(Texture texture, int mode = 0) : Entity
    {
        internal int Draws;
        protected override void OnDraw()
        {
            Draws++;
            if (mode == 1) DrawMSDFTextureRectRegion(texture, new(0, 0, 64, 16), new(0, 0, 4, 1), Tint, pixelRange: 8);
            else if (mode == 2) DrawLCDTextureRectRegion(texture, new(0, 0, 64, 16), new(0, 0, 4, 1), Tint);
            else DrawTextureRect(texture, new(0, 0, 64, 16), false, Tint);
        }
    }
    private sealed class Solid : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 64, 16), Colors.Blue); }
    private sealed partial class Scene(string? output) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<Entity> _nodes = [];
        private readonly List<RID> _items = [];
        private readonly List<(Vector2i Position, int Kind, bool Encoded, string Name)> _probes = [];
        private Painter _borrowed = null!;
        private RID _canvas, _alternate, _gpuItem;
        private GPUParticles _particle = null!;
        private int _frame, _borrowedDraws;
        private long _ownerBytes, _allBytes;
        private bool _borrowedEncoded;
        private T Own<T>(T value) where T : Resource { _owned.Add(value); return value; }
        private Painter Add(Texture texture, int x, int y, bool encoded, int kind = 0, ShaderMaterial? material = null, int mode = 0)
        {
            var node = new Painter(texture, mode) { Position = new(x, y), TextureFilter = TextureFilter.Nearest, Material = material }; _nodes.Add(node); AddChild(node);
            RenderingServer.CanvasItemSetDistanceFieldMode(node.GetCanvasItem(), encoded); Probes(x, y, kind, encoded || mode == 1, "node/" + _nodes.Count); return node;
        }
        private void Probes(int x, int y, int kind, bool encoded, string name)
        { for (var i = 0; i < 4; i++) _probes.Add((new(x + i * 16 + 8, y + 4), kind, encoded, name + "/" + i)); }
        private RID Item(Texture texture, int x, int y, bool encoded, int kind = 0, ShaderMaterial? material = null)
        {
            var rid = RenderingServer.CanvasItemCreate(); _items.Add(rid); RenderingServer.CanvasItemSetParent(rid, _canvas);
            RenderingServer.CanvasItemSetDefaultTextureFilter(rid, TextureFilter.Nearest); RenderingServer.CanvasItemSetDistanceFieldMode(rid, encoded);
            if (material is not null) RenderingServer.CanvasItemSetMaterial(rid, material.GetRID());
            RenderingServer.CanvasItemAddTextureRect(rid, new(x, y, 64, 16), texture.GetRID(), false, Tint);
            Probes(x, y, kind, encoded, "rid/" + _items.Count); return rid;
        }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Background); _canvas = GetWindow()!.FindWorld()!.Canvas;
            using var image = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, Samples); var texture = Own(ImageTexture.CreateFromImage(image));
            Item(texture, 8, 8, true); Add(texture, 88, 8, false); _borrowed = Add(texture, 8, 32, false); Add(texture, 88, 32, true, kind: 1, mode: 1);
            Add(texture, 168, 8, false, kind: 4, mode: 2); Add(texture, 248, 8, true, kind: 4, mode: 2);
            var parent = new Entity(); AddChild(parent); RenderingServer.CanvasItemSetDistanceFieldMode(parent.GetCanvasItem(), true);
            var child = new Painter(texture) { Position = new(168, 32), TextureFilter = TextureFilter.Nearest }; parent.AddChild(child); _nodes.Add(child); Probes(168, 32, 0, false, "not-inherited");
            var solid = new Solid { Position = new(248, 32) }; _nodes.Add(solid); AddChild(solid); RenderingServer.CanvasItemSetDistanceFieldMode(solid.GetCanvasItem(), true);
            var x = 8;
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                var shader = Own(Load("GlyphColor" + language)); var material = Own(new ShaderMaterial { Shader = shader }); material.SetShaderParameter("tint", Colors.White); material.SetShaderParameter("threshold", 0f);
                material.SetShaderParameter("scaleB", 1f); material.SetShaderParameter("scaleC", 0f); material.SetShaderParameter("scaleD", 0f);
                Add(texture, x, 56, true, kind: 2, material: material);
                var raw = Own(Load("Raw" + language)); var rawMaterial = Own(new ShaderMaterial { Shader = raw }); Add(texture, x + 80, 56, true, kind: 3, material: rawMaterial); x += 160;
            }
            Geometry(texture);
            var unique = Own(Load("GlyphColorGLSL")); var alternatingMaterial = Own(new ShaderMaterial { Shader = unique }); alternatingMaterial.SetShaderParameter("tint", Colors.White); alternatingMaterial.SetShaderParameter("scaleB", 1f);
            _alternate = Item(texture, 8, 128, true, kind: 2, material: alternatingMaterial);
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetDistanceFieldMode(_canvas, true));
            var stale = RenderingServer.CanvasItemCreate(); RenderingServer.FreeRID(stale); Reject<ArgumentException>(() => RenderingServer.CanvasItemSetDistanceFieldMode(stale, true));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetDistanceFieldMode(_alternate, true))).GetAwaiter().GetResult();
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            _frame++;
            if (_frame is 2 or 3 or 4) { _borrowedEncoded = _frame != 3; RenderingServer.CanvasItemSetDistanceFieldMode(_borrowed.GetCanvasItem(), _borrowedEncoded); RenderingServer.CanvasItemSetDistanceFieldMode(_gpuItem, _borrowedEncoded); RenderingServer.CanvasItemSetDistanceFieldMode(_particle.GetCanvasItem(), _borrowedEncoded); }
            if (_frame > 4 && _frame <= 85) RenderingServer.CanvasItemSetDistanceFieldMode(_alternate, (_frame & 1) == 0);
            if (_frame == 20) { _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true); }
        }
        private void AfterDraw()
        {
            if (_frame is 21 or 106) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame <= 4)
            {
                using var image = GetWindow()!.GetTexture().GetImage()!;
                foreach (var probe in _probes)
                {
                    if (_frame < 3 && probe.Name.StartsWith("GPU-particle", StringComparison.Ordinal)) continue;
                    var encoded = probe.Position.Y == 36 && probe.Position.X < 72 ? _borrowedEncoded : probe.Encoded;
                    if (probe.Name.StartsWith("GPU-", StringComparison.Ordinal)) encoded = _frame != 3;
                    Match(image.GetPixel(probe.Position.X, probe.Position.Y), Expected((probe.Position.X - 8) % 80 / 16, probe.Kind, encoded), probe.Name + "/frame" + _frame);
                }
                Match(image.GetPixel(280, 36), Colors.Blue, "untextured mode");
                if (_frame == 1) _borrowedDraws = _borrowed.Draws;
                if (_frame == 4) { Check(_borrowed.Draws == _borrowedDraws, "Mode changes reuse retained geometry without redraw."); if (output is not null) image.SavePNG(output); Console.WriteLine($"Retained MSDF: {_probes.Count} probes across ordinary/explicit, owned/borrowed, material, mesh and instance paths."); }
            }
            if (_frame is 85 or 170)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Check(owner == 0 && all == 0, $"Retained MSDF warm phase {_frame}: owner={owner}, all={all}.");
                Console.WriteLine($"Retained MSDF phase {_frame}: 64 complete frames, owner/all managed bytes={owner}/{all}.");
                if (_frame == 85) { foreach (var node in _nodes) { node.Visible = false; if (node is GPUParticles particle) particle.SpeedScale = 0; } foreach (var rid in _items) RenderingServer.CanvasItemSetVisible(rid, false); }
                else Tree!.Quit();
            }
        }
        protected override void Dispose(bool disposing) { foreach (var value in _owned) value.Dispose(); base.Dispose(disposing); }
    }
    private static Color Expected(int texel, int kind, bool encoded)
    {
        var sample = new Color(Samples[texel * 4] / 255f, Samples[texel * 4 + 1] / 255f, Samples[texel * 4 + 2] / 255f, Samples[texel * 4 + 3] / 255f);
        if (kind == 4)
        {
            if (sample.A != 1) return Background;
            return new(sample.R * Tint.A * Tint.R + Background.R * (1 - sample.R * Tint.A), sample.G * Tint.A * Tint.G + Background.G * (1 - sample.G * Tint.A), sample.B * Tint.A * Tint.B + Background.B * (1 - sample.B * Tint.A), 1);
        }
        var median = Math.Max(Math.Min(sample.R, sample.G), Math.Min(Math.Max(sample.R, sample.G), sample.B));
        var coverage = encoded ? Math.Clamp((median - .5f) * (kind == 1 ? 128 : 64) + .5f, 0, 1) : 1;
        var color = Tint;
        if (!encoded && kind == 0) color *= sample;
        if (kind == 3) color = new(color.R * sample.A, color.G * sample.A, color.B * sample.A, color.A);
        color = new(color.R, color.G, color.B, color.A * coverage);
        return new(color.R * color.A + Background.R * (1 - color.A), color.G * color.A + Background.G * (1 - color.A), color.B * color.A + Background.B * (1 - color.A), 1);
    }
    private static void Match(Color actual, Color expected, string name)
    { if (MathF.Abs(actual.R - expected.R) > .018f || MathF.Abs(actual.G - expected.G) > .018f || MathF.Abs(actual.B - expected.B) > .018f || MathF.Abs(actual.A - expected.A) > .018f) throw new InvalidOperationException($"{name}: {actual}, expected {expected}."); }
}
