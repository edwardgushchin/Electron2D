using Electron2D;

internal static class CanvasClipTests
{
    internal static void Run()
    {
        using var item = new Entity();
        Check(item.ClipChildren == ClipChildrenMode.Disabled, "Default mode.");
        foreach (var mode in new[] { (ClipChildrenMode)(-1), ClipChildrenMode.Max, (ClipChildrenMode)99 }) Reject<ArgumentOutOfRangeException>(() => item.ClipChildren = mode);
        Check(item.ClipChildren == ClipChildrenMode.Disabled, "Invalid writes preserve mode.");
        item.ClipChildren = ClipChildrenMode.Only;
        using var packed = new PackedScene(); packed.Pack(item); using var restored = (Entity)packed.Instantiate(); Check(restored.ClipChildren == ClipChildrenMode.Only, "Packed mask policy.");
        var root = new TestViewport(); var group = new CanvasGroup { Name = "group", ClipChildren = ClipChildrenMode.AndDraw }; var neutral = new Node(); var inner = new Entity { ClipChildren = ClipChildrenMode.Only }; root.AddChild(group); group.AddChild(neutral); neutral.AddChild(inner);
        using var tree = new SceneTree(root); tree.EditedSceneRoot = root;
        Check(inner.GetConfigurationWarnings().Length == 2 && group.GetConfigurationWarnings().Length == 0, "Physical ancestor mask/group warnings.");
        var notifications = 0; tree.NodeConfigurationWarningChanged += (_, _) => notifications++;
        inner.ClipChildren = ClipChildrenMode.Only; Check(notifications == 0, "Equal assignment is silent."); inner.ClipChildren = ClipChildrenMode.Disabled; Check(notifications == 1 && inner.GetConfigurationWarnings().Length == 0, "Changed mode refreshes warnings.");
        Action<SceneTree, Node> fail = (_, _) => throw new ApplicationException("warning fixture"); tree.NodeConfigurationWarningChanged += fail; Reject<ApplicationException>(() => inner.ClipChildren = ClipChildrenMode.AndDraw); Check(inner.ClipChildren == ClipChildrenMode.AndDraw, "Observer failure commits mode."); tree.NodeConfigurationWarningChanged -= fail;
        Task.Run(() => { Reject<InvalidOperationException>(() => inner.ClipChildren = ClipChildrenMode.Disabled); Reject<InvalidOperationException>(() => _ = inner.ClipChildren); }).GetAwaiter().GetResult();
        item.Dispose(); Reject<ObjectDisposedException>(() => _ = item.ClipChildren);
        Console.WriteLine("Canvas alpha-mask authoring, enum bounds, packing, warnings and owner checks passed.");
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 128, 96); }
    private sealed class Paint : Entity
    {
        internal Color Fill = Colors.Red;
        internal Rect2 Rect = new(0, 0, 16, 16);
        internal Texture? AlphaTexture;
        internal bool Triangle, Overlap, NoGeometry;
        protected override void OnDraw()
        {
            if (NoGeometry) { DrawSetTransform(Vector2.Zero); return; }
            if (AlphaTexture is { } texture) DrawTextureRect(texture, Rect, false, Fill);
            else if (Triangle) DrawPolygon([new(0, 0), new(16, 0), new(0, 16)], [Fill]);
            else DrawRect(Rect, Fill);
            if (Overlap) DrawRect(Rect, Fill);
        }
    }
    internal static void RunHost(string backend)
    {
        Masks(backend); Boundaries(backend); Offscreen(backend); Warm(backend); Nested();
    }
    private static void Masks(string backend)
    {
        var window = new Window { Size = new(128, 96) };
        var mask = new Paint { Name = "mask", Position = new(8, 8), ClipChildren = ClipChildrenMode.Only, Fill = new(0, 1, 0, .5f), Triangle = true, TextureFilter = TextureFilter.Nearest };
        mask.AddChild(new Paint { Name = "child", Fill = Colors.Red, Rect = new(0, 0, 24, 24) }); mask.AddChild(new Paint { Name = "z", Position = new(0, 28), Fill = Colors.Lime, ZIndex = 1 }); mask.AddChild(new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport }); window.AddChild(mask);
        var empty = new Entity { Name = "empty", Position = new(48, 8), ClipChildren = ClipChildrenMode.Only }; empty.AddChild(new Paint { Name = "empty_child", Fill = Colors.Blue }); window.AddChild(empty);
        var bare = new Paint { Name = "bare", Position = new(80, 8), Fill = Colors.Lime, ClipChildren = ClipChildrenMode.Only }; window.AddChild(bare);
        var phase = 0;
        using var alpha = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8); alpha.SetPixel(0, 0, Colors.White); alpha.SetPixel(1, 0, new(1, 1, 1, .25f)); using var texture = ImageTexture.CreateFromImage(alpha);
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Blue);
            renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                Pixel(image, 52, 12, Colors.Blue); Pixel(image, 84, 12, Colors.Lime); if (phase < 5) Pixel(image, 12, 40, Colors.Lime);
                if (phase == 0) { Pixel(image, 12, 12, new(.5f, 0, .5f)); Pixel(image, 22, 22, Colors.Blue); mask.ClipChildren = ClipChildrenMode.AndDraw; }
                else if (phase == 1) { Pixel(image, 12, 12, new(.5f, 0, .5f)); mask.GetNode<Paint>("child").Visible = false; }
                else if (phase == 2) { Pixel(image, 12, 12, new(0, .25f, .75f)); mask.GetNode<Paint>("child").Visible = true; mask.Triangle = false; mask.AlphaTexture = texture; mask.Fill = Colors.White; mask.ClipChildren = ClipChildrenMode.Only; mask.QueueRedraw(); }
                else if (phase == 3) { Pixel(image, 12, 12, Colors.Red); Pixel(image, 20, 12, new(.25f, 0, .75f)); mask.AlphaTexture = null; mask.Fill = new(1, 1, 1, .5f); mask.Overlap = true; mask.QueueRedraw(); }
                else if (phase == 4) { Pixel(image, 12, 12, new(.75f, 0, .25f)); mask.Position = new(32, 24); mask.Rotation = Mathf.Pi / 2; mask.Overlap = false; mask.QueueRedraw(); }
                else if (phase == 5) { Pixel(image, 28, 28, new(.5f, 0, .5f)); Pixel(image, 12, 12, Colors.Blue); mask.Rotation = 0; mask.Position = new(8, 8); mask.NoGeometry = true; mask.QueueRedraw(); }
                else if (phase == 6) { Pixel(image, 12, 12, Colors.Blue); window.Tree!.Quit(); }
                phase++;
            };
        };
        Check(Engine.Instance.Run(window) == 0 && phase == 7 && window.IsDisposed, "Mask host lifecycle."); Console.WriteLine($"Canvas alpha masks, texture alpha, overlap, Only/AndDraw, no commands, no geometry, Z and transforms passed ({backend}).");
    }
    private static void Boundaries(string backend)
    {
        var window = new Window { Size = new(128, 96) }; var parent = new Entity { YSortEnabled = true };
        var mask = new Paint { Name = "mask", ClipChildren = ClipChildrenMode.Only, Fill = new(1, 1, 1, .5f), YSortEnabled = true, Rect = new(0, 0, 24, 24) };
        mask.AddChild(new Paint { Name = "child", Position = new(0, 8) });
        mask.AddChild(new Paint { Name = "top", TopLevel = true, Position = new(48, 8), Fill = Colors.Lime });
        var neutral = new Node { Name = "neutral" }; neutral.AddChild(new Paint { Name = "neutral_child", Position = new(80, 8), Fill = Colors.Lime }); mask.AddChild(neutral);
        parent.AddChild(mask); var outside = new Paint { Name = "outside", Position = new(0, 4), Fill = Colors.Blue }; parent.AddChild(outside); window.AddChild(parent); var phase = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback(); Pixel(image, 4, 12, phase == 0 ? Colors.Blue : new(.5f, 0, 0)); Pixel(image, 52, 12, Colors.Lime); Pixel(image, 84, 12, Colors.Lime);
                if (phase++ == 0) outside.Position = new(0, -4); else window.Tree!.Quit();
            };
        };
        Check(Engine.Instance.Run(window) == 0 && phase == 2, "Mask ancestry/Y-sort lifecycle."); Console.WriteLine($"Canvas mask parent/internal Y-sort, TopLevel and neutral boundaries passed ({backend}).");
    }
    private static void Offscreen(string backend)
    {
        var window = new Window { Size = new(96, 64) }; var viewport = new SubViewport { Size = new(32, 32), TransparentBG = true, RenderTargetUpdateMode = ViewportUpdateMode.Always }; var mask = new Paint { Name = "mask", Fill = new(1, 1, 1, .5f), ClipChildren = ClipChildrenMode.Only }; mask.AddChild(new Paint { Name = "child", Fill = Colors.Red }); viewport.AddChild(mask); window.AddChild(viewport); var phase = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.FramePostDraw += () =>
            {
                using var image = viewport.GetTexture().GetImage()!; Pixel(image, 4, 4, new(.5f, 0, 0, .5f)); Pixel(image, 20, 20, default);
                if (phase++ == 0) viewport.Size = new(48, 48); else window.Tree!.Quit();
            };
        };
        Check(Engine.Instance.Run(window) == 0 && phase == 2, "Offscreen mask lifecycle."); Console.WriteLine($"Transparent SubViewport mask and resize passed ({backend}).");
    }
    private static void Warm(string backend)
    {
        var window = new Window { Size = new(64, 64) }; var mask = new Paint { Name = "mask", ClipChildren = ClipChildrenMode.AndDraw }; mask.AddChild(new Paint { Name = "child" }); window.AddChild(mask); var frames = 0; long allocated = 0; var prior = GC.GetAllocatedBytesForCurrentThread();
        window.Ready += _ => RenderingServer.Instance!.FramePostDraw += () =>
        {
            var now = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 32) allocated += now - prior; prior = now;
            if (frames < 96) { mask.Position = new((frames & 1) * 2, 0); mask.ClipChildren = (frames & 1) == 0 ? ClipChildrenMode.AndDraw : ClipChildrenMode.Only; mask.SelfModulate = new(1, 1, 1, (frames & 1) == 0 ? .5f : .75f); }
            if (++frames == 160) window.Tree!.Quit();
        };
        Check(Engine.Instance.Run(window) == 0 && frames == 160 && allocated == 0, $"Warmed mask frames allocated {allocated} bytes."); Console.WriteLine($"64 active + 64 idle warmed mask frames: {allocated} managed bytes ({backend}).");
    }
    internal static void Materials(ShaderMaterial material, string language)
    {
        var window = new Window { Size = new(64, 64) }; var mask = new Paint { Name = "mask", ClipChildren = ClipChildrenMode.Only, Fill = Colors.White, Material = material }; mask.AddChild(new Paint { Name = "child", Fill = Colors.Red }); window.AddChild(mask); var phase = 0;
        material.SetShaderParameter("tint", Colors.Blue);
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback(); Pixel(image, 4, 4, phase == 0 ? Colors.Black : Colors.Red);
                if (phase++ == 0) { mask.ClipChildren = ClipChildrenMode.AndDraw; mask.Material = null; } else window.Tree!.Quit();
            };
        };
        Check(Engine.Instance.Run(window) == 0 && phase == 2, "Custom Only shader lifecycle."); material.SetShaderParameter("tint", Colors.White); Console.WriteLine($"Mask custom Only material overrides built-in screen shading ({language}).");
        using var plain = new CanvasItemMaterial(); var baseline = new Window { Size = new(64, 64) }; var owner = new Paint { Name = "owner", Fill = Colors.Lime, Material = plain, ClipChildren = ClipChildrenMode.AndDraw }; owner.AddChild(new Paint { Name = "child", Fill = Colors.Red }); baseline.AddChild(owner); var checkedFrame = false;
        baseline.Ready += _ => RenderingServer.Instance!.FramePostDraw += () => { using var image = RenderingServer.Instance.Readback(); Pixel(image, 4, 4, Colors.Red); checkedFrame = true; baseline.Tree!.Quit(); };
        Check(Engine.Instance.Run(baseline) == 0 && checkedFrame, "AndDraw retains built-in final mask with assigned material.");
        foreach (var earlyOwner in new[] { false, true })
        {
            var rejected = new Window(); var clip = new Paint { Name = "clip", ClipChildren = earlyOwner ? ClipChildrenMode.AndDraw : ClipChildrenMode.Only, Material = earlyOwner ? material : null }; clip.AddChild(new Paint { Name = "child", Material = earlyOwner ? null : material }); rejected.AddChild(clip);
            Reject<NotSupportedException>(() => Engine.Instance.Run(rejected)); Check(rejected.IsDisposed, "Writable-screen read rejects and cleans up.");
        }
    }
    internal static void RejectSoftware()
    {
        var window = new Window(); var owner = new Paint { Name = "mask", ClipChildren = ClipChildrenMode.Only }; owner.AddChild(new Paint { Name = "child" }); window.AddChild(owner); Reject<NotSupportedException>(() => Engine.Instance.Run(window)); Check(window.IsDisposed, "Software mask capability cleanup.");
    }
    private static void Nested()
    {
        var window = new Window(); var outer = new Paint { Name = "outer", ClipChildren = ClipChildrenMode.Only }; var inner = new Paint { Name = "inner", ClipChildren = ClipChildrenMode.AndDraw }; inner.AddChild(new Paint { Name = "child" }); outer.AddChild(inner); window.AddChild(outer);
        Reject<NotSupportedException>(() => Engine.Instance.Run(window)); Check(window.IsDisposed, "Nested mask rejects and cleans up.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y); if (Mathf.Abs(actual.R - expected.R) > .025f || Mathf.Abs(actual.G - expected.G) > .025f || Mathf.Abs(actual.B - expected.B) > .025f || Mathf.Abs(actual.A - expected.A) > .025f) throw new InvalidOperationException($"Mask pixel ({x},{y}) {actual}, expected {expected}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
