namespace Electron2D.Editor;

/// <summary>Builds the editor's initial branded window using the public runtime API.</summary>
internal static class EditorScene
{
    /// <summary>Creates a detached editor window with a centered, pixel-aligned brand composition.</summary>
    /// <param name="mark">The caller-owned dark pixel mark, retained until the window exits.</param>
    /// <param name="wordmark">The caller-owned outlined name, retained until the window exits.</param>
    /// <param name="font">The caller-owned regular caption font, retained until the window exits.</param>
    /// <returns>A root window transferred to Engine.Run by the caller.</returns>
    internal static Window CreateWindow(Texture mark, Texture wordmark, Font font)
    {
        var window = new Window { Title = "Electron2D", Size = new(1152, 800), SnapTransformsToPixel = true };
        window.Ready += _ => RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));
        var brand = new Control { Name = "Brand", Size = new(488, 334), MouseFilter = MouseFilter.Ignore };
        var character = new TextureRect
        {
            Name = "Mark",
            Texture = mark,
            ExpandMode = TextureRectExpandMode.IgnoreSize,
            StretchMode = TextureStretchMode.KeepAspectCentered,
            MouseFilter = MouseFilter.Ignore,
            TextureFilter = TextureFilter.Nearest,
            Position = new(148, 0),
            Size = new(192, 192)
        };
        var title = new TextureRect
        {
            Name = "Wordmark",
            Texture = wordmark,
            ExpandMode = TextureRectExpandMode.IgnoreSize,
            StretchMode = TextureStretchMode.Keep,
            MouseFilter = MouseFilter.Ignore,
            TextureFilter = TextureFilter.Linear,
            Position = new(24, 198),
            Size = wordmark.GetSize()
        };
        var caption = new Label("Game engine")
        {
            Name = "Caption",
            Position = new(0, 270),
            Size = new(488, 44),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            MouseFilter = MouseFilter.Ignore
        };
        caption.AddThemeFontSizeOverride("font_size", 32);
        caption.AddThemeColorOverride("font_color", Color.FromHTML("#B9AEBD"));
        caption.AddThemeFontOverride("font", font);
        brand.AddChild(character);
        brand.AddChild(title);
        brand.AddChild(caption);
        window.AddChild(brand);
        brand.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize);
        return window;
    }
}
