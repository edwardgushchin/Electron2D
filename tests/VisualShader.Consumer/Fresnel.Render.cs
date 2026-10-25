using Electron2D;
using V = Electron2D.VisualShader;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static readonly string[] FresnelNames = ["fresnel_normal", "fresnel_view", "fresnel_invert", "fresnel_power", "fresnel_expected"];
    private static void FresnelOverride(ShaderMaterial material, bool alternate)
    {
        if (!alternate) { foreach (var name in FresnelNames) material.SetShaderParameter(name, (Texture?)null); return; }
        material.SetShaderParameter("fresnel_normal", FresnelAlternate.Normal); material.SetShaderParameter("fresnel_view", FresnelAlternate.View);
        material.SetShaderParameter("fresnel_invert", FresnelAlternate.Invert); material.SetShaderParameter("fresnel_power", FresnelAlternate.Power);
        material.SetShaderParameter("fresnel_expected", FresnelExpected(FresnelAlternate));
    }
    internal static void RenderFresnel(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(256, 216), Title = "Fresnel graphs and native canvas normals", UseHDR2D = args.Contains("hdr") };
        var scene = new FresnelScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Fresnel native host retires after all checks.");
    }
    private sealed class FresnelNormalPainter(Texture[] textures, ArrayMesh mesh) : Entity
    {
        internal int Draws;
        protected override void OnDraw()
        {
            Draws++;
            for (var column = 0; column < textures.Length; column++) for (var flags = 0; flags < 8; flags++)
                {
                    var x = 8 + column * 18; var y = 70 + flags * 16; var flipX = (flags & 1) != 0; var flipY = (flags & 2) != 0;
                    if (column == 2) DrawTextureRectRegion(textures[column], new(x, y, 12, 12), new(1, 1, flipX ? -1 : 1, flipY ? -1 : 1), transpose: (flags & 4) != 0, clipUV: false);
                    else DrawTextureRect(textures[column], new(x, y, flipX ? -12 : 12, flipY ? -12 : 12), false, transpose: (flags & 4) != 0);
                }
            DrawTextureRect(textures[1], new(112, 90, -12, 12), false);
            DrawMesh(mesh, textures[1], new Transform(0, new Vector2(128, 90)));
            DrawTextureRect(textures[1], new(144, 90, 12, -12), false);
        }
    }
    private sealed class FresnelScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<ShaderMaterial> _canvas = [];
        private readonly List<GPUParticles> _particles = [];
        private ShaderMaterial _normal = null!, _blit = null!;
        private CanvasTexture[] _channels = [];
        private ImageTexture _firstNormal = null!, _alternateNormal = null!;
        private FresnelNormalPainter _painter = null!;
        private Color[] _encoded = [];
        private int _frame, _draws;
        private bool _alternate, _lit;
        private DirectionalLight _light = null!;
        private CanvasModulate _modulate = null!;
        private Vector3 _view = new(.25f, -.125f, .5f);
        private float _power = 2;
        private bool _invert;
        private long _ownerBytes, _allBytes;
        internal bool Complete;
        private T OwnFresnel<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        private ShaderMaterial Load(string name)
        {
            var graph = OwnFresnel(ResourceLoader.Load<V>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            return OwnFresnel(new ShaderMaterial { Shader = graph });
        }
        private ImageTexture MakeImage(Color color, Image.Format format = Image.Format.Rgbaf)
        { using var image = Electron2D.Image.CreateEmpty(2, 2, false, format); image.Fill(color); return OwnFresnel(ImageTexture.CreateFromImage(image)); }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            var white = MakeImage(Colors.White, Electron2D.Image.Format.Rgba8);
            _light = new DirectionalLight { Color = Colors.White, Energy = 1, Height = 1, Enabled = false };
            _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_modulate); AddChild(_light);
            foreach (var stage in new[] { V.Type.Vertex, V.Type.Fragment })
            {
                var material = Load("canvas-fresnel-" + (int)stage); _canvas.Add(material);
                AddChild(new GraphPainter(white) { Material = material, Position = new(128 + (int)stage * 24, 8) });
            }
            var contour = OwnFresnel(new OccluderPolygon { Polygon = [new(112, 30), new(236, 30), new(236, 60), new(112, 60)] });
            AddChild(new LightOccluder { Occluder = contour });
            for (var i = 0; i < ParticleStages.Length; i++)
            {
                var particle = new GPUParticles
                {
                    Amount = 1,
                    Emitting = false,
                    Lifetime = 1000,
                    FixedFPS = 8,
                    Interpolate = false,
                    LocalCoords = true,
                    SpeedScale = 0,
                    ProcessMaterial = Load("particle-fresnel-" + (int)ParticleStages[i]),
                    Texture = white
                };
                _particles.Add(particle); AddChild(particle);
                particle.EmitParticle(new Transform(0, new Vector2(128 + i * 20, 44)), Vector2.Zero, Colors.White, Colors.White, (ParticlesEmitFlags)31); particle.RequestParticlesProcess(.125f);
            }
            _blit = Load("blit-fresnel-" + (int)V.Type.TextureBlit); _normal = Load("normal-fresnel");
            Check(!_normal.Shader!.GetShaderUniformList().Any(p => p.Name == "NORMAL_MAP_FLAGS"), "Per-command normal flags stay renderer-owned.");
            Reject<ArgumentException>(() => _normal.SetShaderParameter("NORMAL_MAP_FLAGS", Vector4.One));
            Reject<ArgumentException>(() => RenderingServer.GlobalShaderParameterAdd("NORMAL_MAP_FLAGS", RenderingServer.GlobalShaderParameterType.Vec4, Vector4.Zero));
            _encoded = [new(128f / 255, 128f / 255, 1, 1), new(.75f, .25f, 1, 1), new(153f / 255, 102f / 255, 1, 1), new(1, 1, 0, 1), new(.1f, .7f, 0, 1)];
            _channels = new CanvasTexture[_encoded.Length];
            for (var column = 0; column < _channels.Length; column++)
            {
                var image = column == 0 ? null : MakeImage(_encoded[column], column == 2 ? Electron2D.Image.Format.Rgba8 : Electron2D.Image.Format.Rgbaf);
                _channels[column] = OwnFresnel(new CanvasTexture { DiffuseTexture = white, NormalTexture = image, SpecularColor = Colors.Black });
                if (column == 4) _firstNormal = image!;
            }
            _alternateNormal = MakeImage(new(.8f, .1f, .1f, 1));
            var mesh = OwnFresnel(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            { Vertices = [new(0, 0), new(12, 0), new(12, 12), new(0, 12)], UVs = [new(1, 0), new(0, 0), new(0, 1), new(1, 1)], Indices = [0, 1, 2, 0, 2, 3] });
            _painter = new FresnelNormalPainter(_channels, mesh) { Material = _normal }; AddChild(_painter);
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94)
            {
                _view = (_frame & 1) == 0 ? new(.25f, -.125f, .5f) : new(-.25f, .25f, .375f);
                _normal.SetShaderParameter("normal_view", _view);
                foreach (var particle in _particles) particle.RequestParticlesProcess(.125f);
            }
        }
        private void Change(bool alternate)
        {
            _alternate = alternate;
            foreach (var material in _canvas) FresnelOverride(material, alternate); FresnelOverride(_blit, alternate);
            _view = alternate ? new(-.25f, .25f, .375f) : new(.25f, -.125f, .5f); _power = alternate ? .5f : 2; _invert = alternate;
            if (alternate)
            { _normal.SetShaderParameter("normal_view", _view); _normal.SetShaderParameter("normal_power", _power); _normal.SetShaderParameter("normal_invert", _invert); }
            else foreach (var name in new[] { "normal_view", "normal_power", "normal_invert" }) _normal.SetShaderParameter(name, (Texture?)null);
            _channels[4].NormalTexture = alternate ? _alternateNormal : _firstNormal;
        }
        private void AfterDraw()
        {
            _frame++;
            if (_frame is 1 or 4 or 7 or 10) { VerifyPixels(); CheckBlit(); if (_frame is 1 or 4) Change(_frame == 1); }
            if (_frame == 8) { _light.Enabled = true; _modulate.Color = Colors.Black; _lit = true; }
            if (_frame == 30) _draws = _painter.Draws;
            if (_frame is 30 or 110) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Console.WriteLine($"Fresnel {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared Fresnel normals, parameter changes and particle dispatch allocate zero managed bytes.");
                if (_frame == 94) VerifyPixels();
                else { Check(_painter.Draws == _draws, "Prepared parameter updates retain the original drawing commands."); Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyPixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!; var marker = new Color(.25f, .5f, .75f);
            for (var i = 0; i < _canvas.Count; i++) Near(image.GetPixel(136 + i * 24, 16), marker, "Canvas stage " + i);
            if (GetWindow()!.UseHDR2D) marker = marker.SRGBToLinear();
            for (var i = 0; i < _particles.Count; i++)
            { var bounds = _particles[i].CaptureRect(); var point = bounds.GetCenter(); Near(image.GetPixel((int)point.X, (int)point.Y), marker, "Particle stage " + ParticleStages[i]); }
            for (var column = 0; column < _channels.Length; column++) for (var flags = 0; flags < 8; flags++)
                {
                    var encoded = column == 4 && _alternate ? new Color(.8f, .1f, .1f, 1) : _encoded[column];
                    var wanted = NormalExpected(encoded, flags, column != 0); var actual = image.GetPixel(14 + column * 18, 76 + flags * 16);
                    Near(actual, new(wanted, wanted, wanted, 1), $"Normal channel {column}, flip/transpose {flags}, phase {_frame}");
                }
            var mesh = NormalExpected(_encoded[1], 0); Near(image.GetPixel(134, 96), new(mesh, mesh, mesh, 1), "Mirrored-UV mesh keeps the attribute normal convention");
            var beforeMesh = NormalExpected(_encoded[1], 1); var afterMesh = NormalExpected(_encoded[1], 2);
            Near(image.GetPixel(118, 96), new(beforeMesh, beforeMesh, beforeMesh, 1), "The preceding flipped rect retains its own batch flags");
            Near(image.GetPixel(150, 96), new(afterMesh, afterMesh, afterMesh, 1), "The following rect cannot merge into a mesh with different flags");
            Console.WriteLine($"Fresnel native phase {_frame}: eight stages, 40 implicit-normal rects and a mirrored-UV mesh passed.");
        }
        private float NormalExpected(Color encoded, int flags, bool mapped = true)
        {
            var x = 2 * encoded.R - 1; var y = 1 - 2 * encoded.G;
            if ((flags & 4) != 0) (x, y) = (y, x);
            if ((flags & 1) != 0) x = -x; if ((flags & 2) != 0) y = -y;
            var z = MathF.Sqrt(MathF.Max(0, 1 - x * x - y * y));
            return FresnelExpected(new(new(x, y, z), _view, _invert, _power)) * (_lit && mapped ? z : 1);
        }
        private void CheckBlit()
        {
            using var target = new DrawableTexture(); target.Setup(FresnelCases.Length + 1, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
            target.BlitRectMulti(new(0, 0, FresnelCases.Length + 1, 1), [], [], material: _blit);
            using var image = target.GetImage()!;
            for (var sample = 0; sample <= FresnelCases.Length; sample++)
            {
                var actual = image.GetPixel(sample, 0); var expected = sample == FresnelCases.Length ? 1 : FresnelExpected(sample == 0 && _alternate ? FresnelAlternate : FresnelCases[sample]);
                Check(MathF.Abs(actual.R - expected) < .0003f && actual.G == 1 && actual.B == 0 && actual.A == 1, $"Fresnel raw blit row {sample}: {actual}, expected {expected}.");
            }
        }
        private static void Near(Color actual, Color wanted, string message) => Check(MathF.Abs(actual.R - wanted.R) < .005f && MathF.Abs(actual.G - wanted.G) < .005f && MathF.Abs(actual.B - wanted.B) < .005f, $"{message}: {actual}, expected {wanted}.");
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterDraw;
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
