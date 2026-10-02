using Electron2D;

internal static class TextureButtonTests
{
    internal static void Run()
    {
        VerifyDefaultsAndMinimum();
        VerifyStretchAndMasks();
        VerifyStatesAndFocus();
        VerifyMaskRouting();
        VerifyChangesAndLifetime();
        VerifyReentryAndPacking();
        VerifyResidencyLifetime();
        VerifyWarmReuse();
        Console.WriteLine("Texture-button state selection, seven stretch modes, flips, masks, resource revisions, lifetime, reentry, packing and warmed replay passed.");
    }

    private static void VerifyDefaultsAndMinimum()
    {
        using var button = new TextureButton();
        Check(button.StretchMode == TextureStretchMode.Keep && !button.IgnoreTextureSize && !button.FlipH && !button.FlipV &&
            button.TextureNormal is null && button.TexturePressed is null && button.TextureHover is null && button.TextureDisabled is null && button.TextureFocused is null && button.TextureClickMask is null &&
            button.FocusMode == FocusMode.All && button.GetMinimumSize() == Vector2.Zero, "Texture button defaults retain base focus and zero empty minimum.");
        using var normal = new ProbeTexture(4, 2); using var pressed = new ProbeTexture(8, 6); using var hover = new ProbeTexture(10, 9);
        using var disabled = new ProbeTexture(30, 40); using var focused = new ProbeTexture(50, 60); using var mask = new BitMap(); mask.Create(new(3, 5));
        button.TextureDisabled = disabled; button.TextureFocused = focused; Check(button.GetMinimumSize() == Vector2.Zero, "Disabled and focused textures do not contribute to minimum.");
        button.TextureClickMask = mask; Check(button.GetMinimumSize() == new Vector2(3, 5), "Mask-only minimum.");
        button.TextureHover = hover; Check(button.GetMinimumSize() == new Vector2(10, 9), "Hover wins over mask minimum.");
        button.TexturePressed = pressed; Check(button.GetMinimumSize() == new Vector2(8, 6), "Pressed wins over hover minimum.");
        button.TextureNormal = normal; Check(button.GetMinimumSize() == new Vector2(4, 2), "Normal wins independently of button draw mode.");
        button.IgnoreTextureSize = true; button.CustomMinimumSize = new(1, 2); Check(button.GetMinimumSize() == Vector2.Zero && button.GetCombinedMinimumSize() == new Vector2(1, 2), "Ignore affects intrinsic size only.");
        button.IgnoreTextureSize = false; using var signed = new ProbeTexture(-3, -4); button.TextureNormal = signed; Check(button.GetMinimumSize() == new Vector2(3, 4), "Minimum applies absolute values even for custom signed texture dimensions.");
        button.StretchMode = (TextureStretchMode)99; Check((int)button.StretchMode == 99, "Unknown stretch identities are stored.");
    }

    private static void VerifyStretchAndMasks()
    {
        using var texture = new ProbeTexture(4, 2); using var button = new Probe { TextureNormal = texture, IgnoreTextureSize = true, Size = new(10, 8) };
        var expected = new Rect2[] { new(0, 0, 10, 8), new(0, 0, 10, 8), new(0, 0, 4, 2), new(3, 3, 4, 2), new(0, 0, 10, 5), new(0, 1.5f, 10, 5), new(0, 0, 10, 8) };
        for (var mode = 0; mode < 7; mode++)
        {
            button.StretchMode = (TextureStretchMode)mode; Record(button);
            Check(texture.LastRect == expected[mode], $"Stretch mode {mode} exact destination.");
            Check(texture.LastTile == (mode == 1) && texture.LastWasRegion == (mode != 1), "Tiling alone uses complete rectangle draw; others use a region.");
            Check(texture.LastRegion == (mode == 6 ? new Rect2(.75f, 0, 2.5f, 2) : new Rect2(0, 0, 4, 2)) || mode == 1, "Centered cover uses the expected source crop.");
            button.FlipH = true; button.FlipV = true; Record(button);
            Check(texture.LastRect.Position == expected[mode].Position && texture.LastRect.Size == -expected[mode].Size, "Flips negate drawing size without moving the origin.");
            button.FlipH = false; button.FlipV = false;
        }
        button.StretchMode = (TextureStretchMode)99; Record(button); Check(texture.LastRect == expected[2], "Unknown stretch mode draws at natural top-left.");
        using var mask = new BitMap(); mask.Create(new(4, 2)); mask.SetBit(0, 0, true); mask.SetBit(3, 1, true);
        using var hit = new Probe { IgnoreTextureSize = true, Size = new(10, 8), TextureNormal = texture, TextureClickMask = mask, StretchMode = TextureStretchMode.KeepCentered };
        Check(hit.Point(new(.5f, .5f)) && !hit.Point(new(3.5f, .5f)), "Before first recording, mask uses natural coordinates.");
        Record(hit); Check(hit.Point(new(3.5f, 3.5f)) && hit.Point(new(6.5f, 4.5f)) && !hit.Point(new(4.5f, 3.5f)) && !hit.Point(new(.5f, .5f)), "Centered mask uses the recorded image offset.");
        hit.FlipH = true; hit.FlipV = true; Record(hit); Check(hit.Point(new(3.5f, 3.5f)) && !hit.Point(new(4.5f, 3.5f)), "Visual flips do not mirror the mask.");
        hit.StretchMode = TextureStretchMode.Scale; Record(hit);
        Check(hit.Point(new(.5f, .5f)) && hit.Point(new(9.5f, 7.5f)) && !hit.Point(new(3.5f, .5f)), "Scaled hit mapping uses mask dimensions.");
        hit.StretchMode = TextureStretchMode.Tile; Record(hit);
        Check(hit.Point(new(4.5f, 2.5f)) && hit.Point(new(7.5f, 3.5f)) && !hit.Point(new(-1, 0)) && !hit.Point(new(12, 0)), "Tiled mask repeats within recorded bounds.");
        hit.StretchMode = TextureStretchMode.KeepAspectCovered; Record(hit);
        Check(hit.Point(new(.5f, .5f)) && hit.Point(new(9.5f, 7.5f)) && !hit.Point(new(2.5f, .5f)), "Cover mask accounts for its source crop.");
        using var small = new BitMap(); small.Create(new(2, 2)); small.SetBitRect(new(0, 0, 2, 2), true); hit.TextureClickMask = small; Record(hit);
        Check(!hit.Point(new(9.5f, 7.5f)), "A crop exceeding mismatched bitmap dimensions rejects the point instead of indexing outside the mask.");
        using var empty = new BitMap(); hit.TextureClickMask = empty; Record(hit); Check(!hit.Point(Vector2.Zero), "Empty bitmaps cannot divide or index by zero.");
        hit.TextureNormal = null; hit.TextureClickMask = mask; hit.StretchMode = TextureStretchMode.Scale; Record(hit); Check(hit.Point(new(9.5f, 7.5f)), "Mask-only buttons compute the same stretch mapping.");
        hit.TextureClickMask = null; Check(hit.Point(new(9, 7)) && !hit.Point(new(10, 7)), "No mask delegates to the control rectangle.");
        using var emptyTexture = new ImageTexture(); hit.TextureNormal = emptyTexture;
        foreach (var mode in new[] { TextureStretchMode.KeepAspect, TextureStretchMode.KeepAspectCentered, TextureStretchMode.KeepAspectCovered }) { hit.StretchMode = mode; Record(hit); }
    }

    private static void VerifyStatesAndFocus()
    {
        using var normal = new ProbeTexture(4, 2); using var pressed = new ProbeTexture(6, 4); using var hover = new ProbeTexture(8, 6);
        using var disabled = new ProbeTexture(10, 8); using var focused = new ProbeTexture(12, 10);
        var viewport = new TestViewport(); var button = new Probe { TextureNormal = normal, TexturePressed = pressed, TextureHover = hover, TextureDisabled = disabled, TextureFocused = focused, Size = new(20, 20) };
        viewport.AddChild(button); using var tree = new SceneTree(viewport);
        Record(button); Check(normal.Draws == 1 && focused.Draws == 0, "Normal texture without focus overlay.");
        button.Hover(true); Record(button); Check(hover.Draws == 1, "Hover texture selected.");
        using var down = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(1, 1) };
        using var up = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = new(1, 1) };
        button.Send(down); Record(button); Check(pressed.Draws == 1, "Pressed texture selected during pointer hold.");
        button.Send(up); button.Hover(false); button.GrabFocus(); Record(button);
        Check(focused.Draws == 1 && focused.LastRect == new Rect2(0, 0, 4, 2) && !focused.LastWasRegion, "Focus overlays the selected texture destination using its full image.");
        button.TextureNormal = null; button.ReleaseFocus(); Record(button); var focusedBefore = focused.Draws; button.GrabFocus(); Record(button);
        Check(focused.Draws == focusedBefore + 1 && focused.LastRect.Size == new Vector2(12, 10), "Focus-only button draws once using the focused image's dimensions.");
        button.ReleaseFocus(); button.TextureNormal = normal; button.Disabled = true; Record(button); Check(disabled.Draws == 1, "Disabled texture selected.");
        button.TextureDisabled = null; var normalBefore = normal.Draws; Record(button); Check(normal.Draws == normalBefore + 1, "Disabled falls back to normal.");
        button.Disabled = false; button.TexturePressed = null; button.Hover(true); button.Send(down); var hoverBefore = hover.Draws; Record(button); Check(hover.Draws == hoverBefore + 1, "Pressed falls back to hover.");
        button.TextureHover = null; normalBefore = normal.Draws; Record(button); Check(normal.Draws == normalBefore + 1, "Pressed falls back to normal when hover is absent.");
        button.Send(up); button.TexturePressed = pressed; button.ToggleMode = true; button.ButtonPressed = true; button.Hover(true); Record(button); Check(pressed.LastRect.Size == new Vector2(6, 4), "Hovered toggled button falls back to pressed when hover is absent.");
        var builtInBefore = normal.Draws + pressed.Draws; var order = new List<int>(); button.Draw += _ => order.Add(normal.Draws + pressed.Draws); button.AfterDraw = () => order.Add(normal.Draws + pressed.Draws);
        Record(button); Check(order.Count == 2 && order[0] > builtInBefore && order[0] == order[1], "Built-in drawing precedes user Draw and OnDraw without requiring a base OnDraw call.");
    }

    private static void VerifyMaskRouting()
    {
        using var mask = new BitMap(); mask.Create(new(2, 2)); mask.SetBit(0, 0, true);
        var viewport = new TestViewport(); var button = new TextureButton { Position = new(10, 10), Size = new(20, 20), IgnoreTextureSize = true, TextureClickMask = mask, StretchMode = TextureStretchMode.Scale };
        viewport.AddChild(button); using var tree = new SceneTree(viewport); Record(button); var activations = 0; button.Pressed += () => activations++;
        void Click(Vector2 position)
        {
            using var motion = new InputEventMouseMotion { Position = position }; viewport.PushInput(motion, true);
            using var down = new InputEventMouseButton { Position = position, ButtonIndex = MouseButton.Left, Pressed = true }; viewport.PushInput(down, true);
            using var up = new InputEventMouseButton { Position = position, ButtonIndex = MouseButton.Left, Pressed = false }; viewport.PushInput(up, true);
        }
        Click(new(12, 12)); Check(activations == 1, "Actual viewport picking routes accepted bitmap pixels through BaseButton activation.");
        Click(new(26, 26)); Check(activations == 1, "Actual viewport picking rejects transparent bitmap pixels.");
        button.FlipH = true; button.FlipV = true; Record(button); Click(new(12, 12)); Check(activations == 2, "Actual GUI picking retains unflipped mask coordinates.");
    }

    private static void VerifyChangesAndLifetime()
    {
        using var texture = new ProbeTexture(4, 2); Action<Resource> fail = _ => throw new ApplicationException("expected early observer"); texture.Changed += fail;
        var root = new Node(); var button = new Probe { TextureNormal = texture, TexturePressed = texture, Size = new(10, 8) }; root.AddChild(button); using var tree = new SceneTree(root);
        tree.ProcessFrame(0); Record(button); var draws = texture.Draws;
        Reject<ApplicationException>(() => texture.ChangeColor(Colors.Blue)); tree.ProcessFrame(0); button.PrepareCanvas(); Check(texture.Draws == draws + 1 && texture.LastColor == Colors.Blue, "Revision polling catches same-size changes after an earlier throwing observer.");
        texture.Changed -= fail; button.TextureNormal = null; texture.ChangeColor(Colors.Cyan); button.ToggleMode = true; button.ButtonPressed = true; tree.ProcessFrame(0); button.PrepareCanvas(); Check(texture.LastColor == Colors.Cyan, "Removing one alias retains the remaining borrowed resource subscription.");
        Check(Task.Run(() => Capture(() => button.FlipH = true)).Result is InvalidOperationException, "Attached mutations require the scene owner thread.");
        Task.Run(() => texture.ChangeColor(Colors.Magenta)).GetAwaiter().GetResult(); tree.ProcessFrame(0); button.PrepareCanvas(); Check(texture.LastColor == Colors.Magenta, "Worker resource updates reach owner-thread drawing through deferred delivery.");
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var pixels = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = pixels, Region = new(0, 0, 2, 2) };
        atlas.Changed += fail; button.ButtonPressed = false; button.TextureNormal = atlas; var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        Record(button); button.AppendCanvas(vertices, batches, Transform.Identity); var oldUV = vertices[0].UV;
        Reject<ApplicationException>(() => atlas.Region = new(2, 0, 2, 2)); tree.ProcessFrame(0); button.PrepareCanvas(); vertices.Clear(); batches.Clear(); button.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].UV != oldUV, "Equal-size atlas region mutations rebuild recorded UVs even if its earlier observer throws.");
        atlas.Changed -= fail;
        using var inner = new AtlasTexture { Atlas = pixels, Region = new(0, 0, 2, 2) }; inner.Changed += fail;
        using var outer = new AtlasTexture { Atlas = inner }; button.TextureNormal = outer; Record(button);
        vertices.Clear(); batches.Clear(); button.AppendCanvas(vertices, batches, Transform.Identity); oldUV = vertices[0].UV;
        Reject<ApplicationException>(() => inner.Region = new(2, 0, 2, 2)); tree.ProcessFrame(0); button.PrepareCanvas();
        vertices.Clear(); batches.Clear(); button.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].UV != oldUV, "Polling includes nested atlas sources when an earlier observer prevents Changed forwarding to the assigned outer view.");
        inner.Changed -= fail; button.TextureNormal = texture; button.TexturePressed = null;
        Action<ElectronObject> disposedFail = _ => throw new ApplicationException("expected disposal observer"); texture.Disposed += disposedFail;
        Check(Capture(texture.Dispose) is not null, "External disposal callback fails after committing disposal.");
        Check(ReferenceEquals(button.TextureNormal, texture) && texture.IsDisposed, "Disposed borrowed identity remains readable.");
        Reject<ObjectDisposedException>(() => button.GetMinimumSize()); Reject<ObjectDisposedException>(() => Record(button)); Reject<ObjectDisposedException>(() => button.TextureHover = texture);
        button.TextureNormal = pixels; tree.ProcessFrame(0); Record(button); root.RemoveChild(button); button.Dispose(); Check(!pixels.IsDisposed && !atlas.IsDisposed, "Button disposal never owns borrowed textures.");
    }

    private static void VerifyReentryAndPacking()
    {
        using var first = new ProbeTexture(4, 2); using var replacement = new ProbeTexture(7, 5); using var button = new Probe { TextureNormal = first, Size = new(10, 8) };
        first.SizeQuery = () => { first.SizeQuery = null; button.TextureNormal = replacement; };
        Check(button.GetMinimumSize() == new Vector2(7, 5), "Reentrant size getter cannot publish an obsolete resource minimum.");
        button.TextureNormal = first; first.SizeQuery = () => button.FlipH = !button.FlipH;
        Reject<InvalidOperationException>(() => button.GetMinimumSize()); first.SizeQuery = null; Check(button.GetMinimumSize() == new Vector2(4, 2), "Continuously reentrant size queries are bounded and recover after correction.");
        first.Drawing = () => { first.Drawing = null; button.TextureNormal = replacement; }; Record(button); button.PrepareCanvas(); Check(replacement.Draws > 0, "Resource replacement during virtual drawing remains dirty for the next recording.");
        button.TextureNormal = first; button.TexturePressed = replacement; button.ToggleMode = true;
        first.SizeQuery = () => { first.SizeQuery = null; button.ButtonPressed = true; }; var drawsBefore = replacement.Draws;
        Record(button); Check(replacement.Draws == drawsBefore + 1, "Reentrant base-button state changes during size lookup select the new state's texture before recording.");
        button.ButtonPressed = false; first.Drawing = () => { first.Drawing = null; button.ButtonPressed = true; }; Record(button); drawsBefore = replacement.Draws;
        button.PrepareCanvas(); Check(replacement.Draws == drawsBefore + 1, "Base-button state changes during virtual drawing schedule the corrected next recording.");
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); texture.ResourceLocalToScene = true;
        using var mask = new BitMap(); mask.Create(new(4, 2)); mask.SetBit(1, 0, true); mask.ResourceLocalToScene = true;
        using var root = new Node(); var stored = new TextureButton { Name = "TextureButton", TextureNormal = texture, TexturePressed = texture, TextureClickMask = mask, FlipH = true, FlipV = true, IgnoreTextureSize = true, StretchMode = (TextureStretchMode)99 };
        root.AddChild(stored); stored.Owner = root; using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate(); var instance = copy.GetNode<TextureButton>("TextureButton");
        Check(instance.GetType() == typeof(TextureButton) && instance.FlipH && instance.FlipV && instance.IgnoreTextureSize && (int)instance.StretchMode == 99 &&
            ReferenceEquals(instance.TextureNormal, instance.TexturePressed) && !ReferenceEquals(instance.TextureNormal, texture) && instance.TextureClickMask!.GetBit(1, 0), "Exact typed scene state, local resources and aliases survive packing.");
        using var capture = new CaptureProbe(); capture.DuringCapture = () => stored.StretchMode = TextureStretchMode.Scale; root.AddChild(capture); capture.Owner = root;
        Check(Capture(() => packed.Pack(root)) is not null && (int)stored.StretchMode == 99, "Capture rejects changing texture-button stored state.");
    }

    private static void VerifyResidencyLifetime()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        using var first = ImageTexture.CreateFromImage(image); using var second = ImageTexture.CreateFromImage(image);
        using var inner = new AtlasTexture { Atlas = first }; using var outer = new AtlasTexture { Atlas = inner };
        var button = new Probe { Name = "First", TextureNormal = outer, TexturePressed = outer, TextureDisabled = second };
        button.GetMinimumSize(); Check(!first.RetainRendererCache && !second.RetainRendererCache && !outer.RetainRendererCache, "Detached configuration and queries do not acquire renderer residency.");
        var root = new Node(); root.AddChild(button); using var tree = new SceneTree(root);
        Check(first.RetainRendererCache && second.RetainRendererCache && inner.RetainRendererCache && outer.RetainRendererCache, "Attached state textures and their exact atlas chain retain residency before first draw.");
        var peer = new TextureButton { Name = "Peer", TextureNormal = first }; root.AddChild(peer);
        button.TextureNormal = null; Check(outer.RetainRendererCache, "Removing one alias does not release its remaining slot's lease.");
        button.TexturePressed = null; Check(!inner.RetainRendererCache && !outer.RetainRendererCache && first.RetainRendererCache, "Removed atlas aliases release immediately while a peer keeps the shared source resident.");
        button.TextureHover = outer; inner.Atlas = second; tree.ProcessFrame(0);
        Check(inner.RetainRendererCache && outer.RetainRendererCache && second.RetainRendererCache, "Changing an atlas source reconciles its transitive leases.");
        root.RemoveChild(peer); Check(!first.RetainRendererCache, "A final detached owner releases the former atlas source."); peer.Dispose();
        root.RemoveChild(button); Check(!second.RetainRendererCache && !inner.RetainRendererCache && !outer.RetainRendererCache, "Exit releases every state and chain lease, including aliased source slots.");
        root.AddChild(button); Check(second.RetainRendererCache && inner.RetainRendererCache, "Reentry reacquires exactly one lease per distinct dependency.");
        button.Dispose(); Check(!second.RetainRendererCache && !inner.RetainRendererCache && !outer.RetainRendererCache && !second.IsDisposed, "Attached disposal releases residency without disposing caller resources.");
        using var dying = ImageTexture.CreateFromImage(image); var disposedButton = new TextureButton { Name = "DisposedResource", TextureNormal = dying }; root.AddChild(disposedButton);
        dying.Dispose(); Check(!dying.RetainRendererCache && ReferenceEquals(disposedButton.TextureNormal, dying), "Disposed borrowed resources lose residency while their public identity remains assigned."); disposedButton.Dispose();
        using var failureTexture = ImageTexture.CreateFromImage(image);
        var failingExit = new TextureButton { Name = "FailingExit", TextureNormal = failureTexture }; root.AddChild(failingExit);
        failingExit.TreeExiting += _ => throw new ApplicationException("expected exit observer");
        Check(Capture(() => root.RemoveChild(failingExit)) is not null && !failureTexture.RetainRendererCache, "Exit observer failure still releases attached texture leases."); failingExit.Dispose();
        var failingEntry = new TextureButton { Name = "FailingEntry", TextureNormal = failureTexture };
        failingEntry.Ready += _ => throw new ApplicationException("expected ready observer");
        Check(Capture(() => root.AddChild(failingEntry)) is not null && failingEntry.IsInsideTree && failureTexture.RetainRendererCache, "Insertion callback failure preserves committed attachment and its residency.");
        root.RemoveChild(failingEntry); failingEntry.Dispose(); Check(!failureTexture.RetainRendererCache, "Removing the failed insertion releases its still-valid attached lease.");
        using var failedRoot = new Node(); var rollback = new TextureButton { TextureNormal = failureTexture }; failedRoot.AddChild(rollback);
        rollback.Ready += _ => throw new ApplicationException("expected root activation observer");
        Check(Capture(() => new SceneTree(failedRoot)) is not null && !failureTexture.RetainRendererCache, "SceneTree activation rollback releases residency acquired before the failing callback.");
    }

    private static void VerifyWarmReuse()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        var root = new Node(); var button = new TextureButton { TextureNormal = texture, IgnoreTextureSize = true, Size = new(10, 8), StretchMode = TextureStretchMode.KeepAspectCovered }; root.AddChild(button); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Cycle(int index) { button.FlipH = (index & 1) == 0; tree.ProcessFrame(0); button.PrepareCanvas(); vertices.Clear(); batches.Clear(); button.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var index = 0; index < 64; index++) Cycle(index);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var index = 0; index < 64; index++) Cycle(index); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Warmed texture-button mutation, revision polling, recording and replay allocate {allocated} managed bytes.");
    }

    internal static void Record(TextureButton button) { button.InvalidateCanvas(); button.PrepareCanvas(); }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 200, 150); }
    private sealed class Probe : TextureButton
    {
        internal Action? AfterDraw;
        internal bool Point(Vector2 point) => HasPoint(point);
        internal void Send(InputEvent input) => OnGUIInput(input);
        internal void Hover(bool hovered) => OnNotification(hovered ? NotificationMouseEnter : NotificationMouseExit);
        protected override void OnDraw() => AfterDraw?.Invoke();
    }
    private sealed class CaptureProbe : Node
    {
        internal Action? DuringCapture;
        private static readonly PropertyDescriptor<CaptureProbe, int> Descriptor = new("CaptureProbe", node => { node.DuringCapture?.Invoke(); return 0; }, (_, _) => { }, _ => 0, stored: true);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(Descriptor);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateCaptureProbe;
        private static Node CreateCaptureProbe() => new CaptureProbe();
    }
    private sealed class ProbeTexture(int width, int height) : Texture
    {
        internal Action? SizeQuery, Drawing;
        internal int Draws;
        internal Rect2 LastRect, LastRegion;
        internal bool LastWasRegion, LastTile;
        internal Color LastColor;
        private Color _color = Colors.Red;
        public override int GetWidth() => width;
        public override int GetHeight() => height;
        public override Vector2 GetSize() { ThrowIfDisposed(); SizeQuery?.Invoke(); return new(width, height); }
        internal void ChangeColor(Color value) { _color = value; EmitChanged(); }
        public override void DrawRect(CanvasItem canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
        { LastRect = rect; LastWasRegion = false; LastTile = tile; LastColor = _color; Draws++; canvasItem.DrawRect(rect.Abs(), _color); Drawing?.Invoke(); }
        public override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
        { LastRect = rect; LastRegion = sourceRect; LastWasRegion = true; LastTile = false; LastColor = _color; Draws++; canvasItem.DrawRect(rect.Abs(), _color); Drawing?.Invoke(); }
    }
}
