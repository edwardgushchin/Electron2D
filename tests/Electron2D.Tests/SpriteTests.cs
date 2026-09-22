using Electron2D;

internal static class SpriteTests
{
    internal static void Run()
    {
        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            new byte[] { 255, 0, 0, 0, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        using var replacement = ImageTexture.CreateFromImage(image);
        using var sprite = new Sprite();
        Check(sprite.Centered && !sprite.FlipH && !sprite.FlipV && !sprite.RegionEnabled && !sprite.RegionFilterClipEnabled &&
            sprite.Texture is null && sprite.Offset == Vector2.Zero && sprite.Frame == 0 && sprite.FrameCoords == Vector2I.Zero &&
            sprite.HFrames == 1 && sprite.VFrames == 1 && sprite.RegionRect == default, "Sprite defaults.");
        Check(sprite.GetRect() == new Rect(0, 0, 1, 1) && !sprite.IsPixelOpaque(Vector2.Zero), "Empty sprite bounds and opacity.");
        var frames = 0; var textures = 0; var lists = 0;
        sprite.FrameChanged += () => frames++;
        sprite.TextureChanged += () => textures++;
        sprite.PropertyListChanged += _ => lists++;
        sprite.Texture = texture; sprite.Texture = texture; texture.EmitChanged();
        Check(textures == 1 && ReferenceEquals(sprite.Texture, texture), "Texture identity changes only.");
        Check(sprite.GetRect() == new Rect(-1, -1, 2, 2), "Centered bounds.");
        Check(!sprite.IsPixelOpaque(new(-0.5f, -0.5f)) && sprite.IsPixelOpaque(new(0.5f, -0.5f)) &&
            !sprite.IsPixelOpaque(new(1, 0)) && !sprite.IsPixelOpaque(new(-1.001f, 0)), "Half-open bounds and alpha.");
        sprite.FlipH = true;
        Check(sprite.IsPixelOpaque(new(-0.5f, -0.5f)) && !sprite.IsPixelOpaque(new(0.5f, -0.5f)), "Horizontal opacity flip.");
        sprite.FlipV = true;
        Check(!sprite.IsPixelOpaque(new(0.5f, 0.5f)), "Vertical opacity flip.");
        sprite.FlipH = sprite.FlipV = false;
        sprite.HFrames = 3; sprite.VFrames = 4; sprite.FrameCoords = new(2, 2);
        Check(sprite.Frame == 8 && frames == 1 && lists == 2, "Grid indexing and notifications.");
        sprite.HFrames = 5;
        Check(sprite.Frame == 12 && sprite.FrameCoords == new Vector2I(2, 2) && frames == 1, "Grid preserves coordinates without FrameChanged.");
        sprite.HFrames = 2;
        Check(sprite.Frame == 0 && frames == 1, "Dropped column resets frame.");
        sprite.Frame = 7; sprite.VFrames = 2;
        Check(sprite.Frame == 0 && frames == 2, "Dropped row resets frame.");
        sprite.VFrames = 1; sprite.Frame = 1; sprite.HFrames = 4;
        Check(sprite.Frame == 1 && sprite.FrameCoords == new Vector2I(1, 0), "Single-row column retention.");
        var grid = (sprite.HFrames, sprite.VFrames, sprite.Frame);
        Reject<ArgumentOutOfRangeException>(() => sprite.HFrames = 0);
        Reject<ArgumentOutOfRangeException>(() => sprite.VFrames = -1);
        Reject<ArgumentOutOfRangeException>(() => sprite.VFrames = int.MaxValue);
        Reject<ArgumentOutOfRangeException>(() => sprite.Frame = -1);
        Reject<ArgumentOutOfRangeException>(() => sprite.Frame = 4);
        Reject<ArgumentOutOfRangeException>(() => sprite.FrameCoords = new(4, 0));
        Reject<ArgumentOutOfRangeException>(() => sprite.FrameCoords = new(0, -1));
        Check(grid == (sprite.HFrames, sprite.VFrames, sprite.Frame), "Rejected changes preserve the grid.");
        Reject<ArgumentException>(() => sprite.Offset = new(float.NaN, 0));
        Reject<ArgumentException>(() => sprite.RegionRect = new(0, 0, float.PositiveInfinity, 1));
        Reject<ArgumentException>(() => sprite.IsPixelOpaque(new(float.NaN, 0)));
        sprite.HFrames = 2; sprite.VFrames = 1; sprite.Frame = 0;
        sprite.RegionEnabled = true; sprite.RegionRect = new(0, 0, 5.9f, 3.9f); sprite.Offset = new(2, 3);
        Check(sprite.GetRect() == new Rect(1, 1.5f, 2, 3), "Bounds truncate the region and then frame size.");
        sprite.RegionRect = default;
        Check(sprite.GetRect() == new Rect(2, 3, 1, 1) && !sprite.IsPixelOpaque(new(2, 3)), "Zero region has inspection fallback but no opaque pixels.");
        sprite.Centered = false; sprite.Offset = Vector2.Zero; sprite.RegionRect = new(0, 0, -2, -2); sprite.HFrames = 1;
        Check(!sprite.IsPixelOpaque(new(0.5f, 0.5f)) && sprite.IsPixelOpaque(new(1.5f, 1.5f)), "Negative source/destination flips cancel consistently with drawing.");
        sprite.RegionRect = new(-100, -100, 2, 2);
        Check(!sprite.IsPixelOpaque(new(1, 1)), "Outside regions clamp to the image edge.");
        sprite.RegionEnabled = false;
        sprite.Texture = replacement;
        texture.EmitChanged();
        Check(textures == 2 && !texture.IsDisposed, "Replacement borrows the old texture.");
        using var dead = new ImageTexture(); dead.Dispose();
        Reject<ObjectDisposedException>(() => sprite.Texture = dead);
        Check(ReferenceEquals(sprite.Texture, replacement), "Invalid texture replacement is atomic.");
        sprite.HFrames = 2; sprite.Frame = 0;
        Action fail = () => throw new ApplicationException("frame handler");
        sprite.FrameChanged += fail;
        Reject<ApplicationException>(() => sprite.Frame = 1);
        Check(sprite.Frame == 1, "A callback failure retains committed frame state.");
        sprite.FrameChanged -= fail;
        sprite.TextureChanged += fail;
        Reject<ApplicationException>(() => sprite.Texture = null);
        Check(sprite.Texture is null, "A callback failure retains committed texture state.");
        sprite.TextureChanged -= fail;
        sprite.Texture = texture;
        VerifyPacking(texture);
        VerifyRedraw(texture, replacement);
        VerifyRectChanges(texture, replacement);
        VerifyCustomTexture();
        sprite.Dispose();
        texture.EmitChanged();
        Check(!texture.IsDisposed && !replacement.IsDisposed, "Entity disposal does not dispose borrowed textures.");
        Reject<ObjectDisposedException>(() => sprite.GetRect());
        Reject<ObjectDisposedException>(() => sprite.Frame = 0);
        Console.WriteLine("Sprite managed checks passed.");
    }

    private static void VerifyPacking(ImageTexture texture)
    {
        using var sprite = new Sprite
        {
            Texture = texture,
            Centered = false,
            Offset = new(2, 3),
            FlipH = true,
            FlipV = true,
            RegionEnabled = true,
            RegionRect = new(1, 2, 8, 6),
            RegionFilterClipEnabled = true,
            HFrames = 4,
            VFrames = 3,
            Frame = 11,
        };
        using var scene = new PackedScene(); scene.Pack(sprite);
        using var copy = (Sprite)scene.Instantiate();
        Check(copy.GetType() == typeof(Sprite) && copy.Frame == 11 && copy.FrameCoords == new Vector2I(3, 2) &&
            copy.HFrames == 4 && copy.VFrames == 3 && ReferenceEquals(copy.Texture, texture) && !copy.Centered &&
            copy.Offset == new Vector2(2, 3) && copy.FlipH && copy.FlipV && copy.RegionEnabled &&
            copy.RegionRect == sprite.RegionRect && copy.RegionFilterClipEnabled, "Complete stored sprite reconstruction.");
        texture.ResourceLocalToScene = true;
        try
        {
            scene.Pack(sprite);
            using var local = (Sprite)scene.Instantiate();
            var localTexture = local.Texture!;
            Check(!ReferenceEquals(localTexture, texture) && localTexture is ImageTexture, "Scene-local texture duplicates.");
            local.Dispose();
            Check(localTexture.IsDisposed && !texture.IsDisposed, "Scene root owns only its scene-local duplicate.");
        }
        finally { texture.ResourceLocalToScene = false; }
    }

    private static void VerifyRedraw(ImageTexture first, ImageTexture second)
    {
        var sprite = new CountedSprite { Texture = first, Centered = false };
        using var tree = new SceneTree(sprite);
        sprite.PrepareCanvas(); sprite.PrepareCanvas(); Check(sprite.Draws == 1, "Retained sprite commands.");
        Task.Run(() => first.SetSizeOverride(new(4, 4))).GetAwaiter().GetResult();
        sprite.PrepareCanvas(); Check(sprite.Draws == 2 && sprite.GetRect().Size == new Vector2(4, 4), "Worker resource changes request owner-thread redraw.");
        Reject<InvalidOperationException>(() => Task.Run(() => sprite.Frame = 0).GetAwaiter().GetResult());
        sprite.Texture = second; sprite.PrepareCanvas(); first.EmitChanged(); sprite.PrepareCanvas();
        Check(sprite.Draws == 3, "Old texture notifications are disconnected.");
        sprite.DuringDraw = () => Task.Run(second.EmitChanged).GetAwaiter().GetResult();
        sprite.QueueRedraw(); sprite.PrepareCanvas(); sprite.DuringDraw = null; sprite.PrepareCanvas();
        Check(sprite.Draws == 5, "A resource notification during OnDraw is retained for the next frame.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) sprite.PrepareCanvas();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Unchanged sprite preparation allocates no managed memory.");
        first.SetSizeOverride(new(2, 2));
    }

    private sealed class CountedSprite : Sprite
    {
        internal int Draws;
        internal Action? DuringDraw;
        internal void ReportRectChange(bool sizeChanged = true) => NotifyItemRectChanged(sizeChanged);
        protected override void OnDraw() { Draws++; DuringDraw?.Invoke(); base.OnDraw(); }
    }

    private static void VerifyRectChanges(ImageTexture texture, ImageTexture replacement)
    {
        using var sprite = new CountedSprite();
        var trace = new List<string>();
        Rect observed = default;
        sprite.ItemRectChanged += sender =>
        {
            Check(ReferenceEquals(sender, sprite), "Rectangle event sender.");
            observed = sprite.GetRect(); trace.Add("rect");
        };
        sprite.TextureChanged += () => trace.Add("texture");
        sprite.FrameChanged += () => trace.Add("frame");
        sprite.PropertyListChanged += _ => trace.Add("list");
        void Expect(Action change, string expected)
        {
            trace.Clear(); change();
            Check(string.Join(',', trace) == expected, "Rectangle event sequence: " + expected);
            if (trace.Contains("rect")) Check(observed == sprite.GetRect(), "Rectangle callbacks observe committed geometry.");
        }
        Expect(() => sprite.Texture = texture, "texture,rect");
        Expect(() => sprite.Texture = texture, "");
        Expect(() => sprite.Centered = false, "rect");
        Expect(() => sprite.Centered = false, "");
        Expect(() => sprite.Offset = new(3, 4), "rect");
        Expect(() => sprite.Offset = new(3, 4), "");
        Expect(() => sprite.RegionRect = new(1, 0, 8, 6), "");
        Expect(() => sprite.RegionEnabled = true, "");
        Expect(() => sprite.RegionRect = new(0, 0, 8, 6), "rect");
        Expect(() => sprite.RegionRect = sprite.RegionRect, "");
        Expect(() => sprite.HFrames = 2, "rect,list");
        Expect(() => sprite.VFrames = 3, "rect,list");
        Expect(() => { sprite.HFrames = 2; sprite.VFrames = 3; }, "");
        Expect(() => sprite.Frame = 5, "rect,frame");
        Expect(() => sprite.FrameCoords = new(0, 1), "rect,frame");
        Expect(() => { sprite.Frame = 2; sprite.FrameCoords = new(0, 1); }, "");
        Expect(() => sprite.VFrames = 1, "rect,list");
        Check(sprite.Frame == 0, "Implicit frame adjustment commits before rectangle notification.");
        Expect(() => { sprite.FlipH = true; sprite.FlipV = true; sprite.RegionFilterClipEnabled = true; }, "");
        Expect(() => { sprite.Position = new(5, 6); sprite.Rotation = 1; sprite.Scale = new(2, 3); sprite.Skew = 0.2f; }, "");
        Expect(() => sprite.Texture = replacement, "texture,rect");
        Expect(() => Task.Run(() => { texture.EmitChanged(); replacement.SetSizeOverride(new(3, 3)); }).GetAwaiter().GetResult(), "");
        Expect(() => sprite.Texture = null, "texture,rect");
        Expect(() => sprite.Centered = true, "rect");
        Expect(() =>
        {
            Reject<ArgumentException>(() => sprite.Offset = new(float.NaN, 0));
            Reject<ArgumentException>(() => sprite.RegionRect = new(0, 0, float.NaN, 1));
            Reject<ArgumentOutOfRangeException>(() => sprite.Frame = 2);
            Reject<ArgumentOutOfRangeException>(() => sprite.HFrames = 0);
        }, "");

        sprite.PrepareCanvas(); var detachedDraws = sprite.Draws;
        Expect(() => sprite.ReportRectChange(), "rect");
        sprite.PrepareCanvas(); Check(sprite.Draws == detachedDraws, "Detached rectangle reports deliver without queueing redraw.");
        var childEvents = 0;
        sprite.AddChild(new Sprite());
        ((Sprite)sprite.Children[0]).ItemRectChanged += _ => childEvents++;
        using (var tree = new SceneTree(sprite))
        {
            sprite.PrepareCanvas(); var draws = sprite.Draws;
            Expect(() => sprite.ReportRectChange(false), "rect");
            sprite.PrepareCanvas(); Check(sprite.Draws == draws, "Position-only rectangle reports do not request redraw.");
            Expect(() => sprite.ReportRectChange(), "rect");
            sprite.PrepareCanvas(); Check(sprite.Draws == draws + 1, "Size rectangle reports request redraw.");
            Expect(() =>
            {
                sprite.Visible = false; sprite.ProcessMode = NodeProcessMode.Disabled;
                sprite.Offset = new(8, 9);
            }, "rect");
            Check(childEvents == 0, "Rectangle events stay local, even while hidden or not processing.");
            Expect(() =>
            {
                Reject<InvalidOperationException>(() => Task.Run(() => sprite.ReportRectChange(false)).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => sprite.Centered = false).GetAwaiter().GetResult());
            }, "");

            Action<CanvasItem> failRect = _ => throw new ApplicationException("rectangle handler");
            sprite.ItemRectChanged += failRect;
            Expect(() => Reject<ApplicationException>(() => sprite.Frame = 1), "rect");
            Check(sprite.Frame == 1, "Failed rectangle callback retains frame and skips later frame event.");
            Expect(() => Reject<ApplicationException>(() => sprite.HFrames = 1), "rect");
            Check(sprite.HFrames == 1 && sprite.Frame == 0, "Failed rectangle callback retains grid and skips property-list event.");
            sprite.ItemRectChanged -= failRect;
            Action failTexture = () => throw new ApplicationException("texture handler");
            sprite.TextureChanged += failTexture;
            Expect(() => Reject<ApplicationException>(() => sprite.Texture = texture), "texture");
            sprite.TextureChanged -= failTexture;
            Check(ReferenceEquals(sprite.Texture, texture), "Texture failure commits state and skips rectangle notification.");
            sprite.PrepareCanvas(); Check(sprite.Draws == draws + 2, "Callback failures retain pending redraw.");
        }
        Reject<ObjectDisposedException>(() => sprite.ReportRectChange());
        replacement.SetSizeOverride(new(2, 2));

        using var reentrant = new Sprite();
        var nestedEvents = 0;
        reentrant.ItemRectChanged += _ => { nestedEvents++; if (nestedEvents == 1) reentrant.Offset = Vector2.One; };
        reentrant.Centered = false;
        Check(nestedEvents == 2 && reentrant.Offset == Vector2.One, "Reentrant geometry changes each deliver once.");
        using var disposeFromTexture = new Sprite();
        disposeFromTexture.TextureChanged += disposeFromTexture.Dispose;
        disposeFromTexture.ItemRectChanged += _ => throw new InvalidOperationException("Disposed rectangle event.");
        disposeFromTexture.Texture = texture;
        using var disposeFromRect = new Sprite { HFrames = 2 };
        disposeFromRect.ItemRectChanged += item => item.Dispose();
        disposeFromRect.FrameChanged += () => throw new InvalidOperationException("Disposed frame event.");
        disposeFromRect.Frame = 1;
        Check(disposeFromTexture.IsDisposed && disposeFromRect.IsDisposed && !texture.IsDisposed, "Callback disposal ends remaining events without releasing borrowed texture.");
    }

    private static void VerifyCustomTexture()
    {
        using var texture = new RegionTexture();
        using var sprite = new Sprite
        {
            Texture = texture,
            Centered = false,
            RegionEnabled = true,
            RegionRect = new(4, 6, 8, 4),
            HFrames = 2,
            Frame = 1,
            RegionFilterClipEnabled = true,
        };
        sprite.PrepareCanvas();
        Check(texture.Destination == new Rect(0, 0, 4, 4) && texture.Source == new Rect(8, 6, 4, 4) && texture.Clip,
            "Sprite dispatches the selected frame through the texture's virtual region drawing hook.");
        texture.LogicalWidth = -1;
        Reject<InvalidOperationException>(() => sprite.IsPixelOpaque(Vector2.One));
        texture.LogicalWidth = 2;
        sprite.HFrames = 1; sprite.RegionRect = new(0, 0, float.MaxValue, 2);
        sprite.Centered = true; sprite.Offset = new(-float.MaxValue, 0);
        Reject<InvalidOperationException>(() => sprite.GetRect());
        Reject<InvalidOperationException>(() => sprite.PrepareCanvas());
        texture.LogicalWidth = 0; texture.Dispose();
        Reject<ObjectDisposedException>(() => sprite.IsPixelOpaque(Vector2.Zero));
    }

    private sealed class RegionTexture : Texture
    {
        internal int LogicalWidth = 2;
        internal Rect Destination, Source;
        internal bool Clip;
        public override int Width => LogicalWidth;
        public override int Height => 2;
        public override void DrawRectRegion(CanvasItem canvasItem, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
        {
            Destination = rect; Source = sourceRect; Clip = clipUV;
            canvasItem.DrawRect(rect, Colors.Magenta);
        }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
