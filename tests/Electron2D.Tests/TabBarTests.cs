using Electron2D;

internal static class TabBarTests
{
    internal static void Run()
    {
        Model(); Layout(); InputEvents(); Transfers(); Packing(); Lifetime(); Hover();
        Console.WriteLine("TabBar model, selection, layout/RTL, input/close, transfer, packing and lifetime passed.");
    }
    private static void Model()
    {
        using var bar = new TabBar(); Check(bar.TabCount == 0 && bar.CurrentTab == -1 && bar.GetPreviousTab() == -1 && bar.FocusMode == FocusMode.All && bar.GetChildCount(true) == 1 && bar.ChildCount == 0, "Default state and owned internal timer.");
        var events = new List<string>(); bar.TabSelected += i => events.Add("selected:" + i); bar.TabChanged += i => events.Add("changed:" + i);
        bar.AddTab("A"); bar.AddTab("B"); bar.AddTab("C"); Check(bar.CurrentTab == 0 && events.Count == 0, "First detached addition selects silently.");
        bar.CurrentTab = 1; bar.CurrentTab = 1; Check(events.SequenceEqual(["selected:1", "changed:1", "selected:1"]) && bar.GetPreviousTab() == 1, "Selected/changed ordering and equal assignment.");
        Reject<InvalidOperationException>(() => bar.CurrentTab = -1); bar.DeselectEnabled = true; bar.CurrentTab = -1; Check(bar.SelectNextAvailable() && bar.CurrentTab == 0, "Deselect and forward selection from none.");
        bar.SetTabDisabled(0, true); bar.SetTabHidden(1, true); Check(bar.CurrentTab == 0 && bar.SelectNextAvailable() && bar.CurrentTab == 2 && !bar.SelectNextAvailable(), "Unavailable state retains current, navigation skips and does not wrap.");
        Check(!bar.SelectPreviousAvailable(), "Unavailable previous entries are skipped."); bar.DeselectEnabled = false; bar.CurrentTab = 1; Check(bar.CurrentTab == 1, "Programmatic hidden/disabled selection remains valid.");
        bar.SetTabMetadata(1, "payload"); bar.MoveTab(1, 2); Check(bar.CurrentTab == 2 && bar.GetTabMetadata<string>(2) == "payload", "Move retains current identity and typed payload."); Reject<KeyNotFoundException>(() => bar.GetTabMetadata<int>(2));
        bar.TabCount = 1; Check(bar.CurrentTab == 0, "Count truncation clamps without selected events."); bar.ClearTabs(); Check(bar.CurrentTab == -1 && bar.GetPreviousTab() == -1 && bar.GetTabOffset() == 0, "Clear resets indices and page.");
        using var queued = new TabBar { CurrentTab = 2 }; queued.TabCount = 3; Check(queued.CurrentTab == 2 && queued.GetPreviousTab() == 0, "Initial current index resolves after count.");
        Reject<ArgumentOutOfRangeException>(() => queued.TabCount = -1); Reject<ArgumentOutOfRangeException>(() => queued.MaxTabWidth = -1); Reject<ArgumentOutOfRangeException>(() => queued.TabAlignment = TabBar.AlignmentMode.Max); Reject<ArgumentOutOfRangeException>(() => queued.TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.Max);
        queued.SetTabLanguage(0, "ar"); queued.SetTabTextDirection(0, TextDirection.RTL); queued.SetTabTooltip(0, "details"); queued.SetTabIconMaxWidth(0, 9); Check(queued.GetTabLanguage(0) == "ar" && queued.GetTabTextDirection(0) == TextDirection.RTL && queued.GetTabTooltip(0) == "details" && queued.GetTabIconMaxWidth(0) == 9, "All per-tab scalar configuration.");
        Action<int> nested = i => { if (i == 1) queued.CurrentTab = 0; }; var changes = new List<int>(); queued.TabSelected += nested; queued.TabChanged += changes.Add; queued.CurrentTab = 1; Check(queued.CurrentTab == 0 && changes.SequenceEqual([0]), "Nested selection suppresses stale outer change."); queued.TabSelected -= nested;
        Action<int> fail = _ => throw new InvalidOperationException("observer"); queued.TabSelected += fail; Reject<AggregateException>(() => queued.CurrentTab = 2); Check(queued.CurrentTab == 2 && changes[^1] == 2, "Observer failure follows committed state and remaining change delivery."); queued.TabSelected -= fail;
    }
    private static void Layout()
    {
        var viewport = new TestViewport(); var bar = new TabBar { Size = new(140, 40), CustomMaximumSize = new(140, -1), ScrollToSelected = false };
        for (var i = 0; i < 6; i++) bar.AddTab("Entry " + i); viewport.AddChild(bar); using var tree = new SceneTree(viewport); tree.FlushDeferred();
        Check(bar.GetOffsetButtonsVisible() && bar.GetTabRect(0).Size.X > 20 && bar.GetTabIdxAtPoint(bar.GetTabRect(0).GetCenter()) == 0, "Measured tabs and overflow navigation.");
        bar.EnsureTabVisible(5); Check(bar.GetTabOffset() > 0 && bar.GetTabIdxAtPoint(bar.GetTabRect(5).GetCenter()) == 5, "Explicit reveal reaches a trailing tab."); bar.EnsureTabVisible(0); Check(bar.GetTabOffset() == 0, "Reveal resets the leading page.");
        var ltr = bar.GetTabRect(0); bar.LayoutDirection = LayoutDirection.RTL; var rtl = bar.GetTabRect(0); Check(Math.Abs(rtl.Position.X - (bar.Size.X - ltr.End.X)) < .01f && bar.GetTabIdxAtPoint(rtl.GetCenter()) == 0, "RTL mirrors layout and hit tests.");
        bar.MaxTabWidth = 48; Check(bar.GetTabRect(0).Size.X <= 48.01f && bar.GetTooltip(bar.GetTabRect(0).GetCenter()) == "Entry 0", "Width cap ellipsizes text and supplies title tooltip.");
        bar.SetTabTooltip(0, "override"); Check(bar.GetTooltip(bar.GetTabRect(0).GetCenter()) == "override", "Explicit tooltip wins.");
        using var image = Image.CreateEmpty(24, 8, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var icon = ImageTexture.CreateFromImage(image); bar.SetTabIcon(0, icon); bar.SetTabButtonIcon(0, icon); bar.SetTabIconMaxWidth(0, 6); var before = bar.GetTabRect(0).Size.X; icon.SetSizeOverride(new(40, 8)); Check(bar.GetTabRect(0).Size.X > before, "Shared button/title icon size invalidates measurements."); icon.Dispose(); Check(bar.GetTabIcon(0) is null && bar.GetTabButtonIcon(0) is null, "Disposed borrowed icons clear both roles.");
        bar.ClipTabs = false; Check(!bar.GetOffsetButtonsVisible() && bar.GetTabOffset() == 0 && bar.GetTabRect(5).Position.X < 0, "Unclipped RTL strip retains all tabs and no arrows.");
        bar.LayoutDirection = LayoutDirection.LTR; bar.TabAlignment = TabBar.AlignmentMode.Center; Check(bar.GetTabRect(0).Position.X < 0, "Oversized centered unclipped strip extends symmetrically.");
        Reject<ArgumentException>(() => bar.GetTabIdxAtPoint(new(float.NaN, 0)));
    }
    private static void InputEvents()
    {
        var viewport = new TestViewport(); var bar = new TabBar { Size = new(300, 40), TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowAlways };
        bar.AddTab("One"); bar.AddTab("Two"); viewport.AddChild(bar); using var tree = new SceneTree(viewport); tree.FlushDeferred(); var events = new List<string>();
        bar.TabSelected += i => events.Add("selected:" + i); bar.TabChanged += i => events.Add("changed:" + i); bar.TabClicked += i => events.Add("clicked:" + i); bar.TabRMBClicked += i => events.Add("right:" + i); bar.TabClosePressed += i => events.Add("close:" + i);
        Click(viewport, bar.GetTabRect(1).Position + new Vector2(4, 10), MouseButton.Left); Check(events.Take(3).SequenceEqual(["selected:1", "changed:1", "clicked:1"]), "Pointer selection signals."); events.Clear();
        Click(viewport, bar.GetTabRect(0).Position + new Vector2(4, 10), MouseButton.Right); Check(events.SequenceEqual(["right:0"]) && bar.CurrentTab == 1, "Right click reports without selecting by default."); events.Clear(); bar.SelectWithRMB = true;
        Click(viewport, bar.GetTabRect(0).Position + new Vector2(4, 10), MouseButton.Right); Check(events.SequenceEqual(["selected:0", "changed:0", "clicked:0", "right:0"]), "Enabled right-click selection order."); events.Clear();
        Click(viewport, bar.GetTabRect(1).Position + new Vector2(4, 10), MouseButton.Middle); Check(events.SequenceEqual(["close:1"]) && bar.TabCount == 2, "Middle click requests close without removal."); events.Clear();
        var closePoint = bar.GetTabRect(1).End - new Vector2(10, 20); Click(viewport, closePoint, MouseButton.Left); Check(events.SequenceEqual(["close:1"]), "Close button click emits request only."); events.Clear();
        using var down = new InputEventMouseButton { Position = closePoint, ButtonIndex = MouseButton.Left, Pressed = true }; viewport.PushInput(down, true);
        using var up = new InputEventMouseButton { Position = bar.GetTabRect(0).End - new Vector2(10, 20), ButtonIndex = MouseButton.Left, Pressed = false }; viewport.PushInput(up, true); Check(events.Count == 0, "Release over another close button cancels the original request.");
        bar.SetTabDisabled(1, true); bar.GrabFocus(); using var key = new InputEventAction { Action = "ui_right", Pressed = true }; viewport.PushInput(key, true); Check(bar.CurrentTab == 0, "Navigation skips disabled tabs.");
        bar.SetTabDisabled(1, false); viewport.PushInput(key, true); Check(bar.CurrentTab == 1, "Focused action navigates available tabs.");
    }
    private static void Transfers()
    {
        using var left = new TabBar { DragToRearrangeEnabled = true, TabsRearrangeGroup = 7 }; using var right = new TabBar { DragToRearrangeEnabled = true, TabsRearrangeGroup = 7 };
        left.AddTab("A"); left.AddTab("B"); left.AddTab("C"); left.SetTabMetadata(0, 42); right.AddTab("R"); left.Size = new(400, 40); right.Size = new(400, 40);
        var payload = left.GetDragData(left.GetTabRect(0).GetCenter())!; left.MoveTab(0, 2); Check(right.CanDropData(Vector2.Zero, payload), "Payload follows identity after source reordering."); right.DropData(Vector2.Zero, payload); Check(left.TabCount == 2 && right.GetTabTitle(0) == "A" && right.GetTabMetadata<int>(0) == 42 && right.CurrentTab == 0, "Group transfer moves complete metadata and selects destination.");
        Check(!right.CanDropData(Vector2.Zero, payload), "Removed source payload cannot be reused.");
        payload = right.GetDragData(right.GetTabRect(0).GetCenter())!; var rearranged = -1; right.ActiveTabRearranged += i => rearranged = i; right.DropData(right.GetTabRect(1).End, payload); Check(right.GetTabTitle(1) == "A" && rearranged == 1 && right.CurrentTab == 1, "Same-strip drop requests reorder and selects moved identity.");
        left.TabsRearrangeGroup = -1; Check(!left.CanDropData(Vector2.Zero, right.GetDragData(right.GetTabRect(1).GetCenter())!), "Unmatched transfer group rejects.");
    }
    private static void Packing()
    {
        using var source = new TabBar { DeselectEnabled = true, TabCount = 3, CurrentTab = 2, CloseWithMiddleMouse = false, MaxTabWidth = 90 }; source.SetTabTitle(0, "saved"); source.SetTabTooltip(0, "tip"); source.SetTabDisabled(1, true); source.SetTabMetadata(0, 9);
        using var packed = new PackedScene(); packed.Pack(source); using var copy = (TabBar)packed.Instantiate(); Check(copy.TabCount == 3 && copy.CurrentTab == 2 && copy.GetTabTitle(0) == "saved" && copy.GetTabTooltip(0) == "tip" && copy.IsTabDisabled(1) && !copy.CloseWithMiddleMouse && copy.MaxTabWidth == 90 && copy.ChildCount == 0 && copy.GetChildCount(true) == 1, "Stored count/index/scalar state and independent internal timer."); Reject<KeyNotFoundException>(() => copy.GetTabMetadata<int>(0));
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-tabs-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try { ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var fileCopy = (TabBar)loaded.Instantiate(); Check(fileCopy.GetTabTitle(0) == "saved" && fileCopy.CurrentTab == 2, "Built-in file factory preserves typed tab scene state."); FreshProcess(path); }
        finally { System.IO.File.Delete(path); }
    }
    private static void Lifetime()
    {
        var viewport = new TestViewport(); var bar = new TabBar(); bar.AddTab("A"); viewport.AddChild(bar); using var tree = new SceneTree(viewport);
        Reject<InvalidOperationException>(() => Task.Run(() => bar.SetTabTitle(0, "worker")).GetAwaiter().GetResult());
        var timer = bar.GetChild(0, true); tree.Dispose(); Check(timer.IsDisposed && bar.IsDisposed, "Disposal releases owned timer and list state."); Reject<ObjectDisposedException>(() => bar.GetTabTitle(0));
    }
    private static void Hover()
    {
        var viewport = new TestViewport(); var producer = new Control { Name = "Producer" }; var bar = new TabBar { Size = new(300, 40) }; bar.AddTab("First"); bar.AddTab("Second"); viewport.AddChild(producer); viewport.AddChild(bar); using var tree = new SceneTree(viewport);
        var payload = new DragPayload<int>(42); producer.ForceDrag(payload); var point = bar.GetTabRect(1).GetCenter(); Check(!bar.CanDropData(point, payload), "Foreign data starts hover switching without accepting a tab drop.");
        tree.ProcessFrame(.49); Check(bar.CurrentTab == 0, "Hover delay waits its themed duration."); tree.ProcessFrame(.02); Check(bar.CurrentTab == 1, "Foreign drag switches after the delay.");
        bar.CurrentTab = 0; bar.SetTabDisabled(1, true); bar.CanDropData(point, payload); tree.ProcessFrame(.6); Check(bar.CurrentTab == 0, "Disabled tabs do not activate on hover.");
        viewport.CancelGUIDrag(); bar.SetTabDisabled(1, false); producer.ForceDrag(payload); bar.SwitchOnDragHover = false; bar.CanDropData(point, payload); tree.ProcessFrame(.6); Check(bar.CurrentTab == 0, "Explicit hover switch disable cancels pending selection."); viewport.CancelGUIDrag();
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 700, 300); }
    private static void Click(Viewport viewport, Vector2 point, MouseButton button) { using var down = new InputEventMouseButton { Position = point, ButtonIndex = button, Pressed = true }; using var up = new InputEventMouseButton { Position = point, ButtonIndex = button, Pressed = false }; viewport.PushInput(down, true); viewport.PushInput(up, true); }
    private static void FreshProcess(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TabBarTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_TABS_CHILD"] = path;
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh tab scene process."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh tab scene process passed"), "Fresh tab archive: " + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var bar = (TabBar)loaded.Instantiate(); using var tree = new SceneTree(bar); tree.ProcessFrame(.01);
        Check(bar.GetTabTitle(0) == "saved" && bar.CurrentTab == 2 && bar.IsTabDisabled(1) && bar.GetTabRect(0).Size.X > 0, "Fresh file recreates and runs the concrete tab scene.");
        bar.CurrentTab = 0; Check(bar.SelectNextAvailable() && bar.CurrentTab == 2, "Fresh scene executes selection navigation."); Console.WriteLine("Fresh tab scene process passed");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
