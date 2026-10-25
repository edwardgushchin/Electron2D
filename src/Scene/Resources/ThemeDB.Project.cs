namespace Electron2D;

public sealed partial class ThemeDB
{
    private Theme? _projectTheme;
    private Font? _projectFont;
    private Font? _originalDefaultFont, _originalFallbackFont;

    /// <summary>Returns the project theme selected by the most recent completed theme preparation at engine startup.</summary>
    /// <returns>A borrowed loaded Theme, or null when gui/theme/custom is empty.</returns>
    /// <remarks>Engine.Start and windowed runs load and validate configured resources before activating the main loop.
    /// A completed later theme preparation replaces the owned snapshot; borrowed project resources are valid until that replacement.
    /// Failed preparation preserves the previous snapshot. This getter does not perform file I/O.</remarks>
    /// <exception cref="ObjectDisposedException">The retained service or its selected theme is disposed.</exception>
    public static Theme? GetProjectTheme() => Service.GetProjectThemeCore();

    private Theme? GetProjectThemeCore()
    {
        lock (_gate) { ThrowIfDisposed(); if (_projectTheme?.IsDisposed == true) throw new ObjectDisposedException(nameof(Theme)); return _projectTheme; }
    }
    internal static void LoadProjectTheme() => Service.LoadProjectThemeCore();
    private void LoadProjectThemeCore()
    {
        var path = ProjectSettings.GetWithOverride(ProjectSettings.GUIThemeCustom);
        var fontPath = ProjectSettings.GetWithOverride(ProjectSettings.GUIThemeCustomFont);
        Theme? theme = null; Font? font = null;
        try
        {
            if (path.Length != 0) theme = ResourceLoader.Load<Theme>(path, ResourceLoader.CacheMode.Ignore);
            if (fontPath.Length != 0) font = ResourceLoader.Load<Font>(fontPath, ResourceLoader.CacheMode.Ignore);
        }
        catch
        {
            theme?.Dispose(); font?.Dispose(); throw;
        }
        Theme? oldTheme; Font? oldFont; Font? defaultFont, fallbackFont;
        lock (_gate)
        {
            ThrowIfDisposed(); oldTheme = _projectTheme; oldFont = _projectFont;
            if (theme is null && font is null && oldTheme is null && oldFont is null) return;
            if (font is not null && oldFont is null) { _originalDefaultFont = _defaultTheme.DefaultFont; _originalFallbackFont = _font; }
            defaultFont = font ?? (oldFont is not null && ReferenceEquals(_defaultTheme.DefaultFont, oldFont) ? _originalDefaultFont : _defaultTheme.DefaultFont);
            fallbackFont = font ?? (oldFont is not null && ReferenceEquals(_font, oldFont) ? _originalFallbackFont : _font);
            if (oldTheme is not null) { oldTheme.Changed -= DefaultChanged; oldTheme.Disposed -= DefaultDisposed; }
            _projectTheme = theme; _projectFont = font;
            if (theme is not null) { theme.Changed += DefaultChanged; theme.Disposed += DefaultDisposed; }
        }
        List<Exception>? errors = null;
        try { _defaultTheme.DefaultFont = defaultFont; } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { FallbackFontCore = fallbackFont; } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { _publisherContextChanged.Handlers?.Invoke(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { oldTheme?.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        try { oldFont?.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        Node.ThrowCollected("Project theme publication callbacks failed.", errors);
    }
    private void DisposeProjectTheme()
    {
        if (_projectTheme is { } theme) { theme.Changed -= DefaultChanged; theme.Disposed -= DefaultDisposed; theme.Dispose(); }
        _projectFont?.Dispose(); _projectTheme = null; _projectFont = null;
    }
}
