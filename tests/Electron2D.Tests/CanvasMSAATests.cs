using Electron2D;
using Path = System.IO.Path;

internal static class CanvasMSAATests
{
    internal static void RunManaged()
    {
        using var view = new SubViewport(); Check(view.MSAA2D == Viewport.MSAA.Disabled, "Viewport default is disabled.");
        foreach (var mode in new[] { Viewport.MSAA.Disabled, Viewport.MSAA.Samples2X, Viewport.MSAA.Samples4X, Viewport.MSAA.Samples8X })
        {
            view.MSAA2D = mode; using var scene = new PackedScene(); scene.Pack(view); using var copy = (SubViewport)scene.Instantiate(); Check(copy.MSAA2D == mode && copy.RenderingOwner is null, "Packed scenes retain authored MSAA.");
        }
        foreach (var invalid in new[] { -1, 4, 99 }) Reject<ArgumentOutOfRangeException>(() => view.MSAA2D = (Viewport.MSAA)invalid);
        Check(view.MSAA2D == Viewport.MSAA.Samples8X, "Invalid assignment preserves state.");
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Max));
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetMSAA2D(default, Viewport.MSAA.Disabled));
        view.Dispose(); Reject<ObjectDisposedException>(() => view.MSAA2D = Viewport.MSAA.Disabled);
        var directory = Path.Combine(Path.GetTempPath(), "electron-msaa-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            using var source = new SubViewport { MSAA2D = Viewport.MSAA.Samples4X }; using var scene = new PackedScene(); scene.Pack(source);
            var file = Path.Combine(directory, "viewport.e2dscene"); ResourceSaver.Save(scene, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, cacheMode: ResourceLoader.CacheMode.Ignore); using var restored = (SubViewport)loaded!.Instantiate();
            Check(restored.MSAA2D == Viewport.MSAA.Samples4X, "File scene retains typed MSAA.");
            var info = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add(typeof(CanvasMSAATests).Assembly.Location); info.ArgumentList.Add("--msaa-scene"); info.ArgumentList.Add(file);
            using var child = System.Diagnostics.Process.Start(info)!; var stdout = child.StandardOutput.ReadToEnd(); var stderr = child.StandardError.ReadToEnd();
            child.WaitForExit(); Check(child.ExitCode == 0, $"Fresh scene process: {stdout} {stderr}");
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Canvas MSAA validation and scene storage passed.");
    }
    internal static void ReadFile(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var node = (SubViewport)scene.Instantiate(); Check(node.MSAA2D == Viewport.MSAA.Samples4X, "Fresh scene reconstructs typed coverage mode.");
        Console.WriteLine("Fresh MSAA scene reader passed.");
    }
    private sealed class CaptureProbe : Entity
    {
        internal Action? Attempt;
        protected override void OnDraw() => Attempt?.Invoke();
    }
    internal static void RunNative(string backend, bool HDR = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Disabled);
        if (backend == "compatibility") { Compatibility(); return; }
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [new(2.2f, 2.4f), new(34.4f, 3.9f), new(7.3f, 31.8f)] });
        var window = new Window { Size = new(128, 96), MSAA2D = Viewport.MSAA.Samples2X, UseHDR2D = HDR };
        var view = new SubViewport { Size = new(128, 96), MSAA2D = Viewport.MSAA.Samples4X, UseHDR2D = HDR, RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(view);
        using var rootTexture = window.GetTexture(); using var viewTexture = view.GetTexture();
        var capture = new CaptureProbe(); window.AddChild(capture);
        RID canvas = default, item = default, owned = default, ownedTexture = default; var phase = 0; var warm = 0; long before = 0, active = 0, idle = 0; Vector2 sizeBefore = default, lastSize = default;
        window.Ready += _ =>
        {
            Check(window.MSAA2D == Viewport.MSAA.Samples2X && RenderingServer.GetCurrentRenderingMethod() == "gpu", "Explicit root selection and exact GPU backend.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            capture.Attempt = () => Reject<InvalidOperationException>(() => RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), Viewport.MSAA.Disabled));
            Reject<InvalidOperationException>(() => Task.Run(() => view.MSAA2D = Viewport.MSAA.Disabled).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => view.MSAA2D).GetAwaiter().GetResult());
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetMSAA2D(foreign.GetViewportRID(), Viewport.MSAA.Disabled));
            owned = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(owned, 128, 96); RenderingServer.ViewportSetActive(owned, true);
            RenderingServer.ViewportSetUpdateMode(owned, ViewportUpdateMode.Always); RenderingServer.ViewportSetMSAA2D(owned, Viewport.MSAA.Samples8X); ownedTexture = RenderingServer.ViewportGetTexture(owned); RenderingServer.ViewportSetUseHDR2D(owned, HDR);
            canvas = RenderingServer.CanvasCreate(); item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, canvas);
            foreach (var target in new[] { window.GetViewportRID(), view.GetViewportRID(), owned }) RenderingServer.ViewportAttachCanvas(target, canvas);
            RenderingServer.CanvasItemAddMesh(item, mesh.GetRID(), Transform.Identity, Colors.White);
            foreach (var invalid in new[] { -1, 4, 99 }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), (Viewport.MSAA)invalid));
            Reject<ArgumentException>(() => RenderingServer.ViewportSetMSAA2D(canvas, Viewport.MSAA.Disabled));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), Viewport.MSAA.Disabled)).GetAwaiter().GetResult());
            RenderingServer.FramePreDraw += () =>
            {
                if (phase < 9) return;
                sizeBefore = rootTexture.GetSize(); before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) { RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), Viewport.MSAA.Samples4X); RenderingServer.CanvasItemSetTransform(item, new(0, new(warm % 2, 0))); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase >= 9)
                {
                    var size = rootTexture.GetSize();
                    if (size != lastSize || size != sizeBefore) { lastSize = size; warm = 0; active = idle = 0; }
                    else if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160) { Check(active == 0 && idle == 0, $"64 active/64 idle MSAA frames allocate zero managed bytes: {active}/{idle}."); RenderingServer.FreeRID(owned); RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas); window.Tree!.Quit(); }
                    if (++phase > 2048) throw new Exception("Stable MSAA preparation was not reached."); return;
                }
                using var a = rootTexture.GetImage()!; using var b = viewTexture.GetImage()!; using var c = RenderingServer.Texture2DGet(ownedTexture)!;
                switch (phase++)
                {
                    case 0:
                        Coverage(a, 2); Coverage(b, 4); Coverage(c, 8);
                        RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), Viewport.MSAA.Disabled);
                        Check(window.MSAA2D == Viewport.MSAA.Samples2X && rootTexture.GetRID() == window.GetTexture().GetRID(), "Server override preserves scene value and texture identity."); break;
                    case 1:
                        Coverage(a, 1); Coverage(b, 4); Coverage(c, 8); window.MSAA2D = window.MSAA2D; break;
                    case 2:
                        Coverage(a, 2); RenderingServer.ViewportSetMSAA2D(window.GetViewportRID(), Viewport.MSAA.Samples8X); break;
                    case 3:
                        Coverage(a, 8); window.MSAA2D = Viewport.MSAA.Samples4X; break;
                    case 4:
                        Coverage(a, 4); view.RenderTargetClearMode = ViewportClearMode.Never; RenderingServer.CanvasItemClear(item); break;
                    case 5:
                        Check(CountPartial(a) == 0, "Always clear removes drawing."); Coverage(b, 4);
                        RenderingServer.CanvasItemAddMesh(item, mesh.GetRID(), Transform.Identity, Colors.White); RenderingServer.CanvasItemSetTransform(item, new(0, new(48, 0))); break;
                    case 6:
                        Coverage(b, 4); Check(b.GetPixel(55, 12).R > .9f, "Never clear retains old samples and adds new drawing.");
                        view.RenderTargetClearMode = ViewportClearMode.Always; RenderingServer.CanvasItemSetTransform(item, Transform.Identity);
                        view.Size = new(144, 112); break;
                    case 7:
                        Check(b.Size == new Vector2i(144, 112), "MSAA targets resize with stable logical identity."); Coverage(b, 4); view.Size = new(128, 96); break;
                    case 8:
                        Coverage(b, 4); RenderingServer.CanvasItemSetTransform(item, Transform.Identity); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && warm == 160, "MSAA host completed.");
        Reject<ArgumentException>(() => Viewport.ResolveViewportRID(owned)); Check(window.IsDisposed && window.RenderingOwner is null, "All MSAA targets retire at shutdown.");
        Console.WriteLine($"GPU HDR={HDR}, coverage samples 1/2/4/8, independent viewports, clear history and warm frames passed ({active}/{idle} bytes).");
    }
    private static int CountPartial(Image image)
    {
        var count = 0; for (var y = 0; y < 35; y++) for (var x = 0; x < 38; x++) { var value = image.GetPixel(x, y).R; if (value > .01f && value < .99f) count++; }
        return count;
    }
    private static void Coverage(Image image, int samples)
    {
        var partial = 0; var filled = 0;
        for (var y = 0; y < 35; y++) for (var x = 0; x < 38; x++)
            {
                var value = image.GetPixel(x, y).R; if (value > .99f) filled++;
                if (value > .01f && value < .99f) partial++;
                Check(MathF.Abs(value * samples - MathF.Round(value * samples)) < .04f, $"Resolved sample quantization at {x},{y}: {value}, samples={samples}.");
            }
        Check(filled > 100 && (samples == 1 ? partial == 0 : partial > 10), $"Actual geometry coverage: samples={samples}, partial={partial}, filled={filled}.");
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.White); }
    private static void Compatibility()
    {
        var window = new Window { Size = new(128, 96), MSAA2D = Viewport.MSAA.Samples4X }; using var target = window.GetTexture(); window.AddChild(new Box());
        Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && !RenderingServer.IsAvailable, "Compatibility refuses enabled MSAA and cleans its host.");
        ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples2X); var configured = new Window { Size = new(128, 96) }; using var configuredTarget = configured.GetTexture();
        Reject<NotSupportedException>(() => Engine.Run(configured)); Check(!RenderingServer.IsAvailable && configured.IsDisposed, "Root startup setting executes instead of storing inert configuration.");
        ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Disabled);
        Console.WriteLine("Compatibility MSAA capability and configured-root rejection passed.");
    }
    private static void Reject<T>(Action call) where T : Exception { try { call(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
