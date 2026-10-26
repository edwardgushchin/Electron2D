namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Publishes the producer's current numeric value and enables its native Value interface.</summary>
    /// <param name="id">A live element.</param><param name="position">The actual value, including IEEE nonfinite values retained by the producer.</param>
    /// <remarks>This publishes state; it does not invoke the producer's SetValue callback.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetNumValue(RID id, double position)
    {
        var r = Require(); var e = r.Get(id);
        if (!e.HasNumericValue) { r._native!.EnableValue(e); e.HasNumericValue = true; r._native.ActionsChanged(e); }
        else if (e.NumericValue.Equals(position)) return;
        e.NumericValue = position; r._native!.NumericChanged(e, "CurrentValue", position);
    }
    /// <summary>Publishes finite, ordered numeric bounds without clamping the producer's value.</summary>
    /// <param name="id">A live element.</param><param name="min">Inclusive minimum.</param><param name="max">Inclusive maximum.</param>
    /// <exception cref="ArgumentOutOfRangeException">Bounds are nonfinite or reversed.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetNumRange(RID id, double min, double max)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || min > max) throw new ArgumentOutOfRangeException(nameof(min));
        var r = Require(); var e = r.Get(id);
        if (!e.NumericMinimum.Equals(min)) { e.NumericMinimum = min; if (e.HasNumericValue) r._native!.NumericChanged(e, "MinimumValue", min); }
        if (!e.NumericMaximum.Equals(max)) { e.NumericMaximum = max; if (e.HasNumericValue) r._native!.NumericChanged(e, "MaximumValue", max); }
    }
    /// <summary>Publishes the native minimum increment.</summary>
    /// <param name="id">A live element.</param><param name="step">Finite nonnegative increment; zero indicates continuous values.</param>
    /// <exception cref="ArgumentOutOfRangeException">Step is negative or nonfinite.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetNumStep(RID id, double step)
    {
        if (!double.IsFinite(step) || step < 0) throw new ArgumentOutOfRangeException(nameof(step));
        var r = Require(); var e = r.Get(id); if (e.NumericStep.Equals(step)) return;
        e.NumericStep = step; if (e.HasNumericValue) r._native!.NumericChanged(e, "MinimumIncrement", step);
    }
    /// <summary>Configures the large numeric step consumed by native page actions.</summary>
    /// <param name="id">A live element.</param><param name="jump">Finite nonnegative amount. Zero removes page actions.</param>
    /// <remarks>Positive jump plus a numeric SetValue callback exposes page-decrement and page-increment actions.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Jump is negative or nonfinite.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetNumJump(RID id, double jump)
    {
        if (!double.IsFinite(jump) || jump < 0) throw new ArgumentOutOfRangeException(nameof(jump));
        var r = Require(); var e = r.Get(id); if (e.NumericJump.Equals(jump)) return;
        e.NumericJump = jump; r._native!.ActionsChanged(e);
    }
    /// <summary>Registers a compiled numeric request callback for the native Value interface.</summary>
    /// <param name="id">A live element.</param><param name="action">SetValue only.</param><param name="callback">Borrowed callback invoked on the display owner with a finite requested value.</param>
    /// <remarks>The callback owns validation, mutation and publication of its actual result. It never crosses an unmanaged exception boundary.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Action requires a different callback signature.</exception>
    /// <exception cref="ArgumentNullException">Callback is null.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">The element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<double> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (action != AccessibilityAction.SetValue) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); e.SetValueAction = callback; r._native!.ActionsChanged(e);
    }
    internal static bool NumericEditable(Range range) => range is not (ProgressBar or TextureProgressBar or Slider { Editable: false } or SpinBox { Editable: false });
    internal static bool UpdateSourceNumeric(Element e, Range range)
    {
        var current = (range.Value, range.MinValue, range.MaxValue, range.Step > 0 ? range.Step : 1, range.Page);
        var old = e.SourceNumeric; var first = !e.SourceNumericPrepared;
        var changed = first || !current.Equals(old);
        if (first || !current.Item2.Equals(old.Minimum) || !current.Item3.Equals(old.Maximum)) UpdateSetNumRange(e.ID, current.Item2, current.Item3);
        if (first || !current.Item4.Equals(old.Step)) UpdateSetNumStep(e.ID, current.Item4);
        if (first || !current.Page.Equals(old.Jump)) UpdateSetNumJump(e.ID, current.Page);
        if (first || !current.Value.Equals(old.Value)) UpdateSetNumValue(e.ID, current.Value);
        var editable = NumericEditable(range);
        if (first || editable != e.SourceCanEdit)
        {
            e.SourceCanEdit = editable;
            if (editable) { e.Actions[AccessibilityAction.Decrement] = e.SourceDecrementAction!; e.Actions[AccessibilityAction.Increment] = e.SourceIncrementAction!; e.SetValueAction = e.SourceSetValueAction; }
            else { e.Actions.Remove(AccessibilityAction.Decrement); e.Actions.Remove(AccessibilityAction.Increment); e.SetValueAction = null; }
            Runtime._native!.ActionsChanged(e); changed = true;
        }
        e.SourceNumeric = current; e.SourceNumericPrepared = true; return changed;
    }
}
