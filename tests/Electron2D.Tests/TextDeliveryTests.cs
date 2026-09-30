using Electron2D;

internal static class TextDeliveryTests
{
    internal static void Run()
    {
        VerifyFocusedDelivery();
        VerifyFailureAndReentry();
        Console.WriteLine("Focused text and IME composition delivery, ownership, notifications and failure continuation passed.");
    }

    private static void VerifyFocusedDelivery()
    {
        var viewport = new TestViewport();
        var first = new TextControl { Name = "First", Size = new(40, 20), FocusMode = FocusMode.All };
        var second = new TextControl { Name = "Second", Position = new(50, 0), Size = new(40, 20), FocusMode = FocusMode.All };
        viewport.AddChild(first); viewport.AddChild(second);
        using var tree = new SceneTree(viewport);
        first.GrabFocus();
        var text = "é👩‍🚀";
        tree.DispatchCommittedText(viewport, text);
        Check(first.Committed is ["é👩‍🚀"] && first.TextEvents == 1 && second.Committed.Count == 0 &&
              first.KeyInputs == 0,
            "One native-style commit reaches the focused Control as one complete string without a fabricated key event.");
        tree.DispatchIMEComposition(viewport, "a🙂", new(1, 1));
        Check(first.Composition is [var composition] && composition.Text == "a🙂" && composition.Selection == new Vector2i(1, 1) &&
              first.IMEEvents == 1 && first.IMENotifications == 1 && second.IMENotifications == 1 &&
              second.Composition.Count == 0,
            "Composition notifies the whole scene but delivers typed preedit only to the focused Control.");
        second.GrabFocus();
        tree.DispatchCommittedText(viewport, "漢字");
        Check(second.Committed is ["漢字"] && first.Committed.Count == 1,
            "Focus transfer routes later complete text to the new owner.");
        second.ProcessMode = ProcessMode.Disabled;
        tree.DispatchCommittedText(viewport, "ignored");
        Check(second.Committed.Count == 1, "A focused but non-processing Control does not consume text.");
        second.ProcessMode = ProcessMode.Inherit;
        second.Visible = false;
        tree.DispatchIMEComposition(viewport, string.Empty, Vector2i.Zero);
        Check(!second.HasFocus() && second.Composition.Count == 0 && first.IMENotifications == 2,
            "Hiding the focused Control releases focus; an empty IME update still propagates to live scene nodes.");
        Reject<ArgumentNullException>(() => tree.DispatchCommittedText(viewport, null!));
        Reject<ArgumentNullException>(() => tree.DispatchIMEComposition(viewport, null!, Vector2i.Zero));
    }

    private static void VerifyFailureAndReentry()
    {
        var viewport = new TestViewport();
        var control = new TextControl { Name = "Text", Size = new(40, 20), FocusMode = FocusMode.All };
        viewport.AddChild(control);
        using var tree = new SceneTree(viewport);
        control.GrabFocus();
        control.ThrowOnText = true;
        Reject<AggregateException>(() => tree.DispatchCommittedText(viewport, "x"));
        Check(control.TextEvents == 1 && control.Committed is ["x"],
            "A throwing virtual hook does not suppress the later typed text event.");
        control.ThrowOnText = false;
        control.ThrowOnNotify = true;
        Reject<AggregateException>(() => tree.DispatchIMEComposition(viewport, "preedit", new(0, 1)));
        Check(control.IMEEvents == 1 && control.Composition is [var preedit] && preedit.Text == "preedit",
            "A failing scene IME notification still attempts focused typed composition delivery.");
        control.ThrowOnNotify = false;
        control.Reenter = () => tree.DispatchCommittedText(viewport, "nested");
        Reject<AggregateException>(() => tree.DispatchCommittedText(viewport, "outer"));
        Check(control.Committed.Count == 2 && control.Committed[^1] == "outer",
            "Nested text injection is rejected while the outer commit and later event still finish.");
        control.Reenter = null; control.AcceptText = true;
        tree.DispatchCommittedText(viewport, "accepted");
        Check(control.EventSawHandled && control.Committed[^1] == "accepted",
            "A text hook may mark its active scene delivery handled without suppressing its later typed event.");
        Check(Task.Run(() =>
        {
            try { tree.DispatchCommittedText(viewport, "off-owner"); return false; }
            catch (InvalidOperationException) { return true; }
        }).Result, "Attached text delivery rejects an off-owner caller before mutation.");
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 320, 240);
    }

    private sealed class TextControl : Control
    {
        internal readonly List<string> Committed = [];
        internal readonly List<(string Text, Vector2i Selection)> Composition = [];
        internal int TextEvents, IMEEvents, IMENotifications, KeyInputs;
        internal bool ThrowOnText, ThrowOnNotify;
        internal bool AcceptText, EventSawHandled;
        internal Action? Reenter;

        internal TextControl()
        {
            TextInput += _ => { TextEvents++; EventSawHandled = Tree?.IsInputHandled() == true; };
            IMECompositionChanged += (_, _) => IMEEvents++;
            InputEnabled = true;
        }

        protected override void OnTextInput(string text)
        {
            Committed.Add(text);
            if (AcceptText) AcceptEvent();
            Reenter?.Invoke();
            if (ThrowOnText) throw new ApplicationException("expected text hook failure");
        }

        protected override void OnIMECompositionChanged(string text, Vector2i selection) => Composition.Add((text, selection));

        protected override void OnInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventKey) KeyInputs++;
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationOsImeUpdate)
            {
                IMENotifications++;
                if (ThrowOnNotify) throw new ApplicationException("expected IME notification failure");
            }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
