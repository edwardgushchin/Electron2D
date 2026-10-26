namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Publishes the actual local x scroll offset without moving content or invoking callbacks.</summary>
    /// <param name="id">Live element.</param><param name="position">Finite offset in the producer's local scroll coordinate space.</param>
    /// <remarks>The producer owns mutation and publication. A published offset is not clamped to the declared range.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Position is nonfinite.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetScrollX(RID id, float position) => SetScrollPosition(id, position, true);
    /// <summary>Publishes finite ordered local x scroll bounds without changing the current offset.</summary>
    /// <param name="id">Live element.</param><param name="min">Inclusive minimum.</param><param name="max">Inclusive maximum.</param>
    /// <exception cref="ArgumentOutOfRangeException">Bounds are nonfinite or reversed.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetScrollXRange(RID id, float min, float max) => SetScrollRange(id, min, max, true);
    /// <summary>Publishes the actual local y scroll offset without moving content or invoking callbacks.</summary>
    /// <param name="id">Live element.</param><param name="position">Finite offset in the producer's local scroll coordinate space.</param>
    /// <remarks>The producer owns mutation and publication. A published offset is not clamped to the declared range.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Position is nonfinite.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetScrollY(RID id, float position) => SetScrollPosition(id, position, false);
    /// <summary>Publishes finite ordered local y scroll bounds without changing the current offset.</summary>
    /// <param name="id">Live element.</param><param name="min">Inclusive minimum.</param><param name="max">Inclusive maximum.</param>
    /// <exception cref="ArgumentOutOfRangeException">Bounds are nonfinite or reversed.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetScrollYRange(RID id, float min, float max) => SetScrollRange(id, min, max, false);
    private static void SetScrollPosition(RID id, float position, bool horizontal)
    {
        if (!float.IsFinite(position)) throw new ArgumentOutOfRangeException(nameof(position));
        var r = Require(); var e = r.Get(id); var mask = (byte)(horizontal ? 1 : 2);
        if ((e.ScrollMask & mask) != 0 && (horizontal ? e.ScrollOffset.X : e.ScrollOffset.Y).Equals(position)) return;
        e.ScrollMask |= mask; if (horizontal) e.ScrollOffset.X = position; else e.ScrollOffset.Y = position;
        r._native!.ScrollChanged(e, true);
    }
    private static void SetScrollRange(RID id, float min, float max, bool horizontal)
    {
        if (!float.IsFinite(min) || !float.IsFinite(max) || min > max) throw new ArgumentOutOfRangeException(nameof(min));
        var r = Require(); var e = r.Get(id); var mask = (byte)(horizontal ? 4 : 8); var first = (e.ScrollMask & mask) == 0;
        if (!first && (horizontal ? e.ScrollMinimum.X : e.ScrollMinimum.Y).Equals(min) && (horizontal ? e.ScrollMaximum.X : e.ScrollMaximum.Y).Equals(max)) return;
        e.ScrollMask |= mask;
        if (horizontal) { e.ScrollMinimum.X = min; e.ScrollMaximum.X = max; } else { e.ScrollMinimum.Y = min; e.ScrollMaximum.Y = max; }
        r._native!.ScrollChanged(e, false); if (first) r._native.ActionsChanged(e);
    }
    /// <summary>Registers a directional item/page scroll request.</summary>
    /// <param name="id">Live element.</param><param name="action">ScrollUp, ScrollDown, ScrollLeft or ScrollRight.</param>
    /// <param name="callback">Borrowed callback on the display owner. The producer owns mutation and publication.</param>
    /// <exception cref="ArgumentOutOfRangeException">The selector requires another payload signature.</exception>
    /// <exception cref="ArgumentNullException">Callback is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollUnit> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (action is not (AccessibilityAction.ScrollUp or AccessibilityAction.ScrollDown or AccessibilityAction.ScrollLeft or AccessibilityAction.ScrollRight)) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); (e.ScrollUnitActions ??= [])[action] = callback; r._native!.ActionsChanged(e);
    }
    /// <summary>Registers an element reveal request with a preferred viewport edge.</summary>
    /// <param name="id">Live element.</param><param name="action">ScrollIntoView only.</param>
    /// <param name="callback">Borrowed owner callback. Native anywhere placement may select a suitable supported edge.</param>
    /// <exception cref="ArgumentOutOfRangeException">The selector requires another payload signature.</exception>
    /// <exception cref="ArgumentNullException">Callback is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<AccessibilityScrollHint> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action != AccessibilityAction.ScrollIntoView) throw new ArgumentOutOfRangeException(nameof(action));
        var e = Require().Get(id); e.ScrollIntoViewAction = callback;
    }
    /// <summary>Registers a point-placement or absolute scroll-offset request.</summary>
    /// <param name="id">Live element.</param><param name="action">ScrollToPoint or SetScrollOffset.</param>
    /// <param name="callback">Borrowed owner callback. Points use window-relative logical pixels; offsets use the target's local scroll coordinates.</param>
    /// <remarks>Only finite points cross the native boundary. The callback owns clamping, mutation and publication.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector requires another payload signature.</exception>
    /// <exception cref="ArgumentNullException">Callback is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<Vector2> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action is not (AccessibilityAction.ScrollToPoint or AccessibilityAction.SetScrollOffset)) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); if (action == AccessibilityAction.ScrollToPoint) e.ScrollToPointAction = callback; else { e.SetScrollOffsetAction = callback; r._native!.ActionsChanged(e); }
    }
    private static void ClearScroll(Element e)
    {
        e.ScrollUnitActions?.Clear(); e.ScrollUnitActions = null; e.ScrollIntoViewAction = null; e.ScrollToPointAction = e.SetScrollOffsetAction = e.SourceSetScrollOffsetAction = null;
        e.SourceScrollUpAction = e.SourceScrollDownAction = e.SourceScrollLeftAction = e.SourceScrollRightAction = null; e.SourceScrollBackwardAction = e.SourceScrollForwardAction = null;
    }
}
