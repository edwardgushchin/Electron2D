using System.Diagnostics;
using SDL = SDL3.SDL;
using Electron2D;
using Electron2D.Examples.WaterPlayground;

internal static partial class WaterPlaygroundTests
{
    internal static void Run()
    {
        PhysicsSolverStorageTests.Run();
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WATER_TOYS") == "1") { CheckToys(device, false); CheckToys(device, true); return; }
        CheckOpenTop(device, false);
        CheckOpenTop(device, true);
        using var water = new WaterSimulation();
        water.SetUseGPU(true, device);
        Check(water.Count == 65536 && !water.Duck.IsValid() && !water.Boat.IsValid(), "Water precedes both toys.");
        Check(water.ActiveCount == 0, "Water is released progressively instead of starting as a block.");
        var elapsed = Stopwatch.StartNew();
        var appeared = new bool[2];
        var highestSplash = 0f;
        var frames = 60 * int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_WATER_SECONDS") ?? "20");
        for (var frame = 0; frame < frames; frame++)
        {
            water.Step(1d / 60);
            if (frame % 60 == 59)
                Console.WriteLine($"t={water.Time:F1} min={water.Positions.Min(p => p.Y):F1} max={water.Positions.Max(p => p.Y):F1} above={water.Positions.Count(p => p.Y < 0)} duck={water.DuckPose.Origin} boat={water.BoatPose.Origin} step={water.StepMS:F2}ms elapsed={elapsed.ElapsedMilliseconds}");
            CheckContained(water);
            for (var i = 0; i < water.ActiveCount; i++) highestSplash = Math.Min(highestSplash, water.Positions[i].Y);
            if (frame == 0) Check(water.ActiveCount > 0 && water.Positions.Take(water.ActiveCount).All(p => p.Y + 8 < water.EntryY), "The first water particles and their reconstructed edge start offscreen.");
            for (var slot = 0; slot < appeared.Length; slot++)
                if (water.ActorExists(slot) && !appeared[slot])
                {
                    Check(water.ActorBounds(slot).End.Y < water.EntryY, "Each toy starts entirely above its entry edge.");
                    appeared[slot] = true;
                }
            if (frame == 179) Check(!water.Duck.IsValid() && !water.Boat.IsValid(), "No early toy spawn.");
            if (frame == 599) Check(water.Duck.IsValid() && !water.Boat.IsValid(), "Duck falls before the boat.");
        }
        Check(highestSplash > -1200, $"Inflow and toy impacts avoid explosive outliers: highest splash={highestSplash}.");
        var top = water.Positions.Select(p => p.Y).Order().ElementAt(water.Count / 20);
        Check(top > 450 && top < 620, $"Settled water fills about a third of the window: {top}.");
        Check(water.DuckPose.Origin.Y > 400 && water.DuckPose.Origin.Y < 690, "The duck floats above the floor.");
        Check(water.BoatPose.Origin.Y > 400 && water.BoatPose.Origin.Y < 690, "The boat floats above the floor.");
        var beforeSwitch = water.Positions.ToArray();
        water.SetUseGPU(false);
        Check(water.Positions.SequenceEqual(beforeSwitch), "Changing backend preserves every particle position.");
        var cpu = new double[8];
        for (var i = 0; i < 2; i++) water.Step(1d / 60);
        var allocated = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < cpu.Length; i++) { water.Step(1d / 60); CheckContained(water); cpu[i] = water.StepMS; }
        var cpuBytes = (GC.GetTotalAllocatedBytes(true) - allocated) / cpu.Length;
        water.SetUseGPU(true);
        var warmup = Stopwatch.StartNew();
        while (warmup.Elapsed.TotalSeconds < 1) water.Step(1d / 60);
        var gpu = new double[128]; allocated = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < gpu.Length; i++) { water.Step(1d / 60); gpu[i] = water.StepMS; }
        var gpuBytes = (GC.GetTotalAllocatedBytes(true) - allocated) / gpu.Length;
        Check(cpuBytes == 0 && gpuBytes == 0, "Warmed CPU and GPU fluid steps allocate no managed memory across worker threads.");
        Array.Sort(gpu);
        Console.WriteLine($"Comparable settled 64k steps: CPU mean={cpu.Average():F3}ms {cpuBytes} B/step, GPU mean={gpu.Average():F3}ms median={gpu[gpu.Length / 2]:F3}ms p95={gpu[(int)(gpu.Length * .95)]:F3}ms {gpuBytes} B/step (full step including waits/readback, managed allocations across threads).");
        Check(water.Positions.All(p => p.IsFinite()), "Both algorithms maintain finite positions after switching.");
        var duckBefore = water.DuckPose.Origin;
        water.BeginDrag(duckBefore); water.MovePointer(duckBefore + new Vector2(50, -80));
        for (var i = 0; i < 40; i++) water.Step(1d / 60);
        water.EndDrag();
        Check(water.DuckPose.Origin.DistanceTo(duckBefore) > 20, "Pointer dragging applies a physical impulse to the duck.");
        Check(water.DuckPose.Origin.Y < duckBefore.Y - 45, "The grab lifts against gravity rather than barely balancing its weight.");
        var surface = new WaterSurface(); surface.Update(water);
        Check(surface.VertexCount > 0 && surface.VertexCount < 160000, "Density reconstruction uses compact continuous geometry.");
        var wet = 0;
        for (var x = 32; x < 1120; x += 16) if (surface.Sample(new(x, 770)) > .65f) wet++;
        Check(wet > 60, "The basin has continuous water without a particle-dot pattern.");
        var mass = water.Volume;
        Check(water.Size == WaterSimulation.WorldSize && water.ActiveCount == 65536 && mass > 3, "The physical world and total mass are fixed.");
        Console.WriteLine($"Water playground checks passed; {water.Count} particles, {water.Volume:F2} m3.");
    }

    private static void CheckOpenTop(RenderingDevice device, bool gpu)
    {
        using var water = new WaterSimulation(256) { EntryY = -1600 };
        water.SetUseGPU(gpu, device);
        for (var i = 0; i < 3; i++) water.Step(1d / 60);
        Check(water.ActiveCount > 0 && water.Positions[0].Y < water.EntryY, "The neighbor grid supports offscreen births above its initial extent in both backends.");
        water.SetUseGPU(false);
        water.LaunchFirstParticleForTest();
        water.SetUseGPU(gpu);
        for (var i = 0; i < 12; i++) { water.Step(1d / 60); CheckContained(water); }
        Check(water.Positions[0].Y < -20, "An upward-moving droplet crosses the former ceiling in both backends.");
        for (var i = 0; i < 60; i++) { water.Step(1d / 60); CheckContained(water); }
        Check(water.Positions[0].Y > 100 && water.ActiveCount == 1, $"Escaped water falls back without deletion or respawn: y={water.Positions[0].Y}, count={water.ActiveCount}, t={water.Time}.");
    }

    internal static void RunNative()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WATER_TOYS") == "1") { RunNativeToys(); return; }
        WaterWindow.ConfigurePresentation();
        var method = Environment.GetEnvironmentVariable("ELECTRON2D_WATER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 144;
        using var font = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
        using var window = new WaterWindow(font) { Unfocusable = true };
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_PORTRAIT") == "1") window.Size = new(480, 800);
        var appeared = new bool[2];
        var directory = System.IO.Path.GetFullPath("bin/water-playground/" + method);
        Directory.CreateDirectory(directory);
        var video = Environment.GetEnvironmentVariable("ELECTRON2D_WATER_VIDEO") == "1"; var videoFrame = 0; var nextVideoTime = 0d;
        if (video) Directory.CreateDirectory(System.IO.Path.Combine(directory, "sequence"));
        var frameTimes = new double[4096]; var frameCount = 0; var lastFrame = 0L;
        var phase = 0; var interaction = 0; var dragStart = Vector2.Zero; var boatOrigin = Vector2.Zero; var pausedTime = 0d; var pausedDuck = Transform.Identity; var pausedParticle = Vector2.Zero; var snapshot = Array.Empty<Vector2>();
        void AfterDraw()
        {
            var time = window.Simulation.Time;
            var timestamp = Stopwatch.GetTimestamp();
            if (!video && time is > 14 and < 20 && lastFrame != 0 && frameCount < frameTimes.Length)
                frameTimes[frameCount++] = Stopwatch.GetElapsedTime(lastFrame, timestamp).TotalMilliseconds;
            lastFrame = timestamp;
            if (video && time >= nextVideoTime && time < 20)
            {
                using var frame = RenderingServer.Service!.Readback();
                frame.SavePNG(System.IO.Path.Combine(directory, "sequence", $"{videoFrame++:00000}.png")); nextVideoTime += 1d / 30;
            }
            if (time > 0 && time < .02)
                Check(window.Simulation.Positions.Take(window.Simulation.ActiveCount).All(p => (window.ViewTransform * (p + new Vector2(0, 8))).Y < 0), "The initial water batch is hidden above the visible top, including a portrait view.");
            if (interaction < 105)
                for (var slot = 0; slot < appeared.Length; slot++)
                    if (window.Simulation.ActorExists(slot) && !appeared[slot])
                    {
                        Check((window.ViewTransform * window.Simulation.ActorBounds(slot)).End.Y < 0, "A new toy is fully outside the rendered top edge, including portrait views.");
                        appeared[slot] = true;
                    }
            if (phase == 0 && time > .6 || phase == 1 && time > 4 || phase == 2 && time > 9.8 || phase == 3 && time > 13.5 || phase == 4 && time > 20)
            {
                using var image = RenderingServer.Service!.Readback();
                image.SavePNG(System.IO.Path.Combine(directory, $"{phase:00}.png"));
                Console.WriteLine($"capture {phase}: {window.Size}, t={time:F2}, FPS={Engine.FramesPerSecond:F1}");
                phase++;
                if (phase == 4 && !video) window.Size = new(1920, 1080);

            }
            if (phase == 1 && time > .4 && time < .7 && window.Simulation.EntryY < -100)
            {
                var visibleWaterAboveOrigin = false;
                foreach (var vertex in window.Surface.Vertices) if (vertex.Y < -50) visibleWaterAboveOrigin = true;
                Check(visibleWaterAboveOrigin, "The falling water surface is drawn in the visible air above the original world origin.");
            }
            if (phase < 5) return;
            if (interaction == 0) CheckView();
            interaction++;
            if (interaction == 1) { dragStart = window.ViewTransform * window.Simulation.DuckPose.Origin; NativeMotion(dragStart); NativeButton(dragStart, true); }
            if (interaction == 2) NativeMotion(dragStart + new Vector2(-60, -45));
            if (interaction == 20) { NativeButton(dragStart + new Vector2(-60, -45), false); Check((window.ViewTransform * window.Simulation.DuckPose.Origin).DistanceTo(dragStart) > 10, "Native mouse drags the duck."); }
            if (interaction == 21) { boatOrigin = window.Simulation.BoatPose.Origin; dragStart = window.ViewTransform * (window.Simulation.BoatPose * new Vector2(0, -70)); NativeMotion(dragStart); NativeButton(dragStart, true); }
            if (interaction == 22) NativeMotion(dragStart + new Vector2(-60, -40));
            if (interaction == 40) { NativeButton(dragStart + new Vector2(-60, -40), false); Check(window.Simulation.BoatPose.Origin.DistanceTo(boatOrigin) > 10, "Native mouse drags the boat by its sail."); }
            if (interaction == 41) NativeKey(SDL.Scancode.H);
            if (interaction == 42)
            {
                Check(!window.InterfaceVisible && !window.GetNode<Button>("CPU").Visible && !window.GetNode<Button>("Bucket").Visible && !window.GetNode<Entity>("HUD").Visible, "H hides the complete interface.");
                Capture("interface-hidden.png"); NativeClick(new(window.Size.X - 140, 39));
            }
            if (interaction == 43) { Check(window.Simulation.UseGPU, "Hidden mode controls do not intercept pointer input."); NativeKey(SDL.Scancode.H); }
            if (interaction == 44) Check(window.InterfaceVisible && window.GetNode<Button>("CPU").Visible && window.GetNode<Button>("Bucket").Visible, "H restores interface controls.");
            if (interaction == 61) NativeClick(new(window.Size.X - 140, 39));
            if (interaction == 62) { Check(!window.Simulation.UseGPU, "Native CPU button selects CPU fluid."); NativeClick(new(window.Size.X - 60, 39)); }
            if (interaction == 63) { Check(window.Simulation.UseGPU, "Native GPU button restores GPU fluid."); NativeKey(SDL.Scancode.Space); }
            if (interaction == 64) { Check(window.Paused, "Space pauses the scene."); Check(window.GetNode<Button>("GPU").ButtonPressed && !window.GetNode<Button>("CPU").ButtonPressed, "A mode remains selected after keyboard pause."); pausedTime = window.Simulation.Time; pausedDuck = window.Simulation.DuckPose; pausedParticle = window.Simulation.Positions[0]; snapshot = window.Simulation.Positions.ToArray(); }
            if (interaction == 65) { Check(window.Simulation.Time == pausedTime && window.Simulation.DuckPose == pausedDuck && window.Simulation.Positions[0] == pausedParticle, "Pause holds liquid and rigid state as well as simulation time."); window.Size = new(480, 800); }
            if (interaction == 70) { CheckView(); Capture("portrait.png"); window.Size = new(900, 700); }
            if (interaction == 75) { CheckView(); Capture("compact.png"); NativeKey(SDL.Scancode.F11); }
            if (interaction == 90)
            {
                Check(window.Mode == WindowMode.Fullscreen && window.Borderless, "F11 enters borderless fullscreen.");
                CheckView(); Capture("fullscreen.png"); NativeKey(SDL.Scancode.F11);
            }
            if (interaction == 105)
            {
                Check(window.Mode == WindowMode.Windowed && !window.Borderless && window.Size == new Vector2i(900, 700), "F11 restores the window.");
                CheckView(); NativeKey(SDL.Scancode.R);
            }
            if (interaction == 106)
            {
                Check(window.Simulation.ActiveCount < 1000 && !window.Simulation.Duck.IsValid() && !window.Simulation.Boat.IsValid(), "Reset begins a new pour and removes toys.");
                window.Tree!.Quit();
            }
        }
        window.Ready += _ =>
        {
            Console.WriteLine($"Presentation: cap={Engine.MaxFPS}, vsync={DisplayServer.WindowGetVSyncMode()}");
            Check(Engine.MaxFPS == 144 && DisplayServer.WindowGetVSyncMode() == DisplayServer.VSyncMode.Disabled, "144 FPS cap and native unsynchronized presentation are selected.");
            foreach (var mode in Enum.GetValues<DisplayServer.VSyncMode>())
            {
                DisplayServer.WindowSetVSyncMode(mode);
                Check(DisplayServer.WindowGetVSyncMode() == mode || DisplayServer.WindowGetVSyncMode() == DisplayServer.VSyncMode.Enabled, "Unsupported presentation modes report their enabled fallback.");
            }
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
            var previous = DisplayServer.WindowGetVSyncMode();
            try { DisplayServer.WindowSetVSyncMode((DisplayServer.VSyncMode)99); throw new Exception("Undefined presentation mode accepted."); } catch (ArgumentOutOfRangeException) { }
            Check(DisplayServer.WindowGetVSyncMode() == previous, "Invalid policy preserves the current native state.");
            RenderingServer.FramePostDraw += AfterDraw;
        };
        try { Check(Engine.Run(window) == 0, "Native water scene exits cleanly."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; }
        if (frameCount > 0)
        {
            var samples = frameTimes.AsSpan(0, frameCount); samples.Sort();
            Console.WriteLine($"Rendered 64k frames: count={frameCount}, median={samples[frameCount / 2]:F2}ms p95={samples[(int)(frameCount * .95)]:F2}ms, mean FPS={1000 / samples.ToArray().Average():F1}");
        }
        Check(phase == 5 && interaction == 106, "Native lifecycle reaches water, dragging and resize captures.");
        void Capture(string name)
        {
            using var image = RenderingServer.Service!.Readback();
            image.SavePNG(System.IO.Path.Combine(directory, name));
        }
        void CheckView()
        {
            Check((window.ViewTransform * new Vector2(0, window.Simulation.Size.Y)).IsEqualApprox(new(0, window.Size.Y)) &&
                (window.ViewTransform * window.Simulation.Size).IsEqualApprox((Vector2)window.Size), "Both bottom corners stay at the viewport edges without side gutters.");
            Check(Mathf.IsEqualApprox(window.ViewTransform.X.Length(), window.ViewTransform.Y.Length()) &&
                Mathf.IsZeroApprox(window.ViewTransform.X.Dot(window.ViewTransform.Y)), "Resize and fullscreen preserve shape proportions with one orthogonal scale.");
            Check(window.ToWorld((Vector2)window.Size).IsEqualApprox(window.Simulation.Size), "Pointer mapping follows the bottom-anchored uniform view.");
            CheckContained(window.Simulation);
            if (window.Paused) Check(snapshot.SequenceEqual(window.Simulation.Positions) && window.Simulation.DuckPose == pausedDuck, "Resize and fullscreen preserve every world coordinate.");
        }
    }

    private static void CheckContained(WaterSimulation water)
    {
        for (var i = 0; i < water.ActiveCount; i++)
        {
            var p = water.Positions[i];
            Check(p.IsFinite() && p.X >= 0 && p.X <= water.Size.X && p.Y <= water.Size.Y, "Every active particle stays inside the side and bottom walls; the top is open.");
        }
        for (var slot = 0; slot < 2; slot++)
            if (water.ActorExists(slot))
            {
                var b = water.ActorBounds(slot);
                Check(b.Position.X >= -0.01f && b.End.X <= water.Size.X + .01f && b.End.Y <= water.Size.Y + .01f, "Complete rotated sprites stay inside the side and bottom walls; the top is open.");
            }
    }

    private static uint NativeWindow() => SDL.GetWindowID(SDL.GetWindows(out _)![0]);
    private static void NativeMotion(Vector2 point)
    { var e = new SDL.Event { Motion = new() { Type = SDL.EventType.MouseMotion, WindowID = NativeWindow(), X = point.X, Y = point.Y } }; Check(SDL.PushEvent(ref e), "Mouse motion"); }
    private static void NativeButton(Vector2 point, bool down)
    { var e = new SDL.Event { Button = new() { Type = down ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp, WindowID = NativeWindow(), Button = 1, Down = down, X = point.X, Y = point.Y } }; Check(SDL.PushEvent(ref e), "Mouse button"); }
    private static void NativeClick(Vector2 point) { NativeMotion(point); NativeButton(point, true); NativeButton(point, false); }
    private static void NativeKey(SDL.Scancode code)
    { var e = new SDL.Event { Key = new() { Type = SDL.EventType.KeyDown, WindowID = NativeWindow(), Down = true, Scancode = code, Key = SDL.GetKeyFromScancode(code, SDL.Keymod.None, true) } }; Check(SDL.PushEvent(ref e), "Key down"); e.Key.Type = SDL.EventType.KeyUp; e.Key.Down = false; Check(SDL.PushEvent(ref e), "Key up"); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}

namespace Electron2D.Examples.WaterPlayground
{
    internal sealed partial class WaterSimulation
    {
        // A deterministic physical initial condition, compiled only into the executable checks.
        internal void LaunchFirstParticleForTest()
        {
            ActiveCount = 1; FaucetFlow = 0;
            _state[0] = new(Size.X * .005f, .2f, 0, -4);
            Capture();
        }
    }
}
