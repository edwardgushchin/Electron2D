using Electron2D;
using Electron2D.Examples.WaterPlayground;
using SDL = SDL3.SDL;

internal static partial class WaterPlaygroundTests
{
    private static void RunNativeToys()
    {
        WaterWindow.ConfigurePresentation();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var font = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
        using var window = new WaterWindow(font) { Unfocusable = true };
        var directory = System.IO.Path.GetFullPath("bin/water-playground/toys"); Directory.CreateDirectory(directory);
        var stage = 0; var start = 0d; var resizeFrames = 0; var toggleIndex = 0;
        string[] names = ["Bucket", "Wheel", "Gate", "Wood", "Steel", "Ball"];
        WaterToy[] kinds = [WaterToy.Bucket, WaterToy.Wheel, WaterToy.Gate, WaterToy.Wood, WaterToy.Steel, WaterToy.Ball];
        void Click(string name) { var button = window.GetNode<Button>(name); NativeClick(button.Position + button.Size / 2); }
        void Capture(string name)
        { using var image = RenderingServer.Service!.Readback(); Check(image.Size == window.Size, "Capture dimensions match the resized native window."); image.SavePNG(System.IO.Path.Combine(directory, name + ".png")); Console.WriteLine($"capture {name}: t={window.Simulation.Time:F1} FPS={Engine.FramesPerSecond:F1} GPU={window.Simulation.StepMS:F1}ms"); }
        void Frame()
        {
            var water = window.Simulation;
            if (water.Time < 20 && stage < 16) return;
            if (stage < names.Length) { Click(names[stage++]); return; }
            else if (stage == 6) { start = water.Time; stage++; return; }
            else if (stage == 7 && water.Time - start > 4)
            {
                Check(water.KindCount(WaterToy.Bucket) == 1 && water.KindCount(WaterToy.Wheel) == 1 && water.KindCount(WaterToy.Platform) == 1 && water.KindCount(WaterToy.Gate) == 1 && water.KindCount(WaterToy.Ball) == 1 && water.KindCount(WaterToy.Wood) == 1 && water.KindCount(WaterToy.Steel) == 1, "All native toolbar buttons create their physical toys.");
                Check(names.All(name => window.GetNode<Button>(name).ButtonPressed && !window.GetNode<Button>(name).Disabled), "Toy buttons remain visibly pressed and available for switching off.");
                Capture("all-toys");
                var bucket = window.ViewTransform * water.ActorPose(WaterSimulation.BucketSlot).Origin;
                NativeMotion(bucket); NativeButton(bucket, true); stage++; return;
            }
            else if (stage == 8) { Check(water.Dragged == water.ActorBody(WaterSimulation.BucketSlot), "Native grab picks the bucket."); NativeMotion(window.ViewTransform * new Vector2(270, 360)); start = water.Time; stage++; }
            else if (stage == 9 && water.Time - start > 2)
            { Capture("bucket-lift"); NativeKey(SDL.Scancode.E); NativeKey(SDL.Scancode.E); NativeKey(SDL.Scancode.E); NativeKey(SDL.Scancode.E); start = water.Time; stage++; }
            else if (stage == 10 && water.Time - start > 2)
            { Capture("bucket-pour"); NativeButton(window.ViewTransform * new Vector2(270, 360), false); Click("Drain closed"); stage++; }
            else if (stage == 11) { Check(water.DrainOpen, "Native drain button opens recirculation."); Click("Flow 100%"); stage++; }
            else if (stage == 12) { Check(water.FaucetFlow == .75f, "Native flow button controls the nozzle."); NativeKey(SDL.Scancode.Space); stage++; }
            else if (stage == 13) { Check(window.Paused && water.FaucetFlow == .75f, "Space pauses without activating the focused valve button."); window.Size = new(480, 800); stage++; }
            else if (stage == 14 && ++resizeFrames >= 12) { Check(window.Size == new Vector2i(480, 800), "Portrait resize completed."); Capture("portrait-tools"); window.Size = new(1600, 900); resizeFrames = 0; stage++; }
            else if (stage == 15 && ++resizeFrames >= 12) { Check(window.Size == new Vector2i(1600, 900), "Wide resize completed."); Capture("wide-tools"); stage++; }
            else if (stage is 16 or 17)
            {
                var enabled = stage == 17;
                if (toggleIndex > 0)
                {
                    var i = toggleIndex - 1;
                    Check(window.GetNode<Button>(names[i]).ButtonPressed == enabled && water.KindCount(kinds[i]) == (enabled ? 1 : 0), "Native toggles create and remove toys while paused.");
                }
                if (toggleIndex < names.Length) { Click(names[toggleIndex++]); return; }
                Check(water.ActorExists(WaterSimulation.PlatformSlot) == enabled && water.WheelTurns == 0, "The lift follows the wheel toggle without retaining its old winding.");
                Capture(enabled ? "toys-on-again" : "toys-off"); toggleIndex = 0; stage++;
            }
            else if (stage == 18) { NativeKey(SDL.Scancode.R); stage++; }
            else if (stage == 19)
            {
                Check(names.All(name => !window.GetNode<Button>(name).ButtonPressed) && kinds.All(kind => water.KindCount(kind) == 0), "Reset clears both toys and their toggle states.");
                window.Tree!.Quit(); stage++;
            }
        }
        window.Ready += _ => RenderingServer.FramePostDraw += Frame;
        try { Check(Engine.Run(window) == 0 && stage == 20, "Native toy scene completes interaction and capture checks."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Frame; }
    }
}
