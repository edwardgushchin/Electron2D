using Electron2D;
using System.Runtime.InteropServices;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderPointInputs(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(880, 456), Title = "Canvas point inputs", UseHDR2D = args.Contains("hdr") };
        var scene = new PointInputScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "Point host retires the renderer after actual pixel checks.");
    }
    private sealed class PointPatch(int kind, ArrayMesh mesh, MultiMesh? multi, RID particles) : Entity
    {
        protected override void OnDraw()
        {
            if (particles.IsValid()) RenderingServer.CanvasItemAddParticles(GetCanvasItem(), particles);
            else if (multi is not null) DrawMultiMesh(multi);
            else if (kind is 0 or 9) { DrawPrimitive([new(16.5f, 16.5f)], [Colors.White], []); DrawPrimitive([new(40.5f, 16.5f)], [Colors.White], []); }
            else DrawMesh(mesh);
        }
    }
    private readonly record struct PointProbe(string Program, int Kind, Vector2 Origin);
    private sealed class PointInputScene(string folder) : Node
    {
        private readonly List<Resource> _owned = []; private readonly List<ShaderMaterial> _materials = []; private readonly List<PointProbe> _probes = []; private readonly List<RID> _particles = [], _skeletons = [];
        private DirectionalLight _first = null!, _second = null!; private CanvasModulate _modulate = null!;
        private readonly long[] _samples = new long[256 * 5];
        private int _frame, _diameter = 9, _lights; private long _owner, _all; internal bool Complete;
        private T KeepPoint<T>(T value) where T : Resource { _owned.Add(value); return value; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            _first = new DirectionalLight { Energy = .25f, Height = 1, Enabled = false }; _second = new DirectionalLight { Energy = .25f, Height = 1, Enabled = false };
            _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_first); AddChild(_second); AddChild(_modulate);
            var mesh = KeepPoint(new ArrayMesh()); var cpu = KeepPoint(new ArrayMesh());
            foreach (var target in new[] { mesh, cpu }) target.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new()
            {
                Vertices = [new(16.5f, 16.5f), new(40.5f, 16.5f)],
                Colors = [Colors.White, Colors.White],
                Indices = [1, 0],
                Bones = target == cpu ? new int[8] : [],
                Weights = target == cpu ? [1, 0, 0, 0, 1, 0, 0, 0] : []
            });
            var procedural = KeepPoint(new ArrayMesh { CustomAABB = new(-64, -64, 128, 128) });
            procedural.AddSurfaceFromArrays(Mesh.PrimitiveType.Points, new() { VertexCount = 2, Indices = [1, 0], Colors = [Colors.White, Colors.White] }, flags: Mesh.ArrayFormat.UseEmptyVertexArray);
            var process = KeepPoint(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "support/point-process.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var processMaterial = KeepPoint(new ShaderMaterial { Shader = process });
            for (var row = 0; row < PointPrograms.Length; row++)
            {
                var name = PointPrograms[row];
                var shader = KeepPoint(name.StartsWith("raw-", StringComparison.Ordinal)
                    ? Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, "point-" + name + ".vert.spv")), File.ReadAllBytes(IOPath.Combine(folder, "point-" + name + ".frag.spv")))
                    : ResourceLoader.Load<Shader>(IOPath.Combine(folder, "point-" + name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
                var material = KeepPoint(new ShaderMaterial { Shader = shader }); _materials.Add(material);
                if (name is not "default" and not "output-default") material.SetShaderParameter("diameter", 9f);
                for (var kind = name == "procedural" ? 10 : 0; kind < (name == "procedural" ? 11 : 10); kind++)
                {
                    var origin = new Vector2(4 + kind * 76, 4 + row * 34); MultiMesh? multi = null; RID particles = default;
                    if (kind is 2 or 3 or 4)
                    {
                        multi = KeepPoint(new MultiMesh { Mesh = kind == 3 ? cpu : mesh, InstanceCount = 1, CustomAABB = new(-64, -64, 128, 128) }); multi.SetInstanceTransform2D(0, Transform.Identity);
                        if (kind == 4)
                        {
                            var rid = RenderingServer.MultiMeshGetBufferRDRID(multi.GetRID()); var data = multi.Buffer; data[3] = 2;
                            RenderingServer.GetRenderingDevice()!.BufferUpdate(rid, 0, (uint)(data.Length * sizeof(float)), MemoryMarshal.AsBytes(data.AsSpan()));
                            Check(multi.GetInstanceTransform2D(0).Origin == Vector2.Zero, "Device-written point instances preserve independent CPU storage.");
                        }
                    }
                    if (kind is >= 5 and <= 7)
                    {
                        particles = RenderingServer.ParticlesCreate(); _particles.Add(particles);
                        RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetEmitting(particles, false); RenderingServer.ParticlesSetLifetime(particles, 1000);
                        RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetInterpolate(particles, false);
                        RenderingServer.ParticlesSetUseLocalCoordinates(particles, kind != 7); RenderingServer.ParticlesSetCustomAABB(particles, new(-64, -64, 128, 128));
                        RenderingServer.ParticlesSetProcessMaterial(particles, processMaterial.GetRID()); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, (kind == 6 ? cpu : mesh).GetRID());
                        if (kind == 6) { RenderingServer.ParticlesSetTrailBindPoses(particles, [Transform.Identity, Transform.Identity]); RenderingServer.ParticlesSetTrails(particles, true, .5f); }
                        var emission = new Transform(0, new Vector2(2, 3)); if (kind == 7) RenderingServer.ParticlesSetEmissionTransform(particles, emission);
                        RenderingServer.ParticlesEmit(particles, kind == 7 ? emission : Transform.Identity, Vector2.Zero, Colors.White, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .125f);
                    }
                    var patch = new PointPatch(kind, kind == 8 ? cpu : kind == 10 ? procedural : mesh, multi, particles) { Position = kind == 9 ? Vector2.Zero : origin, Material = material };
                    if (kind == 9) { var clip = new Control { Position = origin, Size = new(44, 18), ClipContents = true }; clip.AddChild(patch); AddChild(clip); }
                    else AddChild(patch);
                    if (kind == 8)
                    {
                        var palette = RenderingServer.SkeletonCreate(); _skeletons.Add(palette); RenderingServer.SkeletonAllocateData(palette, 1);
                        RenderingServer.SkeletonSetBaseTransform2D(palette, new Transform(0, origin)); RenderingServer.SkeletonBoneSetTransform2D(palette, 0, new Transform(0, new Vector2(2, 1))); RenderingServer.CanvasItemAttachSkeleton(patch.GetCanvasItem(), palette);
                    }
                    _probes.Add(new(name, kind, origin));
                }
            }
            Tree!.ProcessFrameStarted += BeginPointProcess; RenderingServer.FramePreDraw += BeforePointDraw; RenderingServer.FramePostDraw += AfterPoints;
        }
        private void BeginPointProcess(SceneTree tree) => SamplePointPhase(0);
        private void BeforePointDraw() => SamplePointPhase(3);
        private void SamplePointPhase(int phase) { if (_frame < 256) _samples[_frame * 5 + phase] = GC.GetAllocatedBytesForCurrentThread(); }
        protected override void OnProcess(double delta)
        {
            SamplePointPhase(1);
            if (_frame is >= 40 and < 104)
            {
                _diameter = (_frame & 1) == 0 ? 5 : 13; SetDiameter();
                GetWindow()!.CanvasTransform = new Transform(0, (_frame & 1) == 0 ? new Vector2(3, 2) : Vector2.Zero);
            }
            SamplePointPhase(2);
        }
        private void SetDiameter()
        {
            for (var i = 0; i < _materials.Count; i++) if (PointPrograms[i] is not "default" and not "output-default") _materials[i].SetShaderParameter("diameter", (float)_diameter);
        }
        private void AfterPoints()
        {
            SamplePointPhase(4); _frame++;
            if (_frame is 2 or 5 or 8 or 11 or 14 or 17 or 108) VerifyPoints();
            if (_frame is 3 or 6) { _diameter = _frame == 3 ? 5 : 13; SetDiameter(); }
            if (_frame == 9) foreach (var material in _materials) { var shader = material.Shader!; shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV()); }
            if (_frame == 12) { _lights = 1; _first.Enabled = true; _modulate.Color = Colors.Black; }
            if (_frame == 15) { _lights = 2; _second.Enabled = true; }
            if (_frame == 106) { _lights = 0; _first.Enabled = _second.Enabled = false; _modulate.Color = Colors.White; }
            if (_frame is 40 or 120) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 104 or 184)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Point inputs {(_frame == 104 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                if (owner != 0 || all != 0)
                    for (var frame = _frame - 64; frame < _frame; frame++) for (var phase = 0; phase < 5; phase++)
                        {
                            var before = frame == _frame - 64 && phase == 0 ? _owner : _samples[frame * 5 + phase - 1];
                            var difference = _samples[frame * 5 + phase] - before;
                            if (difference != 0) Console.WriteLine($"Point allocation frame={frame}, phase={phase} (input/process-start/parameters/render-prepare/draw), bytes={difference}.");
                        }
                Check(owner == 0 && all == 0, "Prepared point execution allocates no managed bytes."); VerifyPoints();
                if (_frame == 184) { Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyPoints()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var probe in _probes)
            {
                var diameter = probe.Program == "default" ? 1 : probe.Program == "output-default" ? 9 : _diameter;
                var factor = _lights == 0 ? 1 : _lights * .25f;
                var lightOnly = probe.Program is "light" or "fragment-light";
                for (var point = 0; point < 2; point++)
                {
                    var center = GetWindow()!.CanvasTransform * (probe.Origin + new Vector2(16.5f + point * 24 + (probe.Kind is 4 or 8 ? 2 : 0), probe.Kind == 8 ? 17.5f : 16.5f));
                    for (var y = -8; y <= 8; y++) for (var x = -8; x <= 8; x++)
                        {
                            var px = (int)center.X + x; var py = (int)center.Y + y; var inside = Math.Abs(x) <= diameter / 2 && Math.Abs(y) <= diameter / 2 && (probe.Kind != 9 || 16 + point * 24 + x < 44 && 16 + y < 18);
                            var expected = !inside ? Colors.Black : lightOnly && _lights == 0 ? Colors.White : new Color((x + diameter * .5f) / diameter, (y + diameter * .5f) / diameter, .75f) * factor;
                            var pixel = image.GetPixel(px, py);
                            Check(MathF.Abs(pixel.R - expected.R) < .009f && MathF.Abs(pixel.G - expected.G) < .009f && MathF.Abs(pixel.B - expected.B) < .009f,
                                $"Point {probe.Program}/{probe.Kind} frame {_frame}, size {diameter}, light {_lights}, ({x},{y}): expected {expected}, got {pixel}.");
                        }
                }
            }
            Console.WriteLine($"Point inputs: {_probes.Count} program/geometry cases, two points each, size {_diameter}, lights {_lights}, frame {_frame} passed.");
        }
        protected override void OnExitTree()
        {
            Tree!.ProcessFrameStarted -= BeginPointProcess; RenderingServer.FramePreDraw -= BeforePointDraw; RenderingServer.FramePostDraw -= AfterPoints;
            foreach (var particles in _particles) RenderingServer.FreeRID(particles);
            foreach (var palette in _skeletons) RenderingServer.FreeRID(palette);
            for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
        }
    }
}
