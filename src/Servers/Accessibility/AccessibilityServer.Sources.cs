namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal static Control FocusSource(Control control) => control is SpinBox spin ? spin.GetLineEdit() : control;
    internal static Element? TryGetSource(RID id, Node node) => Runtime._elements.TryGetValue(id, out var e) && !e.Retired && e.Source == node ? e : null;
    internal static Element? SourceElement(Node node) => TryGetSource(node.AccessibilityIdentity, node);
    internal static Element BindSource(RID id, Node node)
    {
        var e = Runtime.Get(id); if (e.Source is not null && e.Source != node) throw new InvalidOperationException("An accessibility element already has a scene producer.");
        e.Source = node;
        if (node is Control control)
        {
            BindSourceScroll(e, control);
            e.SourceShowTooltipAction = control.ShowAccessibilityTooltip; e.SourceHideTooltipAction = control.HideAccessibilityTooltip;
            e.SourceContextMenuAction = control switch { LineEdit line => line.AccessibilityShowContextMenu, TextEdit edit => edit.AccessibilityShowContextMenu, RichTextLabel rich => rich.AccessibilityShowContextMenu, _ => null };
            var focusSource = FocusSource(control);
            e.SourceFocusAction = focusSource.GrabAccessibilityFocus; e.SourceBlurAction = focusSource.ReleaseFocus;
            if (node is LineEdit or TextEdit)
            {
                e.SourceTextSelectionAction = (anchor, start, focus, end) =>
                {
                    var fromLine = e.SourceTextLines.FindIndex(line => line.ID == anchor); var toLine = e.SourceTextLines.FindIndex(line => line.ID == focus);
                    if (fromLine < 0 || toLine < 0) throw new ArgumentException("Text selection refers to a retired or foreign source line.");
                    if (node is LineEdit line) line.AccessibilitySelect(start, end);
                    else ((TextEdit)node).AccessibilitySelect(fromLine, start, toLine, end);
                };
                e.SourceTextReplaceAction = text => { if (node is LineEdit line) line.AccessibilityReplace(text); else ((TextEdit)node).AccessibilityReplace(text); };
            }
            if (node is BaseButton button) e.SourceClickAction = button.ActivateAccessibility;
            if (node is Range range)
            {
                e.SourceSetValueAction = value => range.Value = value;
                e.SourceIncrementAction = () => range.Value += range.Step > 0 ? range.Step : 1;
                e.SourceDecrementAction = () => range.Value -= range.Step > 0 ? range.Step : 1;
            }
        }
        return e;
    }
    internal static void UpdateSourceActions(Element e, Control control)
    {
        var focus = FocusSource(control).CanReceiveGUIFocus;
        if (!e.SourcePrepared || focus != e.SourceCanFocus)
        {
            e.SourceCanFocus = focus;
            if (focus) { e.Actions[AccessibilityAction.Focus] = e.SourceFocusAction!; e.Actions[AccessibilityAction.Blur] = e.SourceBlurAction!; }
            else { e.Actions.Remove(AccessibilityAction.Focus); e.Actions.Remove(AccessibilityAction.Blur); }
            if (e.SourceClickAction is not null) e.Actions[AccessibilityAction.Click] = e.SourceClickAction;
            Runtime._native!.ActionsChanged(e);
        }
    }
    internal static void ValidateSourceFocus(int windowID, RID selected)
    {
        if (Runtime.Get(selected).WindowID != windowID) throw new InvalidOperationException("The focused accessibility element belongs to another native window.");
    }
    internal static void ReconcileSourceParent(Element e, RID parentID, int ordinal)
    {
        var parent = Runtime.Get(parentID); var slot = parent.Children.Count; var sourceIndex = 0;
        for (var index = 0; index < parent.Children.Count; index++)
            if (parent.Children[index].Source is not null && sourceIndex++ == ordinal) { slot = index; break; }
        if (e.Parent == parent && parent.Children.IndexOf(e) < slot) slot--;
        Runtime.Attach(parent, e, slot);
    }
    internal static void DetachSource(RID id, Node node)
    {
        if (TryGetSource(id, node) is not { } e) return;
        e.Source = null;
        if (e.IsWindow)
        {
            List<Exception>? errors = null;
            while (e.Children.Count != 0)
            {
                var index = e.Children.Count - 1; var child = e.Children[index];
                if (child.OwnedSubelement) { try { Runtime.Free(child); } catch (Exception error) { (errors ??= []).Add(error); } }
                else
                {
                    e.Children.RemoveAt(index); child.Parent = null;
                    try { Runtime._native!.ChildrenChanged(e, child, false, index); } catch (Exception error) { (errors ??= []).Add(error); }
                }
            }
            if (errors is not null) throw new AggregateException("Accessibility source retirement failed.", errors);
        }
        else Runtime.Free(e);
    }
}
