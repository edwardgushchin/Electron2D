using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderVaryings(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(256, 384), Title = "Typed canvas varying transport", UseHDR2D = args.Contains("hdr") };
        var scene = new VaryingsScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Varying scene closes its native session.");
    }
    private sealed class VaryingsPainter(Texture image, ArrayMesh mesh, MultiMesh multi) : Entity
    {
        internal int Draws;
        protected override void OnDraw()
        {
            Draws++; DrawTextureRect(image, new(0, 0, 20, 20), false);
            DrawMesh(mesh, image, new Transform(0, new Vector2(24, 0)));
            DrawSetTransform(new(48, 0)); DrawMultiMesh(multi, image); DrawSetTransform(Vector2.Zero);
        }
    }
    private sealed class VaryingsGlyphPainter(Texture image, int kind) : Entity
    {
        protected override void OnDraw()
        {
            if (kind == 1) DrawMSDFTextureRectRegion(image, new(0, 0, 8, 8), new(0, 0, 2, 2), pixelRange: .1f);
            else DrawLCDTextureRectRegion(image, new(0, 0, 8, 8), new(0, 0, 2, 2));
        }
    }
    private sealed class VaryingsScene(string folder) : Node
    {
        private readonly List<Resource> _owned = []; private readonly List<ShaderMaterial> _materials = []; private readonly List<VaryingsPainter> _painters = [];
        private DirectionalLight _light = null!; private CanvasModulate _modulate = null!;
        private int _frame, _draws; private float _gain = 1; private long _owner, _all;
        private static readonly int[] GlyphMaterials = [0, 3, 6, 18, 21, 24, 26, 27];
        internal bool Complete;
        private T KeepVarying<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(Colors.White); var image = KeepVarying(ImageTexture.CreateFromImage(pixels));
            var mesh = KeepVarying(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            { Vertices = [Vector2.Zero, new(20, 0), new(20, 20), new(0, 20)], UVs = [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], Indices = [0, 1, 2, 0, 2, 3] });
            var multi = KeepVarying(new MultiMesh { Mesh = mesh, InstanceCount = 1 }); multi.SetInstanceTransform2D(0, Transform.Identity);
            _light = new DirectionalLight { Color = Colors.White, Energy = 1, Height = 1, Enabled = false }; _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_modulate); AddChild(_light);
            foreach (var name in VaryingRenderPrograms)
            {
                Shader graph = VaryingSourcePrograms.Contains(name) ? KeepVarying(Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, name + ".vert.spv")), File.ReadAllBytes(IOPath.Combine(folder, name + ".frag.spv")))) : KeepVarying(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
                var material = KeepVarying(new ShaderMaterial { Shader = graph }); material.SetShaderParameter("gain", 1f); var index = _materials.Count; _materials.Add(material);
                if (graph is VisualShader authored) Check(authored.HasVarying("unused7"), "Compiled graphs retain unused authored declarations.");
                var position = new Vector2(8 + index % 2 * 120, 8 + index / 2 * 26);
                var painter = new VaryingsPainter(image, mesh, multi) { Position = position, Material = material }; _painters.Add(painter); AddChild(painter);
                AddChild(new NinePatchRect { Position = position + new Vector2(72, 0), Size = new(20, 20), Texture = image, Material = material });
            }
            for (var i = 0; i < GlyphMaterials.Length; i++)
                for (var kind = 1; kind <= 2; kind++)
                {
                    var x = i * 32 + (kind - 1) * 16;
                    AddChild(new VaryingsGlyphPainter(image, kind) { Position = new(x, 372), Material = _materials[GlyphMaterials[i]] });
                    AddChild(new VaryingsGlyphPainter(image, kind) { Position = new(x + 8, 372), Material = _materials[2] });
                }
            RenderingServer.FramePostDraw += AfterVaryingDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94)
            { _gain = (_frame & 1) == 0 ? .5f : 1f; foreach (var material in _materials) material.SetShaderParameter("gain", _gain); }
        }
        private void AfterVaryingDraw()
        {
            _frame++;
            if (_frame is 2 or 5 or 8) VerifyVaryingPixels();
            if (_frame is 3 or 6) { _light.Enabled = _frame == 3; _modulate.Color = _frame == 3 ? Colors.Black : Colors.White; }
            if (_frame == 30) _draws = _painters.Sum(p => p.Draws);
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Varyings {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared varying pipelines and shared-stage values allocate zero managed bytes."); VerifyVaryingPixels();
                if (_frame == 174) { Check(_painters.Sum(p => p.Draws) == _draws, "Varying uniforms retain recorded geometry."); Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyVaryingPixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            for (var index = 0; index < _materials.Count; index++)
                for (var kind = 0; kind < 4; kind++)
                    foreach (var point in new[] { (X: 4, Y: 6), (X: 14, Y: 15) })
                    {
                        var actual = image.GetPixel(8 + index % 2 * 120 + kind * 24 + point.X, 8 + index / 2 * 26 + point.Y);
                        var expected = new Color(((point.X + .5f) / 20 * .5f + .125f) * _gain, ((point.Y + .5f) / 20 * .5f + .125f) * _gain, .625f * _gain, 1);
                        Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f,
                            $"Varying {VaryingRenderPrograms[index]} kind {kind} pixel {point}, phase {_frame}: {actual}, expected {expected}.");
                    }
            for (var pair = 0; pair < 16; pair++)
                foreach (var point in new[] { (X: 2, Y: 2), (X: 5, Y: 4) })
                {
                    var actual = image.GetPixel(pair * 16 + point.X, 372 + point.Y); var control = image.GetPixel(pair * 16 + 8 + point.X, 372 + point.Y);
                    Check(MathF.Abs(actual.R - control.R) < .005f && MathF.Abs(actual.G - control.G) < .005f && MathF.Abs(actual.B - control.B) < .005f && control.B > .05f,
                        $"Glyph varying pair {pair}, phase {_frame}: {actual}, control {control}.");
                }
            Console.WriteLine($"Varyings phase {_frame}: {VaryingRenderPrograms.Length} programs, four geometry streams and {VaryingRenderPrograms.Length * 8} interpolated pixels passed.");
        }
        protected override void Dispose(bool disposing)
        { if (disposing) RenderingServer.FramePostDraw -= AfterVaryingDraw; try { base.Dispose(disposing); } finally { if (disposing) { for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); _owned.Clear(); } } }
    }
}
