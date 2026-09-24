using System.Globalization;
using Electron2D;

internal static class ControlLayoutTests
{
    internal static void Run()
    {
        Check(typeof(Control).BaseType == typeof(CanvasItem), "Control is a canvas sibling of Entity.");
        var viewport = new TestViewport();
        var parent = new Control { Position = new(10, 20), Size = new(100, 80) };
        var child = new Control { Position = new(5, 6), Size = new(20, 10) };
        var sprite = new Sprite { Position = new(2, 3) };
        viewport.AddChild(parent); parent.AddChild(child); child.AddChild(sprite);
        child.Owner = parent; sprite.Owner = parent;
        child.SetAnchor(Side.Right, 1);
        child.SetOffset(Side.Right, -5);
        var order = new List<string>();
        child.ItemRectChanged += _ => { Check(child.Size.X == parent.Size.X - 10, "Rectangle is committed before callbacks."); order.Add("rect"); };
        child.Resized += () => order.Add("resized");
        using (var tree = new SceneTree(viewport))
        {
            Check(parent.GetParentAreaSize() == new Vector2(200, 150) && child.GetParentControl() == parent,
                "Viewport and direct parent provide layout areas.");
            Check(child.Position == new Vector2(5, 6) && child.Size == new Vector2(90, 10), "Anchors resolve on attachment.");
            Check(sprite.GlobalPosition == new Vector2(17, 29), "Spatial children inherit a control transform.");
            Reject<InvalidOperationException>(() => Task.Run(() => _ = child.GetTransform()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => _ = sprite.GetTransform()).GetAwaiter().GetResult());
            order.Clear();
            parent.Size = new(140, 80);
            Check(child.Size == new Vector2(130, 10) && order.SequenceEqual(new[] { "rect", "resized" }),
                "Parent resize updates descendants and delivers ordered notifications.");
            parent.SetAnchor(Side.Right, 1);
            parent.SetOffset(Side.Right, -10);
            viewport.VisibleSize = new(240, 150);
            viewport.NotifySizeChanged();
            Check(parent.Size.X == 220 && child.Size.X == 210, "Viewport resize cascades through controls.");
            using var packed = new PackedScene(); packed.Pack(parent);
            using var copy = (Control)packed.Instantiate();
            var copiedChild = (Control)copy.GetChild(0);
            Check(copy.AnchorRight == 1 && copy.OffsetRight == -10 && copiedChild.AnchorRight == 1 && copiedChild.OffsetRight == -5,
                "Packed controls retain anchors and offsets, including nested controls.");
            child.PivotOffset = new(10, 0);
            child.RotationDegrees = 90;
            Near(child.GetTransform().Origin, new(15, -4));
            child.GlobalPosition = new(40, 50);
            Near(child.GlobalPosition, new(40, 50));
            child.Scale = Vector2.Zero;
            Check(child.Scale.X > 0 && child.Scale.Y > 0, "Zero scale has an invertible replacement.");
            Action fail = () => throw new InvalidOperationException("resized callback");
            child.Resized += fail;
            Reject<InvalidOperationException>(() => parent.Size = new(230, 80));
            Check(parent.Size.X == 230 && child.Size.X == 220, "A failed resize callback sees committed layout.");
            child.Resized -= fail;
            var movable = new Control { Name = "movable", Position = new(3, 4), Size = new(5, 6) };
            var destination = new Control { Name = "destination", Position = new(60, 30), Size = new(40, 30) };
            parent.AddChild(movable); viewport.AddChild(destination);
            var beforeMove = movable.GlobalPosition;
            movable.Reparent(destination);
            Near(movable.GlobalPosition, beforeMove);
            using var singular = new Entity { Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero) };
            viewport.AddChild(singular);
            Reject<InvalidOperationException>(() => movable.Reparent(singular));
            Check(movable.Parent == destination, "Singular destination rejects before hierarchy mutation.");
            Reject<ArgumentOutOfRangeException>(() => child.SetAnchor((Side)10, 0));
            Reject<ArgumentOutOfRangeException>(() => child.SetOffset(Side.Left, float.NaN));
            using var negative = new Control { Size = new(5, 5) };
            negative.Size = new(-1, 0);
            Check(negative.Size == Vector2.Zero, "Negative finite size requests clamp to the effective minimum.");
            Reject<InvalidOperationException>(() => Task.Run(() => child.Position = Vector2.Zero).GetAwaiter().GetResult());
        }
        VerifyAnchorPresets();
        VerifyMinimumSize();
        VerifyMaximumSize();
        VerifyLayoutDirection();
        VerifyOffsetPresets();
        VerifyLayoutEditing();
        Check(child.IsDisposed && parent.IsDisposed, "Tree disposal releases controls.");
        Console.WriteLine("Control layout checks passed.");
    }

    private static void VerifyAnchorPresets()
    {
        var expected = new (float Left, float Top, float Right, float Bottom)[]
        {
            (0, 0, 0, 0), (1, 0, 1, 0), (0, 1, 0, 1), (1, 1, 1, 1),
            (0, .5f, 0, .5f), (.5f, 0, .5f, 0), (1, .5f, 1, .5f), (.5f, 1, .5f, 1),
            (.5f, .5f, .5f, .5f), (0, 0, 0, 1), (0, 0, 1, 0), (1, 0, 1, 1),
            (0, 1, 1, 1), (.5f, 0, .5f, 1), (0, .5f, 1, .5f), (0, 0, 1, 1)
        };
        using var viewport = new TestViewport();
        var parent = new Control { Size = new(120, 80) };
        viewport.AddChild(parent);
        using var tree = new SceneTree(viewport);
        for (var index = 0; index < expected.Length; index++)
        {
            var item = new Control { Name = $"preset{index}", Position = new(12, 18), Size = new(20, 10) };
            parent.AddChild(item);
            item.SetAnchorsPreset((LayoutPreset)index);
            var anchors = expected[index];
            Check((item.AnchorLeft, item.AnchorTop, item.AnchorRight, item.AnchorBottom) == anchors,
                $"Preset {index} must use the pinned four-anchor arrangement.");
            Check(item.Position == new Vector2(12, 18) && item.Size == new Vector2(20, 10),
                $"Preset {index} must preserve the current rectangle by default.");
        }

        var stretch = new Control { Name = "stretch", Position = new(12, 18), Size = new(20, 10) };
        parent.AddChild(stretch);
        stretch.SetAnchorsPreset(LayoutPreset.FullRect, keepOffsets: true);
        Check(stretch.OffsetLeft == 12 && stretch.OffsetTop == 18 && stretch.OffsetRight == 32 && stretch.OffsetBottom == 28,
            "Keep-offset mode must preserve all four offsets.");
        Check(stretch.Position == new Vector2(12, 18) && stretch.Size == new Vector2(140, 90),
            "Full-rectangle anchors must resolve against the live parent area.");
        parent.Size = new(150, 110);
        Check(stretch.Size == new Vector2(170, 120), "Preset anchors must follow a later parent resize.");
        stretch.SetAnchorsPreset(LayoutPreset.TopLeft);
        Check(stretch.Position == new Vector2(12, 18) && stretch.Size == new Vector2(170, 120),
            "Changing an existing preset must preserve the current rectangle by default.");
        var before = (stretch.AnchorLeft, stretch.AnchorTop, stretch.AnchorRight, stretch.AnchorBottom);
        Reject<ArgumentOutOfRangeException>(() => stretch.SetAnchorsPreset((LayoutPreset)16));
        Check(before == (stretch.AnchorLeft, stretch.AnchorTop, stretch.AnchorRight, stretch.AnchorBottom),
            "An invalid preset must not mutate any anchor.");
    }

    private static void VerifyMinimumSize()
    {
        using var viewport = new TestViewport();
        var host = new Control { Name = "host", Size = new(100, 80) };
        viewport.AddChild(host);
        var child = new MinimumControl { Name = "minimum", Position = new(10, 12), Size = new(20, 10) };
        host.AddChild(child);
        using var tree = new SceneTree(viewport);
        Check(child.GetMinimumSize() == Vector2.Zero && child.GetCombinedMinimumSize() == Vector2.Zero,
            "A base control has no intrinsic or custom minimum size.");
        var events = new List<string>();
        child.Resized += () => events.Add("resized");
        child.MinimumSizeChanged += () => events.Add("minimum");
        child.CustomMinimumSize = new(40, 30);
        Check(child.GetCombinedMinimumSize() == new Vector2(40, 30) && child.Size == new Vector2(20, 10),
            "The new bound is queryable before its deferred layout update.");
        tree.FlushDeferred();
        Check(child.Position == new Vector2(10, 12) && child.Size == new Vector2(40, 30),
            "Default growth keeps the leading edges and enlarges the rectangle.");
        Check(events.SequenceEqual(new[] { "resized", "minimum" }),
            "Resize notifications precede minimum-size change delivery.");
        events.Clear();
        child.CustomMinimumSize = new(50, 35);
        child.CustomMinimumSize = new(60, 40);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(60, 40) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "Multiple changes coalesce to the latest minimum and one signal.");
        events.Clear();
        child.CustomMinimumSize = new(60, 40);
        child.UpdateMinimumSize();
        tree.FlushDeferred();
        Check(events.Count == 0, "An unchanged minimum does not raise another signal.");

        child.SetIntrinsicMinimum(new(75, 15));
        Check(child.GetMinimumSize() == new Vector2(75, 15) && child.GetCombinedMinimumSize() == new Vector2(75, 40),
            "Intrinsic and custom minima combine independently per component.");
        tree.FlushDeferred();
        Check(child.Size == new Vector2(75, 40) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "A derived control can invalidate its intrinsic minimum.");
        events.Clear();
        child.Visible = false;
        child.CustomMinimumSize = new(90, 50);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(75, 40) && events.Count == 0,
            "A hidden control defers minimum-size layout and notifications.");
        child.Visible = true;
        tree.FlushDeferred();
        Check(child.Size == new Vector2(90, 50) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "Showing the control applies its pending minimum-size change.");
        Reject<ArgumentOutOfRangeException>(() => child.CustomMinimumSize = new(float.NaN, 1));
        Check(child.CustomMinimumSize == new Vector2(90, 50), "Invalid custom minima preserve the prior value.");
        Reject<ArgumentOutOfRangeException>(() => child.GrowHorizontal = (GrowDirection)3);
        Check(child.GrowHorizontal == GrowDirection.End, "Invalid growth modes preserve the prior value.");

        var shifts = new[] { 1f, 0f, .5f };
        for (var index = 0; index < shifts.Length; index++)
        {
            var direction = (GrowDirection)index;
            var item = new Control { Name = $"grow{index}", Position = new(10, 12), Size = new(20, 10) };
            host.AddChild(item);
            item.GrowHorizontal = direction;
            item.GrowVertical = direction;
            item.CustomMinimumSize = new(40, 30);
            tree.FlushDeferred();
            Check(item.Size == new Vector2(40, 30) && item.Position == new Vector2(10 - 20 * shifts[index], 12 - 20 * shifts[index]),
                $"Growth direction {direction} must expand from the selected edges.");
        }

        var packable = new Control { Name = "packable", CustomMinimumSize = new(25, 15), GrowHorizontal = GrowDirection.Both };
        host.AddChild(packable);
        using var packed = new PackedScene();
        packed.Pack(packable);
        using var copy = (Control)packed.Instantiate();
        Check(copy.CustomMinimumSize == new Vector2(25, 15) && copy.GrowHorizontal == GrowDirection.Both,
            "Minimum size and growth policy survive packed-scene storage.");

        var entering = new Control { Name = "entering", Size = new(10, 10) };
        entering.SetAnchor(Side.Right, 1);
        entering.SetOffset(Side.Right, -10);
        entering.Resized += () => entering.CustomMinimumSize = new(120, 20);
        host.AddChild(entering);
        tree.FlushDeferred();
        Check(entering.Size == new Vector2(120, 20),
            "A minimum-size change during entry resize must survive the initial layout snapshot.");
    }

    private sealed class MinimumControl : Control
    {
        private Vector2 _minimum;
        protected override Vector2 OnGetMinimumSize() => _minimum;
        internal void SetIntrinsicMinimum(Vector2 value) { _minimum = value; UpdateMinimumSize(); }
    }

    private static void VerifyMaximumSize()
    {
        using var viewport = new TestViewport();
        var host = new Control { Name = "host", Size = new(100, 80) };
        viewport.AddChild(host);
        var child = new MaximumControl { Name = "maximum", Position = new(10, 12), Size = new(80, 50) };
        host.AddChild(child);
        using var tree = new SceneTree(viewport);
        Check(child.GetMaximumSize() == new Vector2(-1, -1) && child.GetCombinedMaximumSize() == new Vector2(-1, -1),
            "A base control is unbounded on both axes.");
        var events = new List<string>();
        child.Resized += () => events.Add("resized");
        child.MaximumSizeChanged += () => events.Add("maximum");
        child.CustomMaximumSize = new(30, 25);
        Check(child.GetCombinedMaximumSize() == new Vector2(30, 25) && child.Size == new Vector2(80, 50),
            "The new maximum is queryable before deferred reflow.");
        tree.FlushDeferred();
        Check(child.Position == new Vector2(10, 12) && child.Size == new Vector2(30, 25),
            "Default growth policy keeps leading edges when the maximum shrinks the rectangle.");
        Check(events.SequenceEqual(new[] { "resized", "maximum" }),
            "Resize delivery precedes the maximum-size signal.");
        events.Clear();
        child.CustomMaximumSize = new(25, 20);
        child.CustomMaximumSize = new(20, 15);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(20, 15) && events.SequenceEqual(new[] { "resized", "maximum" }),
            "Multiple maximum changes coalesce to one applied value and signal.");
        child.CustomMaximumSize = new(20, 15);
        child.UpdateMaximumSize();
        events.Clear();
        tree.FlushDeferred();
        Check(events.Count == 0, "An unchanged maximum does not emit again.");
        child.CustomMaximumSize = new(-8, 15);
        tree.FlushDeferred();
        Check(child.CustomMaximumSize == new Vector2(-1, 15) && child.Size == new Vector2(80, 15),
            "Negative custom maximum components normalize to an unbounded axis.");
        child.SetIntrinsicMaximum(new(35, -1));
        tree.FlushDeferred();
        Check(child.GetMaximumSize() == new Vector2(35, -1) && child.GetCombinedMaximumSize() == new Vector2(35, 15)
            && child.Size == new Vector2(35, 15), "Intrinsic and custom bounds combine per component.");
        Reject<ArgumentOutOfRangeException>(() => child.CustomMaximumSize = new(float.PositiveInfinity, 10));
        Check(child.CustomMaximumSize == new Vector2(-1, 15), "Invalid maximum values preserve the prior bound.");
        events.Clear();
        child.Visible = false;
        child.CustomMaximumSize = new(10, 10);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(35, 15) && events.Count == 0,
            "A hidden control defers maximum-size reflow and notifications.");
        child.Visible = true;
        tree.FlushDeferred();
        Check(child.Size == new Vector2(10, 10) && events.SequenceEqual(new[] { "resized", "maximum" }),
            "Showing the control applies its pending maximum-size change.");

        var shifts = new[] { 1f, 0f, .5f };
        for (var index = 0; index < shifts.Length; index++)
        {
            var direction = (GrowDirection)index;
            var item = new Control { Name = $"shrink{index}", Position = new(10, 12), Size = new(80, 50) };
            host.AddChild(item);
            item.GrowHorizontal = direction;
            item.GrowVertical = direction;
            item.CustomMaximumSize = new(30, 20);
            tree.FlushDeferred();
            Check(item.Size == new Vector2(30, 20) && item.Position == new Vector2(10 + 50 * shifts[index], 12 + 30 * shifts[index]),
                $"Maximum-size growth direction {direction} must keep the selected edges fixed.");
        }

        var conflict = new Control { Name = "conflict", Size = new(80, 50) };
        host.AddChild(conflict);
        conflict.CustomMinimumSize = new(50, 40);
        conflict.CustomMaximumSize = new(30, 25);
        tree.FlushDeferred();
        Check(conflict.GetBoundMinimumSize() == new Vector2(30, 25) && conflict.Size == new Vector2(30, 25),
            "An enabled maximum wins after minimum-size growth when bounds conflict.");

        var parent = new Control { Name = "bounded", Size = new(100, 80) };
        viewport.AddChild(parent);
        var nested = new Control { Name = "nested", Size = new(80, 50) };
        parent.AddChild(nested);
        parent.CustomMaximumSize = new(40, 30);
        parent.PropagateMaximumSize = true;
        tree.FlushDeferred();
        Check(parent.Size == new Vector2(40, 30) && nested.GetCombinedMaximumSize() == new Vector2(40, 30)
            && nested.Size == new Vector2(40, 30), "Parent propagation bounds a direct child control.");
        nested.TopLevel = true;
        Check(nested.GetCombinedMaximumSize() == new Vector2(-1, -1) && nested.Size == new Vector2(80, 50),
            "A top-level child escapes the parent's propagated maximum.");
        nested.TopLevel = false;
        Check(nested.GetCombinedMaximumSize() == new Vector2(40, 30) && nested.Size == new Vector2(40, 30),
            "Rejoining the parent canvas restores its propagated maximum.");
        parent.PropagateMaximumSize = false;
        tree.FlushDeferred();
        Check(nested.GetCombinedMaximumSize() == new Vector2(-1, -1) && nested.Size == new Vector2(80, 50),
            "Disabling propagation restores the child's unconstrained rectangle.");

        var packable = new Control { Name = "packable", CustomMaximumSize = new(22, -5), PropagateMaximumSize = true };
        host.AddChild(packable);
        using var packed = new PackedScene();
        packed.Pack(packable);
        using var copy = (Control)packed.Instantiate();
        Check(copy.CustomMaximumSize == new Vector2(22, -1) && copy.PropagateMaximumSize,
            "Maximum-size settings survive packed-scene storage.");

        var entering = new Control { Name = "entering", Size = new(10, 10) };
        entering.SetAnchor(Side.Right, 1);
        entering.SetOffset(Side.Right, -10);
        entering.Resized += () => entering.CustomMaximumSize = new(20, 10);
        host.AddChild(entering);
        tree.FlushDeferred();
        Check(entering.Size == new Vector2(20, 10),
            "A maximum-size change during entry resize must survive the initial layout snapshot.");
    }

    private sealed class MaximumControl : Control
    {
        private Vector2 _maximum = new(-1, -1);
        protected override Vector2 OnGetMaximumSize() => _maximum;
        internal void SetIntrinsicMaximum(Vector2 value) { _maximum = value; UpdateMaximumSize(); }
    }

    private static void VerifyLayoutDirection()
    {
        var previousCulture = TranslationServer.Culture;
        var previousUICulture = CultureInfo.CurrentUICulture;
        const string domainName = "control-layout-direction-tests";
        try
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en");
            using var viewport = new TestViewport();
            var notifications = new List<string>();
            var parent = new DirectionControl("parent", notifications) { Name = "parent", Position = new(10, 20), Size = new(100, 80) };
            var child = new DirectionControl("child", notifications) { Name = "child", Position = new(5, 6), Size = new(20, 10) };
            viewport.AddChild(parent);
            parent.AddChild(child);
            using var tree = new SceneTree(viewport);
            Check(!parent.IsLayoutRTL() && !child.IsLayoutRTL() && parent.Position == new Vector2(10, 20),
                "An inherited English layout begins left to right.");
            parent.LayoutDirection = LayoutDirection.RTL;
            Check(parent.IsLayoutRTL() && child.IsLayoutRTL() && parent.Position == new Vector2(90, 20)
                && child.Position == new Vector2(75, 6), "Explicit RTL mirrors parent and inherited child rectangles.");
            Check(notifications.SequenceEqual(new[] { "parent", "child" }),
                "Layout-direction notifications reach controls in parent-first order.");
            notifications.Clear();
            child.LayoutDirection = LayoutDirection.LTR;
            Check(!child.IsLayoutRTL() && child.Position == new Vector2(5, 6),
                "An explicit child LTR direction overrides its RTL parent.");
            child.LayoutDirection = LayoutDirection.Inherited;
            Check(child.IsLayoutRTL() && child.Position == new Vector2(75, 6),
                "Returning to inherited direction restores RTL mirroring.");
            child.Position = new(12, 6);
            child.Size = new(30, 10);
            Check(child.Position == new Vector2(12, 6) && child.Size == new Vector2(30, 10),
                "Position and size setters use physical coordinates under RTL.");
            parent.LayoutDirection = LayoutDirection.LTR;
            Check(!child.IsLayoutRTL() && child.Position == new Vector2(58, 6),
                "Returning to LTR reveals the same stored logical offsets.");
            child.LayoutDirection = LayoutDirection.RTL;
            Check(child.Position == new Vector2(12, 6), "Explicit RTL mirrors independently of its parent.");
            Reject<ArgumentOutOfRangeException>(() => child.LayoutDirection = LayoutDirection.Max);
            Reject<ArgumentOutOfRangeException>(() => child.LayoutDirection = (LayoutDirection)(-1));
            Check(child.LayoutDirection == LayoutDirection.RTL,
                "Invalid direction IDs leave the previous policy unchanged.");

            child.LayoutDirection = LayoutDirection.Inherited;
            child.TranslationDomain = domainName;
            Check(!child.IsLayoutRTL(), "A different translation domain stops inherited parent direction.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("ar");
            child.LayoutDirection = LayoutDirection.ApplicationLocale;
            Check(!child.IsLayoutRTL(), "An RTL locale without a matching catalog remains LTR.");
            var domain = TranslationServer.GetOrAddDomain(domainName);
            using var catalog = new Translation { Locale = "ar" };
            domain.AddTranslation(catalog);
            child.PropagateNotification(Node.NotificationTranslationChanged);
            Check(child.IsLayoutRTL() && child.Position == new Vector2(12, 6),
                "A matching RTL catalog enables application-locale mirroring.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en");
            domain.LocaleOverride = "ar";
            child.PropagateNotification(Node.NotificationTranslationChanged);
            Check(child.IsLayoutRTL(), "A domain locale override selects its RTL catalog independently of the process culture.");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar");
            child.LayoutDirection = LayoutDirection.SystemLocale;
            Check(child.IsLayoutRTL(), "System-locale mode uses the current UI culture.");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            child.PropagateNotification(Node.NotificationTranslationChanged);
            Check(!child.IsLayoutRTL(), "A later LTR system culture changes the resolved direction.");

            var packable = new Control { Name = "packable", Position = new(10, 5), Size = new(20, 10), LayoutDirection = LayoutDirection.RTL };
            parent.AddChild(packable);
            using var packed = new PackedScene();
            packed.Pack(packable);
            using var copy = (Control)packed.Instantiate();
            Check(copy.LayoutDirection == LayoutDirection.RTL && copy.IsLayoutRTL(),
                "An explicit layout direction survives packed-scene storage.");
            copy.Name = "copy";
            parent.AddChild(copy);
            Check(copy.Position == packable.Position && copy.Size == packable.Size,
                "An instantiated RTL control resolves the same rectangle under an equal parent area.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUICulture;
            TranslationServer.Culture = previousCulture;
            TranslationServer.RemoveDomain(domainName);
        }
    }

    private static void VerifyOffsetPresets()
    {
        using var viewport = new TestViewport();
        var parent = new Control { Name = "preset-parent", Size = new(100, 80) };
        viewport.AddChild(parent);
        using var tree = new SceneTree(viewport);
        var widths = new[] { 12f, 20f, 12f, 20f };
        var heights = new[] { 6f, 6f, 10f, 10f };
        for (var direction = 0; direction < 2; direction++)
            for (var mode = 0; mode < 4; mode++)
                for (var index = 0; index < 16; index++)
                {
                    using var item = new MinimumControl { Name = $"offset-{direction}-{mode}-{index}", Position = new(7, 9), Size = new(20, 10) };
                    item.SetIntrinsicMinimum(new(12, 6));
                    parent.AddChild(item);
                    item.LayoutDirection = direction == 0 ? LayoutDirection.LTR : LayoutDirection.RTL;
                    item.SetAnchorsAndOffsetsPreset((LayoutPreset)index, (LayoutPresetMode)mode, margin: 3);
                    var (position, size) = ExpectedPreset((LayoutPreset)index, widths[mode], heights[mode], direction != 0);
                    Check(item.Position == position && item.Size == size,
                        $"Preset {index}, mode {mode}, direction {direction}: expected {position}/{size}, got {item.Position}/{item.Size}.");
                }

        using var standalone = new Control { Name = "standalone", Position = new(7, 9), Size = new(20, 10) };
        parent.AddChild(standalone);
        standalone.SetAnchor(Side.Left, .2f);
        standalone.SetAnchor(Side.Right, .3f);
        var originalAnchors = (standalone.AnchorLeft, standalone.AnchorTop, standalone.AnchorRight, standalone.AnchorBottom);
        var resized = 0;
        standalone.Resized += () => resized++;
        standalone.SetOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize, margin: 3);
        Check((standalone.AnchorLeft, standalone.AnchorTop, standalone.AnchorRight, standalone.AnchorBottom) == originalAnchors,
            "An offset preset must leave existing anchors unchanged.");
        Check(standalone.Position == new Vector2(40, 35) && standalone.Size == new Vector2(20, 10) && resized == 0,
            "Center offset preset positions without resizing or emitting a resize signal.");
        standalone.SetOffsetsPreset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize, margin: -3);
        Check(standalone.Position == new Vector2(-3, -3), "Signed margins shift edge presets in both directions.");
        var before = (standalone.OffsetLeft, standalone.OffsetTop, standalone.OffsetRight, standalone.OffsetBottom);
        Reject<ArgumentOutOfRangeException>(() => standalone.SetOffsetsPreset((LayoutPreset)16));
        Reject<ArgumentOutOfRangeException>(() => standalone.SetOffsetsPreset(LayoutPreset.TopLeft, (LayoutPresetMode)4));
        Check(before == (standalone.OffsetLeft, standalone.OffsetTop, standalone.OffsetRight, standalone.OffsetBottom),
            "Invalid offset preset identities leave every offset unchanged.");
        using var invalidCombined = new Control { Name = "invalid-combined", Position = new(7, 9), Size = new(20, 10) };
        parent.AddChild(invalidCombined);
        Reject<ArgumentOutOfRangeException>(() => invalidCombined.SetAnchorsAndOffsetsPreset(
            LayoutPreset.FullRect, (LayoutPresetMode)4));
        Check(invalidCombined.AnchorRight == 1 && invalidCombined.AnchorBottom == 1
            && invalidCombined.Position == new Vector2(7, 9) && invalidCombined.Size == new Vector2(20, 10),
            "The combined operation commits its anchor step before rejecting an invalid offset mode.");
        standalone.SetAnchorAndOffset(Side.Right, .5f, -4);
        Check(standalone.AnchorRight == .5f && standalone.OffsetRight == -4,
            "Combined side update applies anchor before its explicit offset.");
        standalone.SetAnchorAndOffset(Side.Left, .75f, 2);
        Check(standalone.AnchorLeft == .5f && standalone.AnchorRight == .5f && standalone.OffsetLeft == 2,
            "The default combined update clamps a crossing anchor without pushing its opposite.");
        standalone.SetAnchorAndOffset(Side.Left, .75f, 2, pushOppositeAnchor: true);
        Check(standalone.AnchorLeft == .75f && standalone.AnchorRight == .75f,
            "The optional combined update pushes the opposite anchor when they cross.");
        before = (standalone.OffsetLeft, standalone.OffsetTop, standalone.OffsetRight, standalone.OffsetBottom);
        var anchorBefore = standalone.AnchorLeft;
        Reject<ArgumentOutOfRangeException>(() => standalone.SetAnchorAndOffset(Side.Left, .4f, float.NaN));
        Check(standalone.AnchorLeft == anchorBefore && before == (standalone.OffsetLeft, standalone.OffsetTop, standalone.OffsetRight, standalone.OffsetBottom),
            "Invalid combined side input rolls back before either edit.");

        using var rtlOnly = new Control { Name = "rtl-only", Size = new(20, 10) };
        parent.AddChild(rtlOnly);
        rtlOnly.SetAnchor(Side.Left, .2f);
        rtlOnly.SetAnchor(Side.Right, .3f);
        rtlOnly.LayoutDirection = LayoutDirection.RTL;
        rtlOnly.SetOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize, margin: 3);
        Check(rtlOnly.AnchorLeft == .2f && rtlOnly.AnchorRight == .3f
            && rtlOnly.Position == new Vector2(64, 35) && rtlOnly.Size == new Vector2(32, 10),
            "RTL offset-only placement retains arbitrary anchors and uses the pinned negative horizontal span.");

        using var offsetViewport = new OffsetViewport();
        var root = new Control { Name = "root", Size = new(20, 10) };
        offsetViewport.AddChild(root);
        using var offsetTree = new SceneTree(offsetViewport);
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize, margin: 3);
        Check(root.Position == new Vector2(14, 10), "A viewport's nonzero visible-rectangle origin contributes to preset offsets.");
        root.LayoutDirection = LayoutDirection.RTL;
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize, margin: 3);
        Check(root.Position == new Vector2(88, 10), "RTL mirroring uses the viewport rectangle's nonzero origin.");
        root.Position = new(35, 10);
        root.Size = new(30, 10);
        Check(root.Position == new Vector2(35, 10) && root.Size == new Vector2(30, 10),
            "RTL physical position and size setters remain correct for a translated viewport rectangle.");
    }

    private static (Vector2 Position, Vector2 Size) ExpectedPreset(LayoutPreset preset, float width, float height, bool rtl)
    {
        if (preset is LayoutPreset.TopWide or LayoutPreset.BottomWide or LayoutPreset.HCenterWide or LayoutPreset.FullRect)
            width = 94;
        if (preset is LayoutPreset.LeftWide or LayoutPreset.RightWide or LayoutPreset.VCenterWide or LayoutPreset.FullRect)
            height = 74;
        var x = preset switch
        {
            LayoutPreset.TopRight or LayoutPreset.BottomRight or LayoutPreset.CenterRight or LayoutPreset.RightWide => 97 - width,
            LayoutPreset.CenterTop or LayoutPreset.CenterBottom or LayoutPreset.Center or LayoutPreset.VCenterWide => (100 - width) / 2,
            _ => 3
        };
        var y = preset switch
        {
            LayoutPreset.BottomLeft or LayoutPreset.BottomRight or LayoutPreset.CenterBottom or LayoutPreset.BottomWide => 77 - height,
            LayoutPreset.CenterLeft or LayoutPreset.CenterRight or LayoutPreset.Center or LayoutPreset.HCenterWide => (80 - height) / 2,
            _ => 3
        };
        return (new Vector2(rtl ? 100 - x - width : x, y), new Vector2(width, height));
    }

    private static void VerifyLayoutEditing()
    {
        using var viewport = new TestViewport();
        var parent = new Control { Name = "editing-parent", Position = new(20, 30), Size = new(100, 80) };
        var child = new Control { Name = "editing-child", Position = new(10, 12), Size = new(20, 10) };
        viewport.AddChild(parent);
        parent.AddChild(child);
        using var tree = new SceneTree(viewport);
        var originalOffsets = (child.OffsetLeft, child.OffsetTop, child.OffsetRight, child.OffsetBottom);
        child.SetPosition(new(30, 20), keepOffsets: true);
        Check(child.Position == new Vector2(30, 20) && child.Size == new Vector2(20, 10)
            && child.AnchorLeft == .2f && child.AnchorRight == .2f && child.AnchorTop == .1f && child.AnchorBottom == .1f,
            "Keeping offsets moves anchors to place the unchanged rectangle.");
        Check(originalOffsets == (child.OffsetLeft, child.OffsetTop, child.OffsetRight, child.OffsetBottom),
            "Keep-offset position edits preserve all four stored offsets.");
        parent.Size = new(200, 160);
        Check(child.Position == new Vector2(50, 28) && child.Size == new Vector2(20, 10),
            "Recomputed anchors keep their adaptive position on a later parent resize.");
        child.SetSize(new(40, 20), keepOffsets: true);
        Check(child.Position == new Vector2(50, 28) && child.Size == new Vector2(40, 20)
            && originalOffsets == (child.OffsetLeft, child.OffsetTop, child.OffsetRight, child.OffsetBottom),
            "Keep-offset size edits change anchors while retaining the physical upper-left point.");
        var anchors = (child.AnchorLeft, child.AnchorTop, child.AnchorRight, child.AnchorBottom);
        child.SetPosition(new(15, 25));
        child.SetSize(new(45, 25));
        Check(child.Position == new Vector2(15, 25) && child.Size == new Vector2(45, 25)
            && anchors == (child.AnchorLeft, child.AnchorTop, child.AnchorRight, child.AnchorBottom),
            "Default position and size methods edit offsets without moving anchors.");

        parent.Scale = new(2, 3);
        var beforeGlobalOffsets = (child.OffsetLeft, child.OffsetTop, child.OffsetRight, child.OffsetBottom);
        child.SetGlobalPosition(new(100, 90), keepOffsets: true);
        Near(child.GlobalPosition, new(100, 90));
        Check(beforeGlobalOffsets == (child.OffsetLeft, child.OffsetTop, child.OffsetRight, child.OffsetBottom),
            "Global keep-offset placement converts through the parent transform into new anchors.");

        var edge = new Control { Name = "edge", Position = new(5, 6), Size = new(10, 10) };
        parent.AddChild(edge);
        edge.SetBegin(new(8, 9));
        Check(edge.GetBegin() == new Vector2(8, 9) && edge.Position == new Vector2(8, 9) && edge.Size == new Vector2(7, 7),
            "Begin edits both leading offsets and resolves one rectangle.");
        edge.SetEnd(new(25, 29));
        Check(edge.GetEnd() == new Vector2(25, 29) && edge.Size == new Vector2(17, 20),
            "End edits both trailing offsets and resolves one rectangle.");
        var beforeEdge = (edge.GetBegin(), edge.GetEnd());
        Reject<ArgumentOutOfRangeException>(() => edge.SetBegin(new(float.PositiveInfinity, 1)));
        Reject<ArgumentOutOfRangeException>(() => edge.SetEnd(new(1, float.NaN)));
        Check(beforeEdge == (edge.GetBegin(), edge.GetEnd()), "Invalid edge pairs leave all offsets unchanged.");

        var bounded = new Control { Name = "bounded", Size = new(10, 10), CustomMinimumSize = new(30, 20), CustomMaximumSize = new(40, 25) };
        parent.AddChild(bounded);
        bounded.SetSize(new(5, 50));
        Check(bounded.Size == new Vector2(30, 25), "Explicit size clamps minimum before maximum per axis.");
        bounded.ResetSize();
        Check(bounded.Size == new Vector2(30, 20), "ResetSize resolves the effective minimum under current bounds.");

        using var detached = new Control { Position = new(3, 4), Size = new(5, 6) };
        var beforeDetached = (detached.AnchorLeft, detached.AnchorTop, detached.AnchorRight, detached.AnchorBottom);
        Reject<InvalidOperationException>(() => detached.SetPosition(new(10, 20), keepOffsets: true));
        Reject<InvalidOperationException>(() => detached.SetSize(new(8, 9), keepOffsets: true));
        Check(detached.Position == new Vector2(3, 4) && detached.Size == new Vector2(5, 6)
            && beforeDetached == (detached.AnchorLeft, detached.AnchorTop, detached.AnchorRight, detached.AnchorBottom),
            "A detached zero-area control rejects anchor recomputation before mutation.");
        Reject<ArgumentOutOfRangeException>(() => child.SetPosition(new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => child.SetSize(new(float.NaN, 0)));
        child.SetSize(new(-1, 0));
        Check(child.Size == Vector2.Zero, "The method and property share negative-size clamping.");
        Reject<ArgumentOutOfRangeException>(() => child.SetGlobalPosition(new(0, float.PositiveInfinity)));

        var zeroWidthParent = new Control { Name = "zero-width", Size = new(0, 40) };
        var zeroWidthChild = new Control { Name = "zero-width-child", Position = new(3, 4), Size = new(5, 6) };
        viewport.AddChild(zeroWidthParent);
        zeroWidthParent.AddChild(zeroWidthChild);
        var zeroWidthAnchors = (zeroWidthChild.AnchorLeft, zeroWidthChild.AnchorTop, zeroWidthChild.AnchorRight, zeroWidthChild.AnchorBottom);
        Reject<InvalidOperationException>(() => zeroWidthChild.SetPosition(new(7, 8), keepOffsets: true));
        Reject<InvalidOperationException>(() => zeroWidthChild.SetSize(new(9, 10), keepOffsets: true));
        Reject<InvalidOperationException>(() => zeroWidthChild.SetGlobalPosition(new(11, 12), keepOffsets: true));
        Check(zeroWidthChild.Position == new Vector2(3, 4) && zeroWidthChild.Size == new Vector2(5, 6)
            && zeroWidthAnchors == (zeroWidthChild.AnchorLeft, zeroWidthChild.AnchorTop, zeroWidthChild.AnchorRight, zeroWidthChild.AnchorBottom),
            "An attached zero-width parent rejects every keep-offset edit without changing geometry or anchors.");

        using var singular = new Entity { Name = "singular", Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero) };
        var underSingular = new Control { Name = "under-singular", Position = new(3, 4), Size = new(5, 6) };
        viewport.AddChild(singular);
        singular.AddChild(underSingular);
        Reject<InvalidOperationException>(() => underSingular.SetGlobalPosition(new(11, 12)));
        Check(underSingular.Position == new Vector2(3, 4),
            "A singular parent transform rejects global placement before mutating local geometry.");

        using var offsetViewport = new OffsetViewport();
        var root = new Control { Name = "rtl-edit-root", Size = new(20, 10), LayoutDirection = LayoutDirection.RTL };
        offsetViewport.AddChild(root);
        using var offsetTree = new SceneTree(offsetViewport);
        root.SetPosition(new(35, 10), keepOffsets: true);
        root.SetSize(new(30, 20), keepOffsets: true);
        Check(root.Position == new Vector2(35, 10) && root.Size == new Vector2(30, 20),
            "RTL keep-offset edits preserve physical coordinates under a translated viewport rectangle.");
    }

    private sealed class DirectionControl(string tag, List<string> notifications) : Control
    {
        protected override void OnNotification(int what)
        {
            if (what == NotificationLayoutDirectionChanged) notifications.Add(tag);
            base.OnNotification(what);
        }
    }

    private sealed class TestViewport : Viewport
    {
        internal Vector2 VisibleSize = new(200, 150);
        public override Rect2 GetVisibleRect() => new(Vector2.Zero, VisibleSize);
    }

    private sealed class OffsetViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(new Vector2(11, 7), new Vector2(100, 80));
    }

    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.IsEqualApprox(expected), $"Expected {expected}, got {actual}.");
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
