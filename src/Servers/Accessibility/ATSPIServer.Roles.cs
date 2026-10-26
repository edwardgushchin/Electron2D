using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using static Electron2D.GIONative;

namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static uint NativeRole(AccessibilityServer.Element e) => e.IsApplication ? 75u : e.TextProtected ? 40u : e.Role switch
    {
        Role.Unknown => 67u,
        Role.DefaultButton => 43u,
        Role.Audio => 106u,
        Role.Video => 107u,
        Role.StaticText => 116u,
        Role.Container => 20u,
        Role.Panel => 39u,
        Role.Button => 43u,
        Role.Link => 88u,
        Role.CheckBox => 7u,
        Role.RadioButton => 44u,
        Role.CheckButton => 62u,
        Role.ScrollBar => 48u,
        Role.ScrollView => 49u,
        Role.Splitter => 53u,
        Role.Slider => 51u,
        Role.SpinButton => 52u,
        Role.ProgressIndicator => 42u,
        Role.TextField => 79u,
        Role.MultilineTextField => 61u,
        Role.ColorPicker => 9u,
        Role.Table => 55u,
        Role.Cell => 56u,
        Role.Row => 90u,
        Role.RowGroup => 99u,
        Role.RowHeader => 47u,
        Role.ColumnHeader => 10u,
        Role.Tree => 65u,
        Role.TreeItem => 91u,
        Role.List => 31u,
        Role.ListItem => 32u,
        Role.ListBox => 98u,
        Role.ListBoxOption => 32u,
        Role.TabBar => 38u,
        Role.Tab => 37u,
        Role.TabPanel => 39u,
        Role.MenuBar => 34u,
        Role.Menu => 33u,
        Role.MenuItem => 35u,
        Role.MenuItemCheckBox => 8u,
        Role.MenuItemRadio => 45u,
        Role.Image => 27u,
        Role.Window => 23u,
        Role.TitleBar => 104u,
        Role.Dialog => 16u,
        Role.Tooltip => 64u,
        Role.Region => 110u,
        Role.TextRun => 61u,
        _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };
    private static string NativeRoleName(AccessibilityServer.Element e) => e.IsApplication ? "application" : e.TextProtected ? "password text" : e.Role switch
    {
        Role.Unknown => "unknown",
        Role.DefaultButton => "push button",
        Role.Audio => "audio",
        Role.Video => "video",
        Role.StaticText => "static",
        Role.Container => "filler",
        Role.Panel => "panel",
        Role.Button => "push button",
        Role.Link => "link",
        Role.CheckBox => "check box",
        Role.RadioButton => "radio button",
        Role.CheckButton => "toggle button",
        Role.ScrollBar => "scroll bar",
        Role.ScrollView => "scroll pane",
        Role.Splitter => "split pane",
        Role.Slider => "slider",
        Role.SpinButton => "spin button",
        Role.ProgressIndicator => "progress bar",
        Role.TextField => "entry",
        Role.MultilineTextField => "text",
        Role.ColorPicker => "color chooser",
        Role.Table => "table",
        Role.Cell => "table cell",
        Role.Row => "table row",
        Role.RowGroup => "grouping",
        Role.RowHeader => "row header",
        Role.ColumnHeader => "column header",
        Role.Tree => "tree",
        Role.TreeItem => "tree item",
        Role.List => "list",
        Role.ListItem => "list item",
        Role.ListBox => "list box",
        Role.ListBoxOption => "list item",
        Role.TabBar => "page tab list",
        Role.Tab => "page tab",
        Role.TabPanel => "panel",
        Role.MenuBar => "menu bar",
        Role.Menu => "menu",
        Role.MenuItem => "menu item",
        Role.MenuItemCheckBox => "check menu item",
        Role.MenuItemRadio => "radio menu item",
        Role.Image => "image",
        Role.Window => "frame",
        Role.TitleBar => "title bar",
        Role.Dialog => "dialog",
        Role.Tooltip => "tool tip",
        Role.Region => "landmark",
        Role.TextRun => "text",
        _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };
    private static nint States(AccessibilityServer.Element e)
    {
        ulong bits = 0;
        void Set(int bit, bool value) { if (value) bits |= 1UL << bit; }
        var showing = Showing(e);
        Set(1, e.IsWindow && e.WindowFocused);
        Set(6, e.Retired);
        Set(8, !e.Flag(Flag.Disabled));
        Set(24, !e.Flag(Flag.Disabled));
        Set(30, Visible(e));
        Set(25, showing);
        Set(11, e.Actions.ContainsKey(AccessibilityServer.AccessibilityAction.Focus) && !e.Flag(Flag.Disabled));
        Set(12, e.Focused && Visible(e) && WindowRoot(e) is { WindowFocused: true });
        Set(4, e.Checked);
        Set(41, e.Role is Role.CheckBox or Role.CheckButton or Role.RadioButton or Role.MenuItemCheckBox or Role.MenuItemRadio);
        Set(3, e.Flag(Flag.Busy));
        Set(18, e.Flag(Flag.Multiselectable));
        Set(33, e.Flag(Flag.Required));
        Set(40, e.Flag(Flag.Visited));
        Set(16, e.Flag(Flag.Modal));
        Set(43, e.Flag(Flag.ReadOnly) || e.SourceTextPrepared && !e.SourceTextEditable);
        Set(7, e.ReplaceTextAction is not null && !e.Flag(Flag.ReadOnly));
        Set(38, e.SetTextSelectionAction is not null);
        Set(17, e.Role == Role.MultilineTextField);
        Set(26, e.Role == Role.TextField);
        Set(31, AccessibilityServer.RelatedTarget(e, AccessibilityServer.RelationKind.ActiveDescendant) is not null);
        Set(36, AccessibilityServer.RelatedTarget(e, AccessibilityServer.RelationKind.ErrorMessage) is not null);
        Set(39, e.Role == Role.DefaultButton);
        return Array("u", [g_variant_new_uint32((uint)bits), g_variant_new_uint32((uint)(bits >> 32))]);
    }
    private readonly List<AccessibilityServer.Element> _visibilityNodes = [];
    private void VisibilityChanged(AccessibilityServer.Element e)
    {
        var start = _visibilityNodes.Count;
        try
        {
            _visibilityNodes.Add(e);
            for (var index = start; index < _visibilityNodes.Count; index++)
            {
                var node = _visibilityNodes[index]; StateChanged(node, "visible", Visible(node)); StateChanged(node, "showing", Showing(node));
                foreach (var child in node.Children) _visibilityNodes.Add(child);
            }
        }
        finally { _visibilityNodes.RemoveRange(start, _visibilityNodes.Count - start); }
    }

    internal void StateChanged(AccessibilityServer.Element e, Flag flag, bool value)
    {
        if (flag == Flag.Hidden) { VisibilityChanged(e); return; }
        if (flag == Flag.Disabled) { StateChanged(e, "enabled", !value); StateChanged(e, "sensitive", !value); return; }
        var state = flag switch { Flag.Multiselectable => "multiselectable", Flag.Required => "required", Flag.Visited => "visited", Flag.Busy => "busy", Flag.Modal => "modal", Flag.ReadOnly => "read-only", _ => "" };
        if (state.Length != 0) StateChanged(e, state, value);
    }
}
