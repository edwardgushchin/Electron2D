namespace Electron2D;

public partial class FoldableContainer
{
    private readonly List<Texture> _residentIcons = [];
    private readonly TextLayout _measure = new(), _drawText = new();
    private readonly List<Control> _arrangement = [];
    private readonly Stack<List<Control>> _titleSnapshots = [];
    private List<Control> TakeTitleSnapshot()
    {
        var snapshot = _titleSnapshots.Count == 0 ? new List<Control>() : _titleSnapshots.Pop();
        snapshot.AddRange(_titleControls); return snapshot;
    }
    private void ReturnTitleSnapshot(List<Control> snapshot) { snapshot.Clear(); _titleSnapshots.Push(snapshot); }
    private bool LiveTitleControl(Control control) => !control.IsDisposed && ReferenceEquals(control.Parent, this) && _titleControls.Contains(control) && control.Visible;
    private bool _titleDirty = true, _drawDirty = true, _arranging;
    private Vector2 _textSize;
    private float _drawWidth = -1;
    private Font? _font;
    private int _fontSize;
    private string _translated = "", _shapeLanguage = "";
    private HorizontalAlignment ActualAlignment => IsLayoutRTL() ? _alignment switch { HorizontalAlignment.Left => HorizontalAlignment.Right, HorizontalAlignment.Right => HorizontalAlignment.Left, _ => _alignment } : _alignment;
    private int Separation => Math.Max(0, GetThemeConstant("h_separation"));
    private StyleBox? BaseTitleStyle => GetThemeStyleBox(_folded ? "title_collapsed_panel" : "title_panel");
    private StyleBox? DrawingTitleStyle => _hovered ? GetThemeStyleBox(_folded ? "title_collapsed_hover_panel" : "title_hover_panel") : BaseTitleStyle;
    private Texture? TitleIcon => GetThemeIcon(!_folded ? _titlePosition == BarPosition.Top ? "expanded_arrow" : "expanded_arrow_mirrored" : IsLayoutRTL() ? "folded_arrow_mirrored" : "folded_arrow");
    private void ReleaseIcons()
    { foreach (var texture in _residentIcons) texture.ReleaseRendererCacheResidency(); _residentIcons.Clear(); }
    private void RetainIcons()
    {
        if (!IsInsideTree) return;
        foreach (var name in ArrowNames)
        {
            var icon = GetThemeIcon(name); if (icon is null || icon.IsDisposed || _residentIcons.Contains(icon)) continue;
            icon.AcquireRendererCacheResidency(); _residentIcons.Add(icon);
        }
    }
    private static readonly string[] ArrowNames = ["expanded_arrow", "expanded_arrow_mirrored", "folded_arrow", "folded_arrow_mirrored"];
    private void InvalidateTitle(bool shaping = true)
    {
        if (shaping) _titleDirty = _drawDirty = true; List<Exception>? errors = null;
        try { UpdateMinimumSize(); } catch (Exception error) { CollectException(ref errors, error); }
        try { QueueSort(); } catch (Exception error) { CollectException(ref errors, error); }
        try { QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Foldable layout invalidation failed.", errors);
    }
    private void MeasureTitle()
    {
        CheckFoldable(); if (!_titleDirty) return;
        _font = GetThemeFont("font"); _fontSize = GetThemeFontSize("font_size"); _translated = Atr(_title);
        _shapeLanguage = _language.Length != 0 ? _language : TranslationServer.GetOrAddDomain(TranslationDomain).LocaleOverride;
        if (_shapeLanguage.Length == 0) _shapeLanguage = TranslationServer.Culture.Name;
        if (_shapeLanguage.Length == 0) _shapeLanguage = TranslationServer.GetToolLocale();
        _textSize = Vector2.Zero;
        if (_font is not null && _fontSize > 0 && _translated.Length != 0)
        {
            _measure.Build(_font, TextKey(-1), new TextLayoutOptions(Language: _shapeLanguage, Overrun: 0)); _textSize = _measure.Size;
        }
        _titleDirty = false; _drawDirty = true;
    }
    private TextLayoutKey TextKey(float width) => new(_translated, _fontSize, width, ActualAlignment, -1, TextLineBreakFlags.None, TextJustificationFlags.None,
        _direction == global::Electron2D.TextDirection.Inherited ? IsLayoutRTL() ? global::Electron2D.TextDirection.RTL : global::Electron2D.TextDirection.LTR : _direction, TextOrientation.Horizontal);
    private Vector2 TitleMinimum()
    {
        MeasureTitle(); var style = BaseTitleStyle?.GetMinimumSize() ?? Vector2.Zero; var icon = TitleIcon?.GetSize() ?? Vector2.Zero;
        var result = style + new Vector2(icon.X, Math.Max(icon.Y, _title.Length == 0 ? 0 : _textSize.Y));
        if (_title.Length != 0) { result.X += Separation; if (_overrun == TextOverrunBehavior.NoTrimming) result.X += _textSize.X; }
        var count = 0; var height = 0f;
        var snapshot = TakeTitleSnapshot();
        try { foreach (var control in snapshot) if (LiveTitleControl(control)) { var size = control.GetBoundMinimumSize(); if (!LiveTitleControl(control)) continue; result.X += size.X; height = Math.Max(height, size.Y); count++; } }
        finally { ReturnTitleSnapshot(snapshot); }
        result.X += count * Separation; result.Y = Math.Max(result.Y, style.Y + height); return result.Max(Vector2.Zero);
    }
    private Rect2 TitleRect() { var title = TitleMinimum(); return new(new(0, _titlePosition == BarPosition.Top ? 0 : Size.Y - title.Y), new(Size.X, title.Y)); }
    private float TitleControlsWidth()
    {
        var width = 0f; var count = 0;
        var snapshot = TakeTitleSnapshot();
        try { foreach (var c in snapshot) if (LiveTitleControl(c)) { var size = c.GetBoundMinimumSize(); if (!LiveTitleControl(c)) continue; width += size.X; count++; } }
        finally { ReturnTitleSnapshot(snapshot); }
        return width + Math.Max(0, count - 1) * Separation;
    }
    private Vector2? ContentMaximum(Vector2 margins)
    {
        if (!PropagateMaximumSize) return null; var max = GetCombinedMaximumSize();
        return new Vector2(max.X < 0 ? -1 : Math.Max(0, max.X - margins.X), max.Y < 0 ? -1 : Math.Max(0, max.Y - margins.Y));
    }
    private Vector2 ContentSize(bool desired)
    {
        var title = TitleMinimum(); if (_folded) return title;
        var margins = GetThemeStyleBox("panel")?.GetMinimumSize() ?? Vector2.Zero; var maximum = ContentMaximum(margins + new Vector2(0, title.Y)); var body = Vector2.Zero;
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control c && Sortable(c, true)) { c.ContainerMaximum = maximum; body = body.Max(desired ? c.GetBoundDesiredSize() : c.GetBoundMinimumSize()); }
        body += margins; return new(Math.Max(title.X, body.X), title.Y + body.Y);
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => ContentSize(false);
    internal override Vector2 GetDesiredSize() => ContentSize(true);
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsHorizontal() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    /// <inheritdoc />
    protected override SizeFlags[] GetAllowedSizeFlagsVertical() => [SizeFlags.Fill, SizeFlags.ShrinkBegin, SizeFlags.ShrinkCenter, SizeFlags.ShrinkEnd];
    private void ArrangeFoldable()
    {
        if (_arranging) { QueueSort(); return; }
        if (!IsInsideTree || !IsVisibleInTree) return;
        _arranging = true; var expectedTree = Tree; List<Exception>? errors = null;
        try
        {
            var title = TitleMinimum(); var style = BaseTitleStyle; var left = style?.GetMargin(Side.Left) ?? 0; var right = style?.GetMargin(Side.Right) ?? 0;
            var x = IsLayoutRTL() ? left : Size.X - right - TitleControlsWidth();
            var center = title.Y * .5f + ((style?.GetMargin(Side.Top) ?? 0) - (style?.GetMargin(Side.Bottom) ?? 0)) * .5f;
            if (_titlePosition == BarPosition.Bottom) center = Size.Y - center;
            foreach (var c in _titleControls) if (!c.IsDisposed && c.Parent == this && c.Visible) _arrangement.Add(c);
            for (var i = 0; i < _arrangement.Count; i++)
            {
                var c = _arrangement[IsLayoutRTL() ? _arrangement.Count - 1 - i : i]; if (c.IsDisposed || c.Parent != this) continue;
                try { var size = c.GetBoundMinimumSize(); if (!LiveTitleControl(c) || IsDisposed || !ReferenceEquals(Tree, expectedTree)) continue; FitChildInRect(c, new(new(x, center - size.Y * .5f), size)); x += size.X + Separation; } catch (Exception error) { CollectException(ref errors, error); }
            }
            _arrangement.Clear();
            var panel = GetThemeStyleBox("panel"); var margins = panel?.GetMinimumSize() ?? Vector2.Zero; var offset = panel?.GetOffset() ?? Vector2.Zero;
            if (IsLayoutRTL()) offset.X = panel?.GetMargin(Side.Right) ?? 0; if (_titlePosition == BarPosition.Top) offset.Y += title.Y;
            var rect = new Rect2(offset, (Size - margins - new Vector2(0, title.Y)).Max(Vector2.Zero)); var maximum = ContentMaximum(margins + new Vector2(0, title.Y));
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control c && !c.IsDisposed && !c.TopLevel) _arrangement.Add(c);
            foreach (var c in _arrangement)
            {
                if (IsDisposed || !IsInsideTree || !ReferenceEquals(Tree, expectedTree)) break;
                if (c.IsDisposed || c.Parent != this || c.TopLevel) continue;
                try { c.Visible = !_folded; } catch (Exception error) { CollectException(ref errors, error); }
                if (!_folded && !c.IsDisposed && c.Parent == this) try { c.ContainerMaximum = maximum; FitChildInRect(c, rect); } catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        finally { _arrangement.Clear(); _arranging = false; }
        ThrowCollected("Foldable arrangement callbacks failed.", errors);
    }
    private void DrawFlippable(StyleBox? style, Rect2 rect)
    {
        if (style is null) return;
        if (_titlePosition == BarPosition.Top) { DrawStyleBox(style, rect); return; }
        var flipped = new Rect2(-rect.Position, rect.Size); DrawSetTransform(new(0, style.GetDrawRect(flipped).Size.Y), 0, new(1, -1));
        try { DrawStyleBox(style, flipped); } finally { DrawSetTransformMatrix(Transform.Identity); }
    }
    private void DrawFoldable()
    {
        MeasureTitle(); var rect = TitleRect(); var style = DrawingTitleStyle; DrawFlippable(style, rect);
        var icon = TitleIcon; var iconSize = icon?.GetSize() ?? Vector2.Zero; var left = style?.GetMargin(Side.Left) ?? 0; var right = style?.GetMargin(Side.Right) ?? 0;
        var top = style?.GetMargin(_titlePosition == BarPosition.Top ? Side.Top : Side.Bottom) ?? 0; var margins = style?.GetMinimumSize() ?? Vector2.Zero;
        var extra = TitleControlsWidth(); if (extra > 0) extra += Separation;
        var width = Size.X - margins.X - iconSize.X - Separation - extra;
        var iconPos = new Vector2(IsLayoutRTL() ? Size.X - right - iconSize.X : left, top + Math.Max(0, (rect.Size.Y - margins.Y - iconSize.Y) * .5f));
        if (icon is not null) DrawTextureRect(icon, new(rect.Position + iconPos, iconSize), false);
        if (_font is not null && _fontSize > 0 && _translated.Length != 0 && width > 0)
        {
            if (_drawDirty || _drawWidth != width) { _drawWidth = width; _drawText.Build(_font, TextKey(width), new TextLayoutOptions(Language: _shapeLanguage, Overrun: (int)_overrun)); _drawDirty = false; }
            var origin = rect.Position + new Vector2(left + (IsLayoutRTL() ? extra : iconSize.X + Separation), top + Math.Max(0, (rect.Size.Y - margins.Y - _drawText.Size.Y) * .5f) + _drawText.FirstAscent);
            var outline = GetThemeColor("font_outline_color"); var outlineSize = GetThemeConstant("outline_size");
            if (outlineSize > 0 && outline.A > 0) _drawText.Draw(this, origin, outline, outlineSize, outlinePass: true, clipToWidth: true);
            _drawText.Draw(this, origin, GetThemeColor(_hovered ? "hover_font_color" : _folded ? "collapsed_font_color" : "font_color"), clipToWidth: true);
        }
        if (!_folded) DrawFlippable(GetThemeStyleBox("panel"), new(new(0, _titlePosition == BarPosition.Top ? rect.Size.Y : 0), new(Size.X, Math.Max(0, Size.Y - rect.Size.Y))));
        if (HasFocus()) DrawFlippable(GetThemeStyleBox("focus"), _folded ? rect : new(Vector2.Zero, Size));
    }
    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point) => _folded ? TitleRect().HasPoint(point) : base.HasPoint(point);
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition) => TitleRect().HasPoint(atPosition) ? base.OnGetTooltip(atPosition) : "";
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion) { var hover = TitleRect().HasPoint(motion.Position); if (_hovered != hover) { _hovered = hover; QueueRedraw(); } return; }
        if (inputEvent.IsActionPressed("ui_accept", exactMatch: true) || inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouse && TitleRect().HasPoint(mouse.Position))
        { try { RequestFold(!_folded); } finally { AcceptEvent(); } }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                if (what == NotificationDraw) DrawFoldable();
                else if (what == NotificationSortChildren) ArrangeFoldable();
                else if (what is NotificationThemeChanged or NotificationTranslationChanged or NotificationLayoutDirectionChanged) { ReleaseIcons(); RetainIcons(); InvalidateTitle(); }
                else if (what == NotificationEnterTree) RetainIcons();
                else if (what == NotificationExitTree) ReleaseIcons();
                else if (what == NotificationResized) { _drawDirty = true; QueueRedraw(); }
                else if (what is NotificationFocusEnter or NotificationFocusExit) QueueRedraw();
                else if (what == NotificationMouseExit && _hovered) { _hovered = false; QueueRedraw(); }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Foldable notifications failed.", errors);
    }
}
