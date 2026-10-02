using Electron2D;

internal static class StyleBoxTests
{
    internal static void Run()
    {
        VerifyMarginsAndHooks();
        VerifyTextureState();
        VerifyLineGeometry();
        VerifyDrawScope();
        VerifyCopiesAndLifetime();
        VerifyConcurrencyAndAllocations();
        Console.WriteLine("Style-box margins, hooks, texture state, line geometry, drawing scope, resources and allocation checks passed.");
    }

    private static void VerifyMarginsAndHooks()
    {
        using var empty = new StyleBoxEmpty(); var changed = 0; empty.Changed += _ => changed++;
        for (var side = 0; side < 4; side++)
            Check(empty.GetContentMargin((Side)side) == -1 && empty.GetMargin((Side)side) == 0, "Empty styles default to automatic zero content margins.");
        var rect = new Rect2(10, 20, 30, 40);
        Check(empty.GetMinimumSize() == Vector2.Zero && empty.GetOffset() == Vector2.Zero && empty.GetDrawRect(rect) == rect &&
            empty.TestMask(new(-100, -100), rect) && empty.GetCurrentItemDrawn() is null, "Base queries preserve identity bounds and an unrestricted mask outside drawing.");
        empty.SetContentMargin(Side.Left, -1); empty.SetContentMarginAll(-1);
        Check(changed == 2, "Equal content margin assignments each emit once, including an all-sides write.");
        empty.ContentMarginLeft = 1; empty.ContentMarginTop = 2; empty.ContentMarginRight = 3; empty.ContentMarginBottom = 4;
        Check(empty.GetMinimumSize() == new Vector2(4, 6) && empty.GetOffset() == new Vector2(1, 2), "Content properties and side methods share the same margin storage.");
        empty.ContentMarginLeft = -20;
        Check(empty.GetContentMargin(Side.Left) == -20 && empty.GetMargin(Side.Left) == 0, "Every negative content margin selects the style's intrinsic margin.");
        var before = changed;
        Reject<ArgumentOutOfRangeException>(() => empty.GetContentMargin((Side)4));
        Reject<ArgumentOutOfRangeException>(() => empty.GetMargin((Side)(-1)));
        Reject<ArgumentOutOfRangeException>(() => empty.SetContentMargin((Side)4, 0));
        Reject<ArgumentException>(() => empty.SetContentMarginAll(float.NaN));
        Reject<ArgumentException>(() => empty.ContentMarginTop = float.PositiveInfinity);
        Reject<ArgumentException>(() => empty.GetDrawRect(new(float.NaN, 0, 1, 1)));
        Reject<ArgumentException>(() => empty.TestMask(new(float.NaN, 0), rect));
        Check(changed == before && empty.ContentMarginTop == 2, "Rejected margin/query inputs preserve state and events.");
        using var custom = new ProbeStyle { Minimum = new(20, 2) };
        custom.SetContentMarginAll(4); custom.ContentMarginBottom = 8;
        Check(custom.GetMinimumSize() == new Vector2(20, 12), "Custom minimums combine componentwise with resolved content sums.");
        Check(custom.GetDrawRect(rect) == new Rect2(9, 19, 32, 42) && custom.TestMask(rect.Position, rect) && !custom.TestMask(rect.Position + Vector2.One, rect),
            "Custom rectangle and mask hooks are executable through public guarded queries.");
        custom.Minimum = new(float.NaN, 1); Reject<InvalidOperationException>(() => custom.GetMinimumSize());
        custom.InvalidDrawRect = true; Reject<InvalidOperationException>(() => custom.GetDrawRect(rect));
    }

    private static void VerifyTextureState()
    {
        using var style = new StyleBoxTexture(); var changed = 0; style.Changed += _ => changed++;
        Check(style.Texture is null && style.RegionRect == default && style.ModulateColor == Colors.White && style.DrawCenter &&
            style.AxisStretchHorizontal == AxisStretchMode.Stretch && style.AxisStretchVertical == AxisStretchMode.Stretch &&
            (int)AxisStretchMode.Tile == 1 && (int)AxisStretchMode.TileFit == 2, "Texture style defaults and stretch mode identities.");
        style.Texture = null; style.RegionRect = default; style.ModulateColor = Colors.White;
        Check(changed == 0, "Equal texture, region and tint assignments are silent.");
        for (var side = 0; side < 4; side++)
        {
            Check(style.GetTextureMargin((Side)side) == 0 && style.GetExpandMargin((Side)side) == 0, "Texture and expansion margins initially vanish.");
            style.SetTextureMargin((Side)side, 0); style.SetExpandMargin((Side)side, 0);
        }
        style.SetTextureMarginAll(0); style.SetExpandMarginAll(0); style.DrawCenter = true;
        style.AxisStretchHorizontal = AxisStretchMode.Stretch; style.AxisStretchVertical = AxisStretchMode.Stretch;
        Check(changed == 13, "Margin groups, individual margins, center and axis policies emit once even when equal.");
        style.SetTextureMarginAll(3.5f); style.ContentMarginLeft = -2; style.ContentMarginRight = 4;
        Check(style.GetMargin(Side.Left) == 3.5f && style.GetMinimumSize() == new Vector2(7.5f, 7), "Automatic content margins inherit fractional texture margins.");
        style.TextureMarginLeft = -4;
        Check(style.GetMargin(Side.Left) == -4 && style.GetMinimumSize().X == 0, "Signed texture margins remain stored while minimums include the zero default hook.");
        style.ExpandMarginLeft = -2; style.ExpandMarginTop = 3; style.ExpandMarginRight = 4; style.ExpandMarginBottom = -5;
        Check(style.GetDrawRect(new(10, 20, 30, 40)) == new Rect2(12, 17, 32, 38), "Signed drawing expansion applies even without a texture.");
        var before = changed;
        Reject<ArgumentOutOfRangeException>(() => style.AxisStretchHorizontal = (AxisStretchMode)3);
        Reject<ArgumentOutOfRangeException>(() => style.AxisStretchVertical = (AxisStretchMode)(-1));
        Reject<ArgumentOutOfRangeException>(() => style.GetTextureMargin((Side)4));
        Reject<ArgumentOutOfRangeException>(() => style.SetExpandMargin((Side)4, 0));
        Reject<ArgumentException>(() => style.SetTextureMarginAll(float.PositiveInfinity));
        Reject<ArgumentException>(() => style.SetExpandMarginAll(float.NaN));
        Reject<ArgumentException>(() => style.RegionRect = new(float.NaN, 0, 1, 1));
        Reject<ArgumentException>(() => style.ModulateColor = new(float.NaN, 0, 0));
        Check(changed == before, "Invalid texture-style values neither mutate nor emit.");
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        style.Texture = texture; style.Texture = texture; before = changed;
        image.Fill(Colors.Red); texture.Update(image);
        Check(changed == before && ReferenceEquals(style.Texture, texture), "Borrowed texture pixel changes do not forward style Changed.");
        texture.Dispose();
        Check(changed == before && ReferenceEquals(style.Texture, texture), "Disposal retains the borrowed identity without synthesizing a style change.");
        using var painter = new Painter { Paint = node => node.DrawStyleBox(style, new(0, 0, 10, 10)) };
        Reject<ObjectDisposedException>(painter.PrepareCanvas);
        using var dead = new ImageTexture(); dead.Dispose(); Reject<ObjectDisposedException>(() => style.Texture = dead);
    }

    private static void VerifyLineGeometry()
    {
        using var line = new StyleBoxLine(); var changes = 0; line.Changed += _ => changes++;
        Check(line.Color == Colors.Black && line.GrowBegin == 1 && line.GrowEnd == 1 && line.Thickness == 1 && !line.Vertical &&
            line.GetMinimumSize() == new Vector2(0, 1) && line.GetOffset() == new Vector2(0, .5f), "Line defaults expose half-thickness content margins on the cross axis.");
        line.Color = line.Color; line.GrowBegin = 1; line.GrowEnd = 1; line.Thickness = 1; line.Vertical = false;
        Check(changes == 5, "All line settings emit on equal assignments.");
        line.Thickness = 3; line.GrowBegin = 1.75f; line.GrowEnd = 2.25f; line.Color = Colors.Blue;
        var rect = new Rect2(-2.9f, 3.9f, 10.9f, 20.9f); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        using var painter = new Painter { Paint = node => node.DrawStyleBox(line, rect) };
        painter.PrepareCanvas(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(Bounds(vertices) == new Rect2(-3, 3, 14, 3) && vertices.All(vertex => vertex.Color == Colors.Blue),
            "Horizontal lines truncate the incoming rectangle, then each growth assignment, and replace height without centering.");
        Check(line.GetDrawRect(rect) == rect, "Line draw-rectangle queries retain the base identity despite actual growth.");
        line.Vertical = true; painter.Paint = node => line.Draw(node, rect); painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(Bounds(vertices) == new Rect2(-2, 1, 3, 24) && line.GetMinimumSize() == new Vector2(3, 0) && line.GetOffset() == new Vector2(1.5f, 0),
            "Vertical orientation switches geometry and automatic content margins together.");
        line.Thickness = -3;
        Check(line.Thickness == -3 && line.GetMargin(Side.Left) == -1.5f && line.GetMinimumSize() == Vector2.Zero, "Signed thickness storage follows the line contract and the zero minimum hook.");
        painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(Bounds(vertices) == new Rect2(-5, 1, 3, 24), "A negative strip thickness draws the normalized rectangle on the opposite side of its origin.");
        line.GrowBegin = float.MaxValue; painter.InvalidateCanvas(); Reject<InvalidOperationException>(painter.PrepareCanvas);
        vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 0 && line.GetCurrentItemDrawn() is null, "Unrepresentable strip pixels reject before drawing and release the recording scope.");
        Reject<ArgumentException>(() => line.GrowBegin = float.NaN); Reject<ArgumentException>(() => line.GrowEnd = float.PositiveInfinity);
        Reject<ArgumentException>(() => line.Color = new(float.NaN, 0, 0));
    }

    private static void VerifyDrawScope()
    {
        using var style = new ProbeStyle(); using var empty = new StyleBoxEmpty(); var root = new Node();
        var outer = new Painter { Name = "Outer" }; var inner = new Painter { Name = "Inner" }; root.AddChild(outer); root.AddChild(inner);
        using var tree = new SceneTree(root); var stages = new List<int>();
        outer.Notified = notification => { if (notification == CanvasItem.NotificationDraw) { Check(ReferenceEquals(style.GetCurrentItemDrawn(), outer), "Current drawing item is available during notification."); stages.Add(1); } };
        outer.Draw += item => { Check(ReferenceEquals(style.GetCurrentItemDrawn(), item), "Current drawing item is available during Draw subscribers."); stages.Add(2); };
        inner.Paint = item => Check(ReferenceEquals(style.GetCurrentItemDrawn(), item), "Nested recording exposes its own current item.");
        outer.Paint = item =>
        {
            stages.Add(3); Check(ReferenceEquals(style.GetCurrentItemDrawn(), item), "Current drawing item is available during OnDraw.");
            inner.InvalidateCanvas(); inner.PrepareCanvas();
            Check(ReferenceEquals(style.GetCurrentItemDrawn(), item) && Task.Run(() => style.GetCurrentItemDrawn()).Result is null, "Nested recording restores the outer item and current-item scope is thread-local.");
            item.DrawStyleBox(style, style.GetDrawRect(new(1, 1, 4, 4))); item.DrawStyleBox(empty, new(20, 20, 5, 5));
            Reject<ArgumentNullException>(() => item.DrawStyleBox(null!, new(0, 0, 1, 1)));
            Reject<ArgumentException>(() => item.DrawStyleBox(empty, new(float.NaN, 0, 1, 1)));
        };
        outer.PrepareCanvas();
        Check(stages.SequenceEqual(new[] { 1, 2, 3 }) && ReferenceEquals(style.LastTarget, outer) && ReferenceEquals(style.LastCurrent, outer) && style.GetCurrentItemDrawn() is null,
            "Style drawing consumes the adjusted query rectangle and current-item state ends after recording.");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); outer.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 6 && Bounds(vertices) == new Rect2(0, 0, 6, 6), "Custom style drawing is real retained geometry while an empty style draws nothing.");
        Reject<InvalidOperationException>(() => style.Draw(outer, new(0, 0, 1, 1)));
        Reject<InvalidOperationException>(() => outer.DrawStyleBox(empty, new(0, 0, 1, 1)));
        Reject<ArgumentNullException>(() => style.Draw(null!, new(0, 0, 1, 1)));
        outer.Paint = _ => throw new ApplicationException("expected recording failure"); outer.InvalidateCanvas(); Reject<ApplicationException>(outer.PrepareCanvas);
        Check(style.GetCurrentItemDrawn() is null, "Throwing draw callbacks always release current-item scope.");
        vertices.Clear(); batches.Clear(); outer.AppendCanvas(vertices, batches, Transform.Identity); Check(vertices.Count == 0, "Failed style recording discards partial commands.");
        outer.Paint = node => node.DrawStyleBox(empty, new(0, 0, 1, 1)); outer.PrepareCanvas();
        Check(Task.Run(() => Capture(outer.PrepareCanvas)).Result is InvalidOperationException, "Attached style recording retains scene owner-thread guards.");
    }

    private static void VerifyCopiesAndLifetime()
    {
        using var image = Image.CreateEmpty(3, 3, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        using var style = new StyleBoxTexture
        {
            Texture = texture,
            ContentMarginLeft = 5,
            TextureMarginTop = 2,
            ExpandMarginRight = -3,
            RegionRect = new(0, 0, 2, 2),
            ModulateColor = Colors.Blue,
            DrawCenter = false,
            AxisStretchHorizontal = AxisStretchMode.Tile
        };
        using var shallow = (StyleBoxTexture)style.Duplicate();
        Check(ReferenceEquals(shallow.Texture, texture) && shallow.ContentMarginLeft == 5 && shallow.TextureMarginTop == 2 && shallow.ExpandMarginRight == -3 &&
            shallow.RegionRect == style.RegionRect && shallow.ModulateColor == Colors.Blue && !shallow.DrawCenter && shallow.AxisStretchHorizontal == AxisStretchMode.Tile,
            "Shallow duplication preserves exact texture style state and borrows its texture.");
        using var deep = (StyleBoxTexture)style.Duplicate(true); using var copiedTexture = deep.Texture!;
        Check(!ReferenceEquals(copiedTexture, texture) && copiedTexture.GetSize() == texture.GetSize(), "Deep duplication traverses the texture resource edge.");
        using var copy = new StyleBoxTexture(); var changes = 0; copy.Changed += _ => changes++; copy.CopyFromResource(style);
        Check(changes == 1 && copy.ContentMarginLeft == 5 && ReferenceEquals(copy.Texture, texture) && copy.ExpandMarginRight == -3, "Resource copy coalesces all style configuration into one Changed event.");
        using var line = new StyleBoxLine { Color = Colors.Red, Thickness = 7, Vertical = true, GrowBegin = -3, GrowEnd = 4, ContentMarginTop = 2 };
        using var lineCopy = (StyleBoxLine)line.Duplicate(); using var empty = new StyleBoxEmpty { ContentMarginBottom = 9 }; using var emptyCopy = (StyleBoxEmpty)empty.Duplicate();
        Check(lineCopy.Thickness == 7 && lineCopy.Vertical && lineCopy.Color == Colors.Red && lineCopy.GrowBegin == -3 && lineCopy.GrowEnd == 4 && lineCopy.ContentMarginTop == 2 && emptyCopy.ContentMarginBottom == 9,
            "Line and empty resource duplication preserve exact types and inherited margins.");
        style.ResourceLocalToScene = true;
        using var consumer = new StyleConsumer { Style = style }; using var packed = new PackedScene(); packed.Pack(consumer);
        using var instance = (StyleConsumer)packed.Instantiate(); using var localStyle = (StyleBoxTexture)instance.Style!;
        Check(!ReferenceEquals(localStyle, style) && localStyle.ContentMarginLeft == 5 && !ReferenceEquals(localStyle.Texture, texture) && texture.IsBuiltIn && !texture.ResourceLocalToScene,
            "A scene-local style duplicates its built-in texture subresource even when that texture is not independently local-to-scene.");
        texture.ResourcePath = "res://style-box-packing-external.png"; packed.Pack(consumer);
        using var externalInstance = (StyleConsumer)packed.Instantiate(); using var externalStyle = (StyleBoxTexture)externalInstance.Style!;
        Check(!ReferenceEquals(externalStyle, style) && ReferenceEquals(externalStyle.Texture, texture), "A scene-local style retains a nonlocal external texture under the existing graph policy.");
        texture.ResourcePath = string.Empty;
        var property = style.GetPropertyList().Single(value => value.Name == nameof(StyleBox.ContentMarginLeft));
        style.RevertProperty(property); Check(style.ContentMarginLeft == -1 && !style.PropertyCanRevert(property), "Stored content-margin defaults match construction.");
        Action<Resource> fail = _ => throw new ApplicationException("expected style change failure"); line.Changed += fail;
        Reject<ApplicationException>(() => line.Thickness = 9); line.Changed -= fail; Check(line.Thickness == 9, "Changed failure leaves committed style state.");
        shallow.Dispose(); Check(!texture.IsDisposed, "Disposing a style never disposes its borrowed texture.");
        Reject<ObjectDisposedException>(() => shallow.GetMinimumSize()); Reject<ObjectDisposedException>(() => shallow.ContentMarginLeft = 2);
        Reject<ObjectDisposedException>(() => shallow.Texture = null); Reject<ObjectDisposedException>(() => shallow.GetDrawRect(default));
    }

    private static void VerifyConcurrencyAndAllocations()
    {
        using var coherent = new StyleBoxEmpty(); coherent.SetContentMarginAll(1);
        Action<Resource> outsideLock = _ => Check(Task.Run(() => coherent.GetContentMargin(Side.Left)).Wait(TimeSpan.FromSeconds(2)), "Changed callbacks run outside the style state lock.");
        coherent.Changed += outsideLock; coherent.SetContentMarginAll(2); coherent.Changed -= outsideLock;
        Parallel.For(0, 256, index =>
        {
            coherent.SetContentMarginAll(index % 2 + 1); var size = coherent.GetMinimumSize();
            Check(size.X == size.Y && (size.X == 2 || size.X == 4), "Concurrent grouped margin writes and reads observe one coherent resource snapshot.");
        });
        using var line = new StyleBoxLine(); using var painter = new Painter { Paint = node => node.DrawStyleBox(line, new(0, 0, 10, 10)) };
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Frame(int pass)
        {
            line.Thickness = pass % 2 + 1; painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        }
        for (var pass = 0; pass < 64; pass++) Frame(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Frame(pass);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed line mutation, style recording and retained replay allocate zero managed bytes.");
    }

    private sealed class ProbeStyle : StyleBox
    {
        internal Vector2 Minimum;
        internal bool InvalidDrawRect;
        internal CanvasItem? LastTarget, LastCurrent;
        protected override Vector2 OnGetMinimumSize() => Minimum;
        protected override Rect2 OnGetDrawRect(Rect2 rect) => InvalidDrawRect ? new(float.NaN, 0, 1, 1) : new(rect.Position - Vector2.One, rect.Size + new Vector2(2, 2));
        protected override bool OnTestMask(Vector2 point, Rect2 rect) => point == rect.Position;
        protected override void OnDraw(CanvasItem item, Rect2 rect) { LastTarget = item; LastCurrent = GetCurrentItemDrawn(); item.DrawRect(rect, Colors.Green); }
    }
    private sealed class Painter : Entity
    {
        internal Action<Painter>? Paint;
        internal Action<int>? Notified;
        protected override void OnNotification(int what) { base.OnNotification(what); Notified?.Invoke(what); }
        protected override void OnDraw() => Paint?.Invoke(this);
    }
    private sealed class StyleConsumer : Node
    {
        private static readonly PropertyDescriptor<StyleConsumer, StyleBox?> StyleProperty = new(nameof(Style), node => node.Style, (node, value) => node.Style = value, _ => null, stored: true);
        internal StyleBox? Style { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(StyleProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateConsumer;
        private static Node CreateConsumer() => new StyleConsumer();
    }
    private static Rect2 Bounds(List<CanvasVertex> vertices)
    {
        Check(vertices.Count > 0, "Expected actual retained geometry."); var minimum = vertices[0].Position; var maximum = minimum;
        foreach (var vertex in vertices) { minimum = minimum.Min(vertex.Position); maximum = maximum.Max(vertex.Position); }
        return new(minimum, maximum - minimum);
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
