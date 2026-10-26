namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal static int ScrollInteger(float value) => (int)Math.Clamp((double)value, int.MinValue, int.MaxValue);
    internal static (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges) ScrollFromBars(HScrollBar? h, VScrollBar? v, Rect2 view, Vector2 step, bool horizontal = true, bool vertical = true, bool mirrorHorizontal = false)
    {
        var offset = new Vector2((float)(h?.Value ?? 0), (float)(v?.Value ?? 0));
        var minimum = new Vector2((float)(h?.MinValue ?? 0), (float)(v?.MinValue ?? 0));
        var maximum = new Vector2((float)(h is null ? 0 : Math.Max(h.MinValue, h.MaxValue - h.Page)), (float)(v is null ? 0 : Math.Max(v.MinValue, v.MaxValue - v.Page)));
        if (!horizontal) minimum.X = maximum.X = offset.X;
        if (!vertical) minimum.Y = maximum.Y = offset.Y;
        if (mirrorHorizontal) { offset.X = -offset.X; (minimum.X, maximum.X) = (-maximum.X, -minimum.X); }
        var axes = (byte)((horizontal && maximum.X > minimum.X ? 1 : 0) | (vertical && maximum.Y > minimum.Y ? 2 : 0));
        var ranges = (byte)((h is not null ? 4 : 0) | (v is not null ? 8 : 0));
        return (offset, minimum, maximum, step, view.Size, view, axes, ranges);
    }
    private static bool ScrollSource(Control c) => c is ScrollContainer or ItemList or Tree or TextEdit or RichTextLabel or LineEdit or TabBar or GraphEdit;
    private static (Vector2 Offset, Vector2 Minimum, Vector2 Maximum, Vector2 Step, Vector2 Page, Rect2 View, byte Axes, byte Ranges)? ScrollMetrics(Control c) => c switch
    {
        ScrollContainer source => source.AccessibilityScrollMetrics(),
        ItemList source => source.AccessibilityScrollMetrics(),
        Tree source => source.AccessibilityScrollMetrics(),
        TextEdit source => source.AccessibilityScrollMetrics(),
        RichTextLabel source => source.AccessibilityScrollMetrics(),
        LineEdit source => source.AccessibilityScrollMetrics(),
        TabBar source => source.AccessibilityScrollMetrics(),
        GraphEdit source => source.AccessibilityScrollMetrics(),
        _ => null,
    };
    internal static (Transform World, Rect2 View, Vector2 Offset, Vector2 Minimum, Vector2 Maximum, byte Ranges)? SourceScrollFrame(Element e)
    {
        if (e.Source is not Control c || c.IsDisposed || !c.IsInsideTree || ScrollMetrics(c) is not { } state) return null;
        if (!state.Offset.IsFinite() || !state.Minimum.IsFinite() || !state.Maximum.IsFinite()) return null;
        return (e.Transform * c.GetViewport()!.GetScreenTransform() * c.GetGlobalTransformWithCanvas(), state.View, state.Offset, state.Minimum, state.Maximum, state.Ranges);
    }
    private static void SetSourceScroll(Control c, Vector2 offset)
    {
        switch (c)
        {
            case ScrollContainer source: source.AccessibilitySetScrollOffset(offset); break;
            case ItemList source: source.AccessibilitySetScrollOffset(offset); break;
            case Tree source: source.AccessibilitySetScrollOffset(offset); break;
            case TextEdit source: source.AccessibilitySetScrollOffset(offset); break;
            case RichTextLabel source: source.AccessibilitySetScrollOffset(offset); break;
            case LineEdit source: source.AccessibilitySetScrollOffset(offset); break;
            case TabBar source: source.AccessibilitySetScrollOffset(offset); break;
            case GraphEdit source: source.AccessibilitySetScrollOffset(offset); break;
        }
    }
    private static void ScrollSourceBy(Control c, int horizontal, int vertical, AccessibilityScrollUnit unit)
    {
        if (c is TabBar tabs) { tabs.AccessibilityScrollBy(horizontal, unit); return; }
        if (c is TextEdit text) { text.AccessibilityScrollBy(horizontal, vertical, unit); return; }
        if (ScrollMetrics(c) is not { } state) return;
        if (!state.Offset.IsFinite() || !state.Step.IsFinite() || !state.Page.IsFinite()) return;
        var step = unit == AccessibilityScrollUnit.Item ? state.Step : state.Page;
        var x = Math.Clamp((double)state.Offset.X + horizontal * (double)step.X, -float.MaxValue, float.MaxValue);
        var y = Math.Clamp((double)state.Offset.Y + vertical * (double)step.Y, -float.MaxValue, float.MaxValue);
        SetSourceScroll(c, new Vector2((float)x, (float)y));
    }
    private static void ScrollSequential(Control c, int direction)
    {
        if (ScrollMetrics(c) is not { } state) return;
        ScrollSourceBy(c, (state.Axes & 2) == 0 ? direction : 0, (state.Axes & 2) != 0 ? direction : 0, AccessibilityScrollUnit.Item);
    }
    private static void BindSourceScroll(Element e, Control control)
    {
        if (!ScrollSource(control)) return;
        e.SourceScrollUpAction = unit => ScrollSourceBy(control, 0, -1, unit);
        e.SourceScrollDownAction = unit => ScrollSourceBy(control, 0, 1, unit);
        e.SourceScrollLeftAction = unit => ScrollSourceBy(control, -1, 0, unit);
        e.SourceScrollRightAction = unit => ScrollSourceBy(control, 1, 0, unit);
        e.SourceScrollBackwardAction = () => ScrollSequential(control, -1);
        e.SourceScrollForwardAction = () => ScrollSequential(control, 1);
        e.SourceSetScrollOffsetAction = offset => SetSourceScroll(control, offset);
    }
    internal static void UpdateSourceScroll(Element e, Control control)
    {
        if (ScrollMetrics(control) is not { } state) return;
        var first = !e.SourceScrollPrepared;
        if (!state.Offset.IsFinite() || !state.Minimum.IsFinite() || !state.Maximum.IsFinite())
        {
            state.Axes = 0; first = false;
            if (e.SourceScrollPrepared || e.ScrollMask != 0) { e.ScrollMask = 0; Runtime._native!.ScrollChanged(e, false); }
            e.SourceScrollPrepared = false;
        }
        else
        {
            var supported = (byte)((state.Ranges >> 2) | state.Axes);
            if ((state.Ranges & 4) != 0 && (first || state.Minimum.X != e.SourceScrollMinimum.X || state.Maximum.X != e.SourceScrollMaximum.X)) UpdateSetScrollXRange(e.ID, state.Minimum.X, state.Maximum.X);
            if ((state.Ranges & 8) != 0 && (first || state.Minimum.Y != e.SourceScrollMinimum.Y || state.Maximum.Y != e.SourceScrollMaximum.Y)) UpdateSetScrollYRange(e.ID, state.Minimum.Y, state.Maximum.Y);
            if ((supported & 1) != 0 && (first || state.Offset.X != e.SourceScrollOffset.X)) UpdateSetScrollX(e.ID, state.Offset.X);
            if ((supported & 2) != 0 && (first || state.Offset.Y != e.SourceScrollOffset.Y)) UpdateSetScrollY(e.ID, state.Offset.Y);
            e.SourceScrollOffset = state.Offset; e.SourceScrollMinimum = state.Minimum; e.SourceScrollMaximum = state.Maximum; e.SourceScrollPrepared = true;
        }
        if (first || state.Axes != e.SourceScrollAxes)
        {
            e.ScrollUnitActions ??= [];
            if ((state.Axes & 1) != 0) { e.ScrollUnitActions[AccessibilityAction.ScrollLeft] = e.SourceScrollLeftAction!; e.ScrollUnitActions[AccessibilityAction.ScrollRight] = e.SourceScrollRightAction!; }
            else { e.ScrollUnitActions.Remove(AccessibilityAction.ScrollLeft); e.ScrollUnitActions.Remove(AccessibilityAction.ScrollRight); }
            if ((state.Axes & 2) != 0) { e.ScrollUnitActions[AccessibilityAction.ScrollUp] = e.SourceScrollUpAction!; e.ScrollUnitActions[AccessibilityAction.ScrollDown] = e.SourceScrollDownAction!; }
            else { e.ScrollUnitActions.Remove(AccessibilityAction.ScrollUp); e.ScrollUnitActions.Remove(AccessibilityAction.ScrollDown); }
            if (state.Axes != 0)
            {
                e.Actions[AccessibilityAction.ScrollBackward] = e.SourceScrollBackwardAction!; e.Actions[AccessibilityAction.ScrollForward] = e.SourceScrollForwardAction!;
                e.SetScrollOffsetAction = e.SourceSetScrollOffsetAction;
            }
            else { e.Actions.Remove(AccessibilityAction.ScrollBackward); e.Actions.Remove(AccessibilityAction.ScrollForward); e.SetScrollOffsetAction = null; }
            e.SourceScrollAxes = state.Axes; Runtime._native!.ActionsChanged(e);
        }
    }
}
