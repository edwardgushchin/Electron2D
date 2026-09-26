using System.Text.Json;
using Electron2D;

internal static class StyleBoxFlatTests
{
    internal static void Run()
    {
        VerifyDefaultsEventsAndMargins();
        VerifyNotificationFailure();
        VerifyBoundsAndGuards();
        VerifyGeometryOracle();
        VerifyCopiesAndOverrides();
        VerifyConcurrencyAndAllocations();
        Console.WriteLine("Flat style defaults, events, bounds, independent geometry oracle, copies and allocation checks passed.");
    }

    private static void VerifyNotificationFailure()
    {
        using var style = new StyleBoxFlat(); var lists = 0;
        Action<Resource> changedFailure = _ => throw new ApplicationException("Expected change failure.");
        Action<ElectronObject> listFailure = _ => throw new InvalidOperationException("Expected list failure.");
        style.Changed += changedFailure; style.PropertyListChanged += _ => lists++;
        Reject<ApplicationException>(() => style.AntiAliasing = false);
        Check(!style.AntiAliasing && lists == 1, "A failing Changed observer must not suppress the live style's property-list notification.");
        style.PropertyListChanged += listFailure;
        var error = Capture(() => style.AntiAliasing = true);
        Check(style.AntiAliasing && lists == 2 && error is AggregateException aggregate && aggregate.InnerExceptions.Count == 2,
            "Both committed notification failures are reported after both phases run.");
    }

    private static void VerifyDefaultsEventsAndMargins()
    {
        using var style = new StyleBoxFlat(); var changed = 0; var lists = 0; var order = new List<string>();
        style.Changed += _ => { changed++; order.Add("changed"); }; style.PropertyListChanged += _ => { lists++; order.Add("list"); };
        Check(style.BGColor == new Color(.6f, .6f, .6f) && style.BorderColor == new Color(.8f, .8f, .8f) && style.ShadowColor == new Color(0, 0, 0, .6f) &&
            style.DrawCenter && !style.BorderBlend && style.AntiAliasing && style.AntiAliasingSize == 1 && style.CornerDetail == 8 &&
            style.ShadowSize == 0 && style.ShadowOffset == Vector2.Zero && style.Skew == Vector2.Zero && style.GetMinimumSize() == Vector2.Zero,
            "Flat style defaults cover color, fill, antialiasing, shadow, detail and empty margins.");
        Check((int)Corner.TopLeft == 0 && (int)Corner.TopRight == 1 && (int)Corner.BottomRight == 2 && (int)Corner.BottomLeft == 3, "Corner identities follow clockwise order from the top left.");
        for (var index = 0; index < 4; index++)
        {
            Check(style.GetBorderWidth((Side)index) == 0 && style.GetExpandMargin((Side)index) == 0 && style.GetCornerRadius((Corner)index) == 0, "Per-side and per-corner values default to zero.");
            style.SetBorderWidth((Side)index, 0); style.SetExpandMargin((Side)index, 0); style.SetCornerRadius((Corner)index, 0);
        }
        style.BGColor = style.BGColor; style.BorderColor = style.BorderColor; style.ShadowColor = style.ShadowColor;
        style.DrawCenter = true; style.BorderBlend = false; style.Skew = Vector2.Zero; style.AntiAliasing = true;
        style.CornerDetail = 8; style.ShadowSize = 0; style.ShadowOffset = Vector2.Zero; style.AntiAliasingSize = 1;
        style.SetBorderWidthAll(0); style.SetCornerRadiusAll(0); style.SetExpandMarginAll(0);
        Check(changed == 26 && lists == 1, "Every flat setter emits once for equal writes; grouped edits coalesce and only antialiasing changes the property list.");
        order.Clear(); style.AntiAliasing = true;
        Check(order.SequenceEqual(new[] { "changed", "list" }), "Equal antialiasing writes notify Changed before PropertyListChanged.");
        style.CornerDetail = int.MinValue; Check(style.CornerDetail == 1, "Corner detail clamps to one.");
        style.CornerDetail = int.MaxValue; Check(style.CornerDetail == 20, "Corner detail clamps to twenty.");
        style.AntiAliasingSize = -100; Check(style.AntiAliasingSize == .01f, "Antialiasing size clamps to one hundredth.");
        style.AntiAliasingSize = 100; Check(style.AntiAliasingSize == 10, "Antialiasing size clamps to ten.");
        style.BorderWidthLeft = 1; style.BorderWidthTop = 2; style.BorderWidthRight = 3; style.BorderWidthBottom = 4;
        Check(style.GetBorderWidthMin() == 1 && style.GetMinimumSize() == new Vector2(4, 6) && style.GetOffset() == new Vector2(1, 2), "Intrinsic margins equal border widths and combine opposing sides.");
        style.ContentMarginLeft = 8; Check(style.GetMinimumSize() == new Vector2(11, 6), "Explicit content margins replace intrinsic border margins.");
        style.ContentMarginLeft = -9; style.BorderWidthLeft = -20; style.CornerRadiusBottomRight = -13; style.ShadowSize = -9;
        Check(style.GetBorderWidth(Side.Left) == -20 && style.GetBorderWidthMin() == -20 && style.GetCornerRadius(Corner.BottomRight) == -13 && style.ShadowSize == -9 &&
            style.GetMinimumSize() == new Vector2(0, 6), "Signed border, radius and shadow integers are retained; the base zero minimum hook bounds negative sums.");
        style.CornerRadiusTopLeft = 3; style.CornerRadiusTopRight = 4; style.CornerRadiusBottomLeft = 5;
        Check(style.GetCornerRadius(Corner.TopLeft) == 3 && style.GetCornerRadius(Corner.TopRight) == 4 && style.GetCornerRadius(Corner.BottomLeft) == 5, "Named corner properties project the typed indexed API.");
    }

    private static void VerifyBoundsAndGuards()
    {
        using var style = new StyleBoxFlat
        {
            ExpandMarginLeft = 1,
            ExpandMarginTop = 2,
            ExpandMarginRight = 3,
            ExpandMarginBottom = 4,
            ShadowSize = 5,
            ShadowOffset = new(7, -8),
            Skew = new(.7f, -.3f),
            AntiAliasingSize = 10
        };
        var rect = new Rect2(10, 20, 30, 40);
        Check(style.GetDrawRect(rect) == new Rect2(9, 5, 46, 59), "Draw bounds merge the expanded box and offset grown shadow, independently of skew and antialiasing.");
        Check(style.GetMinimumSize() == Vector2.Zero, "Expansion and shadow do not contribute to content minimums.");
        style.ShadowSize = -1; Check(style.GetDrawRect(rect) == new Rect2(9, 18, 34, 46), "Nonpositive shadow size omits shadow bounds.");
        var changes = 0; var lists = 0; style.Changed += _ => changes++; style.PropertyListChanged += _ => lists++;
        Reject<ArgumentOutOfRangeException>(() => style.SetBorderWidth((Side)4, 2));
        Reject<ArgumentOutOfRangeException>(() => style.GetBorderWidth((Side)(-1)));
        Reject<ArgumentOutOfRangeException>(() => style.SetCornerRadius((Corner)4, 2));
        Reject<ArgumentOutOfRangeException>(() => style.GetCornerRadius((Corner)(-1)));
        Reject<ArgumentOutOfRangeException>(() => style.SetExpandMargin((Side)4, 2));
        Reject<ArgumentOutOfRangeException>(() => style.GetExpandMargin((Side)(-1)));
        Reject<ArgumentException>(() => style.SetExpandMarginAll(float.NaN));
        Reject<ArgumentException>(() => style.ExpandMarginLeft = float.PositiveInfinity);
        Reject<ArgumentException>(() => style.AntiAliasingSize = float.NaN);
        Reject<ArgumentException>(() => style.BGColor = new(float.NaN, 0, 0));
        Reject<ArgumentException>(() => style.BorderColor = new(0, float.NaN, 0));
        Reject<ArgumentException>(() => style.ShadowColor = new(0, 0, float.NaN));
        Reject<ArgumentException>(() => style.ShadowOffset = new(float.PositiveInfinity, 0));
        Reject<ArgumentException>(() => style.Skew = new(0, float.NaN));
        Check(changes == 0 && lists == 0 && style.ExpandMarginLeft == 1 && style.AntiAliasingSize == 10, "Invalid edits preserve both configuration and notification counts.");
        Action<Resource> failure = _ => throw new ApplicationException("expected flat style observer failure"); style.Changed += failure;
        Reject<ApplicationException>(() => style.BorderBlend = true); style.Changed -= failure;
        Check(style.BorderBlend, "Observer failure occurs after style state commits.");
    }

    private static void VerifyGeometryOracle()
    {
        using var fixtureStream = typeof(StyleBoxFlatTests).Assembly.GetManifestResourceStream("TestFixtures.StyleBoxFlatGeometry.json") ?? throw new InvalidOperationException("Missing flat style geometry fixture.");
        using var fixture = JsonDocument.Parse(fixtureStream);
        Check(fixture.RootElement.GetProperty("sourceCommit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079" &&
            fixture.RootElement.GetProperty("sourceSHA256").GetString() == "69d56a1feb8705043380f30073af12571c4b9b084838208c6748488133c51173", "Geometry fixtures identify the immutable independently executed source.");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var count = 0;
        foreach (var scenario in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var style = new StyleBoxFlat(); Configure(style, scenario);
            var name = scenario.GetProperty("name").GetString(); var rect = Rectangle(scenario.GetProperty("rect"));
            using var painter = new Painter { Paint = node => node.DrawStyleBox(style, rect) };
            painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
            var expectedVertices = scenario.GetProperty("vertices"); var indices = scenario.GetProperty("indices");
            Check(vertices.Count == indices.GetArrayLength(), $"Flat oracle {name}: expanded triangle count {vertices.Count}/{indices.GetArrayLength()}.");
            for (var index = 0; index < vertices.Count; index++)
            {
                var expected = expectedVertices[indices[index].GetInt32()]; var actual = vertices[index];
                Near(actual.Position.X, expected[0].GetSingle(), name, index, "x"); Near(actual.Position.Y, expected[1].GetSingle(), name, index, "y");
                Near(actual.Color.R, expected[2].GetSingle(), name, index, "r"); Near(actual.Color.G, expected[3].GetSingle(), name, index, "g");
                Near(actual.Color.B, expected[4].GetSingle(), name, index, "b"); Near(actual.Color.A, expected[5].GetSingle(), name, index, "a");
                Near(actual.UV.X, expected[6].GetSingle(), name, index, "u"); Near(actual.UV.Y, expected[7].GetSingle(), name, index, "v");
            }
            Check(style.GetDrawRect(rect) == Rectangle(scenario.GetProperty("drawRect")), $"Flat oracle {name}: independently computed draw bounds.");
            count++;
        }
        Check(count == 15, "The geometry oracle covers sharp, rounded, unequal, oversized, blended, hollow, AA, shadow, skew, signed and degenerate cases.");
    }

    private static void VerifyCopiesAndOverrides()
    {
        using var style = new StyleBoxFlat
        {
            BGColor = Colors.Red,
            BorderColor = Colors.Blue,
            ShadowColor = Colors.Green,
            BorderWidthLeft = 2,
            BorderWidthTop = 3,
            BorderWidthRight = 4,
            BorderWidthBottom = 5,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 9,
            ExpandMarginLeft = 1,
            ExpandMarginTop = -2,
            ExpandMarginRight = 3,
            ExpandMarginBottom = -4,
            AntiAliasing = false,
            AntiAliasingSize = 2,
            CornerDetail = 3,
            ShadowSize = 4,
            ShadowOffset = new(5, -6),
            Skew = new(.2f, .3f),
            DrawCenter = false,
            BorderBlend = true,
            ContentMarginRight = 11
        };
        using var copy = (StyleBoxFlat)style.Duplicate(true);
        Check(copy.BGColor == Colors.Red && copy.BorderColor == Colors.Blue && copy.ShadowColor == Colors.Green && copy.BorderWidthLeft == 2 && copy.BorderWidthBottom == 5 &&
            copy.CornerRadiusTopLeft == 6 && copy.CornerRadiusBottomLeft == 9 && copy.ExpandMarginTop == -2 && copy.ExpandMarginRight == 3 &&
            !copy.AntiAliasing && copy.AntiAliasingSize == 2 && copy.CornerDetail == 3 && copy.ShadowSize == 4 && copy.ShadowOffset == new Vector2(5, -6) &&
            copy.Skew == new Vector2(.2f, .3f) && !copy.DrawCenter && copy.BorderBlend && copy.ContentMarginRight == 11, "Exact flat duplication preserves all decoration families and inherited content margins.");
        using var target = new StyleBoxFlat(); var changes = 0; target.Changed += _ => changes++; target.CopyFromResource(style);
        Check(changes == 1 && target.CornerRadiusBottomRight == 8 && target.BorderWidthRight == 4 && target.ExpandMarginBottom == -4 && target.ContentMarginRight == 11, "CopyFromResource batches the complete flat state into one Changed event.");
        var detail = target.GetPropertyList().Single(property => property.Name == nameof(StyleBoxFlat.CornerDetail)); target.RevertProperty(detail);
        Check(target.CornerDetail == 8 && !target.PropertyCanRevert(detail), "Stored flat defaults agree with construction.");
        style.ResourceLocalToScene = true; using var consumer = new StyleConsumer { Style = style }; using var scene = new PackedScene(); scene.Pack(consumer);
        using var instance = (StyleConsumer)scene.Instantiate();
        Check(instance.Style is { } local && !ReferenceEquals(local, style) && local.BGColor == Colors.Red && local.CornerRadiusBottomLeft == 9 && local.ContentMarginRight == 11,
            "A packed scene owns an independent exact flat resource with inherited state.");
        using var derived = new MinimumStyle(); derived.SetBorderWidthAll(4);
        Check(derived.GetMinimumSize() == new Vector2(50, 8), "Derived flat styles retain the typed additional-minimum hook.");
        Reject<NotSupportedException>(() => derived.Duplicate());
        copy.Dispose(); Reject<ObjectDisposedException>(() => copy.BGColor = Colors.White); Reject<ObjectDisposedException>(() => copy.GetBorderWidthMin());
        Reject<ObjectDisposedException>(() => copy.GetCornerRadius(Corner.TopLeft)); Reject<ObjectDisposedException>(() => copy.GetDrawRect(default));
    }

    private static void VerifyConcurrencyAndAllocations()
    {
        using var coherent = new StyleBoxFlat(); coherent.SetBorderWidthAll(1);
        Action<Resource> outsideLock = _ => Check(Task.Run(() => coherent.GetBorderWidth(Side.Left)).Wait(TimeSpan.FromSeconds(2)), "Flat Changed callbacks execute outside the resource lock.");
        coherent.Changed += outsideLock; coherent.SetBorderWidthAll(2); coherent.Changed -= outsideLock;
        Parallel.For(0, 256, index => { coherent.SetBorderWidthAll(index % 2 + 1); var size = coherent.GetMinimumSize(); Check(size.X == size.Y && (size.X == 2 || size.X == 4), "Concurrent grouped border writes preserve coherent minimum snapshots."); });
        using var style = new StyleBoxFlat { CornerDetail = 3, ShadowSize = 3, ShadowOffset = new(1, 2), Skew = new(.1f, -.2f) };
        style.SetCornerRadiusAll(4); style.SetBorderWidthAll(2);
        using var painter = new Painter { Paint = node => node.DrawStyleBox(style, new(0, 0, 30, 20)) };
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Frame(int pass) { style.ShadowSize = pass % 2 + 3; painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity); }
        for (var pass = 0; pass < 64; pass++) Frame(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Frame(pass);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed flat shadow/AA/ring mutation, recording and replay allocate zero managed bytes.");
    }

    private static void Configure(StyleBoxFlat style, JsonElement scenario)
    {
        foreach (var property in scenario.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "border_width": for (var i = 0; i < 4; i++) style.SetBorderWidth((Side)i, value[i].GetInt32()); break;
                case "corner_radius": for (var i = 0; i < 4; i++) style.SetCornerRadius((Corner)i, value[i].GetInt32()); break;
                case "expand_margin": for (var i = 0; i < 4; i++) style.SetExpandMargin((Side)i, value[i].GetSingle()); break;
                case "bg_color": style.BGColor = Tint(value); break;
                case "border_color": style.BorderColor = Tint(value); break;
                case "shadow_color": style.ShadowColor = Tint(value); break;
                case "skew": style.Skew = Point(value); break;
                case "shadow_offset": style.ShadowOffset = Point(value); break;
                case "draw_center": style.DrawCenter = value.GetBoolean(); break;
                case "blend_border": style.BorderBlend = value.GetBoolean(); break;
                case "anti_aliased": style.AntiAliasing = value.GetBoolean(); break;
                case "corner_detail": style.CornerDetail = value.GetInt32(); break;
                case "shadow_size": style.ShadowSize = value.GetInt32(); break;
                case "aa_size": style.AntiAliasingSize = value.GetSingle(); break;
            }
        }
    }
    private static Vector2 Point(JsonElement value) => new(value[0].GetSingle(), value[1].GetSingle());
    private static Rect2 Rectangle(JsonElement value) => new(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle(), value[3].GetSingle());
    private static Color Tint(JsonElement value) => new(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle(), value[3].GetSingle());
    private static void Near(float actual, float expected, string? scenario, int index, string component)
    {
        if (!float.IsFinite(actual) || MathF.Abs(actual - expected) > .00005f) throw new InvalidOperationException($"Flat oracle {scenario}, vertex {index}, {component}: {actual}/{expected}.");
    }
    private sealed class MinimumStyle : StyleBoxFlat { protected override Vector2 OnGetMinimumSize() => new(50, 1); }
    private sealed class Painter : Entity { internal Action<Painter>? Paint; protected override void OnDraw() => Paint?.Invoke(this); }
    private sealed class StyleConsumer : Node
    {
        private static readonly PropertyDescriptor<StyleConsumer, StyleBoxFlat?> StyleProperty = new(nameof(Style), node => node.Style, (node, value) => node.Style = value, _ => null, stored: true);
        internal StyleBoxFlat? Style { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(StyleProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateConsumer;
        private static Node CreateConsumer() => new StyleConsumer();
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
