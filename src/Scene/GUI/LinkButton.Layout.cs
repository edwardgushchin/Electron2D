namespace Electron2D;

public partial class LinkButton
{
    private readonly TextLayout _layout = new();
    private readonly List<TextBIDIRange> _contexts = [];
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private Font? _font;
    private long _fontGeneration = -1;
    private int _fontSize, _revision, _entryGeneration, _resourcePending;
    private bool _dirty = true, _shaping, _minimumPending;
    private float _width = -1;
    private SceneTree? _resourceTree;
    private Action? _resourceDispatch;

    private void PollFont()
    {
        var font = GetThemeFont("font") ?? throw new InvalidOperationException("A link requires its theme font.");
        ObjectDisposedException.ThrowIf(font.IsDisposed, font);
        var size = GetThemeFontSize("font_size"); Font.ValidateSize(size);
        if (!ReferenceEquals(_font, font))
        {
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            _font = font; font.Changed += _resourceChanged; font.Disposed += _resourceDisposed; MarkFontChange();
        }
        var generation = font.GetContentGeneration();
        if (_fontGeneration != generation || _fontSize != size) { _fontGeneration = generation; _fontSize = size; MarkFontChange(); }
    }
    private void MarkFontChange() { _revision++; _dirty = true; _minimumPending = true; InvalidateCanvas(); }
    private void EnsureShaped()
    {
        CheckLink(); if (_shaping) return; PollFont();
        var width = _overrun == global::Electron2D.TextOverrunBehavior.NoTrimming ? -1 : Math.Max(1, Size.X);
        if (_width != width) { _width = width; _dirty = true; }
        if (!_dirty) return;
        var revision = _revision; _shaping = true;
        try
        {
            var text = _translatedText;
            ParseStructuredText(_parser, _optionsView, text, _contexts); CheckLink();
            if (revision != _revision) return;
            var direction = _textDirection == global::Electron2D.TextDirection.Inherited
                ? IsLayoutRTL() ? global::Electron2D.TextDirection.RTL : global::Electron2D.TextDirection.LTR
                : (int)_textDirection < 0 ? global::Electron2D.TextDirection.Auto : _textDirection;
            var language = _language;
            if (language.Length == 0) language = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
            if (language.Length == 0) language = TranslationServer.Culture.Name;
            if (language.Length == 0) language = TranslationServer.GetToolLocale();
            var overrun = (int)_overrun is >= 0 and <= 6 ? (int)_overrun : 1;
            _layout.Build(_font!, new(text, _fontSize, width, HorizontalAlignment.Left, 1,
                TextLineBreakFlags.None, TextJustificationFlags.None, direction, TextOrientation.Horizontal),
                new TextLayoutOptions(Language: language, Overrun: overrun, Ellipsis: _ellipsis.Length == 0 ? "…" : _ellipsis,
                    VisibleCharacters: -1, BIDIOverride: _contexts, ApplyAlignment: false));
            _dirty = revision != _revision;
        }
        finally { _shaping = false; }
    }
    /// <inheritdoc />
    /// <exception cref="Exception">A font or structured-text hook fails.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        EnsureShaped(); var size = _layout.Size;
        if (_overrun != global::Electron2D.TextOverrunBehavior.NoTrimming) size.X = 0;
        return size;
    }
    private void DrawLink()
    {
        EnsureShaped(); if (_dirty) return;
        var mode = GetDrawMode(); var focused = HasFocus(true);
        var key = mode switch
        {
            DrawMode.Normal => focused ? "font_focus_color" : "font_color",
            DrawMode.Hover => "font_hover_color",
            DrawMode.Pressed or DrawMode.HoverPressed => HasThemeColor("font_pressed_color") ? "font_pressed_color" : "font_color",
            _ => "font_disabled_color"
        };
        var color = GetThemeColor(key);
        if (focused && GetThemeStyleBox("focus") is { } focus) DrawStyleBox(focus, new(Vector2.Zero, Size));
        var width = _layout.Size.X; var x = IsLayoutRTL() ? Size.X - width : 0;
        var baseline = new Vector2(x, _layout.FirstAscent);
        var outlineSize = GetThemeConstant("outline_size"); var outline = GetThemeColor("font_outline_color");
        if (outlineSize > 0 && outline.A > 0) _layout.Draw(this, baseline, outline, outlineSize, outlinePass: true);
        _layout.Draw(this, baseline, color);
        var underline = mode is DrawMode.Hover or DrawMode.Pressed or DrawMode.HoverPressed ? _underline != UnderlineMode.Never : _underline == UnderlineMode.Always;
        if (underline && width > 0)
        {
            var y = _layout.FirstAscent + GetThemeConstant("underline_spacing") + _font!.GetUnderlinePosition(_fontSize);
            DrawLine(new(x, y), new(x + width, y), color, Math.Max(1, _font.GetUnderlineThickness(_fontSize)));
        }
    }
    private void ResourceChanged(Resource resource)
    {
        if (IsDisposed || !ReferenceEquals(resource, _font)) return;
        var mutable = true;
        try { EnsureMutable(); } catch (ObjectDisposedException) { return; } catch (InvalidOperationException) { mutable = false; }
        if (mutable) { InvalidateLink(); return; }
        var tree = Volatile.Read(ref _resourceTree); var dispatch = Volatile.Read(ref _resourceDispatch);
        if (tree is null || dispatch is null) { _dirty = true; return; }
        if (Interlocked.Exchange(ref _resourcePending, 1) == 0)
            try { tree.Defer(dispatch); } catch (ObjectDisposedException) { Interlocked.Exchange(ref _resourcePending, 0); }
    }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    private void EnterResourceTree()
    {
        var tree = Tree!; var generation = ++_entryGeneration; _resourceTree = tree;
        _resourceDispatch = () =>
        {
            if (IsDisposed || !ReferenceEquals(Tree, tree) || _entryGeneration != generation) return;
            Interlocked.Exchange(ref _resourcePending, 0); InvalidateLink();
        };
        _translatedText = Atr(_text); SetButtonResourceProcessing(true); InvalidateLink();
    }
    /// <inheritdoc />
    /// <exception cref="Exception">Inherited, resource, layout or drawing callbacks fail.</exception>
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        HandleNotification();
        if (reversed) base.OnNotification(what);
        void HandleNotification()
        {
            if (what == NotificationDraw) { if (!reversed) base.OnNotification(what); DrawLink(); return; }
            List<Exception>? errors = null;
            try { if (!reversed) base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
            if (!IsDisposed)
                try
                {
                    switch (what)
                    {
                        case NotificationEnterTree: EnterResourceTree(); break;
                        case NotificationExitTree:
                            _entryGeneration++; _resourceTree = null; _resourceDispatch = null; Interlocked.Exchange(ref _resourcePending, 0); SetButtonResourceProcessing(false); break;
                        case NotificationThemeChanged:
                        case NotificationLayoutDirectionChanged: InvalidateLink(); break;
                        case NotificationTranslationChanged: _translatedText = Atr(_text); InvalidateLink(); if (!IsDisposed) UpdateConfigurationWarnings(); break;
                        case NotificationResized: if (_overrun != global::Electron2D.TextOverrunBehavior.NoTrimming) InvalidateLink(); break;
                        case NotificationInternalProcess:
                            PollFont();
                            if (_dirty || _minimumPending) { _minimumPending = false; QueueRedraw(); UpdateMinimumSize(); }
                            break;
                    }
                }
                catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Link notification callbacks failed.", errors);

        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            _resourceTree = null; _resourceDispatch = null; _entryGeneration++; _contexts.Clear();
        }
        base.Dispose(disposing);
    }
}
