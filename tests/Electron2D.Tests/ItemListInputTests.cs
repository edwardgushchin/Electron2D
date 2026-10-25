using Electron2D;

internal static class ItemListInputTests
{
    internal static void Run()
    {
        InputMap.LoadFromProjectSettings();
        VerifyNavigation();
        VerifySearch();
        VerifyPointerOrder();
        VerifyActions();
        VerifyPointerActionBindings();
        VerifyRanges();
        VerifyScroll();
        VerifyCallbackLifetime();
        VerifyPreparedInput();
        Console.WriteLine("ItemList input: navigation/search, pointer/range signal order, wheel/pan, mutation/failure/lifetime and 1024 zero-owner-byte active/idle intervals passed.");
    }

    private static void VerifyNavigation()
    {
        foreach (var mode in Enum.GetValues<ItemList.SelectMode>())
            foreach (var rtl in new[] { false, true })
            {
                using var s = new Session(mode, rtl);
                var list = s.List;
                list.Current = 0; list.SetItemDisabled(4, true);
                s.Key(Key.PageDown);
                Check(list.Current == 3, "Page Down tries exactly four rows then three, without skipping beyond its page target.");
                list.Current = 7; list.SetItemDisabled(3, true);
                s.Key(Key.PageUp); Check(list.Current == 5, "Page Up falls back to the first eligible distance within four rows.");
                list.SetItemDisabled(3, false); list.SetItemDisabled(4, false);
                list.Current = 0; list.SetItemSelectable(1, false); s.Key(Key.Down);
                Check(list.Current == 2, "Vertical arrows skip ineligible entries in the same column.");
                list.SetItemDisabled(1, true); s.Key(Key.Up); Check(list.Current == 0, "Up skips both disabled and unselectable entries.");
                list.Current = 7; s.Key(Key.Down); Check(list.Current == 7, "Navigation does not wrap at the bottom.");
                list.MaxColumns = 3; list.FixedColumnWidth = 30; list.Size = new(160, 100); list.ForceUpdateListSize();
                list.Current = 2; s.Key(Key.Right); Check(list.Current == 2, "Horizontal movement cannot cross a row boundary, including RTL.");
                list.SetItemSelectable(1, true); list.SetItemDisabled(1, false);
                s.Key(Key.Left); Check(list.Current == 1, "Logical left decreases the index in either layout direction.");
                list.SetItemDisabled(0, true); s.Key(Key.Left); Check(list.Current == 1, "An entirely ineligible row boundary retains the cursor.");
                if (mode == ItemList.SelectMode.Single) Check(list.IsSelected(1), "Single navigation selects its current item.");
                else Check(!list.IsAnythingSelected(), "Multi/Toggle navigation moves only its cursor.");
                list.DeselectAll(); s.Key(Key.Down); Check(list.Current == 2, "The unset current cursor advances from minus one using the real column count.");
                list.MaxColumns = int.MaxValue; list.WraparoundItems = false; list.ForceUpdateListSize();
                s.Key(Key.PageDown); s.Key(Key.PageUp);
            }
    }

    private static void VerifySearch()
    {
        using var s = new Session(); var list = s.List;
        list.Clear(); foreach (var text in new[] { "Alpha", "Beta", "Bee", "Boat", "Boo", "😀 first", "😀 second", "漢字" }) list.AddItem(text);
        list.Current = 0; s.Unicode('B'); Check(list.Current == 1, "A Unicode key starts case-insensitive prefix search.");
        s.Key(Key.Down); Check(list.Current == 2, "Down continues a live case-sensitive source prefix search.");
        s.Key(Key.Down); Check(list.Current == 3, "Active search skips intervening nonmatching rows.");
        s.Key(Key.Up); Check(list.Current == 2, "Up reverses active prefix search without wrapping.");
        s.Unicode('B'); Check(list.Current == 3, "Repeated single characters cycle rather than append.");
        s.Unicode('o'); Check(list.Current == 4, "A second character extends the search prefix.");
        s.Key(Key.Escape); s.Commit("😀"); Check(list.Current == 5, "Committed supplementary scalars enter the same prefix search.");
        s.Commit("😀"); Check(list.Current == 6, "Supplementary repeated characters cycle intact.");
        s.Key(Key.Escape); s.Commit("漢字"); Check(list.Current == 7, "One multi-scalar commit incrementally resolves its complete prefix.");
        list.AllowSearch = false; s.Key(Key.Escape); s.Unicode('A'); s.Commit("Alpha"); Check(list.Current == 7, "AllowSearch disables both native-text and key producers.");
        list.AllowSearch = true; s.Key(Key.Escape); list.Current = 0; list.SetItemDisabled(1, true);
        var notifications = new List<int>(); list.ItemSelected += notifications.Add;
        s.Unicode('B'); Check(list.Current == 0 && notifications.SequenceEqual([0]), "Search considers source text first; an ineligible Single target leaves selection unchanged and notifies its retained current index.");
        s.Key(Key.Down); Check(list.Current == 2, "Directional prefix search filters disabled matches.");
        s.Key(Key.Escape); list.Current = 0; s.Unicode('b'); s.Key(Key.Down);
        Check(list.Current == 0, "An ineligible lowercase typed match retains the cursor; directional continuation uses exact prefix casing.");
        list.Current = 0; s.Key(Key.Down); Check(list.Current == 0, "No exact-cased active match consumes the arrow without moving.");
        s.Key(Key.Right); s.Key(Key.Down); Check(list.Current == 2, "Horizontal navigation clears the prefix before ordinary vertical movement.");
    }

    private static void VerifyPointerOrder()
    {
        foreach (var mode in Enum.GetValues<ItemList.SelectMode>())
        {
            using var s = new Session(mode); var list = s.List; var log = new List<string>();
            list.ItemSelected += i => log.Add("s" + i);
            list.MultiSelected += (i, value) => log.Add("m" + i + value);
            list.ItemClicked += (i, point, button) => { Check(point == s.Point(i), "Click positions are list-local."); log.Add("c" + i + button); };
            list.ItemActivated += i => log.Add("a" + i);
            s.Click(1, doubleClick: true);
            Check(log.SequenceEqual(mode == ItemList.SelectMode.Single ? ["s1", "c1Left", "a1"] : new[] { "m1True", "c1Left", "a1" }), "Double-click order is selection, click, activation.");
            log.Clear(); list.SetItemSelectable(2, false); s.Click(2, doubleClick: true);
            Check(log.SequenceEqual(["c2Left", "a2"]), "Unselectable items still click and activate.");
            log.Clear(); list.SetItemDisabled(3, true); s.Click(3, doubleClick: true); Check(log.Count == 0, "Disabled items suppress every item signal.");
            s.Click(4, button: MouseButton.Right); Check(log.SequenceEqual(["c4Right"]), "Right-click remains observable without selecting by default.");
            log.Clear(); list.AllowRMBSelect = true; s.Click(4, button: MouseButton.Right);
            Check(log.SequenceEqual(mode == ItemList.SelectMode.Single ? ["s4", "c4Right"] : new[] { "m4True", "c4Right" }), "Enabled right selection follows ordinary signal order.");
            log.Clear(); list.SetBlockSignals(true); s.Click(5, doubleClick: true); Check(log.Count == 0 && list.IsSelected(5), "Signal blocking preserves input-driven state changes.");
            list.SetBlockSignals(false); log.Clear(); list.Select(0); list.Deselect(0); list.Current = 6; Check(log.Count == 0, "All programmatic selection remains silent.");
        }
        using var single = new Session(); var calls = 0; single.List.ItemSelected += _ => calls++;
        single.Click(0); single.Click(0); Check(calls == 1, "Reselection is silent initially.");
        single.List.AllowReselect = true; single.Click(0); Check(calls == 2, "AllowReselect repeats the selection notification.");
    }

    private static void VerifyActions()
    {
        foreach (var mode in Enum.GetValues<ItemList.SelectMode>())
        {
            using var s = new Session(mode); var list = s.List; list.Current = 1;
            var activated = 0; var menu = 0; var multi = 0;
            list.ItemActivated += i => { Check(i == 1, "Accept activates the current item."); activated++; };
            list.ItemClicked += (i, point, button) => { Check(i == 1 && button == MouseButton.Right && point == list.GetItemRect(1).Position, "Menu actions report the content origin and right button."); menu++; };
            list.MultiSelected += (_, _) => multi++;
            using var accept = new InputEventAction { Action = "ui_accept", Pressed = true };
            using var context = new InputEventAction { Action = "ui_menu", Pressed = true };
            s.Viewport.PushInput(accept, true); Check(activated == 1, "The synthetic accept action activates in every selection mode.");
            s.Viewport.PushInput(context, true); Check(menu == 0, "Context actions respect AllowRMBSelect.");
            list.AllowRMBSelect = true; s.Viewport.PushInput(context, true); Check(menu == 1, "Enabled context actions reach the typed click event.");
            list.SetItemDisabled(1, true); s.Viewport.PushInput(accept, true); s.Viewport.PushInput(context, true);
            Check(activated == 1 && menu == 1, "Disabled current items cannot activate or request a menu.");
            list.SetItemDisabled(1, false); list.SetItemSelectable(1, false); s.Viewport.PushInput(accept, true);
            Check(activated == 2, "Unselectable current items can still activate.");
            if (mode != ItemList.SelectMode.Single)
            {
                list.SetItemSelectable(1, true); list.Select(1, false); list.SetItemDisabled(1, true);
                s.Key(Key.Space); Check(multi == 1 && !list.IsSelected(1), "Select actions may deselect an already selected disabled item.");
                s.Key(Key.Space); Check(multi == 1, "Select actions cannot newly select a disabled item.");
            }
        }
        using var plain = new Session(); var calls = 0;
        plain.List.ItemSelected += _ => { calls++; plain.List.SetBlockSignals(true); };
        plain.List.ItemSelected += _ => calls++;
        plain.List.ItemClicked += (_, _, _) => calls += 100;
        plain.Click(0, doubleClick: true); Check(calls == 2, "Blocking during an emission preserves its delegate snapshot and suppresses subsequent emissions.");
    }

    private static void VerifyPointerActionBindings()
    {
        using var middle = new InputEventMouseButton { ButtonIndex = MouseButton.Middle };
        using var left = new InputEventMouseButton { ButtonIndex = MouseButton.Left };
        using var wheel = new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown };
        try
        {
            InputMap.ActionAddEvent("ui_accept", middle); InputMap.ActionAddEvent("ui_accept", left); InputMap.ActionAddEvent("ui_accept", wheel);
            using var s = new Session(); var list = s.List; list.Current = 1;
            var log = new List<string>(); list.ItemClicked += (i, _, _) => log.Add("c" + i);
            list.ItemActivated += i => { Check(!s.List.Tree!.IsInputHandled(), "Ordinary action activation precedes wheel-only consumption."); log.Add("a" + i); };
            s.Click(2, button: MouseButton.Middle); Check(log.SequenceEqual(["c2", "a1"]), "Nonselecting pointer buttons continue through their custom action bindings after the click.");
            log.Clear(); s.Click(2); Check(log.SequenceEqual(["c2"]), "A selecting left press finishes before custom action processing.");
            log.Clear(); list.Size = new(160, 50); list.ForceUpdateListSize();
            using var input = new InputEventMouseButton { Position = s.Point(0), ButtonIndex = MouseButton.WheelDown, Pressed = true };
            s.Send(input); Check(log.SequenceEqual(["c0", "a2"]) && list.GetVScrollBar().Value > 0, "Wheel bindings run after click and scrolling, before wheel-only consumption.");
        }
        finally { InputMap.LoadFromProjectSettings(); }
    }

    private static void VerifyRanges()
    {
        using var s = new Session(ItemList.SelectMode.Multi); var list = s.List; var changes = new List<(int, bool)>();
        list.MultiSelected += (i, selected) => changes.Add((i, selected));
        list.Current = 1; list.Select(1, false); list.SetItemDisabled(2, true); list.SetItemSelectable(3, false);
        s.Click(4, shift: true, doubleClick: true);
        Check(changes.SequenceEqual([(4, true)]) && list.GetSelectedItems().SequenceEqual([1, 4]), "Mouse ranges skip ineligible entries and preserve other selected items.");
        changes.Clear(); list.DeselectAll(); list.Current = 1;
        s.Key(Key.Down, shift: true);
        Check(changes.SequenceEqual([(1, true), (2, true)]) && list.IsSelected(1) && !list.IsSelected(2) && list.Current == 2,
            "Keyboard ranges report requested selection in index order, including an ineligible item whose selected state stays false.");
        changes.Clear(); s.Key(Key.Down, shift: true); Check(list.Current == 3 && changes.SequenceEqual([(2, true), (3, true)]), "The retained shift anchor extends the requested interval.");
        s.Key(Key.Shift, pressed: false); list.Current = 5; changes.Clear(); s.Key(Key.Up, shift: true);
        Check(list.Current == 4 && list.IsSelected(4) && list.IsSelected(5) && !list.IsSelected(1), "Releasing Shift resets the anchor before another range.");
        list.MaxColumns = 3; list.Size = new(40, 100); list.FixedColumnWidth = 30; list.ForceUpdateListSize(); list.Current = 0;
        s.Key(Key.Down, shift: true); Check(list.Current == 3, "Shift vertical ranges use configured MaxColumns even when wrapping fits fewer columns.");
        list.MaxColumns = 0; list.Current = 1; s.Key(Key.Down, shift: true); Check(list.Current == 1, "Unlimited-column configuration has a zero configured shift-row step.");
        list.SelectionMode = ItemList.SelectMode.Multi; list.DeselectAll(); list.Select(1, false); list.Select(4, false);
        changes.Clear(); s.Press(1); Check(changes.Count == 0 && list.IsSelected(4), "A plain selected Multi press defers exclusive selection.");
        s.Release(1); Check(changes.SequenceEqual([(1, true)]) && list.GetSelectedItems().SequenceEqual([1]), "Release commits one deferred notification.");
        list.Select(4, false); changes.Clear(); s.Press(1); s.Motion(1); s.Release(1);
        Check(changes.Count == 0 && list.IsSelected(4), "Pointer motion cancels deferred exclusive selection.");
        s.Press(1); list.RemoveItem(0); s.Release(0); Check(changes.Count == 0, "Removing any entry cancels a deferred index.");
        s.Press(0); list.ItemCount = 0; s.Release(0, raw: true); Check(list.ItemCount == 0, "Shrinking the model retires deferred selection before release.");
    }

    private static void VerifyScroll()
    {
        foreach (var h in new[] { false, true }) foreach (var v in new[] { false, true }) foreach (var shift in new[] { false, true })
                    foreach (var button in new[] { MouseButton.WheelDown, MouseButton.WheelRight })
                    {
                        using var s = new Session(); var list = s.List;
                        list.MaxColumns = h ? 2 : 1; list.WraparoundItems = !h; list.FixedColumnWidth = h ? 120 : 0;
                        list.Size = new(100, v ? 50 : 500); list.ForceUpdateListSize();
                        var hbar = list.GetHScrollBar(); var vbar = list.GetVScrollBar();
                        Check(hbar.Visible == h && vbar.Visible == v, "The scrolling fixture establishes its real two-axis pages.");
                        var clicked = 0; list.EmptyClicked += (_, b) => { Check(hbar.Value == 0 && vbar.Value == 0 && b == button, "Wheel signals precede scrolling."); clicked++; };
                        using var wheel = new InputEventMouseButton { Position = new(-20, -20), ButtonIndex = button, Pressed = true, ShiftPressed = shift, Factor = 2 };
                        s.Send(wheel);
                        var vertical = button == MouseButton.WheelDown ? !shift && v : shift || !h;
                        Check(clicked == 1 && (vertical ? hbar.Value == 0 : vbar.Value == 0), "Wheel fallback chooses the visible preferred axis, then Shift swaps it.");
                        Check((vertical ? vbar.Value > 0 : hbar.Value > 0) == (vertical ? v : h), "Only an axis with an actual range can move.");
                    }
        using var panSession = new Session(); var panList = panSession.List;
        panList.MaxColumns = 2; panList.FixedColumnWidth = 120; panList.WraparoundItems = false; panList.Size = new(100, 50); panList.ForceUpdateListSize();
        using var pan = new InputEventPanGesture { Delta = new(2, 3) }; panSession.Send(pan);
        Check(panList.GetHScrollBar().Value > 0 && panList.GetVScrollBar().Value > 0, "Pan gestures change both live scroll ranges.");
    }

    private static void VerifyCallbackLifetime()
    {
        using (var s = new Session())
        {
            var list = s.List; var clicked = 0;
            list.ItemSelected += i => { list.Select(i); list.SelectionMode = list.SelectionMode; list.Current = i; list.AllowSearch = list.AllowSearch; list.AllowReselect = list.AllowReselect; list.AllowRMBSelect = list.AllowRMBSelect; list.SetItemSelectable(i, true); list.SetItemDisabled(i, false); };
            list.ItemClicked += (_, _, _) => clicked++;
            s.Click(1); Check(clicked == 1, "Idempotent selection/policy assignments preserve the following click notification.");
        }
        foreach (var mutation in new Action<InputList>[] { l => l.Clear(), l => l.ItemCount = 0, l => l.RemoveItem(0), l => l.MoveItem(0, 7), l => l.SortItemsByText(), l => l.SelectionMode = ItemList.SelectMode.Toggle, l => l.Select(7), l => l.Dispose() })
        {
            using var s = new Session(ItemList.SelectMode.Multi); var list = s.List; list.Current = 0;
            var calls = 0; var clicks = 0; list.ItemClicked += (_, _, _) => clicks++;
            list.MultiSelected += (_, _) => { calls++; mutation(list); };
            s.Click(7, shift: true); Check(calls == 1 && clicks == 0, "Mutating range callbacks stop before any stale index or later click.");
        }
        using (var s = new Session())
        {
            var list = s.List; var activated = 0; list.ItemActivated += _ => activated++;
            list.ItemClicked += (_, _, _) => list.RemoveItem(0);
            s.Click(1, doubleClick: true); Check(activated == 0, "Mutation from ItemClicked suppresses activation with a stale index.");
        }
        using (var s = new Session(ItemList.SelectMode.Multi))
        {
            var list = s.List; list.Select(1); var calls = 0;
            Action<int, bool> fail = (_, _) => throw new ApplicationException("expected item handler failure");
            list.MultiSelected += fail; list.MultiSelected += (_, _) => calls++;
            s.Press(1); Reject<AggregateException>(() => s.Release(1));
            Check(list.IsSelected(1) && calls == 0, "A throwing ordinary multicast handler observes committed state and stops later handlers.");
            list.MultiSelected -= fail; s.Release(1); Check(calls == 0, "Failed release has already cleared its deferred selection.");
            list.MultiSelected += (_, _) => Reject<InvalidOperationException>(() => Reenter());
            void Reenter() { using var input = new InputEventAction { Action = "ui_select", Pressed = true }; list.Send(input); }
            s.Key(Key.Space); Check(calls == 1, "Direct recursive input is rejected while the outer operation can finish.");
            Check(Task.Run(() => { using var input = new InputEventKey { Keycode = Key.Down, Pressed = true }; try { list.Send(input); return false; } catch (InvalidOperationException) { return true; } }).Result, "Attached controls reject off-owner input before mutation.");
        }
        using (var s = new Session())
        {
            var list = s.List; list.ItemSelected += _ => list.Clear(); s.Commit("E"); Check(list.ItemCount == 0, "Committed-text callbacks may retire the model safely.");
        }
        foreach (var phase in new[] { "empty", "wheel", "pan" })
        {
            using var s = new Session(); using var list = s.List;
            list.MaxColumns = 2; list.FixedColumnWidth = 120; list.WraparoundItems = false; list.Size = new(100, 50); list.ForceUpdateListSize();
            if (phase == "empty") list.EmptyClicked += (_, _) => s.Viewport.RemoveChild(list);
            else list.GetVScrollBar().ValueChanged += _ => s.Viewport.RemoveChild(list);
            if (phase == "pan") { using var input = new InputEventPanGesture { Delta = new(2, 2) }; s.Send(input); }
            else { using var input = new InputEventMouseButton { Position = new(-20, -20), ButtonIndex = MouseButton.WheelDown, Pressed = true }; s.Send(input); }
            Check(list.Tree is null && list.GetHScrollBar().Value == 0 && (list.GetVScrollBar().Value == 0) == (phase == "empty"),
                "Detachment from empty/scroll callbacks retires remaining scrolling and does not accept input through a removed tree.");
        }
        using (var s = new Session())
        {
            var interval = ProjectSettings.Get(ProjectSettings.IncrementalSearchMaxIntervalMsec);
            try
            {
                s.List.Clear(); s.List.AddItem("Alpha"); s.List.AddItem("a long prefix beyond sixteen characters"); s.List.AddItem("Beta"); s.List.Current = 0;
                s.Commit("a long prefix beyond sixteen characters"); Check(s.List.Current == 1, "Long Unicode prefixes grow retained capacity without losing the selected match.");
                ProjectSettings.Set(ProjectSettings.IncrementalSearchMaxIntervalMsec, 0);
                var tick = Environment.TickCount64;
                Check(SpinWait.SpinUntil(() => Environment.TickCount64 > tick, TimeSpan.FromSeconds(2)), "The search clock advances before checking expiry.");
                s.Commit("B");
                Check(s.List.Current == 2, "An expired interval clears the complete previous prefix before the next commit.");
                s.Key(Key.Up); Check(s.List.Current == 1, "An expired directional prefix returns to ordinary column navigation.");
            }
            finally { ProjectSettings.Set(ProjectSettings.IncrementalSearchMaxIntervalMsec, interval); }
        }
    }

    private static void VerifyPreparedInput()
    {
        using var s = new Session(); var list = s.List;
        using var down = new InputEventKey { Keycode = Key.Down, Pressed = true };
        using var up = new InputEventKey { Keycode = Key.Up, Pressed = true };
        using var cancel = new InputEventKey { Keycode = Key.Escape, Pressed = true };
        using var typed = new InputEventKey { Unicode = 'E', Pressed = true };
        using var idle = new InputEventKey { Keycode = Key.F12, Pressed = false };
        var notifications = 0; list.ItemSelected += _ => notifications++;
        void Active()
        {
            list.Current = 0; s.Viewport.PushInput(down, true); s.Viewport.PushInput(up, true);
            s.Viewport.PushInput(cancel, true); s.Viewport.PushInput(typed, true); list.Commit("E");
        }
        for (var i = 0; i < 64; i++) Active();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) Active();
        var active = GC.GetAllocatedBytesForCurrentThread() - before;
        for (var i = 0; i < 64; i++) s.Viewport.PushInput(idle, true);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) s.Viewport.PushInput(idle, true);
        var inactive = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0 && inactive == 0 && notifications >= 4096, $"Prepared active input allocated {active} owner bytes; idle allocated {inactive}.");
    }

    private sealed class Session : IDisposable
    {
        internal readonly SubViewport Viewport = new() { Size = new(320, 240) };
        internal readonly InputList List;
        private readonly SceneTree _tree;
        internal Session(ItemList.SelectMode mode = ItemList.SelectMode.Single, bool rtl = false)
        {
            List = new InputList { Size = new(160, 200), SelectionMode = mode, LayoutDirection = rtl ? LayoutDirection.RTL : LayoutDirection.LTR };
            for (var i = 0; i < 8; i++) List.AddItem($"Entry {i}");
            Viewport.AddChild(List); _tree = new SceneTree(Viewport); List.Position = Vector2.Zero; List.GrabFocus(); _tree.FlushDeferred();
        }
        internal Vector2 Point(int index) => List.GetItemRect(index, false).Position + new Vector2(5, 5);
        internal void Key(Key key, bool shift = false, bool pressed = true) { using var input = new InputEventKey { Keycode = key, ShiftPressed = shift, Pressed = pressed }; Viewport.PushInput(input, true); }
        internal void Unicode(int scalar) { using var input = new InputEventKey { Unicode = scalar, Pressed = true }; Viewport.PushInput(input, true); }
        internal void Commit(string text) => List.Commit(text);
        internal void Press(int index) { using var input = new InputEventMouseButton { Position = Point(index), ButtonIndex = MouseButton.Left, Pressed = true }; Send(input); }
        internal void Release(int index, bool raw = false) { using var input = new InputEventMouseButton { Position = raw ? Vector2.Zero : Point(index), ButtonIndex = MouseButton.Left }; Send(input); }
        internal void Motion(int index) { using var input = new InputEventMouseMotion { Position = Point(index) }; Send(input); }
        internal void Click(int index, bool shift = false, bool doubleClick = false, MouseButton button = MouseButton.Left)
        {
            using var input = new InputEventMouseButton { Position = Point(index), ButtonIndex = button, Pressed = true, ShiftPressed = shift, DoubleClick = doubleClick }; Send(input);
        }
        internal void Send(InputEvent input) { List.ForwardInput = true; try { Viewport.PushInput(input, true); } finally { List.ForwardInput = false; } }
        public void Dispose() => _tree.Dispose();
    }
    private sealed class InputList : ItemList
    {
        internal bool ForwardInput;
        internal InputList() { InputEnabled = true; }
        protected override void OnInput(InputEvent input)
        {
            if (!ForwardInput) return;
            var tree = Tree!;
            try { base.OnGUIInput(input); } finally { tree.SetInputAsHandled(); }
        }
        internal void Send(InputEvent input) => base.OnGUIInput(input);
        internal void Commit(string text) => base.OnTextInput(text);
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
