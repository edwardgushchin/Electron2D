using Electron2D;

internal static class SplitContainerTests
{
    internal static void Run()
    {
        VerifyGeometry(); VerifyOffsets(); VerifyStructure(); VerifyInputAndFailures(); VerifyPackingAndGuards(); VerifyIntersections(); VerifyLayoutFailures(); VerifyWarm();
        Console.WriteLine("Split panels, constraints, offsets, structure, drag/input/failures, packing, nested intersections and zero-allocation checks passed.");
    }
    private static Control Child(string name, float minimum = 10, bool expand = true) => new() { Name = name, CustomMinimumSize = new(minimum, 10), SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
    private static void Advance(SceneTree tree, int frames = 4) { for (var i = 0; i < frames; i++) tree.ProcessFrame(0); }
    private static void VerifyGeometry()
    {
        using var empty = new SplitContainer(); Check(empty.SplitOffsets.SequenceEqual([0]) && empty.GetChildCount() == 0 && empty.GetChildCount(true) == 1 && empty.GetMinimumSize() == Vector2.Zero && !empty.Vertical && !empty.Collapsed && empty.DraggingEnabled, "Split defaults and real internal dragger.");
        using var skin = new Skin(); var split = new HSplitContainer { Theme = skin.Theme, Size = new(100, 40) }; var a = Child("A"); var b = Child("B"); split.AddChild(a); split.AddChild(b); using var tree = new SceneTree(split); Advance(tree);
        Check(a.Size == new Vector2(44, 40) && b.Position == new Vector2(56, 0) && b.Size == a.Size && split.GetMinimumSize() == new Vector2(32, 10), "Two expanded panels split around a twelve-pixel gap.");
        a.SizeFlagsStretchRatio = 3; Advance(tree); Check(a.Size.X == 69 && b.Position.X == 81 && b.Size.X == 19, "Two-panel weighted default ignores minima before bounded clamping.");
        split.LayoutDirection = LayoutDirection.RTL; Advance(tree); Check(a.Position.X == 31 && b.Position.X == 0, "RTL mirrors physical panel allocation.");
        split.DragAreaMarginBegin = 3; split.DragAreaMarginEnd = 5; split.DragAreaOffset = 2; Advance(tree); var handle = split.GetDragAreaControl(); Check(handle.Position == new Vector2(17, 3) && handle.Size == new Vector2(12, 32), "RTL drag offset and cross-axis margins affect only the target/bar.");
        split.DraggerVisibilityMode = SplitContainer.DraggerVisibility.HiddenCollapsed; Advance(tree); Check(split.GetMinimumSize().X == 20 && handle.Size.X == 6, "Collapsed gap retains minimum grab thickness.");
        split.Collapsed = true; Advance(tree); Check(!handle.Visible, "Collapsed panel layout hides its drag area.");
        Reject<InvalidOperationException>(() => split.Vertical = false); Reject<ArgumentOutOfRangeException>(() => split.DraggerVisibilityMode = (SplitContainer.DraggerVisibility)3);
        using var vertical = new VSplitContainer { Theme = skin.Theme, Size = new(40, 100) }; vertical.AddChild(Child("VA")); vertical.AddChild(Child("VB")); using var vt = new SceneTree(vertical); Advance(vt); Check(vertical.GetChild(0).GetType() == typeof(Control) && ((Control)vertical.GetChild(0)).Size == new Vector2(40, 44), "Fixed vertical layout transposes geometry.");
    }
    private static void VerifyOffsets()
    {
        using var skin = new Skin(); var split = new SplitContainer { Theme = skin.Theme, Size = new(120, 40) }; var a = Child("A"); var b = Child("B", 20); var c = Child("C", 30); split.AddChild(a); split.AddChild(b); split.AddChild(c); using var tree = new SceneTree(split); Advance(tree);
        Check(a.Size.X == 32 && b.Size.X == 32 && c.Size.X == 32 && split.GetDragAreaControls().Length == 2, "Multi-panel weighted defaults and one dragger per boundary.");
        split.SplitOffsets = [60, -60]; Advance(tree); Check(a.Size.X == 46 && b.Size.X == 20 && c.Size.X == 30 && split.SplitOffsets.SequenceEqual([60, -60]), "Visual clamping resolves overlaps without rewriting stored offsets.");
        split.ClampSplitOffset(1); Advance(tree); Check(split.SplitOffsets.SequenceEqual([-22, -34]) && a.Size.X == 10 && b.Size.X == 20 && c.Size.X == 66, "Active split priority propagates constraints to the left.");
        split.SplitOffsets = [60, -60]; split.ClampSplitOffset(); Advance(tree); Check(split.SplitOffsets.SequenceEqual([14, 2]), "First split priority clamps every stored value.");
        split.SplitOffsets = [0, 0]; a.CustomMaximumSize = new(15, -1); Advance(tree); Check(a.Size.X == 15 && b.Size.X == 40 && c.Size.X == 41, "Capped default weight refit distributes accumulated pixels.");
        var descriptor = new PropertyDescriptor<SplitContainer, int[]>("Offsets", c => c.SplitOffsets, (c, v) => c.SplitOffsets = v, revertValue: _ => [0, 0]); Check(!descriptor.CanRevert(split), "Integer-array revert compares elements rather than fresh getter identities.");
        var areas = split.GetDragAreaControls(); areas[0] = new Control(); areas[0].Dispose(); Check(ReferenceEquals(split.GetDragAreaControl().Parent, split), "The Control array is caller-owned and accepts ordinary Control replacements without changing engine areas.");
        var values = split.SplitOffsets; values[0] = 99; Check(split.SplitOffset == 0, "Returned offsets are independent snapshots.");
        split.SplitOffsets = []; Reject<ArgumentOutOfRangeException>(() => _ = split.SplitOffset); Advance(tree); Check(split.SplitOffsets.SequenceEqual([0, 0]), "Layout grows empty/short offset arrays.");
        Reject<ArgumentOutOfRangeException>(() => split.ClampSplitOffset(4));
    }
    private static void VerifyStructure()
    {
        using var skin = new Skin(); var split = new SplitContainer { Theme = skin.Theme, Size = new(180, 40), SplitOffsets = [5, -5] }; var a = Child("A"); var b = Child("B"); var c = Child("C"); split.AddChild(a); split.AddChild(b); split.AddChild(c); using var tree = new SceneTree(split); Advance(tree);
        var before = new[] { a.Size.X, b.Size.X, c.Size.X }; split.MoveChild(c, 0); Advance(tree);
        Check(c.Size.X == before[2] && a.Size.X == before[0] && b.Size.X == before[1], "Reordering preserves each panel's desired extent.");
        c.Visible = false; Advance(tree); Check(split.GetDragAreaControls().Length == 1 && a.Size.X + b.Size.X == 168, "Removing visibility redistributes space and trims extra draggers.");
        c.Visible = true; Advance(tree); Check(split.GetDragAreaControls().Length == 2, "Reappearing panel restores its boundary without losing existing panels.");
        a.TopLevel = true; Advance(tree); Check(split.GetDragAreaControls().Length == 1, "Top-level panels do not enter split allocation.");
        var handle = split.GetDragAreaControl(); var custom = new Control { Name = "Custom", MouseFilter = MouseFilter.Ignore }; handle.AddChild(custom); Check(ReferenceEquals(custom.Parent, handle) && ReferenceEquals(split.GetDragAreaControls()[0], handle), "Borrowed internal areas host real custom controls.");
    }
    private static void VerifyInputAndFailures()
    {
        using var skin = new Skin(); var split = new SplitContainer { Theme = skin.Theme, Size = new(100, 40) }; split.AddChild(Child("A")); split.AddChild(Child("B")); var viewport = new TestViewport(); var forwarder = new InputForwarder(); viewport.AddChild(split); viewport.AddChild(forwarder); using var tree = new SceneTree(viewport); Advance(tree);
        var handle = split.GetDragAreaControl(); forwarder.Target = handle; void Send(InputEvent e) => viewport.PushInput(e, true); var order = new List<string>(); split.DragStarted += () => order.Add("start"); split.Dragged += value => order.Add("move:" + value); split.DragEnded += () => order.Add("end");
        using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(2, 3) }; using var motion = new InputEventMouseMotion { Position = new(12, 3) }; using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new(12, 3) };
        Send(down); Send(motion); Send(up); Advance(tree); Check(order.SequenceEqual(["start", "move:10", "end"]) && split.SplitOffset == 10, "Pointer drag commits relative delta and ordered events.");
        split.LayoutDirection = LayoutDirection.RTL; Advance(tree); Send(down); Send(motion); Send(up); Check(split.SplitOffset == 0, "RTL drag delta reverses independently of physical movement.");
        using var key = new InputEventKey { Keycode = Key.Left, Pressed = true }; Send(key); Check(split.SplitOffset == 10 && order.Count == 6, "Keyboard ten-percent stepping uses direction but no pointer drag signals.");
        split.TouchDraggerEnabled = true; Advance(tree); var touch = handle.GetChild(0, true) as TextureRect; Check(touch is { Texture: not null } && touch.Size == new Vector2(24, 24), "Touch dragging creates the real overlapping image control.");
        touch!.DispatchGUIInput(motion); var hoverColor = touch.Modulate; split.Vertical = true; Check(touch.Modulate == hoverColor, "Geometry refresh preserves the touch hover tint."); split.Vertical = false;
        Action failure = () => throw new ApplicationException("expected drag end observer"); split.DragEnded += failure; Send(down); Check(Capture(() => split.DraggingEnabled = false) is not null && !split.DraggingEnabled && handle.MouseFilter == MouseFilter.Ignore, "Failed end observer cannot prevent disabling pointer targets."); split.DragEnded -= failure;
        Check(!skin.Grabber.IsDisposed && skin.Grabber.RetainRendererCache, "Attached split retains borrowed icon renderer residency."); tree.Dispose(); Check(!skin.Grabber.RetainRendererCache && !skin.Touch.IsDisposed, "Exit/disposal releases residency without owning textures.");
    }
    private static void VerifyPackingAndGuards()
    {
        using var split = new VSplitContainer { Name = "Split", Size = new(40, 120), SplitOffsets = [7, 9], TouchDraggerEnabled = true, DragNestedIntersections = true, DragAreaOffset = -3, DragAreaMarginBegin = 2, DragAreaMarginEnd = 4, DraggerVisibilityMode = SplitContainer.DraggerVisibility.Hidden };
        split.AddChild(Child("A")); split.AddChild(Child("B")); split.AddChild(Child("C")); using var packed = new PackedScene(); packed.Pack(split); split.SplitOffsets = [42, 43]; using var copy = (VSplitContainer)packed.Instantiate(); Check(copy.SplitOffsets.SequenceEqual([7, 9]) && copy.TouchDraggerEnabled && copy.DragNestedIntersections && copy.Vertical && copy.DragAreaOffset == -3 && copy.GetChildCount() == 0, "Packing stores configuration and reconstructs internal controls, omitting unowned ordinary children.");
        using var tree = new SceneTree(copy); Check(Task.Run(() => Capture(() => _ = copy.SplitOffsets)).Result is InvalidOperationException && Task.Run(() => Capture(() => copy.SplitOffset = 1)).Result is InvalidOperationException, "Attached queries/mutation require owner thread.");
    }
    private static void VerifyIntersections()
    {
        using var skin = new Skin(); var outer = new SplitContainer { Theme = skin.Theme, Size = new(200, 120), DragNestedIntersections = true };
        var left = new Control { Name = "Left", CustomMinimumSize = new(10, 10), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        left.AddChild(new Node { Name = "Unrelated" });
        var nested = new SplitContainer { Name = "Nested", Theme = skin.Theme, Vertical = true, DragNestedIntersections = true }; nested.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); nested.AddChild(Child("Top")); nested.AddChild(Child("Bottom")); left.AddChild(nested); outer.AddChild(left); outer.AddChild(Child("Right")); using var tree = new SceneTree(outer); Advance(tree, 8);
        var parent = outer.GetDragAreaControl(); var intersection = parent.GetChildren(true).FirstOrDefault(n => n.Name.StartsWith("_split_intersection_")) as Control;
        Check(intersection is not null && intersection.GetCursorShape() == Control.CursorShape.Drag, "An unrelated sibling does not suppress a valid orthogonal nested intersection.");
        using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(1, 1) }; using var move = new InputEventMouseMotion { Position = new(1, 11) }; using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = new(1, 11) };
        intersection!.DispatchGUIInput(down); intersection.DispatchGUIInput(move); intersection.DispatchGUIInput(up); Check(nested.SplitOffset == 10, "Intersection drag moves the descendant split along its orthogonal axis.");
        nested.DragNestedIntersections = false; Advance(tree); Check(intersection.IsDisposed, "Disabling nested intersections removes borrowed joint targets.");
    }
    private static void VerifyLayoutFailures()
    {
        using var skin = new Skin(); var split = new SplitContainer { Theme = skin.Theme, Size = new(120, 120) }; var a = Child("A"); var b = Child("B"); split.AddChild(a); split.AddChild(b); using var tree = new SceneTree(split); Advance(tree);
        var first = a.GetRect(); var second = b.GetRect(); split.SplitOffset = int.MaxValue;
        Check(Capture(() => tree.ProcessFrame(0)) is not null && a.GetRect() == first && b.GetRect() == second, "Overflow preflight preserves both existing panel rectangles.");
        split.SplitOffset = 0; Advance(tree);
        var changed = false; Action resize = () => { if (!changed) { changed = true; split.Vertical = true; } }; a.Resized += resize;
        split.Size = new(122, 120); Advance(tree); a.Resized -= resize;
        Check(changed && split.Vertical && a.Size.Y == 54 && b.Position.Y == 66, "A resize callback changing orientation settles through a fresh layout pass.");
        Action fail = () => throw new ApplicationException("expected first panel resize"); a.Rotation = .4f; a.Scale = new(2, 3); a.Resized += fail; split.Size = new(122, 124);
        Check(Capture(() => tree.ProcessFrame(0)) is not null && b.Position.Y == 68 && a.Rotation == 0 && a.Scale == Vector2.One, "A failing first panel callback still attempts later panel placements."); a.Resized -= fail; Advance(tree);
    }
    private static void VerifyWarm()
    {
        using var skin = new Skin(); var split = new SplitContainer { Theme = skin.Theme, Size = new(120, 40) }; split.AddChild(Child("A")); split.AddChild(Child("B")); split.AddChild(Child("C")); using var tree = new SceneTree(split); Advance(tree, 20);
        for (var i = 0; i < 64; i++) { split.SplitOffset = i % 2; Advance(tree); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { split.SplitOffset = i % 2; Advance(tree); }
        var active = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread(); Advance(tree, 64); var idle = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0 && idle == 0, $"Warmed split writes/deferred layout/polling allocate {active}/{idle} managed bytes.");
    }
    internal sealed class Skin : IDisposable
    {
        internal readonly Theme Theme = new(); internal readonly ImageTexture Grabber, Touch; private readonly StyleBoxEmpty _style = new();
        internal Skin()
        {
            using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.Green); Grabber = ImageTexture.CreateFromImage(image);
            using var touch = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8); touch.Fill(Colors.Magenta); Touch = ImageTexture.CreateFromImage(touch);
            foreach (var name in new[] { "grabber", "h_grabber", "v_grabber" }) Theme.SetIcon(name, "SplitContainer", Grabber);
            foreach (var name in new[] { "touch_dragger", "h_touch_dragger", "v_touch_dragger" }) Theme.SetIcon(name, "SplitContainer", Touch);
            Theme.SetStyleBox("split_bar_background", "SplitContainer", _style); Theme.SetConstant("separation", "SplitContainer", 12); Theme.SetConstant("autohide", "SplitContainer", 0);
        }
        public void Dispose() { Theme.Dispose(); Grabber.Dispose(); Touch.Dispose(); _style.Dispose(); }
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 200, 150); }
    private sealed class InputForwarder : Node
    {
        internal Control? Target;
        internal InputForwarder() => InputEnabled = true;
        protected override void OnInput(InputEvent input) => Target?.DispatchGUIInput(input);
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
