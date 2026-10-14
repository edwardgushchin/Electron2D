using Electron2D;
using System.Diagnostics;

internal static class CanvasModulateTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static Color CanvasColor(CanvasItem node) => RenderingCanvasRegistry.Resolve(node.GetCanvas()).Modulate;
    internal static void RunManaged()
    {
        using (var automatic = new Window())
        {
            using var saved = new PackedScene(); saved.Pack(automatic); using var restored = (Window)saved.Instantiate();
            Check(!restored.HasAuthoredPosition && restored.Position == Vector2i.Zero, "Scene restoration preserves automatic native window placement.");
            automatic.Position = Vector2i.Zero; saved.Pack(automatic); using var explicitZero = (Window)saved.Instantiate();
            Check(explicitZero.HasAuthoredPosition && explicitZero.Position == Vector2i.Zero, "Explicit zero position remains an authored request.");
        }
        using var detached = new CanvasModulate(); Check(detached.Color == Colors.White && detached.GetCanvas() == default, "Detached defaults.");
        Reject<ArgumentException>(() => detached.Color = new(float.NaN, 1, 1)); Check(detached.Color == Colors.White, "Invalid color rejects before mutation.");
        detached.Notify(CanvasItem.NotificationEnterCanvas); Check(detached.GetCanvas() == default, "Manual entry cannot activate a detached node.");
        var root = new SubViewport { Size = new(96, 64) }; var first = new CanvasModulate { Name = "First", Color = Colors.Red }; var second = new CanvasModulate { Name = "Second", Color = Colors.Blue };
        var layer = new CanvasLayer { Name = "Layer" }; root.AddChild(first); root.AddChild(second); root.AddChild(layer);
        using var tree = new SceneTree(root);
        Check(CanvasColor(first) == Colors.Red && first.GetConfigurationWarnings().Length == 1 && second.GetConfigurationWarnings().Length == 1, "One modulator selected and both report duplicates.");
        first.Hide(); Check(CanvasColor(second) == Colors.Blue && second.GetConfigurationWarnings().Length == 0, "Hiding promotes the waiting modulator.");
        first.Show(); first.Color = Colors.Green; Check(CanvasColor(second) == Colors.Blue, "Waiting color assignment cannot override the active node.");
        first.Reparent(layer); Check(CanvasColor(first) == Colors.Green && CanvasColor(second) == Colors.Blue && first.GetConfigurationWarnings().Length == 0, "Independent layer canvas and reparenting.");
        first.Notify(CanvasItem.NotificationExitCanvas); Check(CanvasColor(first) == Colors.Green, "Manual exit does not change actual membership.");
        layer.Hide(); Check(CanvasColor(first) == Colors.White, "Layer visibility removes modulation."); layer.Show(); Check(CanvasColor(first) == Colors.Green, "Layer visibility restores modulation.");
        first.TopLevel = true; Check(CanvasColor(first) == Colors.Green, "TopLevel rebinding keeps layer identity and color.");
        var previousCanvas = RenderingCanvasRegistry.Resolve(second.GetCanvas()); root.World = null;
        Check(previousCanvas.Modulate == Colors.White && CanvasColor(second) == Colors.Blue && CanvasColor(first) == Colors.Green, "World replacement moves default-canvas modulation without changing layer modulation.");
        var shared = new SubViewport { Name = "Shared", Size = new(32, 32), World = root.World }; var waiting = new CanvasModulate { Color = Colors.Yellow }; shared.AddChild(waiting); root.AddChild(shared);
        Check(waiting.GetCanvas() == second.GetCanvas() && CanvasColor(waiting) == Colors.Blue && waiting.GetConfigurationWarnings().Length == 1, "Shared World viewports share modulation arbitration.");
        second.Dispose(); Check(CanvasColor(waiting) == Colors.Yellow, "Disposal promotes a node in another viewport sharing the World.");
        var captured = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => first.Color = Colors.Red); } finally { Node.EndSceneCapture(captured); }
        Task.Run(() => { Reject<InvalidOperationException>(() => _ = first.Color); Reject<InvalidOperationException>(() => first.Color = Colors.Red); Reject<InvalidOperationException>(() => first.GetConfigurationWarnings()); }).GetAwaiter().GetResult();
        tree.EditedSceneRoot = root; Action<SceneTree, Node> fail = (_, _) => throw new InvalidOperationException("warning callback"); tree.NodeConfigurationWarningChanged += fail;
        Reject<AggregateException>(() => first.Hide()); Check(CanvasColor(first) == Colors.White && !first.Visible, "Warning failure retains committed visibility and clears modulation."); tree.NodeConfigurationWarningChanged -= fail; first.Show();
        for (var i = 0; i < 16; i++) first.Color = (i & 1) == 0 ? Colors.Red : Colors.Green;
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) first.Color = (i & 1) == 0 ? Colors.Red : Colors.Green;
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed color updates allocate zero managed bytes.");
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Electron2D-modulate-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "scene.e2dscene"); first.Owner = root; layer.Owner = root; using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, path);
            RunArchive(path);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(typeof(CanvasModulateTests).Assembly.Location); start.ArgumentList.Add("--canvas-modulate-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_CANVAS_MODULATE");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh modulation archive deadline."); }
            Check(child.ExitCode == 0, "Fresh process: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
        waiting.Hide(); Check(CanvasColor(waiting) == Colors.White, "Last modulator hiding restores opaque white.");
        Console.WriteLine("CanvasModulate managed: arbitration, layers/shared worlds, visibility, reparent, disposal, warnings/failures, guards, fresh scene and 64 warmed color updates=0 B passed.");
    }
    internal static void RunArchive(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var root = scene.Instantiate(); using var tree = new SceneTree(root);
        var node = (CanvasModulate)root.GetNode("Layer/First"); Check(node.Color == Colors.Green && node.TopLevel && CanvasColor(node) == Colors.Green, "Fresh scene restores exact node, color, layer and active modulation.");
    }
    private static void Pixel(Image image, int x, Color expected)
    {
        var actual = image.GetPixel(x, 8); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Pixel {x},8 expected {expected}, actual {actual}.");
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var unshaded = new CanvasItemMaterial { LightMode = LightMode.Unshaded };
        var window = new Window { Size = new(96, 64), Title = "Canvas modulation validation" }; var first = new CanvasModulate { Name = "Primary", Color = Colors.Red }; var second = new CanvasModulate { Name = "Secondary", Color = Colors.Blue };
        var layer = new CanvasLayer(); var layerModulator = new CanvasModulate { Name = "LayerTint", Color = Colors.Green }; var light = new DirectionalLight { Color = Colors.Red, Energy = .25f, Enabled = false };
        window.AddChild(new Sprite { Name = "Normal", Texture = texture, Centered = false, Position = new(4, 4) }); window.AddChild(new Sprite { Name = "Unshaded", Texture = texture, Centered = false, Position = new(20, 4), Material = backend == "gpu" ? unshaded : null });
        layer.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(36, 4) }); layer.AddChild(layerModulator); window.AddChild(layer); window.AddChild(first); window.AddChild(second); window.AddChild(light);
        using var target = window.GetTexture(); var frames = 0; long start = 0, bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames >= 16) { start = GC.GetAllocatedBytesForCurrentThread(); second.Color = (frames & 1) == 0 ? new(.2f, .2f, .2f) : new(.3f, .3f, .3f); }
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - start;
            frames++;
            if (frames is >= 4 and <= 10)
            {
                using var pixels = target.GetImage()!;
                Pixel(pixels, 8, frames switch { 4 => Colors.Red, 5 or 6 or 7 => Colors.Blue, 8 => Colors.White, 9 => Colors.Yellow, _ => backend == "gpu" ? new(.35f, .1f, .1f) : new(.1f, .1f, .1f) });
                Pixel(pixels, 40, frames == 6 ? Colors.Black : frames <= 7 ? Colors.Green : Colors.Red);
                if (frames == 10 && backend == "gpu") Pixel(pixels, 24, Colors.White);
                if (frames == 4 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { Directory.CreateDirectory(capture); pixels.SavePNG(System.IO.Path.Combine(capture, "canvas-modulate-" + backend + ".png")); }
            }
            if (frames == 4) first.Hide();
            if (frames == 5) { first.Color = Colors.Green; layer.Hide(); }
            if (frames == 6) { layer.Show(); first.Reparent(layer); first.Show(); }
            if (frames == 7) { second.Hide(); layerModulator.Hide(); first.Color = Colors.Red; }
            if (frames == 8) { first.TopLevel = true; RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, Colors.Yellow); }
            if (frames == 9) { second.Show(); second.Color = new(.1f, .1f, .1f); light.Enabled = backend == "gpu"; }
            if (frames == 10) light.Enabled = false;
            if (frames == 80) { Check(bytes == 0, "64 warmed color/layout/render owner frames: " + bytes + " B."); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); }
        }
        Check(Engine.Run(window) == 0 && frames == 80, "Native modulation workflow completes.");
        Console.WriteLine("CanvasModulate native " + backend + ": seven pixel phases, arbitration/layers/server override, " + (backend == "gpu" ? "lighting/unshaded, " : "") + "64 warmed owner frames=" + bytes + " B passed.");
    }
}
