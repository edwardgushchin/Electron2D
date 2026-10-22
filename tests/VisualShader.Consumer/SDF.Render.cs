using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderSDF(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(192, 96), Title = "Imported canvas SDF graphs", UseHDR2D = args.Contains("hdr"), SDFScale = CanvasSDFScale.Percent100, SDFOversize = CanvasSDFOversize.Percent100 };
        window.AddChild(new SDFScene(folder)); Check(Engine.Run(window) == 0, "SDF render host exits cleanly.");
        Check(window.IsDisposed && !RenderingServer.IsAvailable, "SDF renderer session retires.");
    }
    private sealed class SDFScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<(GraphPainter Painter, ShaderMaterial Material)> _cases = [];
        private OccluderPolygon _polygon = null!;
        private LightOccluder _occluder = null!;
        private int _frame;
        private long _owner, _all;
        private int[] _draws = [];
        private T Own<T>(T value) where T : Resource { _owned.Add(value); return value; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false;
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); var texture = Own(ImageTexture.CreateFromImage(image));
            _polygon = Own(new OccluderPolygon { Polygon = [new(48, 24), new(80, 24), new(80, 64), new(48, 64)] });
            _occluder = new LightOccluder { Occluder = _polygon }; AddChild(_occluder);
            foreach (var file in Directory.GetFiles(folder, "sdf-*.e2dres").Order(StringComparer.Ordinal))
            {
                var shader = Own(ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep)); var material = Own(new ShaderMaterial { Shader = shader });
                var painter = new GraphPainter(texture) { Position = new(4 + _cases.Count % 12 * 15, 76 - _cases.Count / 12 * 19), Material = material };
                AddChild(painter); _cases.Add((painter, material));
            }
            Check(_cases.Count == 21, "Complete SDF graph corpus."); RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            _frame++;
            if (_frame == 4) _cases[1].Material.SetShaderParameter("sdf_position", new Vector2(90, 40));
            if (_frame == 6) _cases[1].Material.SetShaderParameter("sdf_position", (Texture?)null);
            if (_frame == 8) _occluder.Position = new(8, 0);
            if (_frame == 10) _occluder.SDFCollision = false;
            if (_frame == 12) { _occluder.SDFCollision = true; _polygon.Closed = false; }
            if (_frame == 14) _polygon.Closed = true;
            if (_frame == 16) _occluder.Hide();
            if (_frame == 18) { _occluder.Show(); GetWindow()!.SDFScale = CanvasSDFScale.Percent50; GetWindow()!.SDFOversize = CanvasSDFOversize.Percent200; }
            if (_frame == 20) GetWindow()!.Size = new(224, 112);
            if (_frame is > 40 and <= 104)
            { _occluder.Position = new(_frame % 2 == 0 ? 8 : 9, 0); _cases[1].Material.SetShaderParameter("sdf_position", new Vector2(_frame % 2 == 0 ? 60 : 61, 40)); }
            if (_frame == 105) _cases[1].Material.SetShaderParameter("sdf_position", (Texture?)null);
        }
        private void AfterDraw()
        {
            if (_frame is 3 or 5 or 7 or 9 or 11 or 13 or 15 or 17 or 19 or 23) Pixels();
            if (_frame == 40) { _draws = _cases.Select(c => c.Painter.Draws).ToArray(); _all = GC.GetTotalAllocatedBytes(true); _owner = GC.GetAllocatedBytesForCurrentThread(); }
            if (_frame == 104) { var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all; Check(owner == 0 && all == 0, $"Moving occluder and prepared SDF parameter frames allocated {owner}/{all} B"); Pixels(); }
            if (_frame == 120) { _all = GC.GetTotalAllocatedBytes(true); _owner = GC.GetAllocatedBytesForCurrentThread(); }
            if (_frame == 184)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all; Check(owner == 0 && all == 0, $"Prepared SDF frames allocated {owner}/{all} B");
                Check(_cases.Select((c, i) => c.Painter.Draws == _draws[i]).All(v => v), "SDF field and parameter updates do not rerecord geometry."); Pixels();
                Console.WriteLine("21 SDF programs: distance, gradient, three ray outputs, implicit/explicit UV, collisions, archives, live params, contours, visibility, resize, scale/oversize; 64 moving + 64 steady frames = 0/0 B owner/all managed."); Tree!.Quit();
            }
        }
        private void Pixels()
        {
            using var pixels = GetWindow()!.GetTexture().GetImage()!;
            for (var mode = 0; mode < _cases.Count; mode++)
            {
                // Flat-field normal normalization retains native undefined arithmetic.
                if ((mode is >= 3 and <= 6 or 19) && (_frame is 11 or 17)) continue;
                var p = _cases[mode].Painter.Position + new Vector2(8, 8); var actual = pixels.GetPixel((int)p.X, (int)p.Y); var expected = ExpectedSDF(mode, p + new Vector2(.5f, .5f));
                if (!GetWindow()!.UseHDR2D) expected = new(Math.Clamp(expected.R, 0, 1), Math.Clamp(expected.G, 0, 1), Math.Clamp(expected.B, 0, 1), 1);
                Check(MathF.Abs(actual.R - expected.R) < .009f && MathF.Abs(actual.G - expected.G) < .009f && MathF.Abs(actual.B - expected.B) < .009f && actual.A > .99f, $"SDF {mode} frame {_frame}: {actual}, expected {expected}");
            }
        }
        private Color ExpectedSDF(int mode, Vector2 pixel)
        {
            float Field(Vector2 at) => SampleSDF(at);
            if (mode is <= 2 or 15 or 17 or 18)
            { var point = mode == 0 ? Vector2.Zero : mode == 1 ? new Vector2(_frame == 5 ? 90 : 60, 40) : new Vector2(90, 40); var v = Field(point) / 256 + .5f; return new(v, v, v, 1); }
            if (mode is >= 3 and <= 6 or 19)
            {
                var p = mode switch { 3 or 19 => new Vector2(90, 40), 4 => new Vector2(40, 40), 5 => new Vector2(60, 16), _ => new Vector2(60, 72) };
                var size = GetWindow()!.Size; var region = new Vector2(size.X, size.Y) * (_frame >= 19 ? 2 : 1); var dx = new Vector2(region.X * .001f, 0); var dy = new Vector2(0, region.Y * .001f);
                var gradient = new Vector2(Field(p + dx) - Field(p - dx), Field(p + dy) - Field(p - dy));
                Check(gradient.LengthSquared() > 0, "A nonflat field has a normal.");
                var value = gradient.Normalized() * .25f + new Vector2(.5f, .5f); return new(value.X, value.Y, 0, 1);
            }
            if (mode is >= 7 and <= 11 or 16 or 20)
            {
                var start = mode switch { 8 => new Vector2(60, 40), 9 => new Vector2(16, 10), 16 => new Vector2(90, 40), _ => new Vector2(16, 40) };
                var end = mode switch { 9 => new Vector2(110, 10), 10 => new Vector2(16, 40), 11 => new Vector2(48, 40), 16 => new Vector2(90, 40), _ => new Vector2(110, 40) };
                var limit = (end - start).Length(); if (limit == 0) return new(.5f, 0, start.X / 192, 1);
                var direction = (end - start) / limit; var at = start; var total = 0f; var steps = 0;
                while (total < limit) { Check(steps++ < 10000, "CPU raymarch converges."); var d = Field(at); total += d; if (d < .01f) break; at += d * direction; }
                var travelled = MathF.Min(limit, total); return new(travelled / 256 + .5f, total < limit ? 1 : 0, (start.X + direction.X * travelled) / 192, 1);
            }
            var sizeUV = GetWindow()!.Size;
            var uv = mode == 12 ? new Vector2(.25f, .75f) : mode == 14 ? new Vector2(96f / sizeUV.X, 24f / sizeUV.Y) : new Vector2(pixel.X / sizeUV.X, pixel.Y / sizeUV.Y);
            return new(uv.X, uv.Y, 0, 1);
        }
        private float SampleSDF(Vector2 p)
        {
            var size = GetWindow()!.Size; var regionSize = new Vector2(size.X, size.Y) * (_frame >= 19 ? 2 : 1); var origin = (new Vector2(size.X, size.Y) - regionSize) / 2; var resolution = regionSize * (_frame >= 19 ? .5f : 1);
            var uv = (p - origin) / regionSize; var grid = uv * resolution - new Vector2(.5f, .5f); var x = MathF.Floor(grid.X); var y = MathF.Floor(grid.Y); var fx = grid.X - x; var fy = grid.Y - y;
            float Texel(float ix, float iy)
            {
                var sample = origin + (new Vector2(Math.Clamp(ix, 0, resolution.X - 1), Math.Clamp(iy, 0, resolution.Y - 1)) + new Vector2(.5f, .5f)) * regionSize / resolution;
                if (_frame is 11 or 17) return regionSize.Length() * 2;
                var shift = _frame >= 9 ? 8 : 0; var points = new Vector2[] { new(48 + shift, 24), new(80 + shift, 24), new(80 + shift, 64), new(48 + shift, 64) }; var closest = regionSize.Length() * 2; var inside = false;
                for (var i = 0; i < (_frame == 13 ? 3 : 4); i++)
                {
                    var a = points[i]; var b = points[(i + 1) % 4]; var d = b - a; var t = Math.Clamp((sample - a).Dot(d) / d.LengthSquared(), 0, 1); closest = MathF.Min(closest, (sample - a - d * t).Length());
                    if (_frame != 13 && (a.Y > sample.Y) != (b.Y > sample.Y) && sample.X < (b.X - a.X) * (sample.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
                }
                return inside ? -closest : closest;
            }
            return (Texel(x, y) * (1 - fx) + Texel(x + 1, y) * fx) * (1 - fy) + (Texel(x, y + 1) * (1 - fx) + Texel(x + 1, y + 1) * fx) * fy;
        }
        protected override void OnExitTree() { RenderingServer.FramePostDraw -= AfterDraw; foreach (var resource in _owned) resource.Dispose(); }
    }
}
