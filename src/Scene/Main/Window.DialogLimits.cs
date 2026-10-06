namespace Electron2D;

public partial class Window
{
    private bool _keepTitleVisible;
    internal bool ClampToEmbedder;
    /// <summary>Gets or sets whether embedded window width expands to display its complete title.</summary>
    /// <value>False initially; AcceptDialog defaults to true.</value>
    /// <remarks>Uses the embedded title font and close-button space. MaxSize still bounds the result.
    /// Native title measurement is unavailable and a native root rejects this enabled policy.</remarks>
    /// <exception cref="NotSupportedException">Enabled on an active native root.</exception>
    public bool KeepTitleVisible
    {
        get { ThrowIfDisposed(); return _keepTitleVisible; }
        set { EnsureMutable(); if (value && _display != null) throw new NotSupportedException("Native title measurement is unavailable."); if (_keepTitleVisible == value) return; _keepTitleVisible = value; UpdateEmbeddedContents(); }
    }
    private Vector2i TitleMinimum()
    {
        if (!_keepTitleVisible || Borderless || GetThemeFont("title_font", "Window") is not { } font) return Vector2i.Zero;
        return new(checked((int)MathF.Ceiling(font.GetStringSize(Title, fontSize: GetThemeFontSize("title_font_size", "Window")).X + 48)), 0);
    }
    private Vector2i ClampEmbeddedPosition(Vector2i position)
    {
        if (!ClampToEmbedder || Embedder == null) return position;
        var area = Embedder.GetVisibleRect(); var top = Borderless ? 0 : Math.Max(0, GetThemeConstant("title_height", "Window"));
        return new((int)Math.Clamp((long)position.X, (long)area.Position.X, Math.Max((long)area.Position.X, (long)(area.End.X) - Size.X)),
            (int)Math.Clamp((long)position.Y, (long)area.Position.Y + top, Math.Max((long)area.Position.Y + top, (long)(area.End.Y) - Size.Y)));
    }
}
