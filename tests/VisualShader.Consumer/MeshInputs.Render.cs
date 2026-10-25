using System.Buffers.Binary;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderMeshInputs(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(496, 304), Title = "Mesh channels and logical instance graph inputs", UseHDR2D = args.Contains("hdr") };
        var scene = new MeshInputsScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Mesh input host retires its renderer.");
    }
    private sealed class MeshInputPainter(Texture image, ArrayMesh mesh, MultiMesh multi) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawMesh(mesh, image); DrawMultiMesh(multi, image); }
    }
    private sealed class MeshInputsScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<ShaderMaterial> _materials = [];
        private readonly List<MeshInputPainter> _painters = [];
        private readonly List<GPUParticles> _particles = [];
        private readonly List<(Vector2 Position, bool Gain)> _probes = [];
        private MeshInputPainter _boundary = null!;
        private DirectionalLight _light = null!; private CanvasModulate _modulate = null!;
        private int _frame, _draws; private float _gain = 1; private long _owner, _all;
        internal bool Complete;
        private T KeepMeshInput<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        private ShaderMaterial MaterialFor(string name, Vector4 a, Vector4 b)
        {
            var graph = KeepMeshInput(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var material = KeepMeshInput(new ShaderMaterial { Shader = graph });
            if (name != "mesh-context") { material.SetShaderParameter("expected0", a); material.SetShaderParameter("expected1", b); material.SetShaderParameter("gain", 1f); _materials.Add(material); }
            return material;
        }
        private static MeshCustomData Channel(MeshCustomFormat format, bool second, out Vector4 expected)
        {
            var value = second ? new Vector4(.75f, .25f, .5f, 1) : new Vector4(.25f, .5f, .75f, 1);
            byte[] record;
            if (format is MeshCustomFormat.RGBA8UNORM or MeshCustomFormat.RGBA8SNORM)
            {
                var scale = format == MeshCustomFormat.RGBA8UNORM ? 255 : 127;
                record = second ? [(byte)(scale * .75f), (byte)(scale * .25f), (byte)(scale * .5f), (byte)scale] : [(byte)(scale * .25f), (byte)(scale * .5f), (byte)(scale * .75f), (byte)scale];
                expected = new((float)record[0] / scale, (float)record[1] / scale, (float)record[2] / scale, 1);
            }
            else
            {
                var half = format is MeshCustomFormat.RGHalf or MeshCustomFormat.RGBAHalf;
                var width = format switch { MeshCustomFormat.RFloat => 1, MeshCustomFormat.RGHalf or MeshCustomFormat.RGFloat => 2, MeshCustomFormat.RGBFloat => 3, _ => 4 };
                record = new byte[width * (half ? 2 : 4)]; expected = new(value.X, width >= 2 ? value.Y : 0, width >= 3 ? value.Z : 0, width == 4 ? value.W : 1);
                for (var component = 0; component < width; component++)
                    if (half) BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(component * 2), BitConverter.HalfToUInt16Bits((Half)value[component]));
                    else BinaryPrimitives.WriteSingleLittleEndian(record.AsSpan(component * 4), value[component]);
            }
            return new() { Format = format, Data = Enumerable.Range(0, 4).SelectMany(_ => record).ToArray() };
        }
        private ArrayMesh MeshFor(MeshCustomData? a, MeshCustomData? b)
        {
            var mesh = KeepMeshInput(new ArrayMesh()); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            { Vertices = [Vector2.Zero, new(12, 0), new(12, 12), new(0, 12)], UVs = [Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down], Indices = [0, 1, 2, 0, 2, 3], Custom0 = a, Custom1 = b }); return mesh;
        }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var pixels = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8); pixels.Fill(Colors.White); var image = KeepMeshInput(ImageTexture.CreateFromImage(pixels));
            _light = new DirectionalLight { Color = Colors.White, Energy = 1, Height = 1, Enabled = false }; _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_modulate); AddChild(_light);
            for (var column = 0; column < MeshInputGraphs.Length; column++)
            {
                var name = MeshInputGraphs[column]; var gain = name != "mesh-context";
                for (var format = 0; format < 8; format++)
                {
                    var a = Channel((MeshCustomFormat)format, false, out var expected0); var b = Channel((MeshCustomFormat)format, true, out var expected1);
                    var mesh = MeshFor(a, b); var material = MaterialFor(name, expected0, expected1);
                    var multi = KeepMeshInput(new MultiMesh { Mesh = mesh, UseCustomData = true, InstanceCount = 3 });
                    for (var instance = 0; instance < 3; instance++) { multi.SetInstanceTransform2D(instance, new(0, new Vector2(16 + instance * 16, 0))); multi.SetInstanceCustomData(instance, new(instance, 1, .875f, 1)); }
                    var position = new Vector2(8 + column * 80, 8 + format * 24); var painter = new MeshInputPainter(image, mesh, multi) { Position = position, Material = material }; _painters.Add(painter); AddChild(painter);
                    for (var item = 0; item < 4; item++) _probes.Add((position + new Vector2(item * 16 + 6, 6), gain));
                }
                var missing = MaterialFor(name, Vector4.Zero, Vector4.Zero);
                AddChild(new NinePatchRect { Position = new(8 + column * 80, 208), Size = new(12, 12), Texture = image, Material = missing }); _probes.Add((new(14 + column * 80, 214), gain));
                var particle = new GPUParticles { Amount = 3, Emitting = false, Lifetime = 1000, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, Texture = image, Material = missing, ProcessMaterial = KeepMeshInput(new ShaderMaterial { Shader = KeepMeshInput(Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, "mesh-process.spv")), Shader.Mode.Particles)) }), DrawOrder = ParticlesDrawOrder.Lifetime };
                _particles.Add(particle); AddChild(particle);
                for (var i = 0; i < 3; i++)
                { var position = new Vector2(14 + column * 80 + i * 16, 246); particle.EmitParticle(new(0, position), Vector2.Zero, Colors.White, new(i, 1, .875f, 1), (ParticlesEmitFlags)31); _probes.Add((position, gain)); }
                particle.RequestParticlesProcess(.125f);
            }
            var boundaryMesh = MeshFor(null, null); var boundary = KeepMeshInput(new MultiMesh { Mesh = boundaryMesh, UseCustomData = true, InstanceCount = 65538 });
            var buffer = boundary.Buffer;
            for (var i = 0; i < 65538; i++) { buffer[i * 12] = 1; buffer[i * 12 + 3] = 10000; buffer[i * 12 + 5] = 1; }
            foreach (var i in new[] { 0, 65535, 65536, 65537 })
            { var item = i == 0 ? 0 : i - 65534; buffer[i * 12 + 3] = item * 16; buffer[i * 12 + 8] = i & 65535; buffer[i * 12 + 9] = (i >> 16) + 1; }
            boundary.Buffer = buffer;
            _boundary = new MeshInputPainter(image, boundaryMesh, boundary) { Position = new(8, 278), Material = MaterialFor("mesh-nodes", Vector4.Zero, Vector4.Zero) }; AddChild(_boundary); _painters.Add(_boundary);
            for (var i = 0; i < 4; i++) _probes.Add((new(14 + i * 16, 284), true));
            RenderingServer.FramePostDraw += AfterMeshInputDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94) { _gain = (_frame & 1) == 0 ? .5f : 1f; foreach (var material in _materials) material.SetShaderParameter("gain", _gain); }
        }
        private void AfterMeshInputDraw()
        {
            _frame++;
            if (_frame is 2 or 5 or 8 or 11 or 14 or 18) VerifyMeshInputPixels();
            if (_frame is 3 or 6) { _light.Enabled = _frame == 3; _modulate.Color = _frame == 3 ? Colors.Black : Colors.White; foreach (var particle in _particles) particle.DrawOrder = _frame == 3 ? ParticlesDrawOrder.Index : ParticlesDrawOrder.Lifetime; }
            if (_frame is 9 or 12 or 15) { GetWindow()!.SnapVerticesToPixel = _frame != 12; if (_frame == 15) { _boundary.Visible = false; _probes.RemoveRange(_probes.Count - 4, 4); } }
            if (_frame == 30) _draws = _painters.Sum(p => p.Draws);
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all; Console.WriteLine($"Mesh inputs {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared graph inputs and live uniforms allocate zero managed bytes."); VerifyMeshInputPixels();
                if (_frame == 174) { Check(_painters.Sum(p => p.Draws) == _draws, "Live input uniforms preserve retained geometry."); Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyMeshInputPixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var probe in _probes)
            {
                var actual = image.GetPixel((int)probe.Position.X, (int)probe.Position.Y); var expected = new Color(.25f, .5f, .75f, 1) * (probe.Gain ? _gain : 1);
                Check(MathF.Abs(actual.R - expected.R) < .006f && MathF.Abs(actual.G - expected.G) < .006f && MathF.Abs(actual.B - expected.B) < .006f, $"Mesh input phase {_frame}, at {probe.Position}: {actual}, expected {expected}.");
            }
            Console.WriteLine($"Mesh inputs phase {_frame}: {_probes.Count} native pixels, eight formats, three instances, particles, absent channels and 16-bit identity boundary passed.");
        }
        protected override void Dispose(bool disposing)
        { if (disposing) RenderingServer.FramePostDraw -= AfterMeshInputDraw; try { base.Dispose(disposing); } finally { if (disposing) { for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); _owned.Clear(); } } }
    }
}
