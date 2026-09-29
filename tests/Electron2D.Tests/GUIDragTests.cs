using Electron2D;

internal static class GUIDragTests
{
    internal static void Run()
    {
        VerifyAutomaticDrag();
        VerifyForcedAndCancelledDrag();
        VerifyCallbackFailure();
        VerifyAncestorAndPreviewPolicy();
        VerifyThresholdAndValidation();
        VerifyActivationRollback();
        Console.WriteLine("Typed GUI drag and drop: automatic threshold, preview, target, cancellation, forwarding and failure passed.");
    }

    private static void VerifyAutomaticDrag()
    {
        var viewport = new TestViewport();
        var source = new DragSource { Name = "Source", Position = new(10, 10), Size = new(40, 40) };
        var target = new DropTarget { Name = "Target", Position = new(100, 10), Size = new(40, 40) };
        viewport.AddChild(source); viewport.AddChild(target);
        using var tree = new SceneTree(viewport);
        Check(viewport.GUIDragThreshold == 10 && !viewport.IsGUIDragging() && viewport.GetGUIDragData() is null,
            "The root viewport starts with the typed ten-pixel threshold and no borrowed payload.");
        using (var press = new InputEventMouseButton { Position = new(20, 20), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(press, inLocalCoordinates: true);
        using (var motion = new InputEventMouseMotion { Position = new(30, 20), Relative = new(10, 0), ButtonMask = MouseButtonMask.Left })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(!viewport.IsGUIDragging() && source.Requests == 0, "Travel equal to the threshold does not start a drag.");
        using (var motion = new InputEventMouseMotion { Position = new(31, 20), Relative = new(1, 0), ButtonMask = MouseButtonMask.Left })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(viewport.IsGUIDragging() && viewport.GetGUIDragData() is DragPayload<string> { Value: "sample" } &&
              source.Requests == 1 && source.Origin == new Vector2(10, 10) && source.Preview is { IsDisposed: false } &&
              viewport.GetGUIDragDescription() == "sample" && source.BeginCount == 1 && target.BeginCount == 1,
            "Crossing the threshold asks the captured source at its original local press and starts one typed drag with a live preview.");
        using (var motion = new InputEventMouseMotion { Position = new(110, 20), Relative = new(79, 0), ButtonMask = MouseButtonMask.Left })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(target.CanDropCount > 0 && source.Preview.Position == new Vector2(110, 20),
            "Drag motion checks the visible target and moves the preview in viewport coordinates.");
        using (var release = new InputEventMouseButton { Position = new(110, 20), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        Check(!viewport.IsGUIDragging() && viewport.IsGUIDragSuccessful() && source.IsDragSuccessful() && viewport.GetGUIDragData() is null &&
              target.Drops == 1 && target.DropPosition == new Vector2(10, 10) &&
              source.Preview.IsDisposed && source.EndCount == 1 && target.EndCount == 1 &&
              viewport.GetGUIDragDescription() == "Drag-and-drop data",
            "An accepted release commits one drop, destroys the preview and exposes success before the end notification.");
    }

    private static void VerifyForcedAndCancelledDrag()
    {
        var viewport = new TestViewport();
        var source = new DragSource { Name = "Source", Size = new(40, 40) };
        var target = new DropTarget { Name = "Target", Position = new(100, 0), Size = new(40, 40) };
        viewport.AddChild(source); viewport.AddChild(target);
        using var tree = new SceneTree(viewport);
        var payload = new DragPayload<int>(42);
        source.ForceDrag(payload);
        Check(viewport.IsGUIDragging() && ReferenceEquals(viewport.GetGUIDragData(), payload),
            "Forced drag borrows the exact caller payload without a pointer threshold.");
        Reject<InvalidOperationException>(() => source.ForceDrag(payload));
        viewport.CancelGUIDrag();
        Check(!viewport.IsGUIDragging() && !viewport.IsGUIDragSuccessful() && viewport.GetGUIDragData() is null,
            "Explicit cancellation clears the payload and records failure.");
        var forwarded = 0;
        source.SetDragForwarding(_ => new DragPayload<string>("forwarded"), null, null);
        target.SetDragForwarding(null, (_, data) => data is DragPayload<string>, (_, data) =>
        {
            Check(data is DragPayload<string> { Value: "forwarded" }, "The forwarded target receives the typed value.");
            forwarded++;
        });
        source.ForceDrag(new DragPayload<string>("forwarded"));
        using (var press = new InputEventMouseButton { Position = new(110, 10), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(press, inLocalCoordinates: true);
        Check(forwarded == 1 && viewport.IsGUIDragSuccessful(),
            "A left press drops a forced drag through the typed forwarding delegates.");
        using (var press = new InputEventMouseButton { Position = new(10, 10), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(press, inLocalCoordinates: true);
        using (var motion = new InputEventMouseMotion { Position = new(21, 10), Relative = new(11, 0), ButtonMask = MouseButtonMask.Left })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(viewport.GetGUIDragData() is DragPayload<string> { Value: "forwarded" } && source.Requests == 0,
            "A supplied source-forwarding delegate replaces the virtual producer on automatic drag.");
        using (var release = new InputEventMouseButton { Position = new(110, 10), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        Check(forwarded == 2 && viewport.IsGUIDragSuccessful(), "The forwarded automatic payload completes through the target delegate.");
        source.ForceDrag(payload);
        using (var right = new InputEventMouseButton { Position = new(110, 10), ButtonIndex = MouseButton.Right, Pressed = true })
            viewport.PushInput(right, inLocalCoordinates: true);
        Check(!viewport.IsGUIDragging() && !viewport.IsGUIDragSuccessful(),
            "A right press cancels an active drag without delivering to the accepting target.");
        source.ForceDrag(payload);
        source.Visible = false;
        Check(!viewport.IsGUIDragging() && !viewport.IsGUIDragSuccessful(),
            "Hiding the source cancels its active drag without retaining viewport state.");
    }

    private static void VerifyCallbackFailure()
    {
        var viewport = new TestViewport();
        var source = new DragSource { Name = "Source", Size = new(40, 40) };
        var target = new DropTarget { Name = "Target", Position = new(100, 0), Size = new(40, 40), ThrowOnDrop = true };
        viewport.AddChild(source); viewport.AddChild(target);
        using var tree = new SceneTree(viewport);
        source.ForceDrag(new DragPayload<string>("sample"));
        using (var press = new InputEventMouseButton { Position = new(110, 10), ButtonIndex = MouseButton.Left, Pressed = true })
            Reject<AggregateException>(() => viewport.PushInput(press, inLocalCoordinates: true));
        Check(!viewport.IsGUIDragging() && !viewport.IsGUIDragSuccessful() && source.EndCount == 1 && target.EndCount == 1,
            "A failing drop callback still ends the drag, clears state and notifies later nodes.");
        source.ThrowOnBegin = true;
        Reject<AggregateException>(() => source.ForceDrag(new DragPayload<int>(1)));
        source.ThrowOnBegin = false;
        Check(viewport.IsGUIDragging(), "A throwing begin observer sees committed drag state without preventing later observers.");
        viewport.CancelGUIDrag();
    }

    private static void VerifyAncestorAndPreviewPolicy()
    {
        var viewport = new TestViewport();
        var source = new DragSource { Name = "Source", Size = new(40, 40) };
        var parent = new Control { Name = "Parent", Position = new(80, 0), Size = new(80, 60), MouseFilter = MouseFilter.Stop };
        var child = new Control { Name = "Child", Position = new(10, 0), Size = new(30, 30), MouseFilter = MouseFilter.Pass };
        parent.AddChild(child); viewport.AddChild(source); viewport.AddChild(parent);
        using var tree = new SceneTree(viewport);
        var drops = 0; var local = Vector2.Zero;
        parent.SetDragForwarding(null, (position, payload) => payload is DragPayload<int> && position == new Vector2(20, 10),
            (position, _) => { drops++; local = position; });
        var firstPreview = new Control { Name = "FirstPreview", Size = new(6, 6) };
        source.ForceDrag(new DragPayload<int>(7), firstPreview);
        var secondPreview = new Control { Name = "SecondPreview", Size = new(6, 6) };
        source.SetDragPreview(secondPreview);
        Check(firstPreview.IsDisposed && !secondPreview.IsDisposed,
            "Replacing a preview disposes the former owned control without ending the drag.");
        using (var release = new InputEventMouseButton { Position = new(100, 10), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        Check(viewport.IsGUIDragSuccessful() && drops == 1 && local == new Vector2(20, 10) && secondPreview.IsDisposed,
            "A passing child lets its accepting parent receive a local-coordinate drop.");
        child.SetDragForwarding(null, (_, _) => throw new ApplicationException("expected candidate failure"), null);
        source.ForceDrag(new DragPayload<int>(7));
        using (var release = new InputEventMouseButton { Position = new(100, 10), ButtonIndex = MouseButton.Left, Pressed = false })
            Reject<AggregateException>(() => viewport.PushInput(release, inLocalCoordinates: true));
        Check(viewport.IsGUIDragSuccessful() && drops == 2,
            "A failed candidate check reports its error after an accepting ancestor receives the drop.");
        child.SetDragForwarding(null, (_, _) => { child.Visible = false; return true; }, null);
        source.ForceDrag(new DragPayload<int>(7));
        using (var release = new InputEventMouseButton { Position = new(100, 10), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        Check(viewport.IsGUIDragSuccessful() && drops == 3 && !child.Visible,
            "A candidate hidden by its own check is skipped and an accepting parent receives the drop.");
        child.Visible = true;
        child.SetDragForwarding(null, null, null);
        child.MouseFilter = MouseFilter.Stop;
        source.ForceDrag(new DragPayload<int>(7));
        Check(viewport.IsGUIDragSuccessful(), "The previous drop result remains visible while a new drag is active.");
        using (var release = new InputEventMouseButton { Position = new(100, 10), ButtonIndex = MouseButton.Left, Pressed = false })
            viewport.PushInput(release, inLocalCoordinates: true);
        Check(!viewport.IsGUIDragSuccessful() && drops == 3,
            "A stopping child blocks its parent from accepting the drop.");
        source.ReturnNull = true;
        using (var press = new InputEventMouseButton { Position = new(10, 10), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(press, inLocalCoordinates: true);
        using (var motion = new InputEventMouseMotion { Position = new(21, 10), Relative = new(11, 0), ButtonMask = MouseButtonMask.Left })
            viewport.PushInput(motion, inLocalCoordinates: true);
        Check(!viewport.IsGUIDragging() && source.Preview.IsDisposed,
            "Returning null after setting a preview rejects the drag and destroys that temporary preview.");
    }

    private static void VerifyThresholdAndValidation()
    {
        var settings = ProjectSettings.Instance;
        var previous = settings.Get(ProjectSettings.DefaultGUIDragThreshold);
        try
        {
            settings.Set(ProjectSettings.DefaultGUIDragThreshold, -1);
            var viewport = new TestViewport();
            var source = new DragSource { Name = "Source", Size = new(40, 40) };
            viewport.AddChild(source);
            using var tree = new SceneTree(viewport);
            Check(viewport.GUIDragThreshold == -1, "A new viewport samples the signed project drag threshold.");
            using (var press = new InputEventMouseButton { Position = new(10, 10), ButtonIndex = MouseButton.Left, Pressed = true })
                viewport.PushInput(press, inLocalCoordinates: true);
            using (var motion = new InputEventMouseMotion { Position = new(10, 10), Relative = Vector2.Zero, ButtonMask = MouseButtonMask.Left })
                viewport.PushInput(motion, inLocalCoordinates: true);
            Check(viewport.IsGUIDragging() && source.Requests == 1,
                "A negative threshold attempts automatic drag on the first left-held motion.");
            InputMap.Instance.LoadFromProjectSettings();
            using (var cancel = new InputEventKey { Keycode = Key.Escape, Pressed = true })
                viewport.PushInput(cancel, inLocalCoordinates: true);
            Check(!viewport.IsGUIDragging() && !viewport.IsGUIDragSuccessful() && source.Preview.IsDisposed,
                "The typed ui_cancel action ends a drag and destroys its preview.");
            using (var press = new InputEventMouseButton { Position = new(10, 10), ButtonIndex = MouseButton.Left, Pressed = true })
                viewport.PushInput(press, inLocalCoordinates: true);
            using (var motion = new InputEventMouseMotion { Position = new(10, 10), Relative = new(float.NaN, 0), ButtonMask = MouseButtonMask.Left })
                Reject<AggregateException>(() => viewport.PushInput(motion, inLocalCoordinates: true));
            Check(!viewport.IsGUIDragging() && source.Requests == 1,
                "Nonfinite pointer travel rejects automatic drag without poisoning the next input state.");
            using (var release = new InputEventMouseButton { Position = new(10, 10), ButtonIndex = MouseButton.Left, Pressed = false })
                viewport.PushInput(release, inLocalCoordinates: true);
            Reject<ArgumentNullException>(() => new DragPayload<string>(null!));
            Reject<ArgumentException>(() => source.GetDragData(new(float.NaN, 0)));
            Reject<ArgumentException>(() => source.CanDropData(new(float.NaN, 0), new DragPayload<int>(1)));
            Reject<ArgumentNullException>(() => source.ForceDrag(null!));
            Reject<InvalidOperationException>(() => source.SetDragPreview(new Control()));
            using var parented = new Control { Name = "Parented" }; source.AddChild(parented);
            Reject<ArgumentException>(() => source.ForceDrag(new DragPayload<int>(1), parented));
            Check(!viewport.IsGUIDragging(), "A rejected parented preview leaves no half-started drag.");
        }
        finally { settings.Set(ProjectSettings.DefaultGUIDragThreshold, previous); }

        using var packed = new PackedScene();
        using var window = new Window { GUIDragThreshold = 23 };
        packed.Pack(window);
        using var copy = (Window)packed.Instantiate();
        Check(copy.GUIDragThreshold == 23,
            "The root window retains an explicit typed drag threshold in scene capture.");
    }

    private static void VerifyActivationRollback()
    {
        var viewport = new TestViewport();
        var source = new DragSource { Name = "Source", Size = new(40, 40) };
        var preview = new Control { Name = "Preview", Size = new(8, 8) };
        viewport.AddChild(source);
        source.Ready += _ =>
        {
            source.ForceDrag(new DragPayload<int>(4), preview);
            throw new ApplicationException("expected activation failure");
        };
        Reject<AggregateException>(() => new SceneTree(viewport));
        Check(preview.IsDisposed && viewport.Tree is null && !viewport.IsDisposed,
            "A failed scene activation closes a drag preview before hierarchy rollback and preserves the detached root.");
        viewport.Dispose();
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 320, 240);
    }

    private sealed class DragSource : Control
    {
        internal int Requests, BeginCount, EndCount;
        internal Vector2 Origin;
        internal Control Preview = null!;
        internal bool ReturnNull;
        internal bool ThrowOnBegin;

        protected override DragPayload? OnGetDragData(Vector2 atPosition)
        {
            Requests++; Origin = atPosition;
            Preview = new Control { Name = "Preview", Size = new(8, 8) };
            SetDragPreview(Preview);
            GetViewport()!.SetGUIDragDescription("sample");
            if (ReturnNull) return null;
            return new DragPayload<string>("sample");
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationDragBegin)
            {
                BeginCount++;
                if (ThrowOnBegin) throw new ApplicationException("expected begin failure");
            }
            if (what == NotificationDragEnd) EndCount++;
        }
    }

    private sealed class DropTarget : Control
    {
        internal int CanDropCount, Drops, BeginCount, EndCount;
        internal Vector2 DropPosition;
        internal bool ThrowOnDrop;

        protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload)
        {
            CanDropCount++;
            return payload is DragPayload<string> && atPosition == new Vector2(10, 10);
        }

        protected override void OnDropData(Vector2 atPosition, DragPayload payload)
        {
            DropPosition = atPosition; Drops++;
            if (ThrowOnDrop) throw new ApplicationException("expected drop failure");
        }

        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationDragBegin) BeginCount++;
            if (what == NotificationDragEnd) EndCount++;
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
