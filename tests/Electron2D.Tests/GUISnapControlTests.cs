using Electron2D;
using Path = System.IO.Path;

internal static partial class GUISnapControlTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
    private static bool Near(Vector2 a, Vector2 b) => (a - b).Length() < .0001f;
    internal static void RunManaged()
    {
        using var detached = new Control { Position = new(.5f, -.5f), Size = new(4, 8) };
        Check(detached.GetVisualTransform() == detached.GetTransform(), "Detached controls retain fractional visual transforms.");
        var host = new SubViewport { Size = new(80, 60) }; var control = new ColorRect { Name = "Paint", Position = new(.5f, -.5f), Size = new(4, 8) }; host.AddChild(control);
        using var tree = new SceneTree(host); Check(host.GUISnapControlsToPixels && !host.SnapTransformsToPixel && !host.SnapVerticesToPixel, "GUI snapping defaults independently to true.");
        foreach (var (position, expected) in new[] { (new Vector2(.5f, -.5f), new Vector2(1, 0)), (new Vector2(-1.5f, 1.5f), new Vector2(-1, 2)), (new Vector2(-.501f, .499f), new Vector2(-1, 0)), (new Vector2(.499f, .501f), new Vector2(0, 1)) })
        {
            control.Position = position; var logical = control.GetTransform(); var rect = control.GetRect(); var size = control.Size;
            Check(control.GetVisualTransform().Origin == expected && control.GetTransform() == logical && control.GetRect() == rect && control.Size == size, "Local visual translations use floor(x+.5), preserving logical geometry: p=" + position + " visual=" + control.GetVisualTransform() + " expected=" + expected + " logical=" + control.GetTransform() + " size=" + control.Size);
        }
        control.Position = new(.4f, -.4f); control.PivotOffset = new(.25f, .5f); control.Scale = new(2, 3);
        Check(Near(control.GetTransform().Origin, new(.15f, -1.4f)) && control.GetVisualTransform().Origin == new Vector2(0, -1), "Pivot/scale translation is snapped after the full primary transform.");
        control.OffsetTransformEnabled = true; control.OffsetTransformPosition = new(.25f, .125f);
        Check(Near(control.GetVisualTransform().Origin, new(.5f, -.625f)), "Visual-only offset follows the snapped primary translation and basis.");
        control.OffsetTransformVisualOnly = false; Check(control.GetVisualTransform().Origin == new Vector2(1, -1), "Logical offset participates before snapping.");
        control.OffsetTransformEnabled = false; control.Scale = Vector2.One; control.PivotOffset = Vector2.Zero; control.Position = new(.25f, .25f);
        foreach (var angle in new[] { 0f, Mathf.Pi / 4, Mathf.Pi / 2, Mathf.Pi, -Mathf.Pi / 4, 2 * Mathf.Pi, Mathf.Pi / 4 + .000001f })
        { control.Rotation = angle; var visual = control.GetVisualTransform(); Check(visual.Origin == Vector2.Zero && visual.X == control.GetTransform().X && visual.Y == control.GetTransform().Y, "Near multiples of 45 degrees round translation without changing the basis."); }
        foreach (var angle in new[] { .1f, Mathf.Pi / 4 + .00001f, Mathf.Pi / 2 - .00001f })
        { control.Rotation = angle; Check(control.GetVisualTransform() == control.GetTransform(), "Other primary rotations retain fractional origins."); }
        control.Rotation = 0; control.OffsetTransformEnabled = true; control.OffsetTransformVisualOnly = false; control.OffsetTransformRotation = .1f;
        Check(control.GetVisualTransform().Origin == (control.GetTransform().Origin + new Vector2(.5f, .5f)).Floor(), "The gate uses primary rotation, independently of the additional offset rotation.");
        control.OffsetTransformEnabled = false;
        var transformEvents = 0; control.NotifyLocalTransformChanges = true; control.LocalTransformChanged += _ => transformEvents++;
        var beforeTransform = control.GetTransform(); var beforeRect = control.GetGlobalRect(); host.GUISnapControlsToPixels = false;
        Check(control.GetVisualTransform() == beforeTransform && control.GetGlobalRect() == beforeRect && transformEvents == 0, "Policy changes preserve logical transform notifications and rectangles.");
        var presses = 0; control.GUIInput += input => { if (input is InputEventMouseButton { Pressed: true }) presses++; };
        void Click(Vector2 point)
        { using var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }; host.PushInput(input, true); input.Pressed = false; input.ButtonMask = MouseButtonMask.None; host.PushInput(input, true); }
        foreach (var enabled in new[] { false, true }) { host.GUISnapControlsToPixels = enabled; Click(new(.1f, .5f)); Click(new(.3f, .5f)); }
        Check(presses == 2, "Hit testing uses fractional logical geometry with either visual policy.");
        Reject<InvalidOperationException>(() => Task.Run(() => _ = host.GUISnapControlsToPixels).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => host.GUISnapControlsToPixels = true).GetAwaiter().GetResult());
        var captured = host.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => host.GUISnapControlsToPixels = true); } finally { Node.EndSceneCapture(captured); }
        var nested = new SubViewport { Name = "Nested", Size = new(32, 32), GUISnapControlsToPixels = false }; var nestedControl = new ColorRect { Position = new(.5f, .5f), Size = new(2, 2) }; nested.AddChild(nestedControl); host.AddChild(nested);
        Check(control.GetVisualTransform().Origin == Vector2.Zero && nestedControl.GetVisualTransform().Origin == new Vector2(.5f, .5f), "Nested viewport GUI policies are independent.");
        var parent = new Entity { Position = new(.25f, .125f) }; var child = new Control { Position = new(.5f, .5f), Size = Vector2.One }; parent.AddChild(child); host.AddChild(parent);
        Check(child.RenderGlobal(1).Origin == new Vector2(1.25f, 1.125f) && child.GetGlobalTransform().Origin == new Vector2(.75f, .625f), "Only local Control translation snaps; Entity parents and global logical geometry remain fractional.");
        host.SnapTransformsToPixel = true; host.SnapVerticesToPixel = true; host.GUISnapControlsToPixels = false;
        Check(child.GetVisualTransform().Origin == new Vector2(.5f, .5f), "Canvas snapping switches do not alias GUI snapping metadata."); host.SnapTransformsToPixel = false; host.SnapVerticesToPixel = false;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 32; i++) { host.GUISnapControlsToPixels = (i & 1) == 0; _ = control.GetVisualTransform(); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) { host.GUISnapControlsToPixels = (i & 1) == 0; _ = control.GetVisualTransform(); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Prepared policy/visual queries allocate " + allocated + " managed owner bytes.");
        host.GUISnapControlsToPixels = true; control.Owner = host; nested.Owner = host; nestedControl.Owner = host; parent.Owner = host; child.Owner = host;
        using (var window = new Window { GUISnapControlsToPixels = false })
        using (var windowScene = new PackedScene())
        { windowScene.Pack(window); using var copy = (Window)windowScene.Instantiate(); Check(!copy.GUISnapControlsToPixels, "Window inherits the same stored GUI policy and false override."); }
        using var packed = new PackedScene(); packed.Pack(host); var path = Path.Combine(Path.GetTempPath(), "electron2d-gui-snap-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); Fresh(path); } finally { File.Delete(path); }
        host.RemoveChild(parent); Check(child.GetVisualTransform() == child.GetTransform(), "Detachment removes viewport GUI snapping."); parent.Dispose();
        using var retired = new SubViewport(); retired.Dispose(); Reject<ObjectDisposedException>(() => _ = retired.GUISnapControlsToPixels); Reject<ObjectDisposedException>(() => retired.GUISnapControlsToPixels = true);
        Console.WriteLine("GUI snapping defaults, local/offset/rotation/negative-tie semantics, unchanged logical/input geometry, owner/capture/lifetime, 128 prepared zero-managed-byte queries and fresh nested scene passed.");
    }
    private static void Fresh(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(GUISnapControlTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_GUI_SNAP"); start.ArgumentList.Add("--gui-snap-archive"); start.ArgumentList.Add(path);
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("GUI snap archive child"); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh GUI snap passed"), "Fresh GUI snap: " + error.GetAwaiter().GetResult());
    }
    internal static void RunArchive(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)!; var host = (SubViewport)packed.Instantiate(); using var tree = new SceneTree(host);
        var nested = host.GetChildren().OfType<SubViewport>().Single(); var control = nested.GetChildren().OfType<Control>().Single();
        Check(host.GUISnapControlsToPixels && !nested.GUISnapControlsToPixels && control.GetVisualTransform() == control.GetTransform(), "Fresh graph retains independent viewport policies and executing geometry."); nested.GUISnapControlsToPixels = true; Check(control.GetVisualTransform().Origin == Vector2.One, "Fresh restored policy affects actual visual transforms."); Console.WriteLine("Fresh GUI snap passed");
    }
}
