namespace Electron2D.Editor;

/// <summary>Builds the editor's initial branded window using the public runtime API.</summary>
internal static class EditorScene
{
    /// <summary>Creates a detached editor window with a centered, borrowed logo texture.</summary>
    /// <param name="texture">The caller-owned dark stacked logo, retained until the window exits.</param>
    /// <returns>A root window transferred to Engine.Run by the caller.</returns>
    internal static Window CreateWindow(Texture texture)
    {
        var window = new Window { Title = "Electron2D", Size = new(1152, 800) };
        window.Ready += _ => RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));
        var brand = new Control { Name = "Brand", Size = texture.GetSize() + new Vector2(0, 44), MouseFilter = MouseFilter.Ignore };
        var logo = new TextureRect
        {
            Name = "Logo",
            Texture = texture,
            ExpandMode = TextureRectExpandMode.IgnoreSize,
            StretchMode = TextureStretchMode.KeepAspectCentered,
            MouseFilter = MouseFilter.Ignore,
            Size = texture.GetSize()
        };
        var caption = new Label("Game engine")
        {
            Name = "Caption",
            Position = new(0, logo.Size.Y),
            Size = new(logo.Size.X, 44),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilter.Ignore
        };
        caption.AddThemeFontSizeOverride("font_size", 32);
        caption.AddThemeColorOverride("font_color", Color.FromHTML("#B9AEBD"));
        brand.AddChild(logo);
        brand.AddChild(caption);
        window.AddChild(brand);
        brand.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize);
        return window;
    }
}
