namespace Electron2D;

public partial class Button
{
    private void ReadTheme()
    {
        if (!_themeDirty) return;
        var rtl = IsLayoutRTL();
        _styles[0] = ResolveStyle("normal", "normal_mirrored", rtl);
        _styles[1] = ResolveStyle("pressed", "pressed_mirrored", rtl);
        _styles[2] = ResolveStyle("hover", "hover_mirrored", rtl);
        _styles[3] = ResolveStyle("disabled", "disabled_mirrored", rtl);
        _styles[4] = HasThemeStyleBox("hover_pressed") ? ResolveStyle("hover_pressed", "hover_pressed_mirrored", rtl) : _styles[1];
        _styles[5] = GetThemeStyleBox("focus");
        _marginLeft = _marginRight = _marginTop = _marginBottom = 0; _largestStyleSize = Vector2.Zero;
        for (var i = 0; i < 5; i++)
        {
            var style = _styles[i] ?? throw new InvalidOperationException("A button requires its state styles.");
            _largestStyleSize = _largestStyleSize.Max(style.GetMinimumSize());
            _marginLeft = Math.Max(_marginLeft, style.GetMargin(Side.Left)); _marginRight = Math.Max(_marginRight, style.GetMargin(Side.Right));
            _marginTop = Math.Max(_marginTop, style.GetMargin(Side.Top)); _marginBottom = Math.Max(_marginBottom, style.GetMargin(Side.Bottom));
        }
        _largestStyleSize = _largestStyleSize.Max(new Vector2(_marginLeft + _marginRight, _marginTop + _marginBottom));
        _alignToLargestStyleBox = GetThemeConstant("align_to_largest_stylebox") != 0;
        _fontSize = GetThemeFontSize("font_size"); _lineSpacing = GetThemeConstant("line_spacing");
        _separation = Math.Max(0, GetThemeConstant("h_separation")); _iconMaxWidth = GetThemeConstant("icon_max_width");
        _outlineSize = GetThemeConstant("outline_size");
        _themeDirty = false; _dirty = true;
    }
    private StyleBox? ResolveStyle(string normal, string mirrored, bool rtl) => GetThemeStyleBox(rtl && HasThemeStyleBox(mirrored) ? mirrored : normal);
    internal StyleBox CurrentButtonStyle { get { ReadTheme(); return _styles[(int)GetDrawMode()]!; } }
    internal Vector2 LargestButtonStyleSize { get { ReadTheme(); return _largestStyleSize; } }
    internal int ButtonSeparation { get { ReadTheme(); return _separation; } }
    internal Vector2 FitButtonIcon(Vector2 size)
    {
        ReadTheme(); return _iconMaxWidth > 0 && size.X > _iconMaxWidth ? new(_iconMaxWidth, size.Y * _iconMaxWidth / size.X) : size;
    }
    internal virtual void RefreshButtonIndicator() { _internalLeft = _internalRight = 0; }
    internal void SetButtonInternalMargins(float left, float right) { _internalLeft = left; _internalRight = right; }
    private Texture? EffectiveIcon() => _icon is { IsDisposed: false } icon ? icon : HasThemeIcon("icon") ? GetThemeIcon("icon") : null;
    private void CheckFont()
    {
        var font = GetThemeFont("font") ?? throw new InvalidOperationException("A button requires its theme font.");
        if (font.IsDisposed) throw new ObjectDisposedException(nameof(Font));
        if (!ReferenceEquals(_font, font))
        {
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            _font = font; font.Changed += _resourceChanged; font.Disposed += _resourceDisposed; MarkPolledResourceChange();
        }
        var generation = font.GetContentGeneration();
        if (_fontGeneration != generation) { _fontGeneration = generation; MarkPolledResourceChange(); }
    }
    private void PollIcon()
    {
        if (_icon?.IsDisposed == true) Icon = null;
        var changed = false; var count = 0;
        try { PollButtonTextures(ref count, ref changed); }
        catch { ReleaseButtonTextureResidency(); throw; }
        if (count < _textureStates.Count) { _textureStates.RemoveRange(count, _textureStates.Count - count); changed = true; }
        try { SyncButtonTextureResidency(); } catch { ReleaseButtonTextureResidency(); throw; }
        if (changed) MarkPolledResourceChange();
        if (_icon?.IsDisposed == true) Icon = null;
    }
    internal virtual void PollButtonTextures(ref int count, ref bool changed)
    {
        PollButtonTexture(EffectiveIcon(), ref count, ref changed);
        foreach (var style in _styles) if (style is StyleBoxTexture textured) PollButtonTexture(textured.Texture, ref count, ref changed);
    }
    internal void PollButtonTexture(Texture? texture, ref int count, ref bool changed)
    {
        var start = count;
        while (texture is not null)
        {
            for (var index = start; index < count; index++)
                if (ReferenceEquals(_textureStates[index].Texture, texture)) { changed = true; return; }
            var state = new ButtonTextureState(texture, texture.ChangeRevision, texture.IsDisposed);
            if (count < _textureStates.Count)
            {
                var previous = _textureStates[count];
                if (!ReferenceEquals(previous.Texture, state.Texture) || previous.Revision != state.Revision || previous.Disposed != state.Disposed)
                { _textureStates[count] = state; changed = true; }
            }
            else { _textureStates.Add(state); changed = true; }
            count++;
            if (state.Disposed || texture is not AtlasTexture atlas) return;
            try { texture = atlas.Atlas; } catch (ObjectDisposedException) { changed = true; return; }
        }
    }
    private void SyncButtonTextureResidency()
    {
        if (!_residencyAttached) { ReleaseButtonTextureResidency(); return; }
        _residentTextures.EnsureCapacity(_textureStates.Count);
        for (var i = _residentTextures.Count - 1; i >= 0; i--)
        {
            var texture = _residentTextures[i]; var retained = false;
            foreach (var state in _textureStates) if (!state.Disposed && ReferenceEquals(state.Texture, texture)) { retained = true; break; }
            if (!retained) { texture.ReleaseRendererCacheResidency(); _residentTextures.RemoveAt(i); }
        }
        foreach (var state in _textureStates)
        {
            if (state.Disposed) continue;
            var retained = false;
            foreach (var texture in _residentTextures) if (ReferenceEquals(texture, state.Texture)) { retained = true; break; }
            if (retained) continue;
            state.Texture.AcquireRendererCacheResidency(); _residentTextures.Add(state.Texture);
        }
    }
    private void ReleaseButtonTextureResidency()
    {
        foreach (var texture in _residentTextures) texture.ReleaseRendererCacheResidency();
        _residentTextures.Clear();
    }
    private void MarkPolledResourceChange() { _revision++; _minimumPending = true; _dirty = true; InvalidateCanvas(); }
    private void EnsureShaped()
    {
        CheckButton(); if (_shaping) return;
        ReadTheme(); CheckFont(); PollIcon(); if (!_dirty) return;
        var revision = _revision; _shaping = true;
        try
        {
            var flags = _autowrapMode switch
            {
                TextAutowrapMode.Arbitrary => TextLineBreakFlags.GraphemeBound,
                TextAutowrapMode.Word => TextLineBreakFlags.WordBound,
                TextAutowrapMode.WordSmart => TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive,
                _ => TextLineBreakFlags.None
            };
            flags |= TextLineBreakFlags.Mandatory | _autowrapTrimFlags;
            var direction = _textDirection == global::Electron2D.TextDirection.Inherited
                ? IsLayoutRTL() ? global::Electron2D.TextDirection.RTL : global::Electron2D.TextDirection.LTR
                : (int)_textDirection < 0 ? global::Electron2D.TextDirection.Auto : _textDirection;
            var language = _language;
            if (language.Length == 0) language = TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
            if (language.Length == 0) language = TranslationServer.Culture.Name;
            if (language.Length == 0) language = TranslationServer.GetToolLocale();
            var overrun = (int)_textOverrunBehavior is >= 0 and <= 6 ? (int)_textOverrunBehavior : 0;
            _layout.Build(_font!, new(_translatedText, _fontSize, _layoutWidth, _layoutAlignment, -1, flags,
                TextJustificationFlags.Kashida | TextJustificationFlags.WordBound | TextJustificationFlags.SkipLastLine | TextJustificationFlags.DoNotSkipSingleLine,
                direction, TextOrientation.Horizontal, true),
                new TextLayoutOptions(LineSpacing: _lineSpacing, Language: language, Overrun: overrun, VisibleCharacters: -1,
                    WrappedBehavior: _autowrapMode != TextAutowrapMode.Off));
            _dirty = revision != _revision;
        }
        finally { _shaping = false; }
    }

    /// <inheritdoc />
    /// <exception cref="Exception">A borrowed resource or text-layout operation fails.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        EnsureShaped();
        var size = _layout.Size; var icon = EffectiveIcon();
        if (_clipText || _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming || _autowrapMode != TextAutowrapMode.Off) size.X = 0;
        if (!_expandIcon && icon is not null)
        {
            var iconSize = FitButtonIcon(icon.GetSize());
            size.Y = _verticalIconAlignment == VerticalAlignment.Center ? Math.Max(size.Y, iconSize.Y) : size.Y + iconSize.Y;
            if (_iconAlignment != HorizontalAlignment.Center)
            {
                size.X += iconSize.X; if (_translatedText.Length != 0) size.X += _separation;
            }
            else size.X = Math.Max(size.X, iconSize.X);
        }
        if (_translatedText.Length != 0)
        {
            var height = _font!.GetHeight(_fontSize);
            size.Y = _verticalIconAlignment == VerticalAlignment.Center ? Math.Max(height, size.Y) : size.Y + height;
        }
        return size + (_alignToLargestStyleBox ? _largestStyleSize : CurrentButtonStyle.GetMinimumSize());
    }
    private static HorizontalAlignment Mirrored(HorizontalAlignment alignment, bool rtl) => !rtl ? alignment : alignment switch
    {
        HorizontalAlignment.Left => HorizontalAlignment.Right,
        HorizontalAlignment.Right => HorizontalAlignment.Left,
        _ => alignment
    };
    private Color TextColor(DrawMode mode, bool focused) => mode switch
    {
        DrawMode.Normal => GetThemeColor(focused ? "font_focus_color" : "font_color"),
        DrawMode.Pressed => GetThemeColor(HasThemeColor("font_pressed_color") ? "font_pressed_color" : "font_color"),
        DrawMode.Hover => GetThemeColor("font_hover_color"),
        DrawMode.HoverPressed => GetThemeColor("font_hover_pressed_color"),
        _ => GetThemeColor("font_disabled_color")
    };
    private Color IconColor(DrawMode mode, bool focused)
    {
        var key = mode switch
        {
            DrawMode.Normal => focused ? "icon_focus_color" : "icon_normal_color",
            DrawMode.Pressed => "icon_pressed_color",
            DrawMode.Hover => "icon_hover_color",
            DrawMode.HoverPressed => "icon_hover_pressed_color",
            _ => "icon_disabled_color"
        };
        return HasThemeColor(key) ? GetThemeColor(key) : mode == DrawMode.Disabled ? new(1, 1, 1, .4f) : Colors.White;
    }
    private void DrawButton()
    {
        EnsureShaped(); RefreshButtonIndicator();
        var style = CurrentButtonStyle; var size = Size; var mode = GetDrawMode(); var focused = HasFocus(true);
        if (!_flat) DrawStyleBox(style, new(Vector2.Zero, size));
        if (focused && _styles[5] is { } focus) DrawStyleBox(focus, new(Vector2.Zero, size));
        var icon = EffectiveIcon(); if (_translatedText.Length == 0 && icon is null) return;
        var left = _alignToLargestStyleBox ? _marginLeft : style.GetMargin(Side.Left);
        var right = _alignToLargestStyleBox ? _marginRight : style.GetMargin(Side.Right);
        var top = _alignToLargestStyleBox ? _marginTop : style.GetMargin(Side.Top);
        var bottom = _alignToLargestStyleBox ? _marginBottom : style.GetMargin(Side.Bottom);
        var internalLeft = _internalLeft + (_internalLeft > 0 ? _separation : 0);
        var internalRight = _internalRight + (_internalRight > 0 ? _separation : 0);
        var available = size - new Vector2(left + right + internalLeft + internalRight, top + bottom);
        var custom = available; var rtl = IsLayoutRTL(); var iconAlignment = Mirrored(_iconAlignment, rtl); var alignment = Mirrored(_alignment, rtl);
        if (icon is not null)
        {
            var iconSize = icon.GetSize();
            if (_expandIcon)
            {
                var room = custom;
                var clipped = _clipText || _textOverrunBehavior != global::Electron2D.TextOverrunBehavior.NoTrimming || _autowrapMode != TextAutowrapMode.Off;
                if (!clipped && iconAlignment != HorizontalAlignment.Center && _layout.Size.X > 0) room.X -= _layout.Size.X + _separation;
                if (_verticalIconAlignment != VerticalAlignment.Center) room.Y -= _layout.Size.Y;
                if (iconSize.X == 0 || iconSize.Y == 0) iconSize = Vector2.Zero;
                else
                {
                    var width = iconSize.X * room.Y / iconSize.Y; var height = room.Y;
                    if (width > room.X) { width = room.X; height = iconSize.Y * width / iconSize.X; }
                    iconSize = new(width, height);
                }
            }
            iconSize = FitButtonIcon(iconSize).Round();
            if (!iconSize.IsFinite()) throw new InvalidOperationException("Button icon geometry exceeds finite coordinates.");
            if (iconSize.X > 0)
            {
                var x = iconAlignment switch
                {
                    HorizontalAlignment.Center => (custom.X - iconSize.X) / 2 + left + internalLeft,
                    HorizontalAlignment.Left or HorizontalAlignment.Fill => left + internalLeft,
                    HorizontalAlignment.Right => size.X - right - internalRight - iconSize.X,
                    _ => 0
                };
                var y = _verticalIconAlignment switch
                {
                    VerticalAlignment.Center => (custom.Y - iconSize.Y) / 2 + top,
                    VerticalAlignment.Top or VerticalAlignment.Fill => top,
                    VerticalAlignment.Bottom => size.Y - bottom - iconSize.Y,
                    _ => 0
                };
                DrawTextureRect(icon, new(new Vector2(x, y).Floor(), iconSize), false, IconColor(mode, focused));
            }
            if (_translatedText.Length != 0)
            {
                if (iconAlignment != HorizontalAlignment.Center) available.X -= iconSize.X + _separation;
                if (_verticalIconAlignment != VerticalAlignment.Center) available.Y -= iconSize.Y;
            }
        }
        if (_translatedText.Length == 0) return;
        var widthToDraw = MathF.Ceiling(Math.Max(1, available.X));
        if (!float.IsFinite(widthToDraw)) throw new InvalidOperationException("Button text width exceeds finite coordinates.");
        var oldWidth = _layoutWidth;
        if (_layoutWidth != widthToDraw || _layoutAlignment != alignment)
        {
            _layoutWidth = widthToDraw; _layoutAlignment = alignment; _dirty = true; EnsureShaped();
            if (_autowrapMode != TextAutowrapMode.Off && oldWidth != widthToDraw) UpdateMinimumSize();
        }
        var offset = Vector2.Zero;
        if (alignment is >= HorizontalAlignment.Left and <= HorizontalAlignment.Fill)
        {
            offset.X = left + internalLeft;
            if (alignment == HorizontalAlignment.Center) offset.X += (available.X - widthToDraw) / 2;
            if (iconAlignment == HorizontalAlignment.Left) offset.X += custom.X - available.X;
        }
        offset.Y = (available.Y - _layout.Size.Y) / 2 + top;
        if (_verticalIconAlignment == VerticalAlignment.Top) offset.Y += custom.Y - available.Y;
        var baseline = offset + new Vector2(0, _layout.FirstAscent);
        var outline = GetThemeColor("font_outline_color");
        if (_outlineSize > 0 && outline.A > 0) _layout.Draw(this, baseline, outline, _outlineSize, outlinePass: true, clipToWidth: true);
        _layout.Draw(this, baseline, TextColor(mode, focused), clipToWidth: true);
    }
    private void ResourceChanged(Resource resource)
    {
        if (IsDisposed || !ReferenceEquals(resource, _icon) && !ReferenceEquals(resource, _font)) return;
        var mutable = true;
        try { EnsureMutable(); } catch (ObjectDisposedException) { return; } catch (InvalidOperationException) { mutable = false; }
        if (mutable) { ReleaseButtonTextureResidency(); Invalidate(); return; }
        var tree = Volatile.Read(ref _resourceTree); var dispatch = Volatile.Read(ref _resourceDispatch);
        if (tree is null || dispatch is null) { _dirty = true; return; }
        if (Interlocked.Exchange(ref _resourcePending, 1) == 0)
            try { tree.Defer(dispatch); } catch (ObjectDisposedException) { Interlocked.Exchange(ref _resourcePending, 0); }
    }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    private void EnterResourceTree()
    {
        var tree = Tree!; var generation = ++_entryGeneration; _resourceTree = tree; _residencyAttached = true;
        _resourceDispatch = () =>
        {
            if (IsDisposed || !ReferenceEquals(Tree, tree) || _entryGeneration != generation) return;
            Interlocked.Exchange(ref _resourcePending, 0); ReleaseButtonTextureResidency(); Invalidate();
        };
        _translatedText = TranslateButtonText(_text); SetButtonResourceProcessing(true); _themeDirty = true; Invalidate();
    }
    /// <inheritdoc />
    /// <exception cref="Exception">A resource, layout, drawing or inherited notification callback fails.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawButton(); return; }
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationEnterTree: EnterResourceTree(); break;
                    case NotificationExitTree:
                        _residencyAttached = false; ReleaseButtonTextureResidency();
                        _entryGeneration++; _resourceTree = null; _resourceDispatch = null; Interlocked.Exchange(ref _resourcePending, 0);
                        SetButtonResourceProcessing(false); break;
                    case NotificationThemeChanged:
                    case NotificationLayoutDirectionChanged:
                        ReleaseButtonTextureResidency(); _themeDirty = true; Invalidate(); break;
                    case NotificationTranslationChanged: _translatedText = TranslateButtonText(_text); Invalidate(); break;
                    case NotificationResized: if (_autowrapMode != TextAutowrapMode.Off) Invalidate(); break;
                    case NotificationInternalProcess:
                        ReadTheme(); CheckFont(); PollIcon();
                        if (_dirty || _minimumPending) { _minimumPending = false; QueueRedraw(); UpdateMinimumSize(); }
                        break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Button notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _residencyAttached = false; ReleaseButtonTextureResidency();
            if (_icon is not null) { _icon.Changed -= _resourceChanged; _icon.Disposed -= _resourceDisposed; }
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            _resourceTree = null; _resourceDispatch = null; _entryGeneration++;
            _textureStates.Clear();
        }
        base.Dispose(disposing);
    }
}
