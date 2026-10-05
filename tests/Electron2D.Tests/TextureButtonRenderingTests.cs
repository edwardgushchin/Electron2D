using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTextureButton(string backend)
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        Color[] colors = [Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Magenta, Colors.Cyan, Colors.White, new(.5f, .5f, .5f)];
        for (var y = 0; y < 2; y++) for (var x = 0; x < 4; x++) image.SetPixel(x, y, colors[y * 4 + x]);
        using var texture = ImageTexture.CreateFromImage(image);
        using var normal = TextureButtonSolid(Colors.Red); using var pressed = TextureButtonSolid(Colors.Green);
        using var hovered = TextureButtonSolid(Colors.Blue); using var disabled = TextureButtonSolid(new(.5f, .5f, .5f));
        using var focusImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); focusImage.SetPixel(0, 0, Colors.White); focusImage.SetPixel(0, 1, Colors.White);
        using var focused = ImageTexture.CreateFromImage(focusImage);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 2, 2) };
        Action<Resource> early = _ => throw new ApplicationException("expected earlier texture observer"); atlas.Changed += early;
        var window = new Window { Size = new(180, 64) }; var buttons = new TextureButton[7];
        for (var mode = 0; mode < buttons.Length; mode++)
        {
            var button = new TextureButton { Name = $"Stretch{mode}", Position = new(3 + mode * 24, 3), Size = new(20, 16), IgnoreTextureSize = true, TextureNormal = texture, StretchMode = (TextureStretchMode)mode, TextureFilter = TextureFilter.Nearest };
            window.AddChild(button); buttons[mode] = button;
        }
        var state = new NativeTextureButtonProbe
        {
            Name = "State",
            Position = new(3, 28),
            Size = new(20, 16),
            IgnoreTextureSize = true,
            StretchMode = TextureStretchMode.Scale,
            TextureNormal = normal,
            TexturePressed = pressed,
            TextureHover = hovered,
            TextureDisabled = disabled,
            TextureFocused = focused,
            TextureFilter = TextureFilter.Nearest,
            ToggleMode = true
        };
        var focusOnly = new TextureButton { Name = "FocusOnly", Position = new(27, 28), Size = new(20, 16), IgnoreTextureSize = true, StretchMode = TextureStretchMode.Scale, TextureFocused = focused, TextureFilter = TextureFilter.Nearest };
        var atlasButton = new TextureButton { Name = "Atlas", Position = new(51, 28), Size = new(20, 16), IgnoreTextureSize = true, StretchMode = TextureStretchMode.Scale, TextureNormal = atlas, TextureFilter = TextureFilter.Nearest };
        window.AddChild(state); window.AddChild(focusOnly); window.AddChild(atlasButton);
        var frame = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frame++; using var pixels = server.Readback();
                for (var mode = 0; mode < buttons.Length; mode++)
                    for (var y = 0; y < 16; y++) for (var x = 0; x < 20; x++)
                            Pixel(pixels, 3 + mode * 24 + x, 3 + y, TextureButtonSample(image, mode, x, y, frame is 2 or 3, frame == 2));
                var stateColor = frame switch { 2 => Colors.Green, 3 => new Color(.5f, .5f, .5f), 4 => Colors.Blue, _ => Colors.Red };
                Pixel(pixels, 18, 36, stateColor); Pixel(pixels, 6, 36, frame == 5 ? Colors.White : stateColor);
                Pixel(pixels, 30, 36, frame >= 6 ? Colors.White : Colors.Black); Pixel(pixels, 42, 36, Colors.Black);
                Pixel(pixels, 54, 30, frame == 7 ? Colors.Blue : Colors.Red);
                if (frame is 1 or 5) File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-texture-button-{backend}-{frame}.png"), pixels.SavePNGToBuffer());
                switch (frame)
                {
                    case 1: foreach (var button in buttons) { button.FlipH = true; button.FlipV = true; } state.ButtonPressed = true; break;
                    case 2: foreach (var button in buttons) button.FlipV = false; state.Disabled = true; break;
                    case 3: foreach (var button in buttons) button.FlipH = false; state.Disabled = false; state.ButtonPressed = false; state.Hover(true); break;
                    case 4: state.Hover(false); state.GrabFocus(); break;
                    case 5: focusOnly.GrabFocus(); break;
                    case 6:
                        var failed = false; try { atlas.Region = new(2, 0, 2, 2); } catch (ApplicationException) { failed = true; }
                        Check(failed, "Texture-button atlas mutation commits before an earlier observer throws."); break;
                    default: window.Tree!.Quit(); break;
                }
            };
        };
        Engine.Run(window); Released(window); atlas.Changed -= early;
        VerifyTextureButtonWarm(backend, texture);
        Console.WriteLine($"Texture-button seven stretch modes, both flips, normal/pressed/hover/disabled/focus overlays, atlas callback recovery and warm frames passed: {backend}.");
    }

    private static ImageTexture TextureButtonSolid(Color color)
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image);
    }
    private static Color TextureButtonSample(Image image, int mode, int x, int y, bool flipH, bool flipV)
    {
        var left = 0f; var top = 0f; var width = 20f; var height = 16f; var sourceLeft = 0f; var sourceWidth = 4f;
        if (mode is 2 or 3) { width = 4; height = 2; if (mode == 3) { left = 8; top = 7; } }
        else if (mode is 4 or 5) { height = 10; if (mode == 5) top = 3; }
        else if (mode == 6) { sourceLeft = .75f; sourceWidth = 2.5f; }
        var px = x + .5f - left; var py = y + .5f - top;
        if (px < 0 || py < 0 || px >= width || py >= height) return Colors.Black;
        float sx, sy;
        if (mode == 1) { sx = (flipH ? width - px : px) % 4; sy = (flipV ? height - py : py) % 2; }
        else { sx = Math.Clamp(sourceLeft + sourceWidth * (flipH ? 1 - px / width : px / width), sourceLeft + .5f, sourceLeft + sourceWidth - .5f); sy = Math.Clamp(2 * (flipV ? 1 - py / height : py / height), .5f, 1.5f); }
        return image.GetPixel(Math.Clamp((int)sx, 0, 3), Math.Clamp((int)sy, 0, 1));
    }
    private static void VerifyTextureButtonWarm(string backend, Texture texture)
    {
        using var pressedTexture = TextureButtonSolid(Colors.Blue);
        var window = new Window { Size = new(64, 48) }; var button = new TextureButton
        {
            TextureNormal = texture,
            TexturePressed = pressedTexture,
            ToggleMode = true,
            IgnoreTextureSize = true,
            Position = new(4, 4),
            Size = new(30, 20),
            StretchMode = TextureStretchMode.KeepAspectCovered,
            TextureFilter = TextureFilter.Nearest
        };
        window.AddChild(button); var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!; var server = RenderingServer.Service!;
            tree.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); button.FlipH = (frames & 1) == 0; button.ButtonPressed = (frames & 1) != 0; };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(allocated == 0, $"Texture-button {backend} 64 warmed active frames alternating distinct state textures allocate {allocated} managed bytes from ProcessFrameStarted through FramePostDraw.");
    }
    private sealed class NativeTextureButtonProbe : TextureButton
    {
        internal void Hover(bool hover) => OnNotification(hover ? NotificationMouseEnter : NotificationMouseExit);
    }
}
