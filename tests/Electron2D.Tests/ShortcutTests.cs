using Electron2D;

internal static class ShortcutTests
{
    internal static void Run()
    {
        using var shortcut = new Shortcut();
        using var key = new InputEventKey { Keycode = Key.K, ControlPressed = true };
        using var actual = new InputEventKey { Keycode = Key.K, ControlPressed = true, Pressed = true };
        using var other = new InputEventKey { Keycode = Key.K, Pressed = true };
        using var direct = new InputEventShortcut { Shortcut = shortcut };
        Check(!shortcut.HasValidEvent() && shortcut.GetAsText() == "None", "Empty shortcut defaults.");
        Check(direct.IsPressed() && !direct.IsReleased() && !direct.IsActionType() && shortcut.MatchesEvent(direct), "Direct identity activates an empty shortcut without becoming an action binding.");
        shortcut.Events = [null, key, key];
        Check(shortcut.HasValidEvent() && shortcut.MatchesEvent(actual) && !shortcut.MatchesEvent(other), "Ordered alternatives use exact matching.");
        var snapshot = shortcut.Events; snapshot[1] = other;
        Check(ReferenceEquals(shortcut.Events[1], key), "Returned arrays cannot edit the stored list.");
        var changes = 0; shortcut.Changed += _ => changes++;
        shortcut.Events = shortcut.Events; Check(changes == 1, "Equal list assignments still notify.");
        key.Keycode = Key.J; Check(changes == 1 && !shortcut.MatchesEvent(actual), "Borrowed event changes apply without forwarding Changed.");
        Reject<ArgumentException>(() => shortcut.Events = [other, direct]);
        Check(ReferenceEquals(shortcut.Events[1], key), "Invalid shortcut-event alternatives cannot partially commit.");
        using var shallow = (Shortcut)shortcut.Duplicate();
        using var deep = (Shortcut)shortcut.Duplicate(true);
        Check(ReferenceEquals(shallow.Events[1], key) && !ReferenceEquals(deep.Events[1], key) && ReferenceEquals(deep.Events[1], deep.Events[2]), "Deep duplication preserves aliases while shallow copies borrow.");
        using var eventCopy = (InputEventShortcut)direct.Duplicate(true);
        Check(eventCopy.IsPressed() && !ReferenceEquals(eventCopy.Shortcut, shortcut) && eventCopy.Shortcut!.MatchesEvent(eventCopy), "Shortcut input duplicates the resource graph when requested.");
        Check(direct.AsText().Contains(shortcut.GetAsText(), StringComparison.Ordinal) && direct.ToString().StartsWith("InputEventShortcut: shortcut=", StringComparison.Ordinal), "Public event descriptions include live shortcut text.");
        using var emptyEvent = new InputEventShortcut();
        Check(emptyEvent.AsText() == "None" && emptyEvent.ToString() == "None", "Missing shortcut descriptions are explicit.");
        var fail = new Action<Resource>(_ => throw new InvalidOperationException("observer")); shortcut.Changed += fail;
        Reject<InvalidOperationException>(() => shortcut.Events = [other]);
        Check(ReferenceEquals(shortcut.Events[0], other), "Throwing Changed observes committed state.");
        shortcut.Changed -= fail;
        using var unset = new InputEventKey(); shortcut.Events = [unset];
        Check(shortcut.HasValidEvent(), "Validity means a live event reference, not a configured physical key.");
        unset.Dispose(); Check(!shortcut.HasValidEvent() && shortcut.GetAsText() == "None", "Disposed borrowed alternatives are ignored.");
        shortcut.Events = [key];
        for (var i = 0; i < 64; i++) { shortcut.MatchesEvent(direct); shortcut.MatchesEvent(actual); shortcut.HasValidEvent(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { shortcut.MatchesEvent(direct); shortcut.MatchesEvent(actual); shortcut.HasValidEvent(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed matching and validity checks allocate no managed memory.");
        using var reentrant = new ComparisonEvent { Compare = _ => { shortcut.Events = []; return false; } };
        key.Keycode = Key.K; shortcut.Events = [reentrant, key];
        Check(shortcut.MatchesEvent(actual) && shortcut.Events.Length == 0, "Reentrant replacement keeps the current immutable comparison snapshot and publishes the new list for subsequent calls.");
        reentrant.Compare = _ => throw new ApplicationException("comparison"); shortcut.Events = [reentrant, key];
        Reject<ApplicationException>(() => shortcut.MatchesEvent(actual));
        using var localShortcut = new Shortcut { ResourceLocalToScene = true, Events = [key, key] };
        using var button = new Button { Shortcut = localShortcut };
        using var packedButton = new PackedScene(); packedButton.Pack(button);
        using var buttonCopy = (Button)packedButton.Instantiate();
        Check(!ReferenceEquals(buttonCopy.Shortcut, localShortcut) && ReferenceEquals(buttonCopy.Shortcut!.GetLocalScene(), buttonCopy) &&
            ReferenceEquals(buttonCopy.Shortcut.Events[0], buttonCopy.Shortcut.Events[1]), "Scene-local shortcut duplication assigns the new scene and preserves alternative aliases.");
        RouteAndStorage();
        Console.WriteLine("Shortcut resources verify exact alternatives, identity events, copying, failures, routing, context storage and warmed matching.");
    }

    private static void RouteAndStorage()
    {
        var root = new TestViewport();
        var order = new List<string>();
        var probe = new Probe { InputEnabled = true, ShortcutInputEnabled = true, UnhandledKeyInputEnabled = true, UnhandledInputEnabled = true, Order = order };
        root.AddChild(probe);
        using var tree = new SceneTree(root);
        using var key = new InputEventKey { Keycode = Key.F12, Pressed = true };
        root.PushInput(key, true); Check(order.SequenceEqual(["input", "shortcut", "key", "unhandled"]), "Shortcut stage precedes unhandled key and general input.");
        order.Clear(); probe.Handle = true; root.PushInput(key, true);
        Check(order.SequenceEqual(["input", "shortcut"]), "Shortcut handling stops later stages.");
        order.Clear(); probe.Handle = false;
        using var motion = new InputEventJoypadMotion { Axis = JoyAxis.RightX, AxisValue = 1 };
        root.PushInput(motion, true); Check(order.SequenceEqual(["input", "unhandled"]), "Analog motion is not a shortcut-stage event.");
        order.Clear(); probe.Fail = true; Reject<AggregateException>(() => root.PushInput(key, true));
        Check(order.SequenceEqual(["input", "shortcut", "key", "unhandled"]), "Shortcut callback failure does not skip unhandled stages.");
        probe.Fail = false;
        using var scene = new Control { Name = "Scene" };
        var button = new Control { Name = "Button" }; var context = new Control { Name = "Context", FocusMode = FocusMode.All };
        scene.AddChild(button); scene.AddChild(context); button.Owner = scene; context.Owner = scene; button.ShortcutContext = context;
        using var packed = new PackedScene(); packed.Pack(scene);
        using var first = (Control)packed.Instantiate(); using var second = (Control)packed.Instantiate();
        Check(ReferenceEquals(((Control)first.GetChild(0)).ShortcutContext, first.GetChild(1)) && ReferenceEquals(((Control)second.GetChild(0)).ShortcutContext, second.GetChild(1)), "Forward node references resolve within each new hierarchy.");
        scene.Dispose(); Check(!first.GetChild(1).IsDisposed && ReferenceEquals(((Control)first.GetChild(0)).ShortcutContext, first.GetChild(1)), "Stored contexts do not keep or resolve to the original scene.");
        var firstButton = (Control)first.GetChild(0); var firstContext = (Control)first.GetChild(1);
        root.AddChild(first); firstContext.GrabFocus(); Check(firstButton.IsFocusOwnerInShortcutContext(), "Context accepts its focused node.");
        firstContext.Dispose(); Check(firstButton.ShortcutContext is null && !firstButton.IsFocusOwnerInShortcutContext(), "An expired weak context does not become global.");
        firstButton.ShortcutContext = null; Check(firstButton.IsFocusOwnerInShortcutContext(), "Explicitly clearing the context restores global matching.");
    }

    private sealed class Probe : Node
    {
        internal List<string> Order = [];
        internal bool Handle, Fail;
        protected override void OnInput(InputEvent @event) => Order.Add("input");
        protected override void OnShortcutInput(InputEvent @event) { Order.Add("shortcut"); if (Handle) Tree!.SetInputAsHandled(); if (Fail) throw new InvalidOperationException("shortcut"); }
        protected override void OnUnhandledKeyInput(InputEventKey @event) => Order.Add("key");
        protected override void OnUnhandledInput(InputEvent @event) => Order.Add("unhandled");
    }
    private sealed class ComparisonEvent : InputEvent
    {
        internal Func<InputEvent, bool>? Compare;
        public override bool IsMatch(InputEvent @event, bool exactMatch = true) => Compare?.Invoke(@event) ?? false;
        public override string AsText() => string.Empty;
        protected override InputEvent CreateEventInstance() => new ComparisonEvent();
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(Vector2.Zero, new(320, 240)); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
