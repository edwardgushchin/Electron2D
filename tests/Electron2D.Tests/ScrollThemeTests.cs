using System.Security.Cryptography;
using Electron2D;

internal static class ScrollThemeTests
{
    internal static void Run()
    {
        var theme = ThemeDB.GetDefaultTheme();
        VerifyScrollBars(theme);
        VerifyContainer(theme);
        VerifyHints(theme);
        Console.WriteLine("Scroll theme verifies exact flat styles, shared empty state icons, absent zero-default constants, focus geometry and pinned SVG gradient pixels.");
    }

    private static void VerifyScrollBars(Theme theme)
    {
        var h = (StyleBoxFlat)theme.GetStyleBox("scroll", "HScrollBar")!;
        var v = (StyleBoxFlat)theme.GetStyleBox("scroll", "VScrollBar")!;
        Check(!ReferenceEquals(h, v) && h.BGColor == new Color(.1f, .1f, .1f, .6f) && v.BGColor == h.BGColor,
            "Scrollbar axes own separate, identically colored track styles.");
        CheckMargins(h, 0, 4, 0, 4); CheckMargins(v, 4, 0, 4, 0);
        Check(h.GetMinimumSize() == new Vector2(0, 8) && v.GetMinimumSize() == new Vector2(8, 0), "Track minima preserve the orientation-specific margin sums.");
        CheckFlat(h, 10, 6); CheckFlat(v, 10, 6);
        foreach (var (key, color) in new[] { ("grabber", new Color(1, 1, 1, .4f)), ("grabber_highlight", new Color(1, 1, 1, .75f)), ("grabber_pressed", new Color(.75f, .75f, .75f, .75f)) })
        {
            var style = (StyleBoxFlat)theme.GetStyleBox(key, "HScrollBar")!;
            Check(ReferenceEquals(style, theme.GetStyleBox(key, "VScrollBar")) && style.BGColor == color && style.DrawCenter,
                "The three grabber states retain shared H/V identities and exact colors, including pressed alpha.");
            CheckMargins(style, 4, 4, 4, 4); CheckFlat(style, 10, 6);
            Check(style.GetMinimumSize() == new Vector2(8, 8), "Each grabber contributes an eight-pixel intrinsic minimum.");
        }
        var focus = theme.GetStyleBox("focus", "Button");
        Check(ReferenceEquals(theme.GetStyleBox("scroll_focus", "HScrollBar"), focus) && ReferenceEquals(theme.GetStyleBox("scroll_focus", "VScrollBar"), focus),
            "Both scrollbar focus states reuse the existing shared control focus style.");
        var empty = theme.GetIcon("increment", "HScrollBar");
        Check(empty is ImageTexture && empty.GetSize() == Vector2.Zero && empty.GetImage() is null && !ReferenceEquals(empty, ThemeDB.FallbackIcon),
            "The built-in arrows are one real uninitialized zero-size image texture, not null or a replacement decoration.");
        foreach (var type in new[] { "HScrollBar", "VScrollBar" })
        {
            foreach (var key in new[] { "increment", "increment_highlight", "increment_pressed", "decrement", "decrement_highlight", "decrement_pressed" })
                Check(theme.HasIcon(key, type) && ReferenceEquals(theme.GetIcon(key, type), empty), "All twelve arrow slots preserve one explicit live empty texture identity.");
            foreach (var key in type == "HScrollBar" ? new[] { "padding_top", "padding_bottom" } : new[] { "padding_left", "padding_right" })
                Check(!theme.HasConstant(key, type) && theme.GetConstant(key, type) == 0, "Unregistered scrollbar padding uses zero fallback without fabricating a stored constant.");
        }
        Check(theme.GetStyleBoxList("ScrollBar").Length == 0 && theme.GetIconList("ScrollBar").Length == 0, "The abstract ScrollBar type has no fabricated default entries.");
    }

    private static void VerifyContainer(Theme theme)
    {
        var panel = theme.GetStyleBox("panel", "ScrollContainer");
        Check(panel is StyleBoxEmpty && panel.GetMinimumSize() == Vector2.Zero && panel.ContentMarginLeft == -1 && panel.ContentMarginTop == -1 && panel.ContentMarginRight == -1 && panel.ContentMarginBottom == -1,
            "The container panel retains untouched StyleBoxEmpty margin defaults.");
        var focus = (StyleBoxFlat)theme.GetStyleBox("focus", "ScrollContainer")!;
        Check(!ReferenceEquals(focus, theme.GetStyleBox("focus", "Button")) && focus.BGColor == new Color(1, 1, 1, .75f) && focus.BorderColor == focus.BGColor && !focus.DrawCenter,
            "The container owns a separate hollow focus style with the explicit translucent border color.");
        CheckMargins(focus, 4, 4, 4, 4); CheckFlat(focus, 3, 5);
        foreach (var side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom })
            Check(focus.GetBorderWidth(side) == 2 && focus.GetExpandMargin(side) == 4, "The container focus expands four pixels and uses a two-pixel border on every side.");
        Check(focus.GetDrawRect(new(2, 3, 20, 10)) == new Rect2(-2, -1, 28, 18), "Focus bounds include the exact expansion.");
        foreach (var key in new[] { "scrollbar_h_separation", "scrollbar_v_separation" })
            Check(!theme.HasConstant(key, "ScrollContainer") && theme.GetConstant(key, "ScrollContainer") == 0, "Container separation remains an absent item resolving to zero.");
        Check(theme.GetColor("scroll_hint_horizontal_color", "ScrollContainer") == Colors.Black && theme.GetColor("scroll_hint_vertical_color", "ScrollContainer") == Colors.Black,
            "Both scroll hints modulate with opaque black.");
    }

    private static void VerifyHints(Theme theme)
    {
        VerifyHint(theme, "scroll_hint_horizontal", new(24, 32), "FBE34532EA6643E2FE839833C3948F428E57E21ED56EE24FC528E68DA68A5928", true);
        VerifyHint(theme, "scroll_hint_vertical", new(32, 24), "F88F0908AC86FB611B8FCAFF552A1EC0ECF8CB118423D0F76B304658C23EA158", false);
    }
    private static void VerifyHint(Theme theme, string name, Vector2i size, string hash, bool horizontal)
    {
        using var stream = typeof(ThemeDB).Assembly.GetManifestResourceStream("Electron2D.Icons." + name + ".svg")!;
        Check(Convert.ToHexString(SHA256.HashData(stream)) == hash, "Embedded scroll-hint bytes match the pinned source asset.");
        var texture = theme.GetIcon(name, "ScrollContainer")!; using var image = texture.GetImage();
        Check(texture.GetSize() == (Vector2)size && image is not null && image.Size == size, "Decoded scroll-hint dimensions match the SVG view.");
        for (var y = 0; y < size.Y; y++) for (var x = 0; x < size.X; x++)
            {
                var color = image!.GetPixel(x, y); var axis = horizontal ? x : y;
                var alpha = .3f * (1 - (axis + .5f) / 24);
                Check(MathF.Abs(color.A - alpha) <= 2f / 255 && MathF.Abs(color.R - 1) * color.A <= 1f / 255 &&
                    MathF.Abs(color.G - 1) * color.A <= 1f / 255 && MathF.Abs(color.B - 1) * color.A <= 1f / 255,
                    "The decoded hint preserves its white linear alpha gradient along the correct axis.");
            }
    }
    private static void CheckMargins(StyleBox style, float left, float top, float right, float bottom) => Check(
        style.ContentMarginLeft == left && style.ContentMarginTop == top && style.ContentMarginRight == right && style.ContentMarginBottom == bottom,
        "Exact raw style margins.");
    private static void CheckFlat(StyleBoxFlat style, int radius, int detail) => Check(style.AntiAliasing && style.CornerDetail == detail &&
        style.CornerRadiusTopLeft == radius && style.CornerRadiusTopRight == radius && style.CornerRadiusBottomLeft == radius && style.CornerRadiusBottomRight == radius,
        "Exact round-corner and anti-alias policy.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
