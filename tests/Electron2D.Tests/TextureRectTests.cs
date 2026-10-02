using Electron2D;

internal static class TextureRectTests
{
    internal static void Run()
    {
        using var defaults = new TextureRect();
        Check(defaults.Texture is null && defaults.ExpandMode == TextureRectExpandMode.KeepSize && defaults.StretchMode == TextureStretchMode.Scale && !defaults.FlipH && !defaults.FlipV && defaults.MouseFilter == MouseFilter.Pass && defaults.GetMinimumSize() == Vector2.Zero, "Texture rectangle defaults match the public contract.");
        VerifyMinimums(); VerifyResourceLifetime(); VerifyPackingAndFlow(); VerifyGeometryAndAllocation(); VerifyFailedObserversAndQueries(); VerifyFlowModes();
        Console.WriteLine("Texture rectangle modes/minima, lifecycle, guards, packing, flow stabilization, geometry and zero-allocation checks passed.");
    }
    private static void VerifyMinimums()
    {
        using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        using var node = new TextureRect { Texture = texture, Size = new(30, 20) };
        var expected = new[] { new Vector2(8, 4), Vector2.Zero, new Vector2(20, 0), new Vector2(40, 0), new Vector2(0, 30), new Vector2(0, 15) };
        for (var i = 0; i < 6; i++) { node.ExpandMode = (TextureRectExpandMode)i; Check(node.GetMinimumSize() == expected[i], $"Expand minimum branch {i}."); }
        using var empty = new ImageTexture(); node.Texture = empty;
        node.ExpandMode = TextureRectExpandMode.FitWidthProportional; Check(node.GetMinimumSize() == Vector2.Zero, "Empty proportional width avoids division by zero.");
        node.ExpandMode = TextureRectExpandMode.FitHeightProportional; Check(node.GetMinimumSize() == Vector2.Zero, "Empty proportional height avoids division by zero.");
        Reject<ArgumentOutOfRangeException>(() => node.ExpandMode = (TextureRectExpandMode)6); Reject<ArgumentOutOfRangeException>(() => node.StretchMode = (TextureStretchMode)7);
        Check(node.ExpandMode == TextureRectExpandMode.FitHeightProportional && node.StretchMode == TextureStretchMode.Scale, "Invalid modes preserve prior state.");
    }
    private static void VerifyResourceLifetime()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        var node = new TextureRect { Texture = texture }; using var tree = new SceneTree(node); tree.ProcessFrame(0);
        using var replacement = Image.CreateEmpty(6, 3, false, Image.Format.Rgba8);
        Task.Run(() => texture.SetImage(replacement)).GetAwaiter().GetResult(); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(node.GetMinimumSize() == new Vector2(6, 3), "Off-thread resource edits invalidate minimums on the scene owner.");
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 2, 2), Margin = new(1, 1, 1, 1) }; node.Texture = atlas;
        node.StretchMode = TextureStretchMode.Tile; Check(node.GetConfigurationWarnings().Length == 1, "Tiled atlas margins produce the concrete warning.");
        atlas.Margin = default; Check(node.GetConfigurationWarnings().Length == 0, "Cleared atlas margins clear the warning.");
        Check(Task.Run(() => Capture(() => node.FlipH = true)).Result is InvalidOperationException && Task.Run(() => Capture(() => _ = node.Texture)).Result is InvalidOperationException, "Node access remains owner-affine.");
        Task.Run(atlas.Dispose).GetAwaiter().GetResult(); tree.ProcessFrame(0); Check(node.Texture is null, "Off-thread disposal clears the borrowed binding without ownership transfer.");
        Check(!texture.IsDisposed, "View/node cleanup preserves the underlying texture resource.");
    }
    private static void VerifyPackingAndFlow()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        using var node = new TextureRect { Name = "Image", Texture = texture, ExpandMode = TextureRectExpandMode.IgnoreSize, StretchMode = TextureStretchMode.KeepAspectCovered, FlipH = true, FlipV = true };
        using var packed = new PackedScene(); packed.Pack(node); using var copied = packed.Instantiate();
        Check(copied is TextureRect copy && ReferenceEquals(copy.Texture, texture) && copy.ExpandMode == node.ExpandMode && copy.StretchMode == node.StretchMode && copy.FlipH && copy.FlipV && copy.MouseFilter == MouseFilter.Pass, "Packing preserves exact image-control identity, enums, flips and borrowed texture.");
        var flow = new HFlowContainer { Size = new(20, 80), HSeparation = 0 };
        var a = new TextureRect { Name = "A", Texture = texture, Size = new(15, 15), ExpandMode = TextureRectExpandMode.FitWidth, CustomMinimumSize = new(15, 0) };
        var b = new TextureRect { Name = "B", Texture = texture, Size = new(15, 15), ExpandMode = TextureRectExpandMode.FitWidth, CustomMinimumSize = new(15, 0) };
        flow.AddChild(a); flow.AddChild(b); using var tree = new SceneTree(flow);
        for (var i = 0; i < 8; i++) tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 2 && a.Size == new Vector2(15, 15) && b.Size == new Vector2(15, 15), "Multi-wrap fitting retains current fit-mode sizes and terminates.");
        flow.Size = new(60, 80); for (var i = 0; i < 4; i++) tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 1, "Single wrap resumes ordinary layout fitting.");
    }
    private static void VerifyGeometryAndAllocation()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        var node = new TextureRect { Texture = texture, ExpandMode = TextureRectExpandMode.IgnoreSize, Size = new(9, 7) }; using var tree = new SceneTree(node);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var mode = 0; mode < 7; mode++)
        {
            node.StretchMode = (TextureStretchMode)mode; node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
            Check(vertices.Count > 0 && vertices.All(v => v.Position.IsFinite() && v.UV.IsFinite()), $"Stretch mode {mode} records actual finite textured geometry.");
            var min = vertices.Select(v => v.Position).Aggregate((a, b) => a.Min(b)); var max = vertices.Select(v => v.Position).Aggregate((a, b) => a.Max(b));
            if (mode == 4) Check(max - min == new Vector2(9, 4), "Aspect fit truncates integer width/height.");
            if (mode == 5) Check(min == new Vector2(0, 1.5f) && max == new Vector2(9, 5.5f), "Centered integer fit keeps fractional offset.");
        }
        for (var i = 0; i < 64; i++) { node.FlipH = i % 2 == 0; node.QueueRedraw(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); tree.ProcessFrame(0); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { node.FlipH = i % 2 == 0; node.QueueRedraw(); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity); tree.ProcessFrame(0); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(bytes == 0, $"Warmed reflected image recording/replay/resource polls allocated {bytes} bytes.");
        using var disposed = new TextureRect(); disposed.Dispose(); Reject<ObjectDisposedException>(() => _ = disposed.Texture); Reject<ObjectDisposedException>(() => disposed.FlipV = true);
    }
    private static void VerifyFailedObserversAndQueries()
    {
        using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        using var inner = new AtlasTexture { Atlas = texture, Region = new(0, 0, 2, 2) };
        Action<Resource> fail = _ => throw new ApplicationException("expected early observer"); inner.Changed += fail;
        using var outer = new AtlasTexture { Atlas = inner }; var node = new TextureRect { Texture = outer, ExpandMode = TextureRectExpandMode.IgnoreSize, Size = new(10, 8) };
        using var tree = new SceneTree(node); tree.ProcessFrame(0); node.PrepareCanvas();
        tree.EditedSceneRoot = node; var warningChanges = 0; tree.NodeConfigurationWarningChanged += (_, _) => warningChanges++;
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity); var uv = vertices[0].UV;
        Reject<ApplicationException>(() => inner.Region = new(2, 0, 2, 2)); tree.ProcessFrame(0); node.PrepareCanvas(); vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].UV != uv, "Nested-atlas revision polling repairs missed change forwarding after an earlier observer fails.");
        Check(warningChanges > 0, "Missed nested-atlas notifications also refresh selected-scene warnings.");
        inner.Changed -= fail;
        using var probe = new QueryTexture(); node.Texture = probe; node.ExpandMode = TextureRectExpandMode.KeepSize;
        probe.Query = () => { probe.Query = null; node.Texture = texture; };
        Check(node.GetMinimumSize() == new Vector2(8, 4), "Reentrant texture size queries retry the committed replacement.");
        node.Texture = probe; probe.Query = () => node.FlipH = !node.FlipH;
        Reject<InvalidOperationException>(() => node.GetMinimumSize()); probe.Query = null;
        probe.SizeValue = new(float.NaN, 2); Reject<InvalidOperationException>(() => node.GetMinimumSize());
        probe.SizeValue = new(-1, 2); Reject<InvalidOperationException>(() => node.GetMinimumSize());
        node.Texture = texture; node.ExpandMode = TextureRectExpandMode.IgnoreSize;
        Action<ElectronObject> failDisposal = _ => throw new ApplicationException("expected early disposal observer");
        using var disposable = new ImageTexture(); disposable.Disposed += failDisposal; node.Texture = disposable;
        Check(Capture(disposable.Dispose) is not null, "Disposal commits despite observer failure."); tree.ProcessFrame(0);
        Check(node.Texture is null, "Polling clears disposed texture after missed disposal notification.");
        Action<SceneTree, Node> failWarning = (_, _) => throw new ApplicationException("expected warning observer"); tree.NodeConfigurationWarningChanged += failWarning;
        Reject<ApplicationException>(() => node.Texture = texture); Check(ReferenceEquals(node.Texture, texture), "Warning failure follows committed texture assignment.");
        tree.NodeConfigurationWarningChanged -= failWarning; tree.ProcessFrame(0); node.ExpandMode = TextureRectExpandMode.KeepSize;
        Check(node.GetMinimumSize() == new Vector2(8, 4), "Minimum invalidation survives a warning callback failure.");
        tree.Dispose(); Check(!texture.IsDisposed, "Disposing an image control leaves its texture usable.");
    }
    private static void VerifyFlowModes()
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        for (var orientation = 0; orientation < 2; orientation++)
            for (var mode = 2; mode < 6; mode++)
                for (var direction = 0; direction < 4; direction++)
                {
                    var flow = new FlowContainer { Vertical = orientation != 0, Size = orientation == 0 ? new(20, 80) : new(80, 20), HSeparation = 0, VSeparation = 0, ReverseFill = (direction & 1) != 0, LayoutDirection = (direction & 2) != 0 ? LayoutDirection.RTL : LayoutDirection.LTR };
                    var a = new TextureRect { Name = "A", Texture = texture, Size = new(15, 15), CustomMinimumSize = new(15, 15), ExpandMode = (TextureRectExpandMode)mode };
                    var b = new TextureRect { Name = "B", Texture = texture, Size = new(15, 15), CustomMinimumSize = new(15, 15), ExpandMode = (TextureRectExpandMode)mode };
                    flow.AddChild(a); flow.AddChild(b); using var tree = new SceneTree(flow);
                    for (var frame = 0; frame < 8; frame++) tree.ProcessFrame(0);
                    var rtl = (direction & 2) != 0; var reverse = (direction & 1) != 0;
                    var first = orientation == 0 ? new Vector2(rtl ? 5 : 0, reverse ? 65 : 0) : new Vector2(rtl != reverse ? 65 : 0, 0);
                    var second = orientation == 0 ? new Vector2(first.X, reverse ? 50 : 15) : new Vector2(rtl != reverse ? 50 : 15, 0);
                    Check(flow.GetLineCount() == 2 && a.Size == new Vector2(15, 15) && b.Size == a.Size && a.Position == first && b.Position == second, "All four fit modes retain multi-wrap sizes under both orientations, RTL and reverse fill.");
                }
    }
    private sealed class QueryTexture : Texture
    {
        internal Vector2 SizeValue = new(4, 2);
        internal Action? Query;
        public override int GetWidth() => (int)SizeValue.X;
        public override int GetHeight() => (int)SizeValue.Y;
        public override Vector2 GetSize() { Query?.Invoke(); return SizeValue; }
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception e) { return e; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
