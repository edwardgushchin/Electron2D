using Electron2D;
using Electron2D.Examples.ProceduralNoise;
using System.Diagnostics;

internal static class NoiseGenerationTests
{
    private sealed class ControlledNoise : Noise
    {
        internal readonly ManualResetEventSlim Started = new(), Release = new(true);
        internal bool Block, Fail, Recurse;
        internal NoiseTexture? Texture;
        internal float Value = -1;
        internal int ThreadID;
        internal void Set(float value) { Value = value; EmitChanged(); }
        public override float GetNoise1D(float x) => Value;
        public override float GetNoise2D(float x, float y) => Value;
        public override Image GetImage(int width, int height, bool invert = false, bool normalize = true)
        {
            var value = Value; var fail = Fail; ThreadID = Environment.CurrentManagedThreadId;
            if (Block) { Started.Set(); if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Sampler release timed out."); }
            if (fail) throw new InvalidOperationException("Sampler fixture failed.");
            if (Recurse) Texture!.GetImage();
            var bytes = new byte[width * height]; Array.Fill(bytes, (byte)Math.Clamp((value + 1) * 127.5f, 0, 255)); return Image.CreateFromData(width, height, false, Image.Format.L8, bytes);
        }
        protected override void Dispose(bool disposing) { if (disposing) { Started.Dispose(); Release.Dispose(); } base.Dispose(disposing); }
    }
    internal static void Run()
    {
        if (!WorkerThreadPool.Service.HasRuntime) ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        NoiseTests.Run(); NoiseTextureTests.Run(); FastNoiseLiteTests.Run(); NoiseImageReferenceTests.Run();
        using var noise = new ControlledNoise(); using var texture = new NoiseTexture { Width = 8, Height = 4, GenerateMIPMaps = false, Normalize = false, Noise = noise }; noise.Texture = texture;
        using var tree = new SceneTree(new Entity()); var owner = Environment.CurrentManagedThreadId; var changes = 0;
        texture.Changed += _ => { Check(Environment.CurrentManagedThreadId == owner, "Completed pixel signals execute on the owner"); changes++; };
        Engine.Start(tree);
        try
        {
            Check(texture.GetImage() is null && changes == 0, "First scene read queues generation without pixels or completion");
            tree.FlushDeferred(); Check(changes == 1 && Pixel(texture) == 0 && noise.ThreadID == owner, "First deferred generation computes on owner before completion");
            if (OperatingSystem.IsBrowser()) return;
            noise.Block = true; noise.Release.Reset(); noise.Set(.5f); tree.FlushDeferred(); Check(noise.Started.Wait(TimeSpan.FromSeconds(5)), "Later generation runs on worker");
            var task = Pending(texture); Check(noise.ThreadID != owner && Pixel(texture) == 0 && changes == 1, "Pending worker keeps old image and completion signal");
            for (var i = 0; i < 16; i++) _ = texture.CapturePixels(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) _ = texture.CapturePixels(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "1000 pending renderer reads allocate zero bytes");
            var guard = WorkerThreadPool.AddTask(() => Reject<InvalidOperationException>(texture.Dispose), highPriority: true); WorkerThreadPool.WaitForTaskCompletion(guard); Check(!texture.IsDisposed, "Worker disposal rejects before logical lifetime mutation");
            noise.Set(1); noise.Block = false; noise.Release.Set(); Pump(tree, () => changes == 2); Check(Pixel(texture) == 255 && changes == 2, "Stale result is discarded and latest captured source publishes once"); Retired(task);
            var previous = texture.CapturePixels(); noise.Fail = true; noise.Set(0); tree.FlushDeferred(); task = Pending(texture); OwnerFailure(tree, "Sampler fixture failed."); Check(ReferenceEquals(previous, texture.CapturePixels()) && Pixel(texture) == 255, "Failed generation retains readable and rendered storage"); Retired(task);
            noise.Fail = false; noise.Set(0); Pump(tree, () => changes == 3); Check(Pixel(texture) == 127, "Corrected input retries after worker failure");
            noise.Recurse = true; noise.Set(.25f); tree.FlushDeferred(); OwnerFailure(tree, "reentrant"); Check(Pixel(texture) == 127, "Recursive worker read fails without destroying completed image"); noise.Recurse = false; noise.Set(.25f); Pump(tree, () => changes == 4);
            var rendered = texture.CapturePixels(); texture.Noise = null; Pump(tree, () => changes == 5); Check(texture.GetImage() is null && ReferenceEquals(rendered, texture.CapturePixels()), "Null image publication retains preceding rendering allocation");
            texture.Noise = noise; noise.Block = true; noise.Started.Reset(); noise.Release.Reset(); tree.FlushDeferred(); Check(noise.Started.Wait(TimeSpan.FromSeconds(5)), "Teardown fixture has live generation"); task = Pending(texture); noise.Block = false; noise.Release.Set(); texture.Dispose(); Retired(task); Check(!noise.IsDisposed, "Texture teardown drains its task and borrows source lifetime");
        }
        finally { noise.Release.Set(); texture.Dispose(); Engine.Stop(); }
        RoundTrip();
        Console.WriteLine("Noise generation passed: deferred owner first image, worker completion, coalescing, errors/retry, recursive/worker disposal guards, null render retention, task retirement, portable/local graphs and 1000 zero-allocation pending reads.");
    }
    private static long Pending(NoiseTexture texture) => ((NoiseTexture.GenerationWork)typeof(NoiseTexture).GetField("_pending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(texture)!).TaskID;
    private static void Retired(long task) => Reject<ArgumentException>(() => WorkerThreadPool.IsTaskCompleted(task));
    private static byte Pixel(NoiseTexture texture) { using var image = texture.GetImage()!; return image.GetData()[0]; }
    private static void Pump(SceneTree tree, Func<bool> done) { for (var i = 0; i < 5000; i++) { tree.FlushDeferred(); if (done()) return; Thread.Sleep(1); } throw new InvalidOperationException("Noise publication timed out."); }
    private static void OwnerFailure(SceneTree tree, string message)
    { for (var i = 0; i < 5000; i++) { try { tree.FlushDeferred(); } catch (Exception error) { Check(error.ToString().Contains(message, StringComparison.OrdinalIgnoreCase), "Expected owner failure: " + error); return; } Thread.Sleep(1); } throw new InvalidOperationException("Expected owner failure was not delivered."); }
    private static void RoundTrip()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-noise-" + Guid.NewGuid().ToString("N"));
        try
        {
            NoiseAssets.Write(directory); RunChild(directory); ThreadedGraph(directory);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(NoiseGenerationTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_NOISE_CHILD"] = directory; using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync(); Check(process.WaitForExit(20000), "Fresh noise process completes"); Check(process.ExitCode == 0, output.Result + errors.Result);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static void ThreadedGraph(string directory)
    {
        using var tree = new SceneTree(new Entity()); Engine.Start(tree); var path = System.IO.Path.Combine(directory, "noise.e2dres"); NoiseTexture? texture = null;
        using var started = new CountdownEvent(2); using var release = new ManualResetEventSlim(); long first = 0, second = 0;
        try
        {
            ResourceLoader.LoadThreadedRequest<NoiseTexture>(path, useSubThreads: true, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            for (var i = 0; i < 5000 && ResourceLoader.LoadThreadedGetStatus(path) == ResourceLoader.ThreadLoadStatus.InProgress; i++) { tree.FlushDeferred(); Thread.Sleep(1); }
            texture = ResourceLoader.LoadThreadedGet<NoiseTexture>(path); Check(texture.GetImage() is null, "Prepared file graph keeps image generation deferred until its owner consumes it"); tree.FlushDeferred(); using var image = texture.GetImage()!;
            Check(image.GetData().SequenceEqual(File.ReadAllBytes(System.IO.Path.Combine(directory, "initial.rgba"))), "Threaded publication supplies the complete authored source graph");
            var source = (FastNoiseLite)texture.Noise!; var ramp = texture.ColorRamp!;
            Action hold = () => { started.Signal(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Graph worker release timed out."); };
            first = WorkerThreadPool.AddTask(hold, true); second = WorkerThreadPool.AddTask(hold, true); Check(started.Wait(TimeSpan.FromSeconds(5)), "Both retained worker lanes are occupied"); source.Seed = 42; tree.FlushDeferred(); var task = Pending(texture);
            release.Set(); texture.Dispose(); Retired(task); Check(source.IsDisposed && ramp.IsDisposed, "Loaded-root teardown drains generation before releasing its owned decoded sources");
        }
        finally { release.Set(); if (first != 0) WorkerThreadPool.WaitForTaskCompletion(first); if (second != 0) WorkerThreadPool.WaitForTaskCompletion(second); texture?.Dispose(); Engine.Stop(); }
    }
    internal static void RunChild(string directory)
    {
        using var scene = new NoiseScene(directory); var a = scene.First; var b = scene.Second;
        Check(NoiseAssets.Settings((FastNoiseLite)a.Noise!) == File.ReadAllText(System.IO.Path.Combine(directory, "settings.txt")), "Every concrete noise configuration field survives the archive");
        Check(a.Width == 32 && a.Height == 32 && a.GenerateMIPMaps && a.Seamless && a.SeamlessBlendSkirt == .2f && a.ResourceLocalToScene && a.ColorRamp is not null && !a.AsNormalMap && a.Normalize && !a.Invert && a.BumpStrength == 8, "Noise texture authored settings survive the resource and local scene graphs");
        using var original = a.GetImage()!; Check(original.GetData().SequenceEqual(File.ReadAllBytes(System.IO.Path.Combine(directory, "initial.rgba"))), "Fresh generated pixels and mipmaps agree with author output");
        scene.Stage(); using var updated = a.GetImage()!; using var independent = b.GetImage()!;
        Check(updated.GetData().SequenceEqual(File.ReadAllBytes(System.IO.Path.Combine(directory, "updated.rgba"))) && independent.GetData().SequenceEqual(original.GetData()) && !updated.GetData().SequenceEqual(original.GetData()), "Local generator edits change only their own complete image");
        Console.WriteLine("Fresh noise resource and scene graphs passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
