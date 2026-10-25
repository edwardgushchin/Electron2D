using Electron2D;

internal static class ItemListLayoutRenderingTests
{
    internal static void Run(string backend)
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var maxFPS = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 0;
            using var r = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [255, 0, 0, 255]); using var red = ImageTexture.CreateFromImage(r);
            using var g = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [0, 255, 0, 255]); using var green = ImageTexture.CreateFromImage(g);
            using var b = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [0, 0, 255, 255]); using var blue = ImageTexture.CreateFromImage(b);
            using var strip = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [0, 255, 0, 255, 0, 0, 255, 255]); using var hint = ImageTexture.CreateFromImage(strip);
            using var empty = new StyleBoxEmpty();
            using var root = new Window { Size = new(400, 240), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var ltr = Grid(new(10, 10), false); var rtl = Grid(new(10, 80), true);
            var auto = new ItemList { Position = new(220, 10), Size = new(40, 40), AutoWidth = true, AutoHeight = true }; auto.AddItem("Inventory label"); root.AddChild(auto);
            var caption = new ItemList { Size = new(170, 40), LayoutDirection = LayoutDirection.RTL }; caption.AddItem("RTL label"); root.AddChild(caption);
            var scroll = new ItemList { Position = new(220, 90), Size = new(100, 100), MaxColumns = 3, FixedColumnWidth = 60, FixedIconSize = new(24, 24), WraparoundItems = false, HintMode = VerticalScrollHintMode.Both };
            for (var i = 0; i < 12; i++) scroll.AddIconItem(i == 11 ? blue : red);
            scroll.AddThemeIconOverride("scroll_hint", hint); scroll.AddThemeColorOverride("scroll_hint_color", Colors.White); scroll.AddThemeStyleBoxOverride("focus", empty); root.AddChild(scroll);
            var negative = new ItemList { Position = new(10, 170), Size = new(180, 60), MaxColumns = 2, FixedColumnWidth = 40, FixedIconSize = new(12, 8), IconScale = -1 }; negative.AddIconItem(hint); negative.AddIconItem(hint); root.AddChild(negative);
            var frame = 0; var before = 0L;
            root.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); ltr.Position = new(10, 10); rtl.Position = new(10, 80); caption.Position = new(220, 200); scroll.Current = 11; scroll.EnsureCurrentIsVisible();
                RenderingServer.FramePostDraw += AfterDraw;
            };
            void AfterDraw()
            {
                frame++;
                if (frame <= 4)
                {
                    using var image = root.GetTexture().GetImage()!;
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_ITEM_LAYOUT_OUTPUT") is { } initial) { Directory.CreateDirectory(initial); image.SavePNG(System.IO.Path.Combine(initial, "phase-" + frame + ".png")); }
                    if (frame == 1)
                    {
                        VerifyGrid(image, ltr, false); VerifyGrid(image, rtl, true);
                        Check(BrightPixels(image, new(340, 201, 45, 25)) > 0 && BrightPixels(image, new(225, 201, 40, 25)) == 0, "RTL captions occupy the right side of their content allocation.");
                        Check(image.GetPixel(62, 179).B > .8f && image.GetPixel(70, 179).G > .8f, "Negative icon scales preserve signed texture footprints and source reversal.");
                        Check(scroll.GetHScrollBar().Value > 0 && scroll.GetVScrollBar().Value > 0 && BluePixels(image, scroll) > 0, $"Queued visibility reveals the final icon: h={scroll.GetHScrollBar().Value}, v={scroll.GetVScrollBar().Value}, blue={BluePixels(image, scroll)}.");
                        scroll.LayoutDirection = LayoutDirection.RTL; scroll.Position = new(220, 90); scroll.EnsureCurrentIsVisible();
                    }
                    else if (frame == 2)
                    {
                        Check(auto.Size.X > 80 && BrightPixels(image, new(265, 12, Math.Max(1, auto.Size.X - 48), auto.Size.Y - 4)) > 0, "Automatic width publishes real text pixels beyond the original forty-pixel box.");
                        Check(scroll.GetHScrollBar().Value < 0 && BluePixels(image, scroll) > 0, "RTL visibility uses the signed horizontal scroll range.");
                        scroll.LayoutDirection = LayoutDirection.LTR; scroll.Position = new(220, 90); scroll.ForceUpdateListSize(); scroll.GetVScrollBar().Value = 0; scroll.GetHScrollBar().Value = 0; scroll.WraparoundItems = true; scroll.MaxColumns = 1; scroll.HintMode = VerticalScrollHintMode.Bottom; scroll.TileScrollHint = false;
                    }
                    else if (frame == 3)
                    {
                        Check(image.GetPixel(240, 189).G > .8f && image.GetPixel(241, 189).G > .8f, "Stretched hints sample a continuous horizontal half."); scroll.TileScrollHint = true;
                    }
                    else Check(image.GetPixel(240, 189).G > .8f && image.GetPixel(241, 189).B > .8f, "Tiled hints preserve adjacent source texels.");
                }
                if (frame >= 4) Frame(frame);
                if (frame == 36) before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 100)
                {
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, $"Prepared active text/icon layout and replay allocate {allocated} owner bytes.");
                    root.Tree!.Quit();
                }
            }
            Check(Engine.Run(root) == 0 && root.IsDisposed && frame == 100, "The public layout session retires its owned hierarchy.");
            Console.WriteLine(backend + ": automatic text sizing, LTR/RTL grid, two-axis visibility, hint pixels and64 zero-owner-byte active frames passed.");

            ItemList Grid(Vector2 position, bool rightToLeft)
            {
                var list = new ItemList { Position = position, Size = new(180, 60), MaxColumns = 3, FixedColumnWidth = 40, FixedIconSize = new(12, 8), LayoutDirection = rightToLeft ? LayoutDirection.RTL : LayoutDirection.LTR };
                list.AddIconItem(red); list.AddIconItem(green); list.AddIconItem(blue); root.AddChild(list); return list;
            }
            void Frame(int i)
            {
                ltr.FixedColumnWidth = (i & 1) == 0 ? 40 : 44; auto.FixedColumnWidth = (i & 1) == 0 ? 0 : 80;
                ltr.ForceUpdateListSize(); auto.ForceUpdateListSize();
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); Engine.MaxFPS = maxFPS; }
    }

    private static void VerifyGrid(Image image, ItemList list, bool rtl)
    {
        var panel = list.GetThemeStyleBox("panel")!; var gap = list.GetThemeConstant("h_separation");
        for (var i = 0; i < 3; i++)
        {
            var x = panel.GetOffset().X + i * (40 + gap) + gap / 2f + 6;
            if (rtl) x = list.Size.X - x + panel.GetMargin(Side.Left) - panel.GetMargin(Side.Right);
            var y = panel.GetOffset().Y + (8 + list.GetThemeConstant("v_separation")) / 2f;
            var color = image.GetPixel((int)(list.Position.X + x), (int)(list.Position.Y + y));
            Check((i == 0 ? color.R : i == 1 ? color.G : color.B) > .8f, $"{(rtl ? "RTL" : "LTR")} cell{i} preserves logical icon placement: {color}.");
            Check(list.GetItemAtPosition(new(x, y), true) == i, "Rendered icons agree with exact content hit testing.");
        }
    }
    private static int BluePixels(Image image, ItemList list)
    {
        var count = 0;
        for (var y = (int)list.Position.Y + 5; y < list.Position.Y + list.Size.Y - 15; y++)
            for (var x = (int)list.Position.X + 15; x < list.Position.X + list.Size.X - 15; x++)
                if (image.GetPixel(x, y) is { B: > .8f, R: < .2f, G: < .2f }) count++;
        return count;
    }
    private static int BrightPixels(Image image, Rect2 area)
    {
        var count = 0;
        for (var y = (int)area.Position.Y; y < area.End.Y; y++) for (var x = (int)area.Position.X; x < area.End.X; x++) if (image.GetPixel(x, y).R > .2f) count++;
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
