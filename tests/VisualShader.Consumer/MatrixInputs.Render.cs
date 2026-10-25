using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderMatrixInputs(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(256, 768), Title = "Canvas matrix components", UseHDR2D = args.Contains("hdr") };
        var scene = new MatrixInputScene(folder, args.Contains("modern")); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Matrix input host retires its renderer.");
    }
    private sealed class MatrixInputPainter(ArrayMesh mesh, MultiMesh? multi, Transform local, bool ordinary = false, RID particles = default) : Entity
    {
        protected override void OnDraw()
        {
            DrawSetTransform(new(2, 3));
            if (particles.IsValid()) RenderingServer.CanvasItemAddParticles(GetCanvasItem(), particles);
            else if (ordinary)
                for (var i = 0; i < 16; i++) DrawColoredPolygon([new(i * 4, 0), new(i * 4 + 3, 0), new(i * 4 + 3, 8), new(i * 4, 8)], Colors.White,
                    [new(i, 0), new(i + .75f, 0), new(i + .75f, 1), new(i, 1)]);
            else if (multi is null) DrawMesh(mesh, transform: local); else DrawMultiMesh(multi);
        }
    }
    private sealed class MatrixInputScene(string folder, bool modern) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<(int Row, Transform Model)> _probes = [];
        private readonly List<RID> _particleRIDs = [];
        private readonly List<(int Row, Transform Model)> _secondaryProbes = [], _proceduralProbes = [];
        private SubViewport _secondary = null!, _procedural = null!;
        private ImageTexture _white = null!;
        private DirectionalLight _light = null!; private CanvasModulate _modulate = null!;
        private int _frame; private long _ownerBytes, _allBytes; internal bool Complete;
        private T KeepMatrix<T>(T value) where T : Resource { _owned.Add(value); return value; }
        private static readonly Transform Item = new(new(1.5f, .25f), new(-.5f, 2), new(7, 9));
        private static readonly Transform Local = new(new(.75f, .125f), new(.25f, 1.25f), new(3, 4));
        private static readonly Transform Canvas = new(new(1, .125f), new(.25f, 1), new(2, 3));
        private ArrayMesh ComponentMesh(bool procedural = false)
        {
            var positions = new List<Vector2>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (var i = 0; i < 16; i++)
            {
                var first = positions.Count; positions.AddRange([new(i * 4, 0), new(i * 4 + 3, 0), new(i * 4 + 3, 8), new(i * 4, 8)]);
                uv.AddRange([new(i, 0), new(i + .75f, 0), new(i + .75f, 1), new(i, 1)]); indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
            }
            var mesh = KeepMatrix(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = procedural ? [] : positions.ToArray(), VertexCount = positions.Count, UVs = uv.ToArray(), Indices = indices.ToArray() }, flags: procedural ? Mesh.ArrayFormat.UseEmptyVertexArray : 0);
            if (procedural) mesh.CustomAABB = new(-128, -128, 512, 512); return mesh;
        }
        private ShaderMaterial MatrixMaterial(string name, int row)
        {
            var shader = File.Exists(IOPath.Combine(folder, name + ".vert.spv"))
                ? KeepMatrix(Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, name + ".vert.spv")), File.ReadAllBytes(IOPath.Combine(folder, "matrix.frag.spv"))))
                : KeepMatrix<Shader>(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var material = KeepMatrix(new ShaderMaterial { Shader = shader }); material.SetShaderParameter("row", (float)row);
            if (File.Exists(IOPath.Combine(folder, name + ".vert.spv"))) material.SetShaderParameter("vertexMap", _white);
            Check(!shader.GetShaderUniformList().Any(p => p.Name is "MODEL_MATRIX" or "CANVAS_MATRIX" or "SCREEN_MATRIX"), "Renderer matrices are hidden from material properties.");
            Reject<ArgumentException>(() => material.SetShaderMatrixParameter("MODEL_MATRIX", new float[16])); return material;
        }
        protected override void OnReady()
        {
            using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); white.Fill(Colors.White); _white = KeepMatrix(ImageTexture.CreateFromImage(white));
            OS.LowProcessorUsageMode = false; GetWindow()!.CanvasTransform = Canvas;
            Console.WriteLine($"Matrix backend: {RenderingServer.GetCurrentRenderingMethod()}/{RenderingServer.GetCurrentRenderingDriverName()}.");
            _light = new DirectionalLight { Enabled = false, Color = Colors.White, Energy = 1, Height = 1 }; _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_light); AddChild(_modulate);
            var mesh = ComponentMesh(); var draw = new Transform(0, new Vector2(2, 3));
            var row = 4;
            foreach (var name in modern ? new[] { "matrix-spv13", "matrix-spv15" } : new[] { "matrix-nodes", "matrix-expression", "matrix-glsl", "matrix-hlsl" })
            {
                AddChild(new MatrixInputPainter(mesh, null, Transform.Identity, ordinary: true) { Transform = Item, Material = MatrixMaterial(name, row) }); _probes.Add((row, Item * draw)); row += 24;
                AddChild(new MatrixInputPainter(mesh, null, Local) { Transform = Item, Material = MatrixMaterial(name, row) }); _probes.Add((row, Item * draw * Local)); row += 24;
                foreach (var gpu in new[] { false, true })
                {
                    var multi = KeepMatrix(new MultiMesh { Mesh = mesh, InstanceCount = 2 });
                    for (var i = 0; i < 2; i++) multi.SetInstanceTransform2D(i, new(new(1 + i * .25f, .125f), new(-.25f, 1), new(3 + i * 5, 4)));
                    if (gpu)
                    {
                        var storage = RenderingServer.MultiMeshGetBufferRDRID(multi.GetRID()); var values = multi.Buffer;
                        for (var i = 0; i < 2; i++) { values[i * 8] += .25f; values[i * 8 + 3] += 2; }
                        RenderingServer.GetRenderingDevice()!.BufferUpdate(storage, 0, (uint)(values.Length * sizeof(float)), System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan()));
                        Check(multi.GetInstanceTransform2D(0).X.X == 1, "GPU buffer updates preserve the authored CPU pose snapshot.");
                    }
                    AddChild(new MatrixInputPainter(mesh, multi, Transform.Identity) { Transform = Item, Material = MatrixMaterial(name, row) });
                    for (var i = 0; i < 2; i++)
                    {
                        var pose = multi.GetInstanceTransform2D(i);
                        if (gpu) { pose.X.X += .25f; pose.Origin.X += 2; }
                        _probes.Add((row + i * 12, Item * draw * pose));
                    }
                    row += 32;
                }
                var particles = RenderingServer.ParticlesCreate(); _particleRIDs.Add(particles);
                RenderingServer.ParticlesSetAmount(particles, 2); RenderingServer.ParticlesSetEmitting(particles, false); RenderingServer.ParticlesSetLifetime(particles, 1000);
                RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetInterpolate(particles, false);
                RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetCustomAABB(particles, new(0, 0, 100, 100));
                var process = KeepMatrix(new ShaderMaterial { Shader = KeepMatrix(Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, "matrix-process.spv")), Shader.Mode.Particles)) });
                RenderingServer.ParticlesSetProcessMaterial(particles, process.GetRID()); RenderingServer.ParticlesSetDrawOrder(particles, ParticlesDrawOrder.Lifetime);
                RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID());
                AddChild(new MatrixInputPainter(mesh, null, Transform.Identity, particles: particles) { Transform = Item, Material = MatrixMaterial(name, row) });
                for (var i = 0; i < 2; i++)
                {
                    var pose = new Transform(new(1 + i * .25f, .125f), new(-.25f, 1), new(3 + i * 5, 4));
                    RenderingServer.ParticlesEmit(particles, pose, Vector2.Zero, Colors.White, Colors.White, 31); _probes.Add((row + i * 12, Item * draw * pose));
                }
                RenderingServer.ParticlesRequestProcessTime(particles, .125f); row += 32;
            }
            _secondary = new SubViewport { Size = new(256, 256), RenderTargetUpdateMode = ViewportUpdateMode.Always }; AddChild(_secondary);
            _procedural = new SubViewport { Size = new(256, 256), RenderTargetUpdateMode = ViewportUpdateMode.Always }; AddChild(_procedural);
            foreach (var procedural in new[] { false, true })
            {
                var instances = KeepMatrix(new MultiMesh { Mesh = procedural ? ComponentMesh(true) : mesh, InstanceCount = procedural ? 2 : 1, CustomAABB = new(-128, -128, 512, 512) });
                for (var i = 0; i < instances.InstanceCount; i++) instances.SetInstanceTransform2D(i, new(new(1.5f + i * .25f, .25f), new(-.375f, 1), new(11 + i * 5, 7)));
                var viewport = procedural ? _procedural : _secondary;
                viewport.AddChild(new MatrixInputPainter((ArrayMesh)instances.Mesh!, instances, Transform.Identity) { Transform = Item, Material = MatrixMaterial(procedural ? "matrix-nodes" : "matrix-no-id", 4) });
                for (var i = 0; i < instances.InstanceCount; i++) (procedural ? _proceduralProbes : _secondaryProbes).Add((4 + i * 12, Item * draw * instances.GetInstanceTransform2D(i)));
            }
            RenderingServer.FramePostDraw += AfterMatrixDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94) GetWindow()!.CanvasTransform = (_frame & 1) == 0 ? Canvas : Transform.Identity;
        }
        private static float[] Columns(Transform t) => [t.X.X, t.X.Y, 0, 0, t.Y.X, t.Y.Y, 0, 0, 0, 0, 1, 0, t.Origin.X, t.Origin.Y, 0, 1];
        private void AfterMatrixDraw()
        {
            _frame++; if (_frame is 2 or 5 or 8 or 11 or 14 or 17 or 20)
                VerifyMatrixPixels();
            if (_frame == 3) GetWindow()!.CanvasTransform = new(Vector2.Zero, Vector2.Zero, new(2, 3));
            if (_frame == 6) GetWindow()!.CanvasTransform = Canvas;
            if (_frame == 9) GetWindow()!.SnapVerticesToPixel = true;
            if (_frame == 12) GetWindow()!.SnapVerticesToPixel = false;
            if (_frame == 15) { _light.Enabled = true; _modulate.Color = Colors.Black; }
            if (_frame == 18) { _light.Enabled = false; _modulate.Color = Colors.White; }
            if (_frame is 30 or 110) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Console.WriteLine($"Matrices {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared renderer matrix publication allocates no managed bytes.");
                VerifyMatrixPixels();
                if (_frame == 174) { Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyMatrixPixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!; VerifyMatrixImage(image, _probes, GetWindow()!.CanvasTransform);
            using var secondary = _secondary.GetTexture().GetImage()!; VerifyMatrixImage(secondary, _secondaryProbes, Transform.Identity);
            using var procedural = _procedural.GetTexture().GetImage()!; VerifyMatrixImage(procedural, _proceduralProbes, Transform.Identity, true);
            Console.WriteLine($"Matrix phase {_frame}: {(_probes.Count + _secondaryProbes.Count + _proceduralProbes.Count) * 48} components passed.");
        }
        private void VerifyMatrixImage(Image image, List<(int Row, Transform Model)> probes, Transform canvas, bool procedural = false)
        {
            var expectedCanvas = Columns(canvas);
            var screen = new float[] { 2f / image.Width, 0, 0, 0, 0, -2f / image.Height, 0, 0, 0, 0, 1, 0, -1, 1, 0, 1 };
            foreach (var probe in probes)
            {
                var model = Columns(probe.Model);
                for (var i = 0; i < 16; i++)
                {
                    var scale = i is 12 or 13 ? 1f / 512 : .125f; var ss = i is 0 or 5 ? 32 : .125f;
                    var expected = new Color(.5f + model[i] * scale, .5f + expectedCanvas[i] * scale, .5f + screen[i] * ss, 1);
                    var position = new Vector2(5.5f + i * 4, probe.Row + 4);
                    if (procedural) position = canvas * (probe.Model * position);
                    var actual = image.GetPixel((int)position.X, (int)position.Y);
                    Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f,
                        $"Matrix component {i}, row {probe.Row}, phase {_frame}, procedural={procedural}: {actual}, expected {expected}.");
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterMatrixDraw;
            base.Dispose(disposing);
            if (disposing) foreach (var value in _owned) value.Dispose();
        }
    }
}
