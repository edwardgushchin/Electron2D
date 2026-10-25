using System.Runtime.InteropServices;
using Electron2D;
using IOPath = System.IO.Path;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static void SwizzleCanvas(string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var root = new Window { Size = new(96, 336), Title = "Texture component views", UseHDR2D = args.Contains("hdr") };
        var scene = new SwizzleScene(); root.AddChild(scene); Engine.Run(root); Check(scene.Complete && root.IsDisposed && !RenderingServer.IsAvailable, "Swizzle canvas session retired.");
    }
    private sealed class SwizzlePainter(Texture texture, ArrayMesh? mesh = null, MultiMesh? multi = null, int glyph = 0) : Entity
    {
        protected override void OnDraw()
        {
            if (glyph == 1) DrawMSDFTextureRectRegion(texture, new(0, 0, 8, 8), new(0, 0, 2, 2), pixelRange: .1f);
            else if (glyph == 2) DrawLCDTextureRectRegion(texture, new(0, 0, 8, 8), new(0, 0, 2, 2));
            else if (multi is not null) DrawMultiMesh(multi, texture);
            else if (mesh is not null) DrawMesh(mesh, texture);
            else DrawTextureRect(texture, new(0, 0, 8, 8), false);
        }
    }
    private sealed class SwizzleScene : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<(Vector2i Left, Vector2i Right, string Name)> _pairs = [];
        private readonly List<ShaderMaterial> _gains = [];
        private readonly List<GPUParticles> _particles = [];
        private TextureRD _view = null!, _control = null!;
        private TextureArrayRD _arrayView = null!, _arrayControl = null!;
        private RenderingDevice _device = null!;
        private RID _viewA, _viewB, _controlA, _controlB, _arrayA, _arrayB, _arrayControlA, _arrayControlB;
        private DrawableTexture _blit = null!; private ShaderMaterial _blitMaterial = null!;
        private int _frame; private long _owner, _all; private bool _alternate;
        internal bool Complete;
        private static readonly Color Input = new(.2f, .4f, .6f, .8f);
        private static Color Mapped(bool alternate) => alternate ? new(Input.G, 0, 1, 1) : new(Input.A, Input.B, Input.R, 1);
        private T Keep<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        private RID Image(Color color, int map, bool array = false, DataFormat encoding = DataFormat.R32G32B32A32SFLOAT)
        {
            var values = new float[16]; for (var i = 0; i < 4; i++) { values[i * 4] = color.R; values[i * 4 + 1] = color.G; values[i * 4 + 2] = color.B; values[i * 4 + 3] = color.A; }
            var bytes = encoding == DataFormat.R8G8B8A8UNORM ? values.Select(v => (byte)MathF.Round(v * 255)).ToArray() : encoding is DataFormat.R32G32B32A32UINT or DataFormat.R32G32B32A32SINT ? MemoryMarshal.AsBytes(values.Select(v => (int)v).ToArray().AsSpan()).ToArray() : MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
            using var format = new RDTextureFormat { Width = 2, Height = 2, Format = encoding, TextureType = array ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = array ? 2u : 1u, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CPURead | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyTo };
            using var view = new RDTextureView();
            if (map == 1) { view.SwizzleR = TextureSwizzle.A; view.SwizzleG = TextureSwizzle.B; view.SwizzleB = TextureSwizzle.R; view.SwizzleA = TextureSwizzle.One; }
            if (map == 2) { view.SwizzleR = TextureSwizzle.G; view.SwizzleG = TextureSwizzle.Zero; view.SwizzleB = TextureSwizzle.One; view.SwizzleA = TextureSwizzle.One; }
            return _device.TextureCreate(format, view, array ? [new byte[bytes.Length], bytes] : [bytes]);
        }
        private ShaderMaterial Material(string name, Texture? source = null)
        {
            var shader = Keep(Load(name, name.StartsWith("Blit", StringComparison.Ordinal))); if (source is not null && name == "SwizzleNamedGLSL") shader.SetDefaultTextureParameter("detail", source);
            var material = Keep(new ShaderMaterial { Shader = shader });
            if (shader.GetShaderUniformList().Any(p => p.Name == "gain")) { material.SetShaderParameter("gain", 1f); _gains.Add(material); }
            return material;
        }
        private void Pair(string name, Texture left, Texture right, ShaderMaterial? a = null, ShaderMaterial? b = null, ArrayMesh? mesh = null, MultiMesh? multi = null, uint lightMask = 1, int glyph = 0, bool nearest = false)
        {
            var y = 4 + _pairs.Count * 12; AddChild(new SwizzlePainter(left, mesh, multi, glyph) { Position = new(8, y), Material = a, LightMask = lightMask, TextureFilter = nearest ? TextureFilter.Nearest : TextureFilter.Linear }); AddChild(new SwizzlePainter(right, mesh, multi, glyph) { Position = new(32, y), Material = b, LightMask = lightMask, TextureFilter = nearest ? TextureFilter.Nearest : TextureFilter.Linear });
            _pairs.Add((new(12, y + 4), new(36, y + 4), name));
        }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black); _device = RenderingServer.GetRenderingDevice()!;
            _viewA = Image(Input, 1); _viewB = Image(Input, 2); _controlA = Image(Mapped(false), 0); _controlB = Image(Mapped(true), 0);
            _arrayA = Image(Input, 1, true); _arrayB = Image(Input, 2, true); _arrayControlA = Image(Mapped(false), 0, true); _arrayControlB = Image(Mapped(true), 0, true);
            _view = Keep(new TextureRD { TextureRDRID = _viewA }); _control = Keep(new TextureRD { TextureRDRID = _controlA }); _arrayView = Keep(new TextureArrayRD { TextureRDRID = _arrayA }); _arrayControl = Keep(new TextureArrayRD { TextureRDRID = _arrayControlA });
            Pair("builtin", _view, _control);
            foreach (var language in new[] { "GLSL", "HLSL" }) Pair(language, _view, _control, Material("SwizzleCanvas" + language), Material("SwizzleCanvas" + language));
            using var whiteImage = Electron2D.Image.CreateEmpty(8, 8, false, Electron2D.Image.Format.Rgba8); whiteImage.Fill(Colors.White); var white = Keep(ImageTexture.CreateFromImage(whiteImage));
            Pair("named", white, white, Material("SwizzleNamedGLSL", _view), Material("SwizzleNamedGLSL", _control));
            var left = Keep(new CanvasTexture { DiffuseTexture = white, NormalTexture = _view, SpecularTexture = _view }); var right = Keep(new CanvasTexture { DiffuseTexture = white, NormalTexture = _control, SpecularTexture = _control });
            Pair("normal", left, right, Material("SwizzleNormalGLSL"), Material("SwizzleNormalGLSL")); Pair("specular", left, right, Material("SwizzleSpecularGLSL"), Material("SwizzleSpecularGLSL"));
            var vertex = File.ReadAllBytes(IOPath.Combine(AppContext.BaseDirectory, "Shaders", "SwizzleVertexGLSL.vert.glsl.spv")); using var pass = Load("PassGLSL"); var paired = Keep(Shader.CreateFromSPIRV(vertex, pass.GetSPIRV()));
            var va = Keep(new ShaderMaterial { Shader = paired }); var vb = Keep(new ShaderMaterial { Shader = paired }); va.SetShaderParameter("gain", 1f); vb.SetShaderParameter("gain", 1f); _gains.Add(va); _gains.Add(vb); Pair("vertex", _view, _control, va, vb);
            RenderingServer.GlobalShaderParameterAdd("swizzle_image", _view); Pair("global", white, white, Material("SwizzleGlobalGLSL"), Material("SwizzleNamedGLSL", _control));
            var aa = Material("SwizzleArrayGLSL"); var ab = Material("SwizzleArrayGLSL"); aa.Shader!.SetDefaultLayeredTextureParameter("detailArray", _arrayView); ab.Shader!.SetDefaultLayeredTextureParameter("detailArray", _arrayControl); Pair("array", white, white, aa, ab);
            Pair("atlas", Keep(new AtlasTexture { Atlas = _view, Region = new(0, 0, 1, 1) }), Keep(new AtlasTexture { Atlas = _control, Region = new(0, 0, 1, 1) }));
            var mesh = Keep(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(0, 0), new(8, 0), new(8, 8), new(0, 8)], UVs = [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], Indices = [0, 1, 2, 0, 2, 3] }); Pair("mesh", _view, _control, mesh: mesh);
            var multi = Keep(new MultiMesh { Mesh = mesh, InstanceCount = 1 }); multi.SetInstanceTransform2D(0, Transform.Identity); Pair("instances", _view, _control, multi: multi);
            var y = 4 + _pairs.Count * 12; AddChild(new NinePatchRect { Position = new(8, y), Size = new(8, 8), Texture = _view }); AddChild(new NinePatchRect { Position = new(32, y), Size = new(8, 8), Texture = _control }); _pairs.Add((new(12, y + 4), new(36, y + 4), "ninepatch"));
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                y = 4 + _pairs.Count * 12;
                for (var side = 0; side < 2; side++)
                {
                    var code = File.ReadAllBytes(IOPath.Combine(AppContext.BaseDirectory, "Shaders", "Particle" + language + ".comp." + (language == "GLSL" ? "glsl" : "hlsl") + ".spv")); var shader = Keep(Shader.CreateFromSPIRV(code, Shader.Mode.Particles)); shader.SetDefaultTextureParameter("pixels", side == 0 ? _view : _control);
                    var particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 0, Interpolate = false, LocalCoords = true, Texture = white, Position = new(side == 0 ? 12 : 36, y + 4), ProcessMaterial = Keep(new ShaderMaterial { Shader = shader }), LightMask = 1 }; AddChild(particle); particle.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, ParticlesEmitFlags.Position); _particles.Add(particle);
                }
                _pairs.Add((new(12, y + 4), new(36, y + 4), "particle " + language));
            }
            AddChild(new DirectionalLight { RangeItemCullMask = 2, Height = 1, Energy = .25f }); Pair("lit builtin", left, right, lightMask: 2); Pair("lit material", left, right, Material("SwizzleCanvasGLSL"), Material("SwizzleCanvasGLSL"), lightMask: 2);
            y = 4 + _pairs.Count * 12; Pair("light mask", white, white, lightMask: 4); AddChild(new PointLight { Position = new(12, y + 4), Texture = _view, TextureScale = 8, RangeItemCullMask = 4, Height = 1, Energy = .5f }); AddChild(new PointLight { Position = new(36, y + 4), Texture = _control, TextureScale = 8, RangeItemCullMask = 4, Height = 1, Energy = .5f });
            Pair("MSDF", _view, _control, glyph: 1);
            Pair("LCD", _view, _control, glyph: 2);
            Pair("MSDF named", _view, _control, Material("SwizzleNamedGLSL", _view), Material("SwizzleNamedGLSL", _control), glyph: 1);
            Pair("LCD named", _view, _control, Material("SwizzleNamedGLSL", _view), Material("SwizzleNamedGLSL", _control), glyph: 2);
            static float Linear(float value) => value <= .04045f ? value / 12.92f : MathF.Pow((value + .055f) / 1.055f, 2.4f);
            var normalized = Keep(new TextureRD { TextureRDRID = Image(Input, 1, encoding: DataFormat.R8G8B8A8UNORM) }); var normalizedColor = GetWindow()!.UseHDR2D ? new Color(Input.A, Linear(Input.B), Linear(Input.R), 1) : Mapped(false); Pair("normalized color", normalized, Keep(new TextureRD { TextureRDRID = Image(normalizedColor, 0) }));
            foreach (var integer in new[] { DataFormat.R32G32B32A32SINT, DataFormat.R32G32B32A32UINT }) Pair(integer.ToString(), Keep(new TextureRD { TextureRDRID = Image(new(0, 0, 1, 1), 1, encoding: integer) }), Keep(new TextureRD { TextureRDRID = Image(new(1, 1, 0, 1), 0) }), nearest: true);
            y = 4 + _pairs.Count * 12;
            for (var side = 0; side < 2; side++) { var mask = new SwizzlePainter(side == 0 ? _view : _control) { Position = new(side == 0 ? 8 : 32, y), ClipChildren = ClipChildrenMode.Only }; mask.AddChild(new SwizzlePainter(white)); AddChild(mask); }
            _pairs.Add((new(12, y + 4), new(36, y + 4), "alpha clip"));
            _blit = Keep(new DrawableTexture()); _blit.Setup(2, 2, DrawableTexture.DrawableFormat.RGBAF); _blitMaterial = Material("BlitSwizzleGLSL");
            RenderingServer.FramePostDraw += AfterDraw;
        }
        private void Change(bool alternate)
        {
            _alternate = alternate; _view.TextureRDRID = alternate ? _viewB : _viewA; _control.TextureRDRID = alternate ? _controlB : _controlA; _arrayView.TextureRDRID = alternate ? _arrayB : _arrayA; _arrayControl.TextureRDRID = alternate ? _arrayControlB : _arrayControlA;
        }
        protected override void OnProcess(double delta)
        { if (_frame is >= 30 and < 94) foreach (var material in _gains) material.SetShaderParameter("gain", (_frame & 1) == 0 ? .5f : 1f); }
        private void AfterDraw()
        {
            _frame++;
            if (_frame is 2 or 5 or 8) { Verify(); if (_frame is 2 or 5) Change(_frame == 2); }
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all; Console.WriteLine($"Swizzle canvas {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes."); Check(owner == 0 && all == 0, "Prepared swizzle frames allocate no managed bytes."); Verify(); if (_frame == 174) { Complete = true; Tree!.Quit(); }
            }
        }
        private void Verify()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var pair in _pairs)
            {
                var a = image.GetPixel(pair.Left.X, pair.Left.Y); var b = image.GetPixel(pair.Right.X, pair.Right.Y);
                Check(MathF.Abs(a.R - b.R) < .005f && MathF.Abs(a.G - b.G) < .005f && MathF.Abs(a.B - b.B) < .005f, $"Swizzle {pair.Name} phase {_frame}: {a}, control {b}."); Check(b.R + b.G + b.B > .01f, "Controls draw visible output: " + pair.Name);
            }
            _blit.BlitRectMulti(new(0, 0, 2, 2), [_view], [], material: _blitMaterial); using var sample = _blit.GetImage()!; var actual = sample.GetPixel(0, 0); var expected = Mapped(_alternate); var stored = _blitMaterial.GetShaderParameter<float>("gain"); expected = new(expected.R * stored, expected.G * stored, expected.B * stored, expected.A * stored); Check(MathF.Abs(actual.R - expected.R) < .0001f && MathF.Abs(actual.G - expected.G) < .0001f && MathF.Abs(actual.B - expected.B) < .0001f, "Writable texture samples the same component view.");
            Console.WriteLine($"Swizzle canvas phase {_frame}: {_pairs.Count} physical-control pairs and native blit passed.");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterDraw;
            try { base.Dispose(disposing); } finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
