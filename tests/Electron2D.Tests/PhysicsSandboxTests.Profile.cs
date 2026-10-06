using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Electron2D.Examples.PhysicsSandbox;

internal static partial class PhysicsSandboxTests
{
    internal static void RunProfile()
    {
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        using var font = new FontFile();
        font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        using var bold = new FontFile();
        bold.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf"));
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_LONG") == "1") { RunLongStress(font, bold, configuration); return; }
        var optimized = !(typeof(Engine).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false).OfType<DebuggableAttribute>().FirstOrDefault()?.IsJITOptimizerDisabled ?? false);
        var failures = new List<string>();
        var sceneFilter = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_SCENE");
        var indices = sceneFilter is null ? Enumerable.Range(0, SandboxWindow.SceneNames.Length).ToArray() : sceneFilter.Split(',').Select(int.Parse).ToArray();
        var stressCount = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_STRESS_COUNT") ?? "512");
        var warmup = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_WARMUP") ?? "1600");
        var tag = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_TAG") ?? "current";
        var path = System.IO.Path.GetFullPath($"bin/physics-sandbox/profile-{configuration}-{tag}.json");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var physics = new List<object>();
        foreach (var index in indices)
        {
            var constructionStart = GC.GetAllocatedBytesForCurrentThread();
            using var root = new SubViewport { Size = SandboxWindow.ClientSize };
            var scene = new PhysicsScene(index, font);
            if (index == 8) scene.SetStoryParameter(stressCount);
            root.AddChild(scene);
            using var tree = new SceneTree(root);
            var constructionBytes = GC.GetAllocatedBytesForCurrentThread() - constructionStart;
            for (var i = 0; i < warmup; i++) { Exercise(scene, i); PhysicsTick(tree); }
            var samples = new double[256];
            double backendStep = 0, backendSolve = 0, backendPairs = 0, backendCollide = 0, backendSensors = 0;
            var backendWorld = scene.Colliders.OfType<PhysicsBody>().First().Space!.WorldID;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < samples.Length; i++)
            {
                var start = Stopwatch.GetTimestamp(); Exercise(scene, i + warmup); PhysicsTick(tree);
                samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var bp = Box2D.NET.B2Worlds.b2World_GetProfile(backendWorld);
                backendStep += bp.step; backendSolve += bp.solve; backendPairs += bp.pairs; backendCollide += bp.collide; backendSensors += bp.sensors;
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            if (allocated != 0) failures.Add($"Physics {index}: {allocated} bytes across {samples.Length} ticks.");
            Console.WriteLine($"Physics {index}: {samples.Average():0.000} ms/tick, {allocated} B/{samples.Length} ticks, {scene.BodyCount} bodies.");
            physics.Add(new { scene = SandboxWindow.SceneNames[index], bodies = scene.BodyCount, constructionBytes, meanMS = samples.Average(), p95MS = Percentile(samples), bytesPerTick = (double)allocated / samples.Length, backendStepMS = backendStep / samples.Length, backendSolveMS = backendSolve / samples.Length, backendPairsMS = backendPairs / samples.Length, backendCollideMS = backendCollide / samples.Length, backendSensorsMS = backendSensors / samples.Length });
        }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_HEADLESS") == "1")
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, optimized, warmupPhysics = warmup, physicsSamples = 256, physics }, new JsonSerializerOptions { WriteIndented = true }));
            Check(failures.Count == 0, "Zero managed allocation budget: " + string.Join("; ", failures));
            return;
        }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.MaxFPS = 60;
        var native = new List<object>();
        var nativeWarmup = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_NATIVE_WARMUP") ?? "768");
        const int nativeSamples = 192;
        var trialFrames = nativeWarmup + nativeSamples;
        using var window = new SandboxWindow(font, bold);
        var times = new double[192]; var renders = new double[192]; var bytes = new long[192]; var renderBytes = new long[192];
        long previous = 0, previousBytes = 0, renderStart = 0, renderAllocated = 0;
        var frame = 0;
        Action pre = () => { if (frame % trialFrames != 0) Exercise(window.Scene, frame % trialFrames); renderStart = Stopwatch.GetTimestamp(); renderAllocated = GC.GetAllocatedBytesForCurrentThread(); };
        Action post = () =>
        {
            var now = Stopwatch.GetTimestamp(); var allocated = GC.GetAllocatedBytesForCurrentThread();
            var renderMS = Stopwatch.GetElapsedTime(renderStart, now).TotalMilliseconds;
            var renderingBytes = allocated - renderAllocated;
            var trial = frame / trialFrames; var phase = frame % trialFrames;
            if (phase == 0)
            {
                if (trial == indices.Length * 2) { window.Tree!.Quit(); return; }
                window.SwitchScene(indices[trial / 2]);
                if (window.Scene.Index == 8) window.Scene.SetStoryParameter(stressCount); window.Scene.DebugEnabled = trial % 2 != 0;
                Exercise(window.Scene, 0);
                NativeMotion(new(-20, -20));
            }
            if (phase >= nativeWarmup)
            {
                var i = phase - nativeWarmup;
                times[i] = Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds;
                renders[i] = renderMS; bytes[i] = allocated - previousBytes; renderBytes[i] = renderingBytes;
            }
            if (phase == trialFrames - 1)
            {
                var mean = times.Average();
                if (bytes.Max() != 0) failures.Add($"Native {trial}: max {bytes.Max()} bytes/frame, render max {renderBytes.Max()}.");
                Console.WriteLine($"Profile {trial}: {1000 / mean:0.0} FPS, {bytes.Average():0.0} B/frame, max {bytes.Max()}, render {renderBytes.Average():0.0}");
                native.Add(new { scene = SandboxWindow.SceneNames[indices[trial / 2]], debug = trial % 2 != 0, bodies = window.Scene.BodyCount, fps = 1000 / mean, frameMS = mean, frameP95MS = Percentile(times), renderMS = renders.Average(), renderP95MS = Percentile(renders), bytesPerFrame = bytes.Average(), renderBytesPerFrame = renderBytes.Average(), maxBytesPerFrame = bytes.Max(), maxRenderBytesPerFrame = renderBytes.Max() });
            }
            frame++; previous = Stopwatch.GetTimestamp(); previousBytes = GC.GetAllocatedBytesForCurrentThread();
        };
        window.Ready += _ => { RenderingServer.FramePreDraw += pre; RenderingServer.FramePostDraw += post; };
        try { Check(Engine.Run(window) == 0 && frame >= indices.Length * 2 * trialFrames, "Complete native performance profile."); }
        finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= pre; RenderingServer.FramePostDraw -= post; } }
        File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, optimized, platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, backend = "gpu", maxFPS = 60, warmupPhysics = warmup, physicsSamples = 256, warmupNative = nativeWarmup, nativeSamples, thread = "scene/render owner", physics, native }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PhysicsSandbox performance profile: " + path);
        Check(failures.Count == 0, "Zero managed allocation budget: " + string.Join("; ", failures));
    }

    private static void RunLongStress(Font font, Font bold, string configuration)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.MaxFPS = 60;
        using var window = new SandboxWindow(font, bold);
        var rows = new List<object>();
        var cadence = new List<double>();
        var watch = new Stopwatch(); var interval = new Stopwatch();
        var frames = 0; double render = 0; long stamp = 0, step = 0;
        Action pre = () => stamp = Stopwatch.GetTimestamp();
        Action post = () =>
        {
            render += Stopwatch.GetElapsedTime(stamp).TotalMilliseconds; frames++;
            if (interval.Elapsed.TotalSeconds < 15) return;
            var seconds = interval.Elapsed.TotalSeconds; var elapsed = watch.Elapsed.TotalSeconds;
            var fps = frames / seconds; var renderMS = render / frames;
            var physicsHz = (window.Scene.PhysicsSteps - step) / seconds;
            Console.WriteLine($"Long stress {elapsed:0}s: {fps:0.0} FPS, render {renderMS:0.00} ms, physics {physicsHz:0.0} Hz, impacts {window.Scene.ContactEvents}.");
            rows.Add(new { seconds = elapsed, fps, renderMS, physicsHz, impacts = window.Scene.ContactEvents, managedHeap = GC.GetTotalMemory(false) });
            cadence.Add(fps);
            frames = 0; render = 0; step = window.Scene.PhysicsSteps; interval.Restart();
            if (elapsed >= 180) window.Tree!.Quit();
        };
        window.Ready += _ =>
        {
            window.SwitchScene(8); window.Scene.SetStoryParameter(1024); window.Scene.DebugEnabled = true;
            NativeMotion(new(-20, -20)); watch.Start(); interval.Start();
            RenderingServer.FramePreDraw += pre; RenderingServer.FramePostDraw += post;
        };
        try { Check(Engine.Run(window) == 0 && rows.Count >= 12, "Three-minute native settling stress."); }
        finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= pre; RenderingServer.FramePostDraw -= post; } }
        var path = System.IO.Path.GetFullPath($"bin/physics-sandbox/long-stress-{configuration}.json");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, bodies = 1024, debug = true, durationSeconds = 180, rows }, new JsonSerializerOptions { WriteIndented = true }));
        var settled = cadence.Skip(3).Take(3).Average();
        var final = cadence.TakeLast(3).Average();
        Check(final >= settled * .8, $"Settled stress cadence does not decay: {settled:0.0} to {final:0.0} FPS.");
    }

    private static void Exercise(PhysicsScene scene, int frame)
    {
        scene.SetPointer(scene.GetGlobalTransformWithCanvas() * new Vector2(570 + 270 * MathF.Sin(frame * .08f), 380 + 160 * MathF.Cos(frame * .06f)));
        if (frame == 0 && scene.Index is 9 or 10) scene.Act(scene.Index == 9 ? 1 : 0);
        if (frame % 48 == 0 && scene.Bodies.Count > 0)
        {
            var body = scene.Bodies[0];
            body.ApplyCentralImpulse(new Vector2(frame % 96 == 0 ? 80 : -80, -40));
        }
    }

    private static double Percentile(double[] samples)
    {
        var sorted = (double[])samples.Clone(); Array.Sort(sorted); return sorted[(int)(sorted.Length * .95)];
    }
}
