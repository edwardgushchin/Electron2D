using Electron2D;

internal static class MenuBarTests
{
    internal static void Run()
    {
        using var empty = new MenuBar();
        Check(!empty.Flat && empty.SwitchOnHover && empty.Language == "" && empty.TextDirection == TextDirection.Auto && empty.FocusMode == FocusMode.Accessibility && empty.GetMenuCount() == 0 && !empty.IsNativeMenu(), "Menu strip defaults.");
        Reject<ArgumentOutOfRangeException>(() => empty.GetMenuPopup(0)); Reject<ArgumentOutOfRangeException>(() => empty.TextDirection = (TextDirection)99); Reject<ArgumentException>(() => empty.Language = "en\0");
        var root = new SubViewport { Size = new(640, 380), GUIEmbedSubwindows = true };
        var bar = new MenuBar { Name = "Bar", FocusMode = FocusMode.All, Position = new(20, 20), Size = new(320, 40) };
        var file = new PopupMenu { Name = "File" }; file.AddItem("Disabled"); file.SetItemDisabled(0, true); file.AddSeparator("Group"); file.AddItem("Open", 42);
        var edit = new PopupMenu { Name = "Edit" }; edit.AddItem("Copy", 84);
        var internalPopup = new PopupMenu { Name = "Internal" }; bar.AddChild(internalPopup, Node.InternalMode.Front); bar.AddChild(new Node { Name = "Neutral" }); bar.AddChild(file); bar.AddChild(edit); root.AddChild(bar);
        using var tree = new SceneTree(root); tree.ProcessFrame(.01);
        Check(bar.GetMenuCount() == 2 && bar.GetMenuPopup(0) == file && bar.GetMenuPopup(1) == edit && file.Parent == bar, "Only direct ordinary popup children participate without reparenting.");
        bar.SetMenuTooltip(0, "File commands"); bar.SetMenuTooltip(1, "Edit commands");
        Check(bar.GetTooltip(LocalPoint(bar, 0)) == "File commands", "Per-menu tooltip lookup.");
        var selected = 0; file.IDPressed += id => selected = id; edit.IDPressed += id => selected = id;
        bar.GrabFocus(); Action(root, "ui_accept"); Check(file.Visible && file.GetFocusedItem() == 2, $"Keyboard opens first eligible command: file={file.Visible}/{file.GetFocusedItem()} edit={edit.Visible} barFocus={bar.HasFocus()}.");
        Action(root, "ui_right"); Check(!file.Visible && edit.Visible && edit.GetFocusedItem() == 0, "Open popup forwards horizontal navigation to its menu strip.");
        Action(root, "ui_accept"); Check(selected == 84 && !edit.Visible, "Command execution closes menu.");
        Click(root, Point(bar, 0)); Check(file.Visible && file.GetFocusedItem() == -1, "Pointer opens without item focus.");
        Motion(root, Point(bar, 1)); tree.ProcessFrame(.01); Check(edit.Visible && !file.Visible && edit.GetFocusedItem() == -1, $"Hovered peer replaces open menu: file={file.Visible} edit={edit.Visible} hovered={root.GUIGetHoveredControl()?.Name} point={LocalPoint(bar, 1)}."); edit.Hide();
        bar.SwitchOnHover = false; Click(root, Point(bar, 0)); Motion(root, Point(bar, 1)); tree.ProcessFrame(.01); Check(file.Visible && !edit.Visible, "Live hover gate."); file.Hide(); bar.SwitchOnHover = true;
        bar.SetMenuDisabled(1, true); bar.GrabFocus(); Action(root, "ui_right"); Check(file.Visible && !edit.Visible, "Navigation skips disabled headers."); file.Hide(); bar.SetMenuHidden(0, true); Action(root, "ui_right"); Check(!file.Visible && !edit.Visible, "No eligible headers terminate navigation.");
        bar.SetMenuHidden(0, false); bar.SetMenuDisabled(1, false);
        using var key = new InputEventKey { Keycode = Electron2D.Key.O }; using var shortcut = new Shortcut { Events = [key] }; file.SetItemShortcut(2, shortcut);
        selected = 0; Key(root, Electron2D.Key.O); Check(selected == 42 && !file.Visible, "Closed menu shortcuts execute.");
        bar.SetDisableShortcuts(true); selected = 0; Key(root, Electron2D.Key.O); Check(selected == 0, "Owner shortcut disable."); bar.SetDisableShortcuts(false);
        bar.SetMenuDisabled(0, true); Key(root, Electron2D.Key.O); Check(selected == 0, "Disabled menu gates shortcuts."); bar.SetMenuDisabled(0, false); bar.SetMenuHidden(0, true); Key(root, Electron2D.Key.O); Check(selected == 0, "Hidden menu gates shortcuts."); bar.SetMenuHidden(0, false);
        file.SetItemShortcutDisabled(2, true); file.AddShortcut(shortcut, 43, allowEcho: true); Key(root, Electron2D.Key.O, true); Check(selected == 43, "Per-command echo policy.");
        bar.SetMenuTitle(0, "Custom"); file.Name = "Renamed"; file.Title = "File title"; Check(bar.GetMenuTitle(0) == "Custom", "Explicit title survives child name/title edits."); bar.SetMenuTitle(0, file.Title); file.Title = "Current title"; Check(bar.GetMenuTitle(0) == "Current title", "Default title assignment resumes automatic naming.");
        bar.MoveChild(edit, 0); Check(bar.GetMenuPopup(0) == edit && bar.GetMenuTooltip(1) == "File commands", "Reorder retains identity/configuration."); bar.MoveChild(file, 0);
        bar.LayoutDirection = LayoutDirection.RTL; Click(root, Point(bar, 0)); Check(file.Visible && file.IsLayoutRTL(), "RTL hit test and popup layout."); var original = file.Position; file.Hide(); root.CanvasTransform = new Transform(0, new Vector2(-40, 15)); Click(root, Point(bar, 0)); Check(file.Position == original + new Vector2i(-40, 15), $"Canvas anchor: original={original} current={file.Position} visible={file.Visible} size={file.Size} transform={bar.GetGlobalTransformWithCanvas()}."); file.Hide(); root.CanvasTransform = Transform.Identity;
        bar.LayoutDirection = LayoutDirection.LTR; Click(root, Point(bar, 0)); bar.SetMenuDisabled(0, true); Check(!file.Visible, "Disabling active menu closes it."); bar.SetMenuDisabled(0, false); Click(root, Point(bar, 0)); bar.Hide(); Check(!file.Visible, "Owner hide closes popups."); bar.Show();
        Reject<InvalidOperationException>(() => Task.Run(() => bar.GetMenuCount()).GetAwaiter().GetResult());
        var retained = bar.GetMenuPopup(0); bar.RemoveChild(retained); Check(!retained.IsDisposed && bar.GetMenuCount() == 1, "Removal detaches subscriptions and retains caller lifetime."); retained.Name = "Detached"; retained.Dispose();
        Packing(); PrunedPacking(); FailureCleanup(); Nested(); ShapingGuard();
        Console.WriteLine("MenuBar defaults, identity, naming, keyboard/pointer/hover, disabled/hidden/echo shortcuts, RTL/canvas, ownership, failures and fresh scenes passed.");
    }
    internal static Vector2 LocalPoint(MenuBar bar, int index)
    {
        var x = 0f; var size = Vector2.Zero; var count = 0;
        for (var i = 0; i <= index; i++)
        {
            if (bar.IsMenuHidden(i)) continue;
            if (count++ > 0) x += bar.GetThemeConstant("h_separation");
            size = bar.GetThemeFont("font")!.GetStringSize(bar.GetMenuTitle(i), fontSize: bar.GetThemeFontSize("font_size")) + bar.GetThemeStyleBox("normal")!.GetMinimumSize();
            if (i != index) x += size.X;
        }
        return new(bar.IsLayoutRTL() ? bar.Size.X - x - size.X / 2 : x + size.X / 2, size.Y / 2);
    }
    internal static Vector2 Point(MenuBar bar, int index) => bar.GetGlobalTransformWithCanvas() * LocalPoint(bar, index);
    private static void Packing()
    {
        using var bar = new MenuBar { Name = "Stored", Flat = true, Language = "ru", SwitchOnHover = false, TextDirection = TextDirection.Inherited };
        var file = new PopupMenu { Name = "File" }; file.AddItem("Open", 71); var edit = new PopupMenu { Name = "Edit" }; edit.AddItem("Copy"); bar.AddChild(file); bar.AddChild(edit); file.Owner = bar; edit.Owner = bar;
        bar.SetMenuTitle(0, "Commands"); bar.SetMenuTooltip(0, "Help"); bar.SetMenuDisabled(1, true); bar.SetMenuHidden(1, true);
        using var scene = new PackedScene(); scene.Pack(bar); using var copy = (MenuBar)scene.Instantiate(); CheckStored(copy); copy.GetMenuPopup(0).Name = "New"; Check(copy.GetMenuTitle(0) == "Commands", "Stored title override survives rename.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-menubar-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path); var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(MenuBarTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_MENU_BAR"); start.Environment["ELECTRON2D_TEST_MENU_BAR_CHILD"] = path;
            using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Menu bar fresh process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh menu bar passed"), "Fresh scene: " + error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
    }
    private static void PrunedPacking()
    {
        using var bar = new MenuBar { Name = "Pruned" }; var excluded = new PopupMenu { Name = "Excluded" }; var stored = new PopupMenu { Name = "Included" }; stored.AddItem("Command"); bar.AddChild(excluded); bar.AddChild(stored); stored.Owner = bar;
        bar.SetMenuTitle(0, "Drop"); bar.SetMenuTitle(1, "Keep"); bar.SetMenuTooltip(1, "Stored help"); bar.SetMenuDisabled(1, true);
        using var scene = new PackedScene(); scene.Pack(bar); using var copy = (MenuBar)scene.Instantiate(); Check(copy.GetMenuCount() == 1 && copy.GetMenuTitle(0) == "Keep" && copy.GetMenuTooltip(0) == "Stored help" && copy.IsMenuDisabled(0), "Pruned siblings do not shift stored header settings.");
    }
    private static void CheckStored(MenuBar bar) => Check(bar.Flat && !bar.SwitchOnHover && bar.Language == "ru" && bar.TextDirection == TextDirection.Inherited && bar.GetMenuCount() == 2 && bar.GetMenuTitle(0) == "Commands" && bar.GetMenuTooltip(0) == "Help" && bar.IsMenuDisabled(1) && bar.IsMenuHidden(1) && bar.GetMenuPopup(0).GetItemID(0) == 71, "Exact stored menu/child records.");
    internal static void RunChild(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var bar = (MenuBar)scene.Instantiate(); CheckStored(bar);
        var root = new SubViewport { Size = new(500, 260), GUIEmbedSubwindows = true }; bar.FocusMode = FocusMode.All; root.AddChild(bar); using var tree = new SceneTree(root); tree.ProcessFrame(.01); var selected = 0; bar.GetMenuPopup(0).IDPressed += id => selected = id; bar.GrabFocus(); Action(root, "ui_accept"); Action(root, "ui_accept"); Check(selected == 71, "Fresh scene executes command through restored strip."); Console.WriteLine("Fresh menu bar passed");
    }
    private static void FailureCleanup()
    {
        var root = new SubViewport { Size = new(500, 260), GUIEmbedSubwindows = true }; var bar = new Probe { FocusMode = FocusMode.All, Size = new(200, 40) }; var a = new PopupMenu { Name = "A" }; var b = new PopupMenu { Name = "B" }; a.AddItem("A"); b.AddItem("B"); bar.AddChild(a); bar.AddChild(b); root.AddChild(bar); using var tree = new SceneTree(root); tree.ProcessFrame(.01);
        Click(root, Point(bar, 0)); System.Action failed = () => throw new InvalidOperationException("Hide observer"); a.PopupHide += failed;
        Reject<AggregateException>(() => { bar.GrabFocus(); Action(root, "ui_right"); }); Check(!a.Visible && b.Visible, "Failed hide observer still presents the next menu."); a.PopupHide -= failed; b.Hide();
        Click(root, Point(bar, 0)); a.PopupHide += failed; Reject<AggregateException>(() => bar.SetMenuHidden(0, true)); Check(!a.Visible && bar.IsMenuHidden(0), "Failed hide commits eligibility and redraw/minimum."); a.PopupHide -= failed; bar.SetMenuHidden(0, false);
        System.Action reenter = () => { using var input = new InputEventAction { Action = "ui_accept", Pressed = true }; bar.Send(input); };
        a.AboutToPopup += reenter; Reject<AggregateException>(() => Click(root, Point(bar, 0))); Check(!a.Visible, "Reentrant presentation aborts and clears opening guard."); a.AboutToPopup -= reenter;
        System.Action detach = () => bar.RemoveChild(a); a.AboutToPopup += detach; Click(root, Point(bar, 0)); Check(bar.GetMenuCount() == 1 && !a.Visible && !a.IsDisposed, "AboutToPopup removal preserves lifetime and skips detached presentation."); a.Dispose();
        Click(root, Point(bar, 0)); b.PopupHide += failed; Reject<AggregateException>(() => bar.RemoveChild(b)); Check(bar.GetMenuCount() == 0 && !b.IsDisposed && !b.Visible, "Active removal completes membership before callback failure."); b.PopupHide -= failed; b.Dispose();
    }
    private static void Nested()
    {
        var root = new SubViewport { Size = new(800, 500), GUIEmbedSubwindows = true }; var window = new Window { Position = new(100, 90), Size = new(400, 260), Visible = true, GUIEmbedSubwindows = true }; var bar = new MenuBar { FocusMode = FocusMode.All, Position = new(25, 30), Size = new(200, 40) }; var popup = new PopupMenu { Name = "File" }; popup.AddItem("Open"); bar.AddChild(popup); window.AddChild(bar); root.AddChild(window); using var tree = new SceneTree(root); tree.ProcessFrame(.01); bar.GrabFocus(); Action(root, "ui_accept"); Check(popup.Visible && popup.Position.X < 100, "Nested window owns popup anchor once."); var position = popup.Position; popup.Hide(); window.CanvasTransform = new Transform(0, new Vector2(45, 15)); bar.GrabFocus(); Action(root, "ui_accept"); Check(popup.Position == position + new Vector2i(45, 15), "Nested canvas anchor.");
    }
    private static void ShapingGuard()
    {
        using var bar = new MenuBar { TranslationDomain = "menu_bar_validation" }; var popup = new PopupMenu { Name = "File" }; bar.AddChild(popup);
        using var translation = new HookTranslation { Locale = "en" }; var domain = TranslationServer.GetOrAddDomain("menu_bar_validation"); domain.LocaleOverride = "en"; domain.AddTranslation(translation); var count = 0;
        try
        {
            translation.During = () => { count++; Reject<InvalidOperationException>(() => bar.SetMenuTitle(0, "Forbidden")); Reject<InvalidOperationException>(bar.Dispose); var property = (PropertyDescriptor<PopupMenu, string?>)popup.GetPropertyList().Single(p => p.Name == "_menu_bar/title"); Reject<InvalidOperationException>(() => property.SetValue(popup, "Forbidden")); };
            bar.SetMenuTitle(0, "Shape"); _ = bar.GetMinimumSize(); Check(count > 0 && bar.GetMenuTitle(0) == "Shape" && !bar.IsDisposed, "Shaping callback cannot mutate/dispose the strip.");
        }
        finally { translation.During = null; domain.RemoveTranslation(translation); }
    }
    private sealed class HookTranslation : Translation
    {
        internal System.Action? During;
        protected override string? OnGetMessage(string source, string context) { During?.Invoke(); return source; }
    }
    private sealed class Probe : MenuBar { internal void Send(InputEvent input) => OnGUIInput(input); }
    private static void Click(Viewport root, Vector2 point) { using var down = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true }; using var up = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left }; root.PushInput(down, true); root.PushInput(up, true); }
    private static void Motion(Viewport root, Vector2 point) { using var move = new InputEventMouseMotion { Position = point, Relative = new(1, 1) }; root.PushInput(move, true); }
    private static void Action(Viewport root, string action) { using var down = new InputEventAction { Action = action, Pressed = true }; using var up = new InputEventAction { Action = action }; root.PushInput(down, true); root.PushInput(up, true); }
    private static void Key(Viewport root, Electron2D.Key key, bool echo = false) { using var input = new InputEventKey { Keycode = key, Pressed = true, Echo = echo }; root.PushInput(input, true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(System.Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
