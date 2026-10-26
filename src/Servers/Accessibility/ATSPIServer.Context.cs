using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static string KeyBinding(Element e, (AAction Action, int Jump) request)
    {
        if (e.Actions.ContainsKey(AAction.Click) ? request.Action != AAction.Click || request.Jump != 0 : ActionCount(e) == 0 || request != ActionAt(e, 0)) return "";
        string? source = null;
        try
        {
            if (e.CollectionSource is PopupMenu.MenuItems items) source = items.MenuOwner.AccessibilityShortcut(e);
            else if (e.Source is BaseButton { IsDisposed: false } button)
                source = button.Shortcut is { IsDisposed: false } shortcut && shortcut.HasValidEvent() ? shortcut.GetAsText() : "";
        }
        catch (Exception error) { throw new CallbackException(error); }
        if (e.Retired) throw new InvalidRequestException("The action source retired.", retired: true);
        if (source is not null)
        {
            if (e.SourceShortcut is null) { e.SourceShortcut = source; if (!e.ExplicitShortcut) e.Shortcut = source; }
            else if (e.SourceShortcut != source) { e.SourceShortcut = source; e.Shortcut = source; e.ExplicitShortcut = false; }
        }
        return e.Shortcut.Length == 0 ? "" : ";;" + e.Shortcut;
    }
    private static void AddContextAttributes(nint builder, Element e)
    {
        if (e.ClassName.Length != 0) g_variant_builder_add_value(builder, g_variant_new_dict_entry(S("class"), S(e.ClassName)));
        if (e.RoleDescription.Length != 0) g_variant_builder_add_value(builder, g_variant_new_dict_entry(S("roledescription"), S(e.RoleDescription)));
    }
}
