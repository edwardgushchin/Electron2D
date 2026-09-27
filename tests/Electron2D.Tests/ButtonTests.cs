using Electron2D;

internal static class ButtonTests
{
    internal static void Run(byte[] fontBytes)
    {
        VerifyDefaultsAndStorage();
        VerifyMinimumAndIconLayout(fontBytes);
        VerifyTextAndClipping(fontBytes);
        VerifyIndicators(fontBytes);
        VerifyResourceUpdates(fontBytes);
        VerifyResidency(fontBytes);
        VerifyWarmFrames(fontBytes);
        Console.WriteLine("Buttons verify defaults, typed storage, state styles, icon/text layout, wrapping/clipping, check indicators, resource lifetime and warmed reuse.");
    }
    private static FontFile Font(byte[] bytes) => new() { Data = bytes, SubpixelPositioning = FontSubpixelPositioning.Quarter };
    private static ImageTexture Texture(int width, int height)
    {
        using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8); image.Fill(Colors.White); return ImageTexture.CreateFromImage(image);
    }
    private static void Configure(Button button, Font font, StyleBox style)
    {
        button.AddThemeFontOverride("font", font); button.AddThemeFontSizeOverride("font_size", 16);
        foreach (var state in new[] { "normal", "pressed", "hover", "disabled", "hover_pressed", "focus" }) button.AddThemeStyleBoxOverride(state, style);
        button.AddThemeConstantOverride("line_spacing", 0);
    }
    private static void VerifyDefaultsAndStorage()
    {
        using var button = new Button();
        Check(button.Text == "" && button.Icon is null && !button.Flat && !button.ClipText && !button.ExpandIcon &&
            button.Alignment == HorizontalAlignment.Center && button.IconAlignment == HorizontalAlignment.Left && button.VerticalIconAlignment == VerticalAlignment.Center &&
            button.AutowrapMode == TextAutowrapMode.Off && button.AutowrapTrimFlags == TextLineBreakFlags.TrimEndEdgeSpaces &&
            button.TextOverrunBehavior == TextOverrunBehavior.NoTrimming && button.TextDirection == TextDirection.Auto && button.Language == "" &&
            button.MouseFilter == MouseFilter.Stop && button.FocusMode == FocusMode.All && button.GetMinimumSize() == new Vector2(8, 8),
            "Button defaults include real empty state style geometry, text and inherited input values.");
        using var box = new CheckBox(); using var toggle = new CheckButton();
        Check(box.ToggleMode && toggle.ToggleMode && box.Alignment == HorizontalAlignment.Left && toggle.Alignment == HorizontalAlignment.Left,
            "Check controls override toggle and alignment defaults.");
        Check(box.GetMinimumSize().X > button.GetMinimumSize().X && toggle.GetMinimumSize().X > box.GetMinimumSize().X,
            "Default theme indicators participate in actual minimum sizing.");
        button.AutowrapTrimFlags = (TextLineBreakFlags)255; Check(button.AutowrapTrimFlags == (TextLineBreakFlags)224, "Trim flags preserve only their source mask.");
        button.Alignment = (HorizontalAlignment)99; button.IconAlignment = (HorizontalAlignment)99; button.VerticalIconAlignment = (VerticalAlignment)99;
        button.AutowrapMode = (TextAutowrapMode)99; button.TextOverrunBehavior = (TextOverrunBehavior)99;
        Check((int)button.Alignment == 99 && (int)button.IconAlignment == 99 && (int)button.VerticalIconAlignment == 99 && (int)button.AutowrapMode == 99 && (int)button.TextOverrunBehavior == 99,
            "Unguarded source enum storage retains unknown values.");
        Reject<ArgumentNullException>(() => button.Text = null!); Reject<ArgumentException>(() => button.Language = "en\0x");
        Reject<ArgumentOutOfRangeException>(() => button.TextDirection = (TextDirection)4);
        button.TextDirection = (TextDirection)(-1); Check((int)button.TextDirection == -1, "Legacy automatic direction remains stored.");
        using var icon = Texture(7, 5);
        using var original = new CheckButton("packed")
        {
            Icon = icon,
            Flat = true,
            ClipText = true,
            ExpandIcon = true,
            Language = "ar",
            IconAlignment = HorizontalAlignment.Right,
            VerticalIconAlignment = VerticalAlignment.Top,
            AutowrapMode = TextAutowrapMode.WordSmart,
            AutowrapTrimFlags = TextLineBreakFlags.TrimIndent,
            TextOverrunBehavior = TextOverrunBehavior.TrimWordEllipsis,
            TextDirection = TextDirection.RTL
        };
        using var scene = new PackedScene(); scene.Pack(original); using var restored = (CheckButton)scene.Instantiate();
        Check(restored.GetType() == typeof(CheckButton) && restored.Text == "packed" && ReferenceEquals(restored.Icon, icon) && restored.ToggleMode &&
            restored.Flat && restored.ClipText && restored.ExpandIcon && restored.Language == "ar" && restored.IconAlignment == HorizontalAlignment.Right &&
            restored.VerticalIconAlignment == VerticalAlignment.Top && restored.AutowrapMode == TextAutowrapMode.WordSmart &&
            restored.AutowrapTrimFlags == TextLineBreakFlags.TrimIndent && restored.TextOverrunBehavior == TextOverrunBehavior.TrimWordEllipsis && restored.TextDirection == TextDirection.RTL,
            "Packing retains exact subtype and all applicable button state while borrowing nonlocal icons.");
        var alignment = box.GetPropertyList().Single(p => p.Name == nameof(Button.Alignment)); box.Alignment = HorizontalAlignment.Center; alignment.Revert(box);
        var toggling = box.GetPropertyList().Single(p => p.Name == nameof(BaseButton.ToggleMode)); box.ToggleMode = false; toggling.Revert(box);
        Check(box.Alignment == HorizontalAlignment.Left && box.ToggleMode, "Typed subtype descriptors retain subtype-specific revert defaults.");
    }
    private static void VerifyMinimumAndIconLayout(byte[] bytes)
    {
        using var font = Font(bytes); using var style = new StyleBoxEmpty(); using var icon = Texture(16, 10);
        var button = new Button("A") { Size = new(100, 40), Icon = icon }; Configure(button, font, style);
        using var tree = new SceneTree(button);
        Check(button.GetMinimumSize() == new Vector2(31, 23), "Horizontal text/icon minimum combines ceiled glyph width, separation and icon size.");
        button.ClipText = true; Check(button.GetMinimumSize() == new Vector2(20, 23), "ClipText removes only the text width from the minimum.");
        button.IconAlignment = HorizontalAlignment.Center; Check(button.GetMinimumSize() == new Vector2(16, 23), "Centered icons overlap text width rather than adding separation.");
        button.ClipText = false; button.IconAlignment = HorizontalAlignment.Left; button.VerticalIconAlignment = VerticalAlignment.Top;
        Check(button.GetMinimumSize() == new Vector2(31, 56), "Top icon placement preserves the source additional font-height minimum.");
        button.ExpandIcon = true; Check(button.GetMinimumSize() == new Vector2(11, 46), "Expanded icons omit intrinsic sizing while preserving text placement semantics.");
        button.VerticalIconAlignment = VerticalAlignment.Center; button.ClipText = true;
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); Record(button, vertices, batches);
        var image = batches.Single(b => ReferenceEquals(b.Texture, icon)); var first = vertices[image.First].Position;
        Check(first == Vector2.Zero && vertices[image.First + 2].Position == new Vector2(64, 40), "Expanded icon preserves aspect ratio inside available content and floors its position.");
        button.LayoutDirection = LayoutDirection.RTL; Record(button, vertices, batches);
        image = batches.Single(b => ReferenceEquals(b.Texture, icon));
        Check(vertices[image.First].Position.X == 36, "RTL swaps left icon placement to the right.");
        button.ExpandIcon = false; button.AddThemeConstantOverride("icon_max_width", 8); button.LayoutDirection = LayoutDirection.LTR; Record(button, vertices, batches);
        image = batches.Single(b => ReferenceEquals(b.Texture, icon));
        Check(vertices[image.First + 2].Position - vertices[image.First].Position == new Vector2(8, 5), "Icon max width scales height proportionally.");
        using var roomy = new StyleBoxEmpty { ContentMarginLeft = 11, ContentMarginTop = 7, ContentMarginRight = 13, ContentMarginBottom = 9 };
        button.AddThemeStyleBoxOverride("pressed", roomy); button.ToggleMode = true; button.ButtonPressed = true;
        Check(button.GetMinimumSize().X == 36, "The current pressed style contributes its own content margins.");
        button.ButtonPressed = false; button.AddThemeConstantOverride("align_to_largest_stylebox", 1);
        Check(button.GetMinimumSize().X == 36 && button.GetMinimumSize().Y == 39, "Largest-style alignment uses maximum per-side margins across states.");
    }
    private static void VerifyTextAndClipping(byte[] bytes)
    {
        using var font = Font(bytes); using var style = new StyleBoxEmpty();
        var button = new Button("AV") { ClipText = true, Alignment = HorizontalAlignment.Left, Size = new(10, 40) }; Configure(button, font, style);
        using var tree = new SceneTree(button); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        Record(button, vertices, batches); Check(vertices.Count == 0, "A partial leading glyph advance is excluded rather than raster-clipped.");
        button.Size = new(11, 40); Record(button, vertices, batches); Check(vertices.Count == 6, "The first complete fractional advance fits while the second is excluded.");
        button.Alignment = HorizontalAlignment.Right; Record(button, vertices, batches); Check(vertices.Count == 6, "Right alignment clips the complete leading glyph and retains the trailing glyph.");
        button.Size = new(60, 80); button.Text = "A\nV"; Record(button, vertices, batches); Check(vertices.Count == 12 && button.GetMinimumSize().Y == 46, "Mandatory breaks retain separate shaped text lines.");
        button.Text = "abcdef"; button.AutowrapMode = TextAutowrapMode.Arbitrary; button.Size = new(20, 160); Record(button, vertices, batches);
        Check(button.GetMinimumSize().Y > 46, "Wrap width from actual drawing determines multiline minimum height.");
        button.AutowrapMode = TextAutowrapMode.Off; button.Size = new(100, 40); button.Text = "A";
        button.AddThemeConstantOverride("outline_size", 2); button.AddThemeColorOverride("font_outline_color", Colors.Red);
        button.AddThemeColorOverride("font_color", Colors.Blue); Record(button, vertices, batches);
        Check(vertices.Count == 12 && vertices[0].Color == Colors.Red && vertices[^1].Color == Colors.Blue, "The outline precedes the main text with distinct colors.");
        button.AddThemeColorOverride("font_disabled_color", Colors.Green); button.Disabled = true; Record(button, vertices, batches);
        Check(vertices[^1].Color == Colors.Green, "Disabled state selects its font color.");
        button.Disabled = false; button.AddThemeConstantOverride("outline_size", 0); button.Alignment = HorizontalAlignment.Fill;
        button.Text = "A A\nA A"; button.Size = new(100, 80); Record(button, vertices, batches);
        Check(vertices.Count == 24 && vertices[6].Position.X > 70 && vertices[18].Position.X < 30,
            "Fill inherits paragraph defaults: expand earlier lines and retain the natural width of the final line.");
        Check(Task.Run(() => Capture(() => button.Text = "off-thread")).Result is InvalidOperationException, "Attached public mutation enforces the owner thread.");
    }
    private static void VerifyIndicators(byte[] bytes)
    {
        using var font = Font(bytes); using var style = new StyleBoxEmpty(); using var checkedIcon = Texture(8, 5); using var uncheckedIcon = Texture(12, 7);
        using var radio = Texture(14, 9); using var disabled = Texture(20, 11);
        var root = new Node(); var box = new CheckBox { Size = new(50, 30) }; var toggle = new CheckButton { Size = new(50, 30) };
        Configure(box, font, style); Configure(toggle, font, style);
        foreach (var control in new Button[] { box, toggle })
        {
            control.AddThemeIconOverride("checked", checkedIcon); control.AddThemeIconOverride("unchecked", uncheckedIcon);
            control.AddThemeIconOverride("checked_disabled", disabled); control.AddThemeIconOverride("unchecked_disabled", disabled);
            foreach (var key in new[] { "radio_checked", "radio_unchecked", "radio_checked_disabled", "radio_unchecked_disabled" }) control.AddThemeIconOverride(key, radio);
            foreach (var key in new[] { "checked_mirrored", "unchecked_mirrored", "checked_disabled_mirrored", "unchecked_disabled_mirrored" }) control.AddThemeIconOverride(key, radio);
            root.AddChild(control);
        }
        using var tree = new SceneTree(root); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        Check(box.GetMinimumSize() == new Vector2(20, 11), "Checkbox minimum reserves maximum dimensions across all eight icon states.");
        Check(toggle.GetMinimumSize() == new Vector2(12, 7), "CheckButton minimum uses only its active enabled/unmirrored on/off pair.");
        Record(box, vertices, batches); Check(vertices[0].Position == new Vector2(0, 9), "Checkbox vertical centering truncates using the all-state maximum size.");
        Record(toggle, vertices, batches); Check(vertices[0].Position == new Vector2(38, 11.5f), "CheckButton uses trailing placement and fractional vertical centering.");
        using var group = new ButtonGroup(); box.ButtonGroup = group; Record(box, vertices, batches);
        Check(ReferenceEquals(batches[0].Texture, radio), "Button groups select radio imagery immediately.");
        box.LayoutDirection = LayoutDirection.RTL; Record(box, vertices, batches);
        Check(vertices[0].Position.X == 30, "RTL checkbox reserves its maximum icon width at the trailing edge.");
        toggle.LayoutDirection = LayoutDirection.RTL; Record(toggle, vertices, batches);
        Check(ReferenceEquals(batches[0].Texture, radio) && vertices[0].Position.X == 0, "RTL switch uses its mirrored icon pair at the opposite edge.");
    }
    private static void VerifyResourceUpdates(byte[] bytes)
    {
        using var font = Font(bytes); using var style = new StyleBoxEmpty(); using var icon = Texture(8, 4);
        icon.Changed += _ => throw new ApplicationException("earlier observer");
        var root = new Node(); var button = new Button { Icon = icon }; Configure(button, font, style); root.AddChild(button); using var tree = new SceneTree(root);
        Check(button.GetMinimumSize().X == 8, "Initial icon contributes its width.");
        var draws = 0; button.Draw += _ => draws++; button.PrepareCanvas(); var initialDraws = draws;
        using var replacement = Image.CreateEmpty(20, 10, false, Image.Format.Rgba8); replacement.Fill(Colors.White);
        Reject<ApplicationException>(() => icon.SetImage(replacement));
        Check(button.GetMinimumSize().X == 20, "A query observes icon changes hidden by an earlier throwing resource observer.");
        tree.ProcessFrame(0); button.PrepareCanvas();
        Check(button.GetCombinedMinimumSize().X == 20 && draws > initialDraws, "A query must not consume pending minimum invalidation and retained redraw before the owner frame.");
        using var background = Texture(9, 6); button.Icon = background;
        using var larger = Image.CreateEmpty(21, 8, false, Image.Format.Rgba8); larger.Fill(Colors.White);
        Task.Run(() => background.SetImage(larger)).GetAwaiter().GetResult(); tree.ProcessFrame(0);
        Check(button.GetMinimumSize().X == 21, "Background icon changes marshal invalidation onto the owner thread.");
        root.RemoveChild(button); Task.Run(() => background.SetImage(larger)).GetAwaiter().GetResult(); root.AddChild(button); tree.ProcessFrame(0);
        button.Icon = null; background.Dispose(); Check(button.GetMinimumSize() == Vector2.Zero, "Replacing an icon detaches subscriptions and never owns the old resource.");
        using var atlasPixels = Texture(8, 4); using var inner = new AtlasTexture { Atlas = atlasPixels, Region = new(0, 0, 4, 4) };
        inner.Changed += _ => throw new ApplicationException("earlier inner atlas observer");
        using var outer = new AtlasTexture { Atlas = inner, Region = new(0, 0, 2, 2) }; button.Icon = outer;
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); Record(button, vertices, batches);
        Check(vertices[0].UV.X == 0, "Initial nested atlas starts in the left region.");
        Reject<ApplicationException>(() => inner.Region = new(4, 0, 4, 4)); button.GetMinimumSize(); tree.ProcessFrame(0);
        button.PrepareCanvas(); vertices.Clear(); batches.Clear(); button.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices[0].UV.X == .5f, "Exact transitive polling refreshes nested atlas coordinates even when the outer revision does not advance.");
    }
    private static void VerifyWarmFrames(byte[] bytes)
    {
        using var font = Font(bytes); using var style = new StyleBoxEmpty(); using var icon = Texture(8, 8);
        var button = new Button("warm A") { Icon = icon, Size = new(120, 40), ToggleMode = true }; Configure(button, font, style); using var tree = new SceneTree(button);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var i = 0; i < 64; i++) { button.Text = (i & 1) == 0 ? "warm A" : "warm V"; button.ButtonPressed = (i & 1) == 0; tree.ProcessFrame(0); Record(button, vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { button.Text = (i & 1) == 0 ? "warm A" : "warm V"; button.ButtonPressed = (i & 1) == 0; tree.ProcessFrame(0); Record(button, vertices, batches); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Prepared active button frame/layout/recording must allocate no managed storage; actual={allocated}.");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) tree.ProcessFrame(0);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Idle button resource polling reuses prepared state.");
    }
    private static void VerifyResidency(byte[] bytes)
    {
        using var font = Font(bytes); using var pixels = Texture(8, 8);
        using var inner = new AtlasTexture { Atlas = pixels, Region = new(0, 0, 4, 4) };
        using var outer = new AtlasTexture { Atlas = inner, Region = new(0, 0, 2, 2) };
        using var style = new StyleBoxTexture { Texture = outer }; style.SetContentMarginAll(0);
        var a = new Button { Name = "A", Icon = outer }; var b = new Button { Name = "B", Icon = outer };
        Configure(a, font, style); Configure(b, font, style); a.GetMinimumSize();
        Check(!pixels.RetainRendererCache && !inner.RetainRendererCache && !outer.RetainRendererCache,
            "Detached configuration and measurement never acquire renderer residency.");
        var root = new Node(); root.AddChild(a); root.AddChild(b); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Check(pixels.RetainRendererCache && inner.RetainRendererCache && outer.RetainRendererCache,
            "Attached controls retain exact nested icon and textured-style dependencies.");
        root.RemoveChild(a); Check(pixels.RetainRendererCache, "Removing one shared owner preserves the other control's lease.");
        root.RemoveChild(b); Check(!pixels.RetainRendererCache && !inner.RetainRendererCache && !outer.RetainRendererCache,
            "Aliases across icons and styles release exactly once when the last owner exits.");
        root.AddChild(a); tree.ProcessFrame(0); a.Dispose();
        Check(!pixels.RetainRendererCache && !outer.RetainRendererCache, "Disposal releases residency without disposing caller-owned textures.");
        b.Dispose(); Check(!pixels.IsDisposed && !inner.IsDisposed && !outer.IsDisposed, "Residency is distinct from resource ownership.");
    }
    private static void Record(CanvasItem item, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    {
        item.InvalidateCanvas(); item.PrepareCanvas(); vertices.Clear(); batches.Clear(); item.AppendCanvas(vertices, batches, Transform.Identity);
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception e) { return e; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
