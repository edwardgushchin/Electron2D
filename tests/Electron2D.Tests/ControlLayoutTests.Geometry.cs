using Electron2D;

internal static partial class ControlLayoutTests
{
    private static void VerifyGeometry()
    {
        using var viewport = new TestViewport();
        var parent = new Control { Position = new(10, 20) };
        var bridge = new Entity { Position = new(3, 4), Scale = new(2, 1) };
        var control = new Control();
        Check(control.Rotation == 0 && control.RotationDegrees == 0 && control.Scale == Vector2.One
            && control.PivotOffset == Vector2.Zero && control.PivotOffsetRatio == Vector2.Zero
            && control.GetTransform().IsEqualApprox(Transform.Identity), "Control geometry defaults.");
        viewport.AddChild(parent);
        parent.AddChild(bridge);
        parent.AddChild(control);
        Check(control.GetParentControl() == parent && control.GetParentAreaSize() == Vector2.Zero,
            "Detached Control ancestry remains queryable.");
        control.Reparent(bridge, keepGlobalTransform: false);
        using var tree = new SceneTree(viewport);

        control.Position = new(7, 9);
        control.Size = new(20, 12);
        control.PivotOffset = new(2, 3);
        control.PivotOffsetRatio = new(.5f, .25f);
        control.RotationDegrees = 90;
        control.Scale = new(-2, 3);
        Check(Mathf.IsEqualApprox(control.Rotation, Mathf.Pi / 2), "Degrees and radians share state.");
        Near(control.GetCombinedPivotOffset(), new(12, 6));
        var local = control.GetTransform();
        Near(local.X, new(0, -2));
        Near(local.Y, new(-3, 0));
        Near(local * new Vector2(12, 6), new(19, 15));
        Near(control.GetRect().Position, new(37, 39));
        Near(control.GetRect().Size, new(40, -36));
        Near(control.GetGlobalRect().Position, new(87, 63));
        Near(control.GetGlobalRect().Size, new(40, -72));

        var original = control.GetGlobalRect();
        control.Reparent(parent);
        Check(control.GetParentControl() == parent, "The direct parent changes on reparent.");
        Near(control.GetGlobalRect().Position, original.Position);
        Near(control.GetGlobalRect().Size, new(40, -36));
        control.TopLevel = true;
        Check(control.GetParentControl() == parent && control.GetParentAreaSize() == viewport.VisibleSize,
            "TopLevel cuts canvas ancestry and retains the direct node parent.");
        Near(control.GetGlobalRect().Position, control.GetRect().Position);

        Reject<ArgumentOutOfRangeException>(() => control.PivotOffset = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => control.Scale = new(0, float.NegativeInfinity));
        Reject<ArgumentOutOfRangeException>(() => control.Rotation = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => control.RotationDegrees = float.NaN);
        Check(control.PivotOffset == new Vector2(2, 3) && control.Scale == new Vector2(-2, 3)
            && Mathf.IsEqualApprox(control.RotationDegrees, 90), "Rejected edits retain the previous state.");
        control.Scale = new(0, -2);
        Check(control.Scale == new Vector2(.00001f, -2) && control.GetTransform().Determinant() < 0,
            "Zero scale normalizes per axis without removing reflection.");
        control.Rotation = -Mathf.Pi;
        Check(Mathf.IsEqualApprox(control.RotationDegrees, -180), "The radians setter updates degrees.");
        using var scene = new PackedScene();
        scene.Pack(control);
        using var copy = (Control)scene.Instantiate();
        Check(copy.Scale == control.Scale && copy.PivotOffset == control.PivotOffset
            && copy.PivotOffsetRatio == control.PivotOffsetRatio && copy.RotationDegrees == control.RotationDegrees,
            "Scene storage preserves signed and normalized scale, both pivots and rotation.");
        Reject<InvalidOperationException>(() => Task.Run(() => _ = control.GetParentAreaSize()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = control.GetParentControl()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = control.Rotation).GetAwaiter().GetResult());
        var globalRect = control.GetGlobalRect();
        var ownerThread = Environment.CurrentManagedThreadId;
        var readFromGroup = false;
        viewport.AddChild(new GeometryReadProbe(() =>
        {
            Check(Environment.CurrentManagedThreadId != ownerThread && control.GetParentControl() == parent
                && control.GetParentAreaSize() == viewport.VisibleSize && control.GetGlobalRect() == globalRect
                && control.Scale == new Vector2(.00001f, -2) && control.Rotation == -Mathf.Pi
                && control.PivotOffset == new Vector2(2, 3) && control.PivotOffsetRatio == new Vector2(.5f, .25f),
                "A foreign processing group observes the owner’s logical geometry.");
            readFromGroup = true;
        }));
        tree.ProcessFrame(.01);
        Check(readFromGroup, "The processing group ran the read probe.");

        _ = control.GetGlobalRect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++)
        {
            _ = control.GetParentControl();
            _ = control.GetParentAreaSize();
            _ = control.GetCombinedPivotOffset();
            _ = control.GetTransform();
            _ = control.GetRect();
            _ = control.GetGlobalRect();
        }
        var queryBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(queryBytes == 0, "Warmed Control geometry queries allocated.");

        var resized = 0;
        control.Resized += () => resized++;
        for (var i = 0; i < 32; i++) control.Size = new(20 + (i & 1), 12);
        resized = 0;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++) control.Size = new(20 + (i & 1), 12);
        var resizeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(resized == 256 && resizeBytes == 0, $"Warmed Control resize callbacks: {resized} events, {resizeBytes} managed bytes.");
        Console.WriteLine($"Control geometry: 256 warmed query sets and 256 layout mutations with Resized at {queryBytes}/{resizeBytes} managed bytes.");
    }

    private static void VerifyParentGeometry()
    {
        using var texture = new GradientTexture { Width = 40, Height = 24 };
        using var viewport = new TestViewport();
        var parent = new Control { Size = new(80, 60) };
        var sprite = new Sprite { Texture = texture, Offset = new(10, 0) };
        using var frames = new SpriteFrames();
        frames.AddFrame("default", texture);
        var animated = new AnimatedSprite { SpriteFrames = frames };
        var copyArea = new BackBufferCopy { Rect = new(-2, -3, 32, 16) };
        var light = new PointLight { Texture = texture, TextureScale = .5f };
        var touch = new TouchScreenButton { TextureNormal = texture };
        viewport.AddChild(animated);
        viewport.AddChild(copyArea);
        viewport.AddChild(light);
        viewport.AddChild(touch);
        var neutral = new Node();
        var plain = new Entity();
        var child = new Control { AnchorRight = 1, AnchorBottom = 1 };
        viewport.AddChild(parent);
        parent.AddChild(sprite);
        parent.AddChild(neutral);
        parent.AddChild(plain);
        sprite.AddChild(child);
        Check(child.GetParentControl() is null && child.GetParentAreaSize() == Vector2.Zero,
            "Detached non-Control parent supplies no layout area.");
        using var tree = new SceneTree(viewport);
        Check(child.GetParentControl() is null && child.GetParentAreaSize() == new Vector2(40, 24)
            && child.Size == new Vector2(40, 24), "Sprite frame bounds supply the anchor size.");
        sprite.HFrames = 2;
        Check(child.GetParentAreaSize() == new Vector2(20, 24) && child.Size == new Vector2(20, 24),
            "Sprite frame edits reflow anchored children.");
        child.LayoutDirection = LayoutDirection.RTL;
        sprite.Offset = new(15, 0);
        Near(child.Position, new(10, 0));
        child.LayoutDirection = LayoutDirection.LTR;

        child.Reparent(animated, keepGlobalTransform: false);
        Check(child.Size == new Vector2(40, 24), "The active animation frame supplies the anchor size.");
        animated.SpriteFrames = null;
        VerifyClearedArea(new(40, 24));
        child.Reparent(copyArea, keepGlobalTransform: false);
        Check(child.Size == new Vector2(32, 16), "BackBufferCopy supplies its authored rectangle.");
        copyArea.Rect = new(1, 2, 30, 14);
        Check(child.Size == new Vector2(30, 14), "Changing the copy rectangle reflows the child.");
        child.Reparent(light, keepGlobalTransform: false);
        Check(child.Size == new Vector2(20, 12), "Point light supplies its scaled mask bounds.");
        light.TextureScale = 2;
        Check(child.Size == new Vector2(80, 48), "Changing the light's texture scale reflows the child.");
        light.Texture = null;
        VerifyClearedArea(new(80, 48));
        child.Reparent(touch, keepGlobalTransform: false);
        Check(child.Size == new Vector2(40, 24), "TouchScreenButton supplies its normal texture size.");
        touch.TextureNormal = null;
        VerifyClearedArea(new(40, 24));
        child.Reparent(plain, keepGlobalTransform: false);
        Check(child.GetParentControl() is null && child.GetParentAreaSize() == Vector2.Zero && child.Size == Vector2.Zero,
            "A plain canvas parent supplies a zero-size area.");
        child.Reparent(neutral, keepGlobalTransform: false);
        Check(child.GetParentControl() is null && child.GetParentAreaSize() == viewport.VisibleSize
            && child.Size == viewport.VisibleSize, "A neutral boundary uses the viewport.");
        child.Reparent(parent, keepGlobalTransform: false);
        Check(child.GetParentControl() == parent && child.GetParentAreaSize() == parent.Size && child.Size == parent.Size,
            "A Control supplies the untransformed layout size.");
        sprite.HFrames = 1;
        Check(child.Size == parent.Size, "Reparent disconnects the former layout source.");
        parent.RemoveChild(child);
        Check(child.GetParentControl() is null && child.GetParentAreaSize() == Vector2.Zero, "Detached parent queries.");
        child.Dispose();
        Reject<ObjectDisposedException>(() => child.GetParentControl());
        Reject<ObjectDisposedException>(() => child.GetParentAreaSize());
        Reject<ObjectDisposedException>(() => child.GetGlobalRect());

        void VerifyClearedArea(Vector2 previousSize)
        {
            Check(child.GetParentAreaSize() == Vector2.Zero && child.Size == previousSize,
                "Resource clearing publishes the live area and keeps the resolved size until reflow.");
            child.AnchorRight = .5f;
            Check(child.Size == Vector2.Zero, "A later anchor edit reflows against the empty area.");
            child.AnchorRight = 1;
        }
    }

    private static void VerifyResizeDelivery()
    {
        using var viewport = new TestViewport();
        var order = new List<string>(8);
        var control = new ResizeControl(order) { PivotOffsetRatio = new(.5f, 0), RotationDegrees = 180 };
        control.ItemRectChanged += _ =>
        {
            Check(control.GetGlobalRect().Position.DistanceTo(new(control.Size.X, 0)) < .00001f,
                "Callbacks observe the recalculated pivot and global origin.");
            order.Add("rect");
        };
        Action reenter = () =>
        {
            order.Add("resized");
            if (control.Size.X == 20) control.Size = new(30, 10);
        };
        control.Resized += reenter;
        control.Size = new(10, 10);
        Check(order.Count == 0, "Detached edits do not emit layout events.");
        viewport.AddChild(control);
        using var tree = new SceneTree(viewport);
        control.Size = new(20, 10);
        Check(control.Size == new Vector2(30, 10)
            && order.SequenceEqual(new[] { "rect", "notification", "resized", "rect", "notification", "resized" }),
            "A reentrant Resized edit commits and delivers its own ordered callbacks.");
        order.Clear();
        control.SetSize(control.Size);
        control.SetSize(new(MathF.BitIncrement(control.Size.X), control.Size.Y));
        control.PivotOffset = new(2, 0);
        Check(control.Size.X == MathF.BitIncrement(30) && order.Count == 0,
            "Insignificant size edits commit exactly, and pivot-only changes do not emit Resized.");
        control.PivotOffset = Vector2.Zero;
        control.Resized -= reenter;
        Action fail = () => throw new InvalidOperationException("resize observer failure");
        control.Resized += fail;
        Reject<InvalidOperationException>(() => control.Size = new(40, 10));
        Check(control.Size.X == 40, "A failing observer leaves the new geometry committed.");
        control.Resized -= fail;
        order.Clear();
        control.Size = new(50, 10);
        Check(order.SequenceEqual(new[] { "rect", "notification" }), "Removed subscriptions stay removed after failure.");

        order.Clear();
        control.ItemRectChanged += _ => control.Dispose();
        control.Size = new(60, 10);
        Check(control.IsDisposed && order.SequenceEqual(new[] { "rect" }),
            "Disposal from ItemRectChanged prevents notification on the released control.");

        VerifyTransformDisposal(node => node.Size = new(20, 10));
        VerifyTransformDisposal(node => node.Rotation = .5f);
        VerifyTransformDisposal(node => node.Scale = new(2, 3));
        VerifyTransformDisposal(node => node.PivotOffset = new(2, 1));
        VerifyTransformDisposal(node => node.PivotOffsetRatio = new(.5f, .5f));
        VerifyTransformDisposal(node => node.OffsetTransformEnabled = true);
        VerifyTransformDisposal(node => node.OffsetTransformPosition = new(3, 4), offsetTransformEnabled: true);

        void VerifyTransformDisposal(Action<Control> mutate, bool offsetTransformEnabled = false)
        {
            using var node = new Control { Size = new(10, 10), OffsetTransformEnabled = offsetTransformEnabled };
            viewport.AddChild(node);
            node.NotifyLocalTransformChanges = true;
            node.LocalTransformChanged += _ => node.Dispose();
            node.ItemRectChanged += _ => throw new InvalidOperationException("A disposed control must not publish its rectangle.");
            node.Resized += () => throw new InvalidOperationException("A disposed control must not publish a resize.");
            mutate(node);
            Check(node.IsDisposed && node.Parent is null, "Local transform callbacks can dispose the control during geometry edits.");
        }
    }

    private sealed class GeometryReadProbe : Node
    {
        private readonly Action _read;

        internal GeometryReadProbe(Action read)
        {
            _read = read;
            ProcessThreadGroup = ProcessThreadGroupMode.SubThread;
        }

        protected override void OnProcess(double delta) => _read();
    }

    private sealed class ResizeControl(List<string> order) : Control
    {
        protected override void OnNotification(int what)
        {
            if (what == NotificationResized) order.Add("notification");
            base.OnNotification(what);
        }
    }
}
