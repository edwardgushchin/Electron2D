using Electron2D;

internal static class WindowPlacementTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
    internal static void RunManaged()
    {
        using var window = new ContentWindow();
        Check(window.InitialPosition == Window.WindowInitialPosition.Absolute, "Initial placement defaults to absolute.");
        var notifications = 0; window.PropertyListChanged += _ => notifications++;
        foreach (var policy in Enum.GetValues<Window.WindowInitialPosition>()) window.InitialPosition = policy;
        Reject<ArgumentOutOfRangeException>(() => window.InitialPosition = (Window.WindowInitialPosition)6);
        Check(window.InitialPosition == Window.WindowInitialPosition.CenterScreenWithKeyboardFocus, "Invalid policy preserves state.");
        window.InitialPosition = window.InitialPosition; Check(notifications == 7, "All valid assignments, including equal values, notify after commitment; invalid values do not.");
        Action<ElectronObject> failObserver = _ => throw new InvalidOperationException("property observer"); window.PropertyListChanged += failObserver;
        Reject<InvalidOperationException>(() => window.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen); Check(window.InitialPosition == Window.WindowInitialPosition.CenterPrimaryScreen, "Observer failure follows policy commitment."); window.PropertyListChanged -= failObserver;
        window.MinSize = new(50, 20); window.Size = new(300, 200); window.ResetSize();
        Check(window.Size == new Vector2i(50, 20), "Reset uses authored bounds without wrapping.");
        window.WrapControls = true; window.ContentScaleFactor = 1.5f; window.Size = new(300, 200); window.Calls = 0; window.ResetSize();
        Check(window.Size == new Vector2i(50, 28) && window.Calls == 1, "Reset samples scaled fractional content exactly once and rounds upward.");
        window.MaxSize = new(60, 25); window.Size = new(300, 200); window.ResetSize();
        Check(window.Size == new Vector2i(50, 25), "Each bounded maximum clips oversized wrapped content.");
        var previous = window.Size; window.Fail = true; Reject<InvalidOperationException>(window.ResetSize); Check(window.Size == previous, "Failed content measurement preserves size."); window.Fail = false;
        window.Content = new(float.MaxValue, 1); Reject<OverflowException>(window.ResetSize); Check(window.Size == previous, "Unrepresentable scaled minimum preserves size."); window.Content = new(30.25f, 18.5f);
        window.DuringMeasure = window.ResetSize; Reject<InvalidOperationException>(window.ResetSize); Check(window.Size == previous, "Reentrant minimum measurement preserves size."); window.DuringMeasure = null;
        var retiring = new ContentWindow { WrapControls = true }; retiring.DuringMeasure = retiring.Dispose; Reject<ObjectDisposedException>(retiring.ResetSize); Check(retiring.IsDisposed, "Disposing from the content hook cannot commit a later size.");
        using var growing = new ContentWindow { Content = new(200, 140) }; growing.Calls = 0; growing.WrapControls = true; Check(growing.Size == new Vector2i(200, 140) && growing.Calls == 1, "Wrapped growth uses its measured minimum without invoking the content hook twice.");
        using var empty = new Window(); empty.ResetSize(); Check(empty.Size == Vector2i.One, "Empty reset retains positive portable viewport dimensions.");
        var captured = window.BeginSceneCapture();
        try { Reject<InvalidOperationException>(window.ResetSize); Reject<InvalidOperationException>(() => window.InitialPosition = Window.WindowInitialPosition.Absolute); }
        finally { Node.EndSceneCapture(captured); }
        using var standard = new Window { InitialPosition = Window.WindowInitialPosition.CenterOtherScreen, Position = new(7, 9), MinSize = new(40, 30) };
        using var packed = new PackedScene(); packed.Pack(standard); using var copy = (Window)packed.Instantiate();
        Check(copy.InitialPosition == standard.InitialPosition && copy.Position == standard.Position, "Packed windows preserve placement separately from authored position.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-placement-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)!; using var fresh = (Window)loaded.Instantiate(); Check(fresh.InitialPosition == standard.InitialPosition, "Typed archive stores the enum without a native window."); }
        finally { File.Delete(path); }
        var host = new SubViewport { Size = new(200, 100), GUIEmbedSubwindows = true }; var child = new Window { Borderless = true, Size = new(40, 20), Position = new(3, 4), InitialPosition = Window.WindowInitialPosition.Absolute }; host.AddChild(child);
        using (var tree = new SceneTree(host))
        {
            Check(child.Position == new Vector2i(3, 4), "Absolute embedded placement is retained.");
            foreach (var policy in Enum.GetValues<Window.WindowInitialPosition>().Where(p => p != Window.WindowInitialPosition.Absolute))
            {
                child.InitialPosition = policy; Check(child.Position != new Vector2i(80, 40), "Changing visible placement does not immediately move the window.");
                child.Hide(); child.Show(); Check(child.Position == new Vector2i(80, 40), "Every centered mode uses the actual embedding viewport.");
                child.Position = new(3, 4);
            }
            child.Hide(); child.InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen; child.Popup(new(new(5, 6), new(40, 20))); Check(child.Position == new Vector2i(80, 40), "Initial show policy also applies to Popup geometry.");
            host.Size = new(240, 100); Check(child.Position == new Vector2i(80, 40), "Initial placement does not continuously track parent resizing.");
            Task.Run(() => { Reject<InvalidOperationException>(() => child.InitialPosition = Window.WindowInitialPosition.Absolute); Reject<InvalidOperationException>(child.ResetSize); Reject<InvalidOperationException>(() => _ = child.InitialPosition); }).GetAwaiter().GetResult();
        }
        var disposed = new Window(); disposed.Dispose(); Reject<ObjectDisposedException>(() => _ = disposed.InitialPosition); Reject<ObjectDisposedException>(() => disposed.InitialPosition = Window.WindowInitialPosition.Absolute); Reject<ObjectDisposedException>(disposed.ResetSize);
        Console.WriteLine("Window initial policies, bounds/scaled reset, failure rollback, packed/archive, embedding and owner/lifetime checks passed.");
    }
    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile == "reject")
        {
            var unsupported = new Window { Size = new(96, 64), InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen };
            Reject<NotSupportedException>(() => Engine.Run(unsupported));
            Check(!RenderingServer.IsAvailable && DisplayServer.Service is null && Engine.MainLoop is null, "Failed native placement releases host services.");
            if (!unsupported.IsDisposed) unsupported.Dispose();
            Console.WriteLine("Unavailable native placement rejects with complete host cleanup."); return;
        }
        Window native = profile == "reset" ? new ContentWindow { Content = new(64, 44), WrapControls = true, ContentScaleFactor = 1.5f, MinSize = new(80, 60), Size = new(240, 180) } : new Window { Size = new(128, 96), GUIEmbedSubwindows = true }; var frames = 0;
        if (profile == "reset")
        {
            native.AddChild(new Box()); native.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Effective renderer."); native.ResetSize();
                RenderingServer.FramePostDraw += () =>
                {
                    if (native.Size == new Vector2i(96, 66))
                    {
                        using var image = native.GetTexture().GetImage()!; Check(image.Width == 96 && image.Height == 66 && image.GetPixel(4, 4).G > .99f, "Reset resizes the native client and renders scaled content into the new image.");
                        frames++; native.Tree!.Quit();
                    }
                    else if (++frames > 30) throw new InvalidOperationException("Native reset did not reach its constrained size: " + native.Size);
                };
            };
        }
        else if (profile == "embedded")
        {
            var child = new Window { Borderless = true, Size = new(32, 24), Position = new(3, 4), InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen };
            child.AddChild(new Box()); native.AddChild(child); var policy = 1;
            native.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Effective renderer."); RenderingServer.SetDefaultClearColor(Colors.Black);
                var expected = (Vector2i)((native.GetVisibleRect().Size - (Vector2)child.Size) / 2);
                RenderingServer.FramePostDraw += () =>
                {

                    Check(child.Position == expected, "Embedded placement committed before drawing.");
                    using var image = native.GetTexture().GetImage()!; var color = image.GetPixel(expected.X + 4, expected.Y + 4); Check(color.G > .99f && color.R < .01f && color.B < .01f, "Placed child produces actual green pixels.");
                    if (++frames == 6) { native.Tree!.Quit(); return; }
                    policy = frames == 1 ? 0 : frames; child.Hide(); child.Position = new(3, 4); child.InitialPosition = (Window.WindowInitialPosition)policy; child.Show(); expected = policy == 0 ? new(3, 4) : (Vector2i)((native.GetVisibleRect().Size - (Vector2)child.Size) / 2);
                };
            };
        }
        else
        {
            var policy = int.Parse(profile); native.InitialPosition = (Window.WindowInitialPosition)policy; if (policy == 0) native.Position = new(200, 300);
            native.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Effective renderer.");
                var screen = policy switch { 1 => DisplayServer.ScreenPrimary, 2 => DisplayServer.ScreenOfMainWindow, 3 => native.CurrentScreen, 4 => DisplayServer.ScreenWithMouseFocus, 5 => DisplayServer.ScreenWithKeyboardFocus, _ => DisplayServer.ScreenPrimary };
                var expected = policy == 0 ? new Vector2i(200, 300) : DisplayServer.ScreenGetPosition(screen) + (DisplayServer.ScreenGetSize(screen) - native.Size) / 2;
                native.InitialPosition = Window.WindowInitialPosition.Absolute;
                RenderingServer.FramePostDraw += () =>
                {
                    if (native.Position == expected) { frames++; native.Tree!.Quit(); }
                    else if (++frames > 30) throw new InvalidOperationException("Native placement did not reach requested position: " + native.Position + " expected " + expected + " decorated " + native.GetPositionWithDecorations() + " size " + native.Size + " outer " + native.GetSizeWithDecorations());
                };
            };
        }
        try { Check(Engine.Run(native) == 0 && frames > 0, "Window placement frame lifecycle."); }
        finally { if (!native.IsDisposed) native.Dispose(); }
        Console.WriteLine("Window placement native profile passed: " + profile);
    }
    private sealed class ContentWindow : Window
    {
        internal Vector2 Content = new(30.25f, 18.5f); internal int Calls; internal bool Fail; internal Action? DuringMeasure;
        protected override Vector2 OnGetContentsMinimumSize() { Calls++; DuringMeasure?.Invoke(); if (Fail) throw new InvalidOperationException("content failure"); return Content; }
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new Rect2(0, 0, 32, 24), Colors.Green); }
}
