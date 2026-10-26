namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal static void Announce(Control source, string message)
    {
        var runtime = Runtime; if (runtime._native is null || SceneAccessibility.NativeWindow(source) is not { } window) return;
        if (runtime._windows.TryGetValue(window.GetWindowID(), out var element)) runtime._native.Announce(element, message);
    }
    internal static void UpdateSourceDrag(Element e, Control control)
    {
        var tree = control.Tree!; var revision = tree.AccessibilityDragRevision;
        var editable = control switch { LineEdit line => line.Editable, TextEdit edit => edit.Editable, _ => true };
        var visible = control.IsVisibleInTree; var locale = TranslationServer.Culture.Name;
        if (e.SourceDragRevision == revision && !control.AccessibilityQueued && e.SourceDragEditable == editable && e.SourceDragVisible == visible && (e.SourceExtraInfo.Length == 0 || e.SourceDragContext is null || e.SourceDragContext.Value.Locale == locale)) return;
        e.SourceDragRevision = revision; e.SourceDragEditable = editable; e.SourceDragVisible = visible;
        DragPayload? payload = null;
        var description = visible ? tree.GetAccessibilityDragDescription(control, out payload) : null;
        (bool Accepted, string Description, string Locale)? context = null;
        if (description is not null)
        {
            var accepted = control.CanAccessibilityDrop(payload!);
            if (e.Retired || control.IsDisposed || !ReferenceEquals(control.Tree, tree)) return;
            if (tree.IsAccessibilityDragPayload(control, payload!)) context = (accepted, description, locale);
        }
        string info;
        if (context is { } value)
        {
            if (e.SourceDragContext is not { } previous || previous.Description != value.Description || previous.Locale != value.Locale)
            {
                e.SourceDragAcceptInfo = string.Format(control.Tr("{0} can be dropped here. Use {1} to drop, use {2} to cancel."), value.Description, control.Tr("Keyboard drag and drop"), control.Tr("Cancel"));
                e.SourceDragRejectInfo = string.Format(control.Tr("{0} cannot be dropped here. Use {1} to cancel."), value.Description, control.Tr("Cancel"));
            }
            e.SourceDragContext = value;
            info = value.Accepted ? e.SourceDragAcceptInfo : e.SourceDragRejectInfo;
        }
        else info = "";
        if (e.SourceExtraInfo == info) return; UpdateSetExtraInfo(e.ID, info); e.SourceExtraInfo = info;
    }
}
