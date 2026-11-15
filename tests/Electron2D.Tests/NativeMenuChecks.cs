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
            Check(!NativeMenu.HasFeature(NativeMenu.Feature.GlobalMenu) && NativeMenu.HasFeature(NativeMenu.Feature.KeyCallback), "Key callback support is real; global menus remain unavailable.");
            Reject<ArgumentOutOfRangeException>(() => NativeMenu.HasFeature((NativeMenu.Feature)99));
            Model(); Keyboard(); KeyboardMatrix(); stale = NativeMenu.CreateMenu();
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
    private static void Keyboard()
    {
        var menu = NativeMenu.CreateMenu();
        var normal = 0; var keys = 0;
        Action<int> ordinary = value => normal += value;
        Action<int> keyboard = value => { Check(!NativeMenu.IsOpened(menu), "Accelerator callback follows native closure."); keys += value; };
        var index = NativeMenu.AddCheckItem(menu, "Accelerator", 3, ordinary, keyCallback: keyboard, accelerator: Key.F8);
        Check(ReferenceEquals(NativeMenu.GetItemKeyCallback<int>(menu, index), keyboard) && NativeMenu.GetItemAccelerator(menu, index) == Key.F8, "Exact accelerator delegate and key.");
        Reject<ArgumentException>(() => NativeMenu.SetItemTag(menu, index, "wrong"));
        Reject<ArgumentException>(() => NativeMenu.SetItemKeyCallback<string>(menu, index, _ => { }));
        var dump = Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:119");
        Check(keys == 3 && normal == 0 && dump.Contains("F8"), "Real native F8 accelerator invokes only typed key callback and displays its shortcut.");
        NativeMenu.SetItemAccelerator(menu, index, Key.F9);
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "miss:119");
        Check(keys == 3, "Replaced accelerator no longer executes.");
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:120");
        Check(keys == 6 && normal == 0, "Replacement executes through native keyboard input.");
        NativeMenu.SetItemDisabled(menu, index, true);
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "miss:120");
        Check(keys == 6, "Disabled accelerator cannot activate.");
        NativeMenu.SetItemDisabled(menu, index, false);
        NativeMenu.SetItemAccelerator(menu, index, Key.None);
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "miss:120");
        Check(keys == 6, "Removed accelerator cannot activate.");
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "0");
        Check(normal == 3 && keys == 6, "Ordinary selection keeps its distinct callback.");
        NativeMenu.SetItemAccelerator(menu, index, Key.F8);
        var prefix = System.IO.Path.Combine(_output, "closed");
        var start = new ProcessStartInfo(_oracle) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var value in new[] { Environment.ProcessId.ToString(), "closed:119", prefix + ".txt", prefix + ".bmp" }) start.ArgumentList.Add(value);
        using (var driver = Process.Start(start)!)
        {
            var timeout = Stopwatch.StartNew();
            while (!driver.HasExited && timeout.ElapsedMilliseconds < 10000) { DisplayServer.ProcessEvents(); Thread.Sleep(10); }
            if (!driver.HasExited) { driver.Kill(); throw new Exception("Closed-menu keyboard driver timed out."); }
            DisplayServer.ProcessEvents();
            Check(driver.ExitCode == 0 && keys == 9 && normal == 3, "Closed native owner input uses the distinct key callback.");
        }
        var inputKeys = new List<bool>();
        Action<InputEvent> observe = value => { if (value is InputEventKey { Keycode: Key.F8 } key) inputKeys.Add(key.Pressed); };
        var events = DisplayServer.GetWindowEvents(); events.Input += observe;
        NativeMenu.SetItemKeyCallback<int>(menu, index, null);
        NativeMenu.SetPopupCloseCallback(menu, DisplayServer.ProcessEvents);
        Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:119");
        DisplayServer.ProcessEvents(); events.Input -= observe;
        Check(inputKeys.SequenceEqual(new[] { true, false }) && !Input.IsKeyPressed(Key.F8) && keys == 9 && normal == 3, "Forwarded input: " + string.Join(",", inputKeys) + "; held=" + Input.IsKeyPressed(Key.F8) + "; keys=" + keys + "; ordinary=" + normal);
        NativeMenu.FreeMenu(menu);
    }
    private static void KeyboardMatrix()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var icon = ImageTexture.CreateFromImage(image);
        for (var family = 0; family < 7; family++)
        {
            var menu = NativeMenu.CreateMenu(); var keys = 0; var ordinary = 0;
            Action<int> normal = value => ordinary += value;
            Action<int> keyboard = value => keys += value;
            var index = family switch
            {
                0 => NativeMenu.AddItem(menu, "Plain", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                1 => NativeMenu.AddCheckItem(menu, "Check", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                2 => NativeMenu.AddRadioCheckItem(menu, "Radio", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                3 => NativeMenu.AddIconItem(menu, icon, "Icon", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                4 => NativeMenu.AddIconCheckItem(menu, icon, "Icon check", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                5 => NativeMenu.AddIconRadioCheckItem(menu, icon, "Icon radio", 7, normal, keyCallback: keyboard, accelerator: Key.F8),
                _ => NativeMenu.AddMultistateItem(menu, "State", 3, 2, 7, normal, keyCallback: keyboard, accelerator: Key.F8)
            };
            Check(ReferenceEquals(NativeMenu.GetItemKeyCallback<int>(menu, index), keyboard), "Family retains exact key delegate: " + family);
            Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:119");
            NativeMenu.SetItemAccelerator(menu, index, Key.F9);
            Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:120");
            NativeMenu.SetItemAccelerator(menu, index, Key.None);
            Present(() => NativeMenu.Popup(menu, new(240, 180)), "miss:120");
            Present(() => NativeMenu.Popup(menu, new(240, 180)), "0");
            Check(keys == 14 && ordinary == 7 && (family != 6 || NativeMenu.GetItemState(menu, index) == 2), "Family executes separate callbacks and explicit state: " + family);
            NativeMenu.FreeMenu(menu);
        }
        var root = NativeMenu.CreateMenu(); var child = NativeMenu.CreateMenu(); var result = 0;
        NativeMenu.AddItem(child, "nested", () => throw new Exception("Wrong ordinary callback"), keyCallback: () => result = 1, accelerator: Key.F8);
        var parent = NativeMenu.AddSubmenuItem(root, "child", child);
        NativeMenu.AddItem(root, "later duplicate", keyCallback: () => result = 2, accelerator: Key.F8);
        Present(() => NativeMenu.Popup(root, new(240, 180)), "accel:119"); Check(result == 1, "Recursive logical order resolves duplicates.");
        NativeMenu.SetItemDisabled(root, parent, true);
        Present(() => NativeMenu.Popup(root, new(240, 180)), "accel:119"); Check(result == 2, "Disabled ancestor excludes submenu accelerators.");
        NativeMenu.SetItemDisabled(root, parent, false); NativeMenu.SetItemHidden(root, parent, true); result = 0;
        Present(() => NativeMenu.Popup(root, new(240, 180)), "accel:119"); Check(result == 2, "Hidden ancestor excludes submenu accelerators.");
        NativeMenu.SetItemHidden(root, parent, false); NativeMenu.SetItemIndex(root, 1, 0); result = 0;
        Present(() => NativeMenu.Popup(root, new(240, 180)), "accel:119"); Check(result == 2, "Reorder updates accelerator priority without changing identity.");
        NativeMenu.SetItemHidden(root, 0, true); NativeMenu.SetItemHidden(child, 0, true); result = 0;
        Present(() => NativeMenu.Popup(root, new(240, 180)), "miss:119"); Check(result == 0, "Hidden leaf accelerators are suppressed.");
        NativeMenu.FreeMenu(root); NativeMenu.FreeMenu(child);

        var lifetime = NativeMenu.CreateMenu(); var selected = 0;
        NativeMenu.AddItem(lifetime, "snapshot", 7, null, keyCallback: value => selected += value, accelerator: Key.F8);
        NativeMenu.SetPopupCloseCallback(lifetime, () => { NativeMenu.SetItemKeyCallback<int>(lifetime, 0, null); NativeMenu.SetItemTag(lifetime, 0, "changed"); NativeMenu.SetItemKeyCallback<string>(lifetime, 0, _ => throw new Exception("Retargeted callback")); });
        Present(() => NativeMenu.Popup(lifetime, new(240, 180)), "accel:119"); Check(selected == 7, "Selected callback and exact tag are captured before close mutation.");
        NativeMenu.SetItemKeyCallback<string>(lifetime, 0, _ => throw new ApplicationException("key failure")); NativeMenu.SetPopupCloseCallback(lifetime, () => throw new InvalidOperationException("close failure"));
        Present(() => Reject<AggregateException>(() => NativeMenu.Popup(lifetime, new(240, 180))), "accel:119");
        NativeMenu.SetPopupCloseCallback(lifetime, () => NativeMenu.FreeMenu(lifetime));
        Present(() => NativeMenu.Popup(lifetime, new(240, 180)), "accel:119"); Check(!NativeMenu.HasMenu(lifetime), "Close retirement suppresses captured keyboard callback.");

        var injected = NativeMenu.CreateMenu(); var injectedCount = 0;
        NativeMenu.AddItem(injected, "Injected", keyCallback: () => injectedCount++, accelerator: Key.F8);
        Present(() => NativeMenu.Popup(injected, new(240, 180)), "inject:0:119"); DisplayServer.ProcessEvents();
        Check(injectedCount == 1, "SendInput keyboard activation executes once across modal and ordinary pumps.");
        NativeMenu.SetItemAccelerator(injected, 0, Key.F8 | (Key)KeyModifierMask.Control);
        Present(() => NativeMenu.Popup(injected, new(240, 180)), "inject:1:119"); DisplayServer.ProcessEvents();
        Check(injectedCount == 2, "Native Control modifier matches the accelerator.");
        foreach (var (modifiers, flags) in new[] { (2, KeyModifierMask.Shift), (4, KeyModifierMask.Alt), (3, KeyModifierMask.Control | KeyModifierMask.Shift), (5, KeyModifierMask.Control | KeyModifierMask.Alt), (7, KeyModifierMask.Control | KeyModifierMask.Alt | KeyModifierMask.Shift), (1, KeyModifierMask.CommandOrControl) })
        {
            var before = injectedCount;
            NativeMenu.SetItemAccelerator(injected, 0, Key.F8 | (Key)flags);
            Present(() => NativeMenu.Popup(injected, new(240, 180)), "inject:" + modifiers + ":119"); DisplayServer.ProcessEvents();
            Check(injectedCount == before + 1, "Native modifier combination matches once: " + flags + "; before=" + before + "; after=" + injectedCount);
        }
        NativeMenu.SetItemAccelerator(injected, 0, Key.F8 | (Key)KeyModifierMask.Control);
        var beforeMismatch = injectedCount;
        Present(() => NativeMenu.Popup(injected, new(240, 180)), "injectmiss:3:119"); DisplayServer.ProcessEvents();
        Check(injectedCount == beforeMismatch, "Extra native Shift does not match Control-only accelerator.");
        NativeMenu.FreeMenu(injected);

        var altOnly = NativeMenu.CreateMenu(); var altCount = 0;
        NativeMenu.AddItem(altOnly, "Alt key", keyCallback: () => altCount++, accelerator: Key.Alt);
        NativeMenu.AddItem(altOnly, "Alt chord", keyCallback: () => throw new Exception("Wrong Alt priority"), accelerator: Key.F8 | (Key)KeyModifierMask.Alt);
        Present(() => NativeMenu.Popup(altOnly, new(240, 180)), "inject:0:18"); DisplayServer.ProcessEvents();
        Check(altCount == 1, "A matching Alt-only accelerator precedes the Alt chord guard.");
        NativeMenu.FreeMenu(altOnly);

        var heldMenu = NativeMenu.CreateMenu(); NativeMenu.AddItem(heldMenu, "Held input", accelerator: Key.F8);
        var heldTrace = new List<bool>(); var heldAtClose = false;
        Action<InputEvent> heldObserver = value => { if (value is InputEventKey { Keycode: Key.F8 } key) heldTrace.Add(key.Pressed); };
        var heldEvents = DisplayServer.GetWindowEvents(); heldEvents.Input += heldObserver;
        var releaseGate = System.IO.Path.Combine(_output, (_popup + 1) + ".txt.release");
        if (File.Exists(releaseGate)) File.Delete(releaseGate);
        NativeMenu.SetPopupCloseCallback(heldMenu, () => { DisplayServer.ProcessEvents(); heldAtClose = Input.IsKeyPressed(Key.F8); File.WriteAllText(releaseGate, "release"); });
        Present(() => NativeMenu.Popup(heldMenu, new(240, 180)), "hold:119"); DisplayServer.ProcessEvents(); heldEvents.Input -= heldObserver;
        Check(heldAtClose && heldTrace.SequenceEqual(new[] { true, false }) && !Input.IsKeyPressed(Key.F8), "Held SendInput remains down during close pump and releases exactly once on actual up.");
        NativeMenu.FreeMenu(heldMenu);

        foreach (var (keycode, vk) in new[] { (Key.Pause, 19), (Key.Menu, 93), (Key.Back, 166), (Key.Forward, 167), (Key.Refresh, 168), (Key.Stop, 169), (Key.Search, 170), (Key.Favorites, 171), (Key.HomePage, 172), (Key.VolumeMute, 173), (Key.VolumeDown, 174), (Key.VolumeUp, 175), (Key.MediaNext, 176), (Key.MediaPrevious, 177), (Key.MediaStop, 178), (Key.MediaPlay, 179) })
        {
            var menu = NativeMenu.CreateMenu(); var count = 0;
            NativeMenu.AddItem(menu, keycode.ToString(), keyCallback: () => count++, accelerator: keycode);
            Present(() => NativeMenu.Popup(menu, new(240, 180)), "accel:" + vk);
            DriveClosed("closed:" + vk);
            Check(count == 2, "Modal and closed Windows key identity agree: " + keycode + "; count=" + count);
            NativeMenu.FreeMenu(menu);
        }
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
            using var echoShortcut = new Shortcut();
            var echoMenu = new PopupMenu { PreferNativeMenu = true }; echoMenu.AddShortcut(echoShortcut, 66, allowEcho: true); echoMenu.SetItemShortcut(0, null); echoMenu.SetItemText(0, "Echo"); echoMenu.SetItemAccelerator(0, Key.F8); root.AddChild(echoMenu);
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
                menu.SetItemAccelerator(0, Key.F8); order.Clear();
                Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "accel:119");
                Check(order.SequenceEqual(new[] { "about", "hide", "id:42", "index:0" }), "PopupMenu accelerator uses native owner and close/ID/index delivery.");
                menu.SetItemShortcutDisabled(0, true); order.Clear();
                Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "miss:119");
                Check(order.SequenceEqual(new[] { "about", "hide" }), "Disabled shortcut preserves native presentation without keyboard activation.");
                order.Clear(); Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "0");
                Check(order.Contains("id:42"), "ShortcutDisabled preserves ordinary native selection.");
                menu.SetItemShortcutDisabled(0, false); order.Clear();
                Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "repeatmiss:119");
                Check(order.SequenceEqual(new[] { "about", "hide" }), "PopupMenu default AllowEcho rejects repeated native accelerators.");
                var echoSelected = false; echoMenu.IDPressed += id => echoSelected = id == 66;
                Present(() => echoMenu.Popup(), "repeat:119"); Check(echoSelected, "Authored AllowEcho survives the native bridge.");
                menu.SetItemAccelerator(0, Key.None); order.Clear();
                menu.PopupHide -= hidden;
                Action<int> hideOnFocus = _ => menu.Hide(); menu.IDFocused += hideOnFocus; Present(() => menu.Popup(new(new(20, 30), new(100, 80))), "keycancel"); menu.IDFocused -= hideOnFocus; Check(!menu.IsNativeMenu() && !menu.Visible && order.Count(value => value == "id:42") == 0, "Hide cancels native tracking from a focused-item callback without activation.");
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
    private static void DriveClosed(string action)
    {
        var prefix = System.IO.Path.Combine(_output, "closed-" + action.Replace(':', '-'));
        var start = new ProcessStartInfo(_oracle) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var value in new[] { Environment.ProcessId.ToString(), action, prefix + ".txt", prefix + ".bmp" }) start.ArgumentList.Add(value);
        using var driver = Process.Start(start) ?? throw new Exception("Closed keyboard driver did not start.");
        var timeout = Stopwatch.StartNew();
        while (!driver.HasExited && timeout.ElapsedMilliseconds < 10000) { DisplayServer.ProcessEvents(); Thread.Sleep(10); }
        if (!driver.HasExited) { driver.Kill(); throw new Exception("Closed keyboard driver timed out."); }
        DisplayServer.ProcessEvents(); Check(driver.ExitCode == 0, "Closed keyboard driver completed: " + action);
    }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static void Reject<T>(Action action) where T : Exception { _checks++; try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
