using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsMassPerformance
{
    private const double Delta = 1d / 60;
    internal static void Run(bool rendered)
    {
        var count = Read("ELECTRON2D_MASS_COUNT", 4096, 64, 65536);
        var warmup = Read("ELECTRON2D_MASS_WARMUP", 240, 32, 4096);
        var samples = Read("ELECTRON2D_MASS_SAMPLES", 240, 32, 2048);
        var choice = Environment.GetEnvironmentVariable("ELECTRON2D_MASS_BACKEND") ?? "both";
        var selected = choice switch
        {
            "cpu" => new[] { PhysicsServer.Backend.CPU },
            "gpu" => new[] { PhysicsServer.Backend.GPU },
            "both" => new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU },
            _ => throw new ArgumentException("Choose cpu, gpu or both.")
        };
        var profile = PhysicsSpace.ProfilingEnabled;
        PhysicsSpace.ProfilingEnabled = true;
        try
        {
            foreach (var backend in selected)
            {
                using var pile = new Pile(backend, count, warmup, samples);
                if (rendered) Render(pile); else { while (!pile.Complete) pile.Step(); pile.Validate(); }
                pile.Report(rendered);
            }
        }
        finally { PhysicsSpace.ProfilingEnabled = profile; }
    }
    private static int Read(string name, int fallback, int min, int max)
    {
        var value = int.Parse(Environment.GetEnvironmentVariable(name) ?? fallback.ToString());
        if (value < min || value > max) throw new ArgumentOutOfRangeException(name); return value;
    }

    private sealed class Pile : IDisposable
    {
        internal readonly PhysicsServer.Backend Backend;
        internal readonly RID Space;
        internal readonly RID[] Bodies;
        internal readonly int Warmup, Samples;
        internal readonly float Width, Height;
        internal const float Radius = 4;
        private readonly RID[] _walls = new RID[3];
        private readonly CircleShape _particle = new() { Radius = Radius };
        private readonly RectangleShape _floor, _side;
        private readonly PhysicsSpace _data;
        private readonly double[] _steps, _waits, _phases = new double[8];
        private readonly double[] _preparation = new double[4];
        private readonly long[] _up, _down, _submissions;
        private long _owner, _all, _uniformBytes;
        private int _tick, _measured;
        internal bool Complete => _measured == Samples;
        internal int Tick => _tick;
        internal int Count => Bodies.Length;
        internal double MaxSpeed, MaxEscape, Energy, InitialEnergy, MaxPenetration;
        internal int OverlappingPairs;
        internal double[] FrameTimes = new double[16384], PublishTimes;
        internal readonly Transform[] Transforms;
        internal int Frames, PublishCount;
        internal long FrameOwner, FrameAll, FrameStart;
        internal long FrameUploadBytes, FrameReadbackBytes, FrameSubmissions, FrameUniformBytes;
        internal double FrameWaitMS;
        internal double WallSeconds;
        internal string VSync = "none";
        internal string? CapturePath;
        internal Pile(PhysicsServer.Backend backend, int count, int warmup, int samples)
        {
            Backend = backend; Warmup = warmup; Samples = samples;
            Bodies = new RID[count]; Transforms = new Transform[count]; _steps = new double[samples]; _waits = new double[samples];
            _up = new long[samples]; _down = new long[samples]; _submissions = new long[samples]; PublishTimes = new double[samples];
            var columns = (int)Math.Ceiling(Math.Sqrt(count * 1.5)); var rows = (count + columns - 1) / columns;
            Width = (columns + 1) * 8; Height = (rows - 1) * 7 + 16;
            _floor = new() { Size = new(Width + 40, 20) }; _side = new() { Size = new(20, Height + 40) };
            try
            {
                Space = PhysicsServer.SpaceCreate(backend); _data = PhysicsServer.Service.GetSceneSpace(Space);
                _data.ForceGPUParameterRefresh = Environment.GetEnvironmentVariable("ELECTRON2D_MASS_REFRESH_PARAMETERS") == "1";
                _data.DecodeGPUTransforms = Environment.GetEnvironmentVariable("ELECTRON2D_MASS_DECODE_TRANSFORMS") == "1";
                if (_data.GPUStore is { } gpu)
                {
                    gpu.PackColoredContacts = Environment.GetEnvironmentVariable("ELECTRON2D_MASS_UNPACKED") != "1";
                    gpu.PackSolverBodyState = Environment.GetEnvironmentVariable("ELECTRON2D_MASS_UNPACKED_BODIES") != "1";
                }
                PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.AreaSetGravity(Space, 980);
                PhysicsServer.SpaceSetSolverIterations(Space, 16);
                PhysicsServer.AreaSetLinearDamp(Space, .05f); PhysicsServer.AreaSetAngularDamp(Space, .05f);
                Wall(0, _floor, new(Width / 2, Height + 10)); Wall(1, _side, new(-10, Height / 2)); Wall(2, _side, new(Width + 10, Height / 2));
                for (var i = 0; i < count; i++)
                {
                    var row = i / columns; var position = new Vector2(4 + i % columns * 8 + row % 2 * 4, 4 + row * 7);
                    InitialEnergy += 980d * (Height - position.Y);
                    var body = Bodies[i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodySetMass(body, 1); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetFriction(body, .3f); PhysicsServer.BodySetBounce(body, 0);
                    PhysicsServer.BodySetTransform(body, new(0, position)); PhysicsServer.BodyAddShape(body, _particle.GetRID()); PhysicsServer.BodySetSpace(body, Space);
                }
                Check(PhysicsServer.SpaceGetBackend(Space) == backend, "Requested full backend must actually run");
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
            }
            catch { Dispose(); throw; }
        }
        private void Wall(int i, Shape shape, Vector2 position)
        {
            var body = _walls[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(body, new(0, position)); PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, Space);
        }
        internal void Step()
        {
            if (Complete) return;
            var gpu = _data.GPUStore;
            var upload = gpu?.UploadBytes ?? 0; var download = gpu?.ReadbackBytes ?? 0; var submissions = gpu?.SubmissionCount ?? 0; var wait = gpu?.WaitMS ?? 0;
            var uniforms = gpu?.UniformBytes ?? 0;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var start = Stopwatch.GetTimestamp();
            PhysicsServer.SpaceStep(Space, Delta);
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(PhysicsServer.SpaceGetTick(Space) == (ulong)++_tick, "One complete physical interval per requested tick");
            if (_tick <= Warmup) return;
            var sample = _measured++;
            _steps[sample] = elapsed; _waits[sample] = (gpu?.WaitMS ?? 0) - wait;
            _up[sample] = (gpu?.UploadBytes ?? 0) - upload; _down[sample] = (gpu?.ReadbackBytes ?? 0) - download; _submissions[sample] = (gpu?.SubmissionCount ?? 0) - submissions;
            _owner += owner; _all += all;
            _uniformBytes += (gpu?.UniformBytes ?? 0) - uniforms;
            for (var i = 0; i < _phases.Length; i++) _phases[i] += _data.ProfileMS[i];
            _preparation[0] += _data.GPUPrepareBodiesMS; _preparation[1] += _data.GPUPrepareReportsMS;
            _preparation[2] += _data.GPUPrepareWakesMS; _preparation[3] += _data.GPUPrepareWakeWaitMS;
        }
        internal void Validate()
        {
            Check(Complete && PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects) == Count, "The exact population remains awake and simulated");
            var positions = new Vector2[Count]; var grid = new Dictionary<(int X, int Y), List<int>>(); var index = 0;
            foreach (var body in Bodies)
            {
                var pose = PhysicsServer.BodyGetTransform(body); var velocity = PhysicsServer.BodyGetLinearVelocity(body);
                var angular = PhysicsServer.BodyGetAngularVelocity(body);
                Check(pose.IsFinite() && velocity.IsFinite(), "No nonfinite state in the complete population");
                MaxSpeed = Math.Max(MaxSpeed, velocity.Length());
                MaxEscape = Math.Max(MaxEscape, Math.Max(-pose.Origin.X, Math.Max(pose.Origin.X - Width, pose.Origin.Y - Height)));
                Energy += .5 * velocity.LengthSquared() + .25 * Radius * Radius * angular * angular + 980 * Math.Max(0, Height - pose.Origin.Y);
                var cell = ((int)MathF.Floor(pose.Origin.X / (2 * Radius)), (int)MathF.Floor(pose.Origin.Y / (2 * Radius)));
                for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++)
                        if (grid.TryGetValue((cell.Item1 + x, cell.Item2 + y), out var neighbours))
                            foreach (var other in neighbours)
                            {
                                var penetration = 2 * Radius - pose.Origin.DistanceTo(positions[other]);
                                if (penetration <= 0) continue;
                                OverlappingPairs++; MaxPenetration = Math.Max(MaxPenetration, penetration);
                            }
                if (!grid.TryGetValue(cell, out var bucket)) grid.Add(cell, bucket = []);
                positions[index] = pose.Origin; bucket.Add(index++);
            }
            Check(MaxEscape < Radius && MaxSpeed < 1000, $"No escaped particle or unbounded kinetic activity: escape={MaxEscape}, speed={MaxSpeed}");
            var penetrationLimit = 2 * PhysicsServer.SpaceGetContactMaxAllowedPenetration(Space) + .001f;
            Check(double.IsFinite(Energy) && Energy < InitialEnergy * 1.02 && MaxPenetration <= penetrationLimit,
                $"Energy growth <=2% and penetration <={penetrationLimit}: energy={Energy}/{InitialEnergy}, penetration={MaxPenetration}");
        }
        internal void Report(bool rendered)
        {
            var report = new
            {
                Schema = 1,
                Scene = "dense-circle-pile",
                Backend = Backend.ToString(),
                Rendered = rendered,
                Renderer = rendered ? "gpu" : "none",
                Count,
                TotalBodies = Count + 3,
                Warmup,
                Samples,
                FixedStep = Delta,
                Substeps = 4,
                SolverIterations = 16,
                Sleeping = false,
                Gravity = 980,
                Radius,
                AllowedPenetration = PhysicsServer.SpaceGetContactMaxAllowedPenetration(Space),
                Profiling = true,
                Distribution = new[] { "mean", "p50", "p95", "p99", "max" },
                StepMS = Summary(_steps, Samples),
                WaitMS = Summary(_waits, Samples),
                PhaseMeanMS = _phases.Select(value => value / Samples).ToArray(),
                GPUPreparationNames = new[] { "policies/activity/motion/joints", "report selection", "command/wake publication", "included wake wait" },
                GPUPreparationMeanMS = _preparation.Select(value => value / Samples).ToArray(),
                ForcedParameterRefresh = _data.ForceGPUParameterRefresh,
                DecodedGPUTransforms = _data.DecodeGPUTransforms,
                UniformBytes = _uniformBytes,
                UploadBytes = _up.Sum(),
                ReadbackBytes = _down.Sum(),
                Submissions = _submissions.Sum(),
                OwnerPhysicsBytes = _owner,
                AllPhysicsBytes = _all,
                ActiveBodies = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects),
                CandidatePairs = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs),
                MaxSpeed,
                MaxEscape,
                Energy,
                InitialEnergy,
                MaxPenetration,
                OverlappingPairs,
                Frames,
                WallSeconds,
                FPS = WallSeconds > 0 ? Frames / WallSeconds : 0,
                PhysicsTicksPerSecond = WallSeconds > 0 ? Samples / WallSeconds : 0,
                FrameMS = Summary(FrameTimes, Frames),
                PublishMS = Summary(PublishTimes, PublishCount),
                FrameUniformBytes,
                FrameOwner,
                FrameAll,
                FrameUploadBytes,
                FrameReadbackBytes,
                FrameSubmissions,
                FrameWaitMS,
                VisualBufferBytes = (long)PublishCount * Count * 12 * sizeof(float),
                VSync,
                CapturePath,
                Driver = _data.GPUStore?.Driver,
                Device = _data.GPUStore?.DeviceName,
                ContactColors = _data.GPUStore?.ContactColorCount,
                ColorRounds = _data.GPUStore?.ContactColorRounds,
                ColorFallbacks = _data.GPUStore?.ContactColorFallbacks,
                PackedContacts = _data.GPUStore?.PackColoredContacts,
                PackedSolverBodies = _data.GPUStore is { } store ? (bool?)(store.PackColoredContacts && store.PackSolverBodyState) : null,
                PackedSolverStorageBytes = _data.GPUStore?.PackedSolverStorageBytes
            };
            var text = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            var directory = System.IO.Path.GetFullPath(Environment.GetEnvironmentVariable("ELECTRON2D_MASS_OUTPUT") ?? "bin/physics-mass"); Directory.CreateDirectory(directory);
            File.WriteAllText(System.IO.Path.Combine(directory, $"{Backend}-{Count}-{(rendered ? "window" : "headless")}.json"), text); Console.WriteLine(text);
            Check(_owner == 0 && _all == 0, $"Warmed public physics must allocate zero managed bytes: {_owner}/{_all}");
            if (rendered) Check(FrameOwner == 0 && FrameAll == 0, $"Warmed rendered frames must allocate zero managed bytes: {FrameOwner}/{FrameAll}");
        }
        public void Dispose()
        {
            foreach (var body in Bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            foreach (var body in _walls) if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (Space.IsValid()) PhysicsServer.FreeRID(Space); _particle.Dispose(); _floor.Dispose(); _side.Dispose();
        }
        internal void MarkFrameCounters(bool begin)
        {
            var gpu = _data.GPUStore;
            FrameUploadBytes = (gpu?.UploadBytes ?? 0) - (begin ? 0 : FrameUploadBytes);
            FrameUniformBytes = (gpu?.UniformBytes ?? 0) - (begin ? 0 : FrameUniformBytes);
            FrameReadbackBytes = (gpu?.ReadbackBytes ?? 0) - (begin ? 0 : FrameReadbackBytes);
            FrameSubmissions = (gpu?.SubmissionCount ?? 0) - (begin ? 0 : FrameSubmissions);
            FrameWaitMS = (gpu?.WaitMS ?? 0) - (begin ? 0 : FrameWaitMS);
        }
    }
    private static double[] Summary(double[] values, int count)
    {
        if (count == 0) return [];
        var mean = values.AsSpan(0, count).ToArray().Average(); Array.Sort(values, 0, count);
        return [mean, values[(count - 1) / 2], values[(int)((count - 1) * .95)], values[(int)((count - 1) * .99)], values[count - 1]];
    }
    private static void Render(Pile pile)
    {
        var renderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var fps = Engine.MaxFPS; var ticks = Engine.PhysicsTicksPerSecond; var budget = Engine.MaxPhysicsStepsPerFrame;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            Engine.MaxFPS = 0; Engine.PhysicsTicksPerSecond = 60; Engine.MaxPhysicsStepsPerFrame = 1;
            using var window = new Window { Title = $"Electron2D · {pile.Backend} dense physics · {pile.Count}", Size = new(1152, 800), Unresizable = true };
            using var mesh = new ArrayMesh(); using var instances = new MultiMesh { UseColors = true };
            var vertices = new Vector2[9]; var indices = new int[24];
            for (var i = 0; i < 8; i++) { vertices[i + 1] = Vector2.FromAngle(i * Mathf.Tau / 8); indices[i * 3 + 1] = i + 1; indices[i * 3 + 2] = (i + 1) % 8 + 1; }
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = vertices, Indices = indices }); instances.Mesh = mesh; instances.InstanceCount = pile.Count;
            var pixels = new float[pile.Count * 12]; var scale = Math.Min(1088 / pile.Width, 696 / pile.Height);
            var field = new Entity { Name = "Pile", Position = new(32, 64) }; field.Draw += canvas => canvas.DrawMultiMesh(instances); window.AddChild(field);
            var ticker = new TickNode(pile, field, instances, pixels, scale); window.AddChild(ticker);
            var ended = false; long previous = 0;
            Action frame = () =>
            {
                if (pile.Tick < pile.Warmup) return;
                var now = Stopwatch.GetTimestamp();
                if (pile.FrameStart == 0) { pile.FrameStart = previous = now; pile.FrameOwner = GC.GetAllocatedBytesForCurrentThread(); pile.FrameAll = GC.GetTotalAllocatedBytes(true); pile.MarkFrameCounters(true); return; }
                Check(pile.Frames < pile.FrameTimes.Length, "Rendered frame sample capacity"); pile.FrameTimes[pile.Frames++] = Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds; previous = now;
                if (!pile.Complete) return;
                pile.WallSeconds = Stopwatch.GetElapsedTime(pile.FrameStart, now).TotalSeconds;
                pile.FrameOwner = GC.GetAllocatedBytesForCurrentThread() - pile.FrameOwner; pile.FrameAll = GC.GetTotalAllocatedBytes(true) - pile.FrameAll;
                pile.MarkFrameCounters(false);
                pile.Validate();
                using var image = window.GetTexture().GetImage()!;
                Check(image.Width == 1152 && image.Height == 800, "A real rendered texture exists");
                var colored = 0;
                for (var y = 64; y < 760; y += 4) for (var x = 32; x < 1120; x += 4) if (image.GetPixel(x, y).R > .5f) colored++;
                Check(colored > 100, "Actual particle pixels fill the captured scene");
                pile.CapturePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(Environment.GetEnvironmentVariable("ELECTRON2D_MASS_OUTPUT") ?? "bin/physics-mass"), $"{pile.Backend}-{pile.Count}.png");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(pile.CapturePath)!); image.SavePNG(pile.CapturePath);
                ended = true; window.Tree!.Quit();
            };
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "The requested GPU renderer must actually run");
                DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled); pile.VSync = DisplayServer.WindowGetVSyncMode().ToString();
                RenderingServer.SetDefaultClearColor(new(.145f, .105f, .16f)); RenderingServer.FramePostDraw += frame;
            };
            try { Check(Engine.Run(window) == 0 && ended, "Native mass-world sequence completed"); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= frame; }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); Engine.MaxFPS = fps; Engine.PhysicsTicksPerSecond = ticks; Engine.MaxPhysicsStepsPerFrame = budget; }
    }
    private sealed class TickNode(Pile pile, Entity field, MultiMesh mesh, float[] pixels, float scale) : Node
    {
        protected override void OnReady() => PhysicsProcessEnabled = true;
        protected override void OnPhysicsProcess(double delta)
        {
            Check(Math.Abs(delta - Delta) < 1e-9, "Window retains the requested fixed physical interval");
            pile.Step(); var start = Stopwatch.GetTimestamp();
            PhysicsServer.BodyGetTransform(pile.Space, pile.Bodies, pile.Transforms);
            for (var i = 0; i < pile.Count; i++)
            {
                var position = pile.Transforms[i].Origin; var data = pixels.AsSpan(i * 12, 12);
                data[0] = data[5] = Pile.Radius * scale; data[3] = position.X * scale; data[7] = position.Y * scale;
                data[8] = i % 3 == 0 ? .94f : .91f; data[9] = i % 3 == 0 ? .62f : .48f; data[10] = i % 3 == 0 ? .45f : .68f; data[11] = 1;
            }
            mesh.Buffer = pixels; mesh.CustomAABB = new(0, 0, pile.Width * scale, pile.Height * scale); field.QueueRedraw();
            if (pile.Tick > pile.Warmup && pile.PublishCount < pile.Samples) pile.PublishTimes[pile.PublishCount++] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
    }
}
