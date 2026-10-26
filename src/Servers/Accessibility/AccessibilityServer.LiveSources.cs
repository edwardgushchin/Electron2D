namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal static void RuntimeActionsChanged(Element e) => Runtime._native!.ActionsChanged(e);
    internal static void UpdateSourceLive(Element e, Control control)
    {
        var mode = control.AccessibilityLive;
        if (e.SourceLiveMode != mode) { UpdateSetLive(e.ID, mode); e.SourceLiveMode = mode; }
        var tooltip = control.TooltipText;
        if (!e.SourcePrepared || e.SourceTooltip != tooltip) { UpdateSetTooltip(e.ID, tooltip); e.SourceTooltip = tooltip; }
        var context = control switch { LineEdit line => line.ContextMenuEnabled, TextEdit edit => edit.ContextMenuEnabled, RichTextLabel rich => rich.ContextMenuEnabled, _ => false };
        AccessibilityPopupType? popup = control switch { OptionButton => AccessibilityPopupType.List, MenuButton or MenuBar => AccessibilityPopupType.Menu, ColorPickerButton => AccessibilityPopupType.Dialog, _ => context ? AccessibilityPopupType.Menu : null };
        SourcePopup(e, popup);
        if (!e.SourcePrepared)
        {
            e.Actions[AccessibilityAction.ShowTooltip] = e.SourceShowTooltipAction!; e.Actions[AccessibilityAction.HideTooltip] = e.SourceHideTooltipAction!;
            Runtime._native!.ActionsChanged(e);
        }
        if (!e.SourcePrepared || e.SourceContextMenuEnabled != context)
        {
            if (context && e.SourceContextMenuAction is not null) e.Actions[AccessibilityAction.ShowContextMenu] = e.SourceContextMenuAction;
            else e.Actions.Remove(AccessibilityAction.ShowContextMenu);
            e.SourceContextMenuEnabled = context; Runtime._native!.ActionsChanged(e);
        }
    }
    internal static void UpdateSourceLiveContent(Element e, Control control)
    {
        var content = control switch { Button button => button.AccessibleButtonText, LinkButton link => link.AccessibleLinkText, Label label => label.AccessibleLabelText, _ => null };
        if (e.SourceLiveContent == content) return; e.SourceLiveContent = content; if (content is not null) Runtime._native!.QueueAnnouncement(e, content);
    }
    internal static void SourcePopup(Element e, AccessibilityPopupType? popup)
    {
        if (e.SourcePopupType == popup) return;
        if (popup is { } kind) UpdateSetPopupType(e.ID, kind);
        else { e.PopupType = null; Runtime._native!.SemanticAttributesChanged(e); }
        e.SourcePopupType = popup;
    }
}
