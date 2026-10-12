using Electron2D;

internal static class WindowContentScaleTests
{
    internal static void Run()
    {
        using var window = new Window { Size = new(240, 160) };
        Check(window.ContentScaleMode == ContentScaleMode.Disabled && window.ContentScaleAspect == ContentScaleAspect.Ignore && window.ContentScaleStretch == ContentScaleStretch.Fractional && window.ContentScaleSize == Vector2i.Zero && window.ContentScaleFactor == 1, "Content defaults.");
        window.ContentScaleSize = new(80, 80);
        var logical = new[] { new Vector2(80, 80), new(80, 80), new(80, 80), new(120, 80), new(120, 80) };
        var screen = new[] { new Rect2i(0, 0, 240, 160), new(40, 0, 160, 160), new(40, 0, 160, 160), new(0, 0, 240, 160), new(0, 0, 240, 160) };
        foreach (var mode in new[] { ContentScaleMode.CanvasItems, ContentScaleMode.Viewport })
        {
            window.ContentScaleMode = mode;
            for (var i = 0; i < 5; i++)
            {
                window.ContentScaleAspect = (ContentScaleAspect)i;
                Check(window.GetVisibleRect().Size == logical[i] && window.ContentLayout.ScreenRect == screen[i], "Wide-client aspect policies.");
                Check(window.GetTexture().GetSize() == (mode == ContentScaleMode.CanvasItems ? (Vector2)screen[i].Size : logical[i]), "Canvas and viewport raster sizes differ.");
                var point = window.GetFinalTransform() * new Vector2(10, 10);
                var scale = (Vector2)screen[i].Size / logical[i];
                Check(point == (Vector2)screen[i].Position + new Vector2(10, 10) * scale && window.GetScreenTransform() == window.GetFinalTransform(), "Public client transform includes fitting and scale.");
            }
        }
        window.Size = new(160, 240);
        window.ContentScaleAspect = ContentScaleAspect.KeepWidth;
        Check(window.GetVisibleRect().Size == new Vector2(80, 120), "KeepWidth expands a taller logical area.");
        window.ContentScaleAspect = ContentScaleAspect.KeepHeight;
        Check(window.GetVisibleRect().Size == new Vector2(80, 80) && window.ContentLayout.ScreenRect == new Rect2i(0, 40, 160, 160), "KeepHeight fits a tall client with margins.");
        window.Size = new(241, 161); window.ContentScaleAspect = ContentScaleAspect.Keep; window.ContentScaleStretch = ContentScaleStretch.Integer;
        Check(window.ContentLayout.ScreenRect == new Rect2i(41, 1, 160, 160), "Integer fitting uses whole scale and rounds half margins away from zero.");
        window.ContentScaleFactor = 2.8f;
        Check(window.ContentScaleFactor == 2 && window.GetVisibleRect().Size == new Vector2(40, 40), "Integer factor floors and changes viewport raster size.");
        window.ContentScaleFactor = .8f; Check(window.ContentScaleFactor == 1, "Integer factor clamps to one.");
        window.Size = new(32, 24);
        Check(window.ContentLayout.ScreenRect == new Rect2i(0, 0, 80, 80), "Small integer clients clip rather than downscale.");
        window.ContentScaleStretch = ContentScaleStretch.Fractional; window.ContentScaleFactor = 1000;
        Check(window.GetVisibleRect().Size == Vector2.Zero && window.GetTexture().GetSize() == Vector2.Zero, "A zero-sized logical raster remains observable.");
        window.ContentScaleMode = ContentScaleMode.Disabled; window.ContentScaleFactor = 2;
        Check(window.GetVisibleRect().Size == new Vector2(16, 12) && window.GetTexture().GetSize() == new Vector2(32, 24), "Disabled resolution fitting still executes the scale factor.");
        window.ContentScaleSize = new(0, 80); window.ContentScaleMode = ContentScaleMode.Viewport;
        Check(window.GetVisibleRect().Size == new Vector2(16, 12), "Either zero desired axis disables fitting.");
        var before = window.ContentLayout;
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleFactor = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleFactor = 0);
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleFactor = -1);
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleSize = new(-1, 0));
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleMode = (ContentScaleMode)3);
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleAspect = (ContentScaleAspect)5);
        Reject<ArgumentOutOfRangeException>(() => window.ContentScaleStretch = (ContentScaleStretch)2);
        Check(window.ContentLayout == before, "Invalid writes preserve all calculated state.");
        window.ContentScaleFactor = 1; window.ContentScaleSize = new(80, 80); window.ContentScaleMode = ContentScaleMode.CanvasItems; window.ContentScaleAspect = ContentScaleAspect.Expand;
        using var packed = new PackedScene(); packed.Pack(window); using var copy = (Window)packed.Instantiate();
        Check(copy.Size == window.Size && copy.ContentScaleSize == window.ContentScaleSize && copy.ContentScaleMode == window.ContentScaleMode && copy.ContentScaleAspect == window.ContentScaleAspect && copy.ContentLayout == window.ContentLayout, "Scene packing restores executable content policy.");
        var allocations = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { _ = window.GetFinalTransform(); _ = window.GetVisibleRect(); _ = window.ContentLayout; }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocations, "Warmed content queries allocate no managed bytes.");
        var notifications = 0; window.SizeChanged += () => notifications++; window.ContentScaleFactor = window.ContentScaleFactor;
        Check(notifications == 0, "Equal writes are silent.");
        window.SizeChanged += () => throw new InvalidOperationException("observer");
        Reject<AggregateException>(() => window.ContentScaleFactor = 2);
        Check(window.ContentScaleFactor == 2 && notifications == 1, "Observer failure leaves committed policy and delivers other observers.");
        using var disposed = new Window(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.ContentScaleFactor = 2);
        GUIRouting();
        Console.WriteLine("Window content policy, aspect/rounding, validation, packing and zero-allocation queries passed.");
    }
    private static void GUIRouting()
    {
        using var root = new SubViewport { Size = new(240, 160), GUIEmbedSubwindows = true };
        var popup = new Window { Size = new(160, 80), Position = new(20, 20), Borderless = true, ContentScaleSize = new(80, 40), ContentScaleMode = ContentScaleMode.Viewport };
        var control = new Control { MouseFilter = MouseFilter.Stop, LayoutDirection = LayoutDirection.LTR };
        control.SetBegin(new(10, 10)); control.SetEnd(new(20, 20)); popup.AddChild(control); root.AddChild(popup);
        using var tree = new SceneTree(root); var count = 0; var point = Vector2.Zero;
        control.GUIInput += input => { if (input is InputEventMouseButton { Pressed: true } mouse) { count++; point = mouse.Position; } };
        using var click = new InputEventMouseButton { Position = new(50, 50), GlobalPosition = new(50, 50), ButtonIndex = MouseButton.Left, Pressed = true };
        root.PushInput(click, true); click.Pressed = false; root.PushInput(click, true);
        Check(count == 1 && point == new Vector2(5, 5), "Embedded input localizes through the content transform before GUI hit testing.");
        Task.Run(() => Reject<InvalidOperationException>(() => popup.ContentScaleFactor = 2)).GetAwaiter().GetResult();
        popup.ContentScaleMode = ContentScaleMode.CanvasItems; popup.ContentScaleFactor = 2;
        using var layer = new CanvasLayer { FollowViewportEnabled = true, FollowViewportScale = 2 };
        Check(popup.GetCanvasRenderTransform(layer).Origin == new Vector2(-80, -40), "Follow scaling pivots on the raster center, rather than the smaller logical center.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
