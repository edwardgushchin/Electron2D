using Electron2D;

internal static class PopupMenuTests
{
    internal static void Run()
    {
        Edges();
        using var menu = new PopupMenu(); Check(menu.ItemCount == 0 && menu.GetFocusedItem() == -1 && menu.AllowSearch && menu.HideOnItemSelection && menu.HideOnCheckableItemSelection && !menu.HideOnStateItemSelection && menu.ShrinkWidth && menu.ShrinkHeight, "Menu defaults.");
        var changed = 0; menu.MenuChanged += () => changed++;
        menu.AddItem("Open", 42, Key.O); menu.AddCheckItem("Check"); menu.AddRadioCheckItem("Radio"); menu.AddSeparator("Group"); menu.AddMultistateItem("State", 3);
        Check(menu.ItemCount == 5 && menu.GetItemID(1) == 1 && menu.IsItemCheckable(2) && menu.IsItemRadioCheckable(2) && changed == 5, "Creation families and IDs.");
        menu.SetItemText(-1, "Last"); Reject<ArgumentOutOfRangeException>(() => menu.GetItemText(-1)); Check(menu.GetItemText(4) == "Last", "Setter end-relative index and strict getter.");
        menu.SetItemMetadata<string?>(0, null); Check(menu.GetItemMetadata<string?>(0) == null, "Typed null metadata."); Reject<KeyNotFoundException>(() => menu.GetItemMetadata<int>(0));
        menu.ToggleItemChecked(1); menu.ToggleItemMultistate(4); Check(menu.IsItemChecked(1) && menu.GetItemMultistate(4) == 1, "Check/state setters are explicit."); menu.SetItemIndex(0, 4); Check(menu.GetItemID(4) == 42, "Reordering preserves explicit ID.");
        var order = new List<string>(); menu.IDPressed += id => order.Add("id:" + id); menu.IndexPressed += index => order.Add("index:" + index);
        using (var key = new InputEventKey { Keycode = Key.O, Pressed = true }) Check(menu.ActivateItemByEvent(key, true) && order.SequenceEqual(new[] { "id:42", "index:4" }), "Accelerators match and emit ID before index.");
        using var input = new InputEventKey { Keycode = Key.P }; using var shortcut = new Shortcut { ResourceName = "Print", Events = [input] }; menu.AddShortcut(shortcut, 77, true); menu.SetItemShortcutDisabled(5, true); using (var press = new InputEventKey { Keycode = Key.P, Pressed = true }) Check(!menu.ActivateItemByEvent(press), "Shortcut disabling."); menu.SetItemShortcutDisabled(5, false); using (var press = new InputEventKey { Keycode = Key.P, Pressed = true }) Check(menu.ActivateItemByEvent(press, true), "Borrowed shortcut matching.");
        using var root = new SubViewport { Size = new(400, 240), GUIEmbedSubwindows = true }; var live = new PopupMenu { Name = "Menu" }; live.AddItem("Alpha", 10); live.AddSeparator(); live.AddCheckItem("Beta", 20); live.AddItem("Gamma", 30); root.AddChild(live); live.Owner = root;
        var child = new PopupMenu { Name = "Child" }; child.AddItem("Nested", 50); live.AddSubmenuNodeItem("More", child, 40); child.Owner = root;
        using var tree = new SceneTree(root); live.Popup(new(new(20, 20), new(200, 120))); tree.ProcessFrame(.01); Check(live.Visible && live.Size.X > 40 && live.Size.Y > 40, "Menu popup has measured content.");
        ActionKey(root, "ui_down"); Check(live.GetFocusedItem() == 0, "Keyboard starts at first enabled item."); ActionKey(root, "ui_down"); Check(live.GetFocusedItem() == 2, "Navigation skips separator."); live.HideOnCheckableItemSelection = false; ActionKey(root, "ui_accept"); Check(live.Visible && !live.IsItemChecked(2), "Activation doesn't toggle check state.");
        live.SetFocusedItem(4); ActionKey(root, "ui_right"); tree.ProcessFrame(.01); Check(child.Visible && child.GetFocusedItem() == 0, "Keyboard opens and focuses submenu."); ActionKey(root, "ui_left"); Check(!child.Visible && live.Visible, "Left returns to parent menu.");
        live.SetFocusedItem(3); var ids = new List<int>(); live.IDPressed += ids.Add; ActionKey(root, "ui_accept"); Check(!live.Visible && ids.SequenceEqual(new[] { 30 }), "Ordinary activation hides before ID delivery.");
        live.SearchBarEnabled = true; live.SearchBarFuzzySearchEnabled = false; live.Popup(); tree.ProcessFrame(.01); var search = (LineEdit)live.GetChild(1, true); tree.DispatchCommittedText(live, "Bet"); tree.ProcessFrame(.01); ActionKey(root, "ui_down"); Check(live.GetFocusedItem() == 2, "Real LineEdit search filters menu navigation.");
        live.Hide(); using var packed = new PackedScene(); packed.Pack(root); using var clone = (SubViewport)packed.Instantiate(); var restored = (PopupMenu)clone.GetChild(0); Check(restored.ItemCount == 5 && restored.GetItemID(2) == 20 && restored.IsItemCheckable(2), "Menu indexed scene schema.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-menu-" + Guid.NewGuid() + ".e2dscene"); ResourceSaver.Save(packed, path); try { FreshProcess(path); } finally { File.Delete(path); }
        Reject<ArgumentOutOfRangeException>(() => live.ItemCount = -1); Reject<ArgumentOutOfRangeException>(() => live.SubmenuPopupDelay = double.NaN); Reject<ArgumentOutOfRangeException>(() => live.SearchBarFuzzySearchMaxMisses = -1);
        Console.WriteLine("PopupMenu model, typed metadata, shortcuts, selection, navigation, submenus, search and packing passed.");
    }


    private static void Edges()
    {
        var root = new SubViewport { Size = new(360, 150), GUIEmbedSubwindows = true }; var menu = new PopupMenu { Name = "Edges", HideOnItemSelection = false }; root.AddChild(menu);
        menu.AddItem("First"); menu.AddItem("Second"); menu.AddItem("Third"); using var tree = new SceneTree(root); menu.Popup(); tree.ProcessFrame(.01);
        var scroll = (ScrollContainer)menu.GetChild(2, true); var view = (Control)scroll.GetChild(0); var row = view.Size.Y / 3;
        Vector2 Point(int index) => (Vector2)menu.Position + view.GetGlobalRect().Position + new Vector2(8, row * (index + .5f));
        var pressed = new List<int>(); menu.IndexPressed += pressed.Add;
        Click(root, Point(1)); Check(pressed.SequenceEqual(new[] { 1 }), "Routed pointer activation."); pressed.Clear();
        using (var down = new InputEventMouseButton { Position = Point(0), ButtonIndex = MouseButton.Left, Pressed = true }) root.PushInput(down, true);
        using (var up = new InputEventMouseButton { Position = Point(2), ButtonIndex = MouseButton.Left }) root.PushInput(up, true);
        Check(pressed.Count == 0, "Press/release on different records cancels.");
        using (var down = new InputEventScreenTouch { Position = Point(1), Index = 0, Pressed = true }) root.PushInput(down, true);
        using (var up = new InputEventScreenTouch { Position = Point(1), Index = 0 }) root.PushInput(up, true);
        Check(pressed.SequenceEqual(new[] { 1 }), "Routed touch activation."); pressed.Clear();
        using var icon = new ImageTexture(); menu.SetItemIcon(0, icon); menu.SetItemIcon(1, icon); Task.Run(icon.Dispose).GetAwaiter().GetResult(); tree.ProcessFrame(.01); Check(menu.GetItemIcon(0) == null && menu.GetItemIcon(1) == null, "Worker disposal clears shared borrowed icons on owner thread.");
        Reject<InvalidOperationException>(() => Task.Run(() => menu.SetItemText(0, "worker")).GetAwaiter().GetResult());
        var child = new PopupMenu { Name = "Nested" }; child.AddItem("Child"); menu.AddSubmenuNodeItem("More", child); menu.SubmenuPopupDelay = .2; menu.Hide(); menu.Popup(); tree.ProcessFrame(.01); row = view.Size.Y / 4;
        using (var motion = new InputEventMouseMotion { Position = Point(3), Relative = new(1, 0) }) root.PushInput(motion, true);
        tree.ProcessFrame(.1); Check(!child.Visible, "Hover waits the submenu timer."); tree.ProcessFrame(.11); Check(child.Visible, "Hover submenu executes its owned timer.");
        child.Hide(); menu.Hide(); menu.ItemCount = 30; for (var i = 0; i < 30; i++) menu.SetItemText(i, "Entry " + i); menu.Popup(); tree.ProcessFrame(.01); menu.ScrollToItem(29); tree.ProcessFrame(.01); Check(scroll.ScrollVertical > 0 && scroll.GetVScrollBar().Visible, "Long menu scrolls the selected item.");
        menu.Hide(); menu.Clear(); menu.SearchBarEnabled = true; menu.SearchBarFuzzySearchMaxMisses = 0; menu.AddItem("𐐀😀"); menu.AddItem("Other"); menu.Popup(); tree.ProcessFrame(.01); tree.DispatchCommittedText(menu, "𐐨😀"); ActionKey(root, "ui_down"); Check(menu.GetFocusedItem() == 0, "Fuzzy matching operates on lowercase Unicode scalars.");
        menu.Hide(); menu.Clear(); menu.AddItem("abc"); menu.AddItem("unrelated"); menu.SearchBarFuzzySearchEnabled = false; menu.Popup(); tree.ProcessFrame(.01); tree.DispatchCommittedText(menu, "ac"); ActionKey(root, "ui_down"); Check(menu.GetFocusedItem() == -1, "Exact search rejects split sequences."); menu.SearchBarFuzzySearchEnabled = true; ActionKey(root, "ui_down"); Check(menu.GetFocusedItem() == 0, "Changing fuzzy policy reapplies the current search.");
        menu.Hide(); menu.SearchBarEnabled = false; menu.Clear(); menu.AddItem("One"); menu.AddItem("Two"); menu.AddItem("Three"); menu.Popup(); tree.ProcessFrame(.01);
        using (var joy = new InputEventJoypadButton { ButtonIndex = JoyButton.DpadDown, Device = 28, Pressed = true }) { Input.ParseInputEvent(joy); root.PushInput(joy, true); }
        Check(menu.GetFocusedItem() == 0, "Initial controller navigation."); tree.ProcessFrame(.49); Check(menu.GetFocusedItem() == 0, "Controller repeat delay."); tree.ProcessFrame(.02); Check(menu.GetFocusedItem() == 1, "Controller repeats held navigation.");
        using (var joy = new InputEventJoypadButton { ButtonIndex = JoyButton.DpadDown, Device = 28 }) Input.ParseInputEvent(joy); tree.ProcessFrame(.2); Check(menu.GetFocusedItem() == 1, "Controller release stops repetition.");
        using (var cancel = new InputEventAction { Action = "ui_cancel", Pressed = true }) menu.PushInput(cancel, true); Check(menu.Visible, "Direct popup cancel remains deferred."); tree.ProcessFrame(.01); Check(!menu.Visible, "Direct popup input executes inherited cancellation."); menu.Popup(); menu.SetFocusedItem(1);
        menu.HideOnItemSelection = true; var afterHide = false; Action failed = () => throw new InvalidOperationException("observer"); menu.PopupHide += failed; menu.IDPressed += _ => afterHide = !menu.Visible; Reject<AggregateException>(() => ActionKey(root, "ui_accept")); Check(afterHide && !menu.Visible, "Hide observer failure still delivers committed activation."); menu.PopupHide -= failed;
        using var model = new PopupMenu(); Action failure = () => throw new InvalidOperationException("mutation"); model.MenuChanged += failure; Reject<AggregateException>(() => model.AddItem("committed")); Check(model.ItemCount == 1 && model.GetPropertyList().Any(p => p.Name == "item_0/text"), "Structural observer failure preserves committed indexed descriptors."); model.MenuChanged -= failure;
        var owned = new PopupMenu { Name = "Owned" }; model.AddSubmenuNodeItem("Child", owned); model.AddSubmenuNodeItem("Again", owned); model.Clear(true); Check(owned.IsDisposed && model.ItemCount == 0, "Distinct submenu owners dispose once on clear.");
        Console.WriteLine("PopupMenu routed pointer/touch, hover timer, scroll, scalar search, controller repeat, borrowed lifetime and observer edges passed.");
    }
    private static void Click(Viewport root, Vector2 point) { using var down = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true }; using var up = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left }; root.PushInput(down, true); root.PushInput(up, true); }

    private static void FreshProcess(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(PopupMenuTests).Assembly.Location);
        start.Environment.Remove("ELECTRON2D_TEST_MENU"); start.Environment["ELECTRON2D_TEST_MENU_CHILD"] = path;
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh menu archive process."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh menu archive passed"), "Fresh menu archive: " + error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    { using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var root = (SubViewport)packed.Instantiate(); using var tree = new SceneTree(root); var menu = (PopupMenu)root.GetChild(0); menu.Popup(); tree.ProcessFrame(.01); Check(menu.ItemCount == 5 && menu.IsItemCheckable(2) && menu.GetItemID(2) == 20, "Fresh menu item schemas execute."); ActionKey(root, "ui_down"); Check(menu.GetFocusedItem() == 0, "Fresh menu keyboard consumer executes."); menu.Hide(); Console.WriteLine("Fresh menu archive passed"); }
    private static void ActionKey(Viewport root, string action) { using var input = new InputEventAction { Action = action, Pressed = true }; root.PushInput(input, true); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
