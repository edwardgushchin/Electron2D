using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderTextureSize(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(168, 208), Title = "Canvas command texture size", UseHDR2D = args.Contains("hdr") };
        var scene = new TextureSizeScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Texture-size scene retires its native session.");
    }
    private sealed class TextureSizePainter(Texture?[] textures, ArrayMesh mesh, MultiMesh multi) : Entity
    {
        internal int Draws;
        protected override void OnDraw()
        {
            Draws++;
            for (var row = 0; row < textures.Length; row++)
            {
                var y = row * 12;
                if (row == 10) DrawColoredPolygon([new(0, y), new(8, y), new(8, y + 8), new(0, y + 8)], Colors.White, [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], textures[row]);
                else if (row == 11) DrawMesh(mesh, textures[row], new Transform(0, new Vector2(0, y)));
                else if (row == 12) { DrawSetTransform(new(0, y)); DrawMultiMesh(multi, textures[row]); DrawSetTransform(Vector2.Zero); }
                else if (row == 9) DrawTextureRectRegion(textures[row]!, new(0, y, 8, 8), new(0, 0, -2, 3), transpose: true, clipUV: false);
                else if (textures[row] is { } texture) DrawTextureRect(texture, new(0, y, 8, 8), false);
                else DrawRect(new(0, y, 8, 8), Colors.White);
            }
        }
    }
    private sealed class TextureSizeScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<ShaderMaterial> _materials = [];
        private readonly List<TextureSizePainter> _painters = [];
        private ImageTexture _dynamic = null!, _override = null!;
        private CanvasTexture _channels = null!;
        private ImageTexture _alternate = null!;
        private Vector2[] _sizes = [];
        private int _frame, _draws; private float _gain = 1; private long _owner, _all;
        internal bool Complete;
        private T KeepSize<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        private ImageTexture SizeImage(int width, int height)
        { using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8); image.Fill(Colors.White); return KeepSize(ImageTexture.CreateFromImage(image)); }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            var image = SizeImage(4, 8); _override = SizeImage(2, 3); _override.SetSizeOverride(new(8, 4));
            var atlas = KeepSize(new AtlasTexture { Atlas = image, Region = new(1, 1, 2, 3), Margin = new(0, 0, 1, 1) });
            _channels = KeepSize(new CanvasTexture { DiffuseTexture = image, NormalTexture = SizeImage(32, 16) });
            var empty = KeepSize(new CanvasTexture()); _alternate = SizeImage(16, 4); _dynamic = SizeImage(8, 8);
            var viewport = new SubViewport { Size = new(16, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always }; AddChild(viewport); viewport.AddChild(new ColorRect { Size = new(16, 8), Color = Colors.White });
            var device = RenderingServer.GetRenderingDevice()!;
            using var format = new RDTextureFormat { Width = 8, Height = 16, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
            using var view = new RDTextureView(); var data = Enumerable.Repeat((byte)255, 8 * 16 * 4).ToArray();
            var rid = device.TextureCreate(format, view, [data]); var textureRD = KeepSize(new TextureRD { TextureRDRID = rid });
            var mesh = KeepSize(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            { Vertices = [Vector2.Zero, new(8, 0), new(8, 8), new(0, 8)], UVs = [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], Indices = [0, 1, 2, 0, 2, 3] });
            var multi = KeepSize(new MultiMesh { Mesh = mesh, InstanceCount = 1 }); multi.SetInstanceTransform2D(0, Transform.Identity);
            Texture?[] textures = [null, image, _override, atlas, _channels, empty, textureRD, viewport.GetTexture(), _dynamic, image, _override, image, image];
            _sizes = [Vector2.One, new(4, 8), new(8, 4), new(4, 8), new(4, 8), Vector2.One, new(8, 16), new(16, 8), new(8, 8), new(4, 8), new(8, 4), new(4, 8), new(4, 8), new(4, 8)];
            foreach (var name in TextureSizeGraphs.Concat(["size-glsl", "size-hlsl"]))
            {
                var shader = name.EndsWith("glsl", StringComparison.Ordinal) || name.EndsWith("hlsl", StringComparison.Ordinal)
                    ? KeepSize(Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, name + ".spv"))))
                    : KeepSize(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
                var material = KeepSize(new ShaderMaterial { Shader = shader }); material.SetShaderParameter("gain", 1f); var column = _materials.Count;
                Check(!shader.GetShaderUniformList().Any(p => p.Name == "TEXTURE_PIXEL_SIZE"), "Command size is hidden from material descriptors.");
                Reject<ArgumentException>(() => material.SetShaderParameter("TEXTURE_PIXEL_SIZE", Vector2.One));
                _materials.Add(material); var painter = new TextureSizePainter(textures, mesh, multi) { Position = new(4 + column * 23, 4), Material = material }; _painters.Add(painter); AddChild(painter);
                AddChild(new NinePatchRect { Position = new(4 + column * 23, 160), Size = new(8, 8), Texture = image, Material = material });
            }
            Reject<ArgumentException>(() => RenderingServer.GlobalShaderParameterAdd("TEXTURE_PIXEL_SIZE", RenderingServer.GlobalShaderParameterType.Vec2, Vector2.One));
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94) { _gain = (_frame & 1) == 0 ? .5f : 1f; foreach (var material in _materials) material.SetShaderParameter("gain", _gain); }
        }
        private void AfterDraw()
        {
            _frame++;
            if (_frame is 2 or 5 or 8) VerifySizePixels();
            if (_frame == 3)
            {
                _override.SetSizeOverride(new(4, 16)); _channels.DiffuseTexture = _alternate;
                using var image = Image.CreateEmpty(16, 8, false, Image.Format.Rgba8); image.Fill(Colors.White); _dynamic.SetImage(image);
                _sizes[2] = _sizes[10] = new(4, 16); _sizes[4] = new(16, 4); _sizes[8] = new(16, 8);
            }
            if (_frame == 30) _draws = _painters.Sum(p => p.Draws);
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Texture size {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared command size and material writes allocate zero managed bytes."); VerifySizePixels();
                if (_frame == 174) { Check(_painters.Sum(p => p.Draws) == _draws, "Material writes retain authored geometry."); Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifySizePixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            for (var column = 0; column < _materials.Count; column++) for (var row = 0; row < _sizes.Length; row++)
                {
                    var size = _sizes[row]; var expected = new Color(_gain / size.X, _gain / size.Y, .25f * _gain, 1);
                    var actual = image.GetPixel(8 + column * 23, row == 13 ? 164 : 8 + row * 12);
                    Check(MathF.Abs(actual.R - expected.R) < .004f && MathF.Abs(actual.G - expected.G) < .004f && MathF.Abs(actual.B - expected.B) < .004f,
                        $"Command texture size column {column}, row {row}, phase {_frame}: {actual}, expected {expected}.");
                }
            Console.WriteLine($"Texture size phase {_frame}: seven graph/source programs and 98 command pixels passed.");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterDraw;
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
