using Electron2D;
using System.Diagnostics;
using Image = Electron2D.Image;

internal static class NativeMenuChecks
{
    private static int _checks, _popup;
    private static string _oracle = "", _output = "";
    internal static void Run(string oracle, string output)
    {
        _oracle = oracle; _output = output; System.IO.Directory.CreateDirectory(output);
        using (var theme = new Theme()) Reject<ArgumentException>(() => theme.SetTypeVariation(nameof(NativeMenu), nameof(ElectronObject)));
        Check(!NativeMenu.IsAvailable, "No service before native session."); Reject<InvalidOperationException>(() => NativeMenu.CreateMenu());
        RID stale;
        using (var display = DisplayServer.Open("Native menu qualification", new(320, 240)))
        {
            Check(NativeMenu.HasFeature(NativeMenu.Feature.PopupMenu) && NativeMenu.HasFeature(NativeMenu.Feature.OpenCloseCallback) && NativeMenu.HasFeature(NativeMenu.Feature.HoverCallback), "Windows native capabilities.");
            Check(!NativeMenu.HasFeature(NativeMenu.Feature.GlobalMenu) && !NativeMenu.HasFeature(NativeMenu.Feature.KeyCallback), "Unsupported producers are not advertised.");
            Reject<ArgumentOutOfRangeException>(() => NativeMenu.HasFeature((NativeMenu.Feature)99));
            Model(); stale = NativeMenu.CreateMenu();
            var child = NativeMenu.CreateMenu(); NativeMenu.AddSubmenuItem(stale, "Retire parent and child", child); Native(display);
        }
        Check(!NativeMenu.IsAvailable, "Display retirement clears service.");
        using (var display = DisplayServer.Open("New native session", new(320, 240)))
        { Check(!NativeMenu.HasMenu(stale), "RIDs cannot bind a later session."); Reject<ArgumentException>(() => NativeMenu.GetItemCount(stale)); }
        GUI(); Console.WriteLine($"NativeMenu: {_checks} public checks; {_popup} independent native popup inspections.");
    }
    internal static void Unavailable()
    {
        using var display = DisplayServer.Open("Unsupported menu backend", new(64, 64), hidden: true);
        Check(NativeMenu.IsAvailable && !NativeMenu.HasFeature(NativeMenu.Feature.PopupMenu), "Active unsupported backend capability.");
        Reject<NotSupportedException>(() => NativeMenu.CreateMenu()); Console.WriteLine("Unsupported native-menu adapter rejected without native entry.");
    }
    private static void Model()
    {
        var menu = NativeMenu.CreateMenu(); Check(NativeMenu.HasMenu(menu) && !NativeMenu.HasMenu(default) && NativeMenu.GetItemCount(menu) == 0 && !NativeMenu.IsOpened(menu) && NativeMenu.GetSize(menu) == Vector2.Zero, "New native owner state.");
        Reject<ArgumentException>(() => NativeMenu.GetItemCount(default)); Reject<ArgumentOutOfRangeException>(() => NativeMenu.GetItemText(menu, -1)); Reject<ArgumentException>(() => NativeMenu.AddItem(menu, "bad\0label"));
        Action plain = () => { }; Action<int> typed = _ => { };
        Check(NativeMenu.AddItem(menu, "plain", plain) == 0 && ReferenceEquals(NativeMenu.GetItemCallback(menu, 0), plain), "Plain lambda/delegate selects the callback overload.");
        Check(NativeMenu.AddCheckItem(menu, "check", 7, typed) == 1 && NativeMenu.IsItemCheckable(menu, 1), "Typed checkbox creation.");
        Check(ReferenceEquals(NativeMenu.GetItemCallback<int>(menu, 1), typed) && NativeMenu.GetItemTag<int>(menu, 1) == 7, "Exact callback/tag identity.");
        Reject<ArgumentException>(() => NativeMenu.SetItemTag(menu, 1, "wrong")); Check(NativeMenu.GetItemTag<int>(menu, 1) == 7, "Failed tag replacement preserves old state.");
        Reject<ArgumentException>(() => NativeMenu.SetItemCallback<string>(menu, 1, _ => { })); NativeMenu.SetItemHoverCallbacks<int>(menu, 1, typed);
        NativeMenu.SetItemTag(menu, 1, 9); Check(NativeMenu.FindItemIndexWithTag(menu, 9) == 1 && NativeMenu.FindItemIndexWithTag(menu, "9") == -1, "Tag lookup keeps exact types.");
        NativeMenu.SetItemCallback<int>(menu, 1, null); NativeMenu.SetItemHoverCallbacks<int>(menu, 1, null); NativeMenu.SetItemTag<string?>(menu, 1, null); Check(NativeMenu.GetItemTag<string?>(menu, 1) is null, "Typed null tag.");
        NativeMenu.AddRadioCheckItem(menu, "radio"); NativeMenu.AddMultistateItem(menu, "state", 3, 2); NativeMenu.AddSeparator(menu);
        Check(NativeMenu.IsItemRadioCheckable(menu, 2) && NativeMenu.GetItemState(menu, 3) == 2 && NativeMenu.GetItemMaxStates(menu, 3) == 3, "Radio/state families.");
        NativeMenu.SetItemChecked(menu, 1, true); NativeMenu.SetItemDisabled(menu, 2, true); NativeMenu.SetItemState(menu, 3, 1); NativeMenu.SetItemMaxStates(menu, 3, 4);
        Check(NativeMenu.IsItemChecked(menu, 1) && NativeMenu.IsItemDisabled(menu, 2) && NativeMenu.GetItemState(menu, 3) == 1 && NativeMenu.GetItemMaxStates(menu, 3) == 4, "Explicit item state.");
        NativeMenu.SetItemHidden(menu, 1, true); NativeMenu.SetItemIndex(menu, 1, 4); Check(NativeMenu.IsItemHidden(menu, 4) && NativeMenu.FindItemIndexWithText(menu, "check") == 4, "Hidden reorder preserves logical identity."); NativeMenu.SetItemHidden(menu, 4, false);
        NativeMenu.SetItemText(menu, 4, "renamed"); Check(NativeMenu.GetItemText(menu, 4) == "renamed", "Native string replacement.");
        Check(NativeMenu.AddItem(menu, "front", index: -2) == 0 && NativeMenu.AddItem(menu, "back", index: 999) == 6, "Insertion clamps except append sentinel.");
        var child = NativeMenu.CreateMenu(); var other = NativeMenu.CreateMenu(); NativeMenu.AddSubmenuItem(menu, "child", child);
        Check(NativeMenu.GetItemSubmenu(menu, 7) == child && NativeMenu.FindItemIndexWithSubmenu(menu, child) == 7, "Submenu identity.");
        Reject<InvalidOperationException>(() => NativeMenu.FreeMenu(child)); Reject<InvalidOperationException>(() => NativeMenu.AddSubmenuItem(other, "duplicate", child)); Reject<ArgumentException>(() => NativeMenu.AddSubmenuItem(child, "cycle", menu));
        NativeMenu.SetItemSubmenu(menu, 7, default); NativeMenu.AddSubmenuItem(other, "reattach", child); NativeMenu.FreeMenu(other); Check(NativeMenu.HasMenu(child), "Freeing a parent detaches independently owned submenus."); NativeMenu.FreeMenu(child);
        var offThread = Task.Run(() => { try { NativeMenu.GetItemCount(menu); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(); Check(offThread, "Owner-thread enforcement.");
        NativeMenu.Clear(menu); Check(NativeMenu.GetItemCount(menu) == 0, "Native clear."); NativeMenu.FreeMenu(menu); Check(!NativeMenu.HasMenu(menu), "Explicit free."); Reject<ArgumentException>(() => NativeMenu.FreeMenu(menu));
    }
    private static void Native(DisplayServer display)
    {
        var menu = NativeMenu.CreateMenu(); var order = new List<string>(); var selected = 0; var hover = 0;
        NativeMenu.AddItem(menu, "First 日本語", 17, value => { selected = value; order.Add("activate"); }); NativeMenu.SetItemHoverCallbacks(menu, 0, () => { hover++; Reject<InvalidOperationException>(() => NativeMenu.SetItemText(menu, 0, "unsafe hover mutation")); });
        NativeMenu.AddCheckItem(menu, "Checked"); NativeMenu.SetItemChecked(menu, 1, true); NativeMenu.AddRadioCheckItem(menu, "Radio"); NativeMenu.SetItemChecked(menu, 2, true);
        NativeMenu.AddItem(menu, "hidden"); NativeMenu.SetItemHidden(menu, 3, true); NativeMenu.AddItem(menu, "disabled"); NativeMenu.SetItemDisabled(menu, 4, true);
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(new Color(1, 0, 0, .5f)); using var icon = ImageTexture.CreateFromImage(image);
        using var unreadable = new PlaceholderTexture(); var fault = NativeMenu.CreateMenu(); NativeMenu.AddIconItem(fault, unreadable, "Unrelated unreadable icon");
        NativeMenu.AddIconItem(menu, icon, "Icon"); NativeMenu.AddIconCheckItem(menu, icon, "Icon check"); NativeMenu.AddIconRadioCheckItem(menu, icon, "Icon radio"); NativeMenu.AddSeparator(menu);
        NativeMenu.SetPopupOpenCallback(menu, () => { Check(NativeMenu.IsOpened(menu), "Actual native open state."); order.Add("open"); Reject<InvalidOperationException>(() => NativeMenu.Popup(menu, default)); Reject<InvalidOperationException>(() => display.Dispose()); Reject<InvalidOperationException>(() => DisplayServer.ProcessEvents()); Reject<InvalidOperationException>(() => DisplayServer.DialogShow("Blocked", "Blocked", ["OK"], _ => { })); Reject<InvalidOperationException>(() => DisplayServer.FileDialogShow("Blocked", "", "", false, FileDialogMode.OpenFile, [], (_, _, _) => { })); NativeMenu.SetItemText(menu, 0, "Opened 日本語"); });
        NativeMenu.SetPopupCloseCallback(menu, () => { Check(!NativeMenu.IsOpened(menu) && NativeMenu.GetSize(menu).X > 0 && NativeMenu.GetSize(menu).Y > 0, "Actual native close state/geometry."); order.Add("close"); });
        var dump = Present(() => NativeMenu.Popup(menu, new(240, 180)), "0"); Check(selected == 17 && order.SequenceEqual(new[] { "open", "close", "activate" }), "Open/close/activation ordering.");
        Check(dump.Contains("text=Opened 日本語") && dump.Contains("items=8") && !dump.Contains("text=hidden") && dump.Contains("state=8") && dump.Contains("type=512") && dump.Contains("state=3") && dump.Contains("red_pixels=768"), "Independent native labels, count, flags and alpha icon pixels.");
        Check(hover > 0, "Actual native mouse hover callback.");
        NativeMenu.SetPopupOpenCallback(menu, null); NativeMenu.SetPopupCloseCallback(menu, null); Check(NativeMenu.GetItemTag<int>(menu, 0) == 17 && NativeMenu.IsItemChecked(menu, 1), "Selection preserves explicit check/tag state.");
        image.Fill(Colors.Blue); icon.Update(image); dump = Present(() => NativeMenu.Popup(menu, new(240, 180)), "cancel"); Check(dump.Contains("blue_pixels=768") && NativeMenu.GetItemIcon(menu, 5) == icon, "Changed icon refresh borrows original source.");
        Reject<NotSupportedException>(() => NativeMenu.Popup(fault, default)); Check(!NativeMenu.IsOpened(fault), "Icon preparation failure releases its display reservation."); NativeMenu.SetItemIcon(fault, 0, icon); Present(() => NativeMenu.Popup(fault, new(240, 180)), "cancel"); NativeMenu.FreeMenu(fault);
        icon.Dispose(); dump = Present(() => NativeMenu.Popup(menu, new(240, 180)), "cancel"); Check(!dump.Contains("bitmap=1"), "Disposed icon clears native snapshots.");
        NativeMenu.SetInterfaceDirection(menu, true); dump = Present(() => NativeMenu.Popup(menu, new(240, 180)), "cancel"); Check(dump.Contains("rtl=1"), "Real native RTL."); NativeMenu.SetInterfaceDirection(menu, false);
        NativeMenu.SetPopupOpenCallback(menu, () => throw new ApplicationException("open failure")); Reject<ApplicationException>(() => NativeMenu.Popup(menu, new(240, 180))); Check(!NativeMenu.IsOpened(menu), "Unmanaged callback failure unwinds the native loop."); NativeMenu.SetPopupOpenCallback(menu, null);
        NativeMenu.SetPopupCloseCallback(menu, () => throw new ApplicationException("close failure")); Present(() => Reject<ApplicationException>(() => NativeMenu.Popup(menu, new(240, 180))), "cancel"); NativeMenu.SetPopupCloseCallback(menu, null);
        NativeMenu.SetItemCallback<int>(menu, 0, _ => throw new ApplicationException("activation failure")); Present(() => Reject<ApplicationException>(() => NativeMenu.Popup(menu, new(240, 180))), "0");
        NativeMenu.SetPopupCloseCallback(menu, () => throw new InvalidOperationException("second failure"));
        Present(() => Reject<AggregateException>(() => NativeMenu.Popup(menu, new(240, 180))), "0");
        var empty = NativeMenu.CreateMenu(); NativeMenu.SetPopupCloseCallback(menu, () => NativeMenu.Popup(empty, default));
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "cancel"); NativeMenu.FreeMenu(empty); NativeMenu.SetPopupCloseCallback(menu, null);
        var child = NativeMenu.CreateMenu(); var childSelected = false; NativeMenu.AddItem(child, "Nested 日本語", () => childSelected = true); NativeMenu.AddSubmenuItem(menu, "More", child);
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "sub:8"); Check(childSelected, "Native submenu selection reaches its typed owner."); NativeMenu.SetItemSubmenu(menu, 9, default); NativeMenu.FreeMenu(child);
        NativeMenu.SetPopupCloseCallback(menu, () => NativeMenu.Clear(menu)); Present(() => NativeMenu.Popup(menu, new(240, 180)), "0"); Check(NativeMenu.GetItemCount(menu) == 0, "Close callback retirement suppresses stale selection.");
        NativeMenu.SetPopupCloseCallback(menu, null); NativeMenu.Popup(menu, default); NativeMenu.FreeMenu(menu);
        var retiring = NativeMenu.CreateMenu(); NativeMenu.AddItem(retiring, "Retire display", () => throw new Exception("Retired item must not activate.")); NativeMenu.SetPopupCloseCallback(retiring, display.Dispose);
        Present(() => NativeMenu.Popup(retiring, new(240, 180)), "0"); Check(!NativeMenu.IsAvailable, "Close callback may retire the display before activation.");
    }
    private static void GUI()
    {
        using (var authored = new PopupMenu { PreferNativeMenu = true })
        { authored.AddItem("Stored"); using var packed = new PackedScene(); packed.Pack(authored); using var restored = (PopupMenu)packed.Instantiate(); Check(restored.PreferNativeMenu && !restored.IsNativeMenu(), "Scene stores preference, never native identities."); }
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, Environment.GetEnvironmentVariable("ELECTRON2D_NATIVE_MENU_RENDERER") == "gpu" ? "gpu" : "compatibility");
        try
        {
            var root = new Window { Title = "PopupMenu native integration", Size = new(320, 240), GUIEmbedSubwindows = true };
            var menu = new PopupMenu { PreferNativeMenu = true }; menu.AddCheckItem("Integrated 日本語", 42); menu.SetItemChecked(0, true); root.AddChild(menu);
            var secondary = new Window { Title = "Secondary native host", Visible = false, ForceNative = true, GUIEmbedSubwindows = true, Size = new(200, 150) };
            var nested = new PopupMenu { PreferNativeMenu = true }; nested.AddItem("Secondary command"); secondary.AddChild(nested); root.AddChild(secondary);
            var button = new MenuButton("Commands") { Position = new(10, 70), Size = new(100, 25) }; button.GetPopup().PreferNativeMenu = true; button.GetPopup().AddItem("Command", 77); root.AddChild(button);
            var option = new OptionButton { Position = new(10, 110), Size = new(100, 25) }; option.AddItem("One", 10); option.AddItem("Two", 20); option.GetPopup().PreferNativeMenu = true; root.AddChild(option); var optionEvents = 0; option.ItemSelected += _ => optionEvents++;
            var focused = new List<int>(); menu.IDFocused += id => focused.Add(id);
            var order = new List<string>(); menu.AboutToPopup += () => order.Add("about"); Action hidden = () => { Check(menu.IsNativeMenu(), "Native state during hide notification."); order.Add("hide"); }; menu.PopupHide += hidden; menu.IDPressed += id => order.Add("id:" + id); menu.IndexPressed += index => order.Add("index:" + index);
            root.Ready += _ =>
            {
                Reject<ArgumentOutOfRangeException>(() => menu.Popup(new(new(20, 30), new(0, 80)))); order.Clear();
                Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "key:0");
                Check(!menu.Visible && !menu.IsNativeMenu() && order.SequenceEqual(new[] { "about", "hide", "id:42", "index:0" }), "GUI selection shares hide/ID/index contract and retires native ownership: " + string.Join(",", order));
                Check(focused.SequenceEqual(new[] { 42 }) && menu.GetFocusedItem() == 0, "Native keyboard-style focus precedes hover without duplicate or mouse IDFocused emission.");
                menu.PopupHide -= hidden;
                Action<int> hideOnFocus = _ => menu.Hide(); menu.IDFocused += hideOnFocus; Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "keycancel"); menu.IDFocused -= hideOnFocus; Check(!menu.IsNativeMenu() && !menu.Visible && order.Count(value => value == "id:42") == 1, "Hide cancels native tracking from a focused-item callback without activation.");
                menu.PreferNativeMenu = false; menu.Popup(); Check(menu.Visible && !menu.IsNativeMenu(), "Explicit canvas presentation."); menu.Hide();
                menu.PreferNativeMenu = true; menu.SearchBarEnabled = true; menu.Popup(); Check(menu.Visible && !menu.IsNativeMenu(), "Unsupported native roles keep the canvas path."); menu.Hide(); var command = false; button.GetPopup().IDPressed += id => command = id == 77; Present(button.ShowPopup, "0"); Check(command && !button.ButtonPressed, "MenuButton consumes native selection and resets its pressed state."); Present(option.ShowPopup, "1"); Check(option.Selected == 1 && option.Text == "Two" && optionEvents == 1 && !option.ButtonPressed, "OptionButton consumes native selection once and resets its pressed state."); secondary.Show(); var secondaryDump = Present(() => nested.Popup(new(new(240, 180), new(100, 50))), "0"); Check(secondaryDump.Contains("menuowner_title=Secondary native host"), "Native popup uses its actual containing native window."); secondary.Hide(); root.Tree!.Quit();
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "Integrated engine lifetime.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); }
    }
    private static string Present(Action popup, string action)
    {
        var name = System.IO.Path.Combine(_output, (++_popup).ToString()); var start = new ProcessStartInfo(_oracle) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var value in new[] { Environment.ProcessId.ToString(), action, name + ".txt", name + ".bmp" }) start.ArgumentList.Add(value);
        using var driver = Process.Start(start) ?? throw new Exception("Native oracle did not start.");
        try { popup(); } finally { if (!driver.WaitForExit(15000)) { driver.Kill(); throw new Exception("Native oracle did not finish."); } }
        Check(driver.ExitCode == 0, "Independent native oracle completed: " + name); return File.ReadAllText(name + ".txt");
    }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static void Reject<T>(Action action) where T : Exception { _checks++; try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
