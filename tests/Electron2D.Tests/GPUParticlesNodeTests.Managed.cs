using Electron2D;
using Path = System.IO.Path;

internal static partial class GPUParticlesNodeTests
{
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        using var node = new GPUParticles();
        Check(node.Amount == 8 && node.Emitting && node.Lifetime == 1 && node.FixedFPS == 30 && node.FractDelta && node.Interpolate && !node.LocalCoords && node.DrawOrder == ParticlesDrawOrder.Lifetime && node.VisibilityRect == new Rect2(-100, -100, 200, 200) && node.TrailSections == 8 && node.TrailSectionSubdivisions == 4 && node.TrailLifetime == .3 && node.CaptureRect() == default, "Detached configuration defaults have no native session dependency.");
        Reject<ArgumentOutOfRangeException>(() => node.Amount = 0); Reject<ArgumentOutOfRangeException>(() => node.Lifetime = double.Epsilon); Reject<ArgumentOutOfRangeException>(() => node.Lifetime = double.NaN);
        Reject<ArgumentOutOfRangeException>(() => node.TrailSections = 1); Reject<ArgumentOutOfRangeException>(() => node.TrailSections = 129); Reject<ArgumentOutOfRangeException>(() => node.TrailSectionSubdivisions = 0); Reject<ArgumentOutOfRangeException>(() => node.TrailSectionSubdivisions = 1025);
        Reject<ArgumentOutOfRangeException>(() => node.TrailLifetime = .009); Reject<ArgumentOutOfRangeException>(() => node.SpeedScale = double.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => node.DrawOrder = (ParticlesDrawOrder)3);
        Reject<ArgumentNullException>(() => node.SubEmitter = null!); node.AmountRatio = -1; node.InterpToEnd = 2; Check(node.AmountRatio == 0 && node.InterpToEnd == 1, "Ratio controls clamp.");
        using var wrongMaterial = new CanvasItemMaterial(); Reject<NotSupportedException>(() => node.ProcessMaterial = wrongMaterial); Check(node.ProcessMaterial is null, "Invalid compute role preserves authored state.");
        Reject<InvalidOperationException>(() => node.RequestParticlesProcess(.1f)); Check(node.CaptureRect() == default, "Headless authoring does not claim native GPU simulation.");
        using (var automaticRoot = new Entity())
        {
            var automatic = new GPUParticles { OneShot = true, Lifetime = .2, Explosiveness = 1 }; automaticRoot.AddChild(automatic); using var automaticTree = new SceneTree(automaticRoot); var automaticFinished = 0; automatic.Finished += () => automaticFinished++;
            automaticTree.ProcessFrame(.1); Check(automatic.Emitting && automaticFinished == 0, "One-shot authoring initializes its clock from all restored settings at first scene entry.");
            automaticTree.ProcessFrame(.11); Check(!automatic.Emitting && automaticFinished == 1, "A newly authored one-shot completes without an extra restart call.");
        }
        using var root = new Entity(); var one = new GPUParticles { Name = "One", Emitting = false, OneShot = true, SpeedScale = 0, Explosiveness = 1, UseFixedSeed = true, Seed = 17 }; root.AddChild(one); using var tree = new SceneTree(root);
        var completions = 0; one.Finished += () => { completions++; if (completions == 1) one.Restart(true); };
        one.Emitting = true; tree.Paused = true; tree.ProcessFrame(2); Check(completions == 0 && one.Emitting, "Scene pause gates completion tracking.");
        tree.Paused = false; tree.ProcessFrame(1.1); Check(completions == 1 && one.Emitting && one.Seed == 17, "Completion commits before a handler restart, independently of native SpeedScale.");
        tree.ProcessFrame(1.1); Check(completions == 2 && !one.Emitting, "Restarted completion delivers exactly once."); tree.ProcessFrame(3); Check(completions == 2, "Idle completion does not replay.");
        one.Restart(true); one.Emitting = true; tree.ProcessFrame(1.1); Check(completions == 2, "Overlapping one-shot emission cancels its completion signal.");
        using (var convertedSource = new CPUParticles { OneShot = true, Lifetime = .2, Explosiveness = 1 })
        {
            var converted = new GPUParticles(); root.AddChild(converted); var convertedFinished = 0; converted.Finished += () => convertedFinished++;
            converted.ConvertFromParticles(convertedSource); tree.ProcessFrame(.21);
            Check(convertedFinished == 1 && !converted.Emitting, "Conversion on an attached emitter starts one uncancelled completion cycle from copied settings.");
        }
        using var material = new ParticleProcessMaterial { Gravity = new(2, 3), Color = Colors.Red }; one.ProcessMaterial = material; one.TrailEnabled = true; one.TrailSections = 9; one.TrailSectionSubdivisions = 7; one.SubEmitter = "../One";
        using var packed = new PackedScene(); one.Owner = root; packed.Pack(root);
        var path = Path.Combine(Path.GetTempPath(), "electron2d-gpu-node-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var instance = loaded.Instantiate(); var restored = (GPUParticles)instance.GetNode("One");
            Check(restored.Seed == 17 && restored.UseFixedSeed && restored.OneShot && restored.TrailEnabled && restored.TrailSections == 9 && restored.TrailSectionSubdivisions == 7 && restored.SubEmitter == "../One" && restored.ProcessMaterial is ParticleProcessMaterial { Gravity.X: 2, Gravity.Y: 3 }, "Typed node factory, settings, enum and process resource survive a saved scene.");
            Check(restored.CaptureRect() == default, "Scene storage excludes native simulation state.");
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(GPUParticlesNodeTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_PARTICLE_SERVER"] = "node-file"; start.Environment["ELECTRON2D_GPU_NODE_FILE"] = path;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("GPU scene file child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh GPU particle scene pixels passed.", StringComparison.Ordinal), error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
        Conversions();
        Console.WriteLine("GPU emitter headless authoring, ranges, roles, one-shot pause/restart/cancellation and typed scene storage passed.");
    }
}
