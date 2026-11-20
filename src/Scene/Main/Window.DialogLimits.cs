namespace Electron2D;

public partial class Window
{
    private bool _keepTitleVisible;
    internal bool ClampToEmbedder;
    /// <summary>Gets or sets whether window width expands to display its complete title.</summary>
    /// <value>False initially; AcceptDialog defaults to true.</value>
    /// <remarks>Uses the embedded title font, maximum focused/unfocused style minimum and close-button space. MaxSize still bounds the result.
    /// On Win32 top-level windows this uses the system caption font and space for caption controls at the window's DPI.
    /// Changes made in fullscreen are applied to the caption width when the window leaves fullscreen.
    /// Disabling the policy removes the title minimum. It leaves the current width in place.</remarks>
    /// <exception cref="NotSupportedException">Enabled on a native backend or popup role without caption measurement.</exception>
    public bool KeepTitleVisible
    {
        get { ThrowIfDisposed(); return _keepTitleVisible; }
        set
        {
            EnsureMutable();
            if (_keepTitleVisible == value)
                return;
            if (_display is { } display)
            {
                display.SetNativeTitleVisibility(value);
                _keepTitleVisible = value;
            }
            else
            {
                _keepTitleVisible = value;
                UpdateEmbeddedContents();
            }
        }
    }
    private Vector2i TitleMinimum()
    {
        if (!_keepTitleVisible || _display is not null || Borderless || GetThemeFont("title_font", "Window") is not { } font) return Vector2i.Zero;
        return new(checked((int)MathF.Ceiling(font.GetStringSize(Title, fontSize: GetThemeFontSize("title_font_size", "Window"), direction: IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR).X + DecorationMetric(true).X + GetThemeConstant("close_h_offset", "Window"))), 0);
    }
    private Vector2i ClampEmbeddedPosition(Vector2i position)
    {
        if (!ClampToEmbedder || Embedder == null) return position;
        var area = Embedder.GetVisibleRect(); var top = Borderless ? 0 : Math.Max(0, GetThemeConstant("title_height", "Window"));
        return new((int)Math.Clamp((long)position.X, (long)area.Position.X, Math.Max((long)area.Position.X, (long)(area.End.X) - Size.X)),
            (int)Math.Clamp((long)position.Y, (long)area.Position.Y + top, Math.Max((long)area.Position.Y + top, (long)(area.End.Y) - Size.Y)));
    }
}
