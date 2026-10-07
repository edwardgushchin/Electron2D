namespace Electron2D;

public partial class RichTextLabel
{
    private RichTextMetadata? _pressedMeta;
    private Vector2 _selectionPointer;
    private bool _dragCandidate, _dragStarted;
    private (Paragraph? Paragraph, int Local, int Global, Format? Style) HitRich(Vector2 point, bool nearest = false)
    {
        // ponytail: loaded scalar-bound scan; retain a per-line interval index if long documents make pointer motion material.
        EnsureRichLayout(); var origin = ContentOrigin; for (var p = _drawParagraphs.Count - 1; p >= 0; p--) { var paragraph = _drawParagraphs[p]; var local = point - origin - paragraph.Origin - new Vector2(0, paragraph.Y); var format = paragraph.Parts.Count > 0 ? paragraph.Parts[0].Format : paragraph.Format; local.X -= format.Indent * _tabSize * _themeValues!.Fonts[0].GetCharSize(' ', _themeValues.Sizes[0]).X; if (local.Y < 0 || local.Y >= paragraph.Height || local.X < 0 || local.X > paragraph.Width) continue; var column = Math.Clamp(paragraph.Layout.HitCharacter(local), 0, paragraph.Styles.Length); Format? style = null; for (var at = 0; at < paragraph.Styles.Length; at++) if (paragraph.Layout.GetCharacterBounds(at).HasPoint(local)) { style = paragraph.Styles[at]; break; } return (paragraph, column, paragraph.Offsets[column], style); }
        if (nearest) { Paragraph? best = null; var distance = float.PositiveInfinity; foreach (var paragraph in _drawParagraphs) { var local = point - origin - paragraph.Origin - new Vector2(0, paragraph.Y); var gap = local.Y < 0 ? -local.Y : local.Y > paragraph.Height ? local.Y - paragraph.Height : 0; if (gap < distance) { distance = gap; best = paragraph; } } if (best != null) { var local = point - origin - best.Origin - new Vector2(0, best.Y); var column = Math.Clamp(best.Layout.HitCharacter(local), 0, best.Styles.Length); return (best, column, best.Offsets[column], null); } }
        return (null, 0, -1, null);
    }
    /// <summary>Executes a built-in rich-text menu action.</summary><param name="option">Copy or SelectAll.</param>
    public void MenuOption(MenuItems option) { MutableRich(); switch (option) { case MenuItems.Copy: DisplayServer.Service?.ClipboardSetCore(GetSelectedText()); break; case MenuItems.SelectAll: SelectAll(); break; default: throw new ArgumentOutOfRangeException(nameof(option)); } }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion) { _selectionPointer = motion.Position; var hit = HitRich(motion.Position, _selecting); var meta = hit.Style?.Meta; _hoverHint = hit.Style?.MetaTooltip.Length > 0 ? hit.Style.MetaTooltip : hit.Style?.Hint ?? ""; if (hit.Paragraph != null) { var point = motion.Position - ContentOrigin - hit.Paragraph.Origin - new Vector2(0, hit.Paragraph.Y); foreach (var part in hit.Paragraph.Parts) if (part.Kind == PartKind.Image && part.Rect.HasPoint(point) && part.Tooltip.Length > 0) { _hoverHint = part.Tooltip; break; } } if (!ReferenceEquals(meta, _hoverMeta)) { var old = _hoverMeta; _hoverMeta = meta; List<Exception>? errors = null; if (old != null) try { MetaHoverEnded?.Invoke(old); } catch (Exception error) { CollectException(ref errors, error); } if (meta != null && !IsDisposed) try { MetaHoverStarted?.Invoke(meta); } catch (Exception error) { CollectException(ref errors, error); } if (!IsDisposed) QueueRedraw(); ThrowCollected("Rich-text hover callbacks failed.", errors); } if (IsDisposed) return; if (_selecting && _selectionEnabled && hit.Global >= 0) { _selectionFrom = Math.Min(_selectionAnchor, hit.Global); _selectionTo = Math.Max(_selectionAnchor, hit.Global); QueueRedraw(); AcceptEvent(); } return; }
        if (inputEvent is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp || mouse.ButtonIndex == MouseButton.WheelDown) { if (mouse.Pressed && _scrollActive) { EnsureRichLayout(); _scroll.Value += mouse.ButtonIndex == MouseButton.WheelUp ? -_themeValues!.Fonts[0].GetHeight(_themeValues.Sizes[0]) * 3 : _themeValues!.Fonts[0].GetHeight(_themeValues.Sizes[0]) * 3; AcceptEvent(); } return; }
            if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed && _contextMenu) { _menu.SetItemDisabled(0, _selectionFrom < 0 || _selectionFrom == _selectionTo); _menu.SetItemDisabled(1, !_selectionEnabled); _menu.Position = (Vector2i)(GetGlobalTransformWithCanvas() * mouse.Position); _menu.Popup(); AcceptEvent(); return; }
            if (mouse.ButtonIndex == MouseButton.Left) { var hit = HitRich(mouse.Position); if (mouse.Pressed) { if (_selectionEnabled && hit.Global >= 0) { GrabFocus(); _selectionPointer = mouse.Position; _dragStarted = false; _dragCandidate = !mouse.DoubleClick && _dragSelection && hit.Global >= _selectionFrom && hit.Global < _selectionTo; if (!_dragCandidate) { _selectionAnchor = hit.Global; _selectionFrom = _selectionTo = hit.Global; _selecting = true; if (mouse.DoubleClick && hit.Paragraph != null) { var range = hit.Paragraph.Layout.WordAt(hit.Local); _selectionFrom = hit.Paragraph.Offsets[range.From]; _selectionTo = hit.Paragraph.Offsets[range.To]; } _selecting = true; } else _selecting = false; QueueRedraw(); AcceptEvent(); } _pressedMeta = hit.Style?.Meta; } else { _selecting = false; if (_dragCandidate && !_dragStarted) Deselect(); _dragCandidate = _dragStarted = false; var meta = _pressedMeta; _pressedMeta = null; if (meta != null && ReferenceEquals(meta, hit.Style?.Meta) && _selectionFrom == _selectionTo) { MetaClicked?.Invoke(meta); if (!IsDisposed) AcceptEvent(); } } }
            return;
        }
        if (inputEvent is InputEventKey { Pressed: true } key && _shortcutKeys) { bool Action(string name) => InputMap.HasAction(name) && key.IsActionPressed(name, true, true); if (Action("ui_copy")) { MenuOption(MenuItems.Copy); AcceptEvent(); return; } if (Action("ui_text_select_all")) { SelectAll(); AcceptEvent(); return; } if (Action("ui_cancel")) { Deselect(); AcceptEvent(); return; } if (Action("ui_page_down") || Action("ui_page_up")) { EnsureRichLayout(); _scroll.Value += Action("ui_page_down") ? _textRect.Size.Y : -_textRect.Size.Y; AcceptEvent(); } }
    }
    /// <inheritdoc />
    protected override CursorShape OnGetCursorShape(Vector2 atPosition) => _hoverMeta != null ? CursorShape.PointingHand : _selectionEnabled ? CursorShape.IBeam : base.OnGetCursorShape(atPosition);
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition) => _hoverHint.Length > 0 ? _hoverHint : base.OnGetTooltip(atPosition);
    /// <inheritdoc />
    protected override DragPayload? OnGetDragData(Vector2 atPosition) { if (!_dragSelection || !_selectionEnabled || _selectionFrom < 0 || _selectionTo <= _selectionFrom) return null; var hit = HitRich(atPosition); if (hit.Global < _selectionFrom || hit.Global >= _selectionTo) return null; _dragStarted = true; _selecting = false; return new DragPayload<string>(GetSelectedText()); }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { EnsureRichLayout(); DrawRichDocument(); return; }
        base.OnNotification(what); if (IsDisposed) return; switch (what) { case NotificationMouseExit: var old = _hoverMeta; _hoverMeta = null; _hoverHint = ""; if (old != null) MetaHoverEnded?.Invoke(old); if (!IsDisposed) QueueRedraw(); break; case NotificationEnterTree: SetInternalProcessing(true, false); break; case NotificationInternalProcess: _elapsed += ProcessDeltaTime; ObserveWorker(); if (_selecting && _scrollActive && _selectionPointer.Y < _textRect.Position.Y || _selecting && _scrollActive && _selectionPointer.Y > _textRect.End.Y) { var distance = _selectionPointer.Y < _textRect.Position.Y ? _selectionPointer.Y - _textRect.Position.Y : _selectionPointer.Y - _textRect.End.Y; _scroll.Value += distance * ProcessDeltaTime * 10; var hit = HitRich(_selectionPointer, true); if (hit.Global >= 0) { _selectionFrom = Math.Min(_selectionAnchor, hit.Global); _selectionTo = Math.Max(_selectionAnchor, hit.Global); QueueRedraw(); } } if (_hasFX || !_finished) QueueRedraw(); break; case NotificationThemeChanged: case NotificationResized: case NotificationLayoutDirectionChanged: _dirty = true; QueueRedraw(); break; case NotificationTranslationChanged: if (!_externalStack) { if (_bbcode) ParseBBCode(Atr(_text)); else { Clear(); AddText(Atr(_text)); } _externalStack = false; } break; case NotificationFocusExit: if (_deselectFocus && !_menu.Visible) Deselect(); break; }
    }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() { if (!_fitContent) return Vector2.Zero; if (_building || _publishing) return _content; EnsureRichLayout(); var margin = _themeValues?.Normal?.GetMinimumSize() ?? Vector2.Zero; return _content + margin; }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(RichTextLabel) ? CreateRich : base.CreateSceneInstanceFactory();
    private static Node CreateRich() => new RichTextLabel();
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_drawing || _building || _publishing) throw new InvalidOperationException("Active rich-text layout cannot dispose its owner."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposingRich = true; StopWorker(); Finished = null; MetaClicked = MetaHoverStarted = MetaHoverEnded = null; _effects = []; ReleaseFX(); _stack.Clear(); _drawParagraphs.Clear(); _lines.Clear(); _fontVersions.Clear(); _main.Paragraphs.Clear(); _renderRoot.Paragraphs.Clear(); } base.Dispose(disposing); }
}
