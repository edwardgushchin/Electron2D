using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class CPUParticlesTests
{
    internal static void Run()
    {
        DefaultsAndGuards(); Simulation(); ChannelsAndEdges(); Distributions(); Lifecycle(); Geometry(); Storage(); Warm();
        Console.WriteLine("CPU particle simulation/material/storage checks passed.");
    }
    private static CPUParticles Emitter(int amount = 1) => new() { Amount = amount, UseFixedSeed = true, Seed = 123, LocalCoords = true, Gravity = Vector2.Zero, Spread = 0, InitialVelocityMin = 30, InitialVelocityMax = 30, SpeedScale = 0 };
    private static void DefaultsAndGuards()
    {
        using var p = new CPUParticles(); Check(p.Amount == 8 && p.Emitting && p.Lifetime == 1 && !p.LocalCoords && p.FixedFPS == 0 && p.FractDelta && p.Gravity == new Vector2(0, 980) && p.ScaleAmountMin == 1 && p.PhysicsInterpolationMode == PhysicsInterpolationMode.Off, "Defaults.");
        p.AngleMin = 40; Check(p.AngleMax == 40, "Minimum raises maximum."); p.AngleMax = -5; Check(p.AngleMin == -5, "Maximum lowers minimum.");
        Reject<ArgumentOutOfRangeException>(() => p.Amount = 0); Reject<ArgumentOutOfRangeException>(() => p.Amount = 65537); Reject<ArgumentOutOfRangeException>(() => p.Lifetime = double.NaN); Reject<ArgumentOutOfRangeException>(() => p.Randomness = 2); Reject<ArgumentOutOfRangeException>(() => p.RequestParticlesProcess(3601)); Reject<ArgumentOutOfRangeException>(() => p.SetParamMin(CPUParticles.Parameter.Max, 1)); Reject<ArgumentOutOfRangeException>(() => p.SetParticleFlag((CPUParticles.ParticleFlags)1, true));
        p.EmissionPoints = [new(4, 5)]; var points = p.EmissionPoints; points[0] = Vector2.Zero; Check(p.EmissionPoints[0] == new Vector2(4, 5), "Point arrays copy."); Reject<ArgumentException>(() => p.EmissionPoints = [new(float.NaN, 1)]); Check(p.EmissionPoints[0].X == 4, "Invalid copy preserves old state.");
        using var curve = new Curve(); p.SetParamCurve(CPUParticles.Parameter.AngularVelocity, curve); Check(curve.PointCount == 2 && curve.MinValue == -360 && curve.MaxValue == 360 && curve.Sample(.5f) == 1, "Empty channel curves receive unit setup.");
        using var dead = new Curve(); dead.Dispose(); Reject<ObjectDisposedException>(() => p.ScaleCurveX = dead);
        using var root = new Node(); root.AddChild(p); using var tree = new SceneTree(root); Exception? error = null; var worker = new Thread(() => { try { p.Amount = 2; } catch (Exception ex) { error = ex; } }); worker.Start(); worker.Join(); Check(error is InvalidOperationException && p.Amount == 8, "Off-owner mutation rejects.");
    }
    private static void Simulation()
    {
        using var p = Emitter(); p.RequestParticlesProcess(.1f); Near(p.ParticlePose(0).Origin.X, 3, .002f, "Actual velocity motion at explicit seek.");
        var pose = p.ParticlePose(0); p.Restart(true); p.RequestParticlesProcess(.1f); Check(p.ParticlePose(0) == pose, "Restart preserves seeded simulation.");
        p.RequestParticlesProcess(0, .1f); Check(!p.Emitting && p.ParticlePose(0).Origin.X > 5.9f, "Residual stops births and still integrates existing particles.");
        using var staggered = Emitter(4); staggered.RequestParticlesProcess(.13f); Check(staggered.ActiveParticleCount == 1, "Index-phase staggering."); staggered.RequestParticlesProcess(.13f); Check(staggered.ActiveParticleCount == 2, "Second quarter birth.");
        using var burst = Emitter(4); burst.Explosiveness = 1; burst.RequestParticlesProcess(.01f); Check(burst.ActiveParticleCount == 4, "Explosive burst.");
        using var gravity = Emitter(); gravity.Gravity = new(0, 60); gravity.RequestParticlesProcess(.2f); Check(gravity.ParticlePose(0).Origin.Y > .5f, "Gravity affects actual motion.");
        using var curve = new Curve(); curve.AddPoint(new(0, 2)); curve.AddPoint(new(1, 2)); using var scalable = Emitter(); scalable.ScaleAmountCurve = curve; scalable.RequestParticlesProcess(.1f); Near(scalable.ParticlePose(0).X.Length(), 1, .0001f, "Curve insertion clamps to its limits.");
        curve.SetPointValue(0, 2); curve.SetPointValue(1, 2); scalable.RequestParticlesProcess(.1f); Near(scalable.ParticlePose(0).X.Length(), 2, .0001f, "Live scale curve consumes actual samples.");
        using var negative = Emitter(); negative.AnimOffsetMin = negative.AnimOffsetMax = -.5f; negative.RequestParticlesProcess(.01f); Near(negative.ParticleAnimation(0), -.5f, .0001f, "Animation offset uses its own curve/channel.");
        using var fixedRoot = new Node(); var fixedEmitter = Emitter(); fixedEmitter.SpeedScale = 1; fixedEmitter.FixedFPS = 10; fixedRoot.AddChild(fixedEmitter); using var fixedTree = new SceneTree(fixedRoot); fixedTree.ProcessFrame(.05); Check(fixedEmitter.ActiveParticleCount == 0, "Fixed step retains remainder."); fixedTree.ProcessFrame(.05); Check(fixedEmitter.ActiveParticleCount == 1, "Fixed step advances at its boundary.");
        using var rollback = Emitter(); rollback.InitialVelocityMin = rollback.InitialVelocityMax = float.MaxValue; rollback.RequestParticlesProcess(.01f); var before = rollback.ParticlePose(0); rollback.Gravity = new(float.MaxValue, float.MaxValue); Reject<InvalidOperationException>(() => rollback.RequestParticlesProcess(.1f)); Check(rollback.ParticlePose(0) == before, "Overflow rejects a whole particle step before publication.");
    }
    private static void ChannelsAndEdges()
    {
        foreach (var channel in new[] { CPUParticles.Parameter.LinearAccel, CPUParticles.Parameter.RadialAccel, CPUParticles.Parameter.TangentialAccel, CPUParticles.Parameter.OrbitVelocity, CPUParticles.Parameter.Damping, CPUParticles.Parameter.AngularVelocity })
        {
            using var baseline = Emitter(); using var p = Emitter(); baseline.EmissionShape = p.EmissionShape = CPUParticles.EmissionShapeMode.Points; baseline.EmissionPoints = p.EmissionPoints = [new(10, 0)]; baseline.RequestParticlesProcess(.3f);
            p.SetParamMin(channel, channel == CPUParticles.Parameter.OrbitVelocity ? .5f : 20); p.RequestParticlesProcess(.3f); Check(p.ParticlePose(0) != baseline.ParticlePose(0), "Executable scalar channel: " + channel);
        }
        using var fraction = Emitter(4); using var whole = Emitter(4); whole.FractDelta = false; fraction.RequestParticlesProcess(.27f); whole.RequestParticlesProcess(.27f); Check(whole.ParticlePose(1).Origin.X > fraction.ParticlePose(1).Origin.X + .1f, "Fractional birth motion differs from full-frame delta.");
        using var angle = new Curve(); angle.AddPoint(new(0, .25f)); angle.AddPoint(new(1, .25f)); using var offset = new Curve(); offset.AddPoint(new(0, .75f)); offset.AddPoint(new(1, .75f)); using var animation = Emitter(); animation.AngleCurve = angle; animation.AnimOffsetCurve = offset; animation.AnimOffsetMin = animation.AnimOffsetMax = 1; animation.RequestParticlesProcess(.01f); Near(animation.ParticleAnimation(0), .75f, .00001f, "Newborn animation offset samples its own curve.");
        using var ramp = new Gradient(); ramp.Colors = [Colors.Red, Colors.Blue]; using var tint = Emitter(); tint.ColorRamp = ramp; tint.RequestParticlesProcess(.2f); Check(tint.ParticleColor(0).R < 1 && tint.ParticleColor(0).B > 0, "Lifetime gradient changes actual color."); tint.ColorInitialRamp = ramp; tint.Restart(true); tint.RequestParticlesProcess(.01f); Check(tint.ParticleColor(0).R < 1, "Initial ramp samples seeded birth color.");
        tint.ColorInitialRamp = null; tint.ColorRamp = null; tint.Color = Colors.Red; tint.HueVariationMin = tint.HueVariationMax = .25f; using var hue = new Curve(); hue.AddPoint(new(0, 1)); hue.AddPoint(new(1, 1)); tint.HueVariationCurve = hue; tint.Restart(true); tint.RequestParticlesProcess(.01f); Check(tint.ParticleColor(0).G != 0 || tint.ParticleColor(0).B != 0, "Hue matrix affects emitted RGB.");
        using var x = new Curve(); x.AddPoint(new(0, .5f)); x.AddPoint(new(1, .5f)); tint.SplitScale = true; tint.ScaleCurveX = x; tint.RequestParticlesProcess(.01f); Near(tint.ParticlePose(0).X.Length(), .5f, .0001f, "Split X scale."); Near(tint.ParticlePose(0).Y.Length(), 1, .0001f, "Missing Y curve preserves unit scale.");
        using var root = new Node(); var pre = Emitter(); pre.Preprocess = .2; root.AddChild(pre); using var tree = new SceneTree(root); tree.ProcessFrame(0); Check(pre.ParticlePose(0).Origin.X > 5.9f, "Preprocess runs independently of zero speed.");
        using var lifetime = Emitter(32); lifetime.Explosiveness = 1; lifetime.LifetimeRandomness = 1; lifetime.RequestParticlesProcess(.01f); lifetime.RequestParticlesProcess(0, 1.2f); Check(lifetime.ActiveParticleCount == 0, "Zero-to-full randomized lifetimes drain without nonfinite state.");
        using var rng = Emitter(); rng.UseFixedSeed = false; var seed = rng.Seed; rng.Restart(true); Check(rng.Seed == seed, "keepSeed applies independently of UseFixedSeed."); rng.Restart(); Check(rng.Seed != seed, "Ordinary restart chooses a new seed.");
        using var independent = Emitter(2); independent.Explosiveness = 1; independent.RequestParticlesProcess(.1f); var vertices = new List<CanvasVertex>(32); var batches = new List<CanvasBatch>(2); independent.PrepareCanvas(); independent.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[0].InstanceCustom.A == 1 && vertices[0].InstanceCustom.G > 0, "Particle lifetime/phase reach the common shader vertex channel.");
        using var invalid = Emitter(); invalid.EmissionShape = CPUParticles.EmissionShapeMode.Ring; invalid.EmissionRingInnerRadius = 2; Reject<InvalidOperationException>(() => invalid.RequestParticlesProcess(.1f)); Check(invalid.ActiveParticleCount == 0, "Invalid ring bounds fail before simulation.");
        var descriptor = pre.GetPropertyList().Single(d => d.Name == "Amount"); using var packed = new PackedScene(); packed.Pack(root); Check(descriptor.IsStored, "Particle authoring is schema-backed.");
    }
    private static void Distributions()
    {
        foreach (var shape in new[] { CPUParticles.EmissionShapeMode.Point, CPUParticles.EmissionShapeMode.Sphere, CPUParticles.EmissionShapeMode.SphereSurface, CPUParticles.EmissionShapeMode.Rectangle, CPUParticles.EmissionShapeMode.Points, CPUParticles.EmissionShapeMode.DirectedPoints, CPUParticles.EmissionShapeMode.Ring })
        {
            using var p = Emitter(32); p.Explosiveness = 1; p.InitialVelocityMax = p.InitialVelocityMin = 0; p.EmissionShape = shape; p.EmissionSphereRadius = 5; p.EmissionRectExtents = new(3, 4); p.EmissionRingRadius = 5; p.EmissionRingInnerRadius = 2; p.EmissionPoints = [new(2, 3), new(4, 1)]; p.EmissionNormals = [new(0, 1), new(1, 0)]; p.EmissionColors = [Colors.Red, Colors.Blue]; p.RequestParticlesProcess(.01f);
            var positions = new Transform[32]; for (var i = 0; i < 32; i++) { positions[i] = p.ParticlePose(i); var point = positions[i].Origin; Check(shape switch { CPUParticles.EmissionShapeMode.Point => point == Vector2.Zero, CPUParticles.EmissionShapeMode.Sphere or CPUParticles.EmissionShapeMode.SphereSurface => point.Length() <= 5.001f, CPUParticles.EmissionShapeMode.Rectangle => Math.Abs(point.X) <= 3 && Math.Abs(point.Y) <= 4, CPUParticles.EmissionShapeMode.Ring => point.Length() >= 1.999f && point.Length() <= 5.001f, _ => point == new Vector2(2, 3) || point == new Vector2(4, 1) }, "Actual emission distribution: " + shape); }
            p.Restart(true); p.RequestParticlesProcess(.01f); for (var i = 0; i < 32; i++) Check(p.ParticlePose(i) == positions[i], "Every shape uses the fixed seed: " + shape);
        }
        using var directed = Emitter(); directed.EmissionShape = CPUParticles.EmissionShapeMode.DirectedPoints; directed.EmissionPoints = [Vector2.Zero]; directed.EmissionNormals = [new(0, 1)]; directed.ParticleFlagAlignY = true; directed.RequestParticlesProcess(.01f); Check(directed.ParticlePose(0).Origin.Y > 0 && directed.ParticlePose(0).Y.Y > .99f, "Directed point velocity and alignment.");
    }
    private static void Lifecycle()
    {
        using var root = new Entity(); var p = Emitter(); p.LocalCoords = false; p.Position = new(10, 20); root.AddChild(p); using var tree = new SceneTree(root);
        p.RequestParticlesProcess(.1f); var world = p.ParticlePose(0).Origin; root.Position = new(100, 100); Check(p.ParticlePose(0).Origin == world, "Existing world particles do not follow the emitter."); p.Restart(true); p.RequestParticlesProcess(.1f); Check(p.ParticlePose(0).Origin.X > 110, "New world particles use the actual changed transform.");
        using var one = Emitter(4); one.OneShot = true; one.Explosiveness = 1; var finished = 0; one.Finished += () => finished++; one.RequestParticlesProcess(1.2f); Check(!one.Emitting, "One-shot stops after one cycle."); one.RequestParticlesProcess(0, 1.2f); Check(finished == 1 && one.ActiveParticleCount == 0, "Finished reports drained committed state once."); one.RequestParticlesProcess(0, .1f); Check(finished == 1, "No finished replay.");
        using var fails = Emitter(); fails.OneShot = true; fails.Finished += () => throw new ApplicationException("Finished callback"); Reject<ApplicationException>(() => fails.RequestParticlesProcess(0.1f, 2)); Check(!fails.Emitting && fails.ActiveParticleCount == 0, "Callback failure preserves drained state."); fails.RequestParticlesProcess(0, .1f);
        using var both = Emitter(); both.OneShot = true; var delivered = 0; both.PropertyListChanged += _ => throw new ApplicationException("Property callback"); both.Finished += () => delivered++; Reject<ApplicationException>(() => both.RequestParticlesProcess(1.2f, 2)); Check(delivered == 1, "Property-list failure does not lose completion delivery.");
        using var restart = Emitter(); restart.Finished += () => restart.Restart(true); restart.RequestParticlesProcess(.1f, 2); Check(restart.Emitting && restart.ActiveParticleCount == 0, "Finished permits restart without reentrant step.");
        using var moving = new MovingEmitterParent(); var follower = Emitter(); follower.InitialVelocityMax = follower.InitialVelocityMin = 0; follower.LocalCoords = false; moving.AddChild(follower); using var interpolated = new SceneTree(moving) { PhysicsInterpolation = true }; interpolated.PhysicsFrame(.1); interpolated.PhysicsFrame(.1); follower.Restart(true); follower.RequestParticlesProcess(.01f); Near(follower.ParticlePose(0).Origin.X, 10, .0001f, "World births follow the interpolated physics pose even after restart and with particle interpolation off.");
        tree.Paused = true; p.SpeedScale = 1; var before = p.ParticlePose(0); tree.ProcessFrame(.1); Check(p.ParticlePose(0) == before, "Scene pause gates internal simulation."); tree.Paused = false; p.Visible = false; tree.ProcessFrame(.1); Check(p.ParticlePose(0) == before, "Hidden simulation retains state.");
    }
    private sealed class MovingEmitterParent : Entity
    {
        internal MovingEmitterParent() { PhysicsProcessEnabled = true; }
        protected override void OnPhysicsProcess(double delta) { Position += new Vector2(10, 0); }
    }
    private static ImageTexture Sheet()
    {
        using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red); for (var y = 0; y < 4; y++) for (var x = 4; x < 8; x++) image.SetPixel(x, y, Colors.Blue); return ImageTexture.CreateFromImage(image);
    }
    private static void Geometry()
    {
        using var texture = Sheet(); using var material = new CanvasItemMaterial { ParticlesAnimation = true, ParticlesAnimHFrames = 2 };
        using var p = Emitter(2); p.Texture = texture; p.Material = material; p.Explosiveness = 1; p.AnimOffsetMin = p.AnimOffsetMax = .75f; p.RequestParticlesProcess(.01f); p.PrepareCanvas();
        var vertices = new List<CanvasVertex>(64); var batches = new List<CanvasBatch>(4); p.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.Count == 12 && vertices[0].UV.X >= .5f, "Sprite sheet reaches actual quad UVs.");
        material.ParticlesAnimation = false; vertices.Clear(); batches.Clear(); p.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[0].UV.X < .2f, "Live material change updates retained commands without simulation/redraw.");
        material.ParticlesAnimation = true; material.ParticlesAnimLoop = true; p.AnimOffsetMin = p.AnimOffsetMax = 1.25f; p.Restart(true); p.RequestParticlesProcess(.01f); p.PrepareCanvas(); vertices.Clear(); batches.Clear(); p.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices[0].UV.X < .2f, "Animation loop wraps phase.");
        Reject<ArgumentOutOfRangeException>(() => material.ParticlesAnimHFrames = 0); using var copy = (CanvasItemMaterial)material.Duplicate(); Check(copy.ParticlesAnimation && copy.ParticlesAnimHFrames == 2 && copy.ParticlesAnimLoop, "Material duplicate preserves animation.");
    }
    private static void Storage()
    {
        using var curve = new Curve { MinValue = -2, MaxValue = 3 }; curve.AddPoint(new(0, 1), rightTangent: .3f); curve.AddPoint(new(1, 2), leftTangent: -.4f); curve.BakeResolution = 81;
        var state = (PropertyDescriptor<Curve, byte[]>)curve.GetPropertyList().Single(d => d.Name == "_curve_data"); var original = state.GetValue(curve);
        var corrupt = (byte[])original.Clone(); corrupt[0] ^= 0xff; Reject<InvalidDataException>(() => state.SetValue(curve, corrupt));
        corrupt = (byte[])original.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(corrupt.AsSpan(24, 4), 65537); Reject<InvalidDataException>(() => state.SetValue(curve, corrupt));
        corrupt = (byte[])original.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(corrupt.AsSpan(36, 4), BitConverter.SingleToInt32Bits(float.PositiveInfinity)); Reject<InvalidDataException>(() => state.SetValue(curve, corrupt));
        Check(state.GetValue(curve).SequenceEqual(original), "Malformed version/count/free-tangent snapshots preserve the full curve.");
        using var ramp = new Gradient(); ramp.Colors = [Colors.Red, Colors.Blue]; using var material = new CanvasItemMaterial { ParticlesAnimation = true, ParticlesAnimHFrames = 2 };
        using var p = Emitter(3); p.Name = "Particles"; p.ScaleAmountCurve = curve; p.ScaleCurveX = curve; p.ColorRamp = ramp; p.Material = material; p.EmissionShape = CPUParticles.EmissionShapeMode.Ring; p.EmissionRingRadius = 5; p.EmissionRingInnerRadius = 2; p.Explosiveness = 1; p.SetParamCurve(CPUParticles.Parameter.InitialLinearVelocity, curve);
        using var packed = new PackedScene(); packed.Pack(p); using var clone = (CPUParticles)packed.Instantiate(); Check(clone.Amount == 3 && clone.ActiveParticleCount == 0 && ReferenceEquals(clone.ScaleAmountCurve, curve), "In-memory copies keep settings and restart state.");
        var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-particles-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); RunChild(path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(CPUParticlesTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_CPU_PARTICLES_CHILD"] = path; using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Particle child."); } Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh CPU particle scene passed"), error.GetAwaiter().GetResult()); }
        finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var p = (CPUParticles)packed.Instantiate();
        Check(p.Amount == 3 && p.ScaleAmountCurve is { PointCount: 2, BakeResolution: 81, MinValue: -2, MaxValue: 3 } && p.ScaleAmountCurve.GetPointRightTangent(0) == .3f && ReferenceEquals(p.ScaleAmountCurve, p.ScaleCurveX) && ReferenceEquals(p.ScaleAmountCurve, p.GetParamCurve(CPUParticles.Parameter.InitialLinearVelocity)) && p.ColorRamp!.Colors[1] == Colors.Blue && p.Material is CanvasItemMaterial { ParticlesAnimHFrames: 2 }, "Fresh resource graph, scalar points/tangents and material."); p.RequestParticlesProcess(.1f); Check(p.ActiveParticleCount == 3 && p.ParticlePose(0).IsFinite(), "Fresh executable simulation."); Console.WriteLine("Fresh CPU particle scene passed.");
    }
    private static void Warm()
    {
        using var p = Emitter(32); p.Explosiveness = 1; p.DrawOrder = CPUParticles.DrawOrderMode.Lifetime; using var curve = new Curve(); curve.AddPoint(new(0, 1)); curve.AddPoint(new(1, .5f)); p.ScaleAmountCurve = curve;
        var vertices = new List<CanvasVertex>(1024); var batches = new List<CanvasBatch>(4);
        void Cycle() { p.RequestParticlesProcess(.01f); p.PrepareCanvas(); vertices.Clear(); batches.Clear(); p.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var i = 0; i < 64; i++) Cycle(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "128 warmed active simulation/curve/order/geometry intervals allocate zero bytes.");
        p.Emitting = false; p.RequestParticlesProcess(0, 2); p.PrepareCanvas(); for (var i = 0; i < 20; i++) { vertices.Clear(); batches.Clear(); p.AppendCanvas(vertices, batches, Transform.Identity); }
        bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { p.PrepareCanvas(); vertices.Clear(); batches.Clear(); p.AppendCanvas(vertices, batches, Transform.Identity); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "64 warmed idle replays allocate zero bytes.");
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_CPU_PARTICLES_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var texture = Sheet(); using var material = new CanvasItemMaterial { ParticlesAnimation = true, ParticlesAnimHFrames = 2 }; var window = new Window { Size = new(360, 220) };
            var parent = new Entity { Position = new(60, 60) }; window.AddChild(parent); var local = Emitter(8); local.Texture = texture; local.Material = material; local.Explosiveness = 1; local.ScaleAmountMin = local.ScaleAmountMax = 3; local.InitialVelocityMin = local.InitialVelocityMax = 0; local.EmissionShape = CPUParticles.EmissionShapeMode.Rectangle; local.EmissionRectExtents = new(18, 18); parent.AddChild(local);
            var world = Emitter(8); world.Name = "World"; world.Position = new(150, 55); world.LocalCoords = false; world.Texture = texture; world.Material = material; world.AnimOffsetMin = world.AnimOffsetMax = .75f; world.Explosiveness = 1; world.ScaleAmountMin = world.ScaleAmountMax = 3; world.InitialVelocityMin = world.InitialVelocityMax = 0; world.EmissionShape = CPUParticles.EmissionShapeMode.Rectangle; world.EmissionRectExtents = new(18, 18); window.AddChild(world);
            var ring = Emitter(32); ring.Name = "Ring"; ring.Position = new(270, 145); ring.Explosiveness = 1; ring.Texture = null; ring.Color = new(.1f, 1, .4f); ring.EmissionShape = CPUParticles.EmissionShapeMode.Ring; ring.EmissionRingRadius = 35; ring.EmissionRingInnerRadius = 25; ring.ScaleAmountMin = ring.ScaleAmountMax = 4f; ring.InitialVelocityMin = ring.InitialVelocityMax = 0; window.AddChild(ring);
            using var shader = backend == "gpu" ? LoadInstanceShader() : null; using var shaderMaterial = shader is null ? null : new ShaderMaterial { Shader = shader };
            CPUParticles? custom = null;
            if (shaderMaterial is not null) { custom = Emitter(); custom.Name = "Custom"; custom.Position = new(70, 160); custom.Material = shaderMaterial; custom.ScaleAmountMin = custom.ScaleAmountMax = 24; custom.InitialVelocityMin = custom.InitialVelocityMax = 0; custom.AnimOffsetMin = custom.AnimOffsetMax = .75f; window.AddChild(custom); }
            var frame = 0; long before = 0, total = 0;
            window.Ready += _ =>
            {
                local.RequestParticlesProcess(.1f); world.RequestParticlesProcess(.1f); ring.RequestParticlesProcess(.1f); world.Scale = Vector2.Zero; custom?.RequestParticlesProcess(.7f);
                RenderingServer.SetDefaultClearColor(new(.04f, .06f, .09f)); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
                RenderingServer.FramePostDraw += () =>
                {
                    var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before;
                    if (frame == 2)
                    {
                        using var image = RenderingServer.Service!.Readback(); var red = 0; var blue = 0;
                        for (var y = 20; y < 100; y++) for (var x = 20; x < 100; x++) if (image.GetPixel(x, y).R > .8f) red++;
                        for (var y = 20; y < 100; y++) for (var x = 110; x < 190; x++) if (image.GetPixel(x, y).B > .8f) blue++;
                        Check(red > 100 && blue > 100, "Actual local sprite and world coordinates survive zero emitter scale."); if (custom is not null) { var pixel = image.GetPixel(70, 160); Check(pixel.G > .5f && pixel.B > .7f && pixel.R < .1f, "GPU particle custom phase/animation reaches the actual shader."); }
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_CPU_PARTICLES_CAPTURE") is { } path) image.SavePNG(path);
                    }
                    local.RequestParticlesProcess(.003f); ring.RequestParticlesProcess(.003f); world.QueueRedraw(); if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after;
                    if (++frame == 128) window.Tree!.Quit();
                };
            };
            Check(Engine.Run(window) == 0 && frame == 128 && total == 0, "64 native prepared particle render/simulation intervals: " + total); Console.WriteLine("64 native prepared CPU particle intervals: " + total + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static Shader LoadInstanceShader()
    {
        using var input = typeof(CPUParticlesTests).Assembly.GetManifestResourceStream("TestShaders.InstanceCustom.spv")!; using var output = new MemoryStream(); input.CopyTo(output); return Shader.CreateFromSPIRV(output.ToArray());
    }
    private static void Near(float value, float expected, float tolerance, string message) => Check(Math.Abs(value - expected) <= tolerance, message + " actual=" + value);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
