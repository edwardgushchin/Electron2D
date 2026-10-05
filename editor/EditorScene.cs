namespace Electron2D.Editor;

/// <summary>Builds the editor's initial branded window using the public runtime API.</summary>
internal static class EditorScene
{
    /// <summary>Creates a detached editor window with a centered character and live font-rendered branding.</summary>
    /// <param name="mark">The caller-owned character texture, retained until the window exits.</param>
    /// <param name="sparkle">The caller-owned decorative texture, retained until the window exits.</param>
    /// <param name="appIcon">The caller-owned application icon, retained until the window exits.</param>
    /// <param name="semibold">The caller-owned wordmark font, retained until the window exits.</param>
    /// <param name="regular">The caller-owned descriptor font, retained until the window exits.</param>
    /// <returns>A root window transferred to Engine.Run by the caller.</returns>
    internal static Window CreateWindow(Texture mark, Texture sparkle, Texture appIcon, Font semibold, Font regular)
    {
        var window = new Window { Title = "Electron2D", Size = new(1152, 800), SnapTransformsToPixel = true };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));
            if (DisplayServer.HasFeature(DisplayServer.Feature.Icon))
            {
                using var icon = appIcon.GetImage();
                DisplayServer.SetIcon(icon ?? throw new InvalidOperationException("The application icon has no image."));
            }
        };
        var brand = new Control { Name = "Brand", Size = new(640, 320), MouseFilter = MouseFilter.Ignore };
        AddTexture("Mark", mark, new(240, 12), new(160, 160));
        var prefixWidth = semibold.GetStringSize("Electron", fontSize: 82).X;
        var suffixWidth = semibold.GetStringSize("2D", fontSize: 82).X;
        var left = (brand.Size.X - prefixWidth - suffixWidth) / 2;
        AddText("Wordmark", "Electron", semibold, 82, "#F9F3EE", left, 260);
        AddText("Suffix", "2D", semibold, 82, "#F2A6CC", left + prefixWidth, 260);
        const string descriptor = "Agent-native cross-platform 2D game engine";
        AddText("Caption", descriptor, regular, 16, "#F9F3EE", (brand.Size.X - regular.GetStringSize(descriptor, fontSize: 16).X) / 2, 292);
        AddTexture("SparkleLeft", sparkle, new(205, 75), new(15, 15));
        AddTexture("SparkleRight", sparkle, new(412, 94), new(18, 18));
        window.AddChild(brand);
        brand.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize);
        return window;

        void AddTexture(string name, Texture texture, Vector2 position, Vector2 size)
        {
            brand.AddChild(new TextureRect
            {
                Name = name,
                Texture = texture,
                ExpandMode = TextureRectExpandMode.IgnoreSize,
                StretchMode = TextureStretchMode.Scale,
                TextureFilter = TextureFilter.Nearest,
                MouseFilter = MouseFilter.Ignore,
                Position = position,
                Size = size
            });
        }

        void AddText(string name, string text, Font font, int size, string color, float x, float baseline)
        {
            var label = new Label(text)
            {
                Name = name,
                Position = new(x, baseline - font.GetAscent(size)),
                Size = font.GetStringSize(text, fontSize: size),
                MouseFilter = MouseFilter.Ignore
            };
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", Color.FromHTML(color));
            brand.AddChild(label);
        }
    }
}
