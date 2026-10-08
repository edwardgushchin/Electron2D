using System.Diagnostics;
using SDL = SDL3.SDL;
using Electron2D;
using Electron2D.Examples.PhysicsSandbox;

internal static class PhysicsSandboxTests
{
    internal static void Run()
    {
        PhysicsSolverStorageTests.Run();
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var water = new WaterSimulation(new(1152, 800));
        water.SetUseGPU(true, device);
        Check(water.Count == 65536 && !water.Duck.IsValid() && !water.Boat.IsValid(), "Water precedes both toys.");
        Check(water.Positions.All(p => p.Y < 0), "The water starts above the window.");
        var elapsed = Stopwatch.StartNew();
        for (var frame = 0; frame < 1200; frame++)
        {
            water.Step(1d / 60);
            if (frame % 60 == 59)
                Console.WriteLine($"t={water.Time:F1} min={water.Positions.Min(p => p.Y):F1} max={water.Positions.Max(p => p.Y):F1} above={water.Positions.Count(p => p.Y < 0)} duck={water.DuckPose.Origin} boat={water.BoatPose.Origin} step={water.StepMS:F2}ms elapsed={elapsed.ElapsedMilliseconds}");
            Check(water.Positions.All(p => p.IsFinite() && p.X > -20 && p.X < 1172 && p.Y < 820), "Fluid remains finite and contained.");
            if (frame == 179) Check(!water.Duck.IsValid() && !water.Boat.IsValid(), "No early toy spawn.");
            if (frame == 239) Check(water.Duck.IsValid() && !water.Boat.IsValid(), "Duck falls before the boat.");
        }
        var top = water.Positions.Select(p => p.Y).Order().ElementAt(water.Count / 20);
        Check(top > 450 && top < 620, $"Settled water fills about a third of the window: {top}.");
        Check(water.DuckPose.Origin.Y > 400 && water.DuckPose.Origin.Y < 690, "The duck floats above the floor.");
        Check(water.BoatPose.Origin.Y > 400 && water.BoatPose.Origin.Y < 690, "The boat floats above the floor.");
        var beforeSwitch = water.Positions.ToArray();
        water.SetUseGPU(false);
        Check(water.Positions.SequenceEqual(beforeSwitch), "Changing backend preserves every particle position.");
        var cpu = new double[8];
        for (var i = 0; i < cpu.Length + 2; i++) { water.Step(1d / 60); if (i >= 2) cpu[i - 2] = water.StepMS; }
        water.SetUseGPU(true);
        var gpu = new double[32];
        for (var i = 0; i < gpu.Length + 8; i++) { water.Step(1d / 60); if (i >= 8) gpu[i - 8] = water.StepMS; }
        Console.WriteLine($"Comparable settled 64k steps: CPU mean={cpu.Average():F3}ms, GPU mean={gpu.Average():F3}ms (includes readback and coupling).");
        Check(water.Positions.All(p => p.IsFinite()), "Both algorithms maintain finite positions after switching.");
        var duckBefore = water.DuckPose.Origin;
        water.BeginDrag(duckBefore); water.MovePointer(duckBefore + new Vector2(50, -80));
        for (var i = 0; i < 40; i++) water.Step(1d / 60);
        water.EndDrag();
        Check(water.DuckPose.Origin.DistanceTo(duckBefore) > 20, "Pointer dragging applies a physical impulse to the duck.");
        var mass = water.Volume;
        water.Resize(new(900, 700));
        for (var frame = 0; frame < 180; frame++) water.Step(1d / 60);
        Check(water.Volume == mass && water.Count == 65536 && water.Positions.All(p => p.IsFinite() && p.X > -20 && p.X < 920 && p.Y < 720), "Resize preserves fluid quantity and containment.");
        Console.WriteLine($"Water playground checks passed; {water.Count} particles, {water.Volume:F2} m3.");
    }

    internal static void RunNative()
    {
        var method = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var font = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
        using var window = new WaterWindow(font);
        var directory = System.IO.Path.GetFullPath("bin/water-playground/" + method);
        Directory.CreateDirectory(directory);
        var phase = 0; var interaction = 0; var dragStart = Vector2.Zero; var boatOrigin = Vector2.Zero; var pausedTime = 0d; var pausedDuck = Transform.Identity; var pausedParticle = Vector2.Zero;
        void AfterDraw()
        {
            var time = window.Simulation.Time;
            if (phase == 0 && time > .6 || phase == 1 && time > 2.8 || phase == 2 && time > 5.5 || phase == 3 && time > 11 || phase == 4 && time > 14)
            {
                using var image = RenderingServer.Service!.Readback();
                image.SavePNG(System.IO.Path.Combine(directory, $"{phase:00}.png"));
                Console.WriteLine($"capture {phase}: {window.Size}, t={time:F2}, FPS={Engine.FramesPerSecond:F1}");
                phase++;
                if (phase == 4) window.Size = new(900, 700);

            }
            if (phase < 5) return;
            interaction++;
            if (interaction == 1) { dragStart = window.Simulation.DuckPose.Origin; NativeMotion(dragStart); NativeButton(dragStart, true); }
            if (interaction == 2) NativeMotion(dragStart + new Vector2(-60, -45));
            if (interaction == 20) { NativeButton(dragStart + new Vector2(-60, -45), false); Check(window.Simulation.DuckPose.Origin.DistanceTo(dragStart) > 10, "Native mouse drags the duck."); }
            if (interaction == 21) { boatOrigin = window.Simulation.BoatPose.Origin; dragStart = window.Simulation.BoatPose * new Vector2(0, -70); NativeMotion(dragStart); NativeButton(dragStart, true); }
            if (interaction == 22) NativeMotion(dragStart + new Vector2(-60, -40));
            if (interaction == 40) { NativeButton(dragStart + new Vector2(-60, -40), false); Check(window.Simulation.BoatPose.Origin.DistanceTo(boatOrigin) > 10, "Native mouse drags the boat by its sail."); }
            if (interaction == 41) NativeClick(new(window.Size.X - 140, 39));
            if (interaction == 42) { Check(!window.Simulation.UseGPU, "Native CPU button selects CPU fluid."); NativeClick(new(window.Size.X - 60, 39)); }
            if (interaction == 43) { Check(window.Simulation.UseGPU, "Native GPU button restores GPU fluid."); NativeKey(SDL.Scancode.Space); }
            if (interaction == 44) { Check(window.Paused, "Space pauses the scene."); pausedTime = window.Simulation.Time; pausedDuck = window.Simulation.DuckPose; pausedParticle = window.Simulation.Positions[0]; }
            if (interaction == 45) { Check(window.Simulation.Time == pausedTime && window.Simulation.DuckPose == pausedDuck && window.Simulation.Positions[0] == pausedParticle, "Pause holds liquid and rigid state as well as simulation time."); NativeKey(SDL.Scancode.R); }
            if (interaction == 46) { Check(window.Simulation.Count == 65536 && !window.Simulation.Duck.IsValid() && !window.Simulation.Boat.IsValid(), "Reset rebuilds only the water and preserves the selected backend."); window.Tree!.Quit(); }
        }
        window.Ready += _ => RenderingServer.FramePostDraw += AfterDraw;
        try { Check(Engine.Run(window) == 0, "Native water scene exits cleanly."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; }
        Check(phase == 5 && interaction == 46, "Native lifecycle reaches water, duck, boat and resize captures.");
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
