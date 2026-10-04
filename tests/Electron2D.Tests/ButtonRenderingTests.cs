using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private sealed class ButtonPixelSkin : IDisposable
    {
        internal readonly Theme Theme = new();
        internal readonly List<Resource> Owned = [];
        internal readonly ImageTexture Icon;
        internal ButtonPixelSkin(Font font)
        {
            var empty = new StyleBoxEmpty(); Owned.Add(empty);
            StyleBoxFlat Style(Color color)
            {
                var style = new StyleBoxFlat { BGColor = color, AntiAliasing = false }; style.SetContentMarginAll(0); Owned.Add(style); return style;
            }
            var focus = Style(Colors.Transparent); focus.DrawCenter = false; focus.BorderColor = Colors.Magenta; focus.SetBorderWidthAll(2);
            foreach (var type in new[] { "Button", "CheckBox", "CheckButton" })
            {
                Theme.SetFont("font", type, font); Theme.SetFontSize("font_size", type, 16);
                foreach (var name in new[] { "normal", "pressed", "hover", "hover_pressed", "disabled", "focus" }) Theme.SetStyleBox(name, type, empty);
                foreach (var name in new[] { "font_color", "font_pressed_color", "font_hover_color", "font_hover_pressed_color", "font_disabled_color" }) Theme.SetColor(name, type, Colors.White);
                Theme.SetColor("font_focus_color", type, Colors.Cyan); Theme.SetConstant("h_separation", type, 4);
                Theme.SetConstant("outline_size", type, 0); Theme.SetConstant("line_spacing", type, 0);
            }
            Theme.SetStyleBox("normal", "Button", Style(Colors.Red)); Theme.SetStyleBox("hover", "Button", Style(Colors.Green));
            Theme.SetStyleBox("pressed", "Button", Style(Colors.Blue)); Theme.SetStyleBox("hover_pressed", "Button", Style(Colors.Yellow));
            Theme.SetStyleBox("disabled", "Button", Style(new(.5f, .5f, .5f))); Theme.SetStyleBox("focus", "Button", focus);
            var on = Solid(Colors.Green, 8, 8); var off = Solid(Colors.Blue, 8, 8); var radio = Solid(Colors.Yellow, 8, 8);
            foreach (var type in new[] { "CheckBox", "CheckButton" })
            {
                foreach (var suffix in new[] { "", "_disabled", "_mirrored", "_disabled_mirrored" })
                {
                    Theme.SetIcon("checked" + suffix, type, on); Theme.SetIcon("unchecked" + suffix, type, off);
                }
            }
            foreach (var name in new[] { "radio_checked", "radio_unchecked", "radio_checked_disabled", "radio_unchecked_disabled" }) Theme.SetIcon(name, "CheckBox", radio);
            Icon = Solid(Colors.Magenta, 8, 4);
        }
        private ImageTexture Solid(Color color, int width, int height)
        {
            using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8); image.Fill(color);
            var texture = ImageTexture.CreateFromImage(image); Owned.Add(texture); return texture;
        }
        public void Dispose() { Theme.Dispose(); foreach (var item in Owned) item.Dispose(); }
    }
    private static void VerifyButtons(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        using var skin = new ButtonPixelSkin(font); using var group = new ButtonGroup();
        using var empty = new StyleBoxEmpty();
        var window = new Window { Size = new(300, 220) };
        var button = new Button("A") { Name = "Action", Theme = skin.Theme, Position = new(10, 10), Size = new(60, 30), MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        var box = new CheckBox("A") { Name = "Check", Theme = skin.Theme, Position = new(10, 60), Size = new(70, 30), MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        var toggle = new CheckButton("A") { Name = "Switch", Theme = skin.Theme, Position = new(110, 60), Size = new(70, 30), MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        var wrap = new Button("AV AV") { Name = "Wrap", Theme = skin.Theme, Position = new(10, 110), Size = new(25, 80), AutowrapMode = TextAutowrapMode.Word, Alignment = HorizontalAlignment.Left, MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        var clip = new Button("A") { Name = "Clip", Theme = skin.Theme, Position = new(100, 110), Size = new(10, 30), ClipText = true, Alignment = HorizontalAlignment.Left, MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        var icon = new Button { Name = "Icon", Theme = skin.Theme, Icon = skin.Icon, ExpandIcon = true, Position = new(180, 110), Size = new(40, 30), MouseFilter = MouseFilter.Ignore, TextureFilter = TextureFilter.Nearest };
        foreach (var item in new[] { wrap, clip, icon })
            foreach (var state in new[] { "normal", "pressed", "hover", "hover_pressed", "disabled", "focus" }) item.AddThemeStyleBoxOverride(state, empty);
        window.AddChild(button); window.AddChild(box); window.AddChild(toggle); window.AddChild(wrap); window.AddChild(clip); window.AddChild(icon);
        var frames = 0; var pressed = 0; var downs = 0; var ups = 0; button.Pressed += () => pressed++; button.ButtonDown += () => downs++; button.ButtonUp += () => ups++;
        window.Ready += _ =>
        {
            wrap.Size = new(25, 80); clip.Size = new(10, 30);
            var nativeWindows = SDL.GetWindows(out var windowCount); var nativeWindow = nativeWindows![0];
            var windowID = SDL.GetWindowID(nativeWindow); var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(nativeWindow) : 1f;
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = server.Readback(); frames++;
                try
                {
                    if (frames == 1)
                    {
                        Pixel(image, 20, 20, Colors.Red); ButtonGlyphPixels(image, new(34, 19), Colors.White, Colors.Red);
                        Pixel(image, 11, 72, Colors.Blue); Pixel(image, 173, 72, Colors.Blue);
                        Check(FontInk(image, new(10, 132, 24, 14)) > 30 && FontInk(image, new(10, 155, 24, 14)) > 30, "Button wraps two visible real glyph baselines.");
                        Check(FontInk(image, new(100, 110, 10, 30)) == 0, "Whole-glyph clipping omits a glyph wider than the content width.");
                        Pixel(image, 185, 120, Colors.Magenta); Pixel(image, 185, 112, Colors.Black);
                        button.MouseFilter = MouseFilter.Stop; SliderMotion(windowID, scale, new(25, 25));
                    }
                    else if (frames == 2)
                    {
                        Pixel(image, 20, 20, Colors.Green); SliderButton(windowID, scale, new(25, 25));
                    }
                    else if (frames == 3)
                    {
                        Pixel(image, 20, 20, Colors.Blue); Check(downs == 1 && pressed == 0, "Native down enters the press-attempt style before release activation.");
                        SliderMotion(windowID, scale, new(250, 25)); SliderButton(windowID, scale, new(250, 25), false);
                    }
                    else if (frames == 4)
                    {
                        Pixel(image, 20, 20, Colors.Red); Check(ups == 1 && pressed == 0, "Outside captured release cancels activation.");
                        SliderMotion(windowID, scale, new(25, 25)); SliderButton(windowID, scale, new(25, 25)); SliderButton(windowID, scale, new(25, 25), false);
                    }
                    else if (frames == 5)
                    {
                        Pixel(image, 20, 20, Colors.Green); Check(pressed == 1 && downs == 2 && ups == 2, "A complete native click emits exactly one action.");
                        button.MouseFilter = MouseFilter.Ignore; button.GrabFocus();
                    }
                    else if (frames == 6)
                    {
                        Pixel(image, 11, 11, Colors.Magenta); ButtonGlyphPixels(image, new(34, 19), Colors.Cyan, Colors.Red);
                        var key = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = windowID, Key = SDL.Keycode.Space, Scancode = SDL.Scancode.Space, Down = true } };
                        Check(SDL.PushEvent(ref key), "Queue native button accept down."); key.Key.Type = SDL.EventType.KeyUp; key.Key.Down = false; Check(SDL.PushEvent(ref key), "Queue native button accept up.");
                    }
                    else if (frames == 7)
                    {
                        Check(pressed == 2, "Focused native accept input activates the same action path.");
                        button.ToggleMode = true; button.ButtonPressed = true; button.MouseFilter = MouseFilter.Stop;
                        SliderMotion(windowID, scale, new(25, 25));
                    }
                    else if (frames == 8)
                    {
                        Pixel(image, 20, 20, Colors.Yellow);
                        button.Disabled = true; box.ButtonGroup = group; box.ButtonPressed = true; toggle.ButtonPressed = true;
                        toggle.LayoutDirection = LayoutDirection.RTL; toggle.Position = new(110, 60);
                    }
                    else if (frames == 9)
                    {
                        Pixel(image, 20, 20, new(.5f, .5f, .5f)); Pixel(image, 11, 72, Colors.Yellow); Pixel(image, 111, 72, Colors.Green);
                        button.Flat = true; button.ReleaseFocus(); box.LayoutDirection = LayoutDirection.RTL; box.Position = new(10, 60);
                    }
                    else
                    {
                        Pixel(image, 20, 20, Colors.Black); Pixel(image, 73, 72, Colors.Yellow); Pixel(image, 111, 72, Colors.Green);
                        File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-buttons-{backend}.png"), image.SavePNGToBuffer()); window.Tree!.Quit();
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Button native {backend}, phase {frames}.", error); }
            };
        };
        Engine.Run(window); Released(window); Check(frames == 10, "All native button phases must execute.");
        VerifyButtonWarm(backend, skin);
        Console.WriteLine($"Buttons native state/focus/click/keyboard, wrapped/clipped text, RTL check/radio/switch and 64 active zero-allocation frames passed: {backend}.");
    }
    private static void VerifyButtonWarm(string backend, ButtonPixelSkin skin)
    {
        var window = new Window { Size = new(240, 130) };
        var button = new Button("A") { Name = "Action", Theme = skin.Theme, Size = new(90, 30), Position = new(10, 10), ToggleMode = true, MouseFilter = MouseFilter.Ignore };
        var box = new CheckBox("A") { Name = "Check", Theme = skin.Theme, Size = new(90, 30), Position = new(10, 50), MouseFilter = MouseFilter.Ignore };
        var toggle = new CheckButton("A") { Name = "Switch", Theme = skin.Theme, Size = new(90, 30), Position = new(120, 50), MouseFilter = MouseFilter.Ignore };
        window.AddChild(button); window.AddChild(box); window.AddChild(toggle);
        var frames = 0; var draws = 0; long before = 0, afterMutation = 0, allocated = 0, mutationAllocated = 0;
        long drawStamp = 0, throughButton = 0, throughBox = 0, throughToggle = 0;
        button.Draw += _ => { var now = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 64) throughButton += now - afterMutation; drawStamp = now; draws++; };
        box.Draw += _ => { var now = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 64) throughBox += now - drawStamp; drawStamp = now; draws++; };
        toggle.Draw += _ => { var now = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 64) throughToggle += now - drawStamp; drawStamp = now; draws++; };
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                var on = (frames & 1) == 0;
                button.Text = on ? "A" : "V"; button.ButtonPressed = on; box.ButtonPressed = on; toggle.ButtonPressed = on;
                afterMutation = GC.GetAllocatedBytesForCurrentThread();
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; mutationAllocated += afterMutation - before; }
                if (++frames == 128) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 128 && draws == 384 && allocated == 0, $"Native {backend} button/check/switch updates, text, recording and rendering allocated {allocated} bytes in 64 measured active frames (mutation={mutationAllocated}, button={throughButton}, box={throughBox}, toggle={throughToggle}, remaining={allocated - mutationAllocated - throughButton - throughBox - throughToggle}).");
    }
    private static void ButtonGlyphPixels(Image image, Vector2i origin, Color foreground, Color background)
    {
        for (var y = -1; y <= 12; y++) for (var x = -1; x <= 11; x++)
            {
                var amount = x >= 0 && x < 11 && y >= 0 && y < 12 ? FontOracleA16[y * 11 + x] / 255f : 0;
                Pixel(image, origin.X + x, origin.Y + y, new Color(
                    foreground.R * amount + background.R * (1 - amount), foreground.G * amount + background.G * (1 - amount),
                    foreground.B * amount + background.B * (1 - amount), 1));
            }
    }
}
